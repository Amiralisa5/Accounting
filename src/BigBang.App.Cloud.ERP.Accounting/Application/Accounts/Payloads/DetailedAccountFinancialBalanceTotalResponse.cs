namespace BigBang.App.Cloud.ERP.Accounting.Application.Accounts.Payloads
{
    public record DetailedAccountFinancialBalanceTotalResponse(
        long TotalCount,
        long TotalDebit,
        long TotalCredit,
        long TotalDifference);
}
