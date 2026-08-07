
namespace HRFlow.Application.Exceptions;

/// <summary>
/// Represents an error that occurs when attempting to access an employee that does not exist.
/// </summary>
public class EmployeeNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmployeeNotFoundException"/> class.
    /// </summary>
    /// <param name="employeeId">The unique identifier of the employee that could not be found.</param>
    public EmployeeNotFoundException(Guid employeeId)
        : base($"No employee found with ID {employeeId}")
    {
    }
}