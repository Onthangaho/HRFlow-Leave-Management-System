using HRFlow.Application.Interfaces.Auth;
using HRFlow.Application.Interfaces.Employees;
using HRFlow.Infrastructure.Persistence;
using HRFlow.Infrastructure.Services.Auth;
using HRFlow.Infrastructure.Services.Employees;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HRFlow.Infrastructure.Extensions;

/// <summary>
/// Registers the infrastructure services required for the HRFlow API, including EF Core and ASP.NET Core Identity.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the EF Core DbContext and ASP.NET Core Identity services for the configured SQLite database.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var databasePath = Path.Combine(AppContext.BaseDirectory, "hrflow.db");
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? $"Data Source={databasePath}";

        services.AddDbContext<HRFlowDbContext>(options => options.UseSqlite(connectionString));
        services.AddIdentityCore<IdentityUser>(options =>
            {
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<HRFlowDbContext>()
            .AddSignInManager<SignInManager<IdentityUser>>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmployeeManagementService, EmployeeManagementService>();

        return services;
    }
}
