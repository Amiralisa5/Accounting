using System;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Products.Payloads
{
    public record ProductResponse(Guid Id, string Name, long BuyPrice, long SuggestedSellPrice, int Stock);
}
