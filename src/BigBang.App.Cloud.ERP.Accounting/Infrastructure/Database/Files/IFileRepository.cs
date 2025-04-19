using System;
using BigBang.App.Cloud.ERP.Accounting.Domain;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Files;

public interface IFileRepository : IRepository<ACC_File, Guid>
{

}