using System;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;

namespace BigBang.App.Cloud.ERP.Accounting.Application.BankAccounts.Payloads
{
    public record BankAccountResponse(Guid Id, Bank Bank, string HolderName, string Title, string ShebaNumber, string CardNumber, long Balance);
}
