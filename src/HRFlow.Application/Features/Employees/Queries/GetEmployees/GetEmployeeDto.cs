namespace HRFlow.Application.Features.Employees.Queries.GetEmployees;

public class GetEmployeeDto
{
    public Guid Id { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? ManagerId { get; set; }
}