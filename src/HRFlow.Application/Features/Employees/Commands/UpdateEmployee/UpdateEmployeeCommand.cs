using System.Text.Json.Serialization;
using HRFlow.Domain.Models.Employees;
using MediatR;

namespace HRFlow.Application.Features.Employees.Commands.UpdateEmployee;

/// <summary>Uses an explicit role replacement and manager operation to avoid accidental privilege or reporting loss.</summary>
public sealed class UpdateEmployeeCommand : IRequest<EmployeeManagementResult>
{
    /// <summary>The API supplies the authenticated actor, ignoring any JSON value.</summary>
    [JsonIgnore]
    public Guid ActorIdentityUserId { get; set; }
    /// <summary>Optional in JSON; the route is authoritative and a differing body ID is rejected.</summary>
    public Guid? EmployeeId { get; set; }
    /// <summary>The version loaded with the edit form; never silently merges a stale replacement.</summary>
    public Guid ExpectedVersion { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    /// <summary>Replaces all current roles. A profile-only edit must send the complete loaded collection.</summary>
    public List<string> Roles { get; set; } = [];
    /// <summary>Preserve is the omission default; Assign requires a manager ID and Clear forbids one.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter<ManagerAssignmentOperation>))]
    public ManagerAssignmentOperation ManagerAssignment { get; set; } = ManagerAssignmentOperation.Preserve;
    public Guid? ManagerId { get; set; }
}
