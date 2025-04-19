using System;
using System.Collections.Generic;
using BigBang.App.Cloud.ERP.Accounting.Common.Validators;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Products.Payloads
{
    public record GrossProfitAndLossTotalRequest(IEnumerable<Guid> Ids, DateTime From, DateTime To) : IRequest;
}
