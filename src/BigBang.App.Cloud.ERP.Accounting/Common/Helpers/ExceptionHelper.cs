using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using BigBang.WebServer.Common.Exceptions;
using FluentValidation.Results;

namespace BigBang.App.Cloud.ERP.Accounting.Common.Helpers
{
    internal static class ExceptionHelper
    {
        public static BigBangException NotFound(string entityName)
        {
            return BigBangException
                .New()
                .WithMessage(string.Format(Messages.Error_EntityNotFound, entityName))
                .WithHttpStatusCode(HttpStatusCode.NotFound)
                .Build();
        }

        public static BigBangException BadRequest(IList<ValidationFailure> errors)
        {
            var errorMessages = errors.Select(failure => failure.ErrorMessage);
            var message = string.Join(Environment.NewLine, errorMessages);
            return BadRequestException(message);
        }

        public static BigBangException BadRequest(IList<string> errors)
        {
            var message = string.Join(Environment.NewLine, errors);
            return BadRequestException(message);
        }

        public static BigBangException BadRequest(string message)
        {
            return BadRequestException(message);
        }

        public static BigBangException Forbidden(string message)
        {
            return BigBangException
                .New()
                .WithMessage(message)
                .WithHttpStatusCode(HttpStatusCode.Forbidden)
                .Build();
        }

        private static BigBangException BadRequestException(string message)
        {
            return BigBangException
                .New()
                .WithMessage(message)
                .WithHttpStatusCode(HttpStatusCode.BadRequest)
                .Build();

        }
    }
}
