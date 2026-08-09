namespace HRFlow.Application.DTOs.Employee;

/// <summary>
/// Represents a summary of an employee for list views.
/// </summary>
public class EmployeeSummaryDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
}