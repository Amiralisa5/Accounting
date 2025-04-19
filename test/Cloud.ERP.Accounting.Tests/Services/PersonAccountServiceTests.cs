
using AutoFixture;
using BigBang.App.Cloud.ERP.Accounting.Application.Enums;
using BigBang.App.Cloud.ERP.Accounting.Application.PersonAccounts;
using BigBang.App.Cloud.ERP.Accounting.Application.PersonAccounts.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.App.Cloud.ERP.Accounting.Domain.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.FiscalPeriods;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.PersonAccounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Identity;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using BigBang.WebServer.Common.Exceptions;
using FluentAssertions;
using Moq;

namespace Cloud.ERP.Accounting.Tests.Services
{
    public class PersonAccountServiceTests : BaseServiceTests
    {
        private readonly Mock<IPersonAccountRepository> _personAccountRepository;
        private readonly Mock<IVoucherRepository> _voucherRepository;
        private readonly Mock<IVoucherService> _voucherService;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly PersonAccountService _service;

        public PersonAccountServiceTests() : base(new Mock<IAccountingIdentityService>(),
            new Mock<IFiscalPeriodRepository>(),
            new Mock<IEnumService>())
        {
            _voucherService = new Mock<IVoucherService>();
            _voucherRepository = new Mock<IVoucherRepository>();
            _accountRepository = new Mock<IAccountRepository>();
            _personAccountRepository = new Mock<IPersonAccountRepository>();

            _service = new PersonAccountService(
                AccountingIdentityService.Object,
                _personAccountRepository.Object,
                _voucherRepository.Object,
                _accountRepository.Object,
                _voucherService.Object);
        }

        [Fact]
        public async Task GetPersonAccounts_ValidBusiness_PersonAccountsShouldBeReturned()
        {
            // Arrange
            var businessId = Guid.NewGuid();

            var personAccounts = Fixture.CreateMany<ACC_PersonAccount>(3).ToList();
            var personRoleTypes = Fixture.CreateMany<PersonRoleType>(3).ToList();

            AccountingIdentityService.Setup(service => service.GetBusinessIdAsync())
                .ReturnsAsync(businessId);

            _personAccountRepository.Setup(service => service.GetListByBusinessIdAndRolesAsync(It.IsAny<Guid>(), It.IsAny<IList<PersonRoleType>>()))
                .ReturnsAsync(personAccounts);

            // Act
            var result = await _service.GetPersonAccountsAsync(personRoleTypes);

            // Assert
            result.Should().NotBeNull().And.HaveCount(3);

            AccountingIdentityService.Verify(service => service.GetBusinessIdAsync(), Times.Once);
            _personAccountRepository.Verify(repository => repository.GetListByBusinessIdAndRolesAsync(businessId, personRoleTypes), Times.Once);
        }

