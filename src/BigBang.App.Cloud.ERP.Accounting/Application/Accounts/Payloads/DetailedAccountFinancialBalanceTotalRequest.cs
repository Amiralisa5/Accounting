using System;
using BigBang.App.Cloud.ERP.Accounting.Common.Validators;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Accounts.Payloads
{
    public record DetailedAccountFinancialBalanceTotalRequest(Guid Id, DateTime FromDate, DateTime ToDate) : IRequest;
}
