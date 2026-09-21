using HRFlow.Domain.Exceptions;
using HRFlow.Domain.Common;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using HRFlow.Application.Exceptions;
using System.Security.Claims;

namespace HRFlow.Api.Filters
{
    /// <summary>
    /// Global exception filter that translates application exceptions into RFC 7807 ProblemDetails responses.
    /// Maps domain exceptions like InvalidCredentialsException and DuplicateEmailException to appropriate HTTP status codes.
    /// </summary>
    public class HttpGlobalExceptionFilter(ILogger<HttpGlobalExceptionFilter> logger) : IExceptionFilter
    {
        private const string ProblemDetailsType = "https://www.rfc-editor.org/rfc/rfc7807";

        /// <summary>
        /// Intercepts unhandled exceptions from MVC pipeline and converts them to ProblemDetails with appropriate status codes.
        /// Logs all exceptions and marks them as handled to prevent ASP.NET Core default error handling.
        /// </summary>
        public void OnException(ExceptionContext context)
        {
            var request = context.HttpContext.Request;
            var correlationId = context.HttpContext.Response.Headers["X-Correlation-ID"].FirstOrDefault()
                ?? context.HttpContext.TraceIdentifier;
            var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            var problemDetails = new ProblemDetails
            {
                Instance = request.Path
            };

            switch (context.Exception)
            {
                case DomainException e:
                    LogExpectedFailure(logger, e.GetType().Name, request, correlationId, userId, StatusCodes.Status400BadRequest);
                    problemDetails.Title = "A domain error occurred.";
                    problemDetails.Detail = e.Message;
                    problemDetails.Status = (int)HttpStatusCode.BadRequest;
                    problemDetails.Type = ProblemDetailsType;
                    break;
                case IdentityException e:
                    LogExpectedFailure(logger, e.GetType().Name, request, correlationId, userId, StatusCodes.Status400BadRequest);
                    problemDetails.Title = "An identity error occurred.";
                    problemDetails.Detail = "Unable to complete the identity operation. Please contact support if the issue persists.";
                    problemDetails.Status = (int)HttpStatusCode.BadRequest;
                    problemDetails.Type = ProblemDetailsType;
                    break;
                case InvalidCredentialsException e:
                    LogExpectedFailure(logger, e.GetType().Name, request, correlationId, userId, StatusCodes.Status401Unauthorized);
                    problemDetails.Title = "Invalid credentials";
                    problemDetails.Detail = e.Message;
                    problemDetails.Status = (int)HttpStatusCode.Unauthorized;
                    problemDetails.Type = ProblemDetailsType;
                    break;
                case DuplicateEmailException e:
                    LogExpectedFailure(logger, e.GetType().Name, request, correlationId, userId, StatusCodes.Status409Conflict);
                    problemDetails.Title = "Duplicate email";
                    problemDetails.Detail = e.Message;
                    problemDetails.Status = (int)HttpStatusCode.Conflict;
                    problemDetails.Type = ProblemDetailsType;
                    break;
                case EmployeeNotFoundException e:
                    LogExpectedFailure(logger, e.GetType().Name, request, correlationId, userId, StatusCodes.Status404NotFound);
                    problemDetails.Title = "Employee not found";
                    problemDetails.Detail = e.Message;
                    problemDetails.Status = (int)HttpStatusCode.NotFound;
                    problemDetails.Type = ProblemDetailsType;
                    break;
                case NotFoundException e:
                    LogExpectedFailure(logger, e.GetType().Name, request, correlationId, userId, StatusCodes.Status404NotFound);
                    problemDetails.Title = "Resource not found";
                    problemDetails.Detail = e.Message;
                    problemDetails.Status = (int)HttpStatusCode.NotFound;
                    problemDetails.Type = ProblemDetailsType;
                    break;
                case ForbiddenException e:
                    LogExpectedFailure(logger, e.GetType().Name, request, correlationId, userId, StatusCodes.Status403Forbidden);
                    problemDetails.Title = "Forbidden";
                    problemDetails.Detail = e.Message;
                    problemDetails.Status = (int)HttpStatusCode.Forbidden;
                    problemDetails.Type = ProblemDetailsType;
                    break;
                case FluentValidation.ValidationException e:
                    LogExpectedFailure(logger, e.GetType().Name, request, correlationId, userId, StatusCodes.Status400BadRequest);
                    problemDetails.Title = "A validation error occurred.";
                    problemDetails.Detail = e.Message;
                    problemDetails.Status = (int)HttpStatusCode.BadRequest;
                    problemDetails.Type = ProblemDetailsType;
                    break;
                default:
                    logger.LogError(
                        context.Exception,
                        "Unexpected API failure. {Method} {Path}; CorrelationId: {CorrelationId}; UserId: {UserId}",
                        request.Method,
                        request.Path,
                        correlationId,
                        userId);
                    problemDetails.Title = "An unexpected error occurred.";
                    problemDetails.Detail = "An internal server error occurred.";
                    problemDetails.Status = (int)HttpStatusCode.InternalServerError;
                    problemDetails.Type = ProblemDetailsType;
                    break;
            }

            context.Result = new ObjectResult(problemDetails)
            {
                StatusCode = problemDetails.Status
            };
            context.ExceptionHandled = true;
        }

        private static void LogExpectedFailure(
            ILogger logger,
            string exceptionType,
            HttpRequest request,
            string correlationId,
            string? userId,
            int statusCode)
        {
            logger.LogWarning(
                "Expected API failure. {Method} {Path} will return {StatusCode}; CorrelationId: {CorrelationId}; UserId: {UserId}; ExceptionType: {ExceptionType}",
                request.Method,
                request.Path,
                statusCode,
                correlationId,
                userId,
                exceptionType);
        }
    }
}