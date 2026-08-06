namespace HRFlow.Application.Exceptions;

/// <summary>
/// Raised when a required resource (employee, identity user, etc.) cannot be located.
/// Callers (e.g. API endpoints) should map this to HTTP 404 rather than 400 so clients
/// can distinguish "bad request" from "resource not found".
/// </summary>
public sealed class NotFoundException : Exception
{
    /// <summary>Initialises the exception with a descriptive message identifying the missing resource.</summary>
    public NotFoundException(string message) : base(message)
    {
    }
}
