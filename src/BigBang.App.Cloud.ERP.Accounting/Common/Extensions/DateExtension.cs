using System;
using System.Globalization;

namespace BigBang.App.Cloud.ERP.Accounting.Common.Extensions
{
    public static class DateExtension
    {
        public static string ToPersian(this DateTime date)
        {
            var persianCalendar = new PersianCalendar();
            int persianYear = persianCalendar.GetYear(date);
            int persianMonth = persianCalendar.GetMonth(date);
            int persianDay = persianCalendar.GetDayOfMonth(date);
            string persianDateString = $"{persianYear:0000}/{persianMonth:00}/{persianDay:00}";

            return persianDateString;
        }
    }
}
