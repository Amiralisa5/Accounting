using System;
using System.Collections.Generic;
using System.Linq;
using BigBang.App.Cloud.ERP.Accounting.Application.Accounts.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Common;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using BigBang.Metadata.Models;

namespace BigBang.App.Cloud.ERP.Accounting.Domain.Accounts
{
    public static class AccountTreeFactory
    {
        public static IEnumerable<ACC_Account> CreateDefault(ACC_FiscalPeriod fiscalPeriod)
        {
            var accounts = new List<ACC_Account>();

            var root = CreateRoot(fiscalPeriod);
            accounts.Add(root);

            var asset = root.AddChild(
                GetAccountName(nameof(Messages.AccountTree_Assets)),
                Messages.AccountTree_Assets,
                AccountNature.Debtor,
                true,
                true,
                null
            );
            accounts.Add(asset);

            accounts.Add(asset.AddChild(
                GetAccountName(nameof(Messages.AccountTree_Fund)),
                Messages.AccountTree_Fund,
                AccountNature.Debtor,
                true,
                true,
                null
            ));

            accounts.Add(asset.AddChild(
                GetAccountName(nameof(Messages.AccountTree_Bank)),
                Messages.AccountTree_Bank,
                AccountNature.Debtor,
                true,
                true,
                LookupType.Bank
            ));

            var receivableFromOthers = asset.AddChild(
                GetAccountName(nameof(Messages.AccountTree_ReceivableFromOthers)),
                Messages.AccountTree_ReceivableFromOthers,
                AccountNature.Debtor,
                true,
                true,
                LookupType.Person
            );

            AddPersonRoles(receivableFromOthers, PersonRoleType.Customer);
            accounts.Add(receivableFromOthers);

            accounts.Add(asset.AddChild(
                GetAccountName(nameof(Messages.AccountTree_ProductInventory)),
                Messages.AccountTree_ProductInventory,
                AccountNature.Debtor,
                true,
                false,
                LookupType.Product
            ));

            var advancePayment = asset.AddChild(
                GetAccountName(nameof(Messages.AccountTree_AdvancePayment)),
                Messages.AccountTree_AdvancePayment,
                AccountNature.Debtor,
                true,
                true,
                LookupType.Person
            );

            AddPersonRoles(advancePayment, PersonRoleType.Supplier);
            accounts.Add(advancePayment);

            var liabilities = root.AddChild(
                GetAccountName(nameof(Messages.AccountTree_Liabilities)),
                Messages.AccountTree_Liabilities,
                AccountNature.Creditor,
                true,
                true,
                null
            );
            accounts.Add(liabilities);

            var liabilitiesToOthers = liabilities.AddChild(
                GetAccountName(nameof(Messages.AccountTree_LiabilitiesToOthers)),
                Messages.AccountTree_LiabilitiesToOthers,
                AccountNature.Creditor,
                true,
                true,
                LookupType.Person
            );

            AddPersonRoles(liabilitiesToOthers, PersonRoleType.Supplier);
            accounts.Add(liabilitiesToOthers);

            var advanceReceipt = liabilities.AddChild(
                GetAccountName(nameof(Messages.AccountTree_AdvanceReceipt)),
                Messages.AccountTree_AdvanceReceipt,
                AccountNature.Creditor,
                true,
                true,
                LookupType.Person
            );

            AddPersonRoles(advanceReceipt, PersonRoleType.Customer);
            accounts.Add(advanceReceipt);

            var generalEquity = root.AddChild(
                GetAccountName(nameof(Messages.AccountTree_GeneralEquity)),
                Messages.AccountTree_GeneralEquity,
                AccountNature.Creditor,
                true,
                true,
                null
            );
            accounts.Add(generalEquity);

            accounts.Add(generalEquity.AddChild(
                GetAccountName(nameof(Messages.AccountTree_SubsidiaryEquity)),
                Messages.AccountTree_SubsidiaryEquity,
                AccountNature.Creditor,
                true,
                true,
                LookupType.Person
            ));

            accounts.Add(generalEquity.AddChild(
                GetAccountName(nameof(Messages.AccountTree_Withdraw)),
                Messages.AccountTree_Withdraw,
                AccountNature.Creditor,
                true,
                true,
                LookupType.Person
            ));

            var revenue = root.AddChild(
                GetAccountName(nameof(Messages.AccountTree_Revenue)),
                Messages.AccountTree_Revenue,
                AccountNature.Debtor,
                false,
                true,
                null
            );
            accounts.Add(revenue);

            accounts.Add(revenue.AddChild(
                GetAccountName(nameof(Messages.AccountTree_ServicesProvided)),
                Messages.AccountTree_ServicesProvided,
                AccountNature.Creditor,
                false,
                true,
                null
            ));

            accounts.Add(revenue.AddChild(
                GetAccountName(nameof(Messages.AccountTree_ProductSell)),
                Messages.AccountTree_ProductSell,
                AccountNature.Creditor,
                false,
                false,
                LookupType.Product
            ));

            var generalCostOfProductSold = root.AddChild(
                GetAccountName(nameof(Messages.AccountTree_GeneralCostOfProductSold)),
                Messages.AccountTree_GeneralCostOfProductSold,
                AccountNature.Creditor,
                false,
                false,
                null
            );
            accounts.Add(generalCostOfProductSold);

            accounts.Add(generalCostOfProductSold.AddChild(
                GetAccountName(nameof(Messages.AccountTree_SubsidiaryCostOfProductSold)),
                Messages.AccountTree_SubsidiaryCostOfProductSold,
                AccountNature.Creditor,
                false,
                true,
                LookupType.Product
            ));

            var expense = root.AddChild(
                GetAccountName(nameof(Messages.AccountTree_Expense)),
                Messages.AccountTree_Expense,
                AccountNature.Creditor,
                false,
                true,
                null
            );
            accounts.Add(expense);

            accounts.Add(expense.AddChild(
                GetAccountName(nameof(Messages.AccountTree_Transport)),
                Messages.AccountTree_Transport,
                AccountNature.Creditor,
                false,
                true,
                null
            ));

            accounts.Add(expense.AddChild(
                GetAccountName(nameof(Messages.AccountTree_Salary)),
                Messages.AccountTree_Salary,
                AccountNature.Creditor,
                false,
                true,
                null
            ));

            accounts.Add(expense.AddChild(
                GetAccountName(nameof(Messages.AccountTree_Food)),
                Messages.AccountTree_Food,
                AccountNature.Creditor,
                false,
                true,
                null
            ));

            accounts.Add(expense.AddChild(
                GetAccountName(nameof(Messages.AccountTree_Rent)),
                Messages.AccountTree_Rent,
                AccountNature.Creditor,
                false,
                true,
                null
            ));

            return accounts;
        }

