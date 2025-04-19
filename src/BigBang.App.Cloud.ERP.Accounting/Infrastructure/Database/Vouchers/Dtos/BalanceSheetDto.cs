using System;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers.Dtos
{
    public record BalanceSheetDto(Guid ParentAccountId, string ParentAccountName, string ParentAccountDisplayName, Guid AccountId, string AccountName, string AccountDisplayName, long Amount, AccountNature Nature);
}