using AutoFixture;
using BigBang.App.Cloud.ERP.Accounting.Application.BankAccounts;
using BigBang.App.Cloud.ERP.Accounting.Application.BankAccounts.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Application.Enums;
using BigBang.App.Cloud.ERP.Accounting.Application.Enums.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Application.PersonAccounts;
using BigBang.App.Cloud.ERP.Accounting.Application.PersonAccounts.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.App.Cloud.ERP.Accounting.Domain.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.BankAccounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.FiscalPeriods;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Identity;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using BigBang.WebServer.Common.Exceptions;
using FluentAssertions;
using Moq;

namespace Cloud.ERP.Accounting.Tests.Services
{
    public class BankAccountServiceTests : BaseServiceTests
    {
        private readonly Mock<IVoucherService> _voucherService;
        private readonly Mock<IPersonAccountService> _personAccountService;
        private readonly Mock<IBankAccountRepository> _bankAccountRepository;
        private readonly Mock<IVoucherRepository> _voucherRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly BankAccountService _service;

        public BankAccountServiceTests() : base(new Mock<IAccountingIdentityService>(),
            new Mock<IFiscalPeriodRepository>(),
            new Mock<IEnumService>())
        {
            _voucherService = new Mock<IVoucherService>();
            _bankAccountRepository = new Mock<IBankAccountRepository>();
            _personAccountService = new Mock<IPersonAccountService>();
            _voucherRepository = new Mock<IVoucherRepository>();
            _accountRepository = new Mock<IAccountRepository>();

            _service = new BankAccountService(AccountingIdentityService.Object,
                _voucherService.Object,
                _personAccountService.Object,
                _bankAccountRepository.Object,
                _voucherRepository.Object,
                _accountRepository.Object);
        }

        [Fact]
        public async Task GetBankAccount_ValidId_ShouldReturnBankAccount()
        {
            //Arrange
            var account = Fixture.Build<ACC_BankAccount>().Create();

            _bankAccountRepository.Setup(repo => repo.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(account);

            //Act
            var result = await _service.GetBankAccountAsync(account.Id);

            //Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(account.Id);
        }

        [Fact]
        public async Task AddBankAccount_InvalidData_MultipleValidationErrors()
        {
            // Arrange
            var request = Fixture.Build<BankAccountRequest>()
                .With(request => request.HolderName, string.Empty)
                .Create();

            // Act
            Func<Task> func = async () => await _service.AddBankAccountAsync(request);

            // Assert
            var exception = await func.Should().ThrowAsync<BigBangException>();
            var message = exception.Which.Message;
            message.Should().Contain(string.Format(Messages.Error_FieldRequired, Messages.Label_HolderName));
            message.Should().Contain(string.Format(Messages.Error_ExactLengthShouldBe, Messages.Label_CardNumber, 16));
        }

        [Fact]
        public async Task GetBankAccount_InvalidId_ShouldThrowException()
        {
            //Arrange
            _bankAccountRepository.Setup(repo => repo.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(default(ACC_BankAccount));

            //Act
            Func<Task> func = async () => await _service.GetBankAccountAsync(Guid.NewGuid());

            //Assert
            await func.Should().ThrowAsync<Exception>().WithMessage(string.Format(Messages.Error_EntityNotFound, Messages.Entity_BankAccount));
        }

        [Fact]
        public async Task GetBankAccounts_ExistingBusiness_BankAccountsShouldBeReturned()
        {
            // Arrange
            var businessId = Guid.NewGuid();

            var bankAccounts = Fixture.CreateMany<ACC_BankAccount>(3).ToList();

            AccountingIdentityService.Setup(service => service.GetBusinessIdAsync())
                .ReturnsAsync(businessId);

            _bankAccountRepository.Setup(repo => repo.GetListByBusinessIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(bankAccounts);

            // Act
            var result = await _service.GetBankAccountsAsync();

            // Assert
            result.Should().NotBeNull().And.HaveCount(3);

            AccountingIdentityService.Verify(service => service.GetBusinessIdAsync(), Times.Once);
            _bankAccountRepository.Verify(repo => repo.GetListByBusinessIdAsync(businessId), Times.Once);
        }

        [Fact]
        public async Task AddBankAccount_ValidRequest_BankAccountShouldBeAdded()
        {
            // Arrange
            var businessId = Guid.NewGuid();
            var fiscalPeriodId = Guid.NewGuid();

            var request = Fixture.Build<BankAccountRequest>()
                .With(request => request.ShebaNumber, "897854637253647293456789")
                .With(request => request.CardNumber, "6789065432156789")
                .With(request => request.Bank, Bank.Parsian)
                .Create();

            AccountingIdentityService.Setup(service => service.GetBusinessIdAsync())
                .ReturnsAsync(businessId);

            AccountingIdentityService.Setup(service => service.GetFiscalPeriodIdAsync())
                .ReturnsAsync(fiscalPeriodId);

            _voucherService.Setup(service => service.RegisterVoucherAsync(It.IsAny<RegisterVoucherRequest>()))
                .ReturnsAsync(Fixture.Create<VoucherResponse>());

            ACC_BankAccount? bankAccount = null;

            _bankAccountRepository.Setup(repo => repo.AddAsync(It.IsAny<ACC_BankAccount>()))
                .Callback<ACC_BankAccount>(addedBankAccount => bankAccount = addedBankAccount);

            var ownerPersonAccount = Fixture.Create<PersonAccountResponse>();

            _personAccountService.Setup(service => service.GetOwnerPersonAccountAsync())
                .ReturnsAsync(ownerPersonAccount);

            var bank = Fixture.Build<ACC_Account>()
                .With(account => account.Name, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Bank)))
                .Create();

            var subsidiaryEquity = Fixture.Build<ACC_Account>()
                .With(account => account.Name, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_SubsidiaryEquity)))
                .Create();

