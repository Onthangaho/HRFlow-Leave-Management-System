using FluentValidation;
using HRFlow.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveRequests.Queries.GetLeaveBalances;

/// <summary>
/// Ensures a balance query is always scoped to an existing domain employee before querying leave
/// history.
/// </summary>
public sealed class GetLeaveBalancesQueryValidator : AbstractValidator<GetLeaveBalancesQuery>
{
    private readonly IApplicationDbContext _context;

    /// <summary>
    /// Creates the validator with database access to confirm the requested employee exists.
    /// </summary>
    public GetLeaveBalancesQueryValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(query => query.EmployeeId)
            .NotEmpty()
            .MustAsync(EmployeeExists)
            .WithMessage("Employee does not exist.");
    }

    private Task<bool> EmployeeExists(Guid employeeId, CancellationToken cancellationToken)
    {
        return _context.Employees.AnyAsync(
            employee => employee.Id == employeeId,
            cancellationToken);
    }
}
