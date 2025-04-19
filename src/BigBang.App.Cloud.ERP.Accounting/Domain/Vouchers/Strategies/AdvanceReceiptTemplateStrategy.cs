using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
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
    internal class AdvanceReceiptTemplateStrategy : BaseTemplateStrategy
    {
        private readonly IPersonAccountRepository _personAccountRepository;
        private ACC_PersonAccount _personAccount;

        public AdvanceReceiptTemplateStrategy(
            IVoucherRepository voucherRepository,
            IAccountRepository accountRepository,
            IBankAccountRepository bankAccountRepository,
            IPersonAccountRepository personAccountRepository,
            IVoucherTemplateRepository voucherTemplateRepository)
            : base(voucherRepository, accountRepository, voucherTemplateRepository, bankAccountRepository)
        {
            _personAccountRepository = personAccountRepository;
        }

        protected override async Task ProcessDebitArticleAsync(ACC_Account account, ArticleRequest article)
        {
            Validate(account, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Bank)), LookupType.Bank);
            await DepositToBankAsync(article.LookupId.GetValueOrDefault(), article.Amount);
        }

        protected override async Task ProcessCreditArticleAsync(ACC_Account account, ArticleRequest article)
        {
            _personAccount = await _personAccountRepository.GetAsync(article.LookupId.GetValueOrDefault());

            Validate(account, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_AdvanceReceipt)),
                LookupType.Person, Messages.Entity_PersonAccount, _personAccount);
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
    }
}
