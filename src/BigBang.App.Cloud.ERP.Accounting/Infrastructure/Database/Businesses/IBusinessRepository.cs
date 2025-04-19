using System;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Domain;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Businesses
{
    public interface IBusinessRepository : IRepository<ACC_Business, Guid>
    {
        Task<ACC_Business> GetByOwnerIdAsync(Guid ownerId);
        Task<ACC_Business> GetByPodBusinessIdAsync(long podBusinessId);
    }
}