            _accountRepository.SetupSequence(account => account.GetByNameAndFiscalPeriodIdAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(bank)
                .ReturnsAsync(subsidiaryEquity);

            var enumMember = new List<EnumMemberDataModel> { new(Bank.Parsian.ToString(), "", (int)Bank.Parsian) };
            var enumDataModel = Fixture.Build<EnumDataModel>()
                .With(model => model.EnumMembers, enumMember)
                .With(model => model.EnumName, nameof(Bank))
                .Create();

            EnumService.Setup(service => service.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(enumDataModel);

            // Act
            var result = await _service.AddBankAccountAsync(request);

            // Assert
            result.Should().NotBeEmpty();

            AccountingIdentityService.Verify(service => service.GetBusinessIdAsync(), Times.Once);
            AccountingIdentityService.Verify(service => service.GetFiscalPeriodIdAsync(), Times.Once);

            _bankAccountRepository.Verify(repo =>
                repo.AddAsync(It.Is<ACC_BankAccount>(account =>
                    account.HolderName == request.HolderName &&
                    account.Title == request.Title &&
                    account.ShebaNumber == request.ShebaNumber &&
                    account.CardNumber == request.CardNumber &&
                    account.Bank == request.Bank)), Times.Once);

            bankAccount?.HolderName.Should().Be(request.HolderName);
            bankAccount?.Title.Should().Be(request.Title);
            bankAccount?.ShebaNumber.Should().Be(request.ShebaNumber);
            bankAccount?.CardNumber.Should().Be(request.CardNumber);
            bankAccount?.Business.Id.Should().Be(businessId);

            _personAccountService.Verify(service => service.GetOwnerPersonAccountAsync(), Times.Once);

            _voucherService.Verify(service => service.RegisterVoucherAsync(It.Is<RegisterVoucherRequest>(voucherRequest =>
                voucherRequest.Template == VoucherTemplate.Deposit &&
                voucherRequest.Articles.ElementAt(0).AccountId == bank.Id &&
                voucherRequest.Articles.ElementAt(0).LookupId == bankAccount!.Id &&
                voucherRequest.Articles.ElementAt(0).Amount == request.Balance &&
                voucherRequest.Articles.ElementAt(0).Currency == Currency.Rial &&
                voucherRequest.Articles.ElementAt(0).Type == ArticleType.Debit &&
                voucherRequest.Articles.ElementAt(1).AccountId == subsidiaryEquity.Id &&
                voucherRequest.Articles.ElementAt(1).LookupId == ownerPersonAccount.Id &&
                voucherRequest.Articles.ElementAt(1).Amount == request.Balance &&
                voucherRequest.Articles.ElementAt(1).Currency == Currency.Rial &&
                voucherRequest.Articles.ElementAt(1).Type == ArticleType.Credit)), Times.Once);
        }

        [Fact]
        public async Task UpdateBankAccount_AccountNotFound_ShouldThrowException()
        {
            //Arrange
            _bankAccountRepository.Setup(repo => repo.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(default(ACC_BankAccount));

            var request = Fixture.Build<BankAccountRequest>()
                .With(request => request.ShebaNumber, "123456789098765432123456")
                .With(request => request.CardNumber, "1234567890987654")
                .Create();

            //Act
            Func<Task> act = async () => await _service.UpdateBankAccountAsync(Guid.NewGuid(), request);

            //Assert
            await act.Should().ThrowAsync<Exception>().WithMessage(string.Format(Messages.Error_EntityNotFound, Messages.Entity_BankAccount));
        }

        [Fact]
        public async Task DeleteBankAccountA_ValidAccount_ShouldDeleteSuccessfully()
        {
            //Arrange
            var account = Fixture.Create<ACC_BankAccount>();
            _voucherRepository.Setup(repo => repo.ArticlesExistAsync(It.IsAny<LookupType>(), It.IsAny<Guid>()))
                .ReturnsAsync(false);
            _bankAccountRepository.Setup(repo => repo.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(account);

            //Act
            await _service.DeleteBankAccountAsync(account.Id);

            //Assert
            _bankAccountRepository.Verify(repo => repo.RemoveAsync(account.Id), Times.Once);
        }

        [Fact]
        public async Task UpdateBankAccount_ExistingBankAccount_ShouldUpdateSuccessfully()
        {
            // Arrange
            var existingAccount = Fixture.Create<ACC_BankAccount>();
            var request = Fixture.Build<BankAccountRequest>()
                .With(request => request.ShebaNumber, "330620000000202901868005")
                .With(request => request.CardNumber, "1234567890987654")
                .Create();

            _bankAccountRepository.Setup(repo => repo.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(existingAccount);

            // Act
            var result = await _service.UpdateBankAccountAsync(existingAccount.Id, request);

            // Assert
            result.Should().Be(existingAccount.Id);

            _bankAccountRepository.Verify(repo => repo.GetAsync(existingAccount.Id), Times.Once);

            _bankAccountRepository.Verify(repo =>
                repo.UpdateAsync(It.Is<ACC_BankAccount>(account =>
                    account.HolderName == request.HolderName &&
                    account.Title == request.Title &&
                    account.ShebaNumber == request.ShebaNumber &&
                    account.CardNumber == request.CardNumber)), Times.Once);
        }

        [Fact]
        public async Task DeleteBankAccount_AccountWithVoucher_ThrowsException()
        {
            // Arrange
            var account = Fixture.Create<ACC_BankAccount>();

            _bankAccountRepository.Setup(repo => repo.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(account);

            _voucherRepository.Setup(repo => repo.ArticlesExistAsync(It.IsAny<LookupType>(), It.IsAny<Guid>()))
                .ReturnsAsync(true);

            // Act
            Func<Task> func = async () => await _service.DeleteBankAccountAsync(Guid.NewGuid());

            // Assert
            await func.Should().ThrowAsync<Exception>().WithMessage(string.Format(Messages.Error_EntityCantBeDeletedDueToHavingVoucher, Messages.Entity_BankAccount));
            _bankAccountRepository.Verify(repo => repo.RemoveAsync(It.IsAny<Guid>()), Times.Never);
        }
        [Fact]
        public async Task AddBankAccount_InvalidBank_ShouldThrowValidationError()
        {
            // Arrange
            var request = Fixture.Build<BankAccountRequest>()
                .With(request => request.ShebaNumber, "330620000000202901868005")
                .With(request => request.CardNumber, "1234567890987654")
                .With(request => request.Bank, (Bank)999) // Invalid enum value
                .Create();

            // Act
            Func<Task> act = async () => await _service.AddBankAccountAsync(request);

            // Assert
            var exception = await act.Should().ThrowAsync<BigBangException>();
            exception.Which.Message.Should().Contain(string.Format(Messages.Error_EntityNotFound, Messages.Label_Bank));

            _bankAccountRepository.Verify(repo => repo.AddAsync(It.IsAny<ACC_BankAccount>()), Times.Never);
        }

        [Fact]
        public async Task GetBankAccountVouchers_ValidRequest_VouchersShouldBeReturned()
        {
            // Arrange
            var request = Fixture.Build<BankAccountVouchersRequest>()
                .With(request => request.FromDate, DateTime.Now)
                .With(request => request.ToDate, DateTime.Now.AddDays(1))
                .Create();

            var fiscalPeriodId = Guid.NewGuid();

            AccountingIdentityService.Setup(service => service.GetFiscalPeriodIdAsync())
                .ReturnsAsync(fiscalPeriodId);

            var articles1 = Fixture.Build<ACC_Article>()
                .With(article => article.LookupId, request.Id)
                .CreateMany(1);
            var voucher1 = Fixture.Build<ACC_Voucher>()
                .With(voucher => voucher.Articles, articles1.ToList())
                .Create();

            var articles2 = Fixture.Build<ACC_Article>()
                .With(article => article.LookupId, request.Id)
                .CreateMany(2);
            var voucher2 = Fixture.Build<ACC_Voucher>()
                .With(voucher => voucher.Articles, articles2.ToList)
                .Create();

            var vouchers = new List<ACC_Voucher>() { voucher1, voucher2 };

            _voucherRepository.Setup(repo =>
                    repo.GetListByLookupIdAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(),
                        It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LookupType>()))
                .ReturnsAsync(vouchers);

            // Act
            var result = await _service.GetBankAccountVouchersAsync(request);

            // Assert
            _voucherRepository.Verify(repo => repo.GetListByLookupIdAsync(request.FromDate, request.ToDate,
                request.PageSize, request.PageNumber, request.Id, fiscalPeriodId, LookupType.Bank), Times.Once);

            var voucherResponses = result.Items;

            voucherResponses.Count.Should().Be(2);
            voucherResponses[0].ArticleResponses.Count.Should().Be(1);
            voucherResponses[1].ArticleResponses.Count.Should().Be(2);

            result.PageNumber.Should().Be(request.PageNumber);
            result.PageSize.Should().Be(request.PageSize);

            for (var i = 0; i < result.Items.Count; i++)
            {
                voucherResponses[i].Id.Should().Be(vouchers.ElementAt(i).Id);
                voucherResponses[i].Template.Should().Be((VoucherTemplate)vouchers.ElementAt(i).VoucherTemplate.Id);
                voucherResponses[i].Title.Should().Be(vouchers.ElementAt(i).Title);
                voucherResponses[i].EffectiveDate.Should().Be(vouchers.ElementAt(i).EffectiveDate);
                voucherResponses[i].Amount.Should().Be(vouchers.ElementAt(i).Amount);
            }
        }

        [Fact]
        public async Task GetBankAccountVouchers_EmptyResult_EmptyListShouldBeReturned()
        {
            // Arrange
            var request = Fixture.Build<BankAccountVouchersRequest>()
                .With(request => request.FromDate, DateTime.Now)
                .With(request => request.ToDate, DateTime.Now.AddDays(1))
                .Create();

            var fiscalPeriodId = Guid.NewGuid();

            AccountingIdentityService.Setup(service => service.GetFiscalPeriodIdAsync())
                .ReturnsAsync(fiscalPeriodId);

            _voucherRepository.Setup(repo =>
                    repo.GetListByLookupIdAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(),
                        It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LookupType>()))
                .ReturnsAsync(new List<ACC_Voucher>());

            // Act
            var result = await _service.GetBankAccountVouchersAsync(request);

            // Assert
            _voucherRepository.Verify(repo => repo.GetListByLookupIdAsync(request.FromDate, request.ToDate,
                request.PageSize, request.PageNumber, request.Id, fiscalPeriodId, LookupType.Bank), Times.Once);

            result.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task GetBankAccountVouchers_WhenFromDateIsBiggerThanToDate_ShouldThrowException()
        {
            // Arrange
            var request = Fixture.Build<BankAccountVouchersRequest>()
                .With(request => request.FromDate, DateTime.Now.AddDays(1))
                .With(request => request.ToDate, DateTime.Now)
                .Create();

            // Act
            var result = async () => await _service.GetBankAccountVouchersAsync(request);

            // Assert
            await result.Should().ThrowAsync<BigBangException>().WithMessage(Messages.Error_FromDateToDateIsNotValid);
        }

        [Fact]
        public async Task GetBankAccount_WhenTitleLengthIsBiggerThanlimit_ShouldThrowException()
        {

            // Arrange
            var length = 101;
            string title = new string('a', length);

            var request = Fixture.Build<BankAccountRequest>()
                .With(request => request.Title, title)
                .Create();

            // Act

            Func<Task> func = async () => await _service.AddBankAccountAsync(request);

            // Assert
            var exception = await func.Should().ThrowAsync<BigBangException>();
            var message = exception.Which.Message;
        }
    }
}