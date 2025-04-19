using System;
using System.Collections.Generic;
using System.Linq;
using BigBang.App.Cloud.ERP.Accounting.Common.Validators;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.App.Cloud.ERP.Accounting.Domain.Vouchers;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using FluentValidation;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Vouchers.Payloads.Validators
{
    internal class RegisterVoucherRequestValidator : BaseValidator<RegisterVoucherRequest>
    {
        private readonly ACC_FiscalPeriod _fiscalPeriod;

        public RegisterVoucherRequestValidator(ACC_FiscalPeriod fiscalPeriod)
        {
            _fiscalPeriod = fiscalPeriod;

            RuleFor(request => request.Template)
                .Must(ValidateTemplate)
                .WithMessage(Messages.Error_TemplateValueIsNotValid);

            RuleFor(request => request.Description)
                .MaximumLength(255)
                .WithMessage(string.Format(Messages.Error_MaximumLengthShouldBe, Messages.Label_VoucherDescription, 255));

            RuleFor(request => request.EffectiveDate)
                .NotEmpty()
                .WithMessage(string.Format(Messages.Error_FieldRequired, Messages.Label_VoucherEffectiveDate))
                .Must(EffectiveDateRangeValidator)
                .WithMessage(Messages.Error_EffectiveDateShouldBeInRangeOfFiscalPeriodDate);

            RuleFor(request => request.Articles)
                .NotNull()
                .WithMessage(string.Format(Messages.Error_FieldRequired, Messages.Label_VoucherArticles))
                .Must(ValidateTotalAmountEqualityForDebitAndCredit)
                .WithMessage(Messages.Error_SumOfDebitAndCreditShouldBeEqual);

            RuleForEach(request => request.Articles)
                .ChildRules(article =>
                {
                    article.RuleFor(articleRequest => articleRequest.AccountId)
                        .NotEmpty()
                        .WithMessage(string.Format(Messages.Error_FieldRequired, Messages.Entity_Account));

                    article.RuleFor(articleRequest => articleRequest.Amount)
                        .GreaterThan(0)
                        .WithMessage(string.Format(Messages.Error_ShouldBeGreaterThan, Messages.Label_Amount, 0));

                    article.RuleFor(articleRequest => articleRequest.Type)
                        .IsInEnum()
                        .WithMessage(Messages.Error_ArticleTypeIsNotValid);

                    article.RuleFor(articleRequest => articleRequest.Currency)
                        .IsInEnum()
                        .WithMessage(Messages.Error_CurencyIsNotValid);
                });

            ValidateTemplates();
        }

        private void ValidateTemplates()
        {
            RuleForEach(request => request.Articles)
                .ChildRules(article =>
                {
                    article.RuleFor(articleRequest => articleRequest.LookupId)
                        .NotEmpty()
                        .WithMessage(string.Format(Messages.Error_ShouldNotBeNull, Messages.Label_Lookup));
                }).When(request => request.Template is not VoucherTemplate.Cost);

            RuleForEach(request => request.Articles)
                .ChildRules(article =>
                {
                    article.RuleFor(articleRequest => articleRequest.LookupId)
                        .Empty()
                        .WithMessage(string.Format(Messages.Error_ShouldBeNull, Messages.Label_Lookup))
                        .When(articleRequest => articleRequest.Type == ArticleType.Debit);

                }).When(request => request.Template == VoucherTemplate.Cost);

            RuleForEach(request => request.Articles)
                .ChildRules(article =>
                {
                    article.RuleFor(articleRequest => articleRequest.Fee)
                        .Empty()
                        .WithMessage(string.Format(Messages.Error_ShouldBeNull, Messages.Label_Fee));

                    article.RuleFor(articleRequest => articleRequest.Quantity)
                        .Empty()
                        .WithMessage(string.Format(Messages.Error_ShouldBeNull, Messages.Label_Quantity));

                }).When(request => request.Template is not VoucherTemplate.ProductBuy and not VoucherTemplate.ProductSell);
        }

        private static bool ValidateTemplate(VoucherTemplate template)
        {
            return Enum.GetValues<VoucherTemplate>().Contains(template);
        }

        private static bool ValidateTotalAmountEqualityForDebitAndCredit(IEnumerable<ArticleRequest> articles)
        {
            var articleRequests = articles.ToList();

            var totalDebit = articleRequests.Where(article => article.Type == ArticleType.Debit)
                .Sum(request => request.Amount);

            var totalCredit = articleRequests.Where(article => article.Type == ArticleType.Credit)
                .Sum(request => request.Amount);

            return totalDebit == totalCredit;
        }

        private bool EffectiveDateRangeValidator(DateTime dateTime)
        {
            return _fiscalPeriod.FromDate <= dateTime && _fiscalPeriod.ToDate >= dateTime;
        }
    }
}