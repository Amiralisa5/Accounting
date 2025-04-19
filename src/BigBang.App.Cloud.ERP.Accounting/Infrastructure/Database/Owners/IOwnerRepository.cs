using System;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Domain;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Owners
{
    public interface IOwnerRepository : IRepository<ACC_Owner, Guid>
    {
        Task<ACC_Owner> GetByUserIdAsync(long userId);
    }
}
