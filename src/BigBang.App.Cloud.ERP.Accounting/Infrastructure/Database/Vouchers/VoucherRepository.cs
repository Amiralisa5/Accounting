using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Linq;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Application.Accounts.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Application.Products.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers.Dtos;
using BigBang.WebServer.Common;
using BigBang.WebServer.Common.Attributes;
using BigBang.WebServer.Common.Services;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.SqlCommand;
using NHibernate.Transform;

namespace BigBang.App.Cloud.ERP.Accounting.Infrastructure.Database.Vouchers
{
    [Service(ServiceType = typeof(IVoucherRepository), InstanceMode = InstanceMode.Scoped, Requestable = false)]
    internal class VoucherRepository : BaseRepository<ACC_Voucher, Guid>, IVoucherRepository
    {
        public VoucherRepository(ISessionLoader sessionLoader) : base(sessionLoader)
        {

        }

        public async Task<bool> ArticlesExistAsync(LookupType lookupType, Guid lookupId)
        {
            var exists = await Session.QueryOver<ACC_Article>()
                .Where(article => article.LookupId == lookupId)
                .JoinQueryOver(article => article.Account)
                .Where(account => account.LookupType == lookupType)
                .RowCountAsync();

            return exists > 0;
        }

        public async Task<ACC_Voucher> GetLastAsync(Guid fiscalPeriodId)
        {
            return await Session.QueryOver<ACC_Voucher>()
                .Where(voucher => voucher.FiscalPeriod.Id == fiscalPeriodId)
                .OrderBy(voucher => voucher.CreationDate)
                .Desc
                .Take(1)
                .SingleOrDefaultAsync();
        }

        public async Task<IList<ACC_Voucher>> GetListAsync(Guid fiscalPeriodId, VoucherTemplate template, int pageSize, int pageNumber)
        {
            var skip = (pageNumber - 1) * pageSize;
            return await Session.QueryOver<ACC_Voucher>()
                .Where(voucher => voucher.FiscalPeriod.Id == fiscalPeriodId && voucher.VoucherTemplate.Id == (byte)template)
                .OrderBy(voucher => voucher.EffectiveDate)
                .Desc
                .Skip(skip)
                .Take(pageSize)
                .ListAsync();
        }

        public async Task<long> GetTotalDebtsAsync(Guid fiscalPeriodId, Guid lookupId, LookupType lookupType, ArticleType articleType, string accountName)
        {
            ACC_Article articleAlias = null;
            ACC_Voucher voucherAlias = null;
            ACC_Account accountAlias = null;

            return await Session.QueryOver(() => articleAlias)
                                .Where(() => articleAlias.LookupId == lookupId && articleAlias.Type == articleType)
                                .Inner
                                .JoinAlias(() => articleAlias.Account, () => accountAlias)
                                .Where(() => accountAlias.Name == accountName && accountAlias.LookupType == lookupType)
                                .Inner
                                .JoinAlias(() => articleAlias.Voucher, () => voucherAlias)
                                .Where(() => voucherAlias.FiscalPeriod.Id == fiscalPeriodId)
                                .Select(Projections.Sum(() => articleAlias.Amount))
                                .SingleOrDefaultAsync<long>();
        }


        public async Task<Guid?> GetFileIdAsync(Guid voucherId)
        {
            ACC_Voucher voucherAlias = null;
            ACC_File fileAlias = null;

            var file = await Session.QueryOver(() => voucherAlias)
                .Where(() => voucherAlias.Id == voucherId)
                .JoinEntityAlias(() => fileAlias, new EqPropertyExpression(Projections.Property(() => voucherAlias.Id), Projections.Property(() => fileAlias.EntityOwnerId)))
                .Take(1)
                .SingleOrDefaultAsync();

            return file?.Id;
        }

