using System;
using System.Collections.Generic;
using BigBang.App.Cloud.ERP.Accounting.Common.Validators;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Products.Payloads
{
    public record GrossProfitAndLossRequest(IEnumerable<Guid> Ids, DateTime From, DateTime To, int PageSize, int PageNumber, string SortBy, SortDirection SortDirection) : IRequest;
}
