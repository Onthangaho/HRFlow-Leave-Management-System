using System.Text.Json.Serialization;
using FluentValidation;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Interfaces.Services.Employees;
using HRFlow.Domain.Models.Employees;
using MediatR;

namespace HRFlow.Application.Features.Employees.Commands.DeactivateEmployee;

/// <summary>Requires explicit confirmation against a loaded version; actor and target come from the API.</summary>
public sealed class DeactivateEmployeeCommand : IRequest<EmployeeDeactivationResult>
{
    [JsonIgnore] public Guid ActorIdentityUserId { get; set; }
    [JsonIgnore] public Guid EmployeeId { get; set; }
    public Guid ExpectedVersion { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>Delegates lifecycle coordination to the existing account-aware transactional service.</summary>
public sealed class DeactivateEmployeeCommandHandler(IEmployeeManagementService service)
    : IRequestHandler<DeactivateEmployeeCommand, EmployeeDeactivationResult>
{
    /// <inheritdoc />
    public Task<EmployeeDeactivationResult> Handle(DeactivateEmployeeCommand request, CancellationToken cancellationToken) =>
        service.DeactivateEmployeeAsync(request.ActorIdentityUserId, request.EmployeeId, request.ExpectedVersion, request.Reason, cancellationToken);
}

/// <summary>Checks confirmation shape; authoritative state and permissions are read after writer reservation.</summary>
public sealed class DeactivateEmployeeCommandValidator : AbstractValidator<DeactivateEmployeeCommand>
{
    /// <summary>Rejects missing versions and blank or excessive reasons before lifecycle changes.</summary>
    public DeactivateEmployeeCommandValidator()
    {
        RuleFor(r => r.EmployeeId).NotEmpty();
        RuleFor(r => r.ExpectedVersion).NotEmpty();
        RuleFor(r => r.Reason).NotEmpty().MaximumLength(Employee.MaxDeactivationReasonLength);
    }
}
