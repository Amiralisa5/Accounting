using System;
using BigBang.App.Cloud.ERP.Accounting.Common.Validators;

namespace BigBang.App.Cloud.ERP.Accounting.Application.BankAccounts.Payloads
{
    public record BankAccountVouchersRequest(DateTime? FromDate, DateTime? ToDate, int PageSize, int PageNumber, Guid Id) : IRequest;
}
