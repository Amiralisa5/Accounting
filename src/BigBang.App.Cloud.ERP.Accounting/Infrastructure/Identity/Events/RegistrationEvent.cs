using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.Owners;
using BigBang.App.Cloud.ERP.Accounting.Application.Owners.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Common;
using BigBang.WebServer.Common.Attributes;
using BigBang.WebServer.Common.Events;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Identity.Events
{
    [EventSubscription(typeof(IRegistrationEvent))]
    public class RegistrationEvent : IRegistrationEvent
    {
        private readonly IOwnerService _ownerService;

        public RegistrationEvent(IOwnerService ownerService)
        {
            _ownerService = ownerService;
        }

        public async Task UserRegistered(IRegistrationContext context)
        {
            var request = new CreateOwnerRequest(
                context.RegistrationInfo[Constants.UserInfoFirstName].Value,
                context.RegistrationInfo[Constants.UserInfoLastName].Value,
                context.RegistrationInfo[Constants.UserInfoBigBangPhone].Value,
                context.User.Id,
                context.RegistrationInfo[Constants.UserInfoBusinessName].Value,
                Constants.PodBusinessId
            );

            await _ownerService.CreateOwnerAsync(request);
        }
    }
}