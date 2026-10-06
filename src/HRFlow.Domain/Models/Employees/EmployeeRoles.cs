namespace HRFlow.Domain.Models.Employees;

/// <summary>Limits employee management to the capabilities supported by this single-organization application.</summary>
public static class EmployeeRoles
{
    public const string Employee = "Employee";
    public const string Manager = "Manager";
    public const string HrAdministrator = "HR Administrator";
    public static IReadOnlyList<string> All { get; } = Array.AsReadOnly(new[] { Employee, Manager, HrAdministrator });
}
