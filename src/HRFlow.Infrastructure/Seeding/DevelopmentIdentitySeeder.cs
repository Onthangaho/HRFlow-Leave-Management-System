using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using HRFlow.Domain.Entities;

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
    private const string ManagerRoleName = "Manager";
    private const string ManagerEmail = "manager@hrflow.local";
    private const string DefaultManagerPassword = "HrFlow!Manager2026";

    /// <summary>
    /// Seeds the development HR Administrator, Manager, and Employee roles and accounts when running in the Development environment.
    /// </summary>
    /// <remarks>
    /// The Identity "Manager" role controls ACCESS to approval endpoints (authorization), while
    /// Employee.ManagerId controls WHICH employees' requests a given manager can see/approve (data scoping).
    /// These are two different concerns that work together, not interchangeable.
    /// </remarks>
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
        var managerPassword =
            configuration["Seeding:ManagerPassword"] ?? DefaultManagerPassword;

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("DevelopmentIdentitySeeder");

        logger.LogInformation("Seeding development identity roles and accounts.");

        await EnsureRoleExistsAsync(roleManager, HrAdministratorRoleName);
        await EnsureRoleExistsAsync(roleManager, EmployeeRoleName);
        await EnsureRoleExistsAsync(roleManager, ManagerRoleName);

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

        await EnsureUserInRoleAsync(
            userManager,
            logger,
            ManagerEmail,
            managerPassword,
            ManagerRoleName,
            "Manager");

        await SeedManagerDataAsync(scope.ServiceProvider);
        await SeedLeaveDataAsync(scope.ServiceProvider);
    }

    private static async Task SeedLeaveDataAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<HRFlow.Infrastructure.Persistence.HRFlowDbContext>();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("DevelopmentLeaveSeeder");

        if (!await context.LeaveTypes.AnyAsync(lt => lt.Name == "Unpaid"))
        {
            logger.LogInformation("Seeding Unpaid leave type.");

            var unpaidPolicy = LeavePolicy.Create(true, 0);
            context.LeavePolicies.Add(unpaidPolicy);

            var unpaidLeaveType = LeaveType.Create("Unpaid", unpaidPolicy);
            context.LeaveTypes.Add(unpaidLeaveType);

            await context.SaveChangesAsync();
        }

        if (!await context.LeaveTypes.AnyAsync(lt => lt.Name == "Annual"))
        {
            logger.LogInformation("Seeding Annual leave type.");

            var annualPolicy = LeavePolicy.Create(false, 20);
            context.LeavePolicies.Add(annualPolicy);

            var annualLeaveType = LeaveType.Create("Annual", annualPolicy);
            context.LeaveTypes.Add(annualLeaveType);

            await context.SaveChangesAsync();
        }

        if (!await context.LeaveTypes.AnyAsync(lt => lt.Name == "Sick"))
        {
            logger.LogInformation("Seeding Sick leave type.");

            var sickPolicy = LeavePolicy.Create(true, 10);
            context.LeavePolicies.Add(sickPolicy);

            var sickLeaveType = LeaveType.Create("Sick", sickPolicy);
            context.LeaveTypes.Add(sickLeaveType);

            await context.SaveChangesAsync();
        }
    }


    private static async Task SeedManagerDataAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<HRFlow.Infrastructure.Persistence.HRFlowDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("DevelopmentDataSeeder");

        var managerUser = await userManager.FindByEmailAsync(ManagerEmail);
        if (managerUser is null)
        {
            logger.LogWarning("Manager user not found, skipping manager assignment.");
            return;
        }

        var managerEmployee = await context.Employees.FirstOrDefaultAsync(e => e.Email == ManagerEmail);
        if (managerEmployee is null)
        {
            var department = await context.Departments.FirstOrDefaultAsync();
            if(department is null)
            {
                logger.LogWarning("No departments found, skipping manager employee creation.");
                return;
            }
            managerEmployee = Employee.Create("Manager User", ManagerEmail, department.Id);
            managerEmployee.SetIdentityUser(managerUser.Id);
            context.Employees.Add(managerEmployee);
            await context.SaveChangesAsync();
        }
        else if (string.IsNullOrEmpty(managerEmployee.IdentityUserId))
        {
            managerEmployee.SetIdentityUser(managerUser.Id);
        }

        var employeeUser = await userManager.FindByEmailAsync(EmployeeEmail);
        var employeeToManage = await context.Employees.FirstOrDefaultAsync(e => e.Email == EmployeeEmail);
        if (employeeToManage is null)
        {
            var department = await context.Departments.FirstOrDefaultAsync();
            if(department is null)
            {
                logger.LogWarning("No departments found, skipping employee creation.");
                return;
            }
            employeeToManage = Employee.Create("Employee User", EmployeeEmail, department.Id);
            if (employeeUser is not null)
            {
                employeeToManage.SetIdentityUser(employeeUser.Id);
            }
            context.Employees.Add(employeeToManage);
        }
        else if (string.IsNullOrEmpty(employeeToManage.IdentityUserId) && employeeUser is not null)
        {
            employeeToManage.SetIdentityUser(employeeUser.Id);
        }

        if (employeeToManage.ManagerId is null)
        {
            employeeToManage.AssignManager(managerEmployee.Id);
            logger.LogInformation("Assigned manager to employee.");
        }

        await context.SaveChangesAsync();
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