using HRFlow.Application.Interfaces;
using HRFlow.Domain.Interfaces;
using HRFlow.Domain.Interfaces.Services;
using HRFlow.Domain.Models.Employees;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Services;

/// <summary>Reads management reference data under the same snapshot as current authorization.</summary>
public sealed class ReferenceDataService(IApplicationDbContext context, CurrentAccountAuthorization authorization,
    ILeaveReportingReadTransaction transaction, IEmployeeRoleLookupService roles)
{
    /// <summary>Preserves authenticated department selection for every supported current capability.</summary>
    public Task<IReadOnlyList<DepartmentReferenceDto>> GetDepartmentsAsync(Guid actor, CancellationToken token) =>
        transaction.ExecuteAsync<IReadOnlyList<DepartmentReferenceDto>>(async protectedToken =>
        {
            await authorization.RequireIdentityAsync(actor, EmployeeRoles.All, protectedToken);
            return await context.Departments.AsNoTracking().OrderBy(d => d.Name)
                .Select(d => new DepartmentReferenceDto(d.Id, d.Name)).ToListAsync(protectedToken);
        }, token);

    /// <summary>Preserves the provisioned-role selector without exposing Identity entities or inventing available roles.</summary>
    public Task<IReadOnlyList<RoleReferenceDto>> GetRolesAsync(Guid actor, CancellationToken token) =>
        transaction.ExecuteAsync<IReadOnlyList<RoleReferenceDto>>(async protectedToken =>
        {
            await authorization.RequireIdentityAsync(actor, [EmployeeRoles.HrAdministrator], protectedToken);
            return (await roles.GetAvailableRoleNamesAsync(protectedToken)).Select(name => new RoleReferenceDto(name)).ToList();
        }, token);
}

/// <summary>Contains the department selector fields already required by the client.</summary>
public sealed record DepartmentReferenceDto(Guid Id, string Name);

/// <summary>Names a provisioned capability without exposing Identity persistence.</summary>
public sealed record RoleReferenceDto(string Name);
