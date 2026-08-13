using HRFlow.Domain.Common;
using HRFlow.Domain.Enums;

namespace HRFlow.Domain.Entities;

public class LeaveRequest : BaseEntity
{
    public Guid EmployeeId { get; private set; }
    public Guid LeaveTypeId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public LeaveRequestStatus Status { get; private set; }

    // Navigation properties
    public Employee Employee { get; private set; } = null!;
    public LeaveType LeaveType { get; private set; } = null!;

    private LeaveRequest(Guid employeeId, Guid leaveTypeId, DateTime startDate, DateTime endDate)
    {
        EmployeeId = employeeId;
        LeaveTypeId = leaveTypeId;
        StartDate = startDate;
        EndDate = endDate;
        Status = LeaveRequestStatus.Pending;
    }

    public static LeaveRequest Create(Guid employeeId, Guid leaveTypeId, DateTime startDate, DateTime endDate)
    {
        if (employeeId == Guid.Empty)
        {
            throw new DomainException("EmployeeId cannot be empty.");
        }

        if (leaveTypeId == Guid.Empty)
        {
            throw new DomainException("LeaveTypeId cannot be empty.");
        }

        if (endDate < startDate)
        {
            throw new DomainException("EndDate cannot be earlier than StartDate.");
        }

        // Normalize dates to date-only (strip time component)
        startDate = startDate.Date;
        endDate = endDate.Date;

        return new LeaveRequest(employeeId, leaveTypeId, startDate, endDate);
    }

    public void ValidateAgainstPolicy(int availableBalance, IEnumerable<LeaveRequest> approvedRequests, LeavePolicy policy)
    {
        if (GetRequestedDays() > availableBalance)
        {
            throw new DomainException("Requested leave exceeds available balance.");
        }

        if (!policy.AllowOverlap && approvedRequests.Where(ar => ar.LeaveTypeId == LeaveTypeId).Any(ar => DatesOverlap(ar.StartDate, ar.EndDate)))
        {
            throw new DomainException("Requested leave overlaps with another approved request of the same type.");
        }
    }

    private int GetRequestedDays() => (EndDate - StartDate).Days + 1;

    private bool DatesOverlap(DateTime otherStart, DateTime otherEnd)
    {
        return StartDate <= otherEnd && otherStart <= EndDate;
    }
}