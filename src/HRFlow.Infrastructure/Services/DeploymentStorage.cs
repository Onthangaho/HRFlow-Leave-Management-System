using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace HRFlow.Infrastructure.Services;

/// <summary>Production storage must be provisioned explicitly; validation never creates or repairs databases.</summary>
public static class DeploymentStorage
{
    /// <summary>Checks private local paths and write permissions with an isolated disposable probe.</summary>
    public static void Validate(IConfiguration config, IHostEnvironment environment)
    {
        if (environment.IsDevelopment()) return;
        if (!config.GetValue<bool>("Storage:LocalFileSystem"))
            throw new InvalidOperationException("Confirm Storage:LocalFileSystem for persistent, single-host storage with compatible SQLite locking.");
        try
        {
            var connection = new SqliteConnectionStringBuilder(config.GetConnectionString("DefaultConnection") ?? "");
            var database = connection.DataSource;
            var privateRoot = config["Storage:PrivateFilesRoot"];
            foreach (var path in new[] { database, privateRoot })
            {
                if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path) || path.StartsWith(@"\\")
                    || path.StartsWith("//") || path.Contains("file:", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException();
                var full = Path.GetFullPath(path);
                if (new DriveInfo(Path.GetPathRoot(full)!).DriveType == DriveType.Network) throw new InvalidOperationException();
                for (var ancestor = Directory.Exists(full) ? new DirectoryInfo(full) : new DirectoryInfo(Path.GetDirectoryName(full)!); ancestor != null; ancestor = ancestor.Parent)
                    if (ancestor.Attributes.HasFlag(FileAttributes.ReparsePoint)) throw new InvalidOperationException();
                if (File.Exists(full) && File.GetAttributes(full).HasFlag(FileAttributes.ReparsePoint)) throw new InvalidOperationException();
                foreach (var root in new[] { environment.ContentRootPath, AppContext.BaseDirectory })
                    if (full.StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                        || full.Equals(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException();
            }
            if (connection.Mode != SqliteOpenMode.ReadWrite || connection.Cache == SqliteCacheMode.Shared
                || !File.Exists(database) || !Directory.Exists(privateRoot)) throw new InvalidOperationException();
            // Opening read/write neither truncates nor changes the existing store.
            using (File.Open(database, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)) { }
            foreach (var directory in new[] { Path.GetDirectoryName(database)!, privateRoot! })
            {
                var probe = Path.Combine(directory, ".hrflow-probe-" + Guid.NewGuid().ToString("N"));
                using var file = new FileStream(probe, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
                file.WriteByte(0);
            }
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            throw new InvalidOperationException("Storage configuration is unavailable or unsafe. Provision an existing writable local SQLite file (Mode=ReadWrite) and private directory outside application/public roots; check service permissions. No database was repaired.");
        }
    }
}
