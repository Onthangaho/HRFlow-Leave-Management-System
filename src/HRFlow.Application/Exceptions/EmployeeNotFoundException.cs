
namespace HRFlow.Application.Exceptions;

public class EmployeeNotFoundException : Exception
{
    public EmployeeNotFoundException(Guid employeeId)
        : base($"No employee found with ID {employeeId}")
    {
    }
}