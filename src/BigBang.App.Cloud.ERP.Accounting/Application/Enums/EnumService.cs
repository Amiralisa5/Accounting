using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.Enums.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Common;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.VoucherTemplates;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.MetadataEnum;
using BigBang.Metadata.Models.Enums;
using BigBang.WebServer.Common.Attributes;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Enums
{
    [Service(ServiceType = typeof(IEnumService), InstanceMode = InstanceMode.Scoped, Requestable = false)]
    internal class EnumService : IEnumService
    {
        private readonly IVoucherTemplateRepository _voucherTemplateRepository;
        private readonly IEnumerable<EnumInfo> _enums;

        public EnumService(IMetadataEnumService metadataEnumService, IVoucherTemplateRepository voucherTemplateRepository)
        {
            _voucherTemplateRepository = voucherTemplateRepository;
            _enums = metadataEnumService.GetDmlEnums();
        }

        public async Task<List<EnumDataModel>> GetListAsync()
        {
            var result = new List<EnumDataModel>();

            foreach (var enumInfo in _enums)
            {
                var memberInfo = new List<EnumMemberDataModel>();
                foreach (var member in enumInfo.Members)
                {
                    string displayName = member.DisplayName;
                    memberInfo.Add(new EnumMemberDataModel(member.Name, displayName, member.Value));
                }
                result.Add(new EnumDataModel(enumInfo.Name, enumInfo.DisplayName, memberInfo));
            }

            //#TODO its better using inheritance feature and reflection 
            var voucherTemplates = await _voucherTemplateRepository.GetAllAsync();

            if (voucherTemplates.Any())
            {
                var memberInfo = voucherTemplates.Select(template => new EnumMemberDataModel(template.Name, template.DisplayName, template.Id))
                    .ToList();
                result.Add(new EnumDataModel(Constants.VoucherTemplateEnumName, null, memberInfo));
            }

            return result;
        }

        public Task<EnumDataModel> GetAsync(string enumName)
        {
            return Task.Run(() =>
            {
                var enumInfo = _enums.FirstOrDefault(enumInfo => enumInfo.Name.Equals(enumName, StringComparison.InvariantCultureIgnoreCase));

                if (enumInfo is null)
                    return null;

                var membersInfo = enumInfo.Members.Select(member => new EnumMemberDataModel(member.Name, member.DisplayName, member.Value)).ToList();

                return new EnumDataModel(enumInfo.Name, enumInfo.DisplayName, membersInfo);
            });
        }
    }
}