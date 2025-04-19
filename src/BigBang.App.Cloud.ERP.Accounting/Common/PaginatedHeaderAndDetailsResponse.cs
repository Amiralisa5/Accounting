using System.Collections.Generic;

namespace BigBang.App.Cloud.ERP.Accounting.Common
{
    public record PaginatedHeaderAndDetailsResponse<THeader, TDetails>(
        int PageSize,
        int PageNumber,
        THeader Header,
        List<TDetails> Details) : PaginatedDetailsResponse<TDetails>(PageSize, PageNumber, Details);

}
