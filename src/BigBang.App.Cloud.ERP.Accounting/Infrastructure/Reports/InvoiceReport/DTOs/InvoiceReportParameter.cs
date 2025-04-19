using System;
using System.Collections.Generic;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Reports.InvoiceReport.DTOs
{
    public class InvoiceReportParameter
    {
        public string Number { get; set; }
        public string Title { get; set; }
        public string EffectiveDate { get; set; }
        public string Description { get; set; }
        public string Buyer { get; set; }
        public string Seller { get; set; }
        public Guid Id { get; set; }

        public List<InvoiceDetailReportParameter> InvoiceDetailReportParameter { get; set; }
    }

    public class InvoiceDetailReportParameter
    {
        public int Quantity { get; set; }
        public long UnitPrice { get; set; }
        public long Amount { get; set; }
        public string ProductName { get; set; }
    }
}