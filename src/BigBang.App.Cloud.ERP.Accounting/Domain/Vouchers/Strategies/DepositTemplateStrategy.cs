using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.Enums;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Common;
using BigBang.App.Cloud.ERP.Accounting.Domain.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.BankAccounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.VoucherTemplates;
using BigBang.App.Cloud.ERP.Accounting.Resources;

namespace BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers.Strategies
{
    internal class DepositTemplateStrategy : BaseTemplateStrategy
    {
        private readonly IEnumService _enumService;

        public DepositTemplateStrategy(IVoucherRepository voucherRepository,
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
            Validate(account, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Bank)), LookupType.Bank);

            await DepositToBankAsync(article.LookupId.GetValueOrDefault(), article.Amount);
        }

        protected override async Task ProcessCreditArticleAsync(ACC_Account account, ArticleRequest article)
        {
            await Task.CompletedTask;
        }

        protected override async Task<List<string>> GetReplaceStringsAsync()
        {
            var bankEnum = await _enumService.GetAsync(nameof(Bank));
            var banks = bankEnum.EnumMembers;

            var bank = banks.First(bank => bank.KeyName == BankAccount.Bank.ToString());

            var fourFirstDigits = BankAccount.CardNumber.Substring(0, 4);
            var fourLastDigits = BankAccount.CardNumber.Substring(BankAccount.CardNumber.Length - 4, 4);
            var maskedCardNumber = @$"{Constants.LeftToRightMark}{fourFirstDigits}-xxxx-xxxx-{fourLastDigits}{Constants.LeftToRightMark}";

            return [
                bank.DisplayName,
                maskedCardNumber
            ];
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
