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
        RuleFor(v => v.DocumentIds).NotNull().Must(ids => ids != null && ids.Count <= HRFlow.Domain.Entities.SupportingDocumentLimits.MaxRequestDocuments && ids.Distinct().Count() == ids.Count).WithMessage("Choose at most five distinct clean documents.");
        RuleFor(v => v.LeaveTypeId)
            .NotEmpty();

        RuleFor(v => v.EmployeeId)
            .NotEmpty();

        RuleFor(v => v.StartDate)
            .NotEmpty()
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .WithMessage("Start date must be in the future.");

        RuleFor(v => v.EndDate)
            .NotEmpty()
            .GreaterThanOrEqualTo(v => v.StartDate)
            .WithMessage("End date must be on or after the start date.");
    }

}
