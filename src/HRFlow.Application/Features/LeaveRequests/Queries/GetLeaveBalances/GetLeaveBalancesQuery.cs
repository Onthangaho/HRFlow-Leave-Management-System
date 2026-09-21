using HRFlow.Domain.Enums;
using HRFlow.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveRequests.Queries.GetLeaveBalances;

/// <summary>
/// Retrieves a current leave-balance snapshot for each leave type by deriving it from policy
/// entitlement and approved history at read time.
/// </summary>
public sealed class GetLeaveBalancesQuery : IRequest<IReadOnlyList<LeaveBalanceDto>>
{
    /// <summary>
    /// Gets or sets the employee whose approved leave history is used for the calculation.
    /// </summary>
    public Guid EmployeeId { get; set; }
}

/// <summary>
/// Handles the balance query without persisting a redundant balance field that could become stale.
/// </summary>
public sealed class GetLeaveBalancesQueryHandler
    : IRequestHandler<GetLeaveBalancesQuery, IReadOnlyList<LeaveBalanceDto>>
{
    private readonly IApplicationDbContext _context;

    /// <summary>
    /// Creates the handler with access to leave policies and approved leave history.
    /// </summary>
    public GetLeaveBalancesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Calculates current balance for every configured leave type for the requested employee.
    /// </summary>
    public async Task<IReadOnlyList<LeaveBalanceDto>> Handle(
        GetLeaveBalancesQuery request,
        CancellationToken cancellationToken)
    {
        var leaveTypes = await _context.LeaveTypes
            .AsNoTracking()
            .Include(leaveType => leaveType.LeavePolicy)
            .OrderBy(leaveType => leaveType.Name)
            .ToListAsync(cancellationToken);

        var approvedRequests = await _context.LeaveRequests
            .AsNoTracking()
            .Where(leaveRequest =>
                leaveRequest.EmployeeId == request.EmployeeId
                && leaveRequest.Status == LeaveRequestStatus.Approved)
            .ToListAsync(cancellationToken);

        return leaveTypes
            .Select(leaveType =>
            {
                var approvedRequestsForType = approvedRequests
                    .Where(leaveRequest => leaveRequest.LeaveTypeId == leaveType.Id);
                var balance = LeaveBalanceCalculator.Calculate(
                    leaveType.LeavePolicy.DefaultBalance,
                    approvedRequestsForType);

                return new LeaveBalanceDto
                {
                    LeaveTypeId = leaveType.Id,
                    LeaveTypeName = leaveType.Name,
                    EntitledDays = balance.EntitledDays,
                    UsedDays = balance.UsedDays,
                    RemainingDays = balance.RemainingDays
                };
            })
            .ToList();
    }
}
