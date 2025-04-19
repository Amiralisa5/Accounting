using System;
using System.Threading.Tasks;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Identity
{
    public interface IAccountingIdentityService
    {
        Task<Guid> GetOwnerIdAsync();
        Task<Guid> GetBusinessIdAsync();
        Task<Guid> GetFiscalPeriodIdAsync();
    }
}
