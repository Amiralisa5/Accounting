using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.BankAccounts.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;

namespace BigBang.App.Cloud.ERP.Accounting.Application.BankAccounts
{
    public interface IBankAccountService
    {
        Task<Guid> AddBankAccountAsync(BankAccountRequest request);
        Task DeleteBankAccountAsync(Guid id);
        Task<BankAccountResponse> GetBankAccountAsync(Guid id);
        Task<IEnumerable<BankAccountResponse>> GetBankAccountsAsync();
        Task<Guid> UpdateBankAccountAsync(Guid id, BankAccountRequest request);
        Task<PaginatedVouchersListByDetailedAccountResponse> GetBankAccountVouchersAsync(BankAccountVouchersRequest request);
    }
}
