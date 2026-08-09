using HRFlow.Application.DTOs.Employee;
using MediatR;

namespace HRFlow.Application.Features.Employees.Queries.GetAllEmployees;

public class GetAllEmployeesQuery : IRequest<IEnumerable<EmployeeSummaryDto>>
{
}