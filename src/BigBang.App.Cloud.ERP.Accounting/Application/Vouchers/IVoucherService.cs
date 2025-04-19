using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Vouchers
{
    public interface IVoucherService
    {
        Task<VoucherResponse> RegisterVoucherAsync(RegisterVoucherRequest request);
        Task<PaginatedVouchersListResponse> GetVoucherListAsync(GetVoucherListRequest request);
        Task<VoucherDetailsResponse> GetVoucherAsync(Guid id);
        Task<IList<NetProfitAndLoss>> CalculateNetProfitAndLossAsync(DateTime from, DateTime to);
        Task<IList<BalanceSheetResponse>> GetBalanceSheetAsync(DateTime to);
    }
}