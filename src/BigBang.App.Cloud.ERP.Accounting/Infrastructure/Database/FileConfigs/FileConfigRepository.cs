using System;
using System.Collections.Generic;
using System.Linq;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.WebServer.Common;
using BigBang.WebServer.Common.Attributes;
using BigBang.WebServer.Common.Services;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.FileConfigs
{
    [Service(ServiceType = typeof(IFileConfigRepository), InstanceMode = InstanceMode.Single, Requestable = false)]
    internal class FileConfigRepository : BaseRepository<ACC_FileConfig, int>, IFileConfigRepository
    {
        private readonly IList<ACC_FileConfig> _fileConfigs;
        public FileConfigRepository(ISessionLoader sessionLoader) : base(sessionLoader)
        {
            _fileConfigs = Session.QueryOver<ACC_FileConfig>().ToList();
        }
        public IList<ACC_FileConfig> GetListByEntityName<TEntity>() where TEntity : class, new()
        {
            return _fileConfigs
                .Where(fileConfig => fileConfig.EntityName.Equals(nameof(TEntity), StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
}