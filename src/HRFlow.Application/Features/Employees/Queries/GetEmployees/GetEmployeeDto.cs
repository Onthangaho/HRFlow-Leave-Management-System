namespace HRFlow.Application.Features.Employees.Queries.GetEmployees;

/// <summary>
/// Data transfer object representing an employee for query results.
/// </summary>
public class GetEmployeeDto
{
    public Guid Id { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? ManagerId { get; set; }
}