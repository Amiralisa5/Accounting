using BigBang.App.Cloud.ERP.Accounting.Common;
using BigBang.App.Cloud.ERP.Accounting.Common.Validators;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using FluentValidation;

namespace BigBang.App.Cloud.ERP.Accounting.Application.BankAccounts.Payloads.Validators
{
    internal class BankAccountValidator : BaseValidator<BankAccountRequest>
    {
        public BankAccountValidator()
        {
            RuleFor(request => request.HolderName)
                .NotEmpty()
                .WithMessage(string.Format(Messages.Error_FieldRequired, Messages.Label_HolderName))
                .MaximumLength(100)
                .WithMessage(string.Format(Messages.Error_MaximumLengthShouldBe, Messages.Label_HolderName, 100));

            RuleFor(request => request.Title)
                .MaximumLength(100)
                .WithMessage(string.Format(Messages.Error_MaximumLengthShouldBe, Messages.Label_Title, 100));

            RuleFor(request => request.ShebaNumber)
               .Matches(Constants.ShebaNumberRegex)
               .WithMessage(string.Format(Messages.Error_ExactLengthShouldBe, Messages.Label_ShebaNumber, 24));

            RuleFor(request => request.CardNumber)
                .NotEmpty()
                .WithMessage(string.Format(Messages.Error_FieldRequired, Messages.Label_CardNumber))
                .Matches(Constants.CardNumberRegex)
                .WithMessage(string.Format(Messages.Error_ExactLengthShouldBe, Messages.Label_CardNumber, 16));

            RuleFor(request => request.Balance)
             .GreaterThan(-1)
             .WithMessage(string.Format(Messages.Error_ShouldNotBeNegative, Messages.Label_Balance));

            RuleFor(request => request.Bank)
                .NotEmpty()
                .WithMessage(string.Format(Messages.Error_FieldRequired, Messages.Label_Bank))
                .IsInEnum()
                .WithMessage(string.Format(Messages.Error_EntityNotFound, Messages.Label_Bank));
        }
    }
}