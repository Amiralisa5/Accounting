using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.PersonAccounts;
using BigBang.App.Cloud.ERP.Accounting.Application.Products.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Application.Products.Payloads.Validators;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Common;
using BigBang.App.Cloud.ERP.Accounting.Common.Helpers;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.App.Cloud.ERP.Accounting.Domain.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Products;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Identity;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using BigBang.WebServer.Common.Attributes;
using BigBang.WebServer.Common.Exceptions;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Products
{
    [Service(ServiceType = typeof(IProductService), InstanceMode = InstanceMode.Scoped, Requestable = false)]
    internal class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IVoucherService _voucherService;
        private readonly IPersonAccountService _personAccountService;
        private readonly IAccountingIdentityService _accountingIdentityService;
        private readonly IVoucherRepository _voucherRepository;
        private readonly IAccountRepository _accountRepository;

        public ProductService(IAccountingIdentityService accountingIdentityService,
           IVoucherService voucherService,
           IPersonAccountService personAccountService,
           IProductRepository productRepository,
           IVoucherRepository voucherRepository,
           IAccountRepository accountRepository)
        {
            _accountingIdentityService = accountingIdentityService;
            _voucherService = voucherService;
            _productRepository = productRepository;
            _voucherRepository = voucherRepository;
            _accountRepository = accountRepository;
            _personAccountService = personAccountService;
        }

        public async Task<IEnumerable<ProductResponse>> GetProductsAsync()
        {
            var businessId = await _accountingIdentityService.GetBusinessIdAsync();

            var products = await _productRepository.GetListByBusinessIdAsync(businessId);

            return products.Select(product => new ProductResponse(
                product.Id,
                product.Name,
                product.BuyPrice,
                product.SuggestedSellPrice,
                product.Stock
            ));
        }

        public async Task<ProductResponse> GetProductAsync(Guid id)
        {
            var product = await _productRepository.GetAsync(id);
            if (product == null) throw ExceptionHelper.NotFound(Messages.Entity_Product);

            return new ProductResponse(
                product.Id,
                product.Name,
                product.BuyPrice,
                product.SuggestedSellPrice,
                product.Stock
            );
        }

        public async Task<Guid> AddProductAsync(ProductRequest request)
        {
            var validator = new ProductValidator();
            var result = await validator.ValidateAsync(request);

            if (!result.IsValid) throw ExceptionHelper.BadRequest(result.Errors);

            var businessId = await _accountingIdentityService.GetBusinessIdAsync();

            var productExists = await _productRepository.ProductExistsAsync(businessId, request.Name);
            if (productExists) throw ExceptionHelper.BadRequest(string.Format(Messages.Error_ProducExists, Messages.Entity_Product));

            var product = new ACC_Product
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                BuyPrice = request.BuyPrice,
                SuggestedSellPrice = request.SuggestedSellPrice,
                Stock = request.Stock,
                Business = new ACC_Business { Id = businessId }
            };

            await _productRepository.AddAsync(product);

            if (request.BuyPrice > 0)
                await RegisterDefineProductVoucherAsync(product.Id, request.BuyPrice, request.Stock);

            return product.Id;
        }

        public async Task<Guid> UpdateProductAsync(Guid id, ProductRequest request)
        {
            var validator = new ProductValidator();
            var result = await validator.ValidateAsync(request);

            if (!result.IsValid) throw ExceptionHelper.BadRequest(result.Errors);

            var product = await _productRepository.GetAsync(id);
            if (product == null) throw ExceptionHelper.NotFound(Messages.Entity_Product);

            var productExists = await _productRepository.ProductExistsAsync(product.Business.Id, request.Name, id);
            if (productExists) throw ExceptionHelper.BadRequest(string.Format(Messages.Error_ProducExists, Messages.Entity_Product));

            if (request.BuyPrice > 0 && product.BuyPrice == 0)
                await RegisterDefineProductVoucherAsync(product.Id, request.BuyPrice, request.Stock);

            product.Name = request.Name;
            product.BuyPrice = request.BuyPrice;
            product.SuggestedSellPrice = request.SuggestedSellPrice;
            product.Stock = request.Stock;

            await _productRepository.UpdateAsync(product);

            return product.Id;
        }

        public async Task DeleteProductAsync(Guid id)
        {
            var product = await _productRepository.GetAsync(id);

            if (product == null)
                throw new BigBangException(string.Format(Messages.Error_EntityNotFound, Messages.Entity_Product));

            await _productRepository.RemoveAsync(id);
        }

        public async Task<PaginatedDetailsResponse<ProductGrossProfitAndLostResponse>> CalculateGrossProfitAndLossAsync(GrossProfitAndLossRequest request)
        {
            var validator = new GrossProfitAndLossValidator();
            var result = await validator.ValidateAsync(request);

            if (!result.IsValid) throw ExceptionHelper.BadRequest(result.Errors);

            var fiscalPeriodId = await _accountingIdentityService.GetFiscalPeriodIdAsync();
            var businessId = await _accountingIdentityService.GetBusinessIdAsync();

            var sell = await _accountRepository.GetByNameAndFiscalPeriodIdAsync(fiscalPeriodId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ProductSell)));
            var cost = await _accountRepository.GetByNameAndFiscalPeriodIdAsync(fiscalPeriodId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_SubsidiaryCostOfProductSold)));

            var productsAggregator = await _voucherRepository.GetProductsAggregatorDataAsync(request, sell.Id, cost.Id, businessId, request.SortBy, request.SortDirection);

            var items = productsAggregator.Select(productsAggregator => new ProductGrossProfitAndLostResponse(productsAggregator.Id,
                                                                                                              productsAggregator.Name,
                                                                                                              productsAggregator.SellAmount,
                                                                                                              productsAggregator.CostAmount,
                                                                                                              productsAggregator.SellQuantity,
                                                                                                              productsAggregator.Difference))
                                          .ToList();

            var totals = await _voucherRepository.GetTotalAmountByAccountIdAsync(request.From, request.To, fiscalPeriodId, [sell.Id, cost.Id]);
            var totalSellAmount = totals.FirstOrDefault(accountTotalAmount => accountTotalAmount.AccountId == sell.Id && accountTotalAmount.ArticleType == ArticleType.Credit);
            var totalCostAmount = totals.FirstOrDefault(accountTotalAmount => accountTotalAmount.AccountId == cost.Id && accountTotalAmount.ArticleType == ArticleType.Debit);

            return new PaginatedDetailsResponse<ProductGrossProfitAndLostResponse>(request.PageSize, request.PageNumber, items);
        }


        public async Task<GrossProfitAndLostTotalResponse> CalculateGrossProfitAndLossTotalAsync(GrossProfitAndLossTotalRequest request)
        {
            var fiscalPeriodId = await _accountingIdentityService.GetFiscalPeriodIdAsync();

            var sell = await _accountRepository.GetByNameAndFiscalPeriodIdAsync(fiscalPeriodId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ProductSell)));
            var cost = await _accountRepository.GetByNameAndFiscalPeriodIdAsync(fiscalPeriodId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_SubsidiaryCostOfProductSold)));

            var totals = await _voucherRepository.GetTotalAmountByAccountIdAsync(request.From, request.To, fiscalPeriodId, [sell.Id, cost.Id]);
            var totalSellAmount = totals.FirstOrDefault(accountTotalAmount => accountTotalAmount.AccountId == sell.Id && accountTotalAmount.ArticleType == ArticleType.Credit);
            var totalCostAmount = totals.FirstOrDefault(accountTotalAmount => accountTotalAmount.AccountId == cost.Id && accountTotalAmount.ArticleType == ArticleType.Debit);

            return new GrossProfitAndLostTotalResponse(totalSellAmount == null ? 0 : totalSellAmount.TotalQuantity.Value, totalSellAmount == null ? 0 : totalSellAmount.TotalAmount, totalCostAmount == null ? 0 : totalCostAmount.TotalAmount);
        }

        public async Task<PaginatedVouchersListByDetailedAccountResponse> GetProductVouchersAsync(ProductVouchersRequest request)
        {
            var validator = new ProductVouchersRequestValidator();

            var result = await validator.ValidateAsync(request);

            if (!result.IsValid) throw ExceptionHelper.BadRequest(result.Errors);

            var fiscalPeriodId = await _accountingIdentityService.GetFiscalPeriodIdAsync();

            var vouchers = await _voucherRepository.GetListByLookupIdAsync(request.FromDate, request.ToDate,
                request.PageSize, request.PageNumber, request.Id,
                fiscalPeriodId, LookupType.Product) ?? [];

            var voucherResponses = vouchers.Select(voucher => new VoucherByDetailedAccountResponse(voucher.Id, (VoucherTemplate)voucher.VoucherTemplate.Id,
                voucher.Title, voucher.EffectiveDate, voucher.Amount, voucher.GetListByLookupId(request.Id))).ToList();

            var totalCount = await _voucherRepository.GetTotalCountAsync(fiscalPeriodId, request.FromDate, request.ToDate, request.Id, LookupType.Product);

            return new PaginatedVouchersListByDetailedAccountResponse(request.PageSize, request.PageNumber, totalCount, voucherResponses);
        }

        private async Task RegisterDefineProductVoucherAsync(Guid productId, long buyPrice, int quantity)
        {
            var fiscalPeriodId = await _accountingIdentityService.GetFiscalPeriodIdAsync();

            var subsidiaryEquity = await _accountRepository.GetByNameAndFiscalPeriodIdAsync(fiscalPeriodId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_SubsidiaryEquity)));
            var productInventory = await _accountRepository.GetByNameAndFiscalPeriodIdAsync(fiscalPeriodId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ProductInventory)));

            var ownerPersonAccount = await _personAccountService.GetOwnerPersonAccountAsync();

            var amount = buyPrice * quantity;

            await _voucherService.RegisterVoucherAsync(new RegisterVoucherRequest(
                VoucherTemplate.Custom,
                null,
                DateTime.Now,
                null,
                [
                    new ArticleRequest(productInventory.Id, productId, amount, Currency.Rial, quantity, (int)buyPrice, ArticleType.Debit, false),
                    new ArticleRequest(subsidiaryEquity.Id, ownerPersonAccount.Id, amount, Currency.Rial, null, null, ArticleType.Credit, false)
                ]
            ));
        }
    }
}