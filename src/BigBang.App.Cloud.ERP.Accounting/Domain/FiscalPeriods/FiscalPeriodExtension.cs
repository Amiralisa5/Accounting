using System.Collections.Generic;
using System.Linq;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;

namespace BigBang.App.Cloud.ERP.Accounting.Domain.FiscalPeriods
{
    internal static class FiscalPeriodExtension
    {
        public static ACC_FiscalPeriod GetActiveFiscalPeriod(this IEnumerable<ACC_FiscalPeriod> fiscalPeriods)
        {
            return fiscalPeriods
                .SingleOrDefault(fiscalPeriod => fiscalPeriod.Status == FiscalPeriodStatus.Active);
        }
    }
}
