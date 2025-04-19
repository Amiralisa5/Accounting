using System;
using System.Collections.Generic;
using System.Linq;
using BigBang.App.Cloud.ERP.Accounting.Application.PersonAccounts;
using BigBang.App.Cloud.ERP.Accounting.Common.Extensions;
using BigBang.App.Cloud.ERP.Accounting.Common.Helpers;
using BigBang.App.Cloud.ERP.Accounting.Domain.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.PersonAccounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Products;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Reports.InvoiceReport.DTOs;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using BigBang.WebServer.Common.Services.Print;
using BigBang.WebServer.Common.Services.Print.DataSources;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Reports.InvoiceReport
{
    public class InvoiceReportPrintTemplate : IPrintTemplateSource
    {
        private readonly IVoucherRepository _voucherRepository;
        private readonly IPersonAccountRepository _personAccountRepository;
        private readonly IProductRepository _productRepository;
        private readonly IPersonAccountService _personAccountService;


        public InvoiceReportPrintTemplate(IVoucherRepository voucherRepository, IPersonAccountRepository personAccountRepository, IProductRepository productRepository, IPersonAccountService personAccountService)
        {
            _voucherRepository = voucherRepository;
            _personAccountRepository = personAccountRepository;
            _productRepository = productRepository;
            _personAccountService = personAccountService;
        }

        public IPrintDataSource Load(IDictionary<string, object> parameters)
        {
            parameters.TryGetValue("Id", out var id);

            if (!Guid.TryParse(id?.ToString(), out var voucherId))
            {
                throw ExceptionHelper.NotFound(Messages.Entity_Voucher);
            }

            var voucher = _voucherRepository.GetAsync(voucherId).Result;
            var productsArticle = _voucherRepository.GetVoucherInvoiceDataAsync(voucherId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ProductSell))).Result;
            var productItems = _productRepository.GetListByIdsAsync(productsArticle.Select(article => article.LookupId.GetValueOrDefault())).Result;
            var detail = productsArticle.Join(productItems,
                article => article.LookupId,
                product => product.Id,
                (article, product) => new InvoiceDetailReportParameter { ProductName = product.Name, Amount = article.Amount, Quantity = article.Quantity.GetValueOrDefault(), UnitPrice = article.Fee.GetValueOrDefault() }).ToList();

            var buyers = _voucherRepository.GetVoucherInvoiceDataAsync(voucherId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ReceivableFromOthers))).Result;
            var buyer = _personAccountRepository.GetAsync(buyers.FirstOrDefault()!.LookupId.GetValueOrDefault()).Result;

            var ownerPersonAccount = _personAccountService.GetOwnerPersonAccountAsync().Result;
            var persianEffectiveDate = voucher.EffectiveDate.ToPersian();
            var result = new InvoiceReportParameter
            {
                InvoiceDetailReportParameter = detail,
                Description = voucher.Description,
                Buyer = $"{buyer.FirstName} {buyer.LastName}",
                EffectiveDate = persianEffectiveDate,
                Number = $"{persianEffectiveDate.Substring(0, 4)}{persianEffectiveDate.Substring(5, 2)}{voucher.Number.Substring(3)}",
                Seller = $"{ownerPersonAccount.FirstName} {ownerPersonAccount.LastName}",
                Title = voucher.Title,
                Id = voucher.Id,
            };

            return new SinglePrintDataSource { Data = result };
        }
    }
}