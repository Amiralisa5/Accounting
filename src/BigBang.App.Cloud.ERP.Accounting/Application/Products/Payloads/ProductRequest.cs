using BigBang.App.Cloud.ERP.Accounting.Common.Validators;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Products.Payloads
{
    public record ProductRequest(string Name, long BuyPrice, long SuggestedSellPrice, int Stock) : IRequest;
}
