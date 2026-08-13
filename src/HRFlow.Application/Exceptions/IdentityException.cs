namespace HRFlow.Application.Exceptions;

/// <summary>
/// Exception thrown when an ASP.NET Core Identity operation fails.
/// </summary>
public class IdentityException : Exception
{
    /// <summary>
    /// Initializes a new instance of the IdentityException class with a specified error message.
    /// </summary>
    public IdentityException(string message) : base(message)
    {
    }
}