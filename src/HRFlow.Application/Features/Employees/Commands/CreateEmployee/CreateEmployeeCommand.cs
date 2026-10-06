using System.Text.Json.Serialization;
using HRFlow.Domain.Models.Employees;
using MediatR;

namespace HRFlow.Application.Features.Employees.Commands.CreateEmployee;

/// <summary>Creates a managed account; credentials are accepted only on creation and never returned.</summary>
public sealed class CreateEmployeeCommand : IRequest<EmployeeManagementResult>
{
    /// <summary>The API supplies this identity from the authenticated caller, never from JSON.</summary>
    [JsonIgnore]
    public Guid ActorIdentityUserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public Guid? ManagerId { get; set; }
    /// <summary>One or more supported capabilities; duplicate names are normalized by the service.</summary>
    public List<string> Roles { get; set; } = [];
}