        public static AccountTreeResponse Create(this IList<ACC_Account> accounts)
        {
            var rootAccount = accounts.FirstOrDefault(
                account => account.Name == GetAccountName(nameof(Messages.AccountTree_Root))
            );

            return rootAccount == null ? null : ConvertToAccountTree(rootAccount, accounts);
        }

        private static AccountTreeResponse ConvertToAccountTree(ACC_Account parentAccount, IList<ACC_Account> accounts)
        {
            var childAccounts = accounts
                .Where(account => account.ParentAccount != null && account.ParentAccount.Id == parentAccount.Id);

            return new AccountTreeResponse(
               parentAccount.Id,
               parentAccount.DisplayName,
               parentAccount.Level,
               parentAccount.Code,
               parentAccount.Nature,
               parentAccount.Name,
               parentAccount.LookupType,
               parentAccount.IsPermanent,
               parentAccount.PersonRoles.Any() ? parentAccount.PersonRoles.FirstOrDefault().PersonRoleTypeId : null,
               childAccounts.Select(child => ConvertToAccountTree(child, accounts)));
        }

        public static string GetAccountName(string accountTreeNodeName)
        {
            return accountTreeNodeName.Length < Constants.AccountTreePrefix.Length
                ? accountTreeNodeName
                : accountTreeNodeName.Substring(Constants.AccountTreePrefix.Length);
        }

        private static ACC_Account AddChild(
            this ACC_Account parent,
            string name,
            string displayName,
            AccountNature? nature,
            bool? isPermanent,
            bool isCustom,
            LookupType? lookupType
        )
        {
            var child = new ACC_Account
            {
                Id = Guid.NewGuid(),
                Name = name,
                DisplayName = displayName,
                Code = CalculateCode(parent),
                Level = Convert.ToByte(parent.Level + 1),
                Nature = nature,
                IsPermanent = isPermanent,
                IsCustom = isCustom,
                LookupType = lookupType,
                FiscalPeriod = parent.FiscalPeriod,
                ParentAccount = parent,
                EntityState = EntityState.Added
            };

            parent.Accounts.Add(child);
            return child;
        }

        private static string CalculateCode(ACC_Account parent)
        {
            var parentCode = string.IsNullOrEmpty(parent.Code) ? string.Empty : parent.Code;
            return $"{parentCode}{parent.Accounts.Count + 1}";
        }

        private static void AddPersonRoles(ACC_Account account, params PersonRoleType[] roles)
        {
            foreach (var role in roles)
            {
                var personRole = new ACC_PersonRole
                {
                    Id = Guid.NewGuid(),
                    PersonRoleTypeId = role,
                    Account = account,
                    EntityState = EntityState.Added
                };
                account.PersonRoles.Add(personRole);
            }
        }

        private static ACC_Account CreateRoot(ACC_FiscalPeriod fiscalPeriod)
        {
            return new ACC_Account
            {
                Id = Guid.NewGuid(),
                Name = GetAccountName(nameof(Messages.AccountTree_Root)),
                DisplayName = Messages.AccountTree_Root,
                Level = 0,
                FiscalPeriod = fiscalPeriod,
                EntityState = EntityState.Added
            };
        }
    }
}