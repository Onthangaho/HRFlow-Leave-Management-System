namespace HRFlow.Application.Exceptions;

/// <summary>
/// Exception thrown when authentication fails due to invalid username or password.
/// Mapped to HTTP 401 Unauthorized by the global exception filter.
/// </summary>
public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException() : base("Invalid credentials.")
    {
    }

    public InvalidCredentialsException(string message) : base(message)
    {
    }

    public InvalidCredentialsException(string message, Exception innerException) : base(message, innerException)
    {
    }
}