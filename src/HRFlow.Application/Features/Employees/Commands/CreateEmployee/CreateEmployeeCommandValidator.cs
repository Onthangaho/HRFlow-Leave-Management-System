using FluentValidation;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Models.Employees;

namespace HRFlow.Application.Features.Employees.Commands.CreateEmployee;

/// <summary>Validates the request shape only; relationship and Identity checks run under the writer reservation.</summary>
public sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    /// <summary>Rejects ambiguous manager changes and incomplete role replacements before side effects.</summary>
    public CreateEmployeeCommandValidator()
    {
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(Employee.MaxFullNameLength);
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(Employee.MaxEmailLength);
        RuleFor(command => command.DepartmentId).NotEmpty();
        RuleFor(command => command.Roles).NotEmpty();
        RuleForEach(command => command.Roles)
            .Must(role => role is not null && EmployeeRoles.All.Contains(role.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("Select only Employee, Manager, or HR Administrator roles.");
        RuleFor(command => command.Password).NotEmpty().MinimumLength(8)
            .Matches("[0-9]").WithMessage("Password must contain a digit.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain a symbol.");
        RuleFor(command => command.ManagerId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("ManagerId must be a non-empty GUID when provided.");
    }
}
