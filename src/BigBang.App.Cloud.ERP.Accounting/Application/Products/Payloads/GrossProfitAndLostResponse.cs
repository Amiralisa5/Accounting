namespace BigBang.App.Cloud.ERP.Accounting.Application.Products.Payloads
{
    public record GrossProfitAndLostTotalResponse(int TotalCount, long TotalSellAmount, long TotalCostOfProductSoldAmount);
}