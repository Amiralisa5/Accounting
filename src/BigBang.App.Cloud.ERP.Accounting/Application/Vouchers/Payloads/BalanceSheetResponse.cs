using System;
using System.Collections.Generic;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads
{
    public record BalanceSheetResponse(string ParentAccountName,
                                       string ParentAccountDisplayName,
                                       long Amount,
                                       IList<BalanceSheetDetaileResponse> Items);
    public record BalanceSheetDetaileResponse(Guid AccountId,
                                              string AccountName,
                                              string AccountDisplayName,
                                              long Amount,
                                              AccountNature AccountNature);
}