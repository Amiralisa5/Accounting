using System.Collections.Generic;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Enums.Payloads
{
    public record EnumDataModel(string EnumName, string DisplayName, List<EnumMemberDataModel> EnumMembers);
}
