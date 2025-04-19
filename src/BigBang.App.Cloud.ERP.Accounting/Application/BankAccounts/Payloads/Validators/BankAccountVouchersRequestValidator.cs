using BigBang.App.Cloud.ERP.Accounting.Common.Validators;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using FluentValidation;

namespace BigBang.App.Cloud.ERP.Accounting.Application.BankAccounts.Payloads.Validators
{
    internal class BankAccountVouchersRequestValidator : BaseValidator<BankAccountVouchersRequest>
    {
        public BankAccountVouchersRequestValidator()
        {
            RuleFor(request => request.ToDate)
                .GreaterThanOrEqualTo(request => request.FromDate)
                .WithMessage(Messages.Error_FromDateToDateIsNotValid)
                .When(request => request.FromDate.HasValue && request.ToDate.HasValue);

            RuleFor(request => request.PageSize)
                .GreaterThan(0)
                .WithMessage(string.Format(Messages.Error_ShouldBeGreaterThan, Messages.Label_PageSize, 0));

            RuleFor(request => request.PageNumber)
                .GreaterThan(0)
                .WithMessage(string.Format(Messages.Error_ShouldBeGreaterThan, Messages.Label_PageNumber, 0));

            RuleFor(request => request.Id)
                .NotEmpty()
                .WithMessage(string.Format(Messages.Error_ShouldNotBeNull, Messages.Entity_BankAccount));
        }
    }
}
