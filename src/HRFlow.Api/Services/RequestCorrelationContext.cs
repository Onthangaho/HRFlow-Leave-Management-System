using HRFlow.Application.Interfaces;

namespace HRFlow.Api.Services;

/// <summary>Uses the same validated identifier as request logging and response headers.</summary>
public sealed class RequestCorrelationContext(IHttpContextAccessor accessor) : IRequestCorrelationContext
{
    /// <inheritdoc />
    public string? CorrelationId => accessor.HttpContext?.TraceIdentifier;
}
