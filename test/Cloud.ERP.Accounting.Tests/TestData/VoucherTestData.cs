using AutoFixture;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.App.Cloud.ERP.Accounting.Domain.Accounts;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Resources;

namespace Cloud.ERP.Accounting.Tests.TestData
{
    public class VoucherTestData : BaseTestData
    {
        private static readonly VoucherTemplate[] AllTemplates;
        public const long BankAccountBalanceOffset = 1000;

        static VoucherTestData()
        {
            AllTemplates = Enum.GetValues<VoucherTemplate>();
        }

        public static IEnumerable<object[]> GetValidVoucherList(int voucherCount)
        {
            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Create<GetVoucherListRequest>(),
                    Fixture.CreateMany<ACC_Voucher>(voucherCount).ToList()
                }
            };
        }

        public static IEnumerable<object[]> GetValidVoucherDetails()
        {
            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<ACC_Voucher>()
                        .With(voucher => voucher.Articles, Fixture.CreateMany<ACC_Article>(2).ToList())
                        .Create()
                }
            };
        }

        public static IEnumerable<object[]> GetInvalidVoucherRequestWithInvalidTemplate()
        {
            var article1 = GetArticleRequest(ArticleType.Debit, 2000);
            var article2 = GetArticleRequest(ArticleType.Credit, 2000);

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, (VoucherTemplate)9)
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [article1, article2])
                        .Create(),
                    fiscalPeriod
                }
            };
        }

        public static IEnumerable<object[]> GetInvalidVoucherRequestWithLengthyDescription()
        {
            var article1 = GetArticleRequest(ArticleType.Debit, 2000);
            var article2 = GetArticleRequest(ArticleType.Credit, 2000);

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Description, new string('X', 256))
                        .With(request => request.Template, GetRandomVoucherTemplate())
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [article1, article2])
                        .Create(),
                    fiscalPeriod
                }
            };
        }

        public static IEnumerable<object[]> GetInvalidVoucherRequestWithUnbalancedArticles()
        {
            var article1 = GetArticleRequest(ArticleType.Debit, 2000);
            var article2 = GetArticleRequest(ArticleType.Credit, 3000);

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, GetRandomVoucherTemplate())
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [article1, article2])
                        .Create(),
                    fiscalPeriod
                }
            };
        }

        public static IEnumerable<object[]> GetInvalidVoucherRequestWithInvalidArticleType()
        {
            var article1 = GetArticleRequest((ArticleType)4, 2000);
            var article2 = GetArticleRequest(ArticleType.Credit, 3000);

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, GetRandomVoucherTemplate())
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [article1, article2])
                        .Create(),
                    fiscalPeriod
                }
            };
        }

        public static IEnumerable<object[]> GetInvalidVoucherRequestWithOutOfRangeEffectiveDate()
        {
            var article1 = GetArticleRequest(ArticleType.Debit, 2000);
            var article2 = GetArticleRequest(ArticleType.Credit, 2000);

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, GetRandomVoucherTemplate())
                        .With(request => request.EffectiveDate, GetOutOfRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [article1, article2])
                        .Create(),
                    fiscalPeriod
                }
            };
        }

        public static IEnumerable<object[]> GetInvalidVoucherRequestWithNegativeAmounts()
        {
            var article1 = GetArticleRequest(ArticleType.Debit, -2000);
            var article2 = GetArticleRequest(ArticleType.Credit, 2000);

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, GetRandomVoucherTemplate())
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [article1, article2])
                        .Create(),
                    fiscalPeriod
                }
            };
        }

        public static IEnumerable<object[]> GetInvalidVoucherRequestArticlesWithoutAccountId()
        {
            var article1 = GetArticleRequest(ArticleType.Debit, 2000);
            var article2 = GetArticleRequestWithoutAccountId(ArticleType.Credit, 2000);

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, GetRandomVoucherTemplate())
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [article1, article2])
                        .Create(),
                    fiscalPeriod
                }
            };
        }

        public static IEnumerable<object[]> GetInvalidVoucherRequestArticlesWithoutLookupId(VoucherTemplate[] voucherTemplates)
        {
            var article1 = GetArticleRequest(ArticleType.Debit, 2000);
            var article2 = GetArticleRequestWithoutLookupId(ArticleType.Credit, 2000);

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, GetRandomVoucherTemplate(voucherTemplates))
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [article1, article2])
                        .Create(),
                    fiscalPeriod
                }
            };
        }

        public static IEnumerable<object[]> GetInvalidExpensesVoucherRequestWithBankAccountIsNull()
        {
            var articleWithdraw = GetArticleRequest(ArticleType.Debit, 2000);
            var accountWithdraw = GetSubsidiaryAccount(articleWithdraw.AccountId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Withdraw)), default);

            var articleBank = GetArticleRequest(ArticleType.Credit, 2000);
            var accountBank = GetSubsidiaryAccount(articleBank.AccountId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Bank)), LookupType.Bank);

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, VoucherTemplate.Expenses)
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [articleWithdraw, articleBank])
                        .Create(),
                    fiscalPeriod,
                    new[] { accountWithdraw, accountBank },
                    GetCurrencyEnum()
                }
            };
        }

        public static IEnumerable<object[]> GetInvalidExpensesVoucherRequestWithBankAccountBalanceIsLessThanArticleAmount()
        {
            var articleWithdraw = GetArticleRequest(ArticleType.Debit, 2000);
            var accountWithdraw = GetSubsidiaryAccount(articleWithdraw.AccountId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Withdraw)), default);

            var (articleBank, accountBank, bankAccount) = GetBankArticle(ArticleType.Credit, 2000, false);
            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, VoucherTemplate.Expenses)
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [articleWithdraw, articleBank])
                        .Create(),
                    fiscalPeriod,
                    new[] { accountWithdraw, accountBank },
                    GetCurrencyEnum(),
                    bankAccount
                }
            };
        }

        public static IEnumerable<object[]> GetValidExpensesVoucherRequest()
        {
            var articleWithdraw = GetArticleRequest(ArticleType.Debit, 2000);
            var accountWithdraw = GetSubsidiaryAccount(articleWithdraw.AccountId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Withdraw)), default);

            var (articleBank, accountBank, bankAccount) = GetBankArticle(ArticleType.Credit, 2000, true);

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, VoucherTemplate.Expenses)
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [articleWithdraw, articleBank])
                        .Create(),
                    fiscalPeriod,
                    new[] { accountWithdraw, accountBank },
                    GetCurrencyEnum(),
                    bankAccount,
                    GetVoucherTemplate(VoucherTemplate.Expenses)
                }
            };
        }

        public static IEnumerable<object[]> GetInvalidPayDebtVoucherRequestWithPersonAccountIsNull()
        {
            var liabilitiesToOthersArticle = GetArticleRequest(ArticleType.Debit, 2000);
            var liabilitiesToOthersAccount = GetSubsidiaryAccount(liabilitiesToOthersArticle.AccountId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_LiabilitiesToOthers)), LookupType.Person);

            var articleBank = GetArticleRequest(ArticleType.Credit, 2000);
            var accountBank = GetSubsidiaryAccount(articleBank.AccountId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Bank)), LookupType.Bank);

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, VoucherTemplate.PayDebt)
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [liabilitiesToOthersArticle, articleBank])
                        .Create(),
                    fiscalPeriod,
                    new[] { liabilitiesToOthersAccount, accountBank }
                }
            };
        }

        public static IEnumerable<object[]> GetInvalidPayDebtVoucherRequestWithBankAccountIsNull()
        {
            var (liabilitiesToOthersArticle, liabilitiesToOthersAccount, personAccount) = GetPersonArticle(ArticleType.Debit, 2000, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_LiabilitiesToOthers)));

            var articleBank = GetArticleRequest(ArticleType.Credit, 2000);
            var accountBank = GetSubsidiaryAccount(articleBank.AccountId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Bank)), LookupType.Bank);

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, VoucherTemplate.PayDebt)
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [liabilitiesToOthersArticle, articleBank])
                        .Create(),
                    fiscalPeriod,
                    new[] { liabilitiesToOthersAccount, accountBank },
                    personAccount
                }
            };
        }

        public static IEnumerable<object[]> GetInvalidPayDebtVoucherRequestWithBankAccountBalanceIsLessThanArticleAmount()
        {
            var (liabilitiesToOthersArticle, liabilitiesToOthersAccount, personAccount) = GetPersonArticle(ArticleType.Debit, 2000, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_LiabilitiesToOthers)));
            var (articleBank, accountBank, bankAccount) = GetBankArticle(ArticleType.Credit, 2000, false);

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, VoucherTemplate.PayDebt)
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [liabilitiesToOthersArticle, articleBank])
                        .Create(),
                    fiscalPeriod,
                    new[] { liabilitiesToOthersAccount, accountBank },
                    personAccount,
                    bankAccount
                }
            };
        }

        public static IEnumerable<object[]> GetValidPayDebtVoucherRequest()
        {
            var (liabilitiesToOthersArticle, liabilitiesToOthersAccount, personAccount) = GetPersonArticle(ArticleType.Debit, 2000, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_LiabilitiesToOthers)));
            var (articleBank, accountBank, bankAccount) = GetBankArticle(ArticleType.Credit, 2000, true);

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, VoucherTemplate.PayDebt)
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [liabilitiesToOthersArticle, articleBank])
                        .Create(),
                    fiscalPeriod,
                    new[] { liabilitiesToOthersAccount, accountBank },
                    personAccount,
                    bankAccount,
                    GetVoucherTemplate(VoucherTemplate.PayDebt)
                }
            };
        }

        public static IEnumerable<object[]> GetInvalidReceiveDebtVoucherRequestWithBankAccountIsNull()
        {
            var articleBank = GetArticleRequest(ArticleType.Debit, 2000);
            var accountBank = GetSubsidiaryAccount(articleBank.AccountId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Bank)), LookupType.Bank);

            var (receivableFromOthersArticle, receivableFromOthersAccount, personAccount) = GetPersonArticle(ArticleType.Credit, 2000, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ReceivableFromOthers)));

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, VoucherTemplate.ReceiveDebt)
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [articleBank, receivableFromOthersArticle])
                        .Create(),
                    fiscalPeriod,
                    new[] { accountBank, receivableFromOthersAccount },
                    personAccount
                }
            };
        }

        public static IEnumerable<object[]> GetInvalidCostVoucherRequestWithLookupIdIsNotNull()
        {
            var article1 = GetArticleRequest(ArticleType.Debit, 2000);
            var articleBank = GetArticleRequest(ArticleType.Credit, 2000);

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, VoucherTemplate.Cost)
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [article1, articleBank])
                        .Create(),
                    fiscalPeriod
                }
            };
        }

        public static IEnumerable<object[]> GetInvalidCostVoucherRequestWithAccountNameIsIncorrect()
        {
            var article1 = GetArticleRequestWithoutLookupId(ArticleType.Debit, 2000);
            var account1 = GetSubsidiaryAccount(article1.AccountId, default);

            var articleBank = GetArticleRequest(ArticleType.Credit, 2000);
            var accountBank = GetSubsidiaryAccount(articleBank.AccountId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Bank)), LookupType.Bank);

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, VoucherTemplate.Cost)
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [article1, articleBank])
                        .Create(),
                    fiscalPeriod,
                    new[] { account1, accountBank },
                    GetSubsidiaryAccounts()
                }
            };
        }

        public static IEnumerable<object[]> GetInvalidCostVoucherRequestWithBankAccountIsNull()
        {
            var articleSalary = GetArticleRequestWithoutLookupId(ArticleType.Debit, 2000);
            var accountSalary = GetSubsidiaryAccount(articleSalary.AccountId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Salary)), default);

            var articleBank = GetArticleRequest(ArticleType.Credit, 2000);
            var accountBank = GetSubsidiaryAccount(articleBank.AccountId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Bank)), LookupType.Bank);

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, VoucherTemplate.Cost)
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [articleSalary, articleBank])
                        .Create(),
                    fiscalPeriod,
                    new[] { accountSalary, accountBank },
                    GetSubsidiaryAccounts().Append(accountSalary)
                }
            };
        }

        public static IEnumerable<object[]> GetInvalidCostVoucherRequestWithBankAccountBalanceIsLessThanArticleAmount()
        {
            var articleSalary = GetArticleRequestWithoutLookupId(ArticleType.Debit, 2000);
            var accountSalary = GetSubsidiaryAccount(articleSalary.AccountId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Salary)), default);

            var (articleBank, accountBank, bankAccount) = GetBankArticle(ArticleType.Credit, 2000, false);
            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, VoucherTemplate.Cost)
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [articleSalary, articleBank])
                        .Create(),
                    fiscalPeriod,
                    new[] { accountSalary, accountBank },
                    bankAccount,
                    GetSubsidiaryAccounts().Append(accountSalary)
                }
            };
        }

        public static IEnumerable<object[]> GetValidCostVoucherRequest()
        {
            var articleSalary = GetArticleRequestWithoutLookupId(ArticleType.Debit, 2000);
            var accountSalary = GetSubsidiaryAccount(articleSalary.AccountId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Salary)), default);

            var (articleBank, accountBank, bankAccount) = GetBankArticle(ArticleType.Credit, 2000, true);

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, VoucherTemplate.Cost)
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [articleSalary, articleBank])
                        .Create(),
                    fiscalPeriod,
                    new[] { accountSalary, accountBank },
                    bankAccount,
                    GetSubsidiaryAccounts().Append(accountSalary),
                    GetVoucherTemplate(VoucherTemplate.Cost)
                }
            };
        }

        public static IEnumerable<object[]> GetProductBuyOrSellVoucherRequestAndFeeIsNullAndQuantityIsNotGreaterThanZero()
        {
            var voucherTemplate = GetRandomVoucherTemplate([VoucherTemplate.ProductBuy, VoucherTemplate.ProductSell]);
            var accountProductInventory = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ProductInventory)), LookupType.Product);
            var productArticleType = voucherTemplate == VoucherTemplate.ProductBuy ? ArticleType.Debit : ArticleType.Credit;
            var articleProductInventory1 = GetProductArticleRequest(productArticleType, accountProductInventory.Id, 2000, 10, default);
            var articleProductInventory2 = GetProductArticleRequest(productArticleType, accountProductInventory.Id, 3000, 0, 200);

            var bankArticleType = voucherTemplate == VoucherTemplate.ProductBuy ? ArticleType.Credit : ArticleType.Debit;
            var accountBank = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Bank)), LookupType.Bank);
            var articleBank = GetArticleRequest(bankArticleType, accountBank.Id, 5000, false);

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, voucherTemplate)
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [articleProductInventory1, articleProductInventory2, articleBank])
                        .Create(),
                    fiscalPeriod,
                    new[] { accountProductInventory, accountBank }
                }
            };
        }

        public static IEnumerable<object[]> GetProductBuyVoucherRequestWithBankAccountWithdrawNotEqualToTransportCost()
        {
            var accountProductInventory = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ProductInventory)), LookupType.Product);
            var articleProductInventory1 = GetProductArticleRequest(ArticleType.Debit, accountProductInventory.Id, 5150, 5, 1000);
            var articleProductInventory2 = GetProductArticleRequest(ArticleType.Debit, accountProductInventory.Id, 20300, 10, 2000);

            var accountTransport = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Transport)), default);
            var articleTransport1 = GetArticleRequest(ArticleType.Debit, accountTransport.Id, 450, true);
            var articleTransport2 = GetArticleRequest(ArticleType.Credit, accountTransport.Id, 450, true);

            var accountLiabilitiesToOthers = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_LiabilitiesToOthers)), LookupType.Person);
            var articleLiabilitiesToOthers = GetArticleRequest(ArticleType.Credit, accountLiabilitiesToOthers.Id, 24950, false);

            var accountBank = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Bank)), LookupType.Bank);
            var articleBank = GetArticleRequest(ArticleType.Credit, accountBank.Id, 500, false);

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, VoucherTemplate.ProductBuy)
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [articleProductInventory1, articleProductInventory2, articleTransport1, articleTransport2, articleLiabilitiesToOthers, articleBank])
                        .Create(),
                    fiscalPeriod,
                    new[] { accountProductInventory, accountTransport, accountLiabilitiesToOthers, accountBank }
                }
            };
        }

        public static IEnumerable<object[]> GetProductBuyVoucherRequestWithTransportationCostDistributionIsIncorrect()
        {
            var accountProductInventory = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ProductInventory)), LookupType.Product);
            var articleProductInventory = GetProductArticleRequest(ArticleType.Debit, accountProductInventory.Id, 4001, 3, 1000);

            var accountTransport = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Transport)), default);
            var articleTransport1 = GetArticleRequest(ArticleType.Debit, accountTransport.Id, 1000, true);
            var articleTransport2 = GetArticleRequest(ArticleType.Credit, accountTransport.Id, 1000, true);

            var accountLiabilitiesToOthers = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_LiabilitiesToOthers)), LookupType.Person);
            var articleLiabilitiesToOthers = GetArticleRequest(ArticleType.Credit, accountLiabilitiesToOthers.Id, 3000, false);

            var accountBank = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Bank)), LookupType.Bank);
            var articleBank = GetArticleRequest(ArticleType.Credit, accountBank.Id, 1001, false);

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, VoucherTemplate.ProductBuy)
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [articleProductInventory, articleTransport1, articleTransport2, articleLiabilitiesToOthers, articleBank])
                        .Create(),
                    fiscalPeriod,
                    new[] { accountProductInventory, accountTransport, accountLiabilitiesToOthers, accountBank }
                }
            };
        }

        public static IEnumerable<object[]> GetProductBuyVoucherRequestWithLiabilitiesToOthersNotEqualToTotalProductsPrice()
        {
            var accountProductInventory = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ProductInventory)), LookupType.Product);
            var articleProductInventory1 = GetProductArticleRequest(ArticleType.Debit, accountProductInventory.Id, 5150, 5, 1000);
            var articleProductInventory2 = GetProductArticleRequest(ArticleType.Debit, accountProductInventory.Id, 20300, 10, 2000);

            var accountTransport = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Transport)), default);
            var articleTransport1 = GetArticleRequest(ArticleType.Debit, accountTransport.Id, 450, true);
            var articleTransport2 = GetArticleRequest(ArticleType.Credit, accountTransport.Id, 450, true);

            var accountLiabilitiesToOthers = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_LiabilitiesToOthers)), LookupType.Person);
            var articleLiabilitiesToOthers1 = GetArticleRequest(ArticleType.Debit, accountLiabilitiesToOthers.Id, 25450, true);
            var articleLiabilitiesToOthers2 = GetArticleRequest(ArticleType.Credit, accountLiabilitiesToOthers.Id, 25450, true);

            var accountBank = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Bank)), LookupType.Bank);
            var articleBank = GetArticleRequest(ArticleType.Credit, accountBank.Id, 25450, false);

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, VoucherTemplate.ProductBuy)
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [articleProductInventory1, articleProductInventory2, articleTransport1, articleTransport2, articleLiabilitiesToOthers1, articleLiabilitiesToOthers2, articleBank])
                        .Create(),
                    fiscalPeriod,
                    new[] { accountProductInventory, accountTransport, accountLiabilitiesToOthers, accountBank }
                }
            };
        }

        public static IEnumerable<object[]> GetProductBuyVoucherRequestWithBankAccountWithdrawNotEqualToTotalProductsAmountWithoutDetailedAccount()
        {
            var accountProductInventory = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ProductInventory)), LookupType.Product);
            var articleProductInventory1 = GetProductArticleRequest(ArticleType.Debit, accountProductInventory.Id, 15811, 7, 200);
            var articleProductInventory2 = GetProductArticleRequest(ArticleType.Debit, accountProductInventory.Id, 21588, 10, 100);

            var accountTransport = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Transport)), default);
            var articleTransport1 = GetArticleRequest(ArticleType.Debit, accountTransport.Id, 35000, true);
            var articleTransport2 = GetArticleRequest(ArticleType.Credit, accountTransport.Id, 34999, true);

            var accountBank = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Bank)), LookupType.Bank);
            var articleBank = GetArticleRequest(ArticleType.Credit, accountBank.Id, 37400, false);

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, VoucherTemplate.ProductBuy)
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [articleProductInventory1, articleProductInventory2, articleTransport1, articleTransport2, articleBank])
                        .Create(),
                    fiscalPeriod,
                    new[] { accountProductInventory, accountTransport, accountBank }
                }
            };
        }

        public static IEnumerable<object[]> GetValidProductBuyVoucherRequest()
        {
            var accountProductInventory = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ProductInventory)), LookupType.Product);
            var articleProductInventory1 = GetProductArticleRequest(ArticleType.Debit, accountProductInventory.Id, 15811, 7, 200);
            var product1 = GetProduct(articleProductInventory1.LookupId.GetValueOrDefault());
            var articleProductInventory2 = GetProductArticleRequest(ArticleType.Debit, accountProductInventory.Id, 21588, 10, 100);
            var product2 = GetProduct(articleProductInventory2.LookupId.GetValueOrDefault());

            var accountTransport = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Transport)), default);
            var articleTransport1 = GetArticleRequest(ArticleType.Debit, accountTransport.Id, 35000, true);
            var articleTransport2 = GetArticleRequest(ArticleType.Credit, accountTransport.Id, 35000, true);

            var accountLiabilitiesToOthers = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_LiabilitiesToOthers)), LookupType.Person);
            var articleLiabilitiesToOthers1 = GetArticleRequest(ArticleType.Debit, accountLiabilitiesToOthers.Id, 2400, true);
            var articleLiabilitiesToOthers2 = GetArticleRequest(ArticleType.Credit, accountLiabilitiesToOthers.Id, 2400, true);

            var accountBank = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Bank)), LookupType.Bank);
            var articleBank = GetArticleRequest(ArticleType.Credit, accountBank.Id, 37399, false);
            var bankAccount = GetBankAccount(articleBank.LookupId.GetValueOrDefault(), 40000);

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, VoucherTemplate.ProductBuy)
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [articleProductInventory1, articleProductInventory2, articleTransport1, articleTransport2, articleLiabilitiesToOthers1, articleLiabilitiesToOthers2, articleBank])
                        .Create(),
                    fiscalPeriod,
                    new[] { accountProductInventory, accountTransport, accountLiabilitiesToOthers, accountBank },
                    new[] { product1, product2 },
                    bankAccount,
                    GetVoucherTemplate(VoucherTemplate.ProductBuy)
                }
            };
        }

        public static IEnumerable<object[]> GetProductSellVoucherRequestAndArticleTypeIsCreditAndAccountNameIProductSellAndAccountLookupTypeIsProductAndProductIsNull()
        {
            var accountCostOfProductSold = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_SubsidiaryCostOfProductSold)), LookupType.Product);
            var articleCostOfProductSold = GetProductArticleRequest(ArticleType.Debit, accountCostOfProductSold.Id, 1500, 10, 150);

            var accountProductInventory = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ProductInventory)), LookupType.Product);
            var articleProductInventory = GetProductArticleRequest(ArticleType.Credit, accountProductInventory.Id, 1500, 10, 150);

            var accountProductSell = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ProductSell)), LookupType.Bank);
            var articleProductSell = GetProductArticleRequest(ArticleType.Credit, accountProductSell.Id, 2000, 10, 200);

            var accountLiabilitiesToOthers = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ReceivableFromOthers)), LookupType.Person);
            var articleLiabilitiesToOthers = GetArticleRequest(ArticleType.Debit, accountLiabilitiesToOthers.Id, 2000, true);

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, VoucherTemplate.ProductSell)
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [articleProductInventory, articleCostOfProductSold, articleProductSell, articleLiabilitiesToOthers])
                        .Create(),
                    fiscalPeriod,
                    new[] { accountProductInventory, accountCostOfProductSold, accountProductSell, accountLiabilitiesToOthers },
                    default(ACC_Product)!
                }
            };
        }

        public static IEnumerable<object[]> GetProductSellVoucherRequestAndProductStockIsLessThanArticleQuantity()
        {
            var accountCostOfProductSold = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_SubsidiaryCostOfProductSold)), LookupType.Product);
            var articleCostOfProductSold = GetProductArticleRequest(ArticleType.Debit, accountCostOfProductSold.Id, 1500, 10, 150);

            var product = GetProduct(5);

            var accountProductInventory = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ProductInventory)), LookupType.Product);
            var articleProductInventory = GetProductArticleRequest(ArticleType.Credit, accountProductInventory.Id, 1500, 10, 150);

            var accountProductSell = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ProductSell)), LookupType.Bank);
            var articleProductSell = GetProductArticleRequest(ArticleType.Credit, accountProductSell.Id, 2000, 10, 200);

            var accountLiabilitiesToOthers = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ReceivableFromOthers)), LookupType.Person);
            var articleLiabilitiesToOthers = GetArticleRequest(ArticleType.Debit, accountLiabilitiesToOthers.Id, 2000, true);

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, VoucherTemplate.ProductSell)
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [articleProductInventory, articleCostOfProductSold, articleProductSell, articleLiabilitiesToOthers])
                        .Create(),
                    fiscalPeriod,
                    new[] { accountProductInventory, accountCostOfProductSold, accountProductSell, accountLiabilitiesToOthers },
                    product
                }
            };
        }

        public static IEnumerable<object[]> GetProductSellVoucherRequestAndReceivablesAmountDoesNotMatchTotalSell()
        {
            var accountBank = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Bank)), LookupType.Bank);
            var articleBank = GetArticleRequest(ArticleType.Debit, accountBank.Id, 8000, false);

            var accountCostOfProductSold = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_SubsidiaryCostOfProductSold)), LookupType.Product);
            var articleCostOfProductSold = GetProductArticleRequest(ArticleType.Debit, accountCostOfProductSold.Id, 5000, 50, 100);

            var accountProductInventory = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ProductInventory)), LookupType.Product);
            var articleProductInventory = GetProductArticleRequest(ArticleType.Credit, accountProductInventory.Id, 5000, 50, 100);

            var accountProductSell = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ProductSell)), LookupType.Product);
            var articleProductSell = GetProductArticleRequest(ArticleType.Credit, accountProductSell.Id, 8000, 50, 100);

            var accountLiabilitiesToOthers = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ReceivableFromOthers)), LookupType.Person);
            var articleLiabilitiesToOthers1 = GetArticleRequest(ArticleType.Debit, accountLiabilitiesToOthers.Id, 7000, true);
            var articleLiabilitiesToOthers2 = GetArticleRequest(ArticleType.Credit, accountLiabilitiesToOthers.Id, 7000, true);

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, VoucherTemplate.ProductSell)
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [articleProductInventory, articleCostOfProductSold, articleProductSell, articleLiabilitiesToOthers1, articleLiabilitiesToOthers2, articleBank])
                        .Create(),
                    fiscalPeriod,
                    new[] { accountProductInventory, accountCostOfProductSold, accountProductSell, accountLiabilitiesToOthers, accountBank }
                }
            };
        }

        public static IEnumerable<object[]> GetValidProductSellVoucherRequest()
        {
            var accountBank = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Bank)), LookupType.Bank);
            var articleBank = GetArticleRequest(ArticleType.Debit, accountBank.Id, 8000, false);
            var bankAccount = GetBankAccount(articleBank.LookupId.GetValueOrDefault(), 10000);

            var product = GetProduct(70);

            var accountCostOfProductSold = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_SubsidiaryCostOfProductSold)), LookupType.Product);
            var articleCostOfProductSold = GetProductArticleRequest(ArticleType.Debit, accountCostOfProductSold.Id, 5000, 50, 100, product.Id);

            var accountProductInventory = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ProductInventory)), LookupType.Product);
            var articleProductInventory = GetProductArticleRequest(ArticleType.Credit, accountProductInventory.Id, 5000, 50, 100, product.Id);

            var accountProductSell = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ProductSell)), LookupType.Product);
            var articleProductSell = GetProductArticleRequest(ArticleType.Credit, accountProductSell.Id, 8000, 50, 100, product.Id);

            var accountLiabilitiesToOthers = GetSubsidiaryAccount(AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_ReceivableFromOthers)), LookupType.Person);
            var articleLiabilitiesToOthers1 = GetArticleRequest(ArticleType.Debit, accountLiabilitiesToOthers.Id, 8000, true);
            var articleLiabilitiesToOthers2 = GetArticleRequest(ArticleType.Credit, accountLiabilitiesToOthers.Id, 8000, true);

            var fiscalPeriod = GetFiscalPeriod();

            return new List<object[]>
            {
                new object[]
                {
                    Fixture.Build<RegisterVoucherRequest>()
                        .With(request => request.Template, VoucherTemplate.ProductSell)
                        .With(request => request.EffectiveDate, GetInRangeEffectiveDate(fiscalPeriod))
                        .With(request => request.Articles, [articleProductInventory, articleCostOfProductSold, articleProductSell, articleLiabilitiesToOthers1, articleLiabilitiesToOthers2, articleBank])
                        .Create(),
                    fiscalPeriod,
                    new[] { accountProductInventory, accountCostOfProductSold, accountProductSell, accountLiabilitiesToOthers, accountBank },
                    bankAccount,
                    product,
                    GetVoucherTemplate(VoucherTemplate.ProductSell)
                }
            };
        }

        private static DateTime GetInRangeEffectiveDate(ACC_FiscalPeriod fiscalPeriod)
        {
            return fiscalPeriod.FromDate.AddMonths(1);
        }

        private static DateTime GetOutOfRangeEffectiveDate(ACC_FiscalPeriod fiscalPeriod)
        {
            return fiscalPeriod.ToDate.AddDays(1);
        }

        private static ArticleRequest GetArticleRequest(ArticleType type, long amount)
        {
            return Fixture.Build<ArticleRequest>()
                .With(article => article.Type, type)
                .With(article => article.Currency, Currency.Rial)
                .With(article => article.Amount, amount)
                .With(article => article.Quantity, default(int?))
                .With(article => article.Fee, default(int?))
                .With(article => article.IsTransactionalOnly, false)
                .Create();
        }

        private static ArticleRequest GetArticleRequest(ArticleType type, Guid accountId, long amount, bool isTransactional)
        {
            return Fixture.Build<ArticleRequest>()
                .With(article => article.Type, type)
                .With(article => article.Currency, Currency.Rial)
                .With(article => article.Amount, amount)
                .With(article => article.Quantity, default(int?))
                .With(article => article.Fee, default(int?))
                .With(article => article.IsTransactionalOnly, isTransactional)
                .With(request => request.AccountId, accountId)
                .Create();
        }

        private static ArticleRequest GetArticleRequestWithoutAccountId(ArticleType type, long amount)
        {
            return Fixture.Build<ArticleRequest>()
                .With(article => article.AccountId, Guid.Empty)
                .With(article => article.Type, type)
                .With(article => article.Currency, Currency.Rial)
                .With(article => article.Amount, amount)
                .With(article => article.Quantity, default(int?))
                .With(article => article.Fee, default(int?))
                .With(article => article.IsTransactionalOnly, false)
                .Create();
        }

        private static ArticleRequest GetArticleRequestWithoutLookupId(ArticleType type, long amount)
        {
            return Fixture.Build<ArticleRequest>()
                .With(article => article.Type, type)
                .With(article => article.LookupId, default(Guid?))
                .With(article => article.Currency, Currency.Rial)
                .With(article => article.Amount, amount)
                .With(article => article.Quantity, default(int?))
                .With(article => article.Fee, default(int?))
                .With(article => article.IsTransactionalOnly, false)
                .Create();
        }

        private static (ArticleRequest, ACC_Account, ACC_BankAccount) GetBankArticle(ArticleType articleType, long amount, bool isSufficientBalance)
        {
            var articleBank = GetArticleRequest(articleType, amount);
            var accountBank = GetSubsidiaryAccount(articleBank.AccountId, AccountTreeFactory.GetAccountName(nameof(Messages.AccountTree_Bank)), LookupType.Bank);
            var bankAccount = GetBankAccount(articleBank.LookupId.GetValueOrDefault(), amount, isSufficientBalance);

            return (articleBank, accountBank, bankAccount);
        }

        private static (ArticleRequest, ACC_Account, ACC_PersonAccount) GetPersonArticle(ArticleType articleType, long amount, string accountName)
        {
            var article = GetArticleRequest(articleType, amount);
            var account = GetSubsidiaryAccount(article.AccountId, accountName, LookupType.Person);
            var personAccount = GetPersonAccount(article.LookupId.GetValueOrDefault());

            return (article, account, personAccount);
        }

        private static ArticleRequest GetProductArticleRequest(ArticleType type, Guid accountId, long amount, int? quantity, long? fee)
        {
            return Fixture.Build<ArticleRequest>()
                .With(article => article.Type, type)
                .With(article => article.Amount, amount)
                .With(article => article.Quantity, quantity)
                .With(article => article.Fee, fee)
                .With(request => request.AccountId, accountId)
                .Create();
        }

        private static ArticleRequest GetProductArticleRequest(ArticleType type, Guid accountId, long amount, int? quantity, long? fee, Guid? lookupId)
        {
            return Fixture.Build<ArticleRequest>()
                .With(article => article.Type, type)
                .With(article => article.Amount, amount)
                .With(article => article.Quantity, quantity)
                .With(article => article.Fee, fee)
                .With(request => request.AccountId, accountId)
                .With(request => request.LookupId, lookupId)
                .Create();
        }

        private static ACC_Account GetSubsidiaryAccount(Guid id, string name, LookupType lookupType)
        {
            return Fixture.Build<ACC_Account>()
                .With(account => account.Id, id)
                .With(account => account.Name, name)
                .With(account => account.LookupType, lookupType)
                .Create();
        }

        private static ACC_Account GetSubsidiaryAccount(string name, LookupType lookupType)
        {
            return Fixture.Build<ACC_Account>()
                .With(account => account.Name, name)
                .With(account => account.LookupType, lookupType)
                .Create();
        }

        private static ACC_Account GetSubsidiaryAccount(Guid id, LookupType lookupType)
        {
            return Fixture.Build<ACC_Account>()
                .With(account => account.Id, id)
                .With(account => account.LookupType, lookupType)
                .Create();
        }

        private static IEnumerable<ACC_Account> GetSubsidiaryAccounts()
        {
            return Fixture.Build<ACC_Account>()
                .CreateMany(3)
                .ToList();
        }

        private static ACC_BankAccount GetBankAccount(Guid id, long amount, bool isSufficientBalance)
        {
            return Fixture.Build<ACC_BankAccount>()
                .With(account => account.Id, id)
                .With(account => account.Balance, isSufficientBalance ? amount + BankAccountBalanceOffset : amount - BankAccountBalanceOffset)
                .Create();
        }

        private static ACC_BankAccount GetBankAccount(Guid id, long balance)
        {
            return Fixture.Build<ACC_BankAccount>()
                .With(account => account.Id, id)
                .With(account => account.Balance, balance)
                .Create();
        }

        private static ACC_PersonAccount GetPersonAccount(Guid id)
        {
            return Fixture.Build<ACC_PersonAccount>()
                .With(person => person.Id, id)
                .Create();
        }

        private static ACC_Product GetProduct(Guid id)
        {
            return Fixture.Build<ACC_Product>()
                .With(product => product.Id, id)
                .Create();
        }

        private static ACC_Product GetProduct(int stock)
        {
            return Fixture.Build<ACC_Product>()
                .With(product => product.Stock, stock)
                .Create();
        }

        private static VoucherTemplate GetRandomVoucherTemplate()
        {
            var index = Random.Next(0, AllTemplates.Length);
            return AllTemplates[index];
        }

        private static VoucherTemplate GetRandomVoucherTemplate(IReadOnlyList<VoucherTemplate> voucherTemplates)
        {
            var index = Random.Next(0, voucherTemplates.Count);
            return voucherTemplates[index];
        }

        private static ACC_VoucherTemplate GetVoucherTemplate(VoucherTemplate voucherTemplate)
        {
            var title = voucherTemplate switch
            {
                VoucherTemplate.Expenses => "برداشت #AmountInCurrency#",
                VoucherTemplate.PayDebt => "#FirstName# , #LastName#",
                VoucherTemplate.Cost => "هزینه بابت #Holder#",
                VoucherTemplate.ProductBuy => "بابت خرید #GoodsName#",
                VoucherTemplate.ProductSell => "فروش کالا #GoodsName#",
                _ => string.Empty
            };

            return Fixture.Build<ACC_VoucherTemplate>()
                .With(voucher => voucher.TitleFormat, title)
                .With(voucherTemp => voucherTemp.Id, (int)voucherTemplate)
                .Create();
        }
    }
}
