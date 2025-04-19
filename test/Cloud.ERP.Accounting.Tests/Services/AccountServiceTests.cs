using AutoFixture;
using BigBang.App.Cloud.ERP.Accounting.Application.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Application.Accounts.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Application.Enums;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.BankAccounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.FiscalPeriods;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.PersonAccounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Products;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers.Dtos;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Identity;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using BigBang.WebServer.Common.Exceptions;
using FluentAssertions;
using Moq;

namespace Cloud.ERP.Accounting.Tests.Services
{
    public class AccountServiceTests : BaseServiceTests
    {
        private readonly Mock<IVoucherRepository> _voucherRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<IBankAccountRepository> _bankAccountRepository;
        private readonly Mock<IPersonAccountRepository> _personAccountRepository;
        private readonly Mock<IProductRepository> _productRepository;
        private readonly AccountService _service;

        public AccountServiceTests() : base(new Mock<IAccountingIdentityService>(),
            new Mock<IFiscalPeriodRepository>(),
            new Mock<IEnumService>())
        {
            _voucherRepository = new Mock<IVoucherRepository>();
            _accountRepository = new Mock<IAccountRepository>();
            _bankAccountRepository = new Mock<IBankAccountRepository>();
            _personAccountRepository = new Mock<IPersonAccountRepository>();
            _productRepository = new Mock<IProductRepository>();

            _service = new AccountService(AccountingIdentityService.Object, _accountRepository.Object,
                _voucherRepository.Object, _bankAccountRepository.Object, _productRepository.Object,
                _personAccountRepository.Object, EnumService.Object);
        }

        [Fact]
        public async Task GetSubsidiaryAccountFinancialBalanceAsync_WhenFromDateIsBiggerThanToDate_ShouldThrowException()
        {
            // Arrange
            var request = Fixture.Build<SubsidiaryAccountFinancialBalanceRequest>()
                .With(request => request.FromDate, DateTime.Today.AddYears(1))
                .With(request => request.ToDate, DateTime.Today)
                .Create();

            var fiscalPeriodId = Guid.NewGuid();

            AccountingIdentityService.Setup(service => service.GetFiscalPeriodIdAsync())
                .ReturnsAsync(fiscalPeriodId);

            // Act
            var result = async () => await _service.GetSubsidiaryAccountFinancialBalanceAsync(request);

            // Assert
            await result.Should().ThrowAsync<BigBangException>().WithMessage(Messages.Error_FromDateToDateIsNotValid);

            AccountingIdentityService.Verify(service => service.GetFiscalPeriodIdAsync(), Times.Never);
            _accountRepository.Verify(repository => repository.GetListByFiscalPeriodIdAsync(fiscalPeriodId), Times.Never);
            _voucherRepository.Verify(repository => repository.GetSubsidiaryAccountFinancialBalanceDataAsync(fiscalPeriodId, request.FromDate, request.ToDate), Times.Never);
        }

