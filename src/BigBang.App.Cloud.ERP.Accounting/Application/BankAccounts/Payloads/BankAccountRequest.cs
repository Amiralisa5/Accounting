using BigBang.App.Cloud.ERP.Accounting.Common.Validators;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;

namespace BigBang.App.Cloud.ERP.Accounting.Application.BankAccounts.Payloads
{
    public record BankAccountRequest(Bank Bank, string HolderName, string Title, string ShebaNumber, string CardNumber, long Balance) : IRequest;
}