using System.Collections.Generic;
using System.Linq;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Common.Helpers;
using BigBang.App.Cloud.ERP.Accounting.Resources;

namespace BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers
{
    internal static class ArticleExtension
    {
        public static void ValidateProductArticles(this IEnumerable<ArticleRequest> articles)
        {
            var errors = new List<string>();

            foreach (var article in articles)
            {
                switch (article.Quantity)
                {
                    case null:
                        errors.Add(string.Format(Messages.Error_ShouldNotBeNull, Messages.Label_Quantity));
                        break;

                    case < 1:
                        errors.Add(string.Format(Messages.Error_ShouldBeGreaterThan, Messages.Label_Quantity, 0));
                        break;
                }

                switch (article.Fee)
                {
                    case null:
                        errors.Add(string.Format(Messages.Error_ShouldNotBeNull, Messages.Label_Fee));
                        break;

                    case < 1:
                        errors.Add(string.Format(Messages.Error_ShouldBeGreaterThan, Messages.Label_Fee, 0));
                        break;
                }
            }

            if (errors.Any())
            {
                throw ExceptionHelper.BadRequest(errors);
            }
        }
    }
}