        [Fact]
        public async Task GetSubsidiaryAccountFinancialBalanceAsync_WhenRequestIsValid_ShouldReturnExpectedResults()
        {
            // Arrange
            var request = Fixture.Build<SubsidiaryAccountFinancialBalanceRequest>()
                .With(request => request.FromDate, DateTime.Today)
                .With(request => request.ToDate, DateTime.Today.AddYears(1))
                .Create();

            var fiscalPeriodId = Guid.NewGuid();

            AccountingIdentityService.Setup(service => service.GetFiscalPeriodIdAsync())
                .ReturnsAsync(fiscalPeriodId);

            var assets = Fixture.Build<ACC_Account>()
                .With(account => account.Level, 1)
                .Create();

            var bank = Fixture.Build<ACC_Account>()
                .With(account => account.Level, 2)
                .With(account => account.ParentAccount, assets)
                .Create();

            var fund = Fixture.Build<ACC_Account>()
                .With(account => account.Level, 2)
                .With(account => account.ParentAccount, assets)
                .Create();

            _accountRepository.Setup(repository => repository.GetListByFiscalPeriodIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync([assets, bank, fund]);

            var bankBalanceSheetData = Fixture.Build<SubsidiaryAccountBalanceSheetDto>()
                .With(bankData => bankData.Id, bank.Id)
                .Create();

            _voucherRepository.Setup(repository =>
                    repository.GetSubsidiaryAccountFinancialBalanceDataAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync([bankBalanceSheetData]);

            // Act
            var result = await _service.GetSubsidiaryAccountFinancialBalanceAsync(request);

            // Assert
            AccountingIdentityService.Verify(service => service.GetFiscalPeriodIdAsync(), Times.Once);
            _accountRepository.Verify(repository => repository.GetListByFiscalPeriodIdAsync(fiscalPeriodId), Times.Once);
            _voucherRepository.Verify(repository => repository.GetSubsidiaryAccountFinancialBalanceDataAsync(fiscalPeriodId, request.FromDate, request.ToDate), Times.Once);

            var subsidiaryAccountBalanceSheet = result.First();
            subsidiaryAccountBalanceSheet.Id.Should().Be(assets.Id);
            subsidiaryAccountBalanceSheet.Code.Should().Be(assets.Code);
            subsidiaryAccountBalanceSheet.Name.Should().Be(assets.Name);
            subsidiaryAccountBalanceSheet.DisplayName.Should().Be(assets.DisplayName);
            subsidiaryAccountBalanceSheet.Nature.Should().Be(assets.Nature);

            var bankBalanceSheet = subsidiaryAccountBalanceSheet.SubsidiaryAccountResponses.Single(subsidiaryAccount => subsidiaryAccount.Id == bank.Id);
            bankBalanceSheet.Id.Should().Be(bank.Id);
            bankBalanceSheet.Code.Should().Be(bank.Code);
            bankBalanceSheet.Name.Should().Be(bank.Name);
            bankBalanceSheet.DisplayName.Should().Be(bank.DisplayName);
            bankBalanceSheet.Nature.Should().Be(bank.Nature);
            bankBalanceSheet.TotalDebit.Should().Be(bankBalanceSheetData.TotalDebit);
            bankBalanceSheet.TotalCredit.Should().Be(bankBalanceSheetData.TotalCredit);

            var fundBalanceSheet = subsidiaryAccountBalanceSheet.SubsidiaryAccountResponses.Single(subsidiaryAccount => subsidiaryAccount.Id == fund.Id);
            fundBalanceSheet.Id.Should().Be(fund.Id);
            fundBalanceSheet.Code.Should().Be(fund.Code);
            fundBalanceSheet.Name.Should().Be(fund.Name);
            fundBalanceSheet.DisplayName.Should().Be(fund.DisplayName);
            fundBalanceSheet.Nature.Should().Be(fund.Nature);
            fundBalanceSheet.TotalDebit.Should().Be(0);
            fundBalanceSheet.TotalCredit.Should().Be(0);
        }

        [Fact]
        public async Task GetDetailedAccountFinancialBalanceAsync_WhenSortByValueIsNotValid_ShouldThrowException()
        {
            // Arrange
            var subsidiaryAccountId = Guid.NewGuid();

            var request = Fixture.Build<DetailedAccountFinancialBalanceRequest>()
                .With(request => request.Id, subsidiaryAccountId)
                .With(request => request.FromDate, DateTime.Today)
                .With(request => request.ToDate, DateTime.Today.AddYears(1))
                .With(request => request.SortDirection, SortDirection.Desc)
                .Create();

            var businessId = Guid.NewGuid();
            AccountingIdentityService.Setup(service => service.GetBusinessIdAsync())
                .ReturnsAsync(businessId);

            var fiscalPeriodId = Guid.NewGuid();
            AccountingIdentityService.Setup(service => service.GetFiscalPeriodIdAsync())
                .ReturnsAsync(fiscalPeriodId);

            // Act
            var result = async () => await _service.GetDetailedAccountFinancialBalanceAsync(request);

            // Assert
            await result.Should().ThrowAsync<BigBangException>()
                .WithMessage(Messages.Error_SortByHaveToBeTotalDebit_TotalCredit_Difference);

            AccountingIdentityService.Verify(service => service.GetFiscalPeriodIdAsync(), Times.Never);
            _accountRepository.Verify(repository => repository.GetAsync(request.Id), Times.Never);
        }

        [Fact]
        public async Task GetDetailedAccountFinancialBalanceAsync_WhenFromDateIsBiggerThanToDate_ShouldThrowException()
        {
            // Arrange
            var subsidiaryAccountId = Guid.NewGuid();

            var request = Fixture.Build<DetailedAccountFinancialBalanceRequest>()
                .With(request => request.Id, subsidiaryAccountId)
                .With(request => request.FromDate, DateTime.Today.AddYears(1))
                .With(request => request.ToDate, DateTime.Today)
                .With(request => request.SortBy, "TotalCredit")
                .With(request => request.SortDirection, SortDirection.Desc)
                .Create();

            var fiscalPeriodId = Guid.NewGuid();

            AccountingIdentityService.Setup(service => service.GetFiscalPeriodIdAsync())
                .ReturnsAsync(fiscalPeriodId);

            // Act
            var result = async () => await _service.GetDetailedAccountFinancialBalanceAsync(request);

            // Assert
            await result.Should().ThrowAsync<BigBangException>()
                .WithMessage(Messages.Error_FromDateToDateIsNotValid);

            AccountingIdentityService.Verify(service => service.GetFiscalPeriodIdAsync(), Times.Never);
            _accountRepository.Verify(repository => repository.GetAsync(request.Id), Times.Never);
        }

        [Fact]
        public async Task GetDetailedAccountFinancialBalanceAsync_WhenSortDirectionIsNotValid_ShouldThrowException()
        {
            // Arrange
            var subsidiaryAccountId = Guid.NewGuid();

            var request = Fixture.Build<DetailedAccountFinancialBalanceRequest>()
                .With(request => request.Id, subsidiaryAccountId)
                .With(request => request.FromDate, DateTime.Today)
                .With(request => request.ToDate, DateTime.Today.AddYears(1))
                .With(request => request.SortBy, "TotalCredit")
                .With(request => request.SortDirection, (SortDirection)3)
                .Create();

            var fiscalPeriodId = Guid.NewGuid();

            AccountingIdentityService.Setup(service => service.GetFiscalPeriodIdAsync())
                .ReturnsAsync(fiscalPeriodId);

            // Act
            var result = async () => await _service.GetDetailedAccountFinancialBalanceAsync(request);

            // Assert
            await result.Should().ThrowAsync<BigBangException>()
                .WithMessage(Messages.Error_SortDirectionHaveToBeDescendingOrAscending);

            AccountingIdentityService.Verify(service => service.GetFiscalPeriodIdAsync(), Times.Never);
            _accountRepository.Verify(repository => repository.GetAsync(request.Id), Times.Never);
        }

        [Fact]
        public async Task GetDetailedAccountFinancialBalanceAsync_WhenLookupTypeOfSubsidiaryAccountIsBank_ShouldReturnExpectedResults()
        {
            // Arrange
            var subsidiaryAccountId = Guid.NewGuid();

            var request = Fixture.Build<DetailedAccountFinancialBalanceRequest>()
                .With(request => request.Id, subsidiaryAccountId)
                .With(request => request.FromDate, DateTime.Today)
                .With(request => request.ToDate, DateTime.Today.AddYears(1))
                .With(request => request.SortBy, "TotalDebit")
                .With(request => request.SortDirection, SortDirection.Desc)
                .Create();

            var businessId = Guid.NewGuid();
            AccountingIdentityService.Setup(service => service.GetBusinessIdAsync())
                .ReturnsAsync(businessId);

            var fiscalPeriodId = Guid.NewGuid();
            AccountingIdentityService.Setup(service => service.GetFiscalPeriodIdAsync())
                .ReturnsAsync(fiscalPeriodId);

            var account = Fixture.Build<ACC_Account>()
                .With(account => account.Id, subsidiaryAccountId)
                .With(account => account.LookupType, LookupType.Bank)
                .Create();

            _accountRepository.Setup(repository => repository.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(account);

            var bankFinancialBalanceData1 = Fixture.Build<DetailedAccountFinancialBalanceDto>()
                .Create();

            var bankFinancialBalanceData2 = Fixture.Build<DetailedAccountFinancialBalanceDto>()
                .With(bank => bank.TotalCredit, 0)
                .With(bank => bank.TotalDebit, 0)
                .With(bank => bank.Difference, 0)
                .Create();

            _voucherRepository.Setup(repository => repository.GetDetailedAccountFinancialBalanceDataAsync(It.IsAny<DetailedAccountFinancialBalanceRequest>(),
                    It.IsAny<Guid>(), It.IsAny<LookupType>()))
                .ReturnsAsync([bankFinancialBalanceData1, bankFinancialBalanceData2]);

            // Act
            var result = await _service.GetDetailedAccountFinancialBalanceAsync(request);

            // Assert
            AccountingIdentityService.Verify(service => service.GetFiscalPeriodIdAsync(), Times.Once);
            AccountingIdentityService.Verify(service => service.GetFiscalPeriodIdAsync(), Times.Once);
            EnumService.Verify(service => service.GetAsync(nameof(Bank)), Times.Once);
            _bankAccountRepository.Verify(repository => repository.GetListByBusinessIdAsync(businessId), Times.Once);
            _accountRepository.Verify(repository => repository.GetAsync(request.Id), Times.Once);
            _voucherRepository.Verify(repository => repository.GetDetailedAccountFinancialBalanceDataAsync(request, fiscalPeriodId, account.LookupType.GetValueOrDefault()), Times.Once);

            result.Header.Id.Should().Be(account.Id);
            result.Header.Code.Should().Be(account.Code);
            result.Header.Name.Should().Be(account.Name);
            result.Header.DisplayName.Should().Be(account.DisplayName);
            result.Header.Nature.Should().Be(account.Nature);

            var bankAccountFinancialBalance1 = result.Details.Single(detailedAccount => detailedAccount.Id == bankFinancialBalanceData1.Id);
            bankAccountFinancialBalance1.Name.Should().Be(bankFinancialBalanceData1.Name);
            bankAccountFinancialBalance1.TotalDebit.Should().Be(bankFinancialBalanceData1.TotalDebit);
            bankAccountFinancialBalance1.TotalCredit.Should().Be(bankFinancialBalanceData1.TotalCredit);
            bankAccountFinancialBalance1.Difference.Should().Be(bankFinancialBalanceData1.Difference);

            var bankAccountFinancialBalance2 = result.Details.Single(detailedAccount => detailedAccount.Id == bankFinancialBalanceData2.Id);
            bankAccountFinancialBalance2.Name.Should().Be(bankFinancialBalanceData2.Name);
            bankAccountFinancialBalance2.TotalDebit.Should().Be(0);
            bankAccountFinancialBalance2.TotalDebit.Should().Be(0);
            bankAccountFinancialBalance2.Difference.Should().Be(0);
        }

        [Fact]
        public async Task GetDetailedAccountFinancialBalanceAsync_WhenLookupTypeOfSubsidiaryAccountIsPerson_ShouldReturnExpectedResults()
        {
            // Arrange
            var subsidiaryAccountId = Guid.NewGuid();

            var request = Fixture.Build<DetailedAccountFinancialBalanceRequest>()
                .With(request => request.Id, subsidiaryAccountId)
                .With(request => request.FromDate, DateTime.Today)
                .With(request => request.ToDate, DateTime.Today.AddYears(1))
                .With(request => request.SortBy, "TotalDebit")
                .With(request => request.SortDirection, SortDirection.Desc)
                .Create();

            var businessId = Guid.NewGuid();
            AccountingIdentityService.Setup(service => service.GetBusinessIdAsync())
                .ReturnsAsync(businessId);

            var fiscalPeriodId = Guid.NewGuid();
            AccountingIdentityService.Setup(service => service.GetFiscalPeriodIdAsync())
                .ReturnsAsync(fiscalPeriodId);

            var account = Fixture.Build<ACC_Account>()
                .With(account => account.Id, subsidiaryAccountId)
                .With(account => account.LookupType, LookupType.Person)
                .Create();

            _accountRepository.Setup(repository => repository.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(account);

            var personFinancialBalanceData1 = Fixture.Build<DetailedAccountFinancialBalanceDto>()
                .Create();

            var personFinancialBalanceData2 = Fixture.Build<DetailedAccountFinancialBalanceDto>()
                .With(person => person.TotalCredit, 0)
                .With(person => person.TotalDebit, 0)
                .With(person => person.Difference, 0)
                .Create();

            _voucherRepository.Setup(repository => repository.GetDetailedAccountFinancialBalanceDataAsync(It.IsAny<DetailedAccountFinancialBalanceRequest>(),
                    It.IsAny<Guid>(), It.IsAny<LookupType>()))
                .ReturnsAsync([personFinancialBalanceData1, personFinancialBalanceData2]);

            // Act
            var result = await _service.GetDetailedAccountFinancialBalanceAsync(request);

            // Assert
            AccountingIdentityService.Verify(service => service.GetFiscalPeriodIdAsync(), Times.Once);
            AccountingIdentityService.Verify(service => service.GetFiscalPeriodIdAsync(), Times.Once);
            _personAccountRepository.Verify(repository => repository.GetListByBusinessIdAsync(businessId), Times.Once);
            _accountRepository.Verify(repository => repository.GetAsync(request.Id), Times.Once);
            _voucherRepository.Verify(repository => repository.GetDetailedAccountFinancialBalanceDataAsync(request, fiscalPeriodId, account.LookupType.GetValueOrDefault()), Times.Once);

            result.Header.Id.Should().Be(account.Id);
            result.Header.Code.Should().Be(account.Code);
            result.Header.Name.Should().Be(account.Name);
            result.Header.DisplayName.Should().Be(account.DisplayName);
            result.Header.Nature.Should().Be(account.Nature);

            var personAccountFinancialBalance1 = result.Details.Single(detailedAccount => detailedAccount.Id == personFinancialBalanceData1.Id);
            personAccountFinancialBalance1.Name.Should().Be(personFinancialBalanceData1.Name);
            personAccountFinancialBalance1.TotalDebit.Should().Be(personFinancialBalanceData1.TotalDebit);
            personAccountFinancialBalance1.TotalCredit.Should().Be(personFinancialBalanceData1.TotalCredit);
            personAccountFinancialBalance1.Difference.Should().Be(personFinancialBalanceData1.Difference);

            var personAccountFinancialBalance2 = result.Details.Single(detailedAccount => detailedAccount.Id == personFinancialBalanceData2.Id);
            personAccountFinancialBalance2.Name.Should().Be(personFinancialBalanceData2.Name);
            personAccountFinancialBalance2.TotalDebit.Should().Be(0);
            personAccountFinancialBalance2.TotalDebit.Should().Be(0);
            personAccountFinancialBalance2.Difference.Should().Be(0);
        }

        [Fact]
        public async Task GetDetailedAccountFinancialBalanceAsync_WhenLookupTypeOfSubsidiaryAccountIsProduct_ShouldReturnExpectedResults()
        {
            // Arrange
            var subsidiaryAccountId = Guid.NewGuid();

            var request = Fixture.Build<DetailedAccountFinancialBalanceRequest>()
                .With(request => request.Id, subsidiaryAccountId)
                .With(request => request.FromDate, DateTime.Today)
                .With(request => request.ToDate, DateTime.Today.AddYears(1))
                .With(request => request.SortBy, "TotalDebit")
                .With(request => request.SortDirection, SortDirection.Desc)
                .Create();

            var businessId = Guid.NewGuid();
            AccountingIdentityService.Setup(service => service.GetBusinessIdAsync())
                .ReturnsAsync(businessId);

            var fiscalPeriodId = Guid.NewGuid();
            AccountingIdentityService.Setup(service => service.GetFiscalPeriodIdAsync())
                .ReturnsAsync(fiscalPeriodId);

            var account = Fixture.Build<ACC_Account>()
                .With(account => account.Id, subsidiaryAccountId)
                .With(account => account.LookupType, LookupType.Product)
                .Create();

            _accountRepository.Setup(repository => repository.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(account);

            var productFinancialBalanceData1 = Fixture.Build<DetailedAccountFinancialBalanceDto>()
                .Create();

            var productFinancialBalanceData2 = Fixture.Build<DetailedAccountFinancialBalanceDto>()
                .With(product => product.TotalCredit, 0)
                .With(product => product.TotalDebit, 0)
                .With(product => product.Difference, 0)
                .Create();

            _voucherRepository.Setup(repository => repository.GetDetailedAccountFinancialBalanceDataAsync(It.IsAny<DetailedAccountFinancialBalanceRequest>(),
                    It.IsAny<Guid>(), It.IsAny<LookupType>()))
                .ReturnsAsync([productFinancialBalanceData1, productFinancialBalanceData2]);

            // Act
            var result = await _service.GetDetailedAccountFinancialBalanceAsync(request);

            // Assert
            AccountingIdentityService.Verify(service => service.GetFiscalPeriodIdAsync(), Times.Once);
            AccountingIdentityService.Verify(service => service.GetFiscalPeriodIdAsync(), Times.Once);
            _productRepository.Verify(repository => repository.GetListByBusinessIdAsync(businessId), Times.Once);
            _accountRepository.Verify(repository => repository.GetAsync(request.Id), Times.Once);
            _voucherRepository.Verify(repository => repository.GetDetailedAccountFinancialBalanceDataAsync(request, fiscalPeriodId, account.LookupType.GetValueOrDefault()), Times.Once);

            result.Header.Id.Should().Be(account.Id);
            result.Header.Code.Should().Be(account.Code);
            result.Header.Name.Should().Be(account.Name);
            result.Header.DisplayName.Should().Be(account.DisplayName);
            result.Header.Nature.Should().Be(account.Nature);

            var productFinancialBalance1 = result.Details.Single(detailedAccount => detailedAccount.Id == productFinancialBalanceData1.Id);
            productFinancialBalance1.Name.Should().Be(productFinancialBalanceData1.Name);
            productFinancialBalance1.TotalDebit.Should().Be(productFinancialBalanceData1.TotalDebit);
            productFinancialBalance1.TotalCredit.Should().Be(productFinancialBalanceData1.TotalCredit);
            productFinancialBalance1.Difference.Should().Be(productFinancialBalanceData1.Difference);

            var productFinancialBalance2 = result.Details.Single(detailedAccount => detailedAccount.Id == productFinancialBalanceData2.Id);
            productFinancialBalance2.Name.Should().Be(productFinancialBalanceData2.Name);
            productFinancialBalance2.TotalDebit.Should().Be(0);
            productFinancialBalance2.TotalDebit.Should().Be(0);
            productFinancialBalance2.Difference.Should().Be(0);
        }

        [Fact]
        public async Task GetDetailedAccountFinancialBalanceAsync_WhenLookupTypeOfSubsidiaryAccountIsNull_DetailedAccountResponsesShouldBeEmpty()
        {
            // Arrange
            var subsidiaryAccountId = Guid.NewGuid();

            var request = Fixture.Build<DetailedAccountFinancialBalanceRequest>()
                .With(request => request.Id, subsidiaryAccountId)
                .With(request => request.FromDate, DateTime.Today)
                .With(request => request.ToDate, DateTime.Today.AddYears(1))
                .With(request => request.SortBy, "TotalDebit")
                .With(request => request.SortDirection, SortDirection.Desc)
                .Create();

            var businessId = Guid.NewGuid();
            AccountingIdentityService.Setup(service => service.GetBusinessIdAsync())
                .ReturnsAsync(businessId);

            var fiscalPeriodId = Guid.NewGuid();
            AccountingIdentityService.Setup(service => service.GetFiscalPeriodIdAsync())
                .ReturnsAsync(fiscalPeriodId);

            var account = Fixture.Build<ACC_Account>()
                .With(account => account.Id, subsidiaryAccountId)
                .With(account => account.LookupType, default(LookupType?))
                .Create();

            _accountRepository.Setup(repository => repository.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(account);

            _voucherRepository.Setup(repository => repository.GetDetailedAccountFinancialBalanceDataAsync(It.IsAny<DetailedAccountFinancialBalanceRequest>(),
                    It.IsAny<Guid>(), It.IsAny<LookupType>()))
                .ReturnsAsync([]);

            // Act
            var result = await _service.GetDetailedAccountFinancialBalanceAsync(request);

            // Assert
            AccountingIdentityService.Verify(service => service.GetFiscalPeriodIdAsync(), Times.Once);
            AccountingIdentityService.Verify(service => service.GetFiscalPeriodIdAsync(), Times.Once);
            _accountRepository.Verify(repository => repository.GetAsync(request.Id), Times.Once);
            _voucherRepository.Verify(repository => repository.GetDetailedAccountFinancialBalanceDataAsync(request, fiscalPeriodId, account.LookupType.GetValueOrDefault()), Times.Once);

            result.Header.Id.Should().Be(account.Id);
            result.Header.Code.Should().Be(account.Code);
            result.Header.Name.Should().Be(account.Name);
            result.Header.DisplayName.Should().Be(account.DisplayName);
            result.Header.Nature.Should().Be(account.Nature);
            result.Details.Should().BeEmpty();
        }
    }
}
