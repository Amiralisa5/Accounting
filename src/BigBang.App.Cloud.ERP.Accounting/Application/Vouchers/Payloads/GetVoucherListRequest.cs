using BigBang.App.Cloud.ERP.Accounting.Common.Validators;
using BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads
{
    public record GetVoucherListRequest(VoucherTemplate Template, int PageSize, int PageNumber) : IRequest;
}
