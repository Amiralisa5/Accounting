using System.Net;
using BigBang.WebServer.Common.Exceptions;
using FluentAssertions;
using FluentAssertions.Specialized;

namespace Cloud.ERP.Accounting.Tests.Extensions
{
    internal static class ExceptionAssertionsExtension
    {
        public static ExceptionAssertions<BigBangException> AssertBadRequest(this ExceptionAssertions<BigBangException> assertions)
        {
            assertions.AssertHttpStatusCode(HttpStatusCode.BadRequest);
            return assertions;
        }

        public static ExceptionAssertions<BigBangException> AssertForbidden(this ExceptionAssertions<BigBangException> assertions)
        {
            assertions.AssertHttpStatusCode(HttpStatusCode.Forbidden);
            return assertions;
        }

        public static ExceptionAssertions<BigBangException> AssertNotFound(this ExceptionAssertions<BigBangException> assertions)
        {
            assertions.AssertHttpStatusCode(HttpStatusCode.NotFound);
            return assertions;
        }

        public static ExceptionAssertions<BigBangException> AssertMessage(this ExceptionAssertions<BigBangException> assertions, string message)
        {
            assertions.Which.Message.Should().Contain(message);
            return assertions;
        }

        private static ExceptionAssertions<BigBangException> AssertHttpStatusCode(this ExceptionAssertions<BigBangException> assertions, HttpStatusCode httpStatusCode)
        {
            assertions.Which.ErrorCode.HttpStatusCode.Should().Be((int)httpStatusCode);
            return assertions;
        }
    }
}