        public async Task<IList<ProductAggregatorDto>> GetProductsAggregatorDataAsync(
         GrossProfitAndLossRequest request,
         Guid sellAccountId,
         Guid costOfProductSoldAccountId,
         Guid businessId,
         string sortBy,
         SortDirection sortDirection)
        {
            var take = request.PageSize;
            var skip = (request.PageNumber - 1) * request.PageSize;
            var idCondition = !request.Ids.Any() ? string.Empty : $"AND Product.Id IN ({string.Join(", ", request.Ids.Select(g => $"'{g}'"))})";

            var query = Session.CreateSQLQuery($@"SELECT 
                                                      Product.Id,
                                                      Product.Name,
                                                      ISNULL(SUM(sells.Quantity), 0) AS SellQuantity,
                                                      ISNULL(SUM(sells.Amount), 0) AS SellAmount,
                                                      ISNULL(SUM(costs.Quantity), 0) AS CostQuantity,
                                                      ISNULL(SUM(costs.Amount), 0) AS CostAmount,
                                                      ISNULL(SUM(sells.Amount), 0) - ISNULL(SUM(costs.Amount), 0) AS DifferenceAmount
                                                  FROM 
                                                      [BigBang_Cloud.ERP.Accounting].dbo.Product
                                                      LEFT JOIN 
                                                          (SELECT
                                                              Article.LookupId,
                                                              Article.Quantity,
                                                              Article.Amount
                                                           FROM
                                                             [BigBang_Cloud.ERP.Accounting].dbo.Article 
                                                              INNER JOIN [BigBang_Cloud.ERP.Accounting].dbo.Voucher 
                                                                  ON Voucher.Id = Article.VoucherId
                                                                  AND (Voucher.EffectiveDate >= Cast(:from as datetime))
                                                                  AND (Voucher.EffectiveDate <= Cast(:to  as datetime))
                                                                  AND AccountId = :sellAccountId
                                                                  AND Article.Type = 2
                                                          ) sells 
                                                          ON sells.LookupId = Product.Id 
                                                      LEFT JOIN 
                                                          (SELECT
                                                              Article.LookupId,
                                                              Article.Quantity,
                                                              Article.Amount
                                                           FROM
                                                              [BigBang_Cloud.ERP.Accounting].dbo.Article 
                                                              INNER JOIN  [BigBang_Cloud.ERP.Accounting].dbo.Voucher 
                                                                  ON Voucher.Id = Article.VoucherId
                                                                  AND (Voucher.EffectiveDate >= Cast(:from as datetime))
                                                                  AND (Voucher.EffectiveDate <= Cast(:to  as datetime))
                                                                  AND AccountId = :costOfProductSoldAccountId
                                                                  AND Article.Type = 1
                                                          ) costs 
                                                          ON costs.LookupId = Product.Id
                                                  WHERE
                                                        Product.BusinessId = :businessId {idCondition}
                                                  GROUP BY 
                                                        Product.Id,
                                                        Product.Name
                                                  ORDER BY 
                                                            {sortBy} {sortDirection}
                                                  OFFSET :skip ROWS
                                                  FETCH NEXT :take ROWS ONLY ");

            query.SetParameter("from", request.From);
            query.SetParameter("to", request.To);
            query.SetParameter("sellAccountId", sellAccountId);
            query.SetParameter("costOfProductSoldAccountId", costOfProductSoldAccountId);
            query.SetParameter("skip", skip);
            query.SetParameter("take", take);
            query.SetParameter("businessId", businessId);
            var result = await query.ListAsync<object[]>();

            return result.Select(row => new ProductAggregatorDto
            {
                Id = (Guid)row[0],
                Name = (string)row[1],
                SellQuantity = Convert.ToInt32(row[2]),
                SellAmount = Convert.ToInt64(row[3]),
                CostQuantity = Convert.ToInt32(row[4]),
                CostAmount = Convert.ToInt64(row[5]),
                Difference = Convert.ToInt64(row[6])
            }).ToList();
        }

        public Task<int> GetTotalCountAsync(Guid fiscalPeriodId, VoucherTemplate template)
        {
            return Session.QueryOver<ACC_Voucher>()
              .Where(voucher => voucher.FiscalPeriod.Id == fiscalPeriodId && voucher.VoucherTemplate.Id == (byte)template)
              .RowCountAsync();
        }

        public async Task<IList<ACC_Voucher>> GetListByLookupIdAsync(DateTime? fromDate, DateTime? toDate, int pageSize, int pageNumber,
            Guid lookupId, Guid fiscalPeriodId, LookupType lookupType)
        {
            var skip = (pageNumber - 1) * pageSize;

            ACC_Article articleAlias = null;
            ACC_Account accountAlias = null;
            ACC_Voucher voucherAlias = null;

            var query = Session.QueryOver(() => voucherAlias)
                .JoinAlias(voucher => voucher.Articles, () => articleAlias)
                .JoinAlias(() => articleAlias.Account, () => accountAlias)
                .Where(() => articleAlias.LookupId == lookupId)
                .And(() => accountAlias.LookupType == lookupType)
                .And(() => voucherAlias.FiscalPeriod.Id == fiscalPeriodId);

            if (fromDate.HasValue)
            {
                query = query.Where(voucher => voucher.EffectiveDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(voucher => voucher.EffectiveDate <= toDate.Value);
            }

            return await query.TransformUsing(Transformers.DistinctRootEntity)
                .OrderBy(voucher => voucher.EffectiveDate).Desc
                .Skip(skip)
                .Take(pageSize)
                .ListAsync();
        }

        public async Task<int> GetTotalCountAsync(Guid fiscalPeriodId, DateTime? fromDate, DateTime? toDate, Guid lookupId, LookupType lookupType)
        {
            ACC_Article articleAlias = null;
            ACC_Account accountAlias = null;
            ACC_Voucher voucherAlias = null;

            var query = Session.QueryOver(() => voucherAlias)
                .JoinAlias(voucher => voucher.Articles, () => articleAlias)
                .JoinAlias(() => articleAlias.Account, () => accountAlias)
                .Where(() => articleAlias.LookupId == lookupId)
                .And(() => accountAlias.LookupType == lookupType)
                .And(() => voucherAlias.FiscalPeriod.Id == fiscalPeriodId);

            if (fromDate.HasValue)
            {
                query = query.Where(voucher => voucher.EffectiveDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(voucher => voucher.EffectiveDate <= toDate.Value);
            }

            return await query.Select(Projections.CountDistinct(() => voucherAlias.Id)).FutureValue<int>().GetValueAsync();
        }

        public async Task<IList<SubsidiaryAccountBalanceSheetDto>> GetSubsidiaryAccountFinancialBalanceDataAsync(Guid fiscalPeriodId, DateTime fromDate, DateTime toDate)
        {
            SubsidiaryAccountBalanceSheetDto resultAlias = null;
            ACC_Voucher voucherAlias = null;
            ACC_Article articleAlias = null;

            var debitSumProjection = Projections.Sum(
                Projections.Conditional(
                    Restrictions.Eq(Projections.Property(() => articleAlias.Type), ArticleType.Debit),
                    Projections.Property(() => articleAlias.Amount),
                    Projections.Constant(0L)
                )
            );

            var creditSumProjection = Projections.Sum(
                Projections.Conditional(
                    Restrictions.Eq(Projections.Property(() => articleAlias.Type), ArticleType.Credit),
                    Projections.Property(() => articleAlias.Amount),
                    Projections.Constant(0L)
                )
            );

            return await Session.QueryOver(() => voucherAlias)
                .Where(() => voucherAlias.FiscalPeriod.Id == fiscalPeriodId)
                .And(() => voucherAlias.EffectiveDate >= fromDate && voucherAlias.EffectiveDate <= toDate)
                .JoinQueryOver(voucher => voucher.Articles, () => articleAlias)
                .Where(() => articleAlias.IsTransactionalOnly == false)
                .SelectList(list => list
                    .SelectGroup(() => articleAlias.Account.Id).WithAlias(() => resultAlias.Id)
                    .Select(debitSumProjection).WithAlias(() => resultAlias.TotalDebit)
                    .Select(creditSumProjection).WithAlias(() => resultAlias.TotalCredit)
                )
                .TransformUsing(Transformers.AliasToBean<SubsidiaryAccountBalanceSheetDto>())
                .ListAsync<SubsidiaryAccountBalanceSheetDto>();
        }

        public async Task<IList<DetailedAccountFinancialBalanceDto>> GetDetailedAccountFinancialBalanceDataAsync(DetailedAccountFinancialBalanceRequest request, Guid fiscalPeriodId, LookupType lookupType)
        {
            var take = request.PageSize;
            var skip = (request.PageNumber - 1) * request.PageSize;
            ISQLQuery query;

            switch (lookupType)
            {
                case LookupType.Person:
                    query = Session.CreateSQLQuery($@"
                                               SELECT
                                               	    PA.Id,
                                               	    CONCAT(PA.FirstName, ' ', PA.LastName) AS Name,
                                               	    SUM((CASE 
                                                        WHEN A.Type = 1 
                                                        AND V.FiscalPeriodId = :fiscalPeriodId 
                                                        AND V.EffectiveDate >= CAST(:fromDate AS DATETIME)
                                               	        AND V.EffectiveDate <= CAST(:toDate AS DATETIME) 
                                                        THEN A.Amount ELSE 0 END)) AS TotalDebit,
                                               	    SUM((CASE WHEN A.Type = 2
                                                        AND V.FiscalPeriodId = :fiscalPeriodId 
                                                        AND V.EffectiveDate >= CAST(:fromDate AS DATETIME)
                                               	        AND V.EffectiveDate <= CAST(:toDate AS DATETIME) 
                                                        THEN A.Amount ELSE 0 END)) AS TotalCredit,
                                                    SUM((CASE 
                                                        WHEN A.Type = 1 
                                                        AND V.FiscalPeriodId = :fiscalPeriodId 
                                                        AND V.EffectiveDate >= CAST(:fromDate AS DATETIME)
                                               	        AND V.EffectiveDate <= CAST(:toDate AS DATETIME) 
                                                        THEN A.Amount ELSE 0 END))  - 
                                                        SUM((CASE WHEN A.Type = 2
                                                        AND V.FiscalPeriodId = :fiscalPeriodId 
                                                        AND V.EffectiveDate >= CAST(:fromDate AS DATETIME)
                                               	        AND V.EffectiveDate <= CAST(:toDate AS DATETIME) 
                                                        THEN A.Amount ELSE 0 END)) AS Difference
                                               FROM [BigBang_Cloud.ERP.Accounting].dbo.PersonAccount AS PA
                                               LEFT JOIN [BigBang_Cloud.ERP.Accounting].dbo.Article AS A
                                               	    ON A.LookupId = PA.Id
                                                    AND A.IsTransactionalOnly = 0
                                                    AND A.AccountId = :subsidiaryAccountId
                                               LEFT JOIN [BigBang_Cloud.ERP.Accounting].dbo.Voucher AS V
                                               	    ON V.Id = A.VoucherId
                                               GROUP BY PA.Id, PA.FirstName, PA.LastName
                                               ORDER BY {request.SortBy} {request.SortDirection}
                                               OFFSET :skip ROWS FETCH NEXT :take ROWS ONLY
                                               ");
                    break;
                case LookupType.Product:
                    query = Session.CreateSQLQuery($@"
                                               SELECT
                                               	    P.Id,
                                               	    P.Name,
                                               	    SUM((CASE 
                                                        WHEN A.Type = 1 
                                                        AND V.FiscalPeriodId = :fiscalPeriodId 
                                                        AND V.EffectiveDate >= CAST(:fromDate AS DATETIME)
                                               	        AND V.EffectiveDate <= CAST(:toDate AS DATETIME) 
                                                        THEN A.Amount ELSE 0 END)) AS TotalDebit,
                                               	    SUM((CASE WHEN A.Type = 2
                                                        AND V.FiscalPeriodId = :fiscalPeriodId 
                                                        AND V.EffectiveDate >= CAST(:fromDate AS DATETIME)
                                               	        AND V.EffectiveDate <= CAST(:toDate AS DATETIME) 
                                                        THEN A.Amount ELSE 0 END)) AS TotalCredit,
                                                    SUM((CASE 
                                                        WHEN A.Type = 1 
                                                        AND V.FiscalPeriodId = :fiscalPeriodId 
                                                        AND V.EffectiveDate >= CAST(:fromDate AS DATETIME)
                                               	        AND V.EffectiveDate <= CAST(:toDate AS DATETIME) 
                                                        THEN A.Amount ELSE 0 END))  - 
                                                        SUM((CASE WHEN A.Type = 2
                                                        AND V.FiscalPeriodId = :fiscalPeriodId 
                                                        AND V.EffectiveDate >= CAST(:fromDate AS DATETIME)
                                               	        AND V.EffectiveDate <= CAST(:toDate AS DATETIME) 
                                                        THEN A.Amount ELSE 0 END)) AS Difference
                                               FROM [BigBang_Cloud.ERP.Accounting].dbo.Product AS P
                                               LEFT JOIN [BigBang_Cloud.ERP.Accounting].dbo.Article AS A
                                               	    ON A.LookupId = P.Id
                                                    AND A.IsTransactionalOnly = 0
                                                    AND A.AccountId = :subsidiaryAccountId
                                               LEFT JOIN [BigBang_Cloud.ERP.Accounting].dbo.Voucher AS V
                                               	    ON V.Id = A.VoucherId
                                               GROUP BY P.Id, P.Name
                                               ORDER BY {request.SortBy} {request.SortDirection}
                                               OFFSET :skip ROWS FETCH NEXT :take ROWS ONLY
                                               ");
                    break;
                case LookupType.Bank:
                    query = Session.CreateSQLQuery($@"
                                               SELECT
                                               	    B.Id,
	                                                B.Title,
                                               	    SUM((CASE 
                                                        WHEN A.Type = 1 
                                                        AND V.FiscalPeriodId = :fiscalPeriodId 
                                                        AND V.EffectiveDate >= CAST(:fromDate AS DATETIME)
                                               	        AND V.EffectiveDate <= CAST(:toDate AS DATETIME) 
                                                        THEN A.Amount ELSE 0 END)) AS TotalDebit,
                                               	    SUM((CASE WHEN A.Type = 2
                                                        AND V.FiscalPeriodId = :fiscalPeriodId 
                                                        AND V.EffectiveDate >= CAST(:fromDate AS DATETIME)
                                               	        AND V.EffectiveDate <= CAST(:toDate AS DATETIME) 
                                                        THEN A.Amount ELSE 0 END)) AS TotalCredit,
                                                    SUM((CASE 
                                                        WHEN A.Type = 1 
                                                        AND V.FiscalPeriodId = :fiscalPeriodId 
                                                        AND V.EffectiveDate >= CAST(:fromDate AS DATETIME)
                                               	        AND V.EffectiveDate <= CAST(:toDate AS DATETIME) 
                                                        THEN A.Amount ELSE 0 END))  - 
                                                        SUM((CASE WHEN A.Type = 2
                                                        AND V.FiscalPeriodId = :fiscalPeriodId 
                                                        AND V.EffectiveDate >= CAST(:fromDate AS DATETIME)
                                               	        AND V.EffectiveDate <= CAST(:toDate AS DATETIME) 
                                                        THEN A.Amount ELSE 0 END)) AS Difference
                                               FROM [BigBang_Cloud.ERP.Accounting].dbo.BankAccount AS B
                                               LEFT JOIN [BigBang_Cloud.ERP.Accounting].dbo.Article AS A
                                               	    ON A.LookupId = B.Id
                                                    AND A.IsTransactionalOnly = 0
                                                    AND A.AccountId = :subsidiaryAccountId
                                               LEFT JOIN [BigBang_Cloud.ERP.Accounting].dbo.Voucher AS V
                                               	    ON V.Id = A.VoucherId
                                               GROUP BY B.Id, B.Title
                                               ORDER BY {request.SortBy} {request.SortDirection}
                                               OFFSET :skip ROWS FETCH NEXT :take ROWS ONLY
                                               ");
                    break;
                default:
                    return [];
            }

            query.SetParameter("skip", skip);
            query.SetParameter("take", take);
            query.SetParameter("fromDate", request.FromDate);
            query.SetParameter("toDate", request.ToDate);
            query.SetParameter("fiscalPeriodId", fiscalPeriodId);
            query.SetParameter("subsidiaryAccountId", request.Id);

            var result = await query.ListAsync<object[]>();

            return result.Select(row =>
                new DetailedAccountFinancialBalanceDto((Guid)row[0], (long)row[2], (long)row[3], (string)row[1], (long)row[4])).ToList();
        }

        public async Task<IList<DetailedAccountFinancialBalanceDto>> CalculateDetailedAccountFinancialBalanceTotalAsync(DetailedAccountFinancialBalanceTotalRequest request,
            Guid fiscalPeriodId, LookupType lookupType)
        {
            ISQLQuery query;

            switch (lookupType)
            {
                case LookupType.Person:
                    query = Session.CreateSQLQuery($@"
                                               SELECT
                                               	    PA.Id,
                                               	    CONCAT(PA.FirstName, ' ', PA.LastName) AS Name,
                                               	    SUM((CASE 
                                                        WHEN A.Type = 1 
                                                        AND V.FiscalPeriodId = :fiscalPeriodId 
                                                        AND V.EffectiveDate >= CAST(:fromDate AS DATETIME)
                                               	        AND V.EffectiveDate <= CAST(:toDate AS DATETIME) 
                                                        THEN A.Amount ELSE 0 END)) AS TotalDebit,
                                               	    SUM((CASE WHEN A.Type = 2
                                                        AND V.FiscalPeriodId = :fiscalPeriodId 
                                                        AND V.EffectiveDate >= CAST(:fromDate AS DATETIME)
                                               	        AND V.EffectiveDate <= CAST(:toDate AS DATETIME) 
                                                        THEN A.Amount ELSE 0 END)) AS TotalCredit,
                                                    SUM((CASE 
                                                        WHEN A.Type = 1 
                                                        AND V.FiscalPeriodId = :fiscalPeriodId 
                                                        AND V.EffectiveDate >= CAST(:fromDate AS DATETIME)
                                               	        AND V.EffectiveDate <= CAST(:toDate AS DATETIME) 
                                                        THEN A.Amount ELSE 0 END))  - 
                                                        SUM((CASE WHEN A.Type = 2
                                                        AND V.FiscalPeriodId = :fiscalPeriodId 
                                                        AND V.EffectiveDate >= CAST(:fromDate AS DATETIME)
                                               	        AND V.EffectiveDate <= CAST(:toDate AS DATETIME) 
                                                        THEN A.Amount ELSE 0 END)) AS Difference
                                               FROM [BigBang_Cloud.ERP.Accounting].dbo.PersonAccount AS PA
                                               LEFT JOIN [BigBang_Cloud.ERP.Accounting].dbo.Article AS A
                                               	    ON A.LookupId = PA.Id
                                                    AND A.IsTransactionalOnly = 0
                                                    AND A.AccountId = :subsidiaryAccountId
                                               LEFT JOIN [BigBang_Cloud.ERP.Accounting].dbo.Voucher AS V
                                               	    ON V.Id = A.VoucherId
                                               GROUP BY PA.Id, PA.FirstName, PA.LastName
                                               ");
                    break;
                case LookupType.Product:
                    query = Session.CreateSQLQuery($@"
                                               SELECT
                                               	    P.Id,
                                               	    P.Name,
                                               	    SUM((CASE 
                                                        WHEN A.Type = 1 
                                                        AND V.FiscalPeriodId = :fiscalPeriodId 
                                                        AND V.EffectiveDate >= CAST(:fromDate AS DATETIME)
                                               	        AND V.EffectiveDate <= CAST(:toDate AS DATETIME) 
                                                        THEN A.Amount ELSE 0 END)) AS TotalDebit,
                                               	    SUM((CASE WHEN A.Type = 2
                                                        AND V.FiscalPeriodId = :fiscalPeriodId 
                                                        AND V.EffectiveDate >= CAST(:fromDate AS DATETIME)
                                               	        AND V.EffectiveDate <= CAST(:toDate AS DATETIME) 
                                                        THEN A.Amount ELSE 0 END)) AS TotalCredit,
                                                    SUM((CASE 
                                                        WHEN A.Type = 1 
                                                        AND V.FiscalPeriodId = :fiscalPeriodId 
                                                        AND V.EffectiveDate >= CAST(:fromDate AS DATETIME)
                                               	        AND V.EffectiveDate <= CAST(:toDate AS DATETIME) 
                                                        THEN A.Amount ELSE 0 END))  - 
                                                        SUM((CASE WHEN A.Type = 2
                                                        AND V.FiscalPeriodId = :fiscalPeriodId 
                                                        AND V.EffectiveDate >= CAST(:fromDate AS DATETIME)
                                               	        AND V.EffectiveDate <= CAST(:toDate AS DATETIME) 
                                                        THEN A.Amount ELSE 0 END)) AS Difference
                                               FROM [BigBang_Cloud.ERP.Accounting].dbo.Product AS P
                                               LEFT JOIN [BigBang_Cloud.ERP.Accounting].dbo.Article AS A
                                               	    ON A.LookupId = P.Id
                                                    AND A.IsTransactionalOnly = 0
                                                    AND A.AccountId = :subsidiaryAccountId
                                               LEFT JOIN [BigBang_Cloud.ERP.Accounting].dbo.Voucher AS V
                                               	    ON V.Id = A.VoucherId
                                               GROUP BY P.Id, P.Name
                                               ");
                    break;
                case LookupType.Bank:
                    query = Session.CreateSQLQuery($@"
                                               SELECT
                                               	    B.Id,
	                                                B.Title,
                                               	    SUM((CASE 
                                                        WHEN A.Type = 1 
                                                        AND V.FiscalPeriodId = :fiscalPeriodId 
                                                        AND V.EffectiveDate >= CAST(:fromDate AS DATETIME)
                                               	        AND V.EffectiveDate <= CAST(:toDate AS DATETIME) 
                                                        THEN A.Amount ELSE 0 END)) AS TotalDebit,
                                               	    SUM((CASE WHEN A.Type = 2
                                                        AND V.FiscalPeriodId = :fiscalPeriodId 
                                                        AND V.EffectiveDate >= CAST(:fromDate AS DATETIME)
                                               	        AND V.EffectiveDate <= CAST(:toDate AS DATETIME) 
                                                        THEN A.Amount ELSE 0 END)) AS TotalCredit,
                                                    SUM((CASE 
                                                        WHEN A.Type = 1 
                                                        AND V.FiscalPeriodId = :fiscalPeriodId 
                                                        AND V.EffectiveDate >= CAST(:fromDate AS DATETIME)
                                               	        AND V.EffectiveDate <= CAST(:toDate AS DATETIME) 
                                                        THEN A.Amount ELSE 0 END))  - 
                                                        SUM((CASE WHEN A.Type = 2
                                                        AND V.FiscalPeriodId = :fiscalPeriodId 
                                                        AND V.EffectiveDate >= CAST(:fromDate AS DATETIME)
                                               	        AND V.EffectiveDate <= CAST(:toDate AS DATETIME) 
                                                        THEN A.Amount ELSE 0 END)) AS Difference
                                               FROM [BigBang_Cloud.ERP.Accounting].dbo.BankAccount AS B
                                               LEFT JOIN [BigBang_Cloud.ERP.Accounting].dbo.Article AS A
                                               	    ON A.LookupId = B.Id
                                                    AND A.IsTransactionalOnly = 0
                                                    AND A.AccountId = :subsidiaryAccountId
                                               LEFT JOIN [BigBang_Cloud.ERP.Accounting].dbo.Voucher AS V
                                               	    ON V.Id = A.VoucherId
                                               GROUP BY B.Id, B.Title
                                               ");
                    break;
                default:
                    return [];
            }

            query.SetParameter("fromDate", request.FromDate);
            query.SetParameter("toDate", request.ToDate);
            query.SetParameter("fiscalPeriodId", fiscalPeriodId);
            query.SetParameter("subsidiaryAccountId", request.Id);

            var result = await query.ListAsync<object[]>();

            return result.Select(row =>
                new DetailedAccountFinancialBalanceDto((Guid)row[0], (long)row[2], (long)row[3], (string)row[1], (long)row[4])).ToList();
        }

        public async Task<IList<ACC_Article>> GetVoucherInvoiceDataAsync(Guid voucherId, string accountName)
        {
            ACC_Article articleAlias = null;
            ACC_Account accountAlias = null;

            return await Session.QueryOver(() => articleAlias)
                .JoinAlias(() => articleAlias.Account, () => accountAlias)
                .Where(() => accountAlias.Name == accountName)
                .And(() => articleAlias.Voucher.Id == voucherId)
                .ToListAsync();
        }

        public async Task<IList<AccountTotalAmountDto>> GetTotalAmountByAccountIdAsync(DateTime from, DateTime to, Guid fiscalPeriod, List<Guid> accountIds)
        {
            ACC_Article articleAlias = null;
            ACC_Voucher voucherAlias = null;
            AccountTotalAmountDto accountTotalAmountDto = null;

            return await Session.QueryOver(() => articleAlias)
                                     .JoinAlias(() => articleAlias.Voucher, () => voucherAlias, JoinType.InnerJoin)
                                     .Where(() => voucherAlias.EffectiveDate >= from && voucherAlias.EffectiveDate <= to)
                                     .And(() => voucherAlias.FiscalPeriod.Id == fiscalPeriod)
                                     .WhereRestrictionOn(() => articleAlias.Account.Id).IsIn(accountIds)
                                     .SelectList(list => list.SelectGroup(() => articleAlias.Account.Id).WithAlias(() => accountTotalAmountDto.AccountId)
                                                             .SelectGroup(() => articleAlias.Type).WithAlias(() => accountTotalAmountDto.ArticleType)
                                                             .SelectSum(() => articleAlias.Amount).WithAlias(() => accountTotalAmountDto.TotalAmount)
                                                             .SelectSum(() => articleAlias.Quantity).WithAlias(() => accountTotalAmountDto.TotalQuantity)
                                                             )
                                     .TransformUsing(Transformers.AliasToBean<AccountTotalAmountDto>())
                                     .ListAsync<AccountTotalAmountDto>();
        }

        public async Task<long> GetAggregateArticlesByParentAccountIdInDurationAsync(DateTime from, DateTime to, Guid fiscalPeriodId, Guid parentId, ArticleType articleType)
        {
            ACC_Article articleAlias = null;
            ACC_Voucher voucherAlias = null;
            ACC_Account accountAlias = null;

            return await Session.QueryOver(() => articleAlias)
                                           .JoinAlias(() => articleAlias.Account, () => accountAlias)
                                           .JoinAlias(() => articleAlias.Voucher, () => voucherAlias)
                                           .Where(() => voucherAlias.EffectiveDate >= from && voucherAlias.EffectiveDate <= to)
                                           .And(() => voucherAlias.FiscalPeriod.Id == fiscalPeriodId)
                                           .And(() => accountAlias.ParentAccount.Id == parentId)
                                           .And(() => articleAlias.Type == articleType)
                                           .Select(Projections.Sum(() => articleAlias.Amount))
                                           .SingleOrDefaultAsync<long>();
        }

        public async Task<IList<BalanceSheetDto>> GetBalanceSheetResponseByDateAsync(Guid fiscalPeriodId,
                                                                             DateTime to,
                                                                             List<Guid> parentAccountIds,
                                                                             List<Guid> accountIds)
        {
            var parentAccountIdCondition = parentAccountIds is null || !parentAccountIds.Any() ? string.Empty : $"AND ParentAccount.Id IN ({string.Join(", ", parentAccountIds.Select(g => $"'{g}'"))})";
            var accountIdCondition = accountIds is null || !accountIds.Any() ? string.Empty : $"AND Account.Id IN ({string.Join(", ", accountIds.Select(g => $"'{g}'"))})";

            var query = Session.CreateSQLQuery($@"SELECT
                                                          ParentAccount.Id AS ParentAccountId, 
		                                                  ParentAccount.Name AS ParentAccountName, 
		                                                  ParentAccount.DisplayName AS ParentAccountDisplayName, 
		                                                  Account.Id AS AccountId, 
		                                                  Account.Name AS AccountName, 
		                                                  Account.DisplayName AS AccountDisplayName,
                                                          Account.Nature,
		                                                  SUM(CASE  WHEN Article.Type <> Account.Nature THEN Article.Amount * -1 
		                                                            ELSE Article.Amount END ) AS Amount
                                                  FROM	
		                                                  [BigBang_Cloud.ERP.Accounting].dbo.[Voucher] INNER JOIN 
		                                                  [BigBang_Cloud.ERP.Accounting].dbo.[Article] on Voucher.Id=Article.VoucherId INNER JOIN
		                                                  [BigBang_Cloud.ERP.Accounting].dbo.[Account] on Article.AccountId=Account.Id INNER JOIN 
		                                                  [BigBang_Cloud.ERP.Accounting].dbo.[Account] ParentAccount on Account.ParentAccountId=ParentAccount.Id
                                                  WHERE	
		                                                  Voucher.EffectiveDate <= :to
		                                                  AND Article.IsTransactionalOnly = 0
		                                                  AND Voucher.FiscalPeriodId = :fiscalPeriodId
                                                          {parentAccountIdCondition}
                                                          {accountIdCondition}
                                                  GROUP BY 
		                                                  ParentAccount.Id, 
		                                                  ParentAccount.Name, 
		                                                  ParentAccount.DisplayName, 
		                                                  Account.Id, 
		                                                  Account.Name, 
		                                                  Account.DisplayName,
                                                          Account.Nature");

            query.SetParameter("to", to);
            query.SetParameter("fiscalPeriodId", fiscalPeriodId);
            var result = await query.ListAsync<object[]>();

            return result.Select(row => new BalanceSheetDto((Guid)row[0],
                                                            (string)row[1],
                                                            (string)row[2],
                                                            (Guid)row[3],
                                                            (string)row[4],
                                                            (string)row[5],
                                                            Convert.ToInt64(row[7]),
                                                            (AccountNature)Convert.ToInt32(row[6])))
                         .ToList();
        }
    }
}