using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using HRFlow.Application.Features.LeaveRequests.Queries.GetDepartmentLeaveReport;
using HRFlow.Application.Features.LeaveRequests.Queries.GetTeamLeaveSummary;
using HRFlow.Application.Interfaces;
using HRFlow.Domain.Common;

namespace HRFlow.Infrastructure.Services;

/// <summary>Produces UTF-8 BOM/CRLF CSV with a visible text marker on every untrusted name, not quoting alone.</summary>
public sealed class ReportCsvWriter : IReportCsvWriter
{
    /// <summary>Metadata/header rows do not consume the data-row allowance.</summary>
    public const int MaximumDataRows = 5000;
    /// <summary>Includes UTF-8 BOM, metadata and all quoted record bytes.</summary>
    public const int MaximumBytes = 2 * 1024 * 1024;
    private const string TextMarker = "Text: ";

    /// <inheritdoc />
    public byte[] Departments(DepartmentLeaveReportDto report, DateTimeOffset generatedAt, CancellationToken token)
    {
        LimitRows(report.Departments.Count);
        return Write(csv =>
        {
            Row(csv, "RecordKind", "DepartmentId", "Department", "PendingRequests", "ApprovedRequests", "DistinctEmployeesWithApprovedLeave",
                "SummedClippedApprovedCalendarRequestDays", "InactiveApprovedRequests", "DistinctInactiveEmployeesWithApprovedLeave",
                "InactiveSummedClippedApprovedCalendarRequestDays", "PeriodStart", "PeriodEnd", "ExportGenerationStartedUtc", "Attribution", "Semantics");
            void Metrics(string kind, string id, string name, LeaveReportMetricsDto metrics) => Row(csv, kind, id, name,
                metrics.PendingRequests, metrics.ApprovedRequests, metrics.EmployeesWithApprovedLeave, metrics.ApprovedRequestDays,
                metrics.InactiveApprovedRequests, metrics.InactiveEmployeesWithApprovedLeave, metrics.InactiveApprovedRequestDays,
                report.Start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), report.End.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), generatedAt.ToString("O", CultureInfo.InvariantCulture),
                "Current employee department; inactive history included",
                "Inclusive period; Pending/Approved intersecting requests; Approved days clipped; overlaps summed separately, not unique absence; inactive columns are subsets of totals; fresh export may differ from screen");
            Metrics("Total", "", "Selected-period total", report.Totals);
            foreach (var department in report.Departments)
            {
                token.ThrowIfCancellationRequested();
                Metrics("Department", department.DepartmentId.ToString(), TextMarker + department.DepartmentName, department.Metrics);
            }
        }, token);
    }

    /// <inheritdoc />
    public byte[] Team(IReadOnlyList<TeamLeaveSummaryDto> entries, DateOnly start, DateOnly end, DateTimeOffset generatedAt, CancellationToken token)
    {
        LimitRows(entries.Count);
        return Write(csv =>
        {
            Row(csv, "RecordKind", "RequestId", "EmployeeId", "EmployeeName", "EmploymentStatus", "LeaveTypeId", "LeaveType",
                "RequestStart", "RequestEnd", "RequestStatus", "PeriodStart", "PeriodEnd", "ExportGenerationStartedUtc", "Attribution", "Semantics");
            void Entry(string kind, params object[] fields) => Row(csv, new object[] { kind }.Concat(fields).Concat(new object[] {
                start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), generatedAt.ToString("O", CultureInfo.InvariantCulture),
                "Current same-department direct reports; excludes self; inactive history included",
                "Approved requests intersecting inclusive period; full request spans, not clipped or unique absence; inactive employees excluded from active coverage; fresh export may differ from screen" }).ToArray());
            // The metadata record preserves scope/time even for a genuinely empty authorized team.
            Entry("Metadata", "", "", "", "", "", "", "", "", "");
            foreach (var entry in entries)
            {
                token.ThrowIfCancellationRequested();
                Entry("ApprovedRequest", entry.RequestId, entry.EmployeeId, TextMarker + entry.EmployeeName,
                    entry.IsActive ? "Active" : "Inactive - historical leave", entry.LeaveTypeId, TextMarker + entry.LeaveTypeName,
                    entry.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), entry.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), "Approved");
            }
        }, token);
    }

    private static void LimitRows(int count)
    {
        if (count > MaximumDataRows) throw new DomainException("Export exceeds 5,000 data rows. Select a narrower period or department; no partial file was created.");
    }

    private static void Row(CsvWriter csv, params object[] values)
    {
        foreach (var value in values) csv.WriteField(value);
        csv.NextRecord();
    }

    private static byte[] Write(Action<CsvWriter> write, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var stream = new BoundedStream();
        using (var text = new StreamWriter(stream, new UTF8Encoding(true), 1024, leaveOpen: true))
        using (var csv = new CsvWriter(text, new CsvConfiguration(CultureInfo.InvariantCulture) { NewLine = "\r\n", ShouldQuote = _ => true }))
            write(csv);
        token.ThrowIfCancellationRequested();
        return stream.ToArray();
    }

    private sealed class BoundedStream : MemoryStream
    {
        public override void Write(byte[] buffer, int offset, int count)
        {
            Check(count);
            base.Write(buffer, offset, count);
        }
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            Check(buffer.Length);
            base.Write(buffer);
        }
        private void Check(int count)
        {
            if (Length + count > MaximumBytes) throw new DomainException("Export exceeds 2 MiB. Select a narrower period or department; no partial file was created.");
        }
    }
}
