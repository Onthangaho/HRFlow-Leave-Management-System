using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces.Employees;
using HRFlow.Application.Models.Employees;
using MediatR;

namespace HRFlow.Application.Features.Employees.Commands.CreateEmployee;

/// <summary>
/// Handles employee creation commands by orchestrating domain and identity writes through the employee management service.
/// </summary>
public sealed class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, EmployeeManagementResult>
{
    private readonly IEmployeeManagementService _employeeManagementService;

    /// <summary>
    /// Creates a command handler that delegates to the transactional employee management service.
    /// </summary>
    public CreateEmployeeCommandHandler(IEmployeeManagementService employeeManagementService)
    {
        _employeeManagementService = employeeManagementService;
    }

    /// <inheritdoc />
    public Task<EmployeeManagementResult> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        return _employeeManagementService.CreateEmployeeAsync(
            request.FullName,
            request.Email,
            request.Password,
            request.DepartmentId,
            request.RoleName,
            request.ManagerId,
            cancellationToken);
    }
}