using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.BankAccounts.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Application.BankAccounts.Payloads.Validators;
using BigBang.App.Cloud.ERP.Accounting.Application.PersonAccounts;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Common.Helpers;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.App.Cloud.ERP.Accounting.Domain.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.BankAccounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Identity;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using BigBang.WebServer.Common.Attributes;

namespace BigBang.App.Cloud.ERP.Accounting.Application.BankAccounts
{
    [Service(ServiceType = typeof(IBankAccountService), InstanceMode = InstanceMode.Scoped, Requestable = false)]
    internal class BankAccountService : IBankAccountService
    {
        private readonly IAccountingIdentityService _accountingIdentityService;
        private readonly IVoucherService _voucherService;
        private readonly IPersonAccountService _personAccountService;
        private readonly IBankAccountRepository _bankAccountRepository;
        private readonly IVoucherRepository _voucherRepository;
        private readonly IAccountRepository _accountRepository;

        public BankAccountService(IAccountingIdentityService accountingIdentityService,
            IVoucherService voucherService,
            IPersonAccountService personAccountService,
            IBankAccountRepository bankAccountRepository,
            IVoucherRepository voucherRepository,
            IAccountRepository accountRepository)
        {
            _accountingIdentityService = accountingIdentityService;
            _voucherService = voucherService;
            _personAccountService = personAccountService;
            _bankAccountRepository = bankAccountRepository;
            _voucherRepository = voucherRepository;
            _accountRepository = accountRepository;
        }

        public async Task<IEnumerable<BankAccountResponse>> GetBankAccountsAsync()
        {
            var businessId = await _accountingIdentityService.GetBusinessIdAsync();

            var bankAccounts = await _bankAccountRepository.GetListByBusinessIdAsync(businessId);

            return bankAccounts.Select(account => new BankAccountResponse(
                account.Id,
                account.Bank,
                account.HolderName,
                account.Title,
                account.ShebaNumber,
                account.CardNumber,
                account.Balance
            ));
        }

        public async Task<BankAccountResponse> GetBankAccountAsync(Guid id)
        {
            var bankAccount = await _bankAccountRepository.GetAsync(id);
            if (bankAccount == null) throw ExceptionHelper.NotFound(Messages.Entity_BankAccount);

            return new BankAccountResponse(
                bankAccount.Id,
                bankAccount.Bank,
                bankAccount.HolderName,
                bankAccount.Title,
                bankAccount.ShebaNumber,
                bankAccount.CardNumber,
                bankAccount.Balance
            );
        }

        public async Task<Guid> AddBankAccountAsync(BankAccountRequest request)
        {
            var validator = new BankAccountValidator();
            var result = await validator.ValidateAsync(request);

            if (!result.IsValid) throw ExceptionHelper.BadRequest(result.Errors);

            var businessId = await _accountingIdentityService.GetBusinessIdAsync();

            var bankAccount = new ACC_BankAccount
            {
                Id = Guid.NewGuid(),
                Bank = request.Bank,
                HolderName = request.HolderName,
                Title = request.Title,
                ShebaNumber = request.ShebaNumber,
                CardNumber = request.CardNumber,
                Balance = 0,
                Business = new ACC_Business { Id = businessId }
            };

            await _bankAccountRepository.AddAsync(bankAccount);

            if (request.Balance > 0)
                await RegisterDepositVoucherAsync(bankAccount.Id, request.Balance);

            return bankAccount.Id;
        }

        public async Task<Guid> UpdateBankAccountAsync(Guid id, BankAccountRequest request)
        {
            var validator = new BankAccountValidator();
            var result = await validator.ValidateAsync(request);

            if (!result.IsValid) throw ExceptionHelper.BadRequest(result.Errors);

            var bankAccount = await _bankAccountRepository.GetAsync(id);
            if (bankAccount == null) throw ExceptionHelper.NotFound(Messages.Entity_BankAccount);

            bankAccount.Bank = request.Bank;
            bankAccount.HolderName = request.HolderName;
            bankAccount.Title = request.Title;
            bankAccount.ShebaNumber = request.ShebaNumber;
            bankAccount.CardNumber = request.CardNumber;
            bankAccount.Bank = request.Bank;

            await _bankAccountRepository.UpdateAsync(bankAccount);

            if (request.Balance > 0 && bankAccount.Balance == 0)
                await RegisterDepositVoucherAsync(bankAccount.Id, request.Balance);

            return bankAccount.Id;
        }

        public async Task DeleteBankAccountAsync(Guid id)
        {
            var bankAccount = await _bankAccountRepository.GetAsync(id);
            if (bankAccount == null) throw ExceptionHelper.NotFound(Messages.Entity_BankAccount);

            var articlesExist = await _voucherRepository.ArticlesExistAsync(LookupType.Bank, id);
            if (articlesExist) throw ExceptionHelper.BadRequest(string.Format(Messages.Error_EntityCantBeDeletedDueToHavingVoucher, Messages.Entity_BankAccount));

            await _bankAccountRepository.RemoveAsync(id);
        }

        public async Task<PaginatedVouchersListByDetailedAccountResponse> GetBankAccountVouchersAsync(BankAccountVouchersRequest request)
        {
            var validator = new BankAccountVouchersRequestValidator();

            var result = await validator.ValidateAsync(request);

            if (!result.IsValid) throw ExceptionHelper.BadRequest(result.Errors);

            var fiscalPeriodId = await _accountingIdentityService.GetFiscalPeriodIdAsync();

            var vouchers = await _voucherRepository.GetListByLookupIdAsync(request.FromDate, request.ToDate,
                request.PageSize, request.PageNumber, request.Id,
                fiscalPeriodId, LookupType.Bank) ?? [];

            var voucherResponses = vouchers.Select(voucher => new VoucherByDetailedAccountResponse(voucher.Id, (VoucherTemplate)voucher.VoucherTemplate.Id,
                voucher.Title, voucher.EffectiveDate, voucher.Amount, voucher.GetListByLookupId(request.Id))).ToList();

            var totalCount = await _voucherRepository.GetTotalCountAsync(fiscalPeriodId, request.FromDate, request.ToDate, request.Id, LookupType.Bank);

            return new PaginatedVouchersListByDetailedAccountResponse(request.PageSize, request.PageNumber, totalCount, voucherResponses);
        }

        private async Task RegisterDepositVoucherAsync(Guid bankAccountId, long balance)
        {
            var fiscalPeriodId = await _accountingIdentityService.GetFiscalPeriodIdAsync();

            var bank = await _accountRepository.GetByNameAndFiscalPeriodIdAsync(fiscalPeriodId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Bank)));
            var subsidiaryEquity = await _accountRepository.GetByNameAndFiscalPeriodIdAsync(fiscalPeriodId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_SubsidiaryEquity)));

            var ownerPersonAccount = await _personAccountService.GetOwnerPersonAccountAsync();

            await _voucherService.RegisterVoucherAsync(new RegisterVoucherRequest(
                VoucherTemplate.Deposit,
                null,
                DateTime.Now,
                null,
                [
                    new ArticleRequest(bank.Id, bankAccountId, balance, Currency.Rial, null, null, ArticleType.Debit, false),
                    new ArticleRequest(subsidiaryEquity.Id, ownerPersonAccount.Id, balance, Currency.Rial, null, null, ArticleType.Credit, false)
                ]));
        }
    }
}