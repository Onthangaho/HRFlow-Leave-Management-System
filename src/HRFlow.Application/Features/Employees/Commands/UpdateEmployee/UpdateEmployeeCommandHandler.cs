using HRFlow.Application.Interfaces.Employees;
using HRFlow.Application.Models.Employees;
using MediatR;

namespace HRFlow.Application.Features.Employees.Commands.UpdateEmployee;

/// <summary>
/// Handles employee update commands by orchestrating domain and identity updates through the employee management service.
/// </summary>
public sealed class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, EmployeeManagementResult>
{
    private readonly IEmployeeManagementService _employeeManagementService;

    /// <summary>
    /// Creates a command handler that delegates to the transactional employee management service.
    /// </summary>
    public UpdateEmployeeCommandHandler(IEmployeeManagementService employeeManagementService)
    {
        _employeeManagementService = employeeManagementService;
    }

    /// <inheritdoc />
    public Task<EmployeeManagementResult> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        return _employeeManagementService.UpdateEmployeeAsync(
            request.EmployeeId,
            request.FullName,
            request.Email,
            request.DepartmentId,
            request.RoleName,
            request.ManagerId,
            cancellationToken);
    }
}
