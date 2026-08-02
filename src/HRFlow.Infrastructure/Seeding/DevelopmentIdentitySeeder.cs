using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HRFlow.Infrastructure.Seeding;

/// <summary>
/// Seeds the development-only identity entry point so the application can be accessed on a clean local database.
/// </summary>
public static class DevelopmentIdentitySeeder
{
    private const string HrAdministratorRoleName = "HR Administrator";
    private const string HrAdministratorEmail = "hr.administrator@hrflow.local";
    private const string DefaultHrAdministratorPassword = "HrFlow!Dev2026";
    private const string EmployeeRoleName = "Employee";
    private const string EmployeeEmail = "employee@hrflow.local";
    private const string DefaultEmployeePassword = "HrFlow!Employee2026";

    /// <summary>
    /// Creates the development HR Administrator account once and self-guards to no-op outside Development.
    /// <summary>
    /// Seeds the development HR Administrator and Employee roles and accounts when running in the Development environment.
    /// </summary>
    public static async Task SeedDevelopmentAdministratorAsync(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        using var scope = serviceProvider.CreateScope();
        var hostEnvironment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        if (!hostEnvironment.IsDevelopment())
        {
            return;
        }

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var hrAdministratorPassword =
            configuration["Seeding:HrAdministratorPassword"] ?? DefaultHrAdministratorPassword;
        var employeePassword =
            configuration["Seeding:EmployeePassword"] ?? DefaultEmployeePassword;

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("DevelopmentIdentitySeeder");

        await EnsureRoleExistsAsync(roleManager, HrAdministratorRoleName);
        await EnsureRoleExistsAsync(roleManager, EmployeeRoleName);

        await EnsureUserInRoleAsync(
            userManager,
            logger,
            HrAdministratorEmail,
            hrAdministratorPassword,
            HrAdministratorRoleName,
            "HR Administrator");

        await EnsureUserInRoleAsync(
            userManager,
            logger,
            EmployeeEmail,
            employeePassword,
            EmployeeRoleName,
            "Employee");
    }

    /// <summary>
    /// Ensures that the specified role exists in the identity store.
    /// </summary>
    /// <param name="roleManager">The manager used to query and create roles.</param>
    /// <param name="roleName">The name of the role to ensure exists.</param>
    /// <exception cref="InvalidOperationException">Thrown when the role cannot be created.</exception>
    ///
    private static async Task EnsureRoleExistsAsync(RoleManager<IdentityRole> roleManager, string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        var createRoleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
        EnsureSucceeded(createRoleResult, $"create the {roleName} role");
    }

    /// <summary>
    /// Ensures a development account exists and is assigned to the specified role.
    /// </summary>
    /// <param name="email">The email address and username for the account.</param>
    /// <param name="password">The password used when creating the account.</param>
    /// <param name="roleName">The role to assign to the account.</param>
    /// <param name="accountLabel">The label used to identify the account in log messages and errors.</param>
    private static async Task EnsureUserInRoleAsync(
        UserManager<IdentityUser> userManager,
        ILogger logger,
        string email,
        string password,
        string roleName,
        string accountLabel)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is null)
        {
            var user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            EnsureSucceeded(createResult, $"create the development {accountLabel} account");

            existingUser = user;
            logger.LogInformation("Created development {AccountLabel} account {Email}.", accountLabel, email);
        }
        else
        {
            logger.LogInformation("Development {AccountLabel} account {Email} already exists.", accountLabel, email);
        }

        if (!await userManager.IsInRoleAsync(existingUser, roleName))
        {
            var addToRoleResult = await userManager.AddToRoleAsync(existingUser, roleName);
            EnsureSucceeded(addToRoleResult, $"assign the development {accountLabel} role");
        }
    }

    /// <summary>
    /// Ensures an identity operation succeeded.
    /// </summary>
    /// <param name="identityResult">The result of the identity operation.</param>
    /// <param name="actionDescription">A description of the action used in the exception message if the operation fails.</param>
    /// <exception cref="InvalidOperationException">Thrown when the identity operation fails.</exception>
    private static void EnsureSucceeded(IdentityResult identityResult, string actionDescription)
    {
        if (identityResult.Succeeded)
        {
            return;
        }

        var errors = string.Join(", ", identityResult.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"Unable to {actionDescription}: {errors}");
    }
}