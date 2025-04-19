using System;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.WebServer.Common.Attributes;
using BigBang.WebServer.Common.Services;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Businesses
{
    [Service(ServiceType = typeof(IBusinessRepository), InstanceMode = InstanceMode.Scoped, Requestable = false)]
    internal class BusinessRepository : BaseRepository<ACC_Business, Guid>, IBusinessRepository
    {
        public BusinessRepository(ISessionLoader sessionLoader) : base(sessionLoader)
        {
        }

        public async Task<ACC_Business> GetByOwnerIdAsync(Guid ownerId)
        {
            return await Session.QueryOver<ACC_Business>()
                .Where(business => business.Owner.Id == ownerId)
                .SingleOrDefaultAsync();
        }

        public async Task<ACC_Business> GetByPodBusinessIdAsync(long podBusinessId)
        {
            return await Session.QueryOver<ACC_Business>()
                .Where(business => business.PodBusinessId == podBusinessId)
                .SingleOrDefaultAsync();
        }
    }
}
