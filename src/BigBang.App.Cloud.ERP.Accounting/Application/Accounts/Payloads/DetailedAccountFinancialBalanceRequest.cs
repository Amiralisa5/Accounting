using System;
using BigBang.App.Cloud.ERP.Accounting.Common.Validators;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Accounts.Payloads
{
    public record DetailedAccountFinancialBalanceRequest(Guid Id, DateTime FromDate, DateTime ToDate, int PageSize, int PageNumber, string SortBy, SortDirection SortDirection) : IRequest;
}
