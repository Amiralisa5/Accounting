using System;
using System.IO;
using System.Threading.Tasks;
using BigBang.WebServer.Common.Services.File;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Files;

public interface IFileService
{
    Task<Guid> UpdateFileOwnerAsync(Guid id, Guid entityOwnerId);
    Task<IFileResult> DownloadAsync(Guid id);
    Task<Guid> UploadAsync<TEntity>(Stream data, string fileName) where TEntity : class, new();
}
