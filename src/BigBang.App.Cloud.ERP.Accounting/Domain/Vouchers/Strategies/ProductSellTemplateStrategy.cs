using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Common.Helpers;
using BigBang.App.Cloud.ERP.Accounting.Domain.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.App.Cloud.ERP.Accounting.Domain.Products;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.BankAccounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Products;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.VoucherTemplates;
using BigBang.App.Cloud.ERP.Accounting.Resources;

namespace BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers.Strategies
{
    internal class ProductSellTemplateStrategy : BaseTemplateStrategy
    {
        private readonly List<ACC_Product> _products;
        private readonly IProductRepository _productRepository;

        public ProductSellTemplateStrategy(
            IVoucherRepository voucherRepository,
            IAccountRepository accountRepository,
            IVoucherTemplateRepository voucherTemplateRepository, IProductRepository productRepository, IBankAccountRepository bankAccountRepository) :
            base(voucherRepository, accountRepository, voucherTemplateRepository, bankAccountRepository)
        {
            _productRepository = productRepository;
            _products = [];
        }

        protected override async Task ProcessDebitArticleAsync(ACC_Account account, ArticleRequest article)
        {
            if (account.LookupType == LookupType.Bank)
            {
                Validate(account, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Bank)),
                    LookupType.Bank);

