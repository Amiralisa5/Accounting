using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.WebServer.Common.Attributes;
using BigBang.WebServer.Common.Services;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.VoucherTemplates
{
    [Service(ServiceType = typeof(IVoucherTemplateRepository), InstanceMode = InstanceMode.Scoped, Requestable = false)]
    internal class VoucherTemplateRepository : BaseRepository<ACC_VoucherTemplate, byte>, IVoucherTemplateRepository
    {
        public VoucherTemplateRepository(ISessionLoader sessionLoader) : base(sessionLoader)
        {
        }
    }
}