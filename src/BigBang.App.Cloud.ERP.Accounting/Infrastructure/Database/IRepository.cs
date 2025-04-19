using System.Collections.Generic;
using System.Threading.Tasks;
using BigBang.Metadata.Models;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database
{
    public interface IRepository<TAggregate, in TKey> where TAggregate : IEntity, new() where TKey : struct
    {
        Task<IList<TAggregate>> GetAllAsync();
        Task<TAggregate> GetAsync(TKey id);
        Task<TAggregate> AddAsync(TAggregate entity);
        Task<TAggregate> UpdateAsync(TAggregate entity);
        Task RemoveAsync(TKey id);
    }
}
