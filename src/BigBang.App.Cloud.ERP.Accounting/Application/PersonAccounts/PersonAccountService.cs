using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.PersonAccounts.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Application.PersonAccounts.Payloads.Validators;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Common.Helpers;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.App.Cloud.ERP.Accounting.Domain.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.App.Cloud.ERP.Accounting.Domain.PersonAccounts;
using BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.PersonAccounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Identity;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using BigBang.WebServer.Common.Attributes;
using NHibernate.Util;

namespace BigBang.App.Cloud.ERP.Accounting.Application.PersonAccounts
{
    [Service(ServiceType = typeof(IPersonAccountService), InstanceMode = InstanceMode.Scoped, Requestable = false)]
    internal class PersonAccountService : IPersonAccountService
    {
        private readonly IAccountingIdentityService _accountingIdentityService;
        private readonly IPersonAccountRepository _personAccountRepository;
        private readonly IVoucherRepository _voucherRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IVoucherService _voucherService;

        public PersonAccountService(
            IAccountingIdentityService accountingIdentityService,
            IPersonAccountRepository personAccountRepository,
            IVoucherRepository voucherRepository,
            IAccountRepository accountRepository,
            IVoucherService voucherService)
        {
            _accountingIdentityService = accountingIdentityService;
            _personAccountRepository = personAccountRepository;
            _voucherRepository = voucherRepository;
            _accountRepository = accountRepository;
            _voucherService = voucherService;
        }

        public async Task<IEnumerable<PersonAccountResponse>> GetPersonAccountsAsync(IList<PersonRoleType> personRoleTypes)
        {
            var businessId = await _accountingIdentityService.GetBusinessIdAsync();

            var personAccounts = await _personAccountRepository.GetListByBusinessIdAndRolesAsync(businessId, personRoleTypes);

            return personAccounts.Select(account => new PersonAccountResponse(
                account.Id,
                account.FirstName,
                account.LastName,
                account.MobileNumber,
                account.PersonAccountRoles.Select(roleType => roleType.PersonRoleTypeId).ToList()
                ));
        }

        public async Task<PersonAccountResponse> GetPersonAccountAsync(Guid id)
        {
            var personAccount = await _personAccountRepository.GetAsync(id);
            if (personAccount == null) throw ExceptionHelper.NotFound(Messages.Entity_PersonAccount);

            return new PersonAccountResponse(
                personAccount.Id,
                personAccount.FirstName,
                personAccount.LastName,
                personAccount.MobileNumber,
                personAccount.PersonAccountRoles.Select(accountRole => accountRole.PersonRoleTypeId).ToList()
            );
        }

        public async Task<PersonAccountResponse> GetOwnerPersonAccountAsync()
        {
            var businessId = await _accountingIdentityService.GetBusinessIdAsync();
            var ownerPersonAccounts = await _personAccountRepository.GetListByBusinessIdAndRolesAsync(businessId, [PersonRoleType.BusinessOwner]);
            var ownerPersonAccount = ownerPersonAccounts.SingleOrDefault();

            if (ownerPersonAccount == null)
            {
                throw ExceptionHelper.NotFound(Messages.Entity_PersonAccount);
            }

            return new PersonAccountResponse(
                ownerPersonAccount.Id,
                ownerPersonAccount.FirstName,
                ownerPersonAccount.LastName,
                ownerPersonAccount.MobileNumber,
                ownerPersonAccount.PersonAccountRoles.Select(accountRole => accountRole.PersonRoleTypeId).ToList()
            );
        }

        public async Task<Guid> AddPersonAccountAsync(AddPersonAccountRequest request)
        {
            var validator = new AddPersonAccountValidator();
            var result = await validator.ValidateAsync(request);

            if (!result.IsValid) throw ExceptionHelper.BadRequest(result.Errors);

            var businessId = await _accountingIdentityService.GetBusinessIdAsync();
            var personAccount = PersonAccountFactory.Create(businessId, request.FirstName, request.LastName, request.MobileNumber, request.RoleTypes);

            await _personAccountRepository.AddAsync(personAccount);

            var fiscalPeriodId = await _accountingIdentityService.GetFiscalPeriodIdAsync();

            if (!request.InitialStatus.HasValue) return personAccount.Id;

            var ownerPersonAccount = await GetOwnerPersonAccountAsync();

            var subsidiaryEquity = await _accountRepository.GetByNameAndFiscalPeriodIdAsync(fiscalPeriodId,
                AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_SubsidiaryEquity)));

            if (request.InitialStatus.Value == AccountNature.Debtor)
            {
                await RegisterVoucherForDebtorPersonAccountAsync(personAccount.Id, ownerPersonAccount.Id,
                    subsidiaryEquity, request.Amount, fiscalPeriodId);
            }
            else
            {
                await RegisterVoucherForCreditorPersonAccountAsync(personAccount.Id, ownerPersonAccount.Id,
                    subsidiaryEquity, request.Amount, fiscalPeriodId);
            }

