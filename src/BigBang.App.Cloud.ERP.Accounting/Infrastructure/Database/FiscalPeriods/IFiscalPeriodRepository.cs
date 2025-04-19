using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Domain;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.FiscalPeriods
{
    public interface IFiscalPeriodRepository : IRepository<ACC_FiscalPeriod, Guid>
    {
        Task<IList<ACC_FiscalPeriod>> GetListByBusinessIdAsync(Guid businessId);
    }
}
