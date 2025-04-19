using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Domain;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Accounts
{
    public interface IAccountRepository : IRepository<ACC_Account, Guid>
    {
        Task SaveAllAsync(IEnumerable<ACC_Account> accounts);
        Task<IList<ACC_Account>> GetListByParentNameAsync(string parentName);
        Task<IList<ACC_Account>> GetListByFiscalPeriodIdAsync(Guid fiscalPeriodId);
        Task<ACC_Account> GetByNameAndFiscalPeriodIdAsync(Guid fiscalPeriodId, string name);
        Task<IList<ACC_Account>> GetListByIdsAsync(IEnumerable<Guid> ids);
        Task<IList<ACC_Account>> GetByNamesAndFiscalPeriodIdAsync(Guid fiscalPeriodId, params string[] names);
    }
}