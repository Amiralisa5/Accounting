using BigBang.App.Cloud.ERP.Accounting.Common;
using BigBang.App.Cloud.ERP.Accounting.Common.Validators;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using FluentValidation;

namespace BigBang.App.Cloud.ERP.Accounting.Application.Owners.Payloads.Validators
{
    internal class CreateOwnerValidator : BaseValidator<CreateOwnerRequest>
    {
        public CreateOwnerValidator()
        {
            RuleFor(request => request.FirstName)
                .NotEmpty()
                .WithMessage(string.Format(Messages.Error_FieldRequired, Messages.Label_FirstName))
                .MaximumLength(50)
                .WithMessage(string.Format(Messages.Error_MaximumLengthShouldBe, Messages.Label_FirstName, 50));

            RuleFor(request => request.LastName)
                .NotEmpty()
                .WithMessage(string.Format(Messages.Error_FieldRequired, Messages.Label_LastName))
                .MaximumLength(50)
                .WithMessage(string.Format(Messages.Error_MaximumLengthShouldBe, Messages.Label_LastName, 50));

            RuleFor(request => request.MobileNumber)
                .NotEmpty()
                .WithMessage(string.Format(Messages.Error_FieldRequired, Messages.Label_MobileNumber))
                .Matches(Constants.MobileNumberRegex)
                .WithMessage(Messages.Error_MobileNumberShouldBeElevenDigits);

            RuleFor(request => request.BusinessName)
                .NotEmpty()
                .WithMessage(string.Format(Messages.Error_FieldRequired, Messages.Label_BusinessName))
                .MaximumLength(100)
                .WithMessage(string.Format(Messages.Error_MaximumLengthShouldBe, Messages.Label_BusinessName, 100));
        }
    }
}