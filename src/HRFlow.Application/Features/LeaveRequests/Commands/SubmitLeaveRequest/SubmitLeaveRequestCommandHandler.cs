using HRFlow.Application.Services;
using HRFlow.Domain.Models.Employees;
using HRFlow.Domain.Interfaces;
using HRFlow.Domain.Entities;
using HRFlow.Application.Features.LeaveRequests;
using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveRequests.Commands.SubmitLeaveRequest;

/// <summary>
/// Handles leave request submission with leave policy validation against approved requests and available balance.
/// </summary>
public class SubmitLeaveRequestCommandHandler : IRequestHandler<SubmitLeaveRequestCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ILeaveNotificationOutbox _notifications;
    private readonly CurrentAccountAuthorization _authorization;
    private readonly IRequestCorrelationContext _correlation;
    private readonly ILeaveApprovalAuthorizationService _leaveApprovalAuthorizationService;
    private readonly ILeaveConfigurationTransaction _transaction;
    private readonly SupportingDocumentService _documents;
    private readonly ILeaveReportingReadTransaction _reads;

    /// <summary>Shares the context and database reservation used by policy management and approvals.</summary>
    public SubmitLeaveRequestCommandHandler(
        IApplicationDbContext context,
        ILeaveNotificationOutbox notifications,
        CurrentAccountAuthorization authorization,
        IRequestCorrelationContext correlation,
        ILeaveApprovalAuthorizationService leaveApprovalAuthorizationService,
        ILeaveConfigurationTransaction transaction, SupportingDocumentService documents, ILeaveReportingReadTransaction reads)
    {
        _context = context;
        _notifications = notifications;
        _authorization = authorization;
        _correlation = correlation;
        _leaveApprovalAuthorizationService = leaveApprovalAuthorizationService;
        _transaction = transaction;
        _documents = documents;
        _reads = reads;
    }

    /// <summary>Acquires writer protection before authoritative policy and relationship reads.</summary>
    public async Task<Guid> Handle(SubmitLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        Guid id = Guid.Empty;
        var hash = LeaveSubmissionIdentity.Hash(request);
        var previous = request.SubmissionKey == null ? null : await _reads.ExecuteAsync(
            ct => LeaveSubmissionIdentity.FindAsync(_context, _authorization, request, hash, ct), cancellationToken);
        var verified = new Dictionary<Guid, Guid>();
        WriteConflictException? verificationFailure = null;
        if (previous == null)
        {
            try { verified = await _documents.VerifyBindingAsync(request.EmployeeId, request.DocumentIds, cancellationToken); }
            // A concurrent identical submission may bind the drafts after our read. Its committed outcome wins below.
            catch (WriteConflictException failure) { verificationFailure = failure; }
        }
        await _transaction.ExecuteAsync(async token =>
        {
            var recovered = await LeaveSubmissionIdentity.FindAsync(_context, _authorization, request, hash, token);
            if (recovered.HasValue) { id = recovered.Value; return; }
            if (verificationFailure != null) throw verificationFailure;
            var dateProblems = LeaveSubmissionDateRules.Problems(request.StartDate, request.EndDate, DateTime.UtcNow.Date);
            if (dateProblems.Count != 0)
                throw new FluentValidation.ValidationException(dateProblems.Select(problem =>
                    new FluentValidation.Results.ValidationFailure(problem.Code, problem.Message)));
            id = await SubmitAsync(request, verified, token);
            if (request.SubmissionKey is { } key)
                _context.LeaveSubmissionOutcomes.Add(new LeaveSubmissionOutcome { OwnerId = request.EmployeeId, Key = key, RequestId = id, PayloadHash = hash });
        }, cancellationToken);
        return id;
    }

    private async Task<Guid> SubmitAsync(SubmitLeaveRequestCommand request, IReadOnlyDictionary<Guid, Guid> verified, CancellationToken cancellationToken)
    {
        var employee = await _context.Employees
            .SingleOrDefaultAsync(employee => employee.Id == request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee was not found.");

        await _authorization.RequireEmployeeAsync(request.EmployeeId, [EmployeeRoles.Employee, EmployeeRoles.Manager], cancellationToken);
        await _leaveApprovalAuthorizationService.EnsureEmployeeHasValidManagerAsync(employee, cancellationToken);

        var leaveType = await _context.LeaveTypes
            .Include(lt => lt.LeavePolicy)
            .SingleOrDefaultAsync(lt => lt.Id == request.LeaveTypeId, cancellationToken)
            ?? throw new WriteConflictException("This leave type is no longer available. Reload the leave types before submitting.");

        if (leaveType.Version != request.ExpectedTypeVersion || leaveType.LeavePolicy.Version != request.ExpectedPolicyVersion)
            throw new WriteConflictException("Leave requirements or policy changed. Refresh and explicitly review the current requirements; your dates, description and uploaded drafts have not been submitted.");
        if (leaveType.EvidenceMode == RequirementModes.NotRequested && request.DocumentIds.Count != 0)
            throw new HRFlow.Domain.Common.DomainException("This type does not request evidence. Deselect documents explicitly; uploaded drafts remain available.");
        if (leaveType.EvidenceMode == RequirementModes.Required && request.DocumentIds.Count == 0)
            throw new HRFlow.Domain.Common.DomainException("At least one clean owned supporting document is required for this company leave type.");

        var approvedRequests = await _context.LeaveRequests
            .Where(lr => lr.EmployeeId == request.EmployeeId &&
                         lr.LeaveTypeId == request.LeaveTypeId &&
                         lr.Status == Domain.Enums.LeaveRequestStatus.Approved)
            .ToListAsync(cancellationToken);

        var leaveRequest = LeaveRequest.Create(
            request.EmployeeId,
            request.LeaveTypeId,
            request.StartDate,
            request.EndDate, _correlation.CorrelationId);

        leaveRequest.CaptureRequirements(new RequestRequirementsSnapshot(leaveType.Id, leaveType.Version,
            leaveType.LeavePolicyId, leaveType.LeavePolicy.Version, leaveType.DescriptionMode, leaveType.EvidenceMode,
            leaveType.EvidenceClass, leaveType.RequirementInstructions), request.Description);

        var balance = LeaveBalanceCalculator.Calculate(
            leaveType.LeavePolicy.DefaultBalance,
            approvedRequests);

        leaveRequest.ValidateAgainstPolicy(
            balance.RemainingDays,
            approvedRequests,
            leaveType.LeavePolicy);

        _context.LeaveRequests.Add(leaveRequest);
        await _documents.BindAsync(employee.Id, leaveRequest.Id, request.DocumentIds, verified, cancellationToken, leaveType.EvidenceClass);
        await _notifications.TransitionAsync(leaveRequest, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return leaveRequest.Id;
    }
}
