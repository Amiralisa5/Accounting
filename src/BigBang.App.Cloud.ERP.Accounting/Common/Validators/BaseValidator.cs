using FluentValidation;

namespace BigBang.App.Cloud.ERP.Accounting.Common.Validators
{
    internal class BaseValidator<T> : AbstractValidator<T> where T : IRequest
    {
    }
}
