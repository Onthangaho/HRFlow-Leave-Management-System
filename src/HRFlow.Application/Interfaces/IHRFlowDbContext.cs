using HRFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Interfaces;

public interface IHRFlowDbContext
{
    DbSet<Employee> Employees { get; }
    DbSet<Department> Departments { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}