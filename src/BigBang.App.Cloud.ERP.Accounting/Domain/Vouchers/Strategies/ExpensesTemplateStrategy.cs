using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.Enums;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Domain.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.BankAccounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.VoucherTemplates;
using BigBang.App.Cloud.ERP.Accounting.Resources;

namespace BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers.Strategies
{
    internal class ExpensesTemplateStrategy : BaseTemplateStrategy
    {
        private readonly IEnumService _enumService;
        private string _amountInCurrency;

        public ExpensesTemplateStrategy(
            IVoucherRepository voucherRepository,
            IAccountRepository accountRepository,
            IBankAccountRepository bankAccountRepository,
            IVoucherTemplateRepository voucherTemplateRepository,
            IEnumService enumService) :
            base(voucherRepository, accountRepository, voucherTemplateRepository, bankAccountRepository)
        {
            _enumService = enumService;
        }

        protected override async Task ProcessDebitArticleAsync(ACC_Account account, ArticleRequest article)
        {
            var currencyEnum = await _enumService.GetAsync(nameof(Currency));
            var currencyDisplayName = currencyEnum.EnumMembers.Single(model => model.KeyValue == (long)article.Currency).DisplayName;

            _amountInCurrency = $"{article.Amount:N0} {currencyDisplayName}";
        }

        protected override async Task ProcessCreditArticleAsync(ACC_Account account, ArticleRequest article)
        {
            Validate(account, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Bank)), LookupType.Bank);

            await WithdrawFromBankAsync(article.LookupId.GetValueOrDefault(), article.Amount);
        }

        protected override Task<List<string>> GetReplaceStringsAsync()
        {
            return Task.FromResult<List<string>>([_amountInCurrency]);
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
