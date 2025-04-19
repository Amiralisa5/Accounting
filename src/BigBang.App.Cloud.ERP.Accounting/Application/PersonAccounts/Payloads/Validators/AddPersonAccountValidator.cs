using System;
using System.Linq;
using BigBang.App.Cloud.ERP.Accounting.Common;
using BigBang.App.Cloud.ERP.Accounting.Common.Validators;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using FluentNHibernate.Conventions;
using FluentValidation;

namespace BigBang.App.Cloud.ERP.Accounting.Application.PersonAccounts.Payloads.Validators
{
    internal class AddPersonAccountValidator : BaseValidator<AddPersonAccountRequest>
    {
        public AddPersonAccountValidator()
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
                .Matches(Constants.MobileNumberRegex)
                .WithMessage(Messages.Error_MobileNumberShouldBeElevenDigits);

            RuleFor(request => request.InitialStatus)
                .Must(status => !status.HasValue || Enum.IsDefined(typeof(AccountNature), status.Value))
                .WithMessage(Messages.Error_AccountNatureIsInvalid);

            RuleFor(request => request.RoleTypes)
                .NotEmpty()
                .WithMessage(string.Format(Messages.Error_FieldRequired, Messages.Label_PersonRole));

            RuleFor(request => request.RoleTypes)
                .Must(types => types.All(type => Enum.IsDefined(typeof(PersonRoleType), type)))
                .When(request => request.RoleTypes != null)
                .WithMessage(string.Format(Messages.Error_EntityNotFound, Messages.Label_PersonRole));

            RuleFor(request => request.RoleTypes)
                .Must(roleTypes => !roleTypes.Contains(PersonRoleType.BusinessOwner))
                .When(request => request.RoleTypes != null)
                .WithMessage(Messages.Error_CannotAssigeBussinessOwnerRoleToAnotherPerson);
        }
    }
}