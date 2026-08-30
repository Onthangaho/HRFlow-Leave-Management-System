using HRFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace HRFlow.Domain.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Employee> Employees { get; }
        DbSet<Department> Departments { get; }
        DbSet<RefreshToken> RefreshTokens { get; }
        DbSet<LeavePolicy> LeavePolicies { get; }
        DbSet<LeaveRequest> LeaveRequests { get; }
        DbSet<LeaveType> LeaveTypes { get; }
        DbSet<AuditEntry> AuditEntries { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}