using HRFlow.Domain.Common;
using System.Security.AccessControl;
using System.Security.Principal;
using HRFlow.Domain.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
namespace HRFlow.Infrastructure.Services.Auth;
/// <summary>Development-only local pickup, not email; restricts the folder to the current OS identity.</summary>
public sealed class DevelopmentActivationDelivery(IHostEnvironment environment, IConfiguration configuration) : IActivationDelivery
{
    private string Profile => configuration["Activation:PickupProfile"] ?? "Default";
    /// <inheritdoc />
    public void EnsureConfigured()
    {
        if (Profile.Length is < 1 or > 64 || Profile.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-'))
            throw new DomainException("Activation:PickupProfile must be 1-64 ASCII letters, digits or hyphens.");
        if (!environment.IsDevelopment() || configuration["Activation:Delivery"] != "PrivatePickup")
            throw new DomainException("Account invitations are unavailable. Configure an approved delivery provider; private pickup is Development-only.");
    }
    /// <inheritdoc />
    public async Task DeliverAsync(Guid accountId, string email, Uri link, CancellationToken token)
    {
        EnsureConfigured();
        // Fixed location cannot be redirected into a repository, web root or shared directory by configuration.
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HRFlow", "ActivationPickup", Profile);
        if (!Path.IsPathFullyQualified(folder)) throw new IOException("Private pickup location is unavailable.");
        Directory.CreateDirectory(folder);
        if (OperatingSystem.IsWindows())
        {
            var security = new DirectorySecurity();
            security.SetAccessRuleProtection(true, false);
            security.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User!, FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            new DirectoryInfo(folder).SetAccessControl(security);
        }
        else File.SetUnixFileMode(folder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var path = Path.Combine(folder, accountId + "-" + Guid.NewGuid() + ".txt");
        await File.WriteAllTextAsync(path, email + Environment.NewLine + link.AbsoluteUri, token);
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
}
