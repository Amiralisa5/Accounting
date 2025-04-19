using AutoFixture;
using BigBang.App.Cloud.ERP.Accounting.Application.Enums.Payloads;
using BigBang.App.Cloud.ERP.Accounting.Domain;
using BigBang.App.Cloud.ERP.Accounting.Domain.Enums;

namespace Cloud.ERP.Accounting.Tests.TestData
{
    public abstract class BaseTestData
    {
        protected static Random Random;
        protected static readonly Fixture Fixture;

        static BaseTestData()
        {
            Random = new Random();

            Fixture = new Fixture();
            Fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
                .ToList()
                .ForEach(behavior => Fixture.Behaviors.Remove(behavior));

            Fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        }

        protected static ACC_FiscalPeriod GetFiscalPeriod()
        {
            return Fixture.Build<ACC_FiscalPeriod>()
                .With(fiscalPeriod => fiscalPeriod.FromDate, DateTime.Today)
                .With(fiscalPeriod => fiscalPeriod.ToDate, DateTime.Today.AddYears(1))
                .Create();
        }

        protected static EnumDataModel GetCurrencyEnum()
        {
            return Fixture.Build<EnumDataModel>()
                .With(model => model.EnumName, nameof(Currency))
                .With(model => model.EnumMembers,
                    [new EnumMemberDataModel(nameof(Currency.Rial), string.Empty, (long)Currency.Rial)])
                .Create();
        }
    }
}
