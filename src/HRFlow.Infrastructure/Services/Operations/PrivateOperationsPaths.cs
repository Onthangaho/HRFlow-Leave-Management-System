using System.Security.AccessControl;
using System.Security.Principal;

namespace HRFlow.Infrastructure.Services.Operations;

/// <summary>Operator artifacts stay on restricted local storage, never application/static paths or links.</summary>
public static class PrivateOperationsPaths
{
    /// <summary>Validates absolute local paths and existing ancestors before accessing data.</summary>
    public static string Check(string value, string applicationRoot, bool mustExist = true)
    {
        if (!Path.IsPathFullyQualified(value) || value.StartsWith(@"\\") || value.StartsWith("//") || value.Contains("file:", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Unsafe operator path.");
        var path = Path.GetFullPath(value);
        if (new DriveInfo(Path.GetPathRoot(path)!).DriveType == DriveType.Network) throw new InvalidDataException("Network storage is unsupported.");
        foreach (var root in new[] { Path.GetFullPath(applicationRoot), AppContext.BaseDirectory, RepositoryRoot() })
            if (root != null && Within(path, root)) throw new InvalidDataException("Operator storage must be outside application/source/public roots.");
        if (mustExist && !File.Exists(path) && !Directory.Exists(path)) throw new InvalidDataException("Required operator input is missing.");
        var parent = Directory.Exists(path) ? new DirectoryInfo(path) : new DirectoryInfo(Path.GetDirectoryName(path)!);
        for (var item = parent; item != null; item = item.Parent)
            if (item.Exists && item.Attributes.HasFlag(FileAttributes.ReparsePoint)) throw new InvalidDataException("Linked storage is unsupported.");
        if (File.Exists(path) && File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint)) throw new InvalidDataException("Linked input is unsupported.");
        if (!parent.Exists) throw new InvalidDataException("Provision the private parent directory first.");
        RequireRestricted(parent.FullName);
        if (File.Exists(path)) RequireRestricted(path);
        return path;
    }

    /// <summary>Directory containment comparisons are conservative across Windows/Linux backup portability.</summary>
    public static bool Within(string path, string root) => path.Equals(root, StringComparison.OrdinalIgnoreCase)
        || path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    /// <summary>Requires owner-only Unix permissions or an explicit Windows owner/system/admin ACL.</summary>
    public static void RequireRestricted(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            var security = Directory.Exists(path) ? (FileSystemSecurity)new DirectoryInfo(path).GetAccessControl() : new FileInfo(path).GetAccessControl();
            var allowed = new[] { WindowsIdentity.GetCurrent().User!.Value, "S-1-5-18", "S-1-5-32-544" };
            foreach (FileSystemAccessRule rule in security.GetAccessRules(true, true, typeof(SecurityIdentifier)))
                if (rule.AccessControlType == AccessControlType.Allow && !allowed.Contains(rule.IdentityReference.Value))
                    throw new InvalidDataException("Restrict operator storage permissions before proceeding.");
        }
        else if ((File.GetUnixFileMode(path) & (UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute
            | UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute)) != 0)
            throw new InvalidDataException("Operator storage must be owner-only.");
    }

    private static string? RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            if (Directory.Exists(Path.Combine(directory.FullName, ".git"))) return directory.FullName;
        return null;
    }
}
