using System.Collections.Generic;
using System.Threading.Tasks;
using BigBang.Metadata.Models;
using BigBang.WebServer.Common.Services;
using NHibernate;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database
{
    internal abstract class BaseRepository<TAggregate, TKey> : IRepository<TAggregate, TKey> where TAggregate : class, IEntity, new() where TKey : struct
    {
        protected readonly ISession Session;

        protected BaseRepository(ISessionLoader sessionLoader)
        {
            Session = sessionLoader.GetSession();
        }

        public async Task<IList<TAggregate>> GetAllAsync()
        {
            return await Session.QueryOver<TAggregate>().ListAsync();
        }

        public virtual async Task<TAggregate> GetAsync(TKey id)
        {
            return await Session.GetAsync<TAggregate>(id);
        }

        public virtual async Task<TAggregate> AddAsync(TAggregate entity)
        {
            await Session.SaveAsync(entity);
            await Session.FlushAsync();
            return entity;
        }

        public virtual async Task<TAggregate> UpdateAsync(TAggregate entity)
        {
            await Session.UpdateAsync(entity);
            await Session.FlushAsync();

            return entity;
        }

        public virtual async Task RemoveAsync(TKey id)
        {
            var entity = await Session.GetAsync<TAggregate>(id);
            await Session.DeleteAsync(entity);
            await Session.FlushAsync();
        }
    }
}
