using System.Globalization;
using HRFlow.Application.Features.LeaveRequests.Queries.GetDepartmentLeaveReport;
using HRFlow.Application.Features.LeaveRequests.Queries.GetTeamLeaveSummary;
using HRFlow.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HRFlow.Application.Features.LeaveRequests.Queries.ExportLeaveReports;

/// <summary>Actor is supplied by authentication; the existing queries validate ranges and authorize fresh snapshots.</summary>
public sealed record ExportDepartmentLeaveQuery(Guid ActorIdentityId, DateOnly Start, DateOnly End, Guid? DepartmentId) : IRequest<ReportCsvDto>;
/// <summary>No client-selected manager or employee can widen this export's scope.</summary>
public sealed record ExportTeamLeaveQuery(Guid ActorIdentityId, DateOnly Start, DateOnly End) : IRequest<ReportCsvDto>;
/// <summary>Contains a bounded, complete file; oversized results fail before response headers or partial bytes are sent.</summary>
public sealed record ReportCsvDto(byte[] Content, string FileName);

/// <summary>Reuses the report handler's single deferred authorization/data snapshot rather than duplicating calculations.</summary>
public sealed class ExportDepartmentLeaveQueryHandler(IMediator mediator, IReportCsvWriter writer, TimeProvider clock,
    ILogger<ExportDepartmentLeaveQueryHandler> logger) : IRequestHandler<ExportDepartmentLeaveQuery, ReportCsvDto>
{
    /// <inheritdoc />
    public async Task<ReportCsvDto> Handle(ExportDepartmentLeaveQuery request, CancellationToken token)
    {
        var generatedAt = clock.GetUtcNow();
        var report = await mediator.Send(new GetDepartmentLeaveReportQuery(request.ActorIdentityId, request.Start, request.End, request.DepartmentId), token);
        var bytes = writer.Departments(report, generatedAt, token);
        logger.LogInformation("Department export completed. ActorId: {ActorId}; Start: {Start}; End: {End}; DepartmentId: {DepartmentId}; Rows: {Rows}; Bytes: {Bytes}",
            request.ActorIdentityId, request.Start, request.End, request.DepartmentId, report.Departments.Count, bytes.Length);
        return new(bytes, $"hr-department-leave-{request.Start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}-{request.End.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.csv");
    }
}

/// <summary>Reuses the current same-department direct-report projection and its credential/role snapshot.</summary>
public sealed class ExportTeamLeaveQueryHandler(IMediator mediator, IReportCsvWriter writer, TimeProvider clock,
    ILogger<ExportTeamLeaveQueryHandler> logger) : IRequestHandler<ExportTeamLeaveQuery, ReportCsvDto>
{
    /// <inheritdoc />
    public async Task<ReportCsvDto> Handle(ExportTeamLeaveQuery request, CancellationToken token)
    {
        var generatedAt = clock.GetUtcNow();
        var entries = await mediator.Send(new GetTeamLeaveSummaryQuery(request.ActorIdentityId, request.Start, request.End), token);
        var bytes = writer.Team(entries, request.Start, request.End, generatedAt, token);
        logger.LogInformation("Team export completed. ActorId: {ActorId}; Start: {Start}; End: {End}; Rows: {Rows}; Bytes: {Bytes}",
            request.ActorIdentityId, request.Start, request.End, entries.Count, bytes.Length);
        return new(bytes, $"manager-team-leave-{request.Start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}-{request.End.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.csv");
    }
}
