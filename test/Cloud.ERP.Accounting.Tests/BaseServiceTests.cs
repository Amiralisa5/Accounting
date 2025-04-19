using AutoFixture;
using BigBang.App.Cloud.ERP.Accounting.Application.Enums;
using BigBang.App.Cloud.ERP.Accounting.Application.Enums.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.FiscalPeriods;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Identity;
using Moq;

namespace Cloud.ERP.Accounting.Tests
{
    public class BaseServiceTests
    {
        protected readonly Mock<IAccountingIdentityService> AccountingIdentityService;
        protected readonly Mock<IFiscalPeriodRepository> FiscalPeriodRepository;
        protected readonly Mock<IEnumService> EnumService;
        protected readonly Fixture Fixture;

        public BaseServiceTests(Mock<IAccountingIdentityService> accountingIdentityService,
            Mock<IFiscalPeriodRepository> fiscalPeriodRepository,
            Mock<IEnumService> enumService)
        {
            AccountingIdentityService = accountingIdentityService;
            FiscalPeriodRepository = fiscalPeriodRepository;
            EnumService = enumService;

            Fixture = new Fixture();
            Fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
                .ToList()
                .ForEach(behavior => Fixture.Behaviors.Remove(behavior));

            Fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        }

        protected Guid SetupFiscalPeriodId()
        {
            var fiscalPeriodId = Guid.NewGuid();

            AccountingIdentityService.Setup(service => service.GetFiscalPeriodIdAsync())
                .ReturnsAsync(fiscalPeriodId);

            return fiscalPeriodId;
        }

        protected void SetupFiscalPeriod(ACC_FiscalPeriod fiscalPeriod)
        {
            AccountingIdentityService.Setup(service => service.GetFiscalPeriodIdAsync())
                .ReturnsAsync(fiscalPeriod.Id);

            FiscalPeriodRepository.Setup(repository => repository.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(fiscalPeriod);
        }

        protected void SetupCurrency(EnumDataModel currencyEnum)
        {
            EnumService.Setup(service => service.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(currencyEnum);
        }

        protected void VerifyFiscalPeriod(Guid fiscalPeriodId)
        {
            AccountingIdentityService.Verify(service => service.GetFiscalPeriodIdAsync(), Times.Once);
            FiscalPeriodRepository.Verify(repository => repository.GetAsync(fiscalPeriodId), Times.Once);
        }
    }
}
