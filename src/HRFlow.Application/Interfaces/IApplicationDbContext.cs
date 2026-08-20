using HRFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Department> Departments { get; }
    DbSet<Employee> Employees { get; }
    DbSet<LeavePolicy> LeavePolicies { get; }
    DbSet<LeaveRequest> LeaveRequests { get; }
    DbSet<LeaveType> LeaveTypes { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}