namespace HRFlow.Application.Interfaces;

/// <summary>Supplies request diagnostic metadata without coupling business operations to HTTP.</summary>
public interface IRequestCorrelationContext
{
    /// <summary>Null outside an HTTP operation; historical records are never backfilled.</summary>
    string? CorrelationId { get; }
}
