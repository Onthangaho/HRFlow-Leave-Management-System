using FluentValidation;
using HRFlow.Domain.Entities;

namespace HRFlow.Application.Features.LeaveRequests.Commands.RejectLeaveRequest;

/// <summary>Rejects oversized manager context before running a protected decision; blank input means no note.</summary>
public sealed class RejectLeaveRequestCommandValidator : AbstractValidator<RejectLeaveRequestCommand>
{
    /// <summary>Uses the domain bound so API and audit validation cannot drift.</summary>
    public RejectLeaveRequestCommandValidator()
    {
        RuleFor(request => request.DecisionNote)
            .Must(note => note is null || note.Trim().Length <= AuditEntry.MaxDecisionNoteLength)
            .WithMessage($"Decision note must contain no more than {AuditEntry.MaxDecisionNoteLength} trimmed characters.");
    }
}
