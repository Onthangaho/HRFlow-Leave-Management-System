namespace HRFlow.Application.Features.Employees.Queries.GetEmployees;

/// <summary>Provides a complete editable snapshot; no Identity credentials or tokens are exposed.</summary>
public sealed class GetEmployeeDto
{
    /// <summary>Inactive records remain visible for history but cannot be edited or assigned as managers.</summary>
    public bool IsActive { get; set; }
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public Guid? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
    /// <summary>Clients must return this version as ExpectedVersion when replacing profile or roles.</summary>
    public Guid Version { get; set; }
}
