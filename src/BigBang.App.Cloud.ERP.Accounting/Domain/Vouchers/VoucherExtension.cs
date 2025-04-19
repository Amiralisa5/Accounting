using System;
using System.Collections.Generic;
using System.Linq;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;

namespace BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers
{
    internal static class VoucherExtension
    {
        public static long CalculateAmount(this ACC_Voucher voucher)
        {
            return voucher.Articles
                .Where(article => !article.IsTransactionalOnly && article.Type == ArticleType.Debit)
                .Sum(article => article.Amount);
        }

        public static IList<ArticleResponse> GetListByLookupId(this ACC_Voucher voucher, Guid lookupId)
        {
            return voucher.Articles.Where(article => article.LookupId == lookupId)
                .Select(article => new ArticleResponse(
                    article.Account.Id,
                    article.LookupId,
                    article.Quantity,
                    article.Amount,
                    article.Currency,
                    article.Type)).ToList();
        }
    }
}
