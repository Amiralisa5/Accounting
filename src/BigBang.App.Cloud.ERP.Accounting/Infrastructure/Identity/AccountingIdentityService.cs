using System;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Common;
using BigBang.App.Cloud.ERP.Accounting.Common.Helpers;
using BigBang.App.Cloud.ERP.Accounting.Domain.FiscalPeriods;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Businesses;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.FiscalPeriods;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Owners;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using BigBang.Domain;
using BigBang.WebServer.Common.Attributes;
using BigBang.WebServer.Common.Services;
using BigBang.WebServer.Common.Services.Cache;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Identity
{
    [Service(ServiceType = typeof(IAccountingIdentityService), InstanceMode = InstanceMode.Scoped, Requestable = false)]
    internal class AccountingIdentityService : IAccountingIdentityService
    {
        private readonly IBigBangIdentityService _bigBangIdentityService;
        private readonly ICacheService _cacheService;
        private readonly IOwnerRepository _ownerRepository;
        private readonly IBusinessRepository _businessRepository;
        private readonly IBigBangActorIdentityEntityLoader _bangActorIdentityEntityLoader;
        private readonly IFiscalPeriodRepository _fiscalPeriodRepository;

        private Guid? _ownerId;
        private Guid? _businessId;
        private Guid? _fiscalPeriodId;

        public AccountingIdentityService(IBigBangIdentityService bigBangIdentityService,
            ICacheService cacheService,
            IOwnerRepository ownerRepository,
            IBusinessRepository businessRepository,
            IBigBangActorIdentityEntityLoader bangActorIdentityEntityLoader,
            IFiscalPeriodRepository fiscalPeriodRepository)
        {
            _bigBangIdentityService = bigBangIdentityService;
            _cacheService = cacheService;
            _ownerRepository = ownerRepository;
            _businessRepository = businessRepository;
            _bangActorIdentityEntityLoader = bangActorIdentityEntityLoader;
            _fiscalPeriodRepository = fiscalPeriodRepository;
        }

        public async Task<Guid> GetOwnerIdAsync()
        {
            if (_ownerId.HasValue)
                return _ownerId.Value;

            if (_bigBangIdentityService.Identity.Claims.TryGetValue(Constants.OwnerIdClaimType, out var ownerIdClaimValue))
            {
                _ownerId = Guid.Parse(ownerIdClaimValue);
            }
            else
            {
                var cacheKey = $"{Constants.OwnerIdClaimType}_{_bigBangIdentityService.Identity.Type}_{_bigBangIdentityService.Identity.Name}";
                ownerIdClaimValue = await _cacheService.GetAsync(cacheKey) as string;

                if (ownerIdClaimValue is null)
                {
                    var user = (BBUser)_bangActorIdentityEntityLoader.GetEntity(_bigBangIdentityService.Identity.Actor);
                    var owner = await _ownerRepository.GetByUserIdAsync(user.Id);
                    if (owner == null) throw ExceptionHelper.NotFound(Messages.Entity_Owner);

                    await _cacheService.SetAsync(cacheKey, owner.Id.ToString());
                    _ownerId = owner.Id;
                }
                else
                {
                    _ownerId = Guid.Parse(ownerIdClaimValue);
                }
            }

            return _ownerId.Value;
        }

        public async Task<Guid> GetBusinessIdAsync()
        {
            if (_businessId.HasValue)
                return _businessId.Value;

            if (_bigBangIdentityService.Identity.Claims.TryGetValue(Constants.BusinessIdClaimType, out var businessIdClaimValue))
            {
                _businessId = Guid.Parse(businessIdClaimValue);
            }
            else
            {
                var cacheKey = $"{Constants.BusinessIdClaimType}_{_bigBangIdentityService.Identity.Type}_{_bigBangIdentityService.Identity.Name}";
                businessIdClaimValue = await _cacheService.GetAsync(cacheKey) as string;

                if (businessIdClaimValue is null)
                {
                    var ownerId = await GetOwnerIdAsync();

                    var business = await _businessRepository.GetByOwnerIdAsync(ownerId);
                    if (business == null) throw ExceptionHelper.NotFound(Messages.Entity_Bussiness);

                    await _cacheService.SetAsync(cacheKey, business.Id.ToString());
                    _businessId = business.Id;
                }
                else
                {
                    _businessId = Guid.Parse(businessIdClaimValue);
                }
            }

            return _businessId.Value;
        }

        public async Task<Guid> GetFiscalPeriodIdAsync()
        {
            if (_fiscalPeriodId.HasValue)
                return _fiscalPeriodId.Value;

            if (_bigBangIdentityService.Identity.Claims.TryGetValue(Constants.FiscalPeriodIdClaimType, out var fiscalPeriodIdClaimValue))
            {
                _fiscalPeriodId = Guid.Parse(fiscalPeriodIdClaimValue);
            }
            else
            {
                var cacheKey = $"{Constants.FiscalPeriodIdClaimType}_{_bigBangIdentityService.Identity.Type}_{_bigBangIdentityService.Identity.Name}";
                fiscalPeriodIdClaimValue = await _cacheService.GetAsync(cacheKey) as string;

                if (fiscalPeriodIdClaimValue is null)
                {
                    var businessId = await GetBusinessIdAsync();

                    var fiscalPeriods = await _fiscalPeriodRepository.GetListByBusinessIdAsync(businessId);
                    var activeFiscalPeriod = fiscalPeriods.GetActiveFiscalPeriod();
                    if (activeFiscalPeriod == null) throw ExceptionHelper.NotFound(Messages.Entity_FiscalPeriod);

                    await _cacheService.SetAsync(cacheKey, activeFiscalPeriod.Id.ToString());
                    _fiscalPeriodId = activeFiscalPeriod.Id;
                }
                else
                {
                    _fiscalPeriodId = Guid.Parse(fiscalPeriodIdClaimValue);
                }
            }

            return _fiscalPeriodId.Value;
        }
    }
}