        [Fact]
        public async Task GetPersonAccount_ValidId_PersonAccountShouldBeReturned()
        {
            // Arrange
            var account = Fixture.Create<ACC_PersonAccount>();

            _personAccountRepository.Setup(repo => repo.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(account);

            // Act
            var result = await _service.GetPersonAccountAsync(account.Id);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(account.Id);
        }

        [Fact]
        public async Task GetPersonAccount_InvalidId_AValidationExceptionShouldBeThrown()
        {
            // Arrange
            _personAccountRepository.Setup(repo => repo.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(default(ACC_PersonAccount));

            // Act
            Func<Task> func = async () => await _service.GetPersonAccountAsync(Guid.NewGuid());

            // Assert
            await func.Should().ThrowAsync<BigBangException>()
                .WithMessage(string.Format(Messages.Error_EntityNotFound, Messages.Entity_PersonAccount));
        }

        [Fact]
        public async Task AddPersonAccount_ValidRequest_APersonAccountShouldBeAdded()
        {
            // Arrange
            var businessId = Guid.NewGuid();

            var request = Fixture.Build<AddPersonAccountRequest>()
                .With(request => request.MobileNumber, "09121234567")
                .With(request => request.InitialStatus, default(AccountNature?))
                .Create();

            AccountingIdentityService.Setup(service => service.GetBusinessIdAsync())
                .ReturnsAsync(businessId);

            _voucherService.Setup(service => service.RegisterVoucherAsync(It.IsAny<RegisterVoucherRequest>()))
               .ReturnsAsync(Fixture.Create<VoucherResponse>());

            ACC_PersonAccount? personAccount = null;
            _personAccountRepository.Setup(repo => repo.AddAsync(It.IsAny<ACC_PersonAccount>()))
                .Callback<ACC_PersonAccount>(addedPersonAccount => personAccount = addedPersonAccount);

            // Act
            var result = await _service.AddPersonAccountAsync(request);

            // Assert
            result.Should().NotBeEmpty();

            AccountingIdentityService.Verify(service => service.GetBusinessIdAsync(), Times.Once);
            _personAccountRepository.Verify(repo => repo.AddAsync(It.Is<ACC_PersonAccount>(account =>
                account.FirstName == request.FirstName &&
                account.LastName == request.LastName &&
                account.MobileNumber == request.MobileNumber)), Times.Once);

            personAccount?.FirstName.Should().Be(request.FirstName);
            personAccount?.LastName.Should().Be(request.LastName);
            personAccount?.MobileNumber.Should().Be(request.MobileNumber);
            personAccount?.Business.Id.Should().Be(businessId);

            _personAccountRepository.Verify(repository => repository.GetListByBusinessIdAndRolesAsync(It.IsAny<Guid>(), It.IsAny<IList<PersonRoleType>>()), Times.Never);
            _accountRepository.Verify(service => service.GetByNameAndFiscalPeriodIdAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _voucherService.Verify(service => service.RegisterVoucherAsync(It.IsAny<RegisterVoucherRequest>()), Times.Never);
        }

        [Fact]
        public async Task AddPersonAccount_InvalidRequest_AValidationExceptionShouldBeThrown()
        {
            // Arrange
            var request = Fixture.Build<AddPersonAccountRequest>()
                .With(request => request.MobileNumber, "0934546")
                .Create();

            // Act
            Func<Task> func = async () => await _service.AddPersonAccountAsync(request);

            // Assert
            await func.Should().ThrowAsync<BigBangException>()
                .WithMessage(Messages.Error_MobileNumberShouldBeElevenDigits);
        }

        [Fact]
        public async Task UpdatePersonAccount_ValidIdAndRequest_APersonAccountShouldBeUpdated()
        {
            // Arrange
            var account = Fixture.Create<ACC_PersonAccount>();
            var request = Fixture.Build<UpdatePersonAccountRequest>()
                .With(request => request.MobileNumber, "09121234567")
                .Create();

            _personAccountRepository.Setup(repo => repo.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(account);

            // Act
            var result = await _service.UpdatePersonAccountAsync(account.Id, request);

            // Assert
            result.Should().Be(account.Id);

            _personAccountRepository.Verify(repo => repo
                .UpdateAsync(It.Is<ACC_PersonAccount>(personAccount => personAccount.Id == account.Id)), Times.Once);
        }

        [Fact]
        public async Task UpdatePersonAccount_InvalidId_AValidationExceptionShouldBeThrown()
        {
            // Arrange
            var request = Fixture.Create<UpdatePersonAccountRequest>();

            _personAccountRepository.Setup(repo => repo.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(default(ACC_PersonAccount));

            // Act
            Func<Task> func = async () => await _service.UpdatePersonAccountAsync(Guid.NewGuid(), request);

            // Assert
            await func.Should().ThrowAsync<BigBangException>()
                .WithMessage(Messages.Error_MobileNumberShouldBeElevenDigits);
        }

        [Fact]
        public async Task DeletePersonAccount_ValidId_APersonAccountShouldBeDeleted()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var account = Fixture.Create<ACC_PersonAccount>();

            _personAccountRepository.Setup(repo => repo.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(account);

            _personAccountRepository.Setup(repo => repo.RemoveAsync(It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);

            _voucherRepository.Setup(repo => repo.ArticlesExistAsync(It.IsAny<LookupType>(), It.IsAny<Guid>()))
                .ReturnsAsync(false);

            // Act
            await _service.DeletePersonAccountAsync(accountId);

            // Assert
            _personAccountRepository.Verify(repo => repo.RemoveAsync(accountId), Times.Once);
        }

        [Fact]
        public async Task DeletePersonAccount_InvalidId_AValidationExceptionShouldBeThrown()
        {
            // Arrange
            _personAccountRepository.Setup(repo => repo.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(default(ACC_PersonAccount));

            // Act
            Func<Task> func = async () => await _service.DeletePersonAccountAsync(Guid.NewGuid());

            // Assert
            await func.Should().ThrowAsync<BigBangException>()
                .WithMessage(string.Format(Messages.Error_EntityNotFound, Messages.Entity_PersonAccount));
        }


        [Fact]
        public async Task DeletePersonAccount_ExistingArticles_AValidationExceptionShouldBeThrown()
        {
            // Arrange
            var account = Fixture.Create<ACC_PersonAccount>();

            _personAccountRepository.Setup(repo => repo.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(account);

            _voucherRepository.Setup(repo => repo.ArticlesExistAsync(It.IsAny<LookupType>(), It.IsAny<Guid>()))
                .ReturnsAsync(true);

            // Act
            Func<Task> func = async () => await _service.DeletePersonAccountAsync(Guid.NewGuid());

            // Assert
            await func.Should().ThrowAsync<Exception>()
                .WithMessage(string.Format(Messages.Error_EntityCantBeDeletedDueToHavingVoucher, Messages.Entity_PersonAccount));
        }

        [Fact]
        public async Task GetTotalDebts_DataIsValid_ShouldReturnCorrectTotalDebts()
        {
            // Arrange
            var personAccountId = Guid.NewGuid();
            var fiscalPeriodId = Guid.NewGuid();

            const long totalDebits = 10000L;
            const long totalCredits = 4000L;

            AccountingIdentityService.Setup(service => service.GetFiscalPeriodIdAsync())
               .ReturnsAsync(fiscalPeriodId);

            _voucherRepository.SetupSequence(repo => repo.GetTotalDebtsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LookupType>(), It.IsAny<ArticleType>(), It.IsAny<string>()))
                .ReturnsAsync(totalDebits)
                .ReturnsAsync(totalCredits);

            // Act
            var result = await _service.GetTotalDebtsAsync(personAccountId);

            // Assert
            result.Should().Be(6000L);

            _voucherRepository.Verify(repository => repository.GetTotalDebtsAsync(fiscalPeriodId, personAccountId, LookupType.Person, ArticleType.Debit, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ReceivableFromOthers))), Times.Once);
            _voucherRepository.Verify(repository => repository.GetTotalDebtsAsync(fiscalPeriodId, personAccountId, LookupType.Person, ArticleType.Credit, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ReceivableFromOthers))), Times.Once);
        }

        [Fact]
        public async Task GetPersonAccountVouchers_ValidRequest_VouchersShouldBeReturned()
        {
            // Arrange
            var request = Fixture.Build<PersonAccountVouchersRequest>()
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

            var vouchers = new List<ACC_Voucher> { voucher1, voucher2 };

            _voucherRepository.Setup(repo =>
                    repo.GetListByLookupIdAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(),
                        It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LookupType>()))
                .ReturnsAsync(vouchers);

            // Act
            var result = await _service.GetPersonAccountVouchersAsync(request);

            // Assert
            _voucherRepository.Verify(repo => repo.GetListByLookupIdAsync(request.FromDate, request.ToDate,
                request.PageSize, request.PageNumber, request.Id, fiscalPeriodId, LookupType.Person), Times.Once);

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
        public async Task GetPersonAccountVouchers_EmptyResult_EmptyListShouldBeReturned()
        {
            // Arrange
            var request = Fixture.Build<PersonAccountVouchersRequest>()
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
            var result = await _service.GetPersonAccountVouchersAsync(request);

            // Assert
            _voucherRepository.Verify(repo => repo.GetListByLookupIdAsync(request.FromDate, request.ToDate,
                request.PageSize, request.PageNumber, request.Id, fiscalPeriodId, LookupType.Person), Times.Once);

            result.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task GetPersonAccountVouchers_WhenFromDateIsBiggerThanToDate_ShouldThrowException()
        {
            // Arrange
            var request = Fixture.Build<PersonAccountVouchersRequest>()
                .With(request => request.FromDate, DateTime.Now.AddDays(1))
                .With(request => request.ToDate, DateTime.Now)
                .Create();

            // Act
            var result = async () => await _service.GetPersonAccountVouchersAsync(request);

            // Assert
            await result.Should().ThrowAsync<BigBangException>().WithMessage(Messages.Error_FromDateToDateIsNotValid);
        }

        [Fact]
        public async Task AddPersonAccount_WithCreditorStatus_ShouldRegisterVoucherForCreditorPersonAccount()
        {
            // Arrange
            var request = Fixture.Build<AddPersonAccountRequest>()
                .With(accountRequest => accountRequest.InitialStatus, AccountNature.Creditor)
                .With(accountRequest => accountRequest.Amount, 1000)
                .With(accountRequest => accountRequest.MobileNumber, "09121234567")
                .Create();

            var businessId = Guid.NewGuid();

            AccountingIdentityService.SetupSequence(identityService => identityService.GetBusinessIdAsync())
                .ReturnsAsync(businessId)
                .ReturnsAsync(businessId);

            var fiscalPeriodId = Guid.NewGuid();

            AccountingIdentityService.Setup(identityService => identityService.GetFiscalPeriodIdAsync())
                .ReturnsAsync(fiscalPeriodId);

            var equityAccount = Fixture.Create<ACC_Account>();
            var receivableFromOthersAccount = Fixture.Create<ACC_Account>();

            _accountRepository.SetupSequence(accountRepository =>
                    accountRepository.GetByNameAndFiscalPeriodIdAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(equityAccount)
                .ReturnsAsync(receivableFromOthersAccount);

            ACC_PersonAccount? personAccount = null;
            _personAccountRepository.Setup(personAccountRepository => personAccountRepository.AddAsync(It.IsAny<ACC_PersonAccount>()))
                .Callback<ACC_PersonAccount>(addedPersonAccount => personAccount = addedPersonAccount);

            var ownerPersonAccount = Fixture.Create<ACC_PersonAccount>();

            _personAccountRepository.Setup(repository =>
                    repository.GetListByBusinessIdAndRolesAsync(It.IsAny<Guid>(), It.IsAny<IList<PersonRoleType>>()))
                .ReturnsAsync([ownerPersonAccount]);

            _voucherService.Setup(voucherService => voucherService.RegisterVoucherAsync(It.IsAny<RegisterVoucherRequest>()))
                .ReturnsAsync(Fixture.Create<VoucherResponse>());

            // Act
            var result = await _service.AddPersonAccountAsync(request);

            // Assert
            result.Should().NotBeEmpty();

            AccountingIdentityService.Verify(service => service.GetBusinessIdAsync(), Times.Exactly(2));
            AccountingIdentityService.Verify(service => service.GetFiscalPeriodIdAsync(), Times.Once);
            _accountRepository.Verify(repository =>
                repository.GetByNameAndFiscalPeriodIdAsync(fiscalPeriodId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_SubsidiaryEquity))), Times.Once);
            _accountRepository.Verify(repository =>
                repository.GetByNameAndFiscalPeriodIdAsync(fiscalPeriodId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ReceivableFromOthers))), Times.Once);
            _personAccountRepository.Verify(repository => repository.AddAsync(It.Is<ACC_PersonAccount>(addedPersonAccount =>
                addedPersonAccount.FirstName == request.FirstName &&
                addedPersonAccount.LastName == request.LastName &&
                addedPersonAccount.MobileNumber == request.MobileNumber &&
                addedPersonAccount.Business.Id == businessId)), Times.Once);

            personAccount.Should().NotBeNull();
            personAccount?.FirstName.Should().Be(request.FirstName);
            personAccount?.LastName.Should().Be(request.LastName);
            personAccount?.MobileNumber.Should().Be(request.MobileNumber);
            personAccount?.Business.Id.Should().Be(businessId);

            _personAccountRepository.Verify(repository => repository.GetListByBusinessIdAndRolesAsync(businessId, It.Is<IList<PersonRoleType>>(personRoleTypes => personRoleTypes.SequenceEqual(new List<PersonRoleType> { PersonRoleType.BusinessOwner }))), Times.Once);

            _voucherService.Verify(voucherService => voucherService.RegisterVoucherAsync(It.Is<RegisterVoucherRequest>(voucherRequest =>
                   voucherRequest.Template == VoucherTemplate.Custom &&
                   voucherRequest.Articles.Count() == 2 &&
                   voucherRequest.Articles.ElementAt(0).AccountId == receivableFromOthersAccount.Id &&
                   voucherRequest.Articles.ElementAt(0).LookupId == personAccount!.Id &&
                   voucherRequest.Articles.ElementAt(0).Amount == request.Amount &&
                   voucherRequest.Articles.ElementAt(0).Currency == Currency.Rial &&
                   voucherRequest.Articles.ElementAt(0).Type == ArticleType.Debit &&
                   voucherRequest.Articles.ElementAt(1).AccountId == equityAccount.Id &&
                   voucherRequest.Articles.ElementAt(1).LookupId == ownerPersonAccount.Id &&
                   voucherRequest.Articles.ElementAt(1).Amount == request.Amount &&
                   voucherRequest.Articles.ElementAt(1).Currency == Currency.Rial &&
                   voucherRequest.Articles.ElementAt(1).Type == ArticleType.Credit
            )), Times.Once);
        }

        [Fact]
        public async Task AddPersonAccount_WithDebtorStatus_ShouldRegisterVoucherForDebtorPersonAccount()
        {
            // Arrange
            var request = Fixture.Build<AddPersonAccountRequest>()
                .With(accountRequest => accountRequest.InitialStatus, AccountNature.Debtor)
                .With(accountRequest => accountRequest.Amount, 2000)
                .With(accountRequest => accountRequest.MobileNumber, "09121234567")
                .Create();

            var businessId = Guid.NewGuid();

            AccountingIdentityService.SetupSequence(identityService => identityService.GetBusinessIdAsync())
                .ReturnsAsync(businessId)
                .ReturnsAsync(businessId);

            var fiscalPeriodId = Guid.NewGuid();

            AccountingIdentityService.Setup(identityService => identityService.GetFiscalPeriodIdAsync())
                .ReturnsAsync(fiscalPeriodId);

            var equityAccount = Fixture.Build<ACC_Account>()
                .With(account => account.Name, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_SubsidiaryEquity)))
                .Create();

