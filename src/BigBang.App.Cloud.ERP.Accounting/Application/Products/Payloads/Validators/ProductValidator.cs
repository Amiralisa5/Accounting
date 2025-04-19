using BigBang.App.Cloud.ERP.Accounting.Common.Validators;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using FluentValidation;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Products.Payloads.Validators
{
    internal class ProductValidator : BaseValidator<ProductRequest>
    {
        public ProductValidator()
        {
            RuleFor(request => request.Name)
                .NotEmpty()
                .WithMessage(string.Format(Messages.Error_FieldRequired, Messages.Label_ProductName))
                .MaximumLength(100)
                .WithMessage(string.Format(Messages.Error_MaximumLengthShouldBe, Messages.Label_ProductName, 100));

            RuleFor(request => request.SuggestedSellPrice)
              .GreaterThan(-1)
              .WithMessage(string.Format(Messages.Error_ShouldNotBeNegative, Messages.Label_BuyPrice));

            RuleFor(request => request.BuyPrice)
              .GreaterThan(-1)
              .WithMessage(string.Format(Messages.Error_ShouldNotBeNegative, Messages.Label_BuyPrice));

            RuleFor(request => request.Stock)
                .GreaterThan(-1)
                .WithMessage(string.Format(Messages.Error_ShouldNotBeNegative, Messages.Label_Stock));
  
        }
    }
}