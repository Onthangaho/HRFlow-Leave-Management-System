using System.Text.Json.Serialization;

namespace HRFlow.Application.Interfaces;

/// <summary>Allowlisted self-account operations; the server supplies Identity actor scope.</summary>
public interface IOwnAccountService
{
    /// <summary>Returns canonical employment facts and private personalisation under one protected snapshot.</summary>
    Task<OwnProfileDto> GetProfileAsync(Guid actor, CancellationToken token);
    /// <summary>Returns defaults without creating metadata on a read.</summary>
    Task<OwnPreferencesDto> GetPreferencesAsync(Guid actor, CancellationToken token);
    /// <summary>Updates only self-profile fields under a dedicated expected version.</summary>
    Task<PrivateProfileDto> SaveProfileAsync(Guid actor, ProfileUpdateDto update, CancellationToken token);
    /// <summary>Serializes delivery-time preference changes with the worker.</summary>
    Task<OwnPreferencesDto> SavePreferencesAsync(Guid actor, PreferencesUpdateDto update, CancellationToken token);
}

/// <summary>Own-account projection only; phone and preferred name never enter existing public employee DTOs.</summary>
public sealed record OwnProfileDto(string CanonicalName, string Email, IReadOnlyList<string> Roles,
    string DepartmentName, string? ManagerName, bool IsActive, bool IsActivated,
    string? PreferredDisplayName, string? ContactPhone, Guid ProfileVersion, string? EmployeeNumber, DateOnly? EmploymentStartDate, HRFlow.Application.Services.ScheduleHistoryDto ScheduleHistory);

/// <summary>Server-confirmed private field generation after a save.</summary>
public sealed record PrivateProfileDto(string? PreferredDisplayName, string? ContactPhone, Guid ProfileVersion);

/// <summary>All currently implemented delivery categories are optional; no security/email/SMS fiction.</summary>
public sealed record OwnPreferencesDto(string Theme, bool SubmissionNotifications, bool DecisionNotifications,
    bool CancellationNotifications, bool ReassignmentNotifications, Guid PreferencesVersion);

/// <summary>Unknown JSON fields are rejected rather than bound to employee or Identity entities.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ProfileUpdateDto
{
    public required Guid ExpectedVersion { get; init; }
    public string? PreferredDisplayName { get; init; }
    public string? ContactPhone { get; init; }
}

/// <summary>Explicit full preference replacement with an original loaded version.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PreferencesUpdateDto
{
    public required Guid ExpectedVersion { get; init; }
    public required string Theme { get; init; }
    public required bool SubmissionNotifications { get; init; }
    public required bool DecisionNotifications { get; init; }
    public required bool CancellationNotifications { get; init; }
    public required bool ReassignmentNotifications { get; init; }
}
