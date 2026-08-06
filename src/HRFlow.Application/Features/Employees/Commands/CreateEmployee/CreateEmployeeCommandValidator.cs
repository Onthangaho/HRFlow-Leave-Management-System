using FluentValidation;
using HRFlow.Application.Interfaces.Employees;
using HRFlow.Domain.Entities;

namespace HRFlow.Application.Features.Employees.Commands.CreateEmployee;

/// <summary>
/// Validates employee creation input before identity and domain persistence side effects start.
/// </summary>
public sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    /// <summary>
    /// Configures create-command validation rules, including department and role checks against live stores.
    /// </summary>
    public CreateEmployeeCommandValidator(IEmployeeManagementService employeeManagementService)
    {
        RuleFor(command => command.FullName)
            .NotEmpty()
            .MaximumLength(Employee.MaxFullNameLength);

        RuleFor(command => command.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(Employee.MaxEmailLength)
            .MustAsync(async (command, email, cancellationToken) =>
                await employeeManagementService.IsEmailAvailableAsync(email, null, cancellationToken))
            .WithMessage("Email is already assigned to another employee account.");

        RuleFor(command => command.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one non-alphanumeric character.")
            .Must(password => password.Distinct().Count() >= 1).WithMessage("Password must contain at least 1 unique character.");

        RuleFor(command => command.DepartmentId)
            .NotEmpty()
            .MustAsync(async (departmentId, cancellationToken) =>
                await employeeManagementService.DepartmentExistsAsync(departmentId, cancellationToken))
            .WithMessage("Department does not exist.");

        RuleFor(command => command.RoleName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (roleName, cancellationToken) =>
                await employeeManagementService.RoleExistsAsync(roleName, cancellationToken))
            .WithMessage("Role does not exist in ASP.NET Identity.");

        RuleFor(command => command.ManagerId)
            .Must(managerId => !managerId.HasValue || managerId.Value != Guid.Empty)
            .WithMessage("ManagerId must be a non-empty GUID when provided.");
    }
}
