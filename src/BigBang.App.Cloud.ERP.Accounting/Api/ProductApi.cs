using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.Products;
using BigBang.App.Cloud.ERP.Accounting.Application.Products.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Common;
using BigBang.WebServer.Common.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace BigBang.App.Cloud.ERP.Accounting.Api
{
    [Endpoint]
    [Route("products")]
    public class ProductApi
    {
        [Route("")]
        [HttpGet]
        public async Task<IEnumerable<ProductResponse>> GetProductsAsync([FromServices] IProductService productService)
        {
            return await productService.GetProductsAsync();
        }

        [Route("{id}")]
        [HttpGet]
        public async Task<ProductResponse> GetProductAsync([FromRoute] Guid id, [FromServices] IProductService productService)
        {
            return await productService.GetProductAsync(id);
        }

        [Route("")]
        [HttpPost]
        public async Task<Guid> AddProductAsync([FromBody] ProductRequest request, [FromServices] IProductService productService)
        {
            return await productService.AddProductAsync(request);
        }

        [Route("{id}")]
        [HttpPut]
        public async Task<Guid> UpdateProductAsync([FromRoute] Guid id, [FromBody] ProductRequest request,
            [FromServices] IProductService productService)
        {
            return await productService.UpdateProductAsync(id, request);
        }

        [Route("{id}")]
        [HttpDelete]
        public async Task DeleteProductAsync([FromRoute] Guid id, [FromServices] IProductService productService)
        {
            await productService.DeleteProductAsync(id);
        }

        [Route("{id}/vouchers")]
        [HttpGet]
        public async Task<PaginatedVouchersListByDetailedAccountResponse> GetProductVouchersAsync([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate,
            [FromQuery] int pageSize, [FromQuery] int pageNumber, [FromRoute] Guid id, [FromServices] IProductService productService)
        {
            return await productService.GetProductVouchersAsync(new ProductVouchersRequest(fromDate, toDate, pageSize, pageNumber, id));
        }

        [Route("gross-profit-and-loss")]
        [HttpPost]
        public async Task<PaginatedDetailsResponse<ProductGrossProfitAndLostResponse>> CalculateGrossProfitAndLossAsync([FromBody] GrossProfitAndLossRequest request, [FromServices] IProductService productService)
        {
            return await productService.CalculateGrossProfitAndLossAsync(request);
        }

        [Route("gross-profit-and-loss-total")]
        [HttpPost]
        public async Task<GrossProfitAndLostTotalResponse> CalculateGrossProfitAndLossTotalAsync([FromBody] GrossProfitAndLossTotalRequest request, [FromServices] IProductService productService)
        {
            return await productService.CalculateGrossProfitAndLossTotalAsync(request);
        }
    }
}
