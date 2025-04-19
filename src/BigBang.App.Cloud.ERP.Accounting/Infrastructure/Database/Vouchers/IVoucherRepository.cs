using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.Accounts.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Application.Products.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers.Dtos;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers
{
    public interface IVoucherRepository : IRepository<ACC_Voucher, Guid>
    {
        Task<bool> ArticlesExistAsync(LookupType lookupType, Guid lookupId);
        Task<IList<ACC_Voucher>> GetListAsync(Guid fiscalPeriodId, VoucherTemplate template, int pageSize, int pageNumber);
        Task<ACC_Voucher> GetLastAsync(Guid fiscalPeriodId);
        Task<Guid?> GetFileIdAsync(Guid voucherId);
        Task<long> GetTotalDebtsAsync(Guid fiscalPeriodId, Guid lookupId, LookupType lookupType, ArticleType articleType, string accountName);
        Task<int> GetTotalCountAsync(Guid fiscalPeriodId, VoucherTemplate template);
        Task<IList<ACC_Voucher>> GetListByLookupIdAsync(DateTime? fromDate, DateTime? toDate, int pageSize, int pageNumber, Guid lookupId, Guid fiscalPeriodId, LookupType lookupType);
        Task<int> GetTotalCountAsync(Guid fiscalPeriodId, DateTime? fromDate, DateTime? toDate, Guid lookupId, LookupType lookupType);
        Task<IList<SubsidiaryAccountBalanceSheetDto>> GetSubsidiaryAccountFinancialBalanceDataAsync(Guid fiscalPeriodId, DateTime fromDate, DateTime toDate);
        Task<IList<DetailedAccountFinancialBalanceDto>> GetDetailedAccountFinancialBalanceDataAsync(DetailedAccountFinancialBalanceRequest request, Guid fiscalPeriodId, LookupType lookupType);
        Task<IList<DetailedAccountFinancialBalanceDto>> CalculateDetailedAccountFinancialBalanceTotalAsync(DetailedAccountFinancialBalanceTotalRequest request, Guid fiscalPeriodId, LookupType lookupType);
        Task<IList<ACC_Article>> GetVoucherInvoiceDataAsync(Guid voucherId, string accountName);
        Task<IList<ProductAggregatorDto>> GetProductsAggregatorDataAsync(GrossProfitAndLossRequest request, Guid sellAccountId, Guid costOfProductSoldAccountId, Guid businessId, string sortBy, SortDirection sortDirection);
        Task<IList<AccountTotalAmountDto>> GetTotalAmountByAccountIdAsync(DateTime from, DateTime to, Guid fiscalPeriod, List<Guid> accountIds);
        Task<long> GetAggregateArticlesByParentAccountIdInDurationAsync(DateTime from, DateTime to, Guid fiscalPeriodId, Guid parentId, ArticleType articleType);
        Task<IList<BalanceSheetDto>> GetBalanceSheetResponseByDateAsync(Guid fiscalPeriodId, DateTime to, List<Guid> parentAccountIds, List<Guid> accountIds);
    }
}