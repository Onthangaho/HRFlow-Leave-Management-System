using HRFlow.Application.Interfaces;
using HRFlow.Application.Exceptions;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Interfaces;
using HRFlow.Domain.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Services;

/// <summary>Checks live profile and Identity capabilities within the caller's read snapshot or writer reservation.</summary>
public sealed class CurrentAccountAuthorization(IApplicationDbContext context, IEmployeeRoleLookupService roles, IAccountAccessService access)
{
    /// <summary>Resolves the authenticated Identity actor without trusting the token's role claims.</summary>
    public async Task<Employee> RequireIdentityAsync(Guid identityId, IReadOnlyCollection<string> capabilities, CancellationToken token)
    {
        var employee = await context.Employees.AsNoTracking()
            .SingleOrDefaultAsync(e => e.IdentityUserId == identityId.ToString(), token);
        return await RequireAsync(employee, capabilities, token);
    }

    /// <summary>Rechecks a server-resolved personal actor after acquiring database protection.</summary>
    public async Task<Employee> RequireEmployeeAsync(Guid employeeId, IReadOnlyCollection<string> capabilities, CancellationToken token)
    {
        var employee = await context.Employees.AsNoTracking().SingleOrDefaultAsync(e => e.Id == employeeId, token);
        return await RequireAsync(employee, capabilities, token);
    }

    private async Task<Employee> RequireAsync(Employee? employee, IReadOnlyCollection<string> capabilities, CancellationToken token)
    {
        if (employee is null || !employee.IsActive || !await access.IsActiveAsync(employee.IdentityUserId, token)
            || !(await roles.GetRolesByIdentityUserIdAsync(employee.IdentityUserId, token)).Any(capabilities.Contains))
            throw new ForbiddenException("An active account with a current required permission is needed to access this resource.");
        return employee;
    }
}
