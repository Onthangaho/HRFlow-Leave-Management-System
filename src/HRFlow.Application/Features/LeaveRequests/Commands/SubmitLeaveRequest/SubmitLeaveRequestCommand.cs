using MediatR;

namespace HRFlow.Application.Features.LeaveRequests.Commands.SubmitLeaveRequest;

/// <summary>
/// Submits a new leave request for an employee.
/// </summary>
public class SubmitLeaveRequestCommand : IRequest<Guid>
{
    /// <summary>Optional for legacy callers; keyed clients retain this exact attempt for explicit recovery.</summary>
    public Guid? SubmissionKey { get; set; }
    public Guid ExpectedTypeVersion { get; set; }
    public Guid ExpectedPolicyVersion { get; set; }
    public string? Description { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<Guid> DocumentIds { get; set; } = [];
}
