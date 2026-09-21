namespace HRFlow.Application.Exceptions;

/// <summary>
/// Raised when an authenticated caller is known but is not permitted to perform an operation on
/// the requested resource, allowing the API to return a precise HTTP 403 response.
/// </summary>
public sealed class ForbiddenException : Exception
{
    /// <summary>
    /// Initializes the exception with a safe explanation of the prohibited operation.
    /// </summary>
    public ForbiddenException(string message)
        : base(message)
    {
    }
}
