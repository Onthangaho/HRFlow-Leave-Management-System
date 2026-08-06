using FluentValidation;
using HRFlow.Application.Interfaces.Employees;
using HRFlow.Domain.Entities;

namespace HRFlow.Application.Features.Employees.Commands.UpdateEmployee;

/// <summary>
/// Validates employee update input before transactional identity/domain writes begin.
/// </summary>
public sealed class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    /// <summary>
    /// Configures update-command validation rules, including department and role checks against live stores.
    /// </summary>
    public UpdateEmployeeCommandValidator(IEmployeeManagementService employeeManagementService)
    {
        RuleFor(command => command.EmployeeId)
            .NotEmpty()
            .MustAsync(async (employeeId, cancellationToken) =>
                await employeeManagementService.EmployeeExistsAsync(employeeId, cancellationToken))
            .WithMessage("Employee does not exist.");

        RuleFor(command => command.FullName)
            .NotEmpty()
            .MaximumLength(Employee.MaxFullNameLength);

        RuleFor(command => command.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(Employee.MaxEmailLength)
            .MustAsync(async (command, email, cancellationToken) =>
                await employeeManagementService.IsEmailAvailableAsync(email, command.EmployeeId, cancellationToken))
            .WithMessage("Email is already assigned to another employee account.");

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
