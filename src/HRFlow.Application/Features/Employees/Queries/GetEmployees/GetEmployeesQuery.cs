using HRFlow.Domain.Interfaces;
using HRFlow.Domain.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.Employees.Queries.GetEmployees;

/// <summary>Returns management snapshots for the directory or one resource after a successful write.</summary>
public sealed class GetEmployeesQuery : IRequest<IReadOnlyList<GetEmployeeDto>>
{
    public Guid? EmployeeId { get; set; }
}

/// <summary>Combines profile/reporting IDs with complete Identity roles so edits preserve existing capabilities.</summary>
public sealed class GetEmployeesQueryHandler(
    IApplicationDbContext context, IEmployeeRoleLookupService roleLookup) : IRequestHandler<GetEmployeesQuery, IReadOnlyList<GetEmployeeDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<GetEmployeeDto>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
    {
        var employees = await context.Employees.AsNoTracking()
            .Include(e => e.Department).Include(e => e.Manager)
            .Where(e => !request.EmployeeId.HasValue || e.Id == request.EmployeeId)
            .OrderBy(e => e.FullName).ToListAsync(cancellationToken);
        var actorIds = employees.Where(e => e.DeactivatedById.HasValue)
            .Select(e => e.DeactivatedById!.Value).Distinct().ToArray();
        var actorNames = await context.Employees.AsNoTracking().Where(e => actorIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.FullName, cancellationToken);
        var results = new List<GetEmployeeDto>();
        foreach (var employee in employees)
        {
            results.Add(new GetEmployeeDto
            {
                Id = employee.Id, FullName = employee.FullName, Email = employee.Email,
                DepartmentId = employee.DepartmentId, DepartmentName = employee.Department.Name,
                ManagerId = employee.ManagerId, ManagerName = employee.Manager?.FullName,
                Version = employee.Version, IsActive = employee.IsActive,
                IdentityUserId = employee.IdentityUserId,
                // SQLite retains the UTC value but not DateTime.Kind; emit an explicit UTC offset for the UI.
                DeactivatedAtUtc = employee.DeactivatedAtUtc.HasValue
                    ? DateTime.SpecifyKind(employee.DeactivatedAtUtc.Value, DateTimeKind.Utc) : null,
                DeactivationReason = employee.DeactivationReason,
                DeactivatedByName = employee.DeactivatedById.HasValue
                    ? actorNames.GetValueOrDefault(employee.DeactivatedById.Value) : null,
                Roles = await roleLookup.GetRolesByIdentityUserIdAsync(employee.IdentityUserId, cancellationToken)
            });
        }
        return results;
    }
}
