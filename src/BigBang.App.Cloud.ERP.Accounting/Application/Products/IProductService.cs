using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.Products.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Common;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Products
{
    public interface IProductService
    {
        Task<Guid> AddProductAsync(ProductRequest request);
        Task DeleteProductAsync(Guid id);
        Task<ProductResponse> GetProductAsync(Guid id);
        Task<IEnumerable<ProductResponse>> GetProductsAsync();
        Task<Guid> UpdateProductAsync(Guid id, ProductRequest request);
        Task<PaginatedDetailsResponse<ProductGrossProfitAndLostResponse>> CalculateGrossProfitAndLossAsync(GrossProfitAndLossRequest request);
        Task<PaginatedVouchersListByDetailedAccountResponse> GetProductVouchersAsync(ProductVouchersRequest request);
        Task<GrossProfitAndLostTotalResponse> CalculateGrossProfitAndLossTotalAsync(GrossProfitAndLossTotalRequest request);
    }
}