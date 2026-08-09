using HRFlow.Application.DTOs.Employee;
using HRFlow.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.Employees.Queries.GetAllEmployees;

public class GetAllEmployeesQueryHandler : IRequestHandler<GetAllEmployeesQuery, IEnumerable<EmployeeSummaryDto>>
{
    private readonly IHRFlowDbContext _dbContext;
    private readonly DbContext _identityContext;

    public GetAllEmployeesQueryHandler(IHRFlowDbContext dbContext)
    {
        _dbContext = dbContext;
        _identityContext = (DbContext)dbContext;
    }

    public async Task<IEnumerable<EmployeeSummaryDto>> Handle(GetAllEmployeesQuery request, CancellationToken cancellationToken)
    {
        var employees = await _dbContext.Employees
            .Include(e => e.Department)
            .Select(e => new EmployeeSummaryDto
            {
                Id = e.Id,
                FullName = e.FullName,
                Email = e.Email,
                DepartmentName = e.Department.Name,
                RoleName = (from ur in _identityContext.Set<IdentityUserRole<string>>()
                            join r in _identityContext.Set<IdentityRole>() on ur.RoleId equals r.Id
                            where ur.UserId == e.IdentityUserId
                            select r.Name).FirstOrDefault() ?? string.Empty
            })
            .ToListAsync(cancellationToken);

        return employees;
    }
}