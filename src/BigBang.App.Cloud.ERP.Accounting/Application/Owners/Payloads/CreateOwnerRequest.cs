using BigBang.App.Cloud.ERP.Accounting.Common.Validators;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Owners.Payloads
{
    public record CreateOwnerRequest(string FirstName, string LastName, string MobileNumber, long UserId, string BusinessName, long BusinessId) : IRequest;
}