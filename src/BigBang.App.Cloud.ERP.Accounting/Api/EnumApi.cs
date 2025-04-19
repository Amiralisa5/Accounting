using System.Collections.Generic;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.Enums;
using BigBang.App.Cloud.ERP.Accounting.Application.Enums.Payloads;
using BigBang.WebServer.Common.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace BigBang.App.Cloud.ERP.Accounting.Api
{
    [Endpoint]
    [Route("enums")]
    public class EnumApi
    {
        [Route("")]
        [HttpGet]
        public async Task<List<EnumDataModel>> GetListAsync([FromServices] IEnumService enumService)
        {
            return await enumService.GetListAsync();
        }

        [Route("{name}")]
        [HttpGet]
        public async Task<EnumDataModel> GetAsync([FromRoute] string name, [FromServices] IEnumService enumService)
        {
            return await enumService.GetAsync(name);
        }
    }
}
