using System.Diagnostics;
using System.Security.Claims;

namespace HRFlow.Api.Middleware;

/// <summary>
/// Emits one structured completion event for each API request so terminal output can correlate
/// HTTP status, elapsed time, and authenticated caller without logging sensitive request data.
/// </summary>
public sealed class ApiRequestLoggingMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private const int SlowRequestThresholdMilliseconds = 500;

    private readonly RequestDelegate _next;
    private readonly ILogger<ApiRequestLoggingMiddleware> _logger;

    /// <summary>
    /// Initializes the middleware that records API request completion details.
    /// </summary>
    public ApiRequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<ApiRequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Adds a correlation ID to the response and records the request outcome after downstream
    /// authentication, authorization, and controller execution have completed.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        var correlationId = GetCorrelationId(context);
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        var stopwatch = Stopwatch.StartNew();
        await _next(context);
        stopwatch.Stop();

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var statusCode = context.Response.StatusCode;

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                "API request failed. {Method} {Path} returned {StatusCode} in {ElapsedMilliseconds} ms. CorrelationId: {CorrelationId}; UserId: {UserId}",
                context.Request.Method,
                context.Request.Path,
                statusCode,
                stopwatch.ElapsedMilliseconds,
                correlationId,
                userId);
            return;
        }

        if (statusCode >= StatusCodes.Status400BadRequest)
        {
            _logger.LogWarning(
                "API request was rejected. {Method} {Path} returned {StatusCode} in {ElapsedMilliseconds} ms. CorrelationId: {CorrelationId}; UserId: {UserId}",
                context.Request.Method,
                context.Request.Path,
                statusCode,
                stopwatch.ElapsedMilliseconds,
                correlationId,
                userId);
            return;
        }

        if (stopwatch.ElapsedMilliseconds >= SlowRequestThresholdMilliseconds)
        {
            _logger.LogWarning(
                "API request completed slowly. {Method} {Path} returned {StatusCode} in {ElapsedMilliseconds} ms. CorrelationId: {CorrelationId}; UserId: {UserId}",
                context.Request.Method,
                context.Request.Path,
                statusCode,
                stopwatch.ElapsedMilliseconds,
                correlationId,
                userId);
            return;
        }

        _logger.LogInformation(
            "API request completed. {Method} {Path} returned {StatusCode} in {ElapsedMilliseconds} ms. CorrelationId: {CorrelationId}; UserId: {UserId}",
            context.Request.Method,
            context.Request.Path,
            statusCode,
            stopwatch.ElapsedMilliseconds,
            correlationId,
            userId);
    }

    private static string GetCorrelationId(HttpContext context)
    {
        var suppliedCorrelationId = context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault();
        return string.IsNullOrWhiteSpace(suppliedCorrelationId)
            ? context.TraceIdentifier
            : suppliedCorrelationId;
    }
}
