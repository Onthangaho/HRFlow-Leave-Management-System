using MediatR;

namespace HRFlow.Application.Features.LeaveRequests.Commands.SubmitLeaveRequest;

/// <summary>
/// Submits a new leave request for an employee.
/// </summary>
public class SubmitLeaveRequestCommand : IRequest<Guid>
{
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}