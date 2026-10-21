using HRFlow.Application.Features.LeaveRequests.Queries.GetHrWorkspace;
using HRFlow.Application.Features.LeaveRequests.Queries.GetPersonalDashboard;
using HRFlow.Application.Interfaces;
using HRFlow.Domain.Models.Auth;
using HRFlow.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Infrastructure.Services.Employees;

/// <summary>Two aggregate queries replace workforce materialization and per-profile activation lookups in the caller's snapshot.</summary>
public sealed class HrWorkforceAttentionReader(HRFlowDbContext context) : IHrWorkforceAttentionReader
{
    private sealed class WorkforceRow
    {
        public Guid EmployeeId { get; set; }
        public bool IsActive { get; set; }
        public bool UnknownFacts { get; set; }
        public DateOnly? EmploymentStartDate { get; set; }
        public bool? RequiresActivation { get; set; }
        public bool HasActiveCanonicalProfile { get; set; }
        public string? InvitationDeliveryState { get; set; }
        public DateTime? ActivationExpiresAtUtc { get; set; }
    }

    /// <inheritdoc />
    public async Task<HrWorkforceAttention> ReadAsync(DateTimeOffset asOfUtc, CancellationToken token)
    {
        var today = GetPersonalDashboardQueryHandler.BusinessDate(asOfUtc);
        var now = asOfUtc.UtcDateTime;
        // The former activation reader used Guid.TryParse, including noncanonical legacy links.
        // Normalize only that join operand; the indexed Identity key keeps its native SQLite TEXT format.
        ((SqliteConnection)context.Database.GetDbConnection()).CreateFunction<string?, string?>(
            "hrflow_identity_key", value => Guid.TryParse(value, out var id) ? id.ToString("D").ToUpperInvariant() : null,
            isDeterministic: true);
        var source = context.Database.SqlQueryRaw<WorkforceRow>("""
            SELECT e.Id AS EmployeeId, e.IsActive, e.EmploymentStartDate,
                (e.EmployeeNumber IS NULL OR e.EmploymentStartDate IS NULL) AS UnknownFacts,
                u.RequiresActivation, u.InvitationDeliveryState, u.ActivationExpiresAtUtc,
                (active.IdentityUserId IS NOT NULL) AS HasActiveCanonicalProfile
            FROM Employees AS e
            LEFT JOIN AspNetUsers AS u ON u.Id = hrflow_identity_key(e.IdentityUserId)
            LEFT JOIN (SELECT DISTINCT IdentityUserId FROM Employees WHERE IsActive = 1) AS active
                ON active.IdentityUserId = lower(u.Id)
            """);
        var flags = source.Select(e => new {
            e.IsActive,
            Facts = e.IsActive && e.UnknownFacts,
            Missing = e.IsActive && e.EmploymentStartDate != null && e.EmploymentStartDate <= today
                && !context.WeeklyScheduleRevisions.Any(s => s.EmployeeId == e.EmployeeId && s.EffectiveFrom <= today),
            Future = context.WeeklyScheduleRevisions.Any(s => s.EmployeeId == e.EmployeeId && s.EffectiveFrom > today),
            Pending = e.IsActive && e.RequiresActivation == true,
            // Match GetStatusAsync: null expiry preserves its safe persisted state; inactive/unlinked
            // canonical accounts and expired invitations do not count as current delivery outcomes.
            Failed = e.IsActive && e.RequiresActivation == true && e.HasActiveCanonicalProfile
                && (e.ActivationExpiresAtUtc == null || e.ActivationExpiresAtUtc > now)
                && (e.InvitationDeliveryState == ActivationDeliveryStates.DeliveryFailed || e.InvitationDeliveryState == ActivationDeliveryStates.DeliveryUnknown),
            Prepared = e.IsActive && e.RequiresActivation == true && e.HasActiveCanonicalProfile
                && (e.ActivationExpiresAtUtc == null || e.ActivationExpiresAtUtc > now)
                && e.InvitationDeliveryState == ActivationDeliveryStates.PendingDelivery
        });
        var counts = await flags.GroupBy(_ => 1).Select(g => new {
            Active = g.Count(e => e.IsActive), Inactive = g.Count(e => !e.IsActive),
            Facts = g.Count(e => e.Facts), Missing = g.Count(e => e.Missing),
            Future = g.Count(e => e.Missing && e.Future), Pending = g.Count(e => e.Pending),
            Failed = g.Count(e => e.Failed), Prepared = g.Count(e => e.Prepared)
        }).SingleOrDefaultAsync(token);
        var departments = await context.Departments.AsNoTracking().OrderBy(d => d.Name).ThenBy(d => d.Id)
            .Select(d => new HrDepartmentDto(d.Id, d.Name,
                context.Employees.Count(e => e.DepartmentId == d.Id && e.IsActive),
                context.Employees.Count(e => e.DepartmentId == d.Id && !e.IsActive))).ToArrayAsync(token);
        return new(counts?.Active ?? 0, counts?.Inactive ?? 0, counts?.Facts ?? 0, counts?.Missing ?? 0,
            counts?.Future ?? 0, counts?.Pending ?? 0, counts?.Failed ?? 0, counts?.Prepared ?? 0, departments);
    }
}
