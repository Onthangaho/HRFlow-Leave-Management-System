using FluentValidation;
using HRFlow.Application.Interfaces;
using HRFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveRequests.Commands.SubmitLeaveRequest;

public class SubmitLeaveRequestCommandValidator : AbstractValidator<SubmitLeaveRequestCommand>
{
    private readonly IApplicationDbContext _context;

    public SubmitLeaveRequestCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(v => v.LeaveTypeId)
            .NotEmpty()
            .MustAsync(LeaveTypeExists)
            .WithMessage("Leave type does not exist.");

        RuleFor(v => v.EmployeeId)
            .NotEmpty()
            .MustAsync(EmployeeExists)
            .WithMessage("Employee does not exist.");

        RuleFor(v => v.StartDate)
            .NotEmpty()
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .WithMessage("Start date must be in the future.");

        RuleFor(v => v.EndDate)
            .NotEmpty()
            .GreaterThanOrEqualTo(v => v.StartDate)
            .WithMessage("End date must be on or after the start date.");
    }

    private async Task<bool> LeaveTypeExists(Guid leaveTypeId, CancellationToken cancellationToken)
    {
        return await _context.LeaveTypes.AnyAsync(lt => lt.Id == leaveTypeId, cancellationToken);
    }

    private async Task<bool> EmployeeExists(Guid employeeId, CancellationToken cancellationToken)
    {
        return await _context.Employees.AnyAsync(e => e.Id == employeeId, cancellationToken);
    }
}