            return personAccount.Id;
        }

        public async Task<Guid> UpdatePersonAccountAsync(Guid id, UpdatePersonAccountRequest request)
        {
            var ownerPersonAccount = await GetOwnerPersonAccountAsync();

            var validator = new UpdatePersonAccountValidator(ownerPersonAccount.Id, id);
            var result = await validator.ValidateAsync(request);

            if (!result.IsValid) throw ExceptionHelper.BadRequest(result.Errors);

            var personAccount = await _personAccountRepository.GetAsync(id);
            if (personAccount == null) throw ExceptionHelper.NotFound(Messages.Entity_PersonAccount);

            await _personAccountRepository.RemoveAllRolesAsync(personAccount);

            personAccount.FirstName = request.FirstName;
            personAccount.LastName = request.LastName;
            personAccount.MobileNumber = request.MobileNumber;

            personAccount.CreateRoleTypes(request.RoleTypes);

            await _personAccountRepository.UpdateAsync(personAccount);

            return personAccount.Id;
        }

        public async Task DeletePersonAccountAsync(Guid id)
        {
            var personAccount = await _personAccountRepository.GetAsync(id);
            if (personAccount == null) throw ExceptionHelper.NotFound(Messages.Entity_PersonAccount);

            if (personAccount.PersonAccountRoles.Any(role => role.PersonRoleTypeId == PersonRoleType.BusinessOwner))
                throw ExceptionHelper.Forbidden(Messages.Error_BuisnessOwnerCannotBeDeleted);

            var articlesExist = await _voucherRepository.ArticlesExistAsync(LookupType.Person, id);
            if (articlesExist) throw ExceptionHelper.BadRequest(string.Format(Messages.Error_EntityCantBeDeletedDueToHavingVoucher, Messages.Entity_PersonAccount));

            await _personAccountRepository.RemoveAsync(id);
        }

        public async Task<long> GetTotalDebtsAsync(Guid id)
        {
            var fiscalPeriodId = await _accountingIdentityService.GetFiscalPeriodIdAsync();

            var totalDebits = await _voucherRepository.GetTotalDebtsAsync(fiscalPeriodId, id, LookupType.Person, ArticleType.Debit, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_LiabilitiesToOthers)));
            var totalCredits = await _voucherRepository.GetTotalDebtsAsync(fiscalPeriodId, id, LookupType.Person, ArticleType.Credit, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_LiabilitiesToOthers)));

            return totalDebits - totalCredits;
        }

        public async Task<PaginatedVouchersListByDetailedAccountResponse> GetPersonAccountVouchersAsync(PersonAccountVouchersRequest request)
        {
            var validator = new PersonAccountVouchersRequestValidator();

            var result = await validator.ValidateAsync(request);

            if (!result.IsValid) throw ExceptionHelper.BadRequest(result.Errors);

            var fiscalPeriodId = await _accountingIdentityService.GetFiscalPeriodIdAsync();

            var vouchers = await _voucherRepository.GetListByLookupIdAsync(request.FromDate, request.ToDate,
                request.PageSize, request.PageNumber, request.Id,
                fiscalPeriodId, LookupType.Person) ?? [];

            var voucherResponses = vouchers.Select(voucher => new VoucherByDetailedAccountResponse(voucher.Id, (VoucherTemplate)voucher.VoucherTemplate.Id,
                voucher.Title, voucher.EffectiveDate, voucher.Amount, voucher.GetListByLookupId(request.Id))).ToList();

            var totalCount = await _voucherRepository.GetTotalCountAsync(fiscalPeriodId, request.FromDate, request.ToDate, request.Id, LookupType.Person);

            return new PaginatedVouchersListByDetailedAccountResponse(request.PageSize, request.PageNumber, totalCount, voucherResponses);
        }

        private async Task RegisterVoucherForDebtorPersonAccountAsync(Guid personAccountId, Guid ownerPersonAccountId, ACC_Account subsidiaryEquity, long amount, Guid fiscalPeriodId)
        {
            var liabilitiesToOthers = await _accountRepository.GetByNameAndFiscalPeriodIdAsync(fiscalPeriodId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_LiabilitiesToOthers)));

            await _voucherService.RegisterVoucherAsync(new RegisterVoucherRequest(
                VoucherTemplate.Custom,
                null,
                DateTime.Now,
                null,
                [
                    new ArticleRequest(liabilitiesToOthers.Id, personAccountId, amount, Currency.Rial, null, null, ArticleType.Credit, false),
                    new ArticleRequest(subsidiaryEquity.Id, ownerPersonAccountId, amount, Currency.Rial, null, null, ArticleType.Debit, false)
                ]
            ));
        }

        private async Task RegisterVoucherForCreditorPersonAccountAsync(Guid personAccountId, Guid ownerPersonAccountId, ACC_Account subsidiaryEquity, long amount, Guid fiscalPeriodId)
        {
            var receivableFromOthers = await _accountRepository.GetByNameAndFiscalPeriodIdAsync(fiscalPeriodId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ReceivableFromOthers)));

            await _voucherService.RegisterVoucherAsync(new RegisterVoucherRequest(
                VoucherTemplate.Custom,
                null,
                DateTime.Now,
                null,
                [
                    new ArticleRequest(receivableFromOthers.Id, personAccountId, amount, Currency.Rial, null, null, ArticleType.Debit, false),
                    new ArticleRequest(subsidiaryEquity.Id, ownerPersonAccountId, amount, Currency.Rial, null, null, ArticleType.Credit, false)
                ]
            ));
        }
    }
}