using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.BankAccounts;
using BigBang.App.Cloud.ERP.Accounting.Application.BankAccounts.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
using BigBang.WebServer.Common.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace BigBang.App.Cloud.ERP.Accounting.Api
{
    [Endpoint]
    [Route("bank-accounts")]
    public class BankAccountApi
    {
        [Route("")]
        [HttpGet]
        public async Task<IEnumerable<BankAccountResponse>> GetBankAccountsAsync([FromServices] IBankAccountService bankAccountService)
        {
            return await bankAccountService.GetBankAccountsAsync();
        }

        [Route("{id}")]
        [HttpGet]
        public async Task<BankAccountResponse> GetBankAccountAsync([FromRoute] Guid id, [FromServices] IBankAccountService bankAccountService)
        {
            return await bankAccountService.GetBankAccountAsync(id);
        }

        [Route("")]
        [HttpPost]
        public async Task<Guid> AddBankAccountAsync([FromBody] BankAccountRequest request, [FromServices] IBankAccountService bankAccountService)
        {
            return await bankAccountService.AddBankAccountAsync(request);
        }

        [Route("{id}")]
        [HttpPut]
        public async Task<Guid> UpdateBankAccountAsync([FromRoute] Guid id, [FromBody] BankAccountRequest request,
            [FromServices] IBankAccountService bankAccountService)
        {
            return await bankAccountService.UpdateBankAccountAsync(id, request);
        }

        [Route("{id}")]
        [HttpDelete]
        public async Task DeleteBankAccountAsync([FromRoute] Guid id, [FromServices] IBankAccountService bankAccountService)
        {
            await bankAccountService.DeleteBankAccountAsync(id);
        }

        [Route("{id}/vouchers")]
        [HttpGet]
        public async Task<PaginatedVouchersListByDetailedAccountResponse> GetBankAccountVouchersAsync([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate,
            [FromQuery] int pageSize, [FromQuery] int pageNumber, [FromRoute] Guid id, [FromServices] IBankAccountService bankAccountService)
        {
            return await bankAccountService.GetBankAccountVouchersAsync(new BankAccountVouchersRequest(fromDate, toDate, pageSize, pageNumber, id));
        }
    }
}
