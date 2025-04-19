using System;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.WebServer.Common.Attributes;
using BigBang.WebServer.Common.Services;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Files;

[Service(ServiceType = typeof(IFileRepository), InstanceMode = InstanceMode.Scoped, Requestable = false)]
internal class FileRepository : BaseRepository<ACC_File, Guid>, IFileRepository
{
    public FileRepository(ISessionLoader sessionLoader) : base(sessionLoader)
    {
    }
}