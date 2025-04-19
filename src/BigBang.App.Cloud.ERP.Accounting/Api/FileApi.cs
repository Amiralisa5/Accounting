using System;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.Files;
using BigBang.WebServer.Common.Attributes;
using BigBang.WebServer.Common.Services.File;
using Microsoft.AspNetCore.Mvc;

namespace BigBang.App.Cloud.ERP.Accounting.Api
{
    [Endpoint]
    [Route("files")]
    public class FileApi
    {
        [Route("{id}")]
        [HttpGet]
        public async Task<IFileResult> DownloadAsync([FromRoute] Guid id, [FromServices] IFileService fileService)
        {
            return await fileService.DownloadAsync(id);
        }
    }
}
