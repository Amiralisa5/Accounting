using System;
using System.Collections.Generic;
using BigBang.App.Cloud.ERP.Accounting.Common.Validators;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads
{
    public record RegisterVoucherRequest(
        VoucherTemplate Template,
        string Description,
        DateTime EffectiveDate,
        Guid? FileId,
        IEnumerable<ArticleRequest> Articles) : IRequest;

    public record ArticleRequest(
        Guid AccountId,
        Guid? LookupId,
        long Amount,
        Currency Currency,
        int? Quantity,
        long? Fee,
        ArticleType Type,
        bool IsTransactionalOnly);
}
