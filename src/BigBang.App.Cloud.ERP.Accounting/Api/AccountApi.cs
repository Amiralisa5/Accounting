using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Application.Accounts.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Common;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
using BigBang.WebServer.Common.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace BigBang.App.Cloud.ERP.Accounting.Api
{
    [Endpoint]
    [Route("accounts")]
    public class AccountApi
    {
        [HttpGet]
        [Route("")]
        public async Task<AccountTreeResponse> GetAccountsAsync([FromServices] IAccountService accountService)
        {
            return await accountService.GetAccountsAsync();
        }

        [Route("financial-balance")]
        [HttpGet]
        public async Task<IList<SubsidiaryAccountFinancialBalanceResponse>> GetSubsidiaryAccountFinancialBalanceAsync([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate, [FromServices] IAccountService accountService)
        {
            return await accountService.GetSubsidiaryAccountFinancialBalanceAsync(new SubsidiaryAccountFinancialBalanceRequest(fromDate, toDate));
        }

        [Route("detailed-accounts")]
        [HttpPost]
        public async Task<PaginatedHeaderAndDetailsResponse<DetailedAccountFinancialBalanceResponse, DetailedAccountResponse>> GetDetailedAccountFinancialBalanceAsync([FromBody] DetailedAccountFinancialBalanceRequest detailedAccountFinancialBalanceRequest, [FromServices] IAccountService accountService)
        {
            return await accountService.GetDetailedAccountFinancialBalanceAsync(detailedAccountFinancialBalanceRequest);
        }

        [Route("detailed-accounts-total")]
        [HttpPost]
        public async Task<DetailedAccountFinancialBalanceTotalResponse> CalculateDetailedAccountFinancialBalanceTotalAsync([FromBody] DetailedAccountFinancialBalanceTotalRequest detailedAccountFinancialBalanceTotalRequest, [FromServices] IAccountService accountService)
        {
            return await accountService.CalculateDetailedAccountFinancialBalanceTotalAsync(detailedAccountFinancialBalanceTotalRequest);
        }

        [Route("net-profit-and-loss-total")]
        [HttpGet]
        public async Task<IList<NetProfitAndLoss>> CalculateNetProfitAndLoss([FromQuery] DateTime from, [FromQuery] DateTime to, [FromServices] IVoucherService voucherService)
        {
            return await voucherService.CalculateNetProfitAndLossAsync(from, to);
        }
    }
}