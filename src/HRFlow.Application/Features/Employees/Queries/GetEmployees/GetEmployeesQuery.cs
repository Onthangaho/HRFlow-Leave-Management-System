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
        var results = new List<GetEmployeeDto>();
        foreach (var employee in employees)
        {
            results.Add(new GetEmployeeDto
            {
                Id = employee.Id, FullName = employee.FullName, Email = employee.Email,
                DepartmentId = employee.DepartmentId, DepartmentName = employee.Department.Name,
                ManagerId = employee.ManagerId, ManagerName = employee.Manager?.FullName,
                Version = employee.Version,
                Roles = await roleLookup.GetRolesByIdentityUserIdAsync(employee.IdentityUserId, cancellationToken)
            });
        }
        return results;
    }
}
