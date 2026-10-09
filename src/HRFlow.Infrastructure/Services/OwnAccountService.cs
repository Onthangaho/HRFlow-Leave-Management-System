using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces;
using HRFlow.Application.Services;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Interfaces.Services;
using HRFlow.Domain.Models.Employees;
using HRFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRFlow.Infrastructure.Services;

/// <summary>Own metadata persistence reuses snapshot/writer safeguards without touching employment or Identity.</summary>
public sealed class OwnAccountService(HRFlowDbContext context, CurrentAccountAuthorization authorization,
    IEmployeeRoleLookupService roles, ILeaveReportingReadTransaction reads, IEmployeeManagementTransaction writes,
    IRequestCorrelationContext correlation, ILogger<OwnAccountService> logger) : IOwnAccountService
{
    private static readonly string[] Capabilities = [EmployeeRoles.Employee, EmployeeRoles.Manager, EmployeeRoles.HrAdministrator];

    /// <inheritdoc />
    public Task<OwnProfileDto> GetProfileAsync(Guid actor, CancellationToken token) => reads.ExecuteAsync(async ct =>
    {
        var employee = await authorization.RequireIdentityAsync(actor, Capabilities, ct);
        var profile = await context.Employees.AsNoTracking().Where(e => e.Id == employee.Id)
            .Select(e => new { e.FullName, e.Email, Department = e.Department.Name, Manager = e.Manager == null ? null : e.Manager.FullName })
            .SingleAsync(ct);
        var metadata = await context.AccountSettings.AsNoTracking().SingleOrDefaultAsync(s => s.EmployeeId == employee.Id, ct);
        return new OwnProfileDto(profile.FullName, profile.Email,
            await roles.GetRolesByIdentityUserIdAsync(employee.IdentityUserId, ct), profile.Department, profile.Manager,
            employee.IsActive, true, metadata?.PreferredDisplayName, metadata?.ContactPhone, metadata?.ProfileVersion ?? Guid.Empty);
    }, token);

    /// <inheritdoc />
    public Task<OwnPreferencesDto> GetPreferencesAsync(Guid actor, CancellationToken token) => reads.ExecuteAsync(async ct =>
    {
        var employee = await authorization.RequireIdentityAsync(actor, Capabilities, ct);
        return Preferences(await context.AccountSettings.AsNoTracking().SingleOrDefaultAsync(s => s.EmployeeId == employee.Id, ct));
    }, token);

    /// <inheritdoc />
    public async Task<PrivateProfileDto> SaveProfileAsync(Guid actor, ProfileUpdateDto update, CancellationToken token)
    {
        PrivateProfileDto? result = null;
        await writes.ExecuteAsync(async ct =>
        {
            var employee = await authorization.RequireIdentityAsync(actor, Capabilities, ct);
            var settings = await GetOrCreateAsync(employee.Id, ct);
            if (settings.ProfileVersion != update.ExpectedVersion)
                throw new WriteConflictException("Your profile changed. Reload explicitly before saving again; unsaved values will be discarded.");
            settings.UpdateProfile(update.PreferredDisplayName, update.ContactPhone);
            result = new(settings.PreferredDisplayName, settings.ContactPhone, settings.ProfileVersion);
        }, token);
        logger.LogInformation("Own account metadata saved. ActorIdentityId: {ActorIdentityId}; Action: {Action}; CorrelationId: {CorrelationId}", actor, "Profile", correlation.CorrelationId);
        return result!;
    }

    /// <inheritdoc />
    public async Task<OwnPreferencesDto> SavePreferencesAsync(Guid actor, PreferencesUpdateDto update, CancellationToken token)
    {
        OwnPreferencesDto? result = null;
        await writes.ExecuteAsync(async ct =>
        {
            var employee = await authorization.RequireIdentityAsync(actor, Capabilities, ct);
            var settings = await GetOrCreateAsync(employee.Id, ct);
            if (settings.PreferencesVersion != update.ExpectedVersion)
                throw new WriteConflictException("Your settings changed. Reload explicitly before saving again; unsaved values will be discarded.");
            settings.UpdatePreferences(update.Theme, update.SubmissionNotifications, update.DecisionNotifications,
                update.CancellationNotifications, update.ReassignmentNotifications);
            result = Preferences(settings);
        }, token);
        logger.LogInformation("Own account metadata saved. ActorIdentityId: {ActorIdentityId}; Action: {Action}; CorrelationId: {CorrelationId}", actor, "Preferences", correlation.CorrelationId);
        return result!;
    }

    private async Task<AccountSettings> GetOrCreateAsync(Guid employeeId, CancellationToken token)
    {
        var settings = await context.AccountSettings.SingleOrDefaultAsync(s => s.EmployeeId == employeeId, token);
        if (settings is not null) return settings;
        settings = AccountSettings.Create(employeeId);
        context.AccountSettings.Add(settings);
        return settings;
    }

    private static OwnPreferencesDto Preferences(AccountSettings? s) => new(s?.Theme ?? AccountSettings.SystemTheme,
        s?.SubmissionNotifications ?? true, s?.DecisionNotifications ?? true, s?.CancellationNotifications ?? true,
        s?.ReassignmentNotifications ?? true, s?.PreferencesVersion ?? Guid.Empty);
}
