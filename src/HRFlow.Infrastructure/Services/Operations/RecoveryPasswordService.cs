using HRFlow.Domain.Entities;
using HRFlow.Infrastructure.Extensions;
using HRFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HRFlow.Infrastructure.Services.Operations;

/// <summary>Explicit offline credential re-establishment after human review, never role/lifecycle repair.</summary>
public static class RecoveryPasswordService
{
    /// <summary>Identity validators remain authoritative; pending/inactive or already-passworded accounts cannot be recovered here.</summary>
    public static async Task RecoverAsync(string database, Guid identityId, string passwordFile, string applicationRoot, bool reviewed)
    {
        if (!reviewed) throw new InvalidDataException("Review current employment, roles and reporting before --confirm-current-permissions.");
        database = PrivateOperationsPaths.Check(database, applicationRoot);
        passwordFile = PrivateOperationsPaths.Check(passwordFile, applicationRoot);
        if (database == passwordFile || new FileInfo(passwordFile).Length > 1024) throw new InvalidDataException("Unsafe password input.");
        var password = File.ReadAllText(passwordFile).TrimEnd('\r', '\n');
        if (password.Length is < 1 or > 256 || password.Contains('\r') || password.Contains('\n')) throw new InvalidDataException("Invalid bounded password input.");
        WriterQuiescence.RequireStopped();
        using var lease = MaintenanceLease.ForOperator(database);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:DefaultConnection"] = new SqliteConnectionStringBuilder { DataSource = database, Mode = SqliteOpenMode.ReadWrite, Pooling = false, DefaultTimeout = 3 }.ToString() }).Build();
        var services = new ServiceCollection(); services.AddLogging(); services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider(); using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HRFlowDbContext>();
        await context.Database.OpenConnectionAsync();
        using var sqlite = ((SqliteConnection)context.Database.GetDbConnection()).BeginTransaction(deferred: false);
        await using var transaction = await context.Database.UseTransactionAsync(sqlite);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByIdAsync(identityId.ToString());
        var employee = await context.Employees.SingleOrDefaultAsync(e => e.IdentityUserId == identityId.ToString());
        if (user == null || employee == null || !employee.IsActive || user.RequiresActivation || user.PasswordHash != null
            || !(await users.GetRolesAsync(user)).Any()) throw new InvalidDataException("Account is ineligible for reviewed recovery.");
        var result = await users.AddPasswordAsync(user, password);
        if (!result.Succeeded) throw new InvalidDataException("Identity password validation rejected the recovery credential.");
        user.CredentialVersion = Guid.NewGuid();
        await context.SaveChangesAsync(); await transaction!.CommitAsync();
    }
}
