using AutoFixture;
using BigBang.App.Cloud.ERP.Accounting.Application.Enums;
using BigBang.App.Cloud.ERP.Accounting.Application.Owners;
using BigBang.App.Cloud.ERP.Accounting.Application.Owners.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Businesses;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.FiscalPeriods;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Owners;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.PersonAccounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Identity;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using BigBang.WebServer.Common.Exceptions;
using FluentAssertions;
using Moq;

namespace Cloud.ERP.Accounting.Tests.Services
{
    public class OwnerServiceTests : BaseServiceTests
    {
        private readonly Mock<IOwnerRepository> _ownerRepository;
        private readonly Mock<IBusinessRepository> _businessRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<IPersonAccountRepository> _personAccountRepository;
        private readonly OwnerService _service;

        public OwnerServiceTests() : base(new Mock<IAccountingIdentityService>(),
            new Mock<IFiscalPeriodRepository>(),
            new Mock<IEnumService>())
        {
            _ownerRepository = new Mock<IOwnerRepository>();
            _businessRepository = new Mock<IBusinessRepository>();
            _accountRepository = new Mock<IAccountRepository>();
            _personAccountRepository = new Mock<IPersonAccountRepository>();

            _service = new OwnerService(
                _ownerRepository.Object,
                _businessRepository.Object,
                FiscalPeriodRepository.Object,
                _accountRepository.Object,
                _personAccountRepository.Object);
        }

        [Fact]
        public async Task CreateOwner_NewBusiness_OwnerAndBusinessAndFiscalPeriodAndAccountTreeShouldBeCreated()
        {
            // Arrange
            var request = Fixture.Build<CreateOwnerRequest>()
                .With(request => request.MobileNumber, "09121234567")
                .Create();

            _ownerRepository.Setup(repository => repository.GetByUserIdAsync(It.IsAny<long>()))
                .ReturnsAsync(default(ACC_Owner));

            ACC_Owner? createdOwner = null;
            _ownerRepository.Setup(repository => repository.AddAsync(It.IsAny<ACC_Owner>()))
                .Callback<ACC_Owner>(owner => createdOwner = owner);

            _businessRepository.Setup(repository => repository.GetByOwnerIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(default(ACC_Business));

            // Act
            await _service.CreateOwnerAsync(request);

            // Assert
            _ownerRepository.Verify(repository => repository.GetByUserIdAsync(request.UserId), Times.Once);

            _ownerRepository.Verify(repository =>
                repository.AddAsync(It.Is<ACC_Owner>(owner => owner.FirstName == request.FirstName &&
                    owner.LastName == request.LastName &&
                    owner.MobileNumber == request.MobileNumber &&
                    owner.UserId == request.UserId)), Times.Once);

            _businessRepository.Verify(repository => repository.GetByOwnerIdAsync(createdOwner!.Id), Times.Once);

            _businessRepository.Verify(repository =>
                repository.AddAsync(It.Is<ACC_Business>(business => business.Name == request.BusinessName)), Times.Once);

            FiscalPeriodRepository.Verify(repository =>
                repository.AddAsync(It.Is<ACC_FiscalPeriod>(fiscalPeriod =>
                    fiscalPeriod.Status == FiscalPeriodStatus.Active)), Times.Once);

            _accountRepository.Verify(repository =>
                repository.SaveAllAsync(It.Is<IEnumerable<ACC_Account>>(accounts => accounts.Count() == 23)), Times.Once);
        }

        [Fact]
        public async Task CreateOwner_ExistingBusiness_OwnerAndBusinessAndFiscalPeriodAndAccountTreeShouldNotBeCreated()
        {
            // Arrange
            var request = Fixture.Build<CreateOwnerRequest>()
                .With(request => request.MobileNumber, "09121234567")
                .Create();

            var existingOwner = Fixture.Create<ACC_Owner>();

            _ownerRepository.Setup(repository => repository.GetByUserIdAsync(It.IsAny<long>()))
                .ReturnsAsync(existingOwner);

            var existingBusiness = Fixture.Build<ACC_Business>()
                .With(business => business.Owner, existingOwner)
                .Create();

            _businessRepository.Setup(repository => repository.GetByOwnerIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(existingBusiness);

            // Act
            await _service.CreateOwnerAsync(request);

            // Assert
            _ownerRepository.Verify(repository => repository.GetByUserIdAsync(request.UserId), Times.Once);
            _ownerRepository.Verify(repository => repository.AddAsync(It.IsAny<ACC_Owner>()), Times.Never);

            _businessRepository.Verify(repository => repository.GetByOwnerIdAsync(existingOwner.Id), Times.Once);
            _businessRepository.Verify(repository => repository.AddAsync(It.IsAny<ACC_Business>()), Times.Never);

            FiscalPeriodRepository.Verify(repository => repository.AddAsync(It.IsAny<ACC_FiscalPeriod>()), Times.Never);
            _accountRepository.Verify(repository => repository.SaveAllAsync(It.IsAny<IEnumerable<ACC_Account>>()), Times.Never);
        }

        [Fact]
        public async Task CreateOwner_InvalidOwnerMobileNumber_ValidationExceptionShouldBeThrown()
        {
            // Arrange
            var request = Fixture.Build<CreateOwnerRequest>()
                .With(request => request.MobileNumber, "091212345")
                .Create();

            // Act
            Func<Task> func = async () => await _service.CreateOwnerAsync(request);

            // Assert
            var exception = await func.Should().ThrowAsync<BigBangException>();
            exception.WithMessage(Messages.Error_MobileNumberShouldBeElevenDigits);

            _ownerRepository.Verify(repository => repository.GetByUserIdAsync(It.IsAny<long>()), Times.Never);
            _ownerRepository.Verify(repository => repository.AddAsync(It.IsAny<ACC_Owner>()), Times.Never);

            _businessRepository.Verify(repository => repository.GetByOwnerIdAsync(It.IsAny<Guid>()), Times.Never);
            _businessRepository.Verify(repository => repository.AddAsync(It.IsAny<ACC_Business>()), Times.Never);

            FiscalPeriodRepository.Verify(repository => repository.AddAsync(It.IsAny<ACC_FiscalPeriod>()), Times.Never);
            _accountRepository.Verify(repository => repository.SaveAllAsync(It.IsAny<IEnumerable<ACC_Account>>()), Times.Never);
        }
    }
}