using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Common.Helpers;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.BankAccounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.PersonAccounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Products;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.VoucherTemplates;
using BigBang.App.Cloud.ERP.Accounting.Resources;

namespace BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers.Strategies
{
    internal class CustomTemplateStrategy : BaseTemplateStrategy
    {
        private readonly IPersonAccountRepository _personAccountRepository;
        private readonly IProductRepository _productRepository;

        public CustomTemplateStrategy(
            IVoucherRepository voucherRepository,
            IAccountRepository accountRepository,
            IBankAccountRepository bankAccountRepository,
            IPersonAccountRepository personAccountRepository,
            IVoucherTemplateRepository voucherTemplateRepository, IProductRepository productRepository) :
            base(voucherRepository, accountRepository, voucherTemplateRepository, bankAccountRepository)
        {
            _personAccountRepository = personAccountRepository;
            _productRepository = productRepository;
        }

        protected override async Task ProcessDebitArticleAsync(ACC_Account account, ArticleRequest article)
        {
            await ValidateArticle(account, article);
        }

        protected override async Task ProcessCreditArticleAsync(ACC_Account account, ArticleRequest article)
        {
            await ValidateArticle(account, article);
        }

        protected override Task<List<string>> GetReplaceStringsAsync()
        {
            return Task.FromResult(new List<string>());
        }

        protected override async Task PostVoucherRegistrationAsync()
        {
            if (Accounts.Any(account => account.LookupType == LookupType.Bank))
            {
                await BankAccountRepository.UpdateAsync(BankAccount);
            }
        }

        private async Task ValidateArticle(ACC_Account account, ArticleRequest article)
        {
            switch (account.LookupType)
            {
                case LookupType.Bank:

                    if (article.Type == ArticleType.Debit)
                    {
                        await DepositToBankAsync(article.LookupId.GetValueOrDefault(), article.Amount);
                    }
                    else
                    {
                        await WithdrawFromBankAsync(article.LookupId.GetValueOrDefault(), article.Amount);
                    }

                    break;

                case LookupType.Person:

                    var person = await _personAccountRepository.GetAsync(article.LookupId.GetValueOrDefault());

                    if (person is null)
                    {
                        throw ExceptionHelper.BadRequest(string.Format(Messages.Error_FieldRequired, Messages.Entity_PersonAccount));
                    }

                    break;

                case LookupType.Product:

                    var product = await _productRepository.GetAsync(article.LookupId.GetValueOrDefault());

                    if (product is null)
                    {
                        throw ExceptionHelper.BadRequest(string.Format(Messages.Error_FieldRequired, Messages.Entity_Product));
                    }

                    break;
            }
        }
    }
}
