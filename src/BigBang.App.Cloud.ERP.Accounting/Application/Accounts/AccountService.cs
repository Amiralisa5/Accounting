using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.Accounts.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Application.Accounts.Payloads.Validators;
using BigBang.App.Cloud.ERP.Accounting.Application.Enums;
using BigBang.App.Cloud.ERP.Accounting.Common;
using BigBang.App.Cloud.ERP.Accounting.Common.Helpers;
using BigBang.App.Cloud.ERP.Accounting.Domain.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.BankAccounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.PersonAccounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Products;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Identity;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using BigBang.WebServer.Common.Attributes;
using FluentValidation;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Accounts
{
    [Service(ServiceType = typeof(IAccountService), InstanceMode = InstanceMode.Scoped, Requestable = false)]
    internal class AccountService : IAccountService
    {
        private readonly IAccountingIdentityService _accountingIdentityService;
        private readonly IAccountRepository _accountRepository;
        private readonly IVoucherRepository _voucherRepository;
        private readonly IBankAccountRepository _bankAccountRepository;
        private readonly IProductRepository _productRepository;
        private readonly IPersonAccountRepository _personAccountRepository;
        private readonly IEnumService _enumService;

        public AccountService(IAccountingIdentityService accountingIdentityService,
            IAccountRepository accountRepository, IVoucherRepository voucherRepository, IBankAccountRepository bankAccountRepository, IProductRepository productRepository, IPersonAccountRepository personAccountRepository, IEnumService enumService)
        {
            _accountingIdentityService = accountingIdentityService;
            _accountRepository = accountRepository;
            _voucherRepository = voucherRepository;
            _bankAccountRepository = bankAccountRepository;
            _productRepository = productRepository;
            _personAccountRepository = personAccountRepository;
            _enumService = enumService;
        }

        public async Task<AccountTreeResponse> GetAccountsAsync()
        {
            var fiscalPeriodId = await _accountingIdentityService.GetFiscalPeriodIdAsync();

            var accounts = await _accountRepository.GetListByFiscalPeriodIdAsync(fiscalPeriodId);

            return accounts.Create();
        }

        public async Task<IList<SubsidiaryAccountFinancialBalanceResponse>> GetSubsidiaryAccountFinancialBalanceAsync(SubsidiaryAccountFinancialBalanceRequest request)
        {
            var validator = new SubsidiaryAccountsFinancialBalanceRequestValidator();

            var result = await validator.ValidateAsync(request);

            if (!result.IsValid) throw ExceptionHelper.BadRequest(result.Errors);

            var fiscalPeriodId = await _accountingIdentityService.GetFiscalPeriodIdAsync();

            var accounts = await _accountRepository.GetListByFiscalPeriodIdAsync(fiscalPeriodId);

            var generalAccounts = accounts
                .Where(account => account.Level == 1)
                .OrderBy(account => account.Code);

            var subsidiaryAccountFinancialBalanceData = await _voucherRepository.GetSubsidiaryAccountFinancialBalanceDataAsync(fiscalPeriodId, request.FromDate, request.ToDate);

            var subsidiaryAccountFinancialBalanceResponses = new List<SubsidiaryAccountFinancialBalanceResponse>();

            foreach (var generalAccount in generalAccounts)
            {
                var subsidiaryAccountResponses = new List<SubsidiaryAccountResponse>();

                var subsidiaryAccounts = accounts
                    .Where(account => account.Level == 2 && account.ParentAccount.Id == generalAccount.Id)
                    .OrderBy(account => account.Code);

                foreach (var subsidiaryAccount in subsidiaryAccounts)
                {
                    var subsidiaryAccountData = subsidiaryAccountFinancialBalanceData.SingleOrDefault(subsidiaryAccountData => subsidiaryAccountData.Id == subsidiaryAccount.Id);

                    var totalDebit = subsidiaryAccountData?.TotalDebit ?? 0;
                    var totalCredit = subsidiaryAccountData?.TotalCredit ?? 0;

                    subsidiaryAccountResponses.Add(new SubsidiaryAccountResponse(subsidiaryAccount.Id, subsidiaryAccount.Code,
                        subsidiaryAccount.Name, subsidiaryAccount.DisplayName, totalDebit, totalCredit,
                        subsidiaryAccount.Nature.GetValueOrDefault()));

                }

                subsidiaryAccountFinancialBalanceResponses.Add(new SubsidiaryAccountFinancialBalanceResponse(generalAccount.Id,
                    generalAccount.Code, generalAccount.Name, generalAccount.DisplayName,
                    generalAccount.Nature.GetValueOrDefault(), subsidiaryAccountResponses));
            }

            return subsidiaryAccountFinancialBalanceResponses;
        }

        public async Task<PaginatedHeaderAndDetailsResponse<DetailedAccountFinancialBalanceResponse, DetailedAccountResponse>> GetDetailedAccountFinancialBalanceAsync(DetailedAccountFinancialBalanceRequest request)
        {
            var validator = new DetailedAccountsFinancialBalanceRequestValidator();

            var result = await validator.ValidateAsync(request);

            if (!result.IsValid) throw ExceptionHelper.BadRequest(result.Errors);

            var fiscalPeriodId = await _accountingIdentityService.GetFiscalPeriodIdAsync();

            var account = await _accountRepository.GetAsync(request.Id);

            if (account is null) throw ExceptionHelper.NotFound(Messages.Entity_Account);

            var detailedAccountFinancialBalanceData = await _voucherRepository.GetDetailedAccountFinancialBalanceDataAsync(request, fiscalPeriodId, account.LookupType.GetValueOrDefault());

            var detailedAccountResponses = detailedAccountFinancialBalanceData.Select(detailedAccount =>
                new DetailedAccountResponse(detailedAccount.Id, detailedAccount.Name, detailedAccount.TotalDebit,
                    detailedAccount.TotalCredit, detailedAccount.Difference)).ToList();

            var detailedAccountFinancialBalanceResponse = new DetailedAccountFinancialBalanceResponse(account.Id, account.Code,
                account.Name,
                account.DisplayName, account.Nature.GetValueOrDefault());

            return new PaginatedHeaderAndDetailsResponse<DetailedAccountFinancialBalanceResponse, DetailedAccountResponse>(
                request.PageSize, request.PageNumber, detailedAccountFinancialBalanceResponse, detailedAccountResponses);
        }

        public async Task<DetailedAccountFinancialBalanceTotalResponse> CalculateDetailedAccountFinancialBalanceTotalAsync(DetailedAccountFinancialBalanceTotalRequest request)
        {
            var validator = new DetailedAccountFinancialBalanceTotalRequestValidator();

            var result = await validator.ValidateAsync(request);

            if (!result.IsValid) throw ExceptionHelper.BadRequest(result.Errors);

            var fiscalPeriodId = await _accountingIdentityService.GetFiscalPeriodIdAsync();

            var account = await _accountRepository.GetAsync(request.Id);

            if (account is null) throw ExceptionHelper.NotFound(Messages.Entity_Account);

            var detailedAccountFinancialBalanceTotal = await _voucherRepository.CalculateDetailedAccountFinancialBalanceTotalAsync(request, fiscalPeriodId, account.LookupType.GetValueOrDefault());

            var totalCount = detailedAccountFinancialBalanceTotal.Count;
            var totalDebit = detailedAccountFinancialBalanceTotal.Sum(detailedAccount => detailedAccount.TotalDebit);
            var totalCredit = detailedAccountFinancialBalanceTotal.Sum(detailedAccount => detailedAccount.TotalCredit);
            var totalDifference = detailedAccountFinancialBalanceTotal.Sum(detailedAccount => detailedAccount.Difference);

            return new DetailedAccountFinancialBalanceTotalResponse(totalCount, totalDebit, totalCredit, totalDifference);
        }
    }
}