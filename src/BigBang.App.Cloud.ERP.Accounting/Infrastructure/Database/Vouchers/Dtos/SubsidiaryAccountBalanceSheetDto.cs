using System;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers.Dtos
{
    public record SubsidiaryAccountBalanceSheetDto
    {
        public SubsidiaryAccountBalanceSheetDto() { }

        public SubsidiaryAccountBalanceSheetDto(Guid id, long totalDebit, long totalCredit)
        {
            Id = id;
            TotalDebit = totalDebit;
            TotalCredit = totalCredit;
        }

        public Guid Id { get; set; }
        public long TotalDebit { get; set; }
        public long TotalCredit { get; set; }
    }
}