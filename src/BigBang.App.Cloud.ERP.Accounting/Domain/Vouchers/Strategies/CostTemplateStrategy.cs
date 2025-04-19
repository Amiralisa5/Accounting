using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Common.Helpers;
using BigBang.App.Cloud.ERP.Accounting.Domain.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.BankAccounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.VoucherTemplates;
using BigBang.App.Cloud.ERP.Accounting.Resources;

namespace BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers.Strategies
{
    internal class CostTemplateStrategy : BaseTemplateStrategy
    {
        private ACC_Account _costAccount;

        public CostTemplateStrategy(IVoucherRepository voucherRepository,
            IAccountRepository accountRepository,
            IBankAccountRepository bankAccountRepository,
            IVoucherTemplateRepository voucherTemplateRepository) :
            base(voucherRepository, accountRepository, voucherTemplateRepository, bankAccountRepository)
        {

        }

        protected override async Task ProcessDebitArticleAsync(ACC_Account account, ArticleRequest article)
        {
            var expenseAccounts = await AccountRepository.GetListByParentNameAsync(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Expense)));

            var expenseAccountsName = expenseAccounts.Select(expenseAccount => expenseAccount.Name);

            if (!expenseAccountsName.Contains(account.Name))
            {
                throw ExceptionHelper.BadRequest(Messages.Error_AccountForTemplateIsNotValid);
            }

            _costAccount = account;
        }

        protected override async Task ProcessCreditArticleAsync(ACC_Account account, ArticleRequest article)
        {
            Validate(account, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Bank)), LookupType.Bank);

            await WithdrawFromBankAsync(article.LookupId.GetValueOrDefault(), article.Amount);
        }

        protected override Task<List<string>> GetReplaceStringsAsync()
        {
            return Task.FromResult<List<string>>([_costAccount.DisplayName]);
        }

        protected override async Task PostVoucherRegistrationAsync()
        {
            if (Accounts.Any(account => account.LookupType == LookupType.Bank))
            {
                await BankAccountRepository.UpdateAsync(BankAccount);
            }
        }
    }
}
