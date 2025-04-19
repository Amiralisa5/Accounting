using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.WebServer.Common;
using BigBang.WebServer.Common.Attributes;
using BigBang.WebServer.Common.Services;
using NHibernate;
using NHibernate.Transform;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Accounts
{
    [Service(ServiceType = typeof(IAccountRepository), InstanceMode = InstanceMode.Scoped, Requestable = false)]
    internal class AccountRepository : BaseRepository<ACC_Account, Guid>, IAccountRepository
    {
        public AccountRepository(ISessionLoader sessionLoader) : base(sessionLoader)
        {
        }

        public async Task SaveAllAsync(IEnumerable<ACC_Account> accounts)
        {
            //await ServiceLocator.Get<IBulkRepository<ACC_Account>>().SaveAsync(accounts);
            foreach (var account in accounts)
            {
                await AddAsync(account);
            }
        }

        public async Task<IList<ACC_Account>> GetListByParentNameAsync(string parentName)
        {
            ACC_Account parentAlias = null;
            ACC_Account childAlias = null;

            return await Session.QueryOver(() => parentAlias)
                .JoinAlias(() => parentAlias.Accounts, () => childAlias)
                .Where(() => parentAlias.Name == parentName)
                .SelectList(list => list
                    .Select(() => childAlias.Id).WithAlias(() => childAlias.Id)
                    .Select(() => childAlias.Name).WithAlias(() => childAlias.Name)
                    .Select(() => childAlias.DisplayName).WithAlias(() => childAlias.DisplayName)
                    .Select(() => childAlias.Code).WithAlias(() => childAlias.Code))
                .TransformUsing(Transformers.AliasToBean<ACC_Account>())
                .ListAsync<ACC_Account>();
        }

        public async Task<IList<ACC_Account>> GetListByFiscalPeriodIdAsync(Guid fiscalPeriodId)
        {
            return await Session.QueryOver<ACC_Account>()
                .Fetch(SelectMode.Fetch, account => account.PersonRoles)
                .Where(account => account.FiscalPeriod.Id == fiscalPeriodId)
                .OrderBy(account => account.Code)
                .Asc
                .TransformUsing(Transformers.DistinctRootEntity)
                .ListAsync();
        }

        public async Task<ACC_Account> GetByNameAndFiscalPeriodIdAsync(Guid fiscalPeriodId, string name)
        {
            return await Session.QueryOver<ACC_Account>()
                .Where(account => account.FiscalPeriod.Id == fiscalPeriodId && account.Name == name)
                .SingleOrDefaultAsync();
        }

        public async Task<IList<ACC_Account>> GetListByIdsAsync(IEnumerable<Guid> ids)
        {
            return await Session.QueryOver<ACC_Account>()
                .Where(account => ids.Contains(account.Id))
                .OrderBy(account => account.Code)
                .Asc
                .ToListAsync();
        }


        public async Task<IList<ACC_Account>> GetByNamesAndFiscalPeriodIdAsync(Guid fiscalPeriodId, params string[] names)
        {
            return await Session.QueryOver<ACC_Account>()
                .Where(account => names.Contains(account.Name) && account.FiscalPeriod.Id == fiscalPeriodId)
                .OrderBy(account => account.Name)
                .Asc
                .TransformUsing(Transformers.DistinctRootEntity)
                .ToListAsync();
        }

    }
}