using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace HRFlow.Infrastructure.Services.Operations;

/// <summary>OS file sharing coordinates process lifetimes with offline operations; it is not an in-memory lock.</summary>
public static class MaintenanceLease
{
    /// <summary>Every API process, including its worker, keeps this shared handle until process shutdown.</summary>
    public static FileStream ForApplication(IConfiguration configuration)
    {
        var configured = configuration.GetConnectionString("DefaultConnection")
            ?? $"Data Source={Path.Combine(AppContext.BaseDirectory, "hrflow.db")}";
        return Open(new SqliteConnectionStringBuilder(configured).DataSource, false);
    }

    /// <summary>Fails immediately while a cooperating API/operator holds the database's sidecar handle.</summary>
    public static FileStream ForOperator(string database) => Open(database, true);

    private static FileStream Open(string database, bool exclusive)
    {
        if (string.IsNullOrWhiteSpace(database) || database == ":memory:" || database.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Operations require an on-disk SQLite store.");
        var path = Path.GetFullPath(database) + ".operations-lock";
        if (File.Exists(path) && File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
            throw new InvalidOperationException("The maintenance lease location is unsafe.");
        try
        {
            return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                exclusive ? FileShare.None : FileShare.ReadWrite);
        }
        catch (IOException)
        {
            throw new InvalidOperationException("The store is in use or under maintenance. Stop all API/worker services and disable automatic restarts before retrying.");
        }
    }
}
