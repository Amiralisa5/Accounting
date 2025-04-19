using System;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.WebServer.Common.Attributes;
using BigBang.WebServer.Common.Services;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Owners
{
    [Service(ServiceType = typeof(IOwnerRepository), InstanceMode = InstanceMode.Scoped, Requestable = false)]
    internal class OwnerRepository : BaseRepository<ACC_Owner, Guid>, IOwnerRepository
    {
        public OwnerRepository(ISessionLoader sessionLoader) : base(sessionLoader)
        {
        }

        public async Task<ACC_Owner> GetByUserIdAsync(long userId)
        {
            return await Session.QueryOver<ACC_Owner>()
                .Where(owner => owner.UserId == userId)
                .SingleOrDefaultAsync();
        }
    }
}
