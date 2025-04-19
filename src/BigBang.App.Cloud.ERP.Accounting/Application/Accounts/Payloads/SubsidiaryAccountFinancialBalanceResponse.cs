using System;
using System.Collections.Generic;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Accounts.Payloads
{
    public record SubsidiaryAccountFinancialBalanceResponse(
        Guid Id,
        string Code,
        string Name,
        string DisplayName,
        AccountNature Nature,
        IList<SubsidiaryAccountResponse> SubsidiaryAccountResponses);

    public record SubsidiaryAccountResponse(
        Guid Id,
        string Code,
        string Name,
        string DisplayName,
        long TotalDebit,
        long TotalCredit,
        AccountNature Nature);
}
