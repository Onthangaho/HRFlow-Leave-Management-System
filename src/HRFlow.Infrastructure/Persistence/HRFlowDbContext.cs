using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Infrastructure.Persistence;

/// <summary>
/// Persists application data and the ASP.NET Core Identity schema for the HRFlow backend.
/// </summary>
public sealed class HRFlowDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
{
    /// <summary>
    /// Creates a new EF Core context for HRFlow with the supplied options.
    /// </summary>
    public HRFlowDbContext(DbContextOptions<HRFlowDbContext> options)
        : base(options)
    {
    }
}
