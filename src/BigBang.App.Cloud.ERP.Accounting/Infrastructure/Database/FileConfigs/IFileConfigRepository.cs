using System.Collections.Generic;
using BigBang.App.Cloud.ERP.Accounting.Domain;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.FileConfigs
{
    public interface IFileConfigRepository : IRepository<ACC_FileConfig, int>
    {
        IList<ACC_FileConfig> GetListByEntityName<TEntity>() where TEntity : class, new();
    }
}