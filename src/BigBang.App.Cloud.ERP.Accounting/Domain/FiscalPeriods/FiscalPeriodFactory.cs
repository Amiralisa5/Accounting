using System;
using System.Globalization;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.App.Cloud.ERP.Accounting.Resources;

namespace BigBang.App.Cloud.ERP.Accounting.Domain.FiscalPeriods
{
    public static class FiscalPeriodFactory
    {
        public static ACC_FiscalPeriod CreateDefault(ACC_Business business)
        {
            // First day of persian year is 21 March
            var year = DateTime.Today < new DateTime(DateTime.Today.Year, 3, 21) ? DateTime.Today.Year - 1 : DateTime.Today.Year;
            var persianCalendar = new PersianCalendar();
            var persianYear = persianCalendar.GetYear(DateTime.Today);

            return new ACC_FiscalPeriod
            {
                Id = Guid.NewGuid(),
                Title = string.Format(Messages.Title_FiscalYear, persianYear),
                FromDate = new DateTime(year, 3, 21),
                ToDate = new DateTime(year + 1, 3, 20),
                Status = FiscalPeriodStatus.Active,
                Business = business
            };
        }
    }
}
