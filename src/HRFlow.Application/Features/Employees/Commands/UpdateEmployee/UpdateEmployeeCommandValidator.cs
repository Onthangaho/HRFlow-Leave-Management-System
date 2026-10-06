using FluentValidation;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Models.Employees;

namespace HRFlow.Application.Features.Employees.Commands.UpdateEmployee;

/// <summary>Validates the request shape only; relationship and Identity checks run under the writer reservation.</summary>
public sealed class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    /// <summary>Rejects ambiguous manager changes and incomplete role replacements before side effects.</summary>
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(command => command.EmployeeId).NotEmpty();
        RuleFor(command => command.ExpectedVersion).NotEmpty();
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(Employee.MaxFullNameLength);
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(Employee.MaxEmailLength);
        RuleFor(command => command.DepartmentId).NotEmpty();
        RuleFor(command => command.Roles).NotEmpty();
        RuleForEach(command => command.Roles)
            .Must(role => role is not null && EmployeeRoles.All.Contains(role.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("Select only Employee, Manager, or HR Administrator roles.");
        RuleFor(command => command.ManagerAssignment).IsInEnum();
        RuleFor(command => command).Must(command =>
            command.ManagerAssignment == ManagerAssignmentOperation.Assign
                ? command.ManagerId.HasValue && command.ManagerId != Guid.Empty
                : !command.ManagerId.HasValue)
            .WithMessage("Assign requires a manager ID; Preserve and Clear must not include one.");
    }
}
