using HRFlow.Application.Exceptions;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HRFlow.Api.Filters
{
    public class HttpGlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<HttpGlobalExceptionFilter> _logger;

        public HttpGlobalExceptionFilter(ILogger<HttpGlobalExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            _logger.LogError(new EventId(context.Exception.HResult), context.Exception, context.Exception.Message);

            var problemDetails = new ProblemDetails
            {
                Instance = context.HttpContext.Request.Path
            };

            switch (context.Exception)
            {
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