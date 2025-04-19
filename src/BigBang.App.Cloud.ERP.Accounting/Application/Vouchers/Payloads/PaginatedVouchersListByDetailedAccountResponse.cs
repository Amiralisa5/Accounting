using System;
using System.Collections.Generic;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads
{
    public record PaginatedVouchersListByDetailedAccountResponse(
        int PageSize,
        int PageNumber,
        int TotalCount,
        IList<VoucherByDetailedAccountResponse> Items);

    public record VoucherByDetailedAccountResponse(
        Guid Id,
        VoucherTemplate Template,
        string Title,
        DateTime EffectiveDate,
        long Amount,
        IList<ArticleResponse> ArticleResponses);

    public record ArticleResponse(
        Guid AccountId,
        Guid? LookupId,
        int? Quantity,
        long Amount,
        Currency Currency,
        ArticleType Type);

}
