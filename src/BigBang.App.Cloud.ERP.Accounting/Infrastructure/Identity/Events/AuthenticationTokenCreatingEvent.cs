using System.Threading;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Common;
using BigBang.App.Cloud.ERP.Accounting.Common.Helpers;
using BigBang.App.Cloud.ERP.Accounting.Domain.FiscalPeriods;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Businesses;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.FiscalPeriods;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Owners;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using BigBang.WebServer.Common.Attributes;
using BigBang.WebServer.Common.Events;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Identity.Events
{
    [EventSubscription(typeof(IAuthenticationTokenCreatingEvent))]
    public class AuthenticationTokenCreatingEvent : IAuthenticationTokenCreatingEvent
    {
        private readonly IOwnerRepository _ownerRepository;
        private readonly IBusinessRepository _businessRepository;
        private readonly IFiscalPeriodRepository _fiscalPeriodRepository;

        public AuthenticationTokenCreatingEvent(IOwnerRepository ownerRepository, IBusinessRepository businessRepository, IFiscalPeriodRepository fiscalPeriodRepository)
        {
            _ownerRepository = ownerRepository;
            _businessRepository = businessRepository;
            _fiscalPeriodRepository = fiscalPeriodRepository;
        }

        public async Task OnCreating(IAuthenticationTokenCreatingEventContext context, CancellationToken cancellationToken)
        {
            var owner = await _ownerRepository.GetByUserIdAsync(context.UserId);
            if (owner == null) throw ExceptionHelper.NotFound(Messages.Entity_Owner);

            context.AddCustomClaim(Constants.OwnerIdClaimType, owner.Id.ToString());

            var business = await _businessRepository.GetByOwnerIdAsync(owner.Id);
            if (business == null) throw ExceptionHelper.NotFound(Messages.Entity_Bussiness);

            context.AddCustomClaim(Constants.BusinessIdClaimType, business.Id.ToString());

            var fiscalPeriods = await _fiscalPeriodRepository.GetListByBusinessIdAsync(business.Id);
            var activeFiscalPeriod = fiscalPeriods.GetActiveFiscalPeriod();
            if (activeFiscalPeriod == null) throw ExceptionHelper.NotFound(Messages.Entity_FiscalPeriod);

            context.AddCustomClaim(Constants.FiscalPeriodIdClaimType, activeFiscalPeriod.Id.ToString());
        }
    }
}
