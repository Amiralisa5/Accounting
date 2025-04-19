using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Common.Helpers;
using BigBang.App.Cloud.ERP.Accounting.Domain.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.BankAccounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.PersonAccounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.VoucherTemplates;
using BigBang.App.Cloud.ERP.Accounting.Resources;

namespace BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers.Strategies
{
    internal class AdvancePaymentTemplateStrategy : BaseTemplateStrategy
    {
        private readonly IPersonAccountRepository _personAccountRepository;
        private ACC_PersonAccount _personAccount;

        public AdvancePaymentTemplateStrategy(
            IVoucherRepository voucherRepository,
            IAccountRepository accountRepository,
            IBankAccountRepository bankAccountRepository,
            IPersonAccountRepository personAccountRepository,
            IVoucherTemplateRepository voucherTemplateRepository) :
            base(voucherRepository, accountRepository, voucherTemplateRepository, bankAccountRepository)
        {
            _personAccountRepository = personAccountRepository;
        }

        protected override Task<List<string>> GetReplaceStringsAsync()
        {
            return Task.FromResult<List<string>>([_personAccount.FirstName, _personAccount.LastName]);
        }

        protected override async Task PostVoucherRegistrationAsync()
        {
            if (Accounts.Any(account => account.LookupType == LookupType.Bank))
            {
                await BankAccountRepository.UpdateAsync(BankAccount);
            }
        }

        protected override void PreProcessValidation(IList<ACC_Account> accounts, RegisterVoucherRequest request)
        {
            var accountBank = accounts
                .SingleOrDefault(account => account.Name == AccountTreeFactory
                    .GetAccountName(nameof(Messages.AccountTree_Bank)));

            var accountAdvancePayment = accounts
                .SingleOrDefault(account => account.Name == AccountTreeFactory
                    .GetAccountName(nameof(Messages.AccountTree_AdvancePayment)));

            var advancePaymentArticles = request.Articles
                .Where(article => article.AccountId == accountAdvancePayment?.Id)
                .ToList();

            var bankArticle = request.Articles
                .FirstOrDefault(article => article.AccountId == accountBank?.Id && article.Type == ArticleType.Credit);

            var advancePaymentTotalAmount = advancePaymentArticles
                .Where(article => article.Type == ArticleType.Debit)
                .Sum(article => article.Amount);

            CheckWithdrawFromBankEqualsToAdvancePayment(advancePaymentTotalAmount, bankArticle);
        }

        private static void CheckWithdrawFromBankEqualsToAdvancePayment(long advancePaymentTotalAmount, ArticleRequest bankArticle)
        {
            if (bankArticle == null)
            {
                throw ExceptionHelper.BadRequest(Messages.Error_BankAccountNotExist);
            }

            if (bankArticle.Amount != advancePaymentTotalAmount)
            {
                throw ExceptionHelper.BadRequest(Messages.Error_WhitdrawFromBankShouldBeEqualToAdvancePayment);
            }
        }

        protected override async Task ProcessCreditArticleAsync(ACC_Account account, ArticleRequest article)
        {
            Validate(account, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Bank)), LookupType.Bank);

            await WithdrawFromBankAsync(article.LookupId.GetValueOrDefault(), article.Amount);
        }

        protected override async Task ProcessDebitArticleAsync(ACC_Account account, ArticleRequest article)
        {
            _personAccount = await _personAccountRepository.GetAsync(article.LookupId.GetValueOrDefault());

            Validate(account, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_AdvancePayment)),
                LookupType.Person, Messages.Entity_PersonAccount, _personAccount);
        }
    }
}