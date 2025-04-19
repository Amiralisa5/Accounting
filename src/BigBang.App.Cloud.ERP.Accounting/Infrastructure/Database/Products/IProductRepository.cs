using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Domain;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Products
{
    public interface IProductRepository : IRepository<ACC_Product, Guid>
    {
        Task<IList<ACC_Product>> GetListByBusinessIdAsync(Guid businessId);
        Task<IList<ACC_Product>> GetListByIdsAsync(IEnumerable<Guid> ids);
        Task<bool> ProductExistsAsync(Guid businessId, string name);
        Task<bool> ProductExistsAsync(Guid businessId, string name, Guid excludedProductId);
    }
}