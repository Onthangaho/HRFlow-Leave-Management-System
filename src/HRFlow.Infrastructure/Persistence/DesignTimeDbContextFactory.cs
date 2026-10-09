using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HRFlow.Infrastructure.Persistence;

/// <summary>Offline schema tooling does not boot the API, workers or Development seed paths.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<HRFlowDbContext>
{
    /// <summary>Requires explicit operator connection configuration; no fallback database is created.</summary>
    public HRFlowDbContext CreateDbContext(string[] args)
    {
        var configured = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(configured))
            throw new InvalidOperationException("Set ConnectionStrings__DefaultConnection explicitly for offline migration tooling; stop writers and verify a backup first.");
        try
        {
            var connection = new SqliteConnectionStringBuilder(configured) { ForeignKeys = true };
            return new HRFlowDbContext(new DbContextOptionsBuilder<HRFlowDbContext>().UseSqlite(connection.ToString()).Options);
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException("Offline SQLite connection configuration is invalid; no fallback database is used.");
        }
    }
}
