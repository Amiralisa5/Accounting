using System;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers.Dtos
{
    public class AccountTotalAmountDto
    {
        public Guid AccountId { get; set; }
        public ArticleType ArticleType { get; set; }
        public long TotalAmount { get; set; }
        public int? TotalQuantity { get; set; }
    }
}