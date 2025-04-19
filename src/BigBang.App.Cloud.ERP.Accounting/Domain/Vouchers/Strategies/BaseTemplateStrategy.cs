using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Common.Helpers;
using BigBang.App.Cloud.ERP.Accounting.Domain.BankAccounts;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.BankAccounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.VoucherTemplates;
using BigBang.App.Cloud.ERP.Accounting.Resources;

namespace BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers.Strategies
{
    internal abstract class BaseTemplateStrategy : ITemplateStrategy
    {
        protected ACC_BankAccount BankAccount;
        protected IList<ACC_Account> Accounts;
        protected readonly IVoucherRepository VoucherRepository;
        protected readonly IAccountRepository AccountRepository;
        protected readonly IBankAccountRepository BankAccountRepository;
        protected readonly IVoucherTemplateRepository VoucherTemplateRepository;

        protected BaseTemplateStrategy(
            IVoucherRepository voucherRepository,
            IAccountRepository accountRepository,
            IVoucherTemplateRepository voucherTemplateRepository, IBankAccountRepository bankAccountRepository)
        {
            VoucherRepository = voucherRepository;
            AccountRepository = accountRepository;
            VoucherTemplateRepository = voucherTemplateRepository;
            BankAccountRepository = bankAccountRepository;
            Accounts = [];
        }

        public async Task<ACC_Voucher> RegisterAsync(RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod)
        {
            var accountIds = request.Articles.Select(article => article.AccountId)
                .Distinct()
                .ToList();

            Accounts = await AccountRepository.GetListByIdsAsync(accountIds);

            if (accountIds.Count != Accounts.Count) throw ExceptionHelper.NotFound(Messages.Entity_Account);

            PreProcessValidation(Accounts, request);

            foreach (var article in request.Articles)
            {
                var account = Accounts.First(account => account.Id == article.AccountId);

                if (article.Type == ArticleType.Debit)
                {
                    await ProcessDebitArticleAsync(account, article);
                }
                else
                {
                    await ProcessCreditArticleAsync(account, article);
                }
            }

            var voucherTemplate = await VoucherTemplateRepository.GetAsync((byte)request.Template);

            var lastVoucher = await VoucherRepository.GetLastAsync(fiscalPeriod.Id);

            var replaceStrings = await GetReplaceStringsAsync();

            var voucher = VoucherFactory.Create(request,
                fiscalPeriod, voucherTemplate, lastVoucher?.Number,
                replaceStrings);

            await VoucherRepository.AddAsync(voucher);

            await PostVoucherRegistrationAsync();

            return voucher;
        }

        protected abstract Task ProcessDebitArticleAsync(ACC_Account account, ArticleRequest article);

        protected abstract Task ProcessCreditArticleAsync(ACC_Account account, ArticleRequest article);

        protected abstract Task<List<string>> GetReplaceStringsAsync();

        protected abstract Task PostVoucherRegistrationAsync();

        protected void Validate<TEntity>(ACC_Account account,
            string expectedAccountName,
            LookupType expectedLookupType,
            string lookupErrorMessage,
            TEntity entity) where TEntity : class
        {

            if (account.Name != expectedAccountName || account.LookupType != expectedLookupType)
            {
                throw ExceptionHelper.BadRequest(Messages.Error_AccountForTemplateIsNotValid);
            }

            if (entity is null)
            {
                throw ExceptionHelper.BadRequest(string.Format(Messages.Error_FieldRequired, lookupErrorMessage));
            }
        }

        protected void Validate(ACC_Account account, string expectedAccountName, LookupType expectedLookupType)
        {
            if (account.Name != expectedAccountName || account.LookupType != expectedLookupType)
            {
                throw ExceptionHelper.BadRequest(Messages.Error_AccountForTemplateIsNotValid);
            }
        }

        protected async Task WithdrawFromBankAsync(Guid bankAccountId, long amount)
        {
            BankAccount = await BankAccountRepository.GetAsync(bankAccountId);

            if (BankAccount is null)
            {
                throw ExceptionHelper.BadRequest(string.Format(Messages.Error_FieldRequired, Messages.Entity_BankAccount));
            }

            BankAccount = BankAccount.Withdraw(amount);
        }

        protected async Task DepositToBankAsync(Guid bankAccountId, long amount)
        {
            BankAccount = await BankAccountRepository.GetAsync(bankAccountId);

            if (BankAccount is null)
            {
                throw ExceptionHelper.BadRequest(string.Format(Messages.Error_FieldRequired, Messages.Entity_BankAccount));
            }

            BankAccount = BankAccount.Deposit(amount);
        }

        protected virtual void PreProcessValidation(IList<ACC_Account> accounts, RegisterVoucherRequest request)
        {

        }
    }
}