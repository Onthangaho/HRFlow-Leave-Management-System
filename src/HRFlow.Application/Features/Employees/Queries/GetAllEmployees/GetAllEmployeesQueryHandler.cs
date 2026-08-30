using HRFlow.Application.DTOs.Employee;
using HRFlow.Domain.Interfaces;
using HRFlow.Domain.Interfaces.Services;
using HRFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.Employees.Queries.GetAllEmployees;

public class GetAllEmployeesQueryHandler : IRequestHandler<GetAllEmployeesQuery, IEnumerable<EmployeeSummaryDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IEmployeeRoleLookupService _roleLookupService;

    public GetAllEmployeesQueryHandler(IApplicationDbContext dbContext, IEmployeeRoleLookupService roleLookupService)
    {
        _dbContext = dbContext;
        _roleLookupService = roleLookupService;
    }

    public async Task<IEnumerable<EmployeeSummaryDto>> Handle(GetAllEmployeesQuery request, CancellationToken cancellationToken)
    {
        var employees = await _dbContext.Employees
            .Include(e => e.Department)
            .ToListAsync(cancellationToken);

        var employeeDtos = new List<EmployeeSummaryDto>();
        foreach (var employee in employees)
        {
            var roleName = string.Empty;
            if (!string.IsNullOrEmpty(employee.IdentityUserId))
            {
                roleName = await _roleLookupService.GetRoleNameByIdentityUserIdAsync(employee.IdentityUserId, cancellationToken);
            }
            
            employeeDtos.Add(new EmployeeSummaryDto
            {
                Id = employee.Id,
                FullName = employee.FullName,
                Email = employee.Email,
                DepartmentName = employee.Department.Name,
                RoleName = roleName
            });
        }

        return employeeDtos;
    }
}