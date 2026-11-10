using FluentValidation;

namespace HRFlow.Application.Features.LeaveRequests.Commands.SubmitLeaveRequest;

/// <summary>
/// Validates request shape; authoritative existence and policy reads happen inside writer protection.
/// </summary>
public class SubmitLeaveRequestCommandValidator : AbstractValidator<SubmitLeaveRequestCommand>
{
    /// <summary>Checks identifiers and dates without reading an unprotected policy/type snapshot.</summary>
    public SubmitLeaveRequestCommandValidator()
    {
        RuleFor(v => v.SubmissionKey).Must(key => key == null || key != Guid.Empty).WithMessage("Use a nonempty submission key.");
        RuleFor(v => v.ExpectedTypeVersion).NotEmpty();
        RuleFor(v => v.ExpectedPolicyVersion).NotEmpty();
        RuleFor(v => v.Description).Must(text => text == null || text.Trim().Length <= 1000).WithMessage("Description must contain at most 1000 trimmed characters.");
        RuleFor(v => v.DocumentIds).NotNull().Must(ids => ids != null && ids.Count <= HRFlow.Domain.Entities.SupportingDocumentLimits.MaxRequestDocuments && ids.Distinct().Count() == ids.Count).WithMessage("Choose at most five distinct clean documents.");
        RuleFor(v => v.LeaveTypeId)
            .NotEmpty();

        RuleFor(v => v.EmployeeId)
            .NotEmpty();

        // Keyed recovery must precede elapsed-date checks. Legacy callers keep their original early validation response.
        RuleFor(v => v.StartDate).NotEmpty().When(v => v.SubmissionKey != null);
        RuleFor(v => v.EndDate).NotEmpty().When(v => v.SubmissionKey != null);
        RuleFor(v => v).Custom((request, validation) =>
        {
            foreach (var problem in LeaveSubmissionDateRules.Problems(request.StartDate, request.EndDate, DateTime.UtcNow.Date))
                validation.AddFailure(problem.Code, problem.Message);
        }).When(v => v.SubmissionKey == null);
    }

}
