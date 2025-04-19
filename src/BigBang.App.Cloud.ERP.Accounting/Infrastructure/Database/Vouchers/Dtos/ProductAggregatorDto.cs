using System;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers.Dtos
{
    public class ProductAggregatorDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int SellQuantity { get; set; }
        public long SellAmount { get; set; }
        public int CostQuantity { get; set; }
        public long CostAmount { get; set; }
        public long Difference { get; set; }
    }
}