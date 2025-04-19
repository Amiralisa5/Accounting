using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Common.Helpers;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using BigBang.WebServer.Common.Exceptions;

namespace BigBang.App.Cloud.ERP.Accounting.Domain.Products
{
    internal static class ProductExtension
    {
        public static ACC_Product Buy(this ACC_Product product, ArticleRequest article)
        {
            var quantity = article.Quantity.GetValueOrDefault();

            // Buy price is updated evey time based on average
            product.BuyPrice = (product.BuyPrice * product.Stock + article.Amount) / (product.Stock + quantity);

            product.Stock += quantity;

            return product;
        }

        public static ACC_Product DecreaseStock(this ACC_Product product, ArticleRequest article)
        {
            if (product.Stock < article.Quantity)
            {
                throw ExceptionHelper.BadRequest(Messages.Error_ProductIsNotInStock);
            }

            product.Stock -= article.Quantity.GetValueOrDefault();

            return product;
        }

        public static ACC_Product UpdateSuggestedSellPrice(this ACC_Product product, ArticleRequest article)
        {
            product.SuggestedSellPrice = article.Fee.GetValueOrDefault();

            return product;
        }
    }
}
