using AutoFixture;
using BigBang.App.Cloud.ERP.Accounting.Application.Enums;
using BigBang.App.Cloud.ERP.Accounting.Application.PersonAccounts;
using BigBang.App.Cloud.ERP.Accounting.Application.PersonAccounts.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Application.Products;
using BigBang.App.Cloud.ERP.Accounting.Application.Products.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.FiscalPeriods;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Products;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Identity;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using BigBang.WebServer.Common.Exceptions;
using FluentAssertions;
using Moq;

namespace Cloud.ERP.Accounting.Tests.Services
{
    public class ProductServiceTests : BaseServiceTests
    {
        private readonly Mock<IProductRepository> _productRepository;
        private readonly Mock<IVoucherRepository> _voucherRepository;
        private readonly Mock<IVoucherService> _voucherService;
        private readonly Mock<IAccountRepository> _accountRepository;
        private readonly Mock<IPersonAccountService> _personAccountService;
        private readonly ProductService _service;

        public ProductServiceTests() : base(new Mock<IAccountingIdentityService>(),
            new Mock<IFiscalPeriodRepository>(),
            new Mock<IEnumService>())
        {
            _voucherService = new Mock<IVoucherService>();
            _productRepository = new Mock<IProductRepository>();
            _voucherRepository = new Mock<IVoucherRepository>();
            _accountRepository = new Mock<IAccountRepository>();
            _personAccountService = new Mock<IPersonAccountService>();

            _service = new ProductService(AccountingIdentityService.Object,
                 _voucherService.Object,
                 _personAccountService.Object,
                 _productRepository.Object,
                 _voucherRepository.Object,
                 _accountRepository.Object);
        }

