using HRFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Interfaces;

/// <summary>
/// Defines the application-layer abstraction for persistence operations on HRFlow domain entities.
/// This interface isolates the Application layer from Infrastructure-specific EF Core implementation details,
/// enabling testability and preserving architectural boundaries.
/// </summary>
public interface IHRFlowDbContext
{
    DbSet<Employee> Employees { get; }
    DbSet<Department> Departments { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    /// <summary>
    /// Asynchronously saves all pending changes to the underlying data store.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the save operation.
    /// If cancelled, the operation will be abandoned and no changes will be persisted.
    /// </param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}