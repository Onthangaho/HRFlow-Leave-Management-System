using HRFlow.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.Employees.Queries.GetEmployees;

/// <summary>
/// Query to retrieve all employees.
/// </summary>
public class GetEmployeesQuery : IRequest<IEnumerable<GetEmployeeDto>>
{
}

/// <summary>
/// Handles employee retrieval by projecting domain entities to DTOs.
/// </summary>
public class GetEmployeesQueryHandler : IRequestHandler<GetEmployeesQuery, IEnumerable<GetEmployeeDto>>
{
    private readonly IApplicationDbContext _context;

    public GetEmployeesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<GetEmployeeDto>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Employees
            .Select(e => new GetEmployeeDto
            {
                Id = e.Id,
                FullName = e.FullName,
                Email = e.Email,
                DepartmentId = e.DepartmentId,
                ManagerId = e.ManagerId
            })
            .ToListAsync(cancellationToken);
    }
}