using HRFlow.Domain.Common;
using HRFlow.Domain.Enums;

namespace HRFlow.Domain.Entities;

public class LeaveRequest : BaseEntity
{
    public string? Description { get; private set; }
    public RequestRequirementsSnapshot? SubmissionRequirements { get; private set; }

    /// <summary>Captures submission facts exactly once; later policy edits never replace them.</summary>
    public void CaptureRequirements(RequestRequirementsSnapshot snapshot, string? description)
    {
        if (SubmissionRequirements != null) throw new DomainException("Submission requirements cannot be replaced.");
        var text = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (text?.Length > RequirementModes.MaxTextLength) throw new DomainException("Description must contain at most 1000 trimmed characters.");
        if (snapshot.DescriptionMode == RequirementModes.Required && text == null) throw new DomainException("A description is required for this leave type. Do not include diagnoses.");
        if (snapshot.DescriptionMode == RequirementModes.NotRequested && text != null) throw new DomainException("This leave type does not request a description. Remove it explicitly before submitting.");
        SubmissionRequirements = snapshot; Description = text;
    }

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

    /// <summary>Records the actual initial submission only for newly created requests.</summary>
    public static LeaveRequest Create(Guid employeeId, Guid leaveTypeId, DateTime startDate, DateTime endDate, string? correlationId = null)
    {
        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid()
        };
        leaveRequest.Update(employeeId, leaveTypeId, startDate, endDate);
        leaveRequest._auditEntries.Add(AuditEntry.Create(leaveRequest.Id, employeeId, "Submit", null, LeaveRequestStatus.Pending, correlationId: correlationId));
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
        var problem = PolicyProblems(LeaveTypeId, StartDate, EndDate, remainingBalance, approvedRequests, policy).FirstOrDefault();
        if (problem != null) throw new DomainException(problem.Message);
    }

    /// <summary>Shares inclusive calendar units across persisted requests and read-only previews.</summary>
    public static int CalendarDays(DateTime start, DateTime end) => (end.Date - start.Date).Days + 1;

    public int GetRequestedDays() => CalendarDays(StartDate, EndDate);

    /// <summary>Evaluates current approved-history rules without creating a request or a submission audit. Pending history is deliberately not an input.</summary>
    public static IReadOnlyList<LeavePolicyProblem> PolicyProblems(Guid typeId, DateTime start, DateTime end,
        int remainingBalance, IEnumerable<LeaveRequest> approvedRequests, LeavePolicy policy)
    {
        var problems = new List<LeavePolicyProblem>();
        if (CalendarDays(start, end) > remainingBalance)
            problems.Add(new("InsufficientBalance", "Requested leave exceeds available balance."));
        if (!policy.AllowOverlap && approvedRequests.Any(ar => ar.LeaveTypeId == typeId && start.Date <= ar.EndDate && ar.StartDate <= end.Date))
            problems.Add(new("ApprovedOverlap", "Requested leave overlaps with another approved request of the same type."));
        return problems;
    }

    /// <summary>Appends the manager decision to the same aggregate for atomic status/audit persistence.</summary>
    public void Approve(Guid actorId, string? correlationId = null, string? decisionNote = null)
    {
        if (Status != LeaveRequestStatus.Pending)
        {
            throw new DomainException("Only pending requests can be approved.");
        }

        var oldStatus = Status;
        Status = LeaveRequestStatus.Approved;
        ProcessedById = actorId;
        ProcessedOn = DateTime.UtcNow;

        _auditEntries.Add(AuditEntry.Create(Id, actorId, "Approve", oldStatus, Status, correlationId: correlationId, decisionNote: decisionNote));
    }

    /// <summary>Retains the rejecting actor and diagnostic context without rewriting prior events.</summary>
    public void Reject(Guid actorId, string? correlationId = null, string? decisionNote = null)
    {
        if (Status != LeaveRequestStatus.Pending)
        {
            throw new DomainException("Only pending requests can be rejected.");
        }

        var oldStatus = Status;
        Status = LeaveRequestStatus.Rejected;
        ProcessedById = actorId;
        ProcessedOn = DateTime.UtcNow;

        _auditEntries.Add(AuditEntry.Create(Id, actorId, "Reject", oldStatus, Status, correlationId: correlationId, decisionNote: decisionNote));
    }

    /// <summary>
    /// Withdraws pending leave only. HR deactivation supplies its reason and acting HR employee;
    /// ordinary owner cancellation leaves the optional reason null.
    /// </summary>
    public void Cancel(Guid actorId, string? reason = null, string? correlationId = null)
    {
        if (Status != LeaveRequestStatus.Pending)
        {
            throw new DomainException("Only pending leave requests can be cancelled.");
        }

        var oldStatus = Status;
        Status = LeaveRequestStatus.Cancelled;
        ProcessedById = actorId;
        ProcessedOn = DateTime.UtcNow;

        _auditEntries.Add(AuditEntry.Create(Id, actorId, "Cancel", oldStatus, Status, reason, correlationId));
    }

}

/// <summary>Stable, owner-safe findings shared by policy enforcement and non-binding preview.</summary>
public sealed record LeavePolicyProblem(string Code, string Message);
