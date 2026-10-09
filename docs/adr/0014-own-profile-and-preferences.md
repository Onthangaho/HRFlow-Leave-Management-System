# Own-account profile and preferences (issue #94)

## Contract before implementation

Only preferred display name (optional, trimmed, at most 80 characters, no control characters) and contact phone (optional, trimmed, at most 30 characters; digits plus +, spaces, parentheses, periods and hyphens) are self-editable. Blank clears either field. Phone is private own-account contact information, not an Identity login/recovery mechanism. Canonical HR name remains the employee-directory and audit actor name. No employee number or employment dates exist yet, so none are fabricated.

Dedicated account-settings metadata uses separate profile and preference versions, independent of HR employee edits and credential versions. Missing legacy/new settings rows mean nullable profile fields, System theme and all implemented notification categories enabled. The all-zero GUID is the explicit initial version for an absent/never-edited section. First save creates metadata and rotates only that section's version under the existing writer reservation; subsequent stale versions return 409. A deferred read never creates a row.

All implemented categories are optional: submission, decisions (approval/rejection), cancellations (owner/HR), and reassignment of Pending work. There are no implemented mandatory security alerts, email/SMS or hidden preference controls. The worker reads preferences inside its delivery writer transaction. Disabled categories acknowledge the event with a suppression timestamp and no notification row. Existing inbox items are untouched; re-enabling does not replay suppressed events. Preference writes and delivery serialize through SQLite protection. The legacy DeliveredAtUtc field remains the processing/acknowledgment marker; SuppressedAtUtc explicitly distinguishes suppression from actual delivery.

Own-account identity comes from authentication, never URL/body target IDs. Current active, activated capabilities and credential proof are checked in the existing consistent snapshots/reservations. Allowlisted write DTOs reject unknown JSON fields using .NET 8 unmapped-member handling (https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/missing-members). No role, HR-name, lifecycle, reporting, credential or activation repair. No profile values or phone in logs.

Theme is persisted server-side: System/Light/Dark. Shared semantic colour tokens cover the shell, existing utility palette, dialogs and charts. System follows media changes while signed in. Account replacement/logout clears the old document theme before applying the new session's settings. No profile/preference data in browser storage. Forms retain original versions/values across refetches and recoverable failures; explicit discard/reload is required.

## API and rollout

GET `/api/v1/me` returns canonical name/email, current roles, department/manager names, active/activated facts and private profile fields/version. GET `/api/v1/me/preferences` returns theme, four toggles and preferencesVersion. PUT `/api/v1/me/profile` accepts expectedVersion and only preferredDisplayName/contactPhone. PUT `/api/v1/me/preferences` requires expectedVersion, theme and the full four-toggle replacement. Unknown JSON fields are rejected. Successful writes return 200 with the confirmed version; validation 400, credential/lifecycle denial 401, capability denial 403, stale/SQLite contention 409. No target-ID contract. Payload limit 4096 bytes. Both saves are non-replayable.

AddOwnAccountSettings adds a RESTRICT-linked metadata table and nullable outbox SuppressedAtUtc; it does not alter existing employee/Identity/request/audit rows. Rollback loses newly stored private values/preferences and suppression markers. DeliveredAtUtc acknowledgments remain, so already suppressed events do not become replay candidates. Re-upgrade uses defaults for future events. There is no automatic historical backfill.

Profile writes rotate only ProfileVersion; preference writes rotate only PreferencesVersion. HR Version and credential version are untouched. SQLite writer reservation is acquired before authoritative account/version reads; the worker uses that same reservation before reading preferences. No application callback replay or new audit mechanism. Deferred reads share authorization and own metadata in one snapshot.

[Actual verification and unexecuted limits](../verification/profile-and-preferences.md). No production/privacy-compliance claim; private metadata follows existing preserved-account retention until a reviewed retention policy exists.
