using System;
using System.Collections.Generic;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;

namespace BigBang.App.Cloud.ERP.Accounting.Application.PersonAccounts.Payloads
{
    public record PersonAccountResponse(Guid Id, string FirstName, string LastName, string MobileNumber, IList<PersonRoleType> RoleTypes);
}