using BigBang.App.Cloud.ERP.Accounting.Common.Validators;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using FluentValidation;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Accounts.Payloads.Validators
{
    internal class DetailedAccountFinancialBalanceTotalRequestValidator : BaseValidator<DetailedAccountFinancialBalanceTotalRequest>
    {
        public DetailedAccountFinancialBalanceTotalRequestValidator()
        {
            RuleFor(request => request.ToDate)
                .GreaterThanOrEqualTo(request => request.FromDate)
                .WithMessage(Messages.Error_FromDateToDateIsNotValid);

            RuleFor(request => request.Id)
                .NotEmpty()
                .WithMessage(string.Format(Messages.Error_ShouldNotBeNull, Messages.Entity_Account));
        }
    }
}
