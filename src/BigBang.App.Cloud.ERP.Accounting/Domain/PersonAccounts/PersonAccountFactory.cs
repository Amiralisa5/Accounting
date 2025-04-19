using System;
using System.Collections.Generic;
using System.Linq;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;

namespace BigBang.App.Cloud.ERP.Accounting.Domain.PersonAccounts
{
    public static class PersonAccountFactory
    {
        public static ACC_PersonAccount Create(Guid businessId, string firstName, string lastName, string mobileNumber, IEnumerable<PersonRoleType> personRoleTypes)
        {
            var personAccount = new ACC_PersonAccount
            {
                Id = Guid.NewGuid(),
                FirstName = firstName,
                LastName = lastName,
                MobileNumber = mobileNumber,
                Business = new ACC_Business { Id = businessId }
            };

            return personAccount.CreateRoleTypes(personRoleTypes);
        }

        public static ACC_PersonAccount CreateRoleTypes(this ACC_PersonAccount personAccount, IEnumerable<PersonRoleType> personRoleTypes)
        {
            var personAccountRoles = personRoleTypes
                .Select(personRoleType => new ACC_PersonAccountRole
                {
                    Id = Guid.NewGuid(),
                    PersonRoleTypeId = personRoleType,
                    PersonAccount = personAccount
                })
                .ToList();

            personAccount.PersonAccountRoles = personAccountRoles;

            return personAccount;
        }
    }
}