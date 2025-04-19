using System;
using System.Collections.Generic;
using System.Linq;
using BigBang.App.Cloud.ERP.Accounting.Common;
using BigBang.App.Cloud.ERP.Accounting.Common.Validators;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using FluentValidation;

namespace BigBang.App.Cloud.ERP.Accounting.Application.PersonAccounts.Payloads.Validators
{
    internal class UpdatePersonAccountValidator : BaseValidator<UpdatePersonAccountRequest>
    {
        private readonly Guid _ownerPersonAccountId;
        private readonly Guid _personAccountId;

        public UpdatePersonAccountValidator(Guid ownerPersonAccountId, Guid personAccountId)
        {
            _ownerPersonAccountId = ownerPersonAccountId;
            _personAccountId = personAccountId;

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

            RuleFor(request => request.RoleTypes)
                .NotEmpty()
                .WithMessage(string.Format(Messages.Error_FieldRequired, Messages.Label_PersonRole));

            RuleFor(request => request.RoleTypes)
                .Must(types => types.All(type => Enum.IsDefined(typeof(PersonRoleType), type)))
                .When(request => request.RoleTypes != null)
                .WithMessage(string.Format(Messages.Error_EntityNotFound, Messages.Label_PersonRole));

            RuleFor(request => request.RoleTypes)
                .Must(ValidatePersonAccountRoleType)
                .WithMessage(Messages.Error_CannotAssigeBussinessOwnerRoleToAnotherPerson);

            RuleFor(request => request.RoleTypes)
                .Must(ValidateOwnerPersonAccountRoleType)
                .WithMessage(Messages.Error_CannotRemoveBussinessOwnerRoleForOwnerPersonAccount);
        }

        private bool ValidatePersonAccountRoleType(IEnumerable<PersonRoleType> roleTypes)
        {
            if (_ownerPersonAccountId == _personAccountId)
                return true;

            return !roleTypes.Contains(PersonRoleType.BusinessOwner);
        }

        private bool ValidateOwnerPersonAccountRoleType(IEnumerable<PersonRoleType> roleTypes)
        {
            if (_ownerPersonAccountId == _personAccountId)
                return roleTypes.Contains(PersonRoleType.BusinessOwner);

            return true;
        }
    }
}