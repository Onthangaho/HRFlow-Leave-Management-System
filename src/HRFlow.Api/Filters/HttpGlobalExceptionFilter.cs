using HRFlow.Application.Exceptions;
using HRFlow.Domain.Common;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HRFlow.Api.Filters
{
    /// <summary>
    /// Global exception filter that translates application exceptions into RFC 7807 ProblemDetails responses.
    /// Maps domain exceptions like InvalidCredentialsException and DuplicateEmailException to appropriate HTTP status codes.
    /// </summary>
    public class HttpGlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<HttpGlobalExceptionFilter> _logger;

        public HttpGlobalExceptionFilter(ILogger<HttpGlobalExceptionFilter> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Intercepts unhandled exceptions from MVC pipeline and converts them to ProblemDetails with appropriate status codes.
        /// Logs all exceptions and marks them as handled to prevent ASP.NET Core default error handling.
        /// </summary>
        public void OnException(ExceptionContext context)
        {
            _logger.LogError(new EventId(context.Exception.HResult), context.Exception, context.Exception.Message);

            var problemDetails = new ProblemDetails
            {
                Instance = context.HttpContext.Request.Path
            };

            switch (context.Exception)
            {
                case DomainException e:
                    problemDetails.Title = "A domain error occurred.";
                    problemDetails.Detail = e.Message;
                    problemDetails.Status = (int)HttpStatusCode.BadRequest;
                    problemDetails.Type = "https://www.rfc-editor.org/rfc/rfc7807";
                    break;
                case IdentityException e:
                    _logger.LogError(e, "Identity operation failed: {ErrorMessage}", e.Message);
                    problemDetails.Title = "An identity error occurred.";
                    problemDetails.Detail = "Unable to complete the identity operation. Please contact support if the issue persists.";
                    problemDetails.Status = (int)HttpStatusCode.BadRequest;
                    problemDetails.Type = "https://www.rfc-editor.org/rfc/rfc7807";
                    break;
                case InvalidCredentialsException e:
                    problemDetails.Title = "Invalid credentials";
                    problemDetails.Detail = e.Message;
                    problemDetails.Status = (int)HttpStatusCode.Unauthorized;
                    problemDetails.Type = "https://www.rfc-editor.org/rfc/rfc7807";
                    break;
                case DuplicateEmailException e:
                    problemDetails.Title = "Duplicate email";
                    problemDetails.Detail = e.Message;
                    problemDetails.Status = (int)HttpStatusCode.Conflict;
                    problemDetails.Type = "https://www.rfc-editor.org/rfc/rfc7807";
                    break;
                case EmployeeNotFoundException e:
                    problemDetails.Title = "Employee not found";
                    problemDetails.Detail = e.Message;
                    problemDetails.Status = (int)HttpStatusCode.NotFound;
                    problemDetails.Type = "https://www.rfc-editor.org/rfc/rfc7807";
                    break;
                case FluentValidation.ValidationException e:
                    problemDetails.Title = "A validation error occurred.";
                    problemDetails.Detail = e.Message;
                    problemDetails.Status = (int)HttpStatusCode.BadRequest;
                    problemDetails.Type = "https://www.rfc-editor.org/rfc/rfc7807";
                    break;
                default:
                    problemDetails.Title = "An unexpected error occurred.";
                    problemDetails.Detail = "An internal server error occurred.";
                    problemDetails.Status = (int)HttpStatusCode.InternalServerError;
                    problemDetails.Type = "https://www.rfc-editor.org/rfc/rfc7807";
                    break;
            }

            context.Result = new ObjectResult(problemDetails)
            {
                StatusCode = problemDetails.Status
            };
            context.ExceptionHandled = true;
        }
    }
}