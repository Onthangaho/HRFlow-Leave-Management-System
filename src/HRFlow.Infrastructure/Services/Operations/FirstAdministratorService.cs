using System.Globalization;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Models.Employees;
using HRFlow.Infrastructure.Extensions;
using HRFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HRFlow.Infrastructure.Services.Operations;

/// <summary>Explicit operator facts; exactly one department selection is required, never a seeded default.</summary>
public sealed record FirstAdministratorOptions(string Database, string ApplicationRoot, string PasswordFile,
    string Name, string Email, string EmployeeNumber, string EmploymentStartDate, Guid? DepartmentId, string? CreateDepartment);

/// <summary>Safe refusal categories contain no operator-supplied values or Identity validation payloads.</summary>
public enum FirstAdministratorRefusal { InvalidInput, NonemptyStore, SchemaMismatch, ConflictingRoles, DepartmentUnavailable, IdentityRejected }

/// <summary>Allows the CLI to explain expected refusal without printing private inputs or exception messages.</summary>
public sealed class FirstAdministratorException(FirstAdministratorRefusal reason) : Exception("First administrator provisioning refused.")
{
    /// <summary>Identifies remediation without exposing private operator input.</summary>
    public FirstAdministratorRefusal Reason { get; } = reason;
}

/// <summary>Bootstraps an empty migrated store offline; never recovers, repairs or provisions a second account.</summary>
public static class FirstAdministratorService
{
    private const int MaximumPasswordBytes = 1024;
    private const int MaximumPasswordCharacters = 256;
    private const int MaximumDepartmentName = 100;

    /// <summary>Reserves the writer before empty-store checks; Identity saves remain inside this one transaction without replay.</summary>
    public static async Task ProvisionAsync(FirstAdministratorOptions options)
    {
        if (!Path.IsPathFullyQualified(options.ApplicationRoot) || !Directory.Exists(options.ApplicationRoot))
            throw Refuse(FirstAdministratorRefusal.InvalidInput);
        var database = PrivateOperationsPaths.Check(options.Database, options.ApplicationRoot);
        var passwordFile = PrivateOperationsPaths.Check(options.PasswordFile, options.ApplicationRoot);
        if (database == passwordFile || new FileInfo(passwordFile).Length is < 1 or > MaximumPasswordBytes)
            throw Refuse(FirstAdministratorRefusal.InvalidInput);
        var password = File.ReadAllText(passwordFile).TrimEnd('\r', '\n');
        if (password.Length is < 1 or > MaximumPasswordCharacters || password.Contains('\r') || password.Contains('\n'))
            throw Refuse(FirstAdministratorRefusal.InvalidInput);

        WriterQuiescence.RequireStopped();
        using var lease = MaintenanceLease.ForOperator(database);
        PrivateOperationsPaths.Check(lease.Name, options.ApplicationRoot);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = new SqliteConnectionStringBuilder
            { DataSource = database, Mode = SqliteOpenMode.ReadWrite, ForeignKeys = true, Pooling = false, DefaultTimeout = 3 }.ToString()
        }).Build();
        var services = new ServiceCollection();
        // This composition root creates no host, logger provider, seeds, workers or invitation delivery.
        services.AddLogging(); services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HRFlowDbContext>();
        await db.Database.OpenConnectionAsync(); db.Database.SetCommandTimeout(3);
        using var writer = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: false);
        await using var transaction = await db.Database.UseTransactionAsync(writer);
        if (!(await db.Database.GetAppliedMigrationsAsync()).SequenceEqual(db.Database.GetMigrations()))
            throw Refuse(FirstAdministratorRefusal.SchemaMismatch);
        if (await db.Users.AnyAsync() || await db.Employees.AnyAsync()) throw Refuse(FirstAdministratorRefusal.NonemptyStore);
        if ((options.DepartmentId.HasValue == (options.CreateDepartment != null))
            || options.DepartmentId == Guid.Empty
            || !DateOnly.TryParseExact(options.EmploymentStartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            throw Refuse(FirstAdministratorRefusal.InvalidInput);

        Department department;
        if (options.DepartmentId is {} id)
            department = await db.Departments.SingleOrDefaultAsync(d => d.Id == id) ?? throw Refuse(FirstAdministratorRefusal.DepartmentUnavailable);
        else
        {
            var name = options.CreateDepartment!.Trim();
            if (name.Length is < 1 or > MaximumDepartmentName || name.Any(char.IsControl)) throw Refuse(FirstAdministratorRefusal.InvalidInput);
            if ((await db.Departments.Select(d => d.Name).ToListAsync()).Any(n => string.Equals(n.Trim(), name, StringComparison.OrdinalIgnoreCase)))
                throw Refuse(FirstAdministratorRefusal.DepartmentUnavailable);
            department = new Department { Id = Guid.NewGuid(), Name = name }; db.Departments.Add(department);
        }
        Employee employee;
        try
        {
            employee = Employee.Create(options.Name, options.Email, department.Id);
            employee.ConfirmEmployment(options.EmployeeNumber, date);
        }
        catch (Exception error) when (error is InvalidOperationException or HRFlow.Domain.Common.DomainException)
        { throw Refuse(FirstAdministratorRefusal.InvalidInput); }
        var normalizer = scope.ServiceProvider.GetRequiredService<ILookupNormalizer>();
        var existingRoles = await db.Roles.ToListAsync();
        if (existingRoles.Any(r => !EmployeeRoles.All.Contains(r.Name!, StringComparer.Ordinal)
                || r.Id == Guid.Empty || r.NormalizedName != normalizer.NormalizeName(r.Name))
            || existingRoles.GroupBy(r => r.NormalizedName, StringComparer.Ordinal).Any(g => g.Count() != 1))
            throw Refuse(FirstAdministratorRefusal.ConflictingRoles);
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in EmployeeRoles.All)
            if (!existingRoles.Any(r => r.Name == role)
                && !(await roles.CreateAsync(new IdentityRole<Guid>(role) { Id = Guid.NewGuid() })).Succeeded)
                throw Refuse(FirstAdministratorRefusal.ConflictingRoles);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(), UserName = employee.Email, Email = employee.Email, RequiresActivation = false,
            ActivatedAtUtc = DateTime.UtcNow, EmailConfirmed = false
        };
        if (!(await users.CreateAsync(user, password)).Succeeded || !(await users.AddToRoleAsync(user, EmployeeRoles.HrAdministrator)).Succeeded)
            throw Refuse(FirstAdministratorRefusal.IdentityRejected);
        employee.SetIdentityUser(user.Id.ToString()); db.Employees.Add(employee);
        await db.SaveChangesAsync();
        await transaction!.CommitAsync();
    }

    private static FirstAdministratorException Refuse(FirstAdministratorRefusal reason) => new(reason);
}
