using HRFlow.Application.Features.LeaveTypes.Models;
using HRFlow.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveTypes.Queries.GetAllLeaveTypes;

public class GetAllLeaveTypesQueryHandler : IRequestHandler<GetAllLeaveTypesQuery, IEnumerable<LeaveTypeModel>>
{
    private readonly IApplicationDbContext _context;

    public GetAllLeaveTypesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<LeaveTypeModel>> Handle(GetAllLeaveTypesQuery request, CancellationToken cancellationToken)
    {
        return await _context.LeaveTypes
            .AsNoTracking()
            .Select(lt => new LeaveTypeModel
            {
                Id = lt.Id,
                Name = lt.Name,
                DefaultBalance = lt.LeavePolicy.DefaultBalance
            })
            .ToListAsync(cancellationToken);
    }
}