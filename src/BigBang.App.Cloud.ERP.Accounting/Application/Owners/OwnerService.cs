using System;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.Owners.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Application.Owners.Payloads.Validators;
using BigBang.App.Cloud.ERP.Accounting.Common.Helpers;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.App.Cloud.ERP.Accounting.Domain.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.App.Cloud.ERP.Accounting.Domain.FiscalPeriods;
using BigBang.App.Cloud.ERP.Accounting.Domain.PersonAccounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Businesses;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.FiscalPeriods;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Owners;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.PersonAccounts;
using BigBang.WebServer.Common.Attributes;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Owners
{
    [Service(ServiceType = typeof(IOwnerService), InstanceMode = InstanceMode.Scoped, Requestable = false)]
    internal class OwnerService : IOwnerService
    {
        private readonly IOwnerRepository _ownerRepository;
        private readonly IBusinessRepository _businessRepository;
        private readonly IFiscalPeriodRepository _fiscalPeriodRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IPersonAccountRepository _personAccountRepository;

        public OwnerService(IOwnerRepository ownerRepository,
            IBusinessRepository businessRepository,
            IFiscalPeriodRepository fiscalPeriodRepository,
            IAccountRepository accountRepository,
            IPersonAccountRepository personAccountRepository)
        {
            _ownerRepository = ownerRepository;
            _businessRepository = businessRepository;
            _fiscalPeriodRepository = fiscalPeriodRepository;
            _accountRepository = accountRepository;
            _personAccountRepository = personAccountRepository;
        }

        public async Task CreateOwnerAsync(CreateOwnerRequest request)
        {
            var validator = new CreateOwnerValidator();
            var result = await validator.ValidateAsync(request);

            if (!result.IsValid) throw ExceptionHelper.BadRequest(result.Errors);

            var owner = await _ownerRepository.GetByUserIdAsync(request.UserId);
            if (owner == null)
            {
                owner = new ACC_Owner
                {
                    Id = Guid.NewGuid(),
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    MobileNumber = request.MobileNumber,
                    UserId = request.UserId
                };

                await _ownerRepository.AddAsync(owner);
            }

            var business = await _businessRepository.GetByOwnerIdAsync(owner.Id);

            if (business == null)
            {
                business = new ACC_Business
                {
                    Id = Guid.NewGuid(),
                    Name = request.BusinessName,
                    PodBusinessId = request.BusinessId,
                    Owner = owner
                };

                await _businessRepository.AddAsync(business);

                var defaultFiscalPeriod = FiscalPeriodFactory.CreateDefault(business);
                await _fiscalPeriodRepository.AddAsync(defaultFiscalPeriod);

                var defaultAccountTree = AccountTreeFactory.CreateDefault(defaultFiscalPeriod);
                await _accountRepository.SaveAllAsync(defaultAccountTree);
                var personAccount = PersonAccountFactory.Create(business.Id, request.FirstName, request.LastName, request.MobileNumber, [PersonRoleType.BusinessOwner]);

                await _personAccountRepository.AddAsync(personAccount);
            }
        }
    }
}