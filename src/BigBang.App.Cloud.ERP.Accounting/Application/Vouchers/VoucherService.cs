using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.Enums;
using BigBang.App.Cloud.ERP.Accounting.Application.Files;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads.Validators;
using BigBang.App.Cloud.ERP.Accounting.Common.Helpers;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.App.Cloud.ERP.Accounting.Domain.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.BankAccounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.FiscalPeriods;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.PersonAccounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Products;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.VoucherTemplates;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Identity;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using BigBang.Common;
using BigBang.WebServer.Common;
using BigBang.WebServer.Common.Attributes;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Vouchers
{
    [Service(ServiceType = typeof(IVoucherService), InstanceMode = InstanceMode.PerRequest, Requestable = false)]
    internal class VoucherService : IVoucherService
    {
        private readonly IAccountingIdentityService _accountingIdentityService;
        private readonly IVoucherRepository _voucherRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IFiscalPeriodRepository _fiscalPeriodRepository;
        private readonly IBankAccountRepository _bankAccountRepository;
        private readonly IPersonAccountRepository _personAccountRepository;
        private readonly IVoucherTemplateRepository _voucherTemplateRepository;
        private readonly IProductRepository _productRepository;
        private readonly IEnumService _enumService;
        private readonly IFileService _fileService;
        public VoucherService(IAccountingIdentityService accountingIdentityService,
            IVoucherRepository voucherRepository,
            IAccountRepository accountRepository,
            IBankAccountRepository bankAccountRepository,
            IPersonAccountRepository personAccountRepository,
            IFiscalPeriodRepository fiscalPeriodRepository,
            IVoucherTemplateRepository voucherTemplateRepository,
            IProductRepository productRepository,
            IEnumService enumService,
            IFileService fileService)
        {
            _accountingIdentityService = accountingIdentityService;
            _voucherRepository = voucherRepository;
            _accountRepository = accountRepository;
            _bankAccountRepository = bankAccountRepository;
            _personAccountRepository = personAccountRepository;
            _fiscalPeriodRepository = fiscalPeriodRepository;
            _voucherTemplateRepository = voucherTemplateRepository;
            _productRepository = productRepository;
            _enumService = enumService;
            _fileService = fileService;
        }

        public async Task<VoucherResponse> RegisterVoucherAsync(RegisterVoucherRequest request)
        {
            var fiscalPeriodId = await _accountingIdentityService.GetFiscalPeriodIdAsync();
            var fiscalPeriod = await _fiscalPeriodRepository.GetAsync(fiscalPeriodId);

            var validator = new RegisterVoucherRequestValidator(fiscalPeriod);

            var result = await validator.ValidateAsync(request);

            if (!result.IsValid) throw ExceptionHelper.BadRequest(result.Errors);


            var templateStrategyFactory = new TemplateStrategyFactory(
                _voucherRepository,
                _accountRepository,
                _bankAccountRepository,
                _personAccountRepository,
                _voucherTemplateRepository,
                _productRepository,
                _enumService);

            var templateStrategy = templateStrategyFactory.Create(request.Template);

            var voucher = await templateStrategy.RegisterAsync(request, fiscalPeriod);

            //TODO: check on validationRules and write test
            if (request.FileId.HasValue && request.FileId.Value != Guid.Empty)
                await _fileService.UpdateFileOwnerAsync(request.FileId.Value, voucher.Id);

            return new VoucherResponse(voucher.Id, request.Template, voucher.Title, voucher.Number, voucher.EffectiveDate, voucher.Amount);
        }

        public async Task<PaginatedVouchersListResponse> GetVoucherListAsync(GetVoucherListRequest request)
        {
            var fiscalPeriodId = await _accountingIdentityService.GetFiscalPeriodIdAsync();

            var vouchers = await _voucherRepository.GetListAsync(fiscalPeriodId, request.Template, request.PageSize, request.PageNumber) ?? [];

            var voucherResponses = vouchers.Select(voucher => new VoucherResponse(voucher.Id, request.Template, voucher.Title, voucher.Number,
                voucher.EffectiveDate, voucher.Amount)).ToList();
            var totalCount = await _voucherRepository.GetTotalCountAsync(fiscalPeriodId, request.Template);

            return new PaginatedVouchersListResponse(request.PageSize, request.PageNumber, totalCount, voucherResponses);
        }

        public async Task<VoucherDetailsResponse> GetVoucherAsync(Guid id)
        {
            var voucher = await _voucherRepository.GetAsync(id);

            if (voucher == null) throw ExceptionHelper.NotFound(Messages.Entity_Voucher);

            var fileId = await _voucherRepository.GetFileIdAsync(voucher.Id);
            var properties = await GetLookupPropertiesAsync(voucher.Articles);

            return new VoucherDetailsResponse(
                voucher.Id,
                voucher.VoucherTemplate == null ? null : voucher.VoucherTemplate.Id,
                voucher.Number,
                voucher.Title,
                voucher.EffectiveDate,
                voucher.Description,
                voucher.Amount,
                fileId,
                voucher.Articles
                       .Select(article => new ArticleDetailsResponse(
                           article.Account.Id,
                           article.LookupId,
                           article.Quantity,
                           article.Fee,
                           article.Amount,
                           article.Currency,
                           article.Type,
                           article.LookupId.HasValue ? properties[article.Id] : null))
                       .ToList());
        }

        //TODO: try using generic to fetch detailed account information
        private async Task<Dictionary<Guid, Dictionary<string, string>>> GetLookupPropertiesAsync(IList<ACC_Article> articles)
        {
            var accountIds = articles.Select(article => article.Account.Id);
            var accounts = await _accountRepository.GetListByIdsAsync(accountIds);

            var result = new Dictionary<Guid, Dictionary<string, string>>();

            var articlesInfo = articles.Where(article => article.LookupId.HasValue)
                .Select(article =>
                    new {
                        AccountId = article.Account.Id,
                        ArticleId = article.Id,
                        LookupId = article.LookupId.Value
                    });

            foreach (var articleInfo in articlesInfo)
            {
                var properties = new Dictionary<string, string>();
                result.Add(articleInfo.ArticleId, properties);
                var account = accounts.First(account => account.Id == articleInfo.AccountId);

                switch (account.LookupType)
                {
                    case LookupType.Person:
                        {
                            var personAccount = await _personAccountRepository.GetAsync(articleInfo.LookupId);
                            properties = EntityHelper.GetProperties(personAccount);
                            break;
                        }

                    case LookupType.Bank:
                        {
                            var bankAccount = await _bankAccountRepository.GetAsync(articleInfo.LookupId);
                            properties = EntityHelper.GetProperties(bankAccount);
                            break;
                        }

                    case LookupType.Product:
                        {
                            var product = await _productRepository.GetAsync(articleInfo.LookupId);
                            properties = EntityHelper.GetProperties(product);
                            break;
                        }

                    default:
                        continue;
                }

                properties.Add(nameof(LookupType).ToCamelCase(), account.LookupType.Value.ToString("D"));
                result[articleInfo.ArticleId] = properties;
            }

            return result;
        }

        public async Task<IList<NetProfitAndLoss>> CalculateNetProfitAndLossAsync(DateTime from, DateTime to)
        {
            var fiscalPeriodId = await _accountingIdentityService.GetFiscalPeriodIdAsync();
            var expenseAccountTask = _accountRepository.GetByNameAndFiscalPeriodIdAsync(fiscalPeriodId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Expense)));
            var sellAccountTask = _accountRepository.GetByNameAndFiscalPeriodIdAsync(fiscalPeriodId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ProductSell)));
            var costOfproductAccountTask = _accountRepository.GetByNameAndFiscalPeriodIdAsync(fiscalPeriodId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_GeneralCostOfProductSold)));
            await Task.WhenAll(expenseAccountTask, sellAccountTask, costOfproductAccountTask);

            var expenseAccount = expenseAccountTask.Result;
            var sellAccount = sellAccountTask.Result;
            var costOfproductAccount = costOfproductAccountTask.Result;

            var totalExpenseTask = _voucherRepository.GetAggregateArticlesByParentAccountIdInDurationAsync(from, to, fiscalPeriodId, expenseAccount.Id, ArticleType.Debit);
            var totalSellTask = _voucherRepository.GetAggregateArticlesByParentAccountIdInDurationAsync(from, to, fiscalPeriodId, sellAccount.Id, ArticleType.Debit);
            var totalcostOfproductTask = _voucherRepository.GetAggregateArticlesByParentAccountIdInDurationAsync(from, to, fiscalPeriodId, costOfproductAccount.Id, ArticleType.Debit);
            await Task.WhenAll(totalExpenseTask, totalSellTask, totalcostOfproductTask);

            var totalExpense = totalExpenseTask.Result;
            var totalSell = totalSellTask.Result;
            var totalcostOfproduct = totalcostOfproductTask.Result;

            return [new NetProfitAndLoss(expenseAccount.Id, expenseAccount.Name, expenseAccount.DisplayName, totalExpense),
                    new NetProfitAndLoss(sellAccount.Id, sellAccount.Name, sellAccount.DisplayName, totalSell),
                    new NetProfitAndLoss(costOfproductAccount.Id, costOfproductAccount.Name, costOfproductAccount.DisplayName, totalcostOfproduct)];
        }
        public async Task<IList<BalanceSheetResponse>> GetBalanceSheetAsync(DateTime to)
        {
            var fiscalPeriodId = await _accountingIdentityService.GetFiscalPeriodIdAsync();
            var accounts = await _accountRepository.GetByNamesAndFiscalPeriodIdAsync(fiscalPeriodId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Assets)),
                                                                                                     AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Liabilities)),
                                                                                                     AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Withdraw)),
                                                                                                     AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_GeneralEquity)),
                                                                                                     AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_SubsidiaryEquity)));

            var assetAccount = accounts.SingleOrDefault(accounts => accounts.Name == AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Assets)));
            var liabilityAccount = accounts.SingleOrDefault(accounts => accounts.Name == AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Liabilities)));
            var generalEquityAccount = accounts.SingleOrDefault(accounts => accounts.Name == AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_GeneralEquity)));
            var subsidiaryEquityAccount = accounts.SingleOrDefault(accounts => accounts.Name == AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_SubsidiaryEquity)));
            var withdrawAccount = accounts.SingleOrDefault(accounts => accounts.Name == AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Withdraw)));

            var assetArticlesAggregated = await _voucherRepository.GetBalanceSheetResponseByDateAsync(fiscalPeriodId, to, [assetAccount.Id], null);
            var liabilityArticlesAggregated = await _voucherRepository.GetBalanceSheetResponseByDateAsync(fiscalPeriodId, to, [liabilityAccount.Id], null);
            var equityArticlesAggregated = await _voucherRepository.GetBalanceSheetResponseByDateAsync(fiscalPeriodId, to, [generalEquityAccount.Id], null);

            var subsidiaryEquityAmount = equityArticlesAggregated.FirstOrDefault(equity => equity.AccountId == subsidiaryEquityAccount.Id);
            var withdrawAmount = equityArticlesAggregated.FirstOrDefault(withdraw => withdraw.AccountId == withdrawAccount.Id);

            return [new BalanceSheetResponse(assetAccount.Name,
                                             assetAccount.DisplayName,
                                             assetArticlesAggregated.Count!=0? assetArticlesAggregated.Sum(asset=>asset.Amount):0,
                                             assetArticlesAggregated?.Select(asset => new BalanceSheetDetaileResponse(asset.AccountId,
                                                                                                                      asset.AccountName,
                                                                                                                      asset.AccountDisplayName,
                                                                                                                      asset.Amount,
                                                                                                                      asset.Nature))
                                                                    .ToList()),
                    new BalanceSheetResponse(liabilityAccount.Name,
                                             liabilityAccount.DisplayName,
                                             liabilityArticlesAggregated.Count !=0? liabilityArticlesAggregated.Sum(liability=>liability.Amount):0,
                                             liabilityArticlesAggregated?.Select(liability => new BalanceSheetDetaileResponse(liability.AccountId,
                                                                                                                              liability.AccountName,
                                                                                                                              liability.AccountDisplayName,
                                                                                                                              liability.Amount,
                                                                                                                              liability.Nature))
                                                                    .ToList()),
                    new BalanceSheetResponse(generalEquityAccount.Name,
                                             generalEquityAccount.DisplayName,
                                             ((subsidiaryEquityAmount == null? 0 : subsidiaryEquityAmount.Amount) - (withdrawAmount == null? 0 :withdrawAmount.Amount)),
                                             equityArticlesAggregated?.Select(equity => new BalanceSheetDetaileResponse(equity.AccountId,
                                                                                                                        equity.AccountName,
                                                                                                                        equity.AccountDisplayName,
                                                                                                                        equity.Amount,
                                                                                                                        equity.Nature))
                                                                    .ToList()),
            ];
        }
    }
}