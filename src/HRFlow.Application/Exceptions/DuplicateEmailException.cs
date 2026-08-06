namespace HRFlow.Application.Exceptions;

/// <summary>
/// Represents an error that occurs when attempting to create a user with an email that already exists.
/// </summary>
public class DuplicateEmailException : Exception
{
    public DuplicateEmailException(string email)
        : base($"An account with the email '{email}' already exists.")
    {
    }
}