using System;
using System.Collections.Generic;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Accounts.Payloads
{
    public record AccountTreeResponse(Guid AccountId, string DisplayName, int Level, string Code,
        AccountNature? Nature, string Name, LookupType? LookupType, bool? IsPermanent, PersonRoleType? DefaultPersonRole,
        IEnumerable<AccountTreeResponse> ChildrenAccounts);
}
