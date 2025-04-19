using System;
using System.Collections.Generic;
using BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads
{
    public record PaginatedVouchersListResponse(
        int PageSize,
        int PageNumber,
        int TotalCount,
        IList<VoucherResponse> Items);

    public record VoucherResponse(
        Guid Id,
        VoucherTemplate Template,
        string Title,
        string Number,
        DateTime EffectiveDate,
        long Amount);
}
