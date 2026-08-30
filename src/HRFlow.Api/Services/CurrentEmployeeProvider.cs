using HRFlow.Application.Interfaces;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HRFlow.Api.Services;

/// <summary>
/// Resolves the current HTTP caller's JWT NameIdentifier claim to the matching domain Employee
/// by joining on Employee.IdentityUserId. Used by all leave-request controllers so that domain
/// identifiers (Employee.Id) are always passed downstream instead of the raw Identity UserId,
/// which would cause ManagerId / ApproverId comparisons to silently fail.
/// </summary>
public class CurrentEmployeeProvider(IHttpContextAccessor httpContextAccessor, IApplicationDbContext dbContext) : ICurrentEmployeeProvider
{
    public async Task<Employee?> GetCurrentEmployeeAsync(CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return null;
        }

        var identityUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(identityUserId))
        {
            return null;
        }

        return await dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.IdentityUserId == identityUserId, cancellationToken);
    }
}
