using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.Files;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Common;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Reports;
using BigBang.WebServer.Common.Attributes;
using BigBang.WebServer.Common.Services.File;
using Microsoft.AspNetCore.Mvc;

namespace BigBang.App.Cloud.ERP.Accounting.Api
{
    [Endpoint]
    [Route("vouchers")]
    public class VoucherApi
    {
        [Route("")]
        [HttpGet]
        public async Task<PaginatedVouchersListResponse> GetVoucherListAsync([FromQuery] VoucherTemplate template, [FromQuery] int pageSize, [FromQuery] int pageNumber,
            [FromServices] IVoucherService voucherService)
        {
            return await voucherService.GetVoucherListAsync(new GetVoucherListRequest(template, pageSize, pageNumber));
        }

        [Route("{id}")]
        [HttpGet]
        public async Task<VoucherDetailsResponse> GetVoucherAsync([FromRoute] Guid id, [FromServices] IVoucherService voucherService)
        {
            return await voucherService.GetVoucherAsync(id);
        }

        [Route("")]
        [HttpPost]
        public async Task<VoucherResponse> RegisterVoucherAsync([FromBody] RegisterVoucherRequest request, [FromServices] IVoucherService voucherService)
        {
            return await voucherService.RegisterVoucherAsync(request);
        }

        [Route("file-upload")]
        [HttpPost]
        public async Task<Guid> UploadAsync([FromBody] Stream data, [FromBody] string fileName, [FromServices] IFileService fileService)
        {
            return await fileService.UploadAsync<ACC_Voucher>(data, fileName);
        }

        [Route("{id}/print")]
        [HttpGet]
        public ITemporaryFile InvoiceReport([FromRoute] Guid id, [FromServices] IReportService reportService)
        {
            var parameters = new Dictionary<string, object> { { "Id", id } };
            return reportService.Print<ACC_Product>(Constants.InvoiceReport, parameters);
        }

        [Route("balance-sheet")]
        [HttpGet]
        public async Task<IList<BalanceSheetResponse>> GetBalanceSheetAsync([FromQuery] DateTime to, [FromServices] IVoucherService voucherService)
        {
            return await voucherService.GetBalanceSheetAsync(to);
        }
    }
}