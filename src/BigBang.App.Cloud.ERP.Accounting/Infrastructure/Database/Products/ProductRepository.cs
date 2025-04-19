using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.WebServer.Common;
using BigBang.WebServer.Common.Attributes;
using BigBang.WebServer.Common.Services;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Products
{
    [Service(ServiceType = typeof(IProductRepository), InstanceMode = InstanceMode.Scoped, Requestable = false)]
    internal class ProductRepository : BaseRepository<ACC_Product, Guid>, IProductRepository
    {
        public ProductRepository(ISessionLoader sessionLoader) : base(sessionLoader)
        {
        }

        public async Task<IList<ACC_Product>> GetListByIdsAsync(IEnumerable<Guid> ids)
        {
            return await Session.QueryOver<ACC_Product>()
                                .Where(product => ids.Contains(product.Id))
                                .OrderBy(product => product.Name)
                                .Asc
                                .ToListAsync();
        }

        public async Task<IList<ACC_Product>> GetListByBusinessIdAsync(Guid businessId)
        {
            return await Session.QueryOver<ACC_Product>()
                .Where(product => product.Business.Id == businessId)
                .OrderBy(product => product.Name)
                .Asc
                .ToListAsync();
        }

        public async Task<bool> ProductExistsAsync(Guid businessId, string name)
        {
            return await Session.QueryOver<ACC_Product>()
                .Where(product => product.Business.Id == businessId && product.Name == name)
                .RowCountAsync() > 0;
        }

        public async Task<bool> ProductExistsAsync(Guid businessId, string name, Guid excludedProductId)
        {
            return await Session.QueryOver<ACC_Product>()
                .Where(product => product.Business.Id == businessId && product.Name == name && product.Id != excludedProductId)
                .RowCountAsync() > 0;
        }
    }
}