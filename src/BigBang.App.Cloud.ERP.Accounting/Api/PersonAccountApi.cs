using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.PersonAccounts;
using BigBang.App.Cloud.ERP.Accounting.Application.PersonAccounts.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Common.Collections;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.WebServer.Common.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace BigBang.App.Cloud.ERP.Accounting.Api
{
    [Endpoint]
    [Route("person-accounts")]
    public class PersonAccountApi
    {
        [Route("")]
        [HttpGet]
        public async Task<IEnumerable<PersonAccountResponse>> GetPersonAccountsAsync([FromQuery] ByteCollection roleTypes, [FromServices] IPersonAccountService personAccountService)
        {
            var personRoleTypes = roleTypes is null ? Enumerable.Empty<PersonRoleType>() : roleTypes.Cast<PersonRoleType>();
            return await personAccountService.GetPersonAccountsAsync(personRoleTypes.ToList());
        }

        [Route("{id}")]
        [HttpGet]
        public async Task<PersonAccountResponse> GetPersonAccountAsync([FromRoute] Guid id, [FromServices] IPersonAccountService personAccountService)
        {
            return await personAccountService.GetPersonAccountAsync(id);
        }

        [Route("business-owner")]
        [HttpGet]
        public async Task<PersonAccountResponse> GetOwnerPersonAccountAsync([FromServices] IPersonAccountService personAccountService)
        {
            return await personAccountService.GetOwnerPersonAccountAsync();
        }

        [Route("")]
        [HttpPost]
        public async Task<Guid> AddPersonAccountAsync([FromBody] AddPersonAccountRequest request, [FromServices] IPersonAccountService personAccountService)
        {
            return await personAccountService.AddPersonAccountAsync(request);
        }

        [Route("{id}")]
        [HttpPut]
        public async Task<Guid> UpdatePersonAccountAsync([FromRoute] Guid id, [FromBody] UpdatePersonAccountRequest request, [FromServices] IPersonAccountService personAccountService)
        {
            return await personAccountService.UpdatePersonAccountAsync(id, request);
        }

        [Route("{id}")]
        [HttpDelete]
        public async Task DeletePersonAccountAsync([FromRoute] Guid id, [FromServices] IPersonAccountService personAccountService)
        {
            await personAccountService.DeletePersonAccountAsync(id);
        }

        [Route("{id}/debts")]
        [HttpGet]
        public async Task<long> GetTotalDebtsAsync([FromRoute] Guid id, [FromServices] IPersonAccountService personAccountService)
        {
            return await personAccountService.GetTotalDebtsAsync(id);
        }

        [Route("{id}/vouchers")]
        [HttpGet]
        public async Task<PaginatedVouchersListByDetailedAccountResponse> GetPersonAccountVouchersAsync([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate,
            [FromQuery] int pageSize, [FromQuery] int pageNumber, [FromRoute] Guid id, [FromServices] IPersonAccountService personAccountService)
        {
            return await personAccountService.GetPersonAccountVouchersAsync(new PersonAccountVouchersRequest(fromDate, toDate, pageSize, pageNumber, id));
        }
    }
}