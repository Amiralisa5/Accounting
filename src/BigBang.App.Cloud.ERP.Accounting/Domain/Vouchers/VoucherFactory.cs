using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Common;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;

namespace BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers
{
    public static class VoucherFactory
    {

        public static ACC_Voucher Create(RegisterVoucherRequest request,
            ACC_FiscalPeriod fiscalPeriod,
            ACC_VoucherTemplate voucherTemplate,
            string lastNumber,
            List<string> replaceStrings)
        {
            var title = GenerateTitle(voucherTemplate.TitleFormat, replaceStrings);

            var number = GenerateNumber(request.EffectiveDate, lastNumber);

            return MapToVoucher(request, voucherTemplate, fiscalPeriod, number, title);
        }

        private static ACC_Voucher MapToVoucher(RegisterVoucherRequest request,
            ACC_VoucherTemplate voucherTemplate,
            ACC_FiscalPeriod fiscalPeriod,
            string number,
            string title)
        {
            var voucher = new ACC_Voucher
            {
                Id = Guid.NewGuid(),
                EffectiveDate = request.EffectiveDate,
                Description = request.Description,
                Number = number,
                CreationDate = DateTime.Now,
                VoucherTemplate = voucherTemplate,
                Type = VoucherType.Normal,
                FiscalPeriod = fiscalPeriod,
                Title = title
            };

            voucher.Articles = MapToArticles(request.Articles, voucher);

            voucher.Amount = voucher.CalculateAmount();

            return voucher;
        }

        private static IList<ACC_Article> MapToArticles(IEnumerable<ArticleRequest> articles,
            ACC_Voucher voucher)
        {
            return articles.Select(article => new ACC_Article
            {
                Id = Guid.NewGuid(),
                Amount = article.Amount,
                Type = article.Type,
                Currency = article.Currency,
                LookupId = article.LookupId == Guid.Empty ? null : article.LookupId,
                Account = new ACC_Account { Id = article.AccountId },
                Quantity = article.Quantity,
                Fee = article.Fee,
                Voucher = voucher,
                IsTransactionalOnly = article.IsTransactionalOnly
            }).ToList();
        }

        private static string GenerateTitle(string titleFormat, IList<string> replaceStrings)
        {
            var matches = Regex.Matches(titleFormat,
                Constants.VoucherTemplateTitleFormatRegex);

            var title = titleFormat;

            for (var i = 0; i < matches.Count; i++)
            {
                title = title.Replace(matches[i].ToString()!, replaceStrings[i]);
            }

            return title;
        }

        private static string GenerateNumber(DateTime effectiveDate, string lastNumber)
        {
            var pc = new PersianCalendar();

            var fiscalPeriodYear = $"{pc.GetYear(effectiveDate):00}";

            var number = $"{fiscalPeriodYear.Substring(2, 2)}-";

            if (lastNumber is null)
            {
                return $"{number}{1}";
            }

            var splitString = lastNumber.Split("-");

            var numberPart = int.Parse(splitString[1]);

            return $"{number}{numberPart + 1}";
        }
    }
}