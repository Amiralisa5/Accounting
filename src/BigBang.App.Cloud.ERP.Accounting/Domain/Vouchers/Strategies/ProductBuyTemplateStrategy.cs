
using System;
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
    internal class ProductBuyTemplateStrategy : BaseTemplateStrategy
    {
        private readonly IProductRepository _productRepository;
        private readonly List<ACC_Product> _products;

        public ProductBuyTemplateStrategy(
            IVoucherRepository voucherRepository,
            IAccountRepository accountRepository,
            IBankAccountRepository bankAccountRepository,
            IProductRepository productRepository,
            IVoucherTemplateRepository voucherTemplateRepository) :
            base(voucherRepository, accountRepository, voucherTemplateRepository, bankAccountRepository)
        {
            _productRepository = productRepository;
            _products = [];
        }

        protected override Task<List<string>> GetReplaceStringsAsync()
        {
            return _products.Count == 1 ? Task.FromResult<List<string>>([_products.First().Name])
                : Task.FromResult<List<string>>([_products.First().Name + " و ..."]);
        }


        protected override async Task ProcessCreditArticleAsync(ACC_Account account, ArticleRequest article)
        {
            if (account.LookupType == LookupType.Bank)
            {
                Validate(account, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Bank)), LookupType.Bank);

                await WithdrawFromBankAsync(article.LookupId.GetValueOrDefault(), article.Amount);
            }
        }

        protected override async Task ProcessDebitArticleAsync(ACC_Account account, ArticleRequest article)
        {
            if (account.LookupType == LookupType.Product)
            {
                var product = await _productRepository.GetAsync(article.LookupId.GetValueOrDefault());

                Validate(account, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ProductInventory)),
                    LookupType.Product, Messages.Entity_Product, product);

                product = product.Buy(article);

                _products.Add(product);
            }

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
            var accountProductInventory = accounts.SingleOrDefault(account => account.Name == AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ProductInventory)));
            var productArticles = request.Articles.Where(article => article.AccountId == accountProductInventory?.Id).ToList();

            productArticles.ValidateProductArticles();

            var accountTransportation = accounts.SingleOrDefault(account => account.Name == AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Transport)));
            var totalProductsPrice = productArticles.Sum(article => article.Quantity.GetValueOrDefault() * article.Fee.GetValueOrDefault());
            var articleTransportation = request.Articles.FirstOrDefault(article => article.AccountId == accountTransportation?.Id);

            var accountLiabilitiesToOthers = accounts.SingleOrDefault(account => account.Name == AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_LiabilitiesToOthers)));
            var articleLiabilitiesToOthersList = request.Articles.Where(article => article.AccountId == accountLiabilitiesToOthers?.Id).ToList();
            var accountBank = accounts.SingleOrDefault(account => account.Name == AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Bank)));
            var articleBank = request.Articles.FirstOrDefault(article => article.AccountId == accountBank?.Id);

            if (articleTransportation != null) CheckCorrectDistributionTransportCalculation(productArticles, articleTransportation);

            switch (articleLiabilitiesToOthersList.Count)
            {
                case 0:
                    CheckBankAccountWithdrawEqualsToTotalProductsAmount(productArticles, articleBank);
                    break;

                case 1 when articleTransportation == null && articleBank == null:
                    CheckLiabilitiesToOthersEqualsToTotalProductsPrice(articleLiabilitiesToOthersList, totalProductsPrice);
                    break;

                case 1 when articleTransportation == null:
                    throw ExceptionHelper.BadRequest(Messages.Error_BankAccountShouldNotInvolved);

                case 1 when true:
                    CheckBankAccountWithdrawEqualsToTransportAmount(articleBank, articleTransportation);
                    CheckLiabilitiesToOthersEqualsToTotalProductsPrice(articleLiabilitiesToOthersList, totalProductsPrice);
                    break;

                case 2:
                    CheckBankAccountWithdrawEqualsToTotalProductsAmount(productArticles, articleBank);
                    CheckLiabilitiesToOthersEqualsToTotalProductsPrice(articleLiabilitiesToOthersList, totalProductsPrice);
                    break;
            }
        }

        private static void CheckBankAccountWithdrawEqualsToTotalProductsAmount(IEnumerable<ArticleRequest> productArticles, ArticleRequest articleBank)
        {
            if (articleBank == null) ExceptionHelper.BadRequest(Messages.Error_BankAccountNotExist);

            if (articleBank?.Amount != productArticles.Sum(article => article.Amount))
                throw ExceptionHelper.BadRequest(Messages.Error_BankAccountWithdrawShouldEqualToTotalProductsAmount);
        }

        private static void CheckLiabilitiesToOthersEqualsToTotalProductsPrice(IEnumerable<ArticleRequest> articleLiabilitiesToOthersList, long totalProductsPrice)
        {
            if (articleLiabilitiesToOthersList.First().Amount != totalProductsPrice)
            {
                throw ExceptionHelper.BadRequest(Messages.Error_LiablitiesToOthersShouldEqualToTotalProductsAmount);
            }
        }

        private static void CheckCorrectDistributionTransportCalculation(IList<ArticleRequest> productArticles, ArticleRequest articleTransportation)
        {
            var totalQuantity = productArticles.Sum(article => article.Quantity.GetValueOrDefault());

            var transportCostPerUnit = (double)articleTransportation.Amount / totalQuantity;

            foreach (var article in productArticles)
            {
                var expectedAmount = (transportCostPerUnit + article.Fee.GetValueOrDefault()) * article.Quantity.GetValueOrDefault();
                if (Math.Abs((int)(expectedAmount - article.Amount)) >= 1)
                {
                    throw ExceptionHelper.BadRequest(Messages.Error_IncorrectDistributionTransportCalculation);
                }
            }
        }

        private static void CheckBankAccountWithdrawEqualsToTransportAmount(ArticleRequest articleBank, ArticleRequest articleTransportation)
        {
            if (articleBank == null) ExceptionHelper.BadRequest(Messages.Error_BankAccountNotExist);

            if (articleBank?.Amount != articleTransportation.Amount) throw ExceptionHelper.BadRequest(Messages.Error_BankAccountWithdrawShouldEqualToTransportAmount);
        }
    }
}
