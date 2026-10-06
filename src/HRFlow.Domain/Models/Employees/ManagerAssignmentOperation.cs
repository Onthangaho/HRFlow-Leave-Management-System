namespace HRFlow.Domain.Models.Employees;

/// <summary>Makes a profile edit preserve reporting by default; clearing requires an intentional operation.</summary>
public enum ManagerAssignmentOperation
{
    Preserve,
    Assign,
    Clear
}
