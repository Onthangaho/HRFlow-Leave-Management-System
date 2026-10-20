using HRFlow.Application.Interfaces;
using HRFlow.Application.Services;
using HRFlow.Domain.Models.Employees;
using MediatR;
namespace HRFlow.Application.Features.Employees.Queries.GetEmployees;
/// <summary>Server-derived actor and applied filters; there is no unbounded fallback.</summary>
public sealed record GetEmployeesQuery(Guid ActorIdentityId, EmployeeDirectoryFilter Filter) : IRequest<EmployeeDirectoryPage>;
/// <summary>Reloads a complete edit snapshot independently of list filters.</summary>
public sealed record GetEmployeeQuery(Guid ActorIdentityId, Guid EmployeeId) : IRequest<GetEmployeeDto?>;
/// <summary>Searches eligible Managers and returns the assigned reporting snapshot separately.</summary>
public sealed record GetManagersQuery(Guid ActorIdentityId, Guid DepartmentId, Guid? EmployeeId, string? Search, int Page = 1) : IRequest<ManagerChoices>;
/// <summary>Authorization and projections share a deferred snapshot, without reserving a writer.</summary>
public sealed class GetEmployeesQueryHandler(IEmployeeDirectoryReader reader, ILeaveReportingReadTransaction transaction,
    CurrentAccountAuthorization authorization) : IRequestHandler<GetEmployeesQuery, EmployeeDirectoryPage>,
    IRequestHandler<GetEmployeeQuery, GetEmployeeDto?>, IRequestHandler<GetManagersQuery, ManagerChoices>
{
    private Task<T> Read<T>(Guid actor, Func<CancellationToken, Task<T>> read, CancellationToken token) =>
        transaction.ExecuteAsync(async ct => { await authorization.RequireIdentityAsync(actor, [EmployeeRoles.HrAdministrator], ct); return await read(ct); }, token);
    /// <inheritdoc />
    public Task<EmployeeDirectoryPage> Handle(GetEmployeesQuery query, CancellationToken token) => Read(query.ActorIdentityId, ct => reader.ReadAsync(query.Filter, ct), token);
    /// <inheritdoc />
    public Task<GetEmployeeDto?> Handle(GetEmployeeQuery query, CancellationToken token) => Read(query.ActorIdentityId, ct => reader.DetailAsync(query.EmployeeId, ct), token);
    /// <inheritdoc />
    public Task<ManagerChoices> Handle(GetManagersQuery query, CancellationToken token) => Read(query.ActorIdentityId, ct => reader.ManagersAsync(query.DepartmentId, query.EmployeeId, query.Search, query.Page, ct), token);
}
