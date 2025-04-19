using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.PersonAccounts
{
    public interface IPersonAccountRepository : IRepository<ACC_PersonAccount, Guid>
    {
        Task<IList<ACC_PersonAccount>> GetListByBusinessIdAsync(Guid businessId);
        Task<IList<ACC_PersonAccount>> GetListByBusinessIdAndRolesAsync(Guid businessId, IList<PersonRoleType> personRoles);
        Task RemoveAllRolesAsync(ACC_PersonAccount personAccount);
    }
}

