using BigBang.App.Cloud.ERP.Accounting.Common.Validators;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads
{
    public record VoucherFileUploadedRequest(byte[] Data, string FileName) : IRequest;
}