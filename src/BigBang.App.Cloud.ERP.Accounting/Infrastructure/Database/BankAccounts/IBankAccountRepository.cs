using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Domain;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.BankAccounts
{
    public interface IBankAccountRepository : IRepository<ACC_BankAccount, Guid>
    {
        Task<IList<ACC_BankAccount>> GetListByBusinessIdAsync(Guid businessId);
    }
}