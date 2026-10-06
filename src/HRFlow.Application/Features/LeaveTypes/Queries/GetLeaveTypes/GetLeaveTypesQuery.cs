using HRFlow.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveTypes.Queries.GetLeaveTypes;

/// <summary>Provides the authenticated submission selector without management or EF navigation data.</summary>
public class GetLeaveTypesQuery : IRequest<IReadOnlyList<LeaveTypeSelectionDto>>
{
}

/// <summary>Projects only the ID and display name required by the existing employee client.</summary>
public sealed record LeaveTypeSelectionDto(Guid Id, string Name);

/// <summary>Loads current selector values without leaking persistence entities.</summary>
public class GetLeaveTypesQueryHandler : IRequestHandler<GetLeaveTypesQuery, IReadOnlyList<LeaveTypeSelectionDto>>
{
    private readonly IApplicationDbContext _context;

    /// <summary>Uses the scoped read context to project current persisted categories.</summary>
    public GetLeaveTypesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LeaveTypeSelectionDto>> Handle(GetLeaveTypesQuery request, CancellationToken cancellationToken)
    {
        return await _context.LeaveTypes.AsNoTracking().OrderBy(type => type.Name)
            .Select(type => new LeaveTypeSelectionDto(type.Id, type.Name)).ToListAsync(cancellationToken);
    }
}
