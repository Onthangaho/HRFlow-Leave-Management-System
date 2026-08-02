using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HRFlow.Infrastructure.Seeding;

/// <summary>
/// Seeds the development-only identity entry point so the application can be accessed on a clean local database.
/// </summary>
public static class DevelopmentIdentitySeeder
{
    private const string HrAdministratorRoleName = "HR Administrator";
    private const string HrAdministratorEmail = "hr.administrator@hrflow.local";
    private const string HrAdministratorPassword = "HrFlow!Dev2026";

    /// <summary>
    /// Creates the development HR Administrator account once, but only when the host is running in Development.
    /// </summary>
    public static async Task SeedDevelopmentAdministratorAsync(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("DevelopmentIdentitySeeder");

        await EnsureRoleExistsAsync(roleManager);

        var existingAdmin = await userManager.FindByEmailAsync(HrAdministratorEmail);
        if (existingAdmin is null)
        {
            var adminUser = new IdentityUser
            {
                UserName = HrAdministratorEmail,
                Email = HrAdministratorEmail,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(adminUser, HrAdministratorPassword);
            EnsureSucceeded(createResult, "create the development HR Administrator account");

            existingAdmin = adminUser;
            logger.LogInformation("Created development HR Administrator account {Email}.", HrAdministratorEmail);
        }
        else
        {
            logger.LogInformation("Development HR Administrator account {Email} already exists.", HrAdministratorEmail);
        }

        if (!await userManager.IsInRoleAsync(existingAdmin, HrAdministratorRoleName))
        {
            var addToRoleResult = await userManager.AddToRoleAsync(existingAdmin, HrAdministratorRoleName);
            EnsureSucceeded(addToRoleResult, "assign the development HR Administrator role");
        }
    }

    private static async Task EnsureRoleExistsAsync(RoleManager<IdentityRole> roleManager)
    {
        if (await roleManager.RoleExistsAsync(HrAdministratorRoleName))
        {
            return;
        }

        var createRoleResult = await roleManager.CreateAsync(new IdentityRole(HrAdministratorRoleName));
        EnsureSucceeded(createRoleResult, "create the HR Administrator role");
    }

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