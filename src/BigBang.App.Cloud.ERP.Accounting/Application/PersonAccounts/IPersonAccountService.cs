using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.PersonAccounts.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;

namespace BigBang.App.Cloud.ERP.Accounting.Application.PersonAccounts
{
    public interface IPersonAccountService
    {
        Task<Guid> AddPersonAccountAsync(AddPersonAccountRequest request);
        Task DeletePersonAccountAsync(Guid id);
        Task<IEnumerable<PersonAccountResponse>> GetPersonAccountsAsync(IList<PersonRoleType> personRoleTypes);
        Task<PersonAccountResponse> GetPersonAccountAsync(Guid id);
        Task<PersonAccountResponse> GetOwnerPersonAccountAsync();
        Task<Guid> UpdatePersonAccountAsync(Guid id, UpdatePersonAccountRequest request);
        Task<long> GetTotalDebtsAsync(Guid id);
        Task<PaginatedVouchersListByDetailedAccountResponse> GetPersonAccountVouchersAsync(PersonAccountVouchersRequest request);
    }
}
