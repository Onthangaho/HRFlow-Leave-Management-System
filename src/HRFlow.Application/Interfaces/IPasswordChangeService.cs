namespace HRFlow.Application.Interfaces;
/// <summary>Changes only the authenticated actor's credentials and revokes every prior session atomically.</summary>
public interface IPasswordChangeService
{
    /// <summary>Uses Identity validation; failed changes leave all credentials and sessions unchanged.</summary>
    Task ChangeAsync(string currentPassword, string newPassword, CancellationToken token);
}

/// <summary>Shared server bounds for secret-bearing password-change requests.</summary>
public static class PasswordChangeLimits
{
    /// <summary>Bounds Identity hashing input without modifying or trimming the supplied secret.</summary>
    public const int MaximumPasswordLength = 256;
    /// <summary>Bounds the complete JSON payload before model binding.</summary>
    public const int MaximumBodyBytes = 4096;
}
