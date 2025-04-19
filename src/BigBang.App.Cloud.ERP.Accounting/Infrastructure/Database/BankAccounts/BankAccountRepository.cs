using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.WebServer.Common;
using BigBang.WebServer.Common.Attributes;
using BigBang.WebServer.Common.Services;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.BankAccounts
{
    [Service(ServiceType = typeof(IBankAccountRepository), InstanceMode = InstanceMode.Scoped, Requestable = false)]
    internal class BankAccountRepository : BaseRepository<ACC_BankAccount, Guid>, IBankAccountRepository
    {
        public BankAccountRepository(ISessionLoader sessionLoader) : base(sessionLoader)
        {
        }

        public async Task<IList<ACC_BankAccount>> GetListByBusinessIdAsync(Guid businessId)
        {
            return await Session.QueryOver<ACC_BankAccount>()
                .Where(bankAccount => bankAccount.Business.Id == businessId)
                .OrderBy(bankAccount => bankAccount.Title)
                .Desc
                .ToListAsync();
        }
    }
}
