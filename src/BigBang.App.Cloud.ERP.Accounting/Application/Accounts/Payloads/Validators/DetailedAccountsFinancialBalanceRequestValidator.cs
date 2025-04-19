using System.Linq;
using BigBang.App.Cloud.ERP.Accounting.Common;
using BigBang.App.Cloud.ERP.Accounting.Common.Validators;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using FluentValidation;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Accounts.Payloads.Validators
{
    internal class DetailedAccountsFinancialBalanceRequestValidator : BaseValidator<DetailedAccountFinancialBalanceRequest>
    {
        public DetailedAccountsFinancialBalanceRequestValidator()
        {
            RuleFor(request => request.ToDate)
                .GreaterThanOrEqualTo(request => request.FromDate)
                .WithMessage(Messages.Error_FromDateToDateIsNotValid);

            RuleFor(request => request.Id)
                .NotEmpty()
                .WithMessage(string.Format(Messages.Error_ShouldNotBeNull, Messages.Entity_Account));

            RuleFor(request => request.SortDirection)
                .NotEmpty()
                .WithMessage(Messages.Error_SortDirectionHaveToBeDescendingOrAscending)
                .IsInEnum()
                .WithMessage(Messages.Error_SortDirectionHaveToBeDescendingOrAscending);

            RuleFor(request => request.SortBy)
                .NotEmpty()
                .WithMessage(Messages.Error_SortByHaveToBeTotalDebit_TotalCredit_Difference)
                .Must(sortBy => Constants.DetailedAccountFinancialBalanceSortBy.Contains(sortBy))
                .WithMessage(Messages.Error_SortByHaveToBeTotalDebit_TotalCredit_Difference);
        }
    }
}
