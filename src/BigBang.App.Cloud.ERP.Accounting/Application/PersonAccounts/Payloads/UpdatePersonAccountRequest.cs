using System.Collections.Generic;
using BigBang.App.Cloud.ERP.Accounting.Common.Validators;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;

namespace BigBang.App.Cloud.ERP.Accounting.Application.PersonAccounts.Payloads
{
    public record UpdatePersonAccountRequest(string FirstName, string LastName, string MobileNumber,
        AccountNature? InitialStatus, long Amount, IEnumerable<PersonRoleType> RoleTypes) : IRequest;
}
