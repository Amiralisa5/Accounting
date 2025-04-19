using System;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Accounts.Payloads;

public record DetailedAccountResponse(
    Guid Id,
    string Name,
    long TotalDebit,
    long TotalCredit,
    long Difference);