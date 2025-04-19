using System.Collections.Generic;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.Accounts.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Common;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Accounts
{
    public interface IAccountService
    {
        Task<AccountTreeResponse> GetAccountsAsync();
        Task<IList<SubsidiaryAccountFinancialBalanceResponse>> GetSubsidiaryAccountFinancialBalanceAsync(SubsidiaryAccountFinancialBalanceRequest request);
        Task<PaginatedHeaderAndDetailsResponse<DetailedAccountFinancialBalanceResponse, DetailedAccountResponse>> GetDetailedAccountFinancialBalanceAsync(DetailedAccountFinancialBalanceRequest request);
        Task<DetailedAccountFinancialBalanceTotalResponse> CalculateDetailedAccountFinancialBalanceTotalAsync(DetailedAccountFinancialBalanceTotalRequest request);
    }
}
