using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using HRFlow.Domain.Entities;

namespace HRFlow.Infrastructure.Seeding;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var hostEnvironment = serviceProvider.GetRequiredService<IHostEnvironment>();
        if (!hostEnvironment.IsDevelopment())
        {
            return;
        }

        var context = serviceProvider.GetRequiredService<HRFlow.Infrastructure.Persistence.HRFlowDbContext>();
        await context.Database.MigrateAsync();

        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("DatabaseSeeder");

        try
        {
            await SeedIdentityAsync(serviceProvider, logger);
            await SeedApplicationDataAsync(serviceProvider, logger);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private static async Task SeedIdentityAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        const string HrAdministratorRoleName = "HR Administrator";
        const string HrAdministratorEmail = "hr.administrator@hrflow.local";
        const string DefaultHrAdministratorPassword = "HrFlow!Dev2026";
        const string EmployeeRoleName = "Employee";
        const string EmployeeEmail = "employee@hrflow.local";
        const string DefaultEmployeePassword = "HrFlow!Employee2026";
        const string ManagerRoleName = "Manager";
        const string ManagerEmail = "manager@hrflow.local";
        const string DefaultManagerPassword = "HrFlow!Manager2026";

        var hrAdministratorPassword =
            configuration["Seeding:HrAdministratorPassword"] ?? DefaultHrAdministratorPassword;
        var employeePassword =
            configuration["Seeding:EmployeePassword"] ?? DefaultEmployeePassword;
        var managerPassword =
            configuration["Seeding:ManagerPassword"] ?? DefaultManagerPassword;

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
    }

    private static async Task SeedApplicationDataAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        var context = serviceProvider.GetRequiredService<HRFlow.Infrastructure.Persistence.HRFlowDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await context.Departments.AnyAsync())
        {
            logger.LogInformation("Seeding Departments.");
            context.Departments.AddRange(
                new Department { Name = "Human Resources" },
                new Department { Name = "Engineering" },
                new Department { Name = "Marketing" },
                new Department { Name = "Sales" },
                new Department { Name = "Customer Support" }
            );
            await context.SaveChangesAsync();
        }

        if (!await context.LeaveTypes.AnyAsync(lt => lt.Name == "Unpaid"))
        {
            logger.LogInformation("Seeding Unpaid leave type.");

            var unpaidPolicy = LeavePolicy.Create(true, 0);
            context.LeavePolicies.Add(unpaidPolicy);

            var unpaidLeaveType = LeaveType.Create("Unpaid", unpaidPolicy);
            context.LeaveTypes.Add(unpaidLeaveType);
        }

        if (!await context.LeaveTypes.AnyAsync(lt => lt.Name == "Annual"))
        {
            logger.LogInformation("Seeding Annual leave type.");

            var annualPolicy = LeavePolicy.Create(false, 20);
            context.LeavePolicies.Add(annualPolicy);

            var annualLeaveType = LeaveType.Create("Annual", annualPolicy);
            context.LeaveTypes.Add(annualLeaveType);
        }

        if (!await context.LeaveTypes.AnyAsync(lt => lt.Name == "Sick"))
        {
            logger.LogInformation("Seeding Sick leave type.");

            var sickPolicy = LeavePolicy.Create(true, 10);
            context.LeavePolicies.Add(sickPolicy);

            var sickLeaveType = LeaveType.Create("Sick", sickPolicy);
            context.LeaveTypes.Add(sickLeaveType);
        }

        var managerUser = await userManager.FindByEmailAsync("manager@hrflow.local");
        if (managerUser is null)
        {
            logger.LogWarning("Manager user not found, skipping manager assignment.");
            return;
        }

        var managerEmployee = await context.Employees.FirstOrDefaultAsync(e => e.Email == "manager@hrflow.local");
        if (managerEmployee is null)
        {
            var department = await context.Departments.FirstOrDefaultAsync();
            if(department is null)
            {
                logger.LogWarning("No departments found, skipping manager employee creation.");
                return;
            }
            managerEmployee = Employee.Create("Manager User", "manager@hrflow.local", department.Id);
            managerEmployee.SetIdentityUser(managerUser.Id.ToString());
            context.Employees.Add(managerEmployee);
        }
        else if (string.IsNullOrEmpty(managerEmployee.IdentityUserId))
        {
            managerEmployee.SetIdentityUser(managerUser.Id.ToString());
        }

        var employeeUser = await userManager.FindByEmailAsync("employee@hrflow.local");
        var employeeToManage = await context.Employees.FirstOrDefaultAsync(e => e.Email == "employee@hrflow.local");
        if (employeeToManage is null)
        {
            var department = await context.Departments.FirstOrDefaultAsync();
            if(department is null)
            {
                logger.LogWarning("No departments found, skipping employee creation.");
                return;
            }
            employeeToManage = Employee.Create("Employee User", "employee@hrflow.local", department.Id);
            if (employeeUser is not null)
            {
                employeeToManage.SetIdentityUser(employeeUser.Id.ToString());
            }
            context.Employees.Add(employeeToManage);
        }
        else if (string.IsNullOrEmpty(employeeToManage.IdentityUserId) && employeeUser is not null)
        {
            employeeToManage.SetIdentityUser(employeeUser.Id.ToString());
        }

        if (employeeToManage.ManagerId is null)
        {
            employeeToManage.AssignManager(managerEmployee.Id);
            logger.LogInformation("Assigned manager to employee.");
        }

        var hrAdminUser = await userManager.FindByEmailAsync("hr.administrator@hrflow.local");
        var hrAdminEmployee = await context.Employees.FirstOrDefaultAsync(e => e.Email == "hr.administrator@hrflow.local");
        if (hrAdminEmployee is null)
        {
            var hrDepartment = await context.Departments.FirstOrDefaultAsync(d => d.Name == "Human Resources")
                               ?? await context.Departments.FirstOrDefaultAsync();
            if (hrDepartment is null)
            {
                logger.LogWarning("No departments found, skipping HR admin employee creation.");
                return;
            }
            hrAdminEmployee = Employee.Create("HR Administrator", "hr.administrator@hrflow.local", hrDepartment.Id);
            if (hrAdminUser is not null)
            {
                hrAdminEmployee.SetIdentityUser(hrAdminUser.Id.ToString());
            }
            context.Employees.Add(hrAdminEmployee);
            logger.LogInformation("Created HR Administrator employee record in Human Resources department.");
        }
        else if (string.IsNullOrEmpty(hrAdminEmployee.IdentityUserId) && hrAdminUser is not null)
        {
            hrAdminEmployee.SetIdentityUser(hrAdminUser.Id.ToString());
            logger.LogInformation("Linked HR Administrator identity user to existing employee record.");
        }
    }

    private static async Task EnsureRoleExistsAsync(RoleManager<IdentityRole<Guid>> roleManager, string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        var createRoleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
        EnsureSucceeded(createRoleResult, $"create the {roleName} role");
    }

    private static async Task EnsureUserInRoleAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        string email,
        string password,
        string roleName,
        string accountLabel)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is null)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            EnsureSucceeded(createResult, $"create the development {accountLabel} account");

            // Re-fetch the user to ensure we have the fully tracked entity from the DbContext
            existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser is null)
            {
                // This should never happen, but it's a safeguard.
                throw new InvalidOperationException($"Failed to find user '{email}' immediately after creation.");
            }
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