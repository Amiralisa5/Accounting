using System;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers.Dtos
{
    public record DetailedAccountFinancialBalanceDto
    {
        public DetailedAccountFinancialBalanceDto() { }

        public DetailedAccountFinancialBalanceDto(Guid id, long totalDebit, long totalCredit, string name, long difference)
        {
            Id = id;
            TotalDebit = totalDebit;
            TotalCredit = totalCredit;
            Name = name;
            Difference = difference;
        }
        public Guid Id { get; set; }
        public string Name { get; set; }
        public long TotalDebit { get; set; }
        public long TotalCredit { get; set; }
        public long Difference { get; set; }
    }
}
