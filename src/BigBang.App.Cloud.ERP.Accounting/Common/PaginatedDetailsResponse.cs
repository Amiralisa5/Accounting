using System.Collections.Generic;

namespace BigBang.App.Cloud.ERP.Accounting.Common
{
    public record PaginatedDetailsResponse<TDetails>(int PageSize, int PageNumber, List<TDetails> Details);
}
