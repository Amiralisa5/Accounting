using System;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Products.Payloads
{
    public record ProductGrossProfitAndLostResponse(Guid Id, string Name, long SellAmount, long CostOfProductSoldAmount, int Quantity, long DiffrenceAmount);
}