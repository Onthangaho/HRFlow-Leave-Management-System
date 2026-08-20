using HRFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Interfaces;

/// <summary>
/// Defines the application's database context contract for domain entity access and persistence.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Department> Departments { get; }
    DbSet<Employee> Employees { get; }
    DbSet<LeavePolicy> LeavePolicies { get; }
    DbSet<LeaveRequest> LeaveRequests { get; }
    DbSet<LeaveType> LeaveTypes { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    /// <summary>
    /// Asynchronously saves all pending changes to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}