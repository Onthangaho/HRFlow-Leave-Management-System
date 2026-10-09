using HRFlow.Application.Interfaces;
using HRFlow.Application.Services;
using HRFlow.Domain.Models.Employees;
using HRFlow.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveTypes.Queries.GetLeaveTypes;

/// <summary>Provides the authenticated submission selector without management or EF navigation data.</summary>
public class GetLeaveTypesQuery : IRequest<IReadOnlyList<LeaveTypeSelectionDto>>
{
    /// <summary>Comes from authentication, never client JSON.</summary>
    public Guid ActorIdentityId { get; set; }
}

/// <summary>Projects company requirements and original type/policy versions for explicit submission review.</summary>
public sealed record LeaveTypeSelectionDto(Guid Id, string Name, Guid Version, Guid PolicyId, Guid PolicyVersion, string DescriptionMode, string EvidenceMode, string EvidenceClass, string? RequirementInstructions);

/// <summary>Loads current selector values without leaking persistence entities.</summary>
public class GetLeaveTypesQueryHandler : IRequestHandler<GetLeaveTypesQuery, IReadOnlyList<LeaveTypeSelectionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILeaveReportingReadTransaction _readTransaction;
    private readonly CurrentAccountAuthorization _authorization;

    /// <summary>Uses the scoped read context to project current persisted categories.</summary>
    public GetLeaveTypesQueryHandler(IApplicationDbContext context, ILeaveReportingReadTransaction readTransaction, CurrentAccountAuthorization authorization)
    {
        _context = context;
        _readTransaction = readTransaction;
        _authorization = authorization;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LeaveTypeSelectionDto>> Handle(GetLeaveTypesQuery request, CancellationToken cancellationToken)
    {
        return await _readTransaction.ExecuteAsync<IReadOnlyList<LeaveTypeSelectionDto>>(async token =>
        {
            cancellationToken = token;
            await _authorization.RequireIdentityAsync(request.ActorIdentityId, EmployeeRoles.All, cancellationToken);

            return await _context.LeaveTypes.AsNoTracking().OrderBy(type => type.Name)
                .Select(type => new LeaveTypeSelectionDto(type.Id, type.Name, type.Version, type.LeavePolicyId, type.LeavePolicy.Version, type.DescriptionMode, type.EvidenceMode, type.EvidenceClass, type.RequirementInstructions)).ToListAsync(cancellationToken);
        }, cancellationToken);
    }
}
