using System;
using System.Collections.Generic;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads
{
    public record VoucherDetailsResponse(
        Guid Id,
        byte? Template,
        string Number,
        string Title,
        DateTime EffectiveDate,
        string Description,
        long Amount,
        Guid? FileId,
        IEnumerable<ArticleDetailsResponse> Articles);

    public record ArticleDetailsResponse(
        Guid AccountId,
        Guid? LookupId,
        int? Quantity,
        long? Fee,
        long Amount,
        Currency Currency,
        ArticleType Type,
        Dictionary<string, string> LookupInfo);
}