using System.Text.RegularExpressions;
using HRFlow.Domain.Common;

namespace HRFlow.Domain.Entities;

/// <summary>Own-account metadata with independent edit generations; never mutates HR or Identity facts.</summary>
public sealed class AccountSettings
{
    public const int MaxPreferredNameLength = 80;
    public const int MaxContactPhoneLength = 30;
    public const string SystemTheme = "System";
    public const string LightTheme = "Light";
    public const string DarkTheme = "Dark";

    /// <summary>One metadata row per preserved employee profile; not a client-selected target.</summary>
    public Guid EmployeeId { get; private set; }
    /// <summary>Personalisation only; canonical audit names remain unchanged.</summary>
    public string? PreferredDisplayName { get; private set; }
    /// <summary>Private self-profile contact, excluded from HR selectors and reports.</summary>
    public string? ContactPhone { get; private set; }
    /// <summary>Zero identifies a never-edited section, not an authentication credential.</summary>
    public Guid ProfileVersion { get; private set; }
    /// <summary>Theme/toggle edits conflict independently from profile and employment edits.</summary>
    public Guid PreferencesVersion { get; private set; }
    public string Theme { get; private set; } = SystemTheme;
    public bool SubmissionNotifications { get; private set; } = true;
    public bool DecisionNotifications { get; private set; } = true;
    public bool CancellationNotifications { get; private set; } = true;
    public bool ReassignmentNotifications { get; private set; } = true;

    private AccountSettings() { }

    /// <summary>Materialises defaults only during a protected save; read paths stay read-only.</summary>
    public static AccountSettings Create(Guid employeeId) => new() { EmployeeId = employeeId };

    /// <summary>Validates all allowed content before changing a dedicated profile version.</summary>
    public void UpdateProfile(string? preferredName, string? phone)
    {
        var name = string.IsNullOrWhiteSpace(preferredName) ? null : preferredName.Trim();
        var contact = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        if (name is not null && (name.Length > MaxPreferredNameLength || name.Any(char.IsControl)))
            throw new DomainException("Preferred display name must be at most 80 characters without control characters.");
        if (contact is not null && (contact.Length > MaxContactPhoneLength
            || !Regex.IsMatch(contact, @"\A[0-9+ ().-]+\z") || !contact.Any(char.IsDigit)))
            throw new DomainException("Contact phone must be at most 30 characters using digits, +, spaces, parentheses, periods or hyphens.");
        PreferredDisplayName = name;
        ContactPhone = contact;
        ProfileVersion = Guid.NewGuid();
    }

    /// <summary>Replaces implemented preferences without altering profile, HR or credential versions.</summary>
    public void UpdatePreferences(string theme, bool submissions, bool decisions, bool cancellations, bool reassignments)
    {
        if (theme is not (SystemTheme or LightTheme or DarkTheme))
            throw new DomainException("Theme must be System, Light or Dark.");
        Theme = theme;
        SubmissionNotifications = submissions;
        DecisionNotifications = decisions;
        CancellationNotifications = cancellations;
        ReassignmentNotifications = reassignments;
        PreferencesVersion = Guid.NewGuid();
    }

    /// <summary>Checked at delivery time; unknown future categories fail closed until explicitly configured.</summary>
    public bool AllowsNotification(string kind) => kind switch
    {
        "Submit" => SubmissionNotifications,
        "Approve" or "Reject" => DecisionNotifications,
        "Cancel" => CancellationNotifications,
        "Reassigned" => ReassignmentNotifications,
        _ => false
    };
}
