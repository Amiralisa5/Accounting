using BigBang.App.Cloud.ERP.Accounting.Common.Validators;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using FluentValidation;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Accounts.Payloads.Validators
{
    internal class SubsidiaryAccountsFinancialBalanceRequestValidator : BaseValidator<SubsidiaryAccountFinancialBalanceRequest>
    {
        public SubsidiaryAccountsFinancialBalanceRequestValidator()
        {
            RuleFor(request => request.ToDate)
                .GreaterThanOrEqualTo(request => request.FromDate)
                .WithMessage(Messages.Error_FromDateToDateIsNotValid);
        }
    }
}