                await DepositToBankAsync(article.LookupId.GetValueOrDefault(), article.Amount);
            }
        }

        protected override async Task ProcessCreditArticleAsync(ACC_Account account, ArticleRequest article)
        {
            if (account.LookupType == LookupType.Product)
            {
                var productId = article.LookupId.GetValueOrDefault();
                var product = _products.SingleOrDefault(product => product.Id == productId);
                if (product == null)
                {
                    product = await _productRepository.GetAsync(productId);

                    Validate(account, account.Name, LookupType.Product, Messages.Entity_Product, product);

                    _products.Add(product);
                }

                if (account.Name == AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ProductSell)))
                {
                    product.UpdateSuggestedSellPrice(article);
                }
                else if (account.Name == AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ProductInventory)))
                {
                    product.DecreaseStock(article);
                }
            }
        }

        protected override Task<List<string>> GetReplaceStringsAsync()
        {
            return _products.Count == 1 ? Task.FromResult<List<string>>([_products.First().Name])
                : Task.FromResult<List<string>>([_products.First().Name + " و ..."]);
        }

        protected override async Task PostVoucherRegistrationAsync()
        {
            if (Accounts.Any(account => account.LookupType == LookupType.Bank))
            {
                await BankAccountRepository.UpdateAsync(BankAccount);
            }

            if (Accounts.Any(account => account.LookupType == LookupType.Product))
            {
                foreach (var product in _products)
                {
                    await _productRepository.UpdateAsync(product);
                }
            }
        }

        protected override void PreProcessValidation(IList<ACC_Account> accounts, RegisterVoucherRequest request)
        {
            var accountProductInventory = accounts
                .SingleOrDefault(account => account.Name == AccountTreeFactory
                .GetAccountName(nameof(Messages.AccountTree_ProductInventory)));

            var accountSubsidiaryCost = accounts
                .SingleOrDefault(account => account.Name == AccountTreeFactory
                .GetAccountName(nameof(Messages.AccountTree_SubsidiaryCostOfProductSold)));

            var accountProductSell = accounts
                .SingleOrDefault(account => account.Name == AccountTreeFactory
                .GetAccountName(nameof(Messages.AccountTree_ProductSell)));

            var accountReceivables = accounts
                .SingleOrDefault(account => account.Name == AccountTreeFactory
                .GetAccountName(nameof(Messages.AccountTree_ReceivableFromOthers)));

            var accountBank = accounts
                .SingleOrDefault(account => account.Name == AccountTreeFactory
                .GetAccountName(nameof(Messages.AccountTree_Bank)));

            var accountTransportation = accounts
                .SingleOrDefault(account => account.Name == AccountTreeFactory
                .GetAccountName(nameof(Messages.AccountTree_Transport)));

            var productInventoryArticles = request.Articles.Where(article => article.AccountId == accountProductInventory?.Id).ToList();
            var subsidiaryCostOfProductSoldArticles = request.Articles.Where(article => article.AccountId == accountSubsidiaryCost?.Id).ToList();
            var productSellArticles = request.Articles.Where(article => article.AccountId == accountProductSell?.Id).ToList();

            var productArticles = productInventoryArticles
                .Union(subsidiaryCostOfProductSoldArticles)
                .Union(productSellArticles);

            productArticles.ValidateProductArticles();

            var receivableArticles = request.Articles.Where(article => article.AccountId == accountReceivables?.Id).ToList();

            var articleTransportation = accountTransportation == null ? null :
                request.Articles.FirstOrDefault(a => a.AccountId == accountTransportation.Id);

            var bankArticle = accountBank == null ? null :
                request.Articles.FirstOrDefault(article => article.AccountId == accountBank.Id);

            CheckTotalOfProductInventoryEqualsToSubsidiaryCost(productInventoryArticles, subsidiaryCostOfProductSoldArticles);

            var debitReceivableArticlesTotalAmount = receivableArticles.Where(article => article.Type == ArticleType.Debit).Sum(article => article.Amount);
            var creditReceivableArticlesTotalAmount = receivableArticles.Where(article => article.Type == ArticleType.Credit).Sum(article => article.Amount);
            var productSellArticlesTotalAmount = productSellArticles.Sum(article => article.Amount);

            switch (receivableArticles.Count)
            {
                case 2:
                    if (productSellArticlesTotalAmount != debitReceivableArticlesTotalAmount)
                    {
                        throw ExceptionHelper.BadRequest(Messages.Error_ReceivableFromOthersShouldEqualToTotalProductsAmount);
                    }

                    if (debitReceivableArticlesTotalAmount != creditReceivableArticlesTotalAmount)
                    {
                        throw ExceptionHelper.BadRequest(Messages.Error_ReceivableFromOthersDebitShouldBeEqualToReceivableFromOthersCredit);
                    }

                    CheckDepositToBankEqualsToReceivableFromOthersCredit(creditReceivableArticlesTotalAmount, bankArticle);
                    break;

                case 1:
                    if (productSellArticlesTotalAmount != debitReceivableArticlesTotalAmount)
                    {
                        throw ExceptionHelper.BadRequest(Messages.Error_ReceivableFromOthersShouldEqualToTotalProductsAmount);
                    }

                    break;

                default:
                    throw ExceptionHelper.BadRequest(Messages.Error_ArticlesAreNotValid);
            }
        }

        private static void CheckTotalOfProductInventoryEqualsToSubsidiaryCost(IEnumerable<ArticleRequest> productInventoryArticles, IEnumerable<ArticleRequest> subsidiaryCostArticles)
        {
            var totalInventoryAmount = productInventoryArticles.Sum(article => article.Amount);
            var totalSubsidiaryCost = subsidiaryCostArticles.Sum(article => article.Amount);
            if (totalInventoryAmount != totalSubsidiaryCost)
            {
                throw ExceptionHelper.BadRequest(Messages.Error_SubsidiaryCostOfProductSoldShouldEqualToTotalProductAmount);
            }
        }

        private static void CheckDepositToBankEqualsToReceivableFromOthersCredit(long creditReceivableArticlesTotalAmount, ArticleRequest bankArticle)
        {
            if (bankArticle == null)
            {
                throw ExceptionHelper.BadRequest(Messages.Error_BankAccountNotExist);
            }

            if (bankArticle.Amount != creditReceivableArticlesTotalAmount)
            {
                throw ExceptionHelper.BadRequest(Messages.Error_ReceivableFromOthersShouldEqualToDepositToBank);
            }
        }
    }
}