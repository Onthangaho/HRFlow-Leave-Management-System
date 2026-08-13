namespace HRFlow.Domain.Common;

/// <summary>
/// Base class for all domain entities providing unique identifier.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }
}

/// <summary>
/// Exception thrown when a domain rule or invariant is violated.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}