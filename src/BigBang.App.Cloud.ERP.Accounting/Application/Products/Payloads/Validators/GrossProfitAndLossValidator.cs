using AngleSharp.Text;
using BigBang.App.Cloud.ERP.Accounting.Common;
using BigBang.App.Cloud.ERP.Accounting.Common.Validators;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using FluentValidation;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Products.Payloads.Validators
{
    internal class GrossProfitAndLossValidator : BaseValidator<GrossProfitAndLossRequest>
    {
        public GrossProfitAndLossValidator()
        {
            RuleFor(request => request.SortDirection)
                .NotEmpty()
                .WithMessage(Messages.Error_SortDirectionHaveToBeDescendingOrAscending)
                .IsInEnum()
                .WithMessage(Messages.Error_SortDirectionHaveToBeDescendingOrAscending);

            RuleFor(request => request.SortBy)
                .NotEmpty()
                .WithMessage(Messages.Error_SortByHaveToBeSellQuantity_SellAmount_CostAmountAndDifferenceAmount)
                .Must(sortBy => Constants.GrossProfitAndLossSortBy.Contains(sortBy))
                .WithMessage(Messages.Error_SortByHaveToBeSellQuantity_SellAmount_CostAmountAndDifferenceAmount);
        }
    }
}
