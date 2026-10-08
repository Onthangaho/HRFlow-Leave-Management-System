using HRFlow.Application.Interfaces;
using HRFlow.Application.Services;
using HRFlow.Domain.Models.Employees;
using HRFlow.Domain.Interfaces;
using HRFlow.Domain.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.Employees.Queries.GetEmployees;

/// <summary>Returns management snapshots for the directory or one resource after a successful write.</summary>
public sealed class GetEmployeesQuery : IRequest<IReadOnlyList<GetEmployeeDto>>
{
    public Guid? EmployeeId { get; set; }
    /// <summary>The server-derived Identity actor, not the employee being edited.</summary>
    public Guid ActorIdentityId { get; set; }
}

/// <summary>Combines profile/reporting IDs with complete Identity roles so edits preserve existing capabilities.</summary>
public sealed class GetEmployeesQueryHandler(
    IApplicationDbContext context, IEmployeeRoleLookupService roleLookup,
    ILeaveReportingReadTransaction transaction, CurrentAccountAuthorization authorization, IAccountActivationService activation) : IRequestHandler<GetEmployeesQuery, IReadOnlyList<GetEmployeeDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<GetEmployeeDto>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
    {
        return await transaction.ExecuteAsync<IReadOnlyList<GetEmployeeDto>>(async token =>
        {
            cancellationToken = token;
            await authorization.RequireIdentityAsync(request.ActorIdentityId, [EmployeeRoles.HrAdministrator], token);

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
                var state = await activation.GetStatusAsync(employee.IdentityUserId, cancellationToken);
                results.Add(new GetEmployeeDto
                {
                    RequiresActivation = state.RequiresActivation, InvitationDeliveryState = state.DeliveryState, ActivatedAtUtc = state.ActivatedAtUtc,
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
        }, cancellationToken);
    }
}
