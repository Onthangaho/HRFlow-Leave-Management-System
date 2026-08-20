using HRFlow.Application.Features.LeaveTypes.Models;
using MediatR;

namespace HRFlow.Application.Features.LeaveTypes.Queries.GetAllLeaveTypes;

public class GetAllLeaveTypesQuery : IRequest<IEnumerable<LeaveTypeModel>>
{
    
}