using System.Collections.Generic;
using BigBang.Metadata.Models.Enums;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.MetadataEnum
{
    public interface IMetadataEnumService
    {
        IEnumerable<EnumInfo> GetDmlEnums();
    }
}