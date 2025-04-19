using System;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads
{
    public record NetProfitAndLoss(Guid AccountId, string AccountName, string AccountDisplayName, long TotalAmount);
}