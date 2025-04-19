using System;
using BigBang.App.Cloud.ERP.Accounting.Application.Enums;
using BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers.Strategies;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.BankAccounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.PersonAccounts;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Products;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.VoucherTemplates;

namespace BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers
{
    public class TemplateStrategyFactory
    {
        private readonly IProductRepository _productRepository;
        private readonly IVoucherRepository _voucherRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IBankAccountRepository _bankAccountRepository;
        private readonly IPersonAccountRepository _personAccountRepository;
        private readonly IVoucherTemplateRepository _voucherTemplateRepository;
        private readonly IEnumService _enumService;

        public TemplateStrategyFactory(
            IVoucherRepository voucherRepository,
            IAccountRepository accountRepository,
            IBankAccountRepository bankAccountRepository,
            IPersonAccountRepository personAccountRepository,
            IVoucherTemplateRepository voucherTemplateRepository,
            IProductRepository productRepository,
            IEnumService enumService)
        {
            _voucherRepository = voucherRepository;
            _accountRepository = accountRepository;
            _bankAccountRepository = bankAccountRepository;
            _personAccountRepository = personAccountRepository;
            _voucherTemplateRepository = voucherTemplateRepository;
            _productRepository = productRepository;
            _enumService = enumService;
        }

        public ITemplateStrategy Create(VoucherTemplate? voucherTemplate)
        {
            switch (voucherTemplate)
            {
                case VoucherTemplate.Expenses:
                    return new ExpensesTemplateStrategy(
                        _voucherRepository,
                        _accountRepository,
                        _bankAccountRepository,
                        _voucherTemplateRepository,
                        _enumService);

                case VoucherTemplate.Cost:
                    return new CostTemplateStrategy(
                        _voucherRepository,
                        _accountRepository,
                        _bankAccountRepository,
                        _voucherTemplateRepository);

                case VoucherTemplate.PayDebt:
                    return new PayDebtTemplateStrategy(
                        _voucherRepository,
                        _accountRepository,
                        _bankAccountRepository,
                        _personAccountRepository,
                        _voucherTemplateRepository);

                case VoucherTemplate.Deposit:
                    return new DepositTemplateStrategy(
                        _voucherRepository,
                        _accountRepository,
                        _bankAccountRepository,
                        _voucherTemplateRepository,
                        _enumService);

                case VoucherTemplate.ProductSell:
                    return new ProductSellTemplateStrategy(
                        _voucherRepository,
                        _accountRepository,
                        _voucherTemplateRepository,
                        _productRepository,
                        _bankAccountRepository);

                case VoucherTemplate.ProductBuy:
                    return new ProductBuyTemplateStrategy(
                        _voucherRepository,
                        _accountRepository,
                        _bankAccountRepository,
                        _productRepository,
                        _voucherTemplateRepository);

                case VoucherTemplate.ReceiveDebt:
                    return new ReceiveDebtTemplateStrategy(
                        _voucherRepository,
                        _accountRepository,
                        _bankAccountRepository,
                        _personAccountRepository,
                        _voucherTemplateRepository);

                case VoucherTemplate.Custom:
                    return new CustomTemplateStrategy(
                        _voucherRepository,
                        _accountRepository,
                        _bankAccountRepository,
                        _personAccountRepository,
                        _voucherTemplateRepository,
                        _productRepository);

                case VoucherTemplate.AdvanceReceipt:
                    return new AdvanceReceiptTemplateStrategy(
                        _voucherRepository,
                        _accountRepository,
                        _bankAccountRepository,
                        _personAccountRepository,
                        _voucherTemplateRepository);

                case VoucherTemplate.AdvancePayment:
                    return new AdvancePaymentTemplateStrategy(
                        _voucherRepository,
                        _accountRepository,
                        _bankAccountRepository,
                        _personAccountRepository,
                        _voucherTemplateRepository);

                default:
                    throw new NotImplementedException();
            }
        }
    }
}