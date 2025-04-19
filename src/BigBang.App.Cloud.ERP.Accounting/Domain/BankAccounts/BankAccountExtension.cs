using BigBang.App.Cloud.ERP.Accounting.Common.Helpers;
using BigBang.App.Cloud.ERP.Accounting.Resources;

namespace BigBang.App.Cloud.ERP.Accounting.Domain.BankAccounts
{
    internal static class BankAccountExtension
    {
        public static ACC_BankAccount Withdraw(this ACC_BankAccount bankAccount, long amount)
        {
            if (bankAccount.Balance < amount)
            {
                throw ExceptionHelper.Forbidden(Messages.Error_BankAccountBalanceIsNotEnough);
            }

            bankAccount.Balance -= amount;

            return bankAccount;
        }

        public static ACC_BankAccount Deposit(this ACC_BankAccount bankAccount, long amount)
        {
            bankAccount.Balance += amount;

            return bankAccount;
        }
    }
}