        [Fact]
        public async Task GetProducts_ValidBusiness_ProductsShouldBeReturned()
        {
            // Arrange
            var products = Fixture.CreateMany<ACC_Product>(3).ToList();
            var businessId = Guid.NewGuid();
            AccountingIdentityService.Setup(service => service.GetBusinessIdAsync())
                .ReturnsAsync(businessId);
            _productRepository.Setup(repo => repo.GetListByBusinessIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(products);

            // Act
            var result = await _service.GetProductsAsync();

            // Assert
            result.Should().NotBeNull().And.HaveCount(3);
            AccountingIdentityService.Verify(service => service.GetBusinessIdAsync(), Times.Once);
        }

        [Fact]
        public async Task AddProduct_ValidRequest_AProductShouldBeAdded()
        {
            // Arrange
            var request = Fixture.Build<ProductRequest>()
                                  .With(request => request.Name, "Phone")
                                  .With(request => request.BuyPrice, 100)
                                  .With(request => request.SuggestedSellPrice, 150)
                                  .With(request => request.Stock, 10)
                                  .Create();

            var businessId = Guid.NewGuid();
            AccountingIdentityService.Setup(service => service.GetBusinessIdAsync())
                .ReturnsAsync(businessId);

            _productRepository.Setup(repo => repo.ProductExistsAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            ACC_Product? addedProduct = null;
            _productRepository.Setup(repo => repo.AddAsync(It.IsAny<ACC_Product>()))
                .Callback<ACC_Product>(product => addedProduct = product);


            AccountingIdentityService.Setup(service => service.GetFiscalPeriodIdAsync())
                .ReturnsAsync(Guid.NewGuid());

            var subsidiaryEquity = Fixture.Create<ACC_Account>();
            var productInventory = Fixture.Create<ACC_Account>();

            _accountRepository.SetupSequence(repo => repo.GetByNameAndFiscalPeriodIdAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(subsidiaryEquity)
                .ReturnsAsync(productInventory);

            _voucherService.Setup(service => service.RegisterVoucherAsync(It.IsAny<RegisterVoucherRequest>()))
               .ReturnsAsync(Fixture.Create<VoucherResponse>());

            var ownerPersonAccount = Fixture.Create<PersonAccountResponse>();

            _personAccountService.Setup(service => service.GetOwnerPersonAccountAsync())
                .ReturnsAsync(ownerPersonAccount);

            var amount = request.BuyPrice * request.Stock;

            // Act
            var result = await _service.AddProductAsync(request);

            // Assert
            result.Should().NotBeEmpty();

            _productRepository.Verify(repo => repo.AddAsync(It.Is<ACC_Product>(product =>
                product.Name == request.Name &&
                product.BuyPrice == request.BuyPrice &&
                product.SuggestedSellPrice == request.SuggestedSellPrice &&
                product.Stock == request.Stock)), Times.Once);

            _voucherService.Verify(service => service.RegisterVoucherAsync(
                It.Is<RegisterVoucherRequest>(
                    registerVoucherRequest => registerVoucherRequest.Template == VoucherTemplate.Custom
                                              && registerVoucherRequest.FileId == null
                                              && registerVoucherRequest.Description == null
                                              && registerVoucherRequest.Articles.First().AccountId == productInventory.Id
                                              && registerVoucherRequest.Articles.First().LookupId == addedProduct.Id
                                              && registerVoucherRequest.Articles.First().Amount == amount
                                              && registerVoucherRequest.Articles.First().Fee == request.BuyPrice
                                              && registerVoucherRequest.Articles.First().Quantity == request.Stock
                                              && registerVoucherRequest.Articles.First().Type == ArticleType.Debit
                                              && registerVoucherRequest.Articles.First().Currency == Currency.Rial
                                              && registerVoucherRequest.Articles.First().IsTransactionalOnly == false
                                              && registerVoucherRequest.Articles.Last().AccountId == subsidiaryEquity.Id
                                              && registerVoucherRequest.Articles.Last().LookupId == ownerPersonAccount.Id
                                              && registerVoucherRequest.Articles.Last().Amount == amount
                                              && registerVoucherRequest.Articles.Last().Fee == null
                                              && registerVoucherRequest.Articles.Last().Quantity == null
                                              && registerVoucherRequest.Articles.Last().Type == ArticleType.Credit
                                              && registerVoucherRequest.Articles.Last().Currency == Currency.Rial
                                              && registerVoucherRequest.Articles.Last().IsTransactionalOnly == false
                    )), Times.Once);

            addedProduct?.Name.Should().Be(request.Name);
            addedProduct?.BuyPrice.Should().Be(request.BuyPrice);
            addedProduct?.SuggestedSellPrice.Should().Be(request.SuggestedSellPrice);
            addedProduct?.Stock.Should().Be(request.Stock);
            addedProduct?.Business.Id.Should().Be(businessId);
        }

        [Fact]
        public async Task AddProduct_DuplicateName_AValidationExceptionShouldBeThrown()
        {
            // Arrange
            var request = Fixture.Build<ProductRequest>().Create();

            var businessId = Guid.NewGuid();
            AccountingIdentityService.Setup(service => service.GetBusinessIdAsync())
                .ReturnsAsync(businessId);

            _productRepository.Setup(repo => repo.ProductExistsAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            Func<Task> func = async () => await _service.AddProductAsync(request);

            // Assert
            await func.Should().ThrowAsync<BigBangException>()
                .WithMessage(string.Format(Messages.Error_ProducExists, Messages.Entity_Product));
        }

        [Fact]
        public async Task UpdateProduct_ValidIdRequest_AProductShouldBeUpdated()
        {
            // Arrange
            var product = Fixture.Build<ACC_Product>()
                .With(product => product.BuyPrice, 0)
                .Create();
            var request = Fixture.Build<ProductRequest>()
                .With(request => request.BuyPrice, 100)
                .Create();

            _productRepository.Setup(repo => repo.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(product);

            _productRepository.Setup(repo => repo.ProductExistsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>()))
                .ReturnsAsync(false);

            AccountingIdentityService.Setup(service => service.GetFiscalPeriodIdAsync())
                .ReturnsAsync(Guid.NewGuid());

            var subsidiaryEquity = Fixture.Create<ACC_Account>();
            var productInventory = Fixture.Create<ACC_Account>();

            _accountRepository.SetupSequence(repo => repo.GetByNameAndFiscalPeriodIdAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(subsidiaryEquity)
                .ReturnsAsync(productInventory);

            _voucherService.Setup(service => service.RegisterVoucherAsync(It.IsAny<RegisterVoucherRequest>()))
                .ReturnsAsync(Fixture.Create<VoucherResponse>());

            var ownerPersonAccount = Fixture.Create<PersonAccountResponse>();

            _personAccountService.Setup(service => service.GetOwnerPersonAccountAsync())
                .ReturnsAsync(ownerPersonAccount);

            var amount = request.Stock * request.BuyPrice;

            // Act
            var result = await _service.UpdateProductAsync(product.Id, request);

            // Assert
            result.Should().Be(product.Id);

            _productRepository.Verify(repo => repo
                .UpdateAsync(It.Is<ACC_Product>(accProduct => accProduct.Id == product.Id
                                                                    && accProduct.Name == request.Name
                                                                    && accProduct.Stock == request.Stock
                                                                    && accProduct.BuyPrice == request.BuyPrice
                                                                    && accProduct.SuggestedSellPrice == request.SuggestedSellPrice
                )), Times.Once);

            _voucherService.Verify(service => service.RegisterVoucherAsync(
                It.Is<RegisterVoucherRequest>(
                    registerVoucherRequest => registerVoucherRequest.Template == VoucherTemplate.Custom
                                              && registerVoucherRequest.FileId == null
                                              && registerVoucherRequest.Description == null
                                              && registerVoucherRequest.Articles.First().AccountId == productInventory.Id
                                              && registerVoucherRequest.Articles.First().LookupId == product.Id
                                              && registerVoucherRequest.Articles.First().Amount == amount
                                              && registerVoucherRequest.Articles.First().Fee == request.BuyPrice
                                              && registerVoucherRequest.Articles.First().Quantity == request.Stock
                                              && registerVoucherRequest.Articles.First().Type == ArticleType.Debit
                                              && registerVoucherRequest.Articles.First().Currency == Currency.Rial
                                              && registerVoucherRequest.Articles.First().IsTransactionalOnly == false
                                              && registerVoucherRequest.Articles.Last().AccountId == subsidiaryEquity.Id
                                              && registerVoucherRequest.Articles.Last().LookupId == ownerPersonAccount.Id
                                              && registerVoucherRequest.Articles.Last().Amount == amount
                                              && registerVoucherRequest.Articles.Last().Fee == null
                                              && registerVoucherRequest.Articles.Last().Quantity == null
                                              && registerVoucherRequest.Articles.Last().Type == ArticleType.Credit
                                              && registerVoucherRequest.Articles.Last().Currency == Currency.Rial
                                              && registerVoucherRequest.Articles.Last().IsTransactionalOnly == false
                    )), Times.Once);
        }

        [Fact]
        public async Task UpdateProduct_DuplicateName_AValidationExceptionShouldBeThrown()
        {
            // Arrange
            var product = Fixture.Create<ACC_Product>();
            var request = Fixture.Build<ProductRequest>().Create();

            _productRepository.Setup(repo => repo.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(product);

            _productRepository.Setup(repo => repo.ProductExistsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>()))
                .ReturnsAsync(true);

            // Act
            Func<Task> func = async () => await _service.UpdateProductAsync(product.Id, request);

            // Assert
            await func.Should().ThrowAsync<BigBangException>()
                .WithMessage(string.Format(Messages.Error_ProducExists, Messages.Entity_Product));
        }

        [Fact]
        public async Task DeleteProduct_ValidId_AProductShouldBeDeleted()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var product = Fixture.Create<ACC_Product>();

            _productRepository.Setup(repo => repo.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(product);

            _productRepository.Setup(repo => repo.RemoveAsync(It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.DeleteProductAsync(productId);

            // Assert
            _productRepository.Verify(repo => repo.RemoveAsync(productId), Times.Once);
        }

        [Fact]
        public async Task DeleteProduct_InvalidId_AValidationExceptionShouldBeThrown()
        {
            // Arrange
            _productRepository.Setup(repo => repo.GetAsync(It.IsAny<Guid>()))
                .ReturnsAsync(default(ACC_Product));

            // Act
            var func = async () => await _service.DeleteProductAsync(Guid.NewGuid());

            // Assert
            await func.Should().ThrowAsync<BigBangException>()
                .WithMessage(string.Format(Messages.Error_EntityNotFound, Messages.Entity_Product));
        }

        [Fact]
        public async Task GetProductVouchers_ValidRequest_VouchersShouldBeReturned()
        {
            // Arrange
            var request = Fixture.Build<ProductVouchersRequest>()
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
            var result = await _service.GetProductVouchersAsync(request);

            // Assert
            _voucherRepository.Verify(repo => repo.GetListByLookupIdAsync(request.FromDate, request.ToDate,
                request.PageSize, request.PageNumber, request.Id, fiscalPeriodId, LookupType.Product), Times.Once);

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
        public async Task GetProductVouchers_EmptyResult_EmptyListShouldBeReturned()
        {
            // Arrange
            var request = Fixture.Build<ProductVouchersRequest>()
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
            var result = await _service.GetProductVouchersAsync(request);

            // Assert
            _voucherRepository.Verify(repo => repo.GetListByLookupIdAsync(request.FromDate, request.ToDate,
                request.PageSize, request.PageNumber, request.Id, fiscalPeriodId, LookupType.Product), Times.Once);

            result.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task GetProductVouchers_WhenFromDateIsBiggerThanToDate_ShouldThrowException()
        {
            // Arrange
            var request = Fixture.Build<ProductVouchersRequest>()
                .With(request => request.FromDate, DateTime.Now.AddDays(1))
                .With(request => request.ToDate, DateTime.Now)
                .Create();

            // Act
            var result = async () => await _service.GetProductVouchersAsync(request);

            // Assert
            await result.Should().ThrowAsync<BigBangException>().WithMessage(Messages.Error_FromDateToDateIsNotValid);
        }
    }
}