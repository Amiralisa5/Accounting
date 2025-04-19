using System;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Accounts.Payloads
{
    public record DetailedAccountFinancialBalanceResponse(
        Guid Id,
        string Code,
        string Name,
        string DisplayName,
        AccountNature Nature);
}