            var liabilitiesToOthersAccount = Fixture.Build<ACC_Account>()
                .With(account => account.Name, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_LiabilitiesToOthers)))
                .Create();

            _accountRepository.SetupSequence(accountRepository => accountRepository.GetByNameAndFiscalPeriodIdAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(equityAccount)
                .ReturnsAsync(liabilitiesToOthersAccount);

            ACC_PersonAccount? personAccount = null;
            _personAccountRepository.Setup(personAccountRepository => personAccountRepository.AddAsync(It.IsAny<ACC_PersonAccount>()))
                .Callback<ACC_PersonAccount>(account => personAccount = account);

            var ownerPersonAccount = Fixture.Create<ACC_PersonAccount>();

            _personAccountRepository.Setup(repository =>
                    repository.GetListByBusinessIdAndRolesAsync(It.IsAny<Guid>(), It.IsAny<IList<PersonRoleType>>()))
                .ReturnsAsync([ownerPersonAccount]);

            _voucherService.Setup(voucherService => voucherService.RegisterVoucherAsync(It.IsAny<RegisterVoucherRequest>()))
                .ReturnsAsync(Fixture.Create<VoucherResponse>());

            // Act
            var result = await _service.AddPersonAccountAsync(request);

            // Assert
            result.Should().NotBeEmpty();
            AccountingIdentityService.Verify(service => service.GetBusinessIdAsync(), Times.Exactly(2));
            AccountingIdentityService.Verify(service => service.GetFiscalPeriodIdAsync(), Times.Once);

            _accountRepository.Verify(repository => repository.GetByNameAndFiscalPeriodIdAsync(fiscalPeriodId, equityAccount.Name), Times.Once);
            _accountRepository.Verify(repository => repository.GetByNameAndFiscalPeriodIdAsync(fiscalPeriodId, equityAccount.Name), Times.Once);

            _personAccountRepository.Verify(repository => repository.AddAsync(It.Is<ACC_PersonAccount>(account =>
                account.FirstName == request.FirstName &&
                account.LastName == request.LastName &&
                account.MobileNumber == request.MobileNumber &&
                account.Business.Id == businessId)), Times.Once);

            personAccount.Should().NotBeNull();
            personAccount?.FirstName.Should().Be(request.FirstName);
            personAccount?.LastName.Should().Be(request.LastName);
            personAccount?.MobileNumber.Should().Be(request.MobileNumber);
            personAccount?.Business.Id.Should().Be(businessId);

            _personAccountRepository.Verify(repository => repository.GetListByBusinessIdAndRolesAsync(businessId, It.Is<IList<PersonRoleType>>(personRoleTypes => personRoleTypes.SequenceEqual(new List<PersonRoleType> { PersonRoleType.BusinessOwner }))), Times.Once);

            _voucherService.Verify(voucherService => voucherService.RegisterVoucherAsync(It.Is<RegisterVoucherRequest>(voucherRequest =>
                   voucherRequest.Template == VoucherTemplate.Custom &&
                   voucherRequest.Articles.Count() == 2 &&
                   voucherRequest.Articles.ElementAt(0).AccountId == liabilitiesToOthersAccount.Id &&
                   voucherRequest.Articles.ElementAt(0).LookupId == personAccount!.Id &&
                   voucherRequest.Articles.ElementAt(0).Amount == request.Amount &&
                   voucherRequest.Articles.ElementAt(0).Currency == Currency.Rial &&
                   voucherRequest.Articles.ElementAt(0).Type == ArticleType.Credit &&
                   voucherRequest.Articles.ElementAt(1).AccountId == equityAccount.Id &&
                   voucherRequest.Articles.ElementAt(1).LookupId == ownerPersonAccount.Id &&
                   voucherRequest.Articles.ElementAt(1).Amount == request.Amount &&
                   voucherRequest.Articles.ElementAt(1).Currency == Currency.Rial &&
                   voucherRequest.Articles.ElementAt(1).Type == ArticleType.Debit
            )), Times.Once);
        }

        [Fact]
        public async Task AddPersonAccount_WithNoStatus_ShouldNotRegisterVoucher()
        {
            // Arrange
            var request = Fixture.Build<AddPersonAccountRequest>()
                .With(accountRequest => accountRequest.InitialStatus, (AccountNature?)null)
                .With(accountRequest => accountRequest.MobileNumber, "09121234567")
                .Create();

            var businessId = Guid.NewGuid();

            AccountingIdentityService.Setup(identityService => identityService.GetBusinessIdAsync())
                .ReturnsAsync(businessId);

            ACC_PersonAccount? personAccount = null;
            _personAccountRepository.Setup(personAccountRepository => personAccountRepository.AddAsync(It.IsAny<ACC_PersonAccount>()))
                .Callback<ACC_PersonAccount>(pa => personAccount = pa);

            // Act
            var result = await _service.AddPersonAccountAsync(request);

            // Assert
            result.Should().NotBeEmpty();
            AccountingIdentityService.Verify(identityService => identityService.GetBusinessIdAsync(), Times.Once);
            _personAccountRepository.Verify(personAccountRepository => personAccountRepository.AddAsync(It.Is<ACC_PersonAccount>(pa =>
                pa.FirstName == request.FirstName &&
                pa.LastName == request.LastName &&
                pa.MobileNumber == request.MobileNumber &&
                pa.Business.Id == businessId)), Times.Once);

            personAccount.Should().NotBeNull();
            personAccount?.FirstName.Should().Be(request.FirstName);
            personAccount?.LastName.Should().Be(request.LastName);
            personAccount?.MobileNumber.Should().Be(request.MobileNumber);
            personAccount?.Business.Id.Should().Be(businessId);

            _voucherService.Verify(voucherService => voucherService.RegisterVoucherAsync(It.IsAny<RegisterVoucherRequest>()), Times.Never);
        }

        [Fact]
        public async Task UpdatePersonAccount_WhenAssigningBusinessOwnerRoleToAnotherPerson_ShouldThrowException()
        {
            // Arrange
            var personAccountId = Guid.NewGuid();

            var personAccount = Fixture.Build<ACC_PersonAccount>()
                .With(account => account.Id, personAccountId)
                .Create();

            var request = Fixture.Build<UpdatePersonAccountRequest>()
                .With(request => request.MobileNumber, "09121234567")
                .With(request => request.RoleTypes, [PersonRoleType.BusinessOwner, PersonRoleType.Employee])
                .Create();

            _personAccountRepository.Setup(repo => repo.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(personAccount);

            var businessId = Guid.NewGuid();

            AccountingIdentityService.Setup(service => service.GetBusinessIdAsync())
                .ReturnsAsync(businessId);

            var ownerPersonAccount = Fixture.Create<ACC_PersonAccount>();

            _personAccountRepository.Setup(service => service.GetListByBusinessIdAndRolesAsync(It.IsAny<Guid>(), It.IsAny<IList<PersonRoleType>>()))
                .ReturnsAsync([ownerPersonAccount]);

            // Act
            var result = async () => await _service.UpdatePersonAccountAsync(personAccountId, request);

            // Assert
            await result.Should().ThrowAsync<BigBangException>()
                .WithMessage(Messages.Error_CannotAssigeBussinessOwnerRoleToAnotherPerson);
        }

        [Fact]
        public async Task UpdatePersonAccount_WhenRemovingBusinessOwnerRoleFromOneself_ShouldThrowException()
        {
            // Arrange
            var ownerPersonAccountId = Guid.NewGuid();

            var ownerPersonAccount = Fixture.Build<ACC_PersonAccount>()
                .With(account => account.Id, ownerPersonAccountId)
                .Create();

            var request = Fixture.Build<UpdatePersonAccountRequest>()
                .With(request => request.MobileNumber, "09121234567")
                .With(request => request.RoleTypes, [PersonRoleType.Employee])
                .Create();

            _personAccountRepository.Setup(repo => repo.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(ownerPersonAccount);

            AccountingIdentityService.Setup(service => service.GetBusinessIdAsync())
                .ReturnsAsync(ownerPersonAccountId);

            _personAccountRepository.Setup(service => service.GetListByBusinessIdAndRolesAsync(It.IsAny<Guid>(), It.IsAny<IList<PersonRoleType>>()))
                .ReturnsAsync([ownerPersonAccount]);

            // Act
            var result = async () => await _service.UpdatePersonAccountAsync(ownerPersonAccountId, request);

            // Assert
            await result.Should().ThrowAsync<BigBangException>()
                .WithMessage(Messages.Error_CannotRemoveBussinessOwnerRoleForOwnerPersonAccount);
        }

        [Fact]
        public async Task AddPersonAccount_WhenRoleIsOutOfRange_ShouldThrowException()
        {
            // Arrange
            var request = Fixture.Build<AddPersonAccountRequest>()
                .With(request => request.MobileNumber, "09121234567")
                .With(request => request.RoleTypes, [(PersonRoleType)7])
                .Create();

            // Act
            var result = async () => await _service.AddPersonAccountAsync(request);

            // Assert
            await result.Should().ThrowAsync<BigBangException>()
                .WithMessage(string.Format(Messages.Error_EntityNotFound, Messages.Label_PersonRole));
        }

        [Fact]
        public async Task AddPersonAccount_WhenAssigningBusinessOwnerRole_ShouldThrowException()
        {
            // Arrange
            var request = Fixture.Build<AddPersonAccountRequest>()
                .With(request => request.MobileNumber, "09121234567")
                .With(request => request.RoleTypes, [PersonRoleType.BusinessOwner, PersonRoleType.Employee])
                .Create();

            // Act
            var result = async () => await _service.AddPersonAccountAsync(request);

            // Assert
            await result.Should().ThrowAsync<BigBangException>()
                .WithMessage(Messages.Error_CannotAssigeBussinessOwnerRoleToAnotherPerson);
        }

        [Fact]
        public async Task DeletePersonAccount_WhenPersonIsBusinessOwner_ShouldThrowException()
        {
            // Arrange
            var personAccountId = Guid.NewGuid();

            var personAccount = Fixture.Build<ACC_PersonAccount>()
                .With(account => account.Id, personAccountId)
                .With(account => account.PersonAccountRoles,
                    [
                        new ACC_PersonAccountRole { PersonRoleTypeId = PersonRoleType.BusinessOwner }
                    ])
                .Create();

            _personAccountRepository.Setup(repo => repo.GetAsync(personAccountId))
                .ReturnsAsync(personAccount);

            // Act
            var result = async () => await _service.DeletePersonAccountAsync(personAccountId);

            // Assert
            await result.Should().ThrowAsync<BigBangException>()
                .WithMessage(Messages.Error_BuisnessOwnerCannotBeDeleted);

            _personAccountRepository.Verify(repo => repo.GetAsync(personAccountId), Times.Once);
            _personAccountRepository.Verify(repo => repo.RemoveAsync(It.IsAny<Guid>()), Times.Never);
        }
    }
}