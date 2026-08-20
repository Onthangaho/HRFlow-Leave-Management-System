using HRFlow.Application.Interfaces;
using HRFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveTypes.Queries.GetLeaveTypes;

public class GetLeaveTypesQuery : IRequest<IEnumerable<LeaveType>>
{
}

public class GetLeaveTypesQueryHandler : IRequestHandler<GetLeaveTypesQuery, IEnumerable<LeaveType>>
{
    private readonly IApplicationDbContext _context;

    public GetLeaveTypesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<LeaveType>> Handle(GetLeaveTypesQuery request, CancellationToken cancellationToken)
    {
        return await _context.LeaveTypes.ToListAsync(cancellationToken);
    }
}