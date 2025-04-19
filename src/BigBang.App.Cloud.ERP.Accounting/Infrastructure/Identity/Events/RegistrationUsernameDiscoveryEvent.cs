using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Common;
using BigBang.WebServer.Common.Attributes;
using BigBang.WebServer.Common.Events;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Identity.Events
{
    [EventSubscription(typeof(IRegistrationUsernameDiscoveryEvent))]
    public class RegistrationUsernameDiscoveryEvent : IRegistrationUsernameDiscoveryEvent
    {
        public RegistrationUsernameDiscoveryEvent()
        {
        }

        public async Task Discover(IRegistrationUsernameDiscoveryEventContext context)
        {
            await Task.Run(() =>
            {
                var mobileNumber = context.RegistrationInfo[Constants.UserInfoBigBangPhone].Value;
                context.SetUsername(mobileNumber);
            });
        }
    }
}