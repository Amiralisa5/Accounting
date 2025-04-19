using System.Collections.Generic;
using BigBang.Metadata.Models.Enums;
using BigBang.WebServer.Common.Attributes;
using BigBang.WebServer.Common.Services.Metadata;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.MetadataEnum
{
    [Service(ServiceType = typeof(IMetadataEnumService), InstanceMode = InstanceMode.Single, Requestable = false)]
    internal class MetadataEnumService : IMetadataEnumService
    {
        private readonly string _applicationName = "Cloud_ERP_Accounting";
        private readonly IMetadataService _metadataService;
        private IEnumerable<EnumInfo> _enumInfos;

        public MetadataEnumService(IMetadataService metadataService)
        {
            _metadataService = metadataService;
        }

        public IEnumerable<EnumInfo> GetDmlEnums()
        {
            _enumInfos ??= _metadataService.GetApplication(_applicationName).Enums;
            return _enumInfos;
        }
    }
}