using BigBang.App.Cloud.ERP.Accounting.Application.Enums;
using BigBang.App.Cloud.ERP.Accounting.Application.Enums.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Application.Files;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.App.Cloud.ERP.Accounting.Domain.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.BankAccounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.FiscalPeriods;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.PersonAccounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Products;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.VoucherTemplates;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Identity;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using BigBang.WebServer.Common.Exceptions;
using Cloud.ERP.Accounting.Tests.Extensions;
using Cloud.ERP.Accounting.Tests.TestData;
using FluentAssertions;
using Moq;

namespace Cloud.ERP.Accounting.Tests.Services
{
    public class VoucherServiceTests : BaseServiceTests
    {
        private readonly Mock<IVoucherRepository> _voucherRepository;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<IBankAccountRepository> _bankAccountRepository;
        private readonly Mock<IPersonAccountRepository> _personAccountRepository;
        private readonly Mock<IVoucherTemplateRepository> _voucherTemplateRepository;
        private readonly Mock<IProductRepository> _productRepository;
        private readonly VoucherService _service;

        public VoucherServiceTests() : base(new Mock<IAccountingIdentityService>(),
            new Mock<IFiscalPeriodRepository>(),
            new Mock<IEnumService>())
        {
            _voucherRepository = new Mock<IVoucherRepository>();
            _accountRepository = new Mock<IAccountRepository>();
            _bankAccountRepository = new Mock<IBankAccountRepository>();
            _personAccountRepository = new Mock<IPersonAccountRepository>();
            _voucherTemplateRepository = new Mock<IVoucherTemplateRepository>();
            _productRepository = new Mock<IProductRepository>();

            _service = new VoucherService(
                AccountingIdentityService.Object,
                _voucherRepository.Object,
                _accountRepository.Object,
                _bankAccountRepository.Object,
                _personAccountRepository.Object,
                FiscalPeriodRepository.Object,
                _voucherTemplateRepository.Object,
                _productRepository.Object,
                EnumService.Object,
                new Mock<IFileService>().Object);
        }

        [Theory]
        [MemberData(nameof(VoucherTestData.GetValidVoucherList), parameters: 3, MemberType = typeof(VoucherTestData))]
        public async Task GetVoucherList_ValidRequest_VouchersShouldBeReturned(GetVoucherListRequest request, List<ACC_Voucher> vouchers)
        {
            // Arrange
            var fiscalPeriodId = SetupFiscalPeriodId();

            _voucherRepository.Setup(repo => repo.GetListAsync(It.IsAny<Guid>(), It.IsAny<VoucherTemplate>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(vouchers);

            // Act
            var result = await _service.GetVoucherListAsync(request);

            // Assert
            _voucherRepository.Verify(repository => repository.GetListAsync(fiscalPeriodId, request.Template, request.PageSize, request.PageNumber), Times.Once);

            var voucherResponses = result.Items;
            voucherResponses.Count.Should().Be(3);

            result.PageNumber.Should().Be(request.PageNumber);
            result.PageSize.Should().Be(request.PageSize);

            for (var i = 0; i < result.Items.Count; i++)
            {
                voucherResponses[i].Id.Should().Be(vouchers.ElementAt(i).Id);
                voucherResponses[i].Template.Should().Be(request.Template);
                voucherResponses[i].Title.Should().Be(vouchers.ElementAt(i).Title);
                voucherResponses[i].EffectiveDate.Should().Be(vouchers.ElementAt(i).EffectiveDate);
                voucherResponses[i].Amount.Should().Be(vouchers.ElementAt(i).Amount);
            }
        }

        [Theory]
        [MemberData(nameof(VoucherTestData.GetValidVoucherList), parameters: 0, MemberType = typeof(VoucherTestData))]
        public async Task GetVoucherList_WhenNoResult_EmptyListShouldBeReturned(GetVoucherListRequest request, List<ACC_Voucher> vouchers)
        {
            // Arrange
            var fiscalPeriodId = SetupFiscalPeriodId();

            _voucherRepository.Setup(repo => repo.GetListAsync(It.IsAny<Guid>(), It.IsAny<VoucherTemplate>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(vouchers);

            // Act
            var result = await _service.GetVoucherListAsync(request);

            // Assert
            _voucherRepository.Verify(repo => repo.GetListAsync(fiscalPeriodId, request.Template, request.PageSize, request.PageNumber), Times.Once);
            result.Items.Should().BeEmpty();
        }

        [Theory]
        [MemberData(nameof(VoucherTestData.GetValidVoucherDetails), MemberType = typeof(VoucherTestData))]
        public async Task GetVoucher_WhenVoucherExists_ShouldReturnVoucherDetails(ACC_Voucher voucher)
        {
            // Arrange
            var accounts = voucher.Articles.Select(article => article.Account).ToList();

            var fileId = Guid.NewGuid();

            _voucherRepository.Setup(repository => repository.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(voucher);

            _voucherRepository.Setup(repository => repository.GetFileIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(fileId);

            _accountRepository.Setup(repo => repo.GetListByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(voucher.Articles.Select(article => article.Account).ToList);

            // Act
            var result = await _service.GetVoucherAsync(voucher.Id);

            // Assert
            result.Should().NotBeNull();
            result.Number.Should().Be(voucher.Number);
            result.Title.Should().Be(voucher.Title);
            result.Description.Should().Be(voucher.Description);
            result.Amount.Should().Be(voucher.Amount);
            result.Template.Should().Be(voucher.VoucherTemplate.Id);
            result.Articles.Should().HaveCount(2);
            result.FileId.Should().Be(fileId);

            _voucherRepository.Verify(repository => repository.GetAsync(voucher.Id), Times.Once);
            _voucherRepository.Verify(repository => repository.GetFileIdAsync(voucher.Id), Times.Once);
            _accountRepository.Verify(repository => repository.GetListByIdsAsync(It.Is<IEnumerable<Guid>>(ids =>
                ids.SequenceEqual(accounts.Select(account => account.Id)))), Times.Once);
        }

        [Fact]
        public async Task GetVoucher_WhenVoucherDoesNotExist_ShouldThrowException()
        {
            // Arrange
            _voucherRepository.Setup(repository => repository.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(default(ACC_Voucher));

            var voucherId = Guid.NewGuid();

            // Act
            var result = async () => await _service.GetVoucherAsync(voucherId);

            // Assert
            var exceptionAssertions = await result.Should().ThrowAsync<BigBangException>();
            exceptionAssertions.AssertNotFound()
                .AssertMessage(string.Format(Messages.Error_EntityNotFound, Messages.Entity_Voucher));

            _voucherRepository.Verify(repository => repository.GetAsync(voucherId), Times.Once);
            _voucherRepository.Verify(repository => repository.GetFileIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        #region General

        [Theory]
        [MemberData(nameof(VoucherTestData.GetInvalidVoucherRequestWithInvalidTemplate), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenTemplateValueIsNotValid_ShouldThrowException(RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            // Act
            var result = async () => await _service.RegisterVoucherAsync(request);

            // Assert
            var exceptionAssertions = await result.Should().ThrowAsync<BigBangException>();
            exceptionAssertions.AssertBadRequest()
                .AssertMessage(Messages.Error_TemplateValueIsNotValid);

            VerifyFiscalPeriod(fiscalPeriod.Id);

            _voucherRepository.Verify(repository => repository.GetLastAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Theory]
        [MemberData(nameof(VoucherTestData.GetInvalidVoucherRequestWithLengthyDescription), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenDescriptionLengthIsGreaterThan255_ShouldThrowException(RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            // Act
            var result = async () => await _service.RegisterVoucherAsync(request);

            // Assert
            var exceptionAssertions = await result.Should().ThrowAsync<BigBangException>();
            exceptionAssertions.AssertBadRequest()
                .AssertMessage(string.Format(Messages.Error_MaximumLengthShouldBe, Messages.Label_VoucherDescription, 255));

            VerifyFiscalPeriod(fiscalPeriod.Id);

            _voucherRepository.Verify(repository => repository.GetLastAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Theory]
        [MemberData(nameof(VoucherTestData.GetInvalidVoucherRequestWithOutOfRangeEffectiveDate), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenEffectiveDateIsNotInFiscalPeriod_ShouldThrowException(RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            // Act
            var result = async () => await _service.RegisterVoucherAsync(request);

            // Assert
            var exceptionAssertions = await result.Should().ThrowAsync<BigBangException>();
            exceptionAssertions.AssertBadRequest()
                .AssertMessage(Messages.Error_EffectiveDateShouldBeInRangeOfFiscalPeriodDate);

            VerifyFiscalPeriod(fiscalPeriod.Id);

            _voucherRepository.Verify(repository => repository.GetLastAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Theory]
        [MemberData(nameof(VoucherTestData.GetInvalidVoucherRequestWithUnbalancedArticles), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenTotalAmountOfDebitAndCreditArticlesAreNotEqual_ShouldThrowException(RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            // Act
            var result = async () => await _service.RegisterVoucherAsync(request);

            // Assert
            var exceptionAssertions = await result.Should().ThrowAsync<BigBangException>();
            exceptionAssertions.AssertBadRequest()
                .AssertMessage(Messages.Error_SumOfDebitAndCreditShouldBeEqual);

            VerifyFiscalPeriod(fiscalPeriod.Id);

            _voucherRepository.Verify(repository => repository.GetLastAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Theory]
        [MemberData(nameof(VoucherTestData.GetInvalidVoucherRequestArticlesWithoutAccountId), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenArticlesWithoutAccountId_ShouldThrowException(RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            // Act
            var result = async () => await _service.RegisterVoucherAsync(request);

            // Assert
            var exceptionAssertions = await result.Should().ThrowAsync<BigBangException>();
            exceptionAssertions.AssertBadRequest()
                .AssertMessage(string.Format(Messages.Error_FieldRequired, Messages.Entity_Account));

            VerifyFiscalPeriod(fiscalPeriod.Id);

            _voucherRepository.Verify(repository => repository.GetLastAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Theory]
        [MemberData(nameof(VoucherTestData.GetInvalidVoucherRequestWithNegativeAmounts), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenArticleAmountsAreNotGreaterThanZero_ShouldThrowException(RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            // Act
            var result = async () => await _service.RegisterVoucherAsync(request);

            // Assert
            var exceptionAssertions = await result.Should().ThrowAsync<BigBangException>();
            exceptionAssertions.AssertBadRequest()
                .AssertMessage(Messages.Error_SumOfDebitAndCreditShouldBeEqual)
                .AssertMessage(string.Format(Messages.Error_ShouldBeGreaterThan, Messages.Label_Amount, 0));

            VerifyFiscalPeriod(fiscalPeriod.Id);

            _voucherRepository.Verify(repository => repository.GetLastAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Theory]
        [MemberData(nameof(VoucherTestData.GetInvalidVoucherRequestWithInvalidArticleType), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenArticleTypeIsNotValid_ShouldThrowException(RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            // Act
            var result = async () => await _service.RegisterVoucherAsync(request);

            // Assert
            var exceptionAssertions = await result.Should().ThrowAsync<BigBangException>();
            exceptionAssertions.AssertBadRequest()
                .AssertMessage(Messages.Error_SumOfDebitAndCreditShouldBeEqual)
                .AssertMessage(Messages.Error_ArticleTypeIsNotValid);

            VerifyFiscalPeriod(fiscalPeriod.Id);

            _voucherRepository.Verify(repository => repository.GetLastAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Theory]
        [MemberData(nameof(VoucherTestData.GetInvalidVoucherRequestArticlesWithoutLookupId), parameters: new[] { VoucherTemplate.PayDebt, VoucherTemplate.ReceiveDebt, VoucherTemplate.Expenses, VoucherTemplate.Deposit }, MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenLookupIdIsNull_ShouldThrowException(RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            // Act
            var result = async () => await _service.RegisterVoucherAsync(request);

            // Assert
            var exceptionAssertions = await result.Should().ThrowAsync<BigBangException>();
            exceptionAssertions.AssertBadRequest()
                .AssertMessage(string.Format(Messages.Error_ShouldNotBeNull, Messages.Label_Lookup));

            VerifyFiscalPeriod(fiscalPeriod.Id);

            _voucherRepository.Verify(repository => repository.GetLastAsync(It.IsAny<Guid>()), Times.Never);
        }

        #endregion

        #region Expenses Template

        [Theory]
        [MemberData(nameof(VoucherTestData.GetInvalidExpensesVoucherRequestWithBankAccountIsNull), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenTemplateIsExpensesAndArticleTypeIsCreditAndAccountNameIsBankAndAccountLookupTypeIstBankAndBankAccountIsNull_ShouldThrowException(
            RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod, ACC_Account[] accounts, EnumDataModel currencyEnum)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            _accountRepository.Setup(repository => repository.GetListByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(accounts);

            SetupCurrency(currencyEnum);

            _bankAccountRepository.Setup(repository => repository.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(default(ACC_BankAccount));

            // Act
            var result = async () => await _service.RegisterVoucherAsync(request);

            // Assert
            var exceptionAssertions = await result.Should().ThrowAsync<BigBangException>();
            exceptionAssertions.AssertBadRequest()
                .AssertMessage(string.Format(Messages.Error_FieldRequired, Messages.Entity_BankAccount));

            VerifyFiscalPeriod(fiscalPeriod.Id);
            VerifyAccounts(request);

            EnumService.Verify(service => service.GetAsync(nameof(Currency)), Times.Once);
            _bankAccountRepository.Verify(repository => repository.GetAsync(request.Articles.Last().LookupId!.Value), Times.Once);
            _accountRepository.Verify(repository => repository.GetListByParentNameAsync(It.IsAny<string>()), Times.Never);
            _personAccountRepository.Verify(repository => repository.GetAsync(It.IsAny<Guid>()), Times.Never);
            _voucherRepository.Verify(repository => repository.GetLastAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Theory]
        [MemberData(nameof(VoucherTestData.GetInvalidExpensesVoucherRequestWithBankAccountBalanceIsLessThanArticleAmount), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenTemplateIsExpensesAndBankAccountBalanceIsLessThanArticleAmount_ShouldThrowException(
            RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod, ACC_Account[] accounts, EnumDataModel currencyEnum, ACC_BankAccount bankAccount)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            _accountRepository.Setup(repository => repository.GetListByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(accounts);

            SetupCurrency(currencyEnum);

            _bankAccountRepository.Setup(repository => repository.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(bankAccount);

            // Act
            var result = async () => await _service.RegisterVoucherAsync(request);

            // Assert
            var exceptionAssertions = await result.Should().ThrowAsync<BigBangException>();
            exceptionAssertions.AssertForbidden()
                .AssertMessage(Messages.Error_BankAccountBalanceIsNotEnough);

            VerifyFiscalPeriod(fiscalPeriod.Id);
            VerifyAccounts(request);

            EnumService.Verify(service => service.GetAsync(nameof(Currency)), Times.Once);
            _bankAccountRepository.Verify(repository => repository.GetAsync(request.Articles.Last().LookupId!.Value), Times.Once);
            _accountRepository.Verify(repository => repository.GetListByParentNameAsync(It.IsAny<string>()), Times.Never);
            _personAccountRepository.Verify(repository => repository.GetAsync(It.IsAny<Guid>()), Times.Never);
            _voucherRepository.Verify(repository => repository.GetLastAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Theory]
        [MemberData(nameof(VoucherTestData.GetValidExpensesVoucherRequest), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenTemplateIsExpensesAndRequestIsValid_VoucherShouldBeAdded(
            RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod, ACC_Account[] accounts, EnumDataModel currencyEnum, ACC_BankAccount bankAccount, ACC_VoucherTemplate voucherTemplate)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            _accountRepository.Setup(repository => repository.GetListByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(accounts);

            SetupCurrency(currencyEnum);

            _bankAccountRepository.Setup(repository => repository.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(bankAccount);

            _voucherTemplateRepository.Setup(repository => repository.GetAsync(It.IsAny<byte>()))
                .ReturnsAsync(voucherTemplate);

            // Act
            await _service.RegisterVoucherAsync(request);

            // Assert
            VerifyFiscalPeriod(fiscalPeriod.Id);
            VerifyAccounts(request);

            EnumService.Verify(service => service.GetAsync(nameof(Currency)), Times.Once);
            _voucherTemplateRepository.Verify(repository => repository.GetAsync((byte)request.Template), Times.Once);
            _bankAccountRepository.Verify(repository => repository.GetAsync(request.Articles.Last().LookupId!.Value), Times.Once);
            _voucherRepository.Verify(repository => repository.GetLastAsync(fiscalPeriod.Id), Times.Once);

            _voucherRepository.Verify(repository =>
                repository.AddAsync(It.Is<ACC_Voucher>(voucher => voucher.EffectiveDate == request.EffectiveDate
                                                                  && voucher.Title.StartsWith($"برداشت {request.Articles.Last().Amount:N0}")
                                                                  && voucher.Number.EndsWith("1")
                                                                  && voucher.Description == request.Description
                                                                  && voucher.VoucherTemplate.Id == (byte)request.Template
                                                                  && voucher.Type == VoucherType.Normal
                                                                  && voucher.FiscalPeriod.Id == fiscalPeriod.Id)), Times.Once);

            _bankAccountRepository.Verify(repository => repository.UpdateAsync(It.Is<ACC_BankAccount>(bank => bank.Balance == VoucherTestData.BankAccountBalanceOffset)), Times.Once);
        }

        #endregion

        #region Receive Debt Template

        [Theory]
        [MemberData(nameof(VoucherTestData.GetInvalidReceiveDebtVoucherRequestWithBankAccountIsNull), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenTemplateIsReceiveDebtAndArticleTypeIsDebitAndAccountNameIsBankAndAccountLookupTypeIsBankAndBankAccountIsNull_ShouldThrowException(
            RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod, ACC_Account[] accounts, ACC_PersonAccount personAccount)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            _accountRepository.Setup(repository => repository.GetListByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(accounts);

            _bankAccountRepository.Setup(repository => repository.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(default(ACC_BankAccount));

            _personAccountRepository.Setup(repository => repository.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(personAccount);

            // Act
            var result = async () => await _service.RegisterVoucherAsync(request);

            // Assert
            var exceptionAssertions = await result.Should().ThrowAsync<BigBangException>();
            exceptionAssertions.AssertBadRequest()
                .AssertMessage(string.Format(Messages.Error_FieldRequired, Messages.Entity_BankAccount));

            VerifyFiscalPeriod(fiscalPeriod.Id);
            VerifyAccounts(request);

            _personAccountRepository.Verify(repository => repository.GetAsync(request.Articles.Last().LookupId!.Value), Times.Never);
            _bankAccountRepository.Verify(repository => repository.GetAsync(request.Articles.First().LookupId!.Value), Times.Once);
            _voucherRepository.Verify(repository => repository.GetLastAsync(It.IsAny<Guid>()), Times.Never);
        }

        #endregion

        #region Pay Debt Template

        [Theory]
        [MemberData(nameof(VoucherTestData.GetInvalidPayDebtVoucherRequestWithPersonAccountIsNull), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenTemplateIsPeyDebtAndArticleTypeIsDebitAndAccountLookupTypeIsPersonAndAccountNameIsLiabilitiesToOthersAndPersonAccountIsNull_ShouldThrowException(
            RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod, ACC_Account[] accounts)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            _accountRepository.Setup(repository => repository.GetListByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(accounts);

            _personAccountRepository.Setup(repository => repository.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(default(ACC_PersonAccount));

            // Act
            var result = async () => await _service.RegisterVoucherAsync(request);

            // Assert
            var exceptionAssertions = await result.Should().ThrowAsync<BigBangException>();
            exceptionAssertions.AssertBadRequest()
                .AssertMessage(string.Format(Messages.Error_FieldRequired, Messages.Entity_PersonAccount));

            VerifyFiscalPeriod(fiscalPeriod.Id);
            VerifyAccounts(request);

            _personAccountRepository.Verify(repository => repository.GetAsync(request.Articles.First().LookupId!.Value), Times.Once);
            _voucherRepository.Verify(repository => repository.GetLastAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Theory]
        [MemberData(nameof(VoucherTestData.GetInvalidPayDebtVoucherRequestWithBankAccountIsNull), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenTemplateIsPayDebtAndArticleTypeIsCreditAndAccountNameIsBankAndAccountLookupTypeIsBankAndBankAccountIsNull_ShouldThrowException(
            RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod, ACC_Account[] accounts, ACC_PersonAccount personAccount)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            _accountRepository.Setup(repository => repository.GetListByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(accounts);

            _personAccountRepository.Setup(repository => repository.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(personAccount);

            _bankAccountRepository.Setup(repository => repository.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(default(ACC_BankAccount));

            // Act
            var result = async () => await _service.RegisterVoucherAsync(request);

            // Assert
            var exceptionAssertions = await result.Should().ThrowAsync<BigBangException>();
            exceptionAssertions.AssertBadRequest()
                .AssertMessage(string.Format(Messages.Error_FieldRequired, Messages.Entity_BankAccount));

            VerifyFiscalPeriod(fiscalPeriod.Id);
            VerifyAccounts(request);

            _personAccountRepository.Verify(repository => repository.GetAsync(request.Articles.First().LookupId!.Value), Times.Once);
            _bankAccountRepository.Verify(repository => repository.GetAsync(request.Articles.Last().LookupId!.Value), Times.Once);
            _voucherRepository.Verify(repository => repository.GetLastAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Theory]
        [MemberData(nameof(VoucherTestData.GetInvalidPayDebtVoucherRequestWithBankAccountBalanceIsLessThanArticleAmount), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenTemplateIsPayDebtAndBankAccountBalanceIsLessThanArticleAmount_ShouldThrowException(
            RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod, ACC_Account[] accounts, ACC_PersonAccount personAccount, ACC_BankAccount bankAccount)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            _accountRepository.Setup(repository => repository.GetListByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(accounts);

            _personAccountRepository.Setup(repository => repository.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(personAccount);

            _bankAccountRepository.Setup(repository => repository.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(bankAccount);

            // Act
            var result = async () => await _service.RegisterVoucherAsync(request);

            // Assert
            var exceptionAssertions = await result.Should().ThrowAsync<BigBangException>();
            exceptionAssertions.AssertForbidden()
                .AssertMessage(Messages.Error_BankAccountBalanceIsNotEnough);

            VerifyFiscalPeriod(fiscalPeriod.Id);
            VerifyAccounts(request);

            _personAccountRepository.Verify(repository => repository.GetAsync(request.Articles.First().LookupId!.Value), Times.Once);
            _bankAccountRepository.Verify(repository => repository.GetAsync(request.Articles.Last().LookupId!.Value), Times.Once);
            _voucherRepository.Verify(repository => repository.GetLastAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Theory]
        [MemberData(nameof(VoucherTestData.GetValidPayDebtVoucherRequest), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenTemplateIsPayDebtAndRequestIsValid__VoucherShouldBeAdded(
            RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod, ACC_Account[] accounts, ACC_PersonAccount personAccount, ACC_BankAccount bankAccount, ACC_VoucherTemplate voucherTemplate)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            _accountRepository.Setup(repository => repository.GetListByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(accounts);

            _bankAccountRepository.Setup(repository => repository.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(bankAccount);

            _voucherRepository.Setup(repository => repository.GetLastAsync(It.IsAny<Guid>()))
                .ReturnsAsync(default(ACC_Voucher));

            _personAccountRepository.Setup(repository => repository.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(personAccount);

            _voucherTemplateRepository.Setup(repository => repository.GetAsync(It.IsAny<byte>()))
                .ReturnsAsync(voucherTemplate);

            // Act
            await _service.RegisterVoucherAsync(request);

            // Assert
            VerifyFiscalPeriod(fiscalPeriod.Id);
            VerifyAccounts(request);

            _voucherTemplateRepository.Verify(repository => repository.GetAsync((byte)request.Template), Times.Once);
            _bankAccountRepository.Verify(repository => repository.GetAsync(request.Articles.Last().LookupId!.Value), Times.Once);
            _voucherRepository.Verify(repository => repository.GetLastAsync(fiscalPeriod.Id), Times.Once);

            _voucherRepository.Verify(repository =>
                repository.AddAsync(It.Is<ACC_Voucher>(voucher => voucher.EffectiveDate == request.EffectiveDate
                                                                  && voucher.Title == $"{personAccount.FirstName} , {personAccount.LastName}"
                                                                  && voucher.Number.EndsWith("1")
                                                                  && voucher.Description == request.Description
                                                                  && voucher.VoucherTemplate.Id == (byte)request.Template
                                                                  && voucher.Type == VoucherType.Normal
                                                                  && voucher.FiscalPeriod.Id == fiscalPeriod.Id)), Times.Once);

            _bankAccountRepository.Verify(repository => repository.UpdateAsync(It.Is<ACC_BankAccount>(bank => bank.Balance == VoucherTestData.BankAccountBalanceOffset)), Times.Once);
        }

        #endregion

        #region Cost Template

        [Theory]
        [MemberData(nameof(VoucherTestData.GetInvalidCostVoucherRequestWithLookupIdIsNotNull), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenTemplateIsCostAndArticleTypeIsDebitAndLookupIdIsNotNull_ShouldThrowException(RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            // Act
            var result = async () => await _service.RegisterVoucherAsync(request);

            // Assert
            var exceptionAssertions = await result.Should().ThrowAsync<BigBangException>();
            exceptionAssertions.AssertBadRequest()
                .AssertMessage(string.Format(Messages.Error_ShouldBeNull, Messages.Label_Lookup));

            VerifyFiscalPeriod(fiscalPeriod.Id);

            _voucherRepository.Verify(repository => repository.GetLastAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Theory]
        [MemberData(nameof(VoucherTestData.GetInvalidCostVoucherRequestWithAccountNameIsIncorrect), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenTemplateIsCostAndArticleTypeIsDebitAndAccountNameIsIncorrect_ShouldThrowException(
            RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod, ACC_Account[] accounts, ACC_Account[] subsidiaryAccounts)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            _accountRepository.Setup(repository => repository.GetListByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(accounts);

            _accountRepository.Setup(repository => repository.GetListByParentNameAsync(It.IsAny<string>()))
                .ReturnsAsync(subsidiaryAccounts);

            // Act
            var result = async () => await _service.RegisterVoucherAsync(request);

            // Assert
            var exceptionAssertions = await result.Should().ThrowAsync<BigBangException>();
            exceptionAssertions.AssertBadRequest()
                .AssertMessage(Messages.Error_AccountForTemplateIsNotValid);

            VerifyFiscalPeriod(fiscalPeriod.Id);
            VerifyAccounts(request);

            _accountRepository.Verify(repository => repository.GetListByParentNameAsync(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Expense))), Times.Once);
            _voucherRepository.Verify(repository => repository.GetLastAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Theory]
        [MemberData(nameof(VoucherTestData.GetInvalidCostVoucherRequestWithBankAccountIsNull), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenTemplateIsCostAndArticleTypeIsCreditAndAccountLookupTypeIsBankAndAccountNameIsBankAndBankAccountIsNull_ShouldThrowException(
            RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod, ACC_Account[] accounts, ACC_Account[] subsidiaryCostAccounts)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            _accountRepository.Setup(repository => repository.GetListByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(accounts);

            _bankAccountRepository.Setup(repository => repository.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(default(ACC_BankAccount));

            _accountRepository.Setup(repository => repository.GetListByParentNameAsync(It.IsAny<string>()))
                .ReturnsAsync(subsidiaryCostAccounts);

            // Act
            var result = async () => await _service.RegisterVoucherAsync(request);

            // Assert
            var exceptionAssertions = await result.Should().ThrowAsync<BigBangException>();
            exceptionAssertions.AssertBadRequest()
                .AssertMessage(string.Format(Messages.Error_FieldRequired, Messages.Entity_BankAccount, request.Articles.Last().LookupId));

            VerifyFiscalPeriod(fiscalPeriod.Id);
            VerifyAccounts(request);

            _accountRepository.Verify(repository => repository.GetListByParentNameAsync(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Expense))), Times.Once);
            _bankAccountRepository.Verify(repository => repository.GetAsync(request.Articles.Last().LookupId!.Value), Times.Once);
            _voucherRepository.Verify(repository => repository.GetLastAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Theory]
        [MemberData(nameof(VoucherTestData.GetInvalidCostVoucherRequestWithBankAccountBalanceIsLessThanArticleAmount), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenTemplateIsCostAndBankAccountBalanceIsLessThanArticleAmount_ShouldThrowException(
            RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod, ACC_Account[] accounts, ACC_BankAccount bankAccount, ACC_Account[] subsidiaryCostAccounts)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            _accountRepository.Setup(repository => repository.GetListByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(accounts);

            _bankAccountRepository.Setup(repository => repository.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(bankAccount);

            _accountRepository.Setup(repository => repository.GetListByParentNameAsync(It.IsAny<string>()))
                .ReturnsAsync(subsidiaryCostAccounts);

            // Act
            var result = async () => await _service.RegisterVoucherAsync(request);

            // Assert
            var exceptionAssertions = await result.Should().ThrowAsync<BigBangException>();
            exceptionAssertions.AssertForbidden()
                .AssertMessage(Messages.Error_BankAccountBalanceIsNotEnough);

            VerifyFiscalPeriod(fiscalPeriod.Id);
            VerifyAccounts(request);

            _accountRepository.Verify(repository => repository.GetListByParentNameAsync(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Expense))), Times.Once);
            _bankAccountRepository.Verify(repository => repository.GetAsync(request.Articles.Last().LookupId!.Value), Times.Once);
            _voucherRepository.Verify(repository => repository.GetLastAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Theory]
        [MemberData(nameof(VoucherTestData.GetValidCostVoucherRequest), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenTemplateIsCostAndRequestIsValid_VoucherShouldBeAdded(
            RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod, ACC_Account[] accounts, ACC_BankAccount bankAccount, ACC_Account[] subsidiaryCostAccounts, ACC_VoucherTemplate voucherTemplate)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            _accountRepository.Setup(repository => repository.GetListByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(accounts);

            _bankAccountRepository.Setup(repository => repository.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(bankAccount);

            _accountRepository.Setup(repository => repository.GetListByParentNameAsync(It.IsAny<string>()))
                .ReturnsAsync(subsidiaryCostAccounts);

            _voucherTemplateRepository.Setup(repository => repository.GetAsync(It.IsAny<byte>()))
                .ReturnsAsync(voucherTemplate);

            // Act
            await _service.RegisterVoucherAsync(request);

            // Assert
            VerifyFiscalPeriod(fiscalPeriod.Id);
            VerifyAccounts(request);

            _voucherTemplateRepository.Verify(repository => repository.GetAsync((byte)request.Template), Times.Once);
            _bankAccountRepository.Verify(repository => repository.GetAsync(request.Articles.Last().LookupId!.Value), Times.Once);
            _voucherRepository.Verify(repository => repository.GetLastAsync(fiscalPeriod.Id), Times.Once);
            _accountRepository.Verify(repository => repository.GetListByParentNameAsync(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Expense))), Times.Once);

            _voucherRepository.Verify(repository =>
                repository.AddAsync(It.Is<ACC_Voucher>(voucher => voucher.EffectiveDate == request.EffectiveDate
                                                                  && voucher.Title == $"هزینه بابت {accounts.First().DisplayName}"
                                                                  && voucher.Number.EndsWith("1")
                                                                  && voucher.Description == request.Description
                                                                  && voucher.VoucherTemplate.Id == (byte)request.Template
                                                                  && voucher.Type == VoucherType.Normal
                                                                  && voucher.FiscalPeriod.Id == fiscalPeriod.Id)), Times.Once);

            _bankAccountRepository.Verify(repository => repository.UpdateAsync(It.Is<ACC_BankAccount>(bank => bank.Balance == VoucherTestData.BankAccountBalanceOffset)), Times.Once);
        }

        #endregion

        #region Product Buy/Sell Templates

        [Theory]
        [MemberData(nameof(VoucherTestData.GetProductBuyOrSellVoucherRequestAndFeeIsNullAndQuantityIsNotGreaterThanZero), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenTemplateIsProductBuyOrSellAndFeeIsNullAndQuantityIsNotGreaterThanZero_ShouldThrowException(
            RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod, ACC_Account[] accounts)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            _accountRepository.Setup(repository => repository.GetListByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(accounts);

            // Act
            var result = async () => await _service.RegisterVoucherAsync(request);

            // Assert
            var exceptionAssertions = await result.Should().ThrowAsync<BigBangException>();
            exceptionAssertions.AssertBadRequest()
                .AssertMessage(string.Format(Messages.Error_ShouldNotBeNull, Messages.Label_Fee))
                .AssertMessage(string.Format(Messages.Error_ShouldBeGreaterThan, Messages.Label_Quantity, 0));

            VerifyFiscalPeriod(fiscalPeriod.Id);
            VerifyAccounts(request);

            _voucherRepository.Verify(repository => repository.GetLastAsync(It.IsAny<Guid>()), Times.Never);
        }

        # endregion

        #region Product Buy Template

        [Theory]
        [MemberData(nameof(VoucherTestData.GetProductBuyVoucherRequestWithBankAccountWithdrawNotEqualToTransportCost), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenTemplateIsProductBuyAndBankAccountWithdrawNotEqualToTransportCost_ShouldThrowException(
            RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod, ACC_Account[] accounts)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            _accountRepository.Setup(repository => repository.GetListByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(accounts);

            // Act
            var result = async () => await _service.RegisterVoucherAsync(request);

            // Assert
            var exceptionAssertions = await result.Should().ThrowAsync<BigBangException>();
            exceptionAssertions.AssertBadRequest()
                .AssertMessage(Messages.Error_BankAccountWithdrawShouldEqualToTransportAmount);

            VerifyFiscalPeriod(fiscalPeriod.Id);
            VerifyAccounts(request);

            _voucherRepository.Verify(repository => repository.GetLastAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Theory]
        [MemberData(nameof(VoucherTestData.GetProductBuyVoucherRequestWithTransportationCostDistributionIsIncorrect), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenTemplateIsProductBuyAndTransportationCostDistributionIsIncorrect_ShouldThrowException(
            RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod, ACC_Account[] accounts)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            _accountRepository.Setup(repository => repository.GetListByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(accounts);

            // Act
            var result = async () => await _service.RegisterVoucherAsync(request);

            // Assert
            var exceptionAssertions = await result.Should().ThrowAsync<BigBangException>();
            exceptionAssertions.AssertBadRequest()
                .AssertMessage(Messages.Error_IncorrectDistributionTransportCalculation);

            VerifyFiscalPeriod(fiscalPeriod.Id);
            VerifyAccounts(request);

            _voucherRepository.Verify(repository => repository.GetLastAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Theory]
        [MemberData(nameof(VoucherTestData.GetProductBuyVoucherRequestWithLiabilitiesToOthersNotEqualToTotalProductsPrice), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenTemplateIsProductBuyAndLiabilitiesToOthersNotEqualToTotalProductsPrice_ShouldThrowException(
            RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod, ACC_Account[] accounts)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            _accountRepository.Setup(repository => repository.GetListByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(accounts);

            // Act
            var result = async () => await _service.RegisterVoucherAsync(request);

            // Assert
            var exceptionAssertions = await result.Should().ThrowAsync<BigBangException>();
            exceptionAssertions.AssertBadRequest()
                .AssertMessage(Messages.Error_LiablitiesToOthersShouldEqualToTotalProductsAmount);

            VerifyFiscalPeriod(fiscalPeriod.Id);
            VerifyAccounts(request);

            _voucherRepository.Verify(repository => repository.GetLastAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Theory]
        [MemberData(nameof(VoucherTestData.GetProductBuyVoucherRequestWithBankAccountWithdrawNotEqualToTotalProductsAmountWithoutDetailedAccount), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenTemplateIsProductBuyAndBankAccountWithdrawNotEqualToTotalProductsAmountWithoutDetailedAccount_ShouldThrowException(
            RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod, ACC_Account[] accounts)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            _accountRepository.Setup(repository => repository.GetListByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(accounts);

            // Act
            var result = async () => await _service.RegisterVoucherAsync(request);

            // Assert
            var exceptionAssertions = await result.Should().ThrowAsync<BigBangException>();
            exceptionAssertions.AssertBadRequest()
                .AssertMessage(Messages.Error_BankAccountWithdrawShouldEqualToTotalProductsAmount);

            VerifyFiscalPeriod(fiscalPeriod.Id);
            VerifyAccounts(request);

            _voucherRepository.Verify(repository => repository.GetLastAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Theory]
        [MemberData(nameof(VoucherTestData.GetValidProductBuyVoucherRequest), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenTemplateIsProductBuyAndTransportDistributionIsCorrectWithIrrationalNumbers_VoucherShouldBeAdded(
            RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod, ACC_Account[] accounts, ACC_Product[] products, ACC_BankAccount bankAccount, ACC_VoucherTemplate voucherTemplate)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            _accountRepository.Setup(repository => repository.GetListByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(accounts);

            _bankAccountRepository.Setup(repository => repository.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(bankAccount);

            _productRepository.SetupSequence(repository => repository.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(products[0])
                .ReturnsAsync(products[1]);

            _voucherTemplateRepository.Setup(repository => repository.GetAsync(It.IsAny<byte>()))
                .ReturnsAsync(voucherTemplate);

            // Act
            await _service.RegisterVoucherAsync(request);

            // Assert
            VerifyFiscalPeriod(fiscalPeriod.Id);
            VerifyAccounts(request);

            _bankAccountRepository.Verify(repository => repository.GetAsync(request.Articles.Last().LookupId.GetValueOrDefault()), Times.Once);
            _productRepository.Verify(repository => repository.GetAsync(request.Articles.ElementAt(0).LookupId.GetValueOrDefault()), Times.Once);
            _productRepository.Verify(repository => repository.GetAsync(request.Articles.ElementAt(1).LookupId.GetValueOrDefault()), Times.Once);
            _voucherRepository.Verify(repository => repository.GetLastAsync(fiscalPeriod.Id), Times.Once);
            _voucherTemplateRepository.Verify(repository => repository.GetAsync((byte)request.Template), Times.Once);

            _voucherRepository.Verify(repository =>
                repository.AddAsync(It.Is<ACC_Voucher>(voucher => voucher.EffectiveDate == request.EffectiveDate
                                                                  && voucher.Title == $"بابت خرید {products[0].Name} و ..."
                                                                  && voucher.Number.EndsWith("1")
                                                                  && voucher.Description == request.Description
                                                                  && voucher.VoucherTemplate.Id == (byte)request.Template
                                                                  && voucher.Type == VoucherType.Normal
                                                                  && voucher.FiscalPeriod.Id == fiscalPeriod.Id)), Times.Once);

            _bankAccountRepository.Verify(repository => repository.UpdateAsync(It.Is<ACC_BankAccount>(bank => bank.Balance == 2601)), Times.Once);
        }

        #endregion

        #region Product Sell Template

        [Theory]
        [MemberData(nameof(VoucherTestData.GetProductSellVoucherRequestAndArticleTypeIsCreditAndAccountNameIProductSellAndAccountLookupTypeIsProductAndProductIsNull), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenTemplateIsProductSellAndArticleTypeIsCreditAndAccountNameIProductSellAndAccountLookupTypeIsProductAndProductIsNull_ShouldThrowException(
            RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod, ACC_Account[] accounts, ACC_Product product)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            _accountRepository.Setup(repository => repository.GetListByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(accounts);

            _productRepository.Setup(repository => repository.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(product);

            // Act
            var result = async () => await _service.RegisterVoucherAsync(request);

            // Assert
            var exceptionAssertions = await result.Should().ThrowAsync<BigBangException>();
            exceptionAssertions.AssertBadRequest()
                .AssertMessage(string.Format(Messages.Error_FieldRequired, Messages.Entity_Product));

            VerifyFiscalPeriod(fiscalPeriod.Id);
            VerifyAccounts(request);

            _productRepository.Verify(repository => repository.GetAsync(request.Articles.First().LookupId!.Value), Times.Once);
            _personAccountRepository.Verify(repository => repository.GetAsync(It.IsAny<Guid>()), Times.Never);
            _voucherRepository.Verify(repository => repository.GetLastAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Theory]
        [MemberData(nameof(VoucherTestData.GetProductSellVoucherRequestAndProductStockIsLessThanArticleQuantity), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenTemplateIsProductSellAndProductStockIsLessThanArticleQuantity_ShouldThrowException(
            RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod, ACC_Account[] accounts, ACC_Product product)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            _accountRepository.Setup(repository => repository.GetListByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(accounts);

            _productRepository.Setup(repository => repository.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(product);

            // Act
            var result = async () => await _service.RegisterVoucherAsync(request);

            // Assert
            var exceptionAssertions = await result.Should().ThrowAsync<BigBangException>();
            exceptionAssertions.AssertBadRequest()
                .AssertMessage(Messages.Error_ProductIsNotInStock);

            VerifyFiscalPeriod(fiscalPeriod.Id);
            VerifyAccounts(request);

            _productRepository.Verify(repository => repository.GetAsync(request.Articles.First().LookupId!.Value), Times.Once);
            _personAccountRepository.Verify(repository => repository.GetAsync(It.IsAny<Guid>()), Times.Never);
            _voucherRepository.Verify(repository => repository.GetLastAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Theory]
        [MemberData(nameof(VoucherTestData.GetProductSellVoucherRequestAndReceivablesAmountDoesNotMatchTotalSell), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenTemplateIsProductSellAndReceivablesAmountDoesNotMatchTotalSell_ShouldThrowException(
            RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod, ACC_Account[] accounts)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            _accountRepository.Setup(repository => repository.GetListByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(accounts);

            // Act
            var result = async () => await _service.RegisterVoucherAsync(request);

            // Assert
            var exceptionAssertions = await result.Should().ThrowAsync<BigBangException>();
            exceptionAssertions.AssertBadRequest()
                .AssertMessage(Messages.Error_ReceivableFromOthersShouldEqualToTotalProductsAmount);

            VerifyFiscalPeriod(fiscalPeriod.Id);
            VerifyAccounts(request);

            _bankAccountRepository.Verify(repository => repository.GetAsync(It.IsAny<Guid>()), Times.Never);
            _productRepository.Verify(repository => repository.GetAsync(It.IsAny<Guid>()), Times.Never);
            _personAccountRepository.Verify(repository => repository.GetAsync(It.IsAny<Guid>()), Times.Never);
            _voucherRepository.Verify(repository => repository.GetLastAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Theory]
        [MemberData(nameof(VoucherTestData.GetValidProductSellVoucherRequest), MemberType = typeof(VoucherTestData))]
        public async Task RegisterVoucher_WhenTemplateIsProductSellAndRequestIsValid_VoucherShouldBeAdded(
            RegisterVoucherRequest request, ACC_FiscalPeriod fiscalPeriod, ACC_Account[] accounts, ACC_BankAccount bankAccount, ACC_Product product, ACC_VoucherTemplate voucherTemplate)
        {
            // Arrange
            SetupFiscalPeriod(fiscalPeriod);

            _accountRepository.Setup(repository => repository.GetListByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(accounts);

            _bankAccountRepository.Setup(repository => repository.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(bankAccount);

            _voucherRepository.Setup(repository => repository.GetLastAsync(It.IsAny<Guid>()))
                .ReturnsAsync(default(ACC_Voucher));

            _productRepository.Setup(repository => repository.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(product);

            _voucherTemplateRepository.Setup(repository => repository.GetAsync(It.IsAny<byte>()))
                .ReturnsAsync(voucherTemplate);

            // Act
            await _service.RegisterVoucherAsync(request);

            // Assert
            VerifyFiscalPeriod(fiscalPeriod.Id);
            VerifyAccounts(request);

            _bankAccountRepository.Verify(repository => repository.GetAsync(request.Articles.Last().LookupId.GetValueOrDefault()), Times.Once);
            _productRepository.Verify(repository => repository.GetAsync(request.Articles.First().LookupId.GetValueOrDefault()), Times.Once);
            _voucherRepository.Verify(repository => repository.GetLastAsync(fiscalPeriod.Id), Times.Once);
            _voucherTemplateRepository.Verify(repository => repository.GetAsync((byte)request.Template), Times.Once);

            _productRepository.Verify(repository =>
                repository.UpdateAsync(It.Is<ACC_Product>(products => products.Id == product.Id &&
                                                                      products.Stock == 20 &&
                                                                      products.Name == product.Name &&
                                                                      products.SuggestedSellPrice == 100)), Times.Once);

            _bankAccountRepository.Verify(repository => repository.UpdateAsync(It.Is<ACC_BankAccount>(bank => bank.Balance == 18000)), Times.Once);

            _voucherRepository.Verify(repository =>
                repository.AddAsync(It.Is<ACC_Voucher>(voucher => voucher.EffectiveDate == request.EffectiveDate
                                                                  && voucher.Title == $"فروش کالا {product.Name}"
                                                                  && voucher.Number.EndsWith("1")
                                                                  && voucher.Description == request.Description
                                                                  && voucher.VoucherTemplate.Id == (byte)request.Template
                                                                  && voucher.Type == VoucherType.Normal
                                                                  && voucher.FiscalPeriod.Id == fiscalPeriod.Id)), Times.Once);
        }

        #endregion

        private void VerifyAccounts(RegisterVoucherRequest request)
        {
            _accountRepository.Verify(repository => repository.GetListByIdsAsync(It.Is<IEnumerable<Guid>>(ids =>
                ids.SequenceEqual(request.Articles.Select(article => article.AccountId).Distinct()))), Times.Once);
        }
    }
}