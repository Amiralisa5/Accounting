using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.WebServer.Common;
using BigBang.WebServer.Common.Attributes;
using BigBang.WebServer.Common.Services;
using NHibernate;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.PersonAccounts
{
    [Service(ServiceType = typeof(IPersonAccountRepository), InstanceMode = InstanceMode.Scoped, Requestable = false)]
    internal class PersonAccountRepository : BaseRepository<ACC_PersonAccount, Guid>, IPersonAccountRepository
    {
        public PersonAccountRepository(ISessionLoader sessionLoader) : base(sessionLoader)
        {
        }

        //todo: should return just specific RoleTypes
        public async Task<IList<ACC_PersonAccount>> GetListByBusinessIdAndRolesAsync(Guid businessId, IList<PersonRoleType> personRoles)
        {
            var result = await GetListByBusinessIdAsync(businessId);

            if (personRoles.SequenceEqual(Enumerable.Empty<PersonRoleType>()))
            {
                return result;
            }

            result = result.Select(personAccount => new ACC_PersonAccount
            {
                Id = personAccount.Id,
                PersonAccountRoles = personAccount.PersonAccountRoles.Where(personRole => personRoles.Contains(personRole.PersonRoleTypeId)).ToList(),
                FirstName = personAccount.FirstName,
                LastName = personAccount.LastName,
                MobileNumber = personAccount.MobileNumber,
                Business = personAccount.Business,
            }).ToList();

            return result.Where(personAccount => personAccount.PersonAccountRoles.Any()).ToList();
        }

        public async Task<IList<ACC_PersonAccount>> GetListByBusinessIdAsync(Guid businessId)
        {
            return await Session.QueryOver<ACC_PersonAccount>()
                .Where(personAccount => personAccount.Business.Id == businessId)
                .OrderBy(personAccount => personAccount.FirstName)
                .Asc
                .ThenBy(personAccount => personAccount.LastName)
                .Asc
                .ToListAsync();
        }

        public override async Task<ACC_PersonAccount> GetAsync(Guid id)
        {
            return await Session.QueryOver<ACC_PersonAccount>()
                .Fetch(SelectMode.Fetch, personAccount => personAccount.PersonAccountRoles)
                .Where(personAccount => personAccount.Id == id)
                .SingleOrDefaultAsync();
        }

        public async Task RemoveAllRolesAsync(ACC_PersonAccount personAccount)
        {
            foreach (var role in personAccount.PersonAccountRoles)
            {
                await Session.DeleteAsync(role);
            }

            await Session.EvictAsync(personAccount);
        }
    }
}