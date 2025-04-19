using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;

namespace BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers.Strategies
{
    public interface ITemplateStrategy
    {
        Task<ACC_Voucher> RegisterAsync(RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod);
    }
}
