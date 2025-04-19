using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.WebServer.Common;
using BigBang.WebServer.Common.Attributes;
using BigBang.WebServer.Common.Services;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.FiscalPeriods
{
    [Service(ServiceType = typeof(IFiscalPeriodRepository), InstanceMode = InstanceMode.Scoped, Requestable = false)]
    internal class FiscalPeriodRepository : BaseRepository<ACC_FiscalPeriod, Guid>, IFiscalPeriodRepository
    {
        public FiscalPeriodRepository(ISessionLoader sessionLoader) : base(sessionLoader)
        {
        }

        public async Task<IList<ACC_FiscalPeriod>> GetListByBusinessIdAsync(Guid businessId)
        {
            return await Session.QueryOver<ACC_FiscalPeriod>()
                .Where(fiscalPeriod => fiscalPeriod.Business.Id == businessId)
                .ToListAsync();
        }
    }
}
