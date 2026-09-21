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
    public Guid? ProcessedById { get; private set; }
    public DateTime? ProcessedOn { get; private set; }

    // Navigation properties
    public Employee Employee { get; private set; } = null!;
    public LeaveType LeaveType { get; private set; } = null!;
    public Employee? ProcessedBy { get; private set; }

    private readonly List<AuditEntry> _auditEntries = new();
    public IReadOnlyCollection<AuditEntry> AuditEntries => _auditEntries.AsReadOnly();

    private LeaveRequest()
    {
        // Required for EF Core
    }

    public static LeaveRequest Create(Guid employeeId, Guid leaveTypeId, DateTime startDate, DateTime endDate)
    {
        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid()
        };
        leaveRequest.Update(employeeId, leaveTypeId, startDate, endDate);
        return leaveRequest;
    }

    public void Update(Guid employeeId, Guid leaveTypeId, DateTime startDate, DateTime endDate)
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

        EmployeeId = employeeId;
        LeaveTypeId = leaveTypeId;
        StartDate = startDate.Date;
        EndDate = endDate.Date;
        Status = LeaveRequestStatus.Pending;
    }

    public void ValidateAgainstPolicy(int remainingBalance, IEnumerable<LeaveRequest> approvedRequests, LeavePolicy policy)
    {
        if (GetRequestedDays() > remainingBalance)
        {
            throw new DomainException("Requested leave exceeds available balance.");
        }

        if (!policy.AllowOverlap && approvedRequests.Where(ar => ar.LeaveTypeId == LeaveTypeId).Any(ar => DatesOverlap(ar.StartDate, ar.EndDate)))
        {
            throw new DomainException("Requested leave overlaps with another approved request of the same type.");
        }
    }

    public int GetRequestedDays() => (EndDate - StartDate).Days + 1;

    private bool DatesOverlap(DateTime otherStart, DateTime otherEnd)
    {
        return StartDate <= otherEnd && otherStart <= EndDate;
    }

    public void Approve(Guid actorId)
    {
        if (Status != LeaveRequestStatus.Pending)
        {
            throw new DomainException("Only pending requests can be approved.");
        }

        var oldStatus = Status;
        Status = LeaveRequestStatus.Approved;
        ProcessedById = actorId;
        ProcessedOn = DateTime.UtcNow;

        _auditEntries.Add(AuditEntry.Create(Id, actorId, "Approve", oldStatus, Status));
    }

    public void Reject(Guid actorId)
    {
        if (Status != LeaveRequestStatus.Pending)
        {
            throw new DomainException("Only pending requests can be rejected.");
        }

        var oldStatus = Status;
        Status = LeaveRequestStatus.Rejected;
        ProcessedById = actorId;
        ProcessedOn = DateTime.UtcNow;

        _auditEntries.Add(AuditEntry.Create(Id, actorId, "Reject", oldStatus, Status));
    }

    /// <summary>
    /// Withdraws an employee-owned request before a manager or administrator has made it
    /// read-only by approving or rejecting it.
    /// </summary>
    public void Cancel(Guid actorId)
    {
        if (Status != LeaveRequestStatus.Pending)
        {
            throw new DomainException("Only pending leave requests can be cancelled.");
        }

        var oldStatus = Status;
        Status = LeaveRequestStatus.Cancelled;
        ProcessedById = actorId;
        ProcessedOn = DateTime.UtcNow;

        _auditEntries.Add(AuditEntry.Create(Id, actorId, "Cancel", oldStatus, Status));
    }

    public bool CanBeModifiedBy(Employee actor, IEnumerable<string> actorRoles, Employee requestOwner)
    {
        if (actor.Id == EmployeeId)
        {
            return false; // Employees cannot approve their own requests
        }

        if (actorRoles.Contains("HR Administrator"))
        {
            return true;
        }

        return requestOwner is not null && actor.Id == requestOwner.ManagerId;
    }
}