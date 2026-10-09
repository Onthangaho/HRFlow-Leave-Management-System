# Own profile and preferences verification — issue #94

Checked 9 October 2026 on `feat/profile-and-preferences`, from merged PR #105 / main `725413bec97ce1889273a3348d084f9054cf2cee`. These implementation checks preceded PR finalisation; no deployment or issue closure. Only disposable synthetic accounts/databases were used. Temporary scripts, private configuration, logs and database copies are excluded from the review diff. No automated test files were added; the #76 gate remains deferred.

## Executed HTTP and persisted-row checks

Two independent API processes shared one fresh SQLite database. Real HTTP calls and SQLite row inspection verified:

- Employee, Manager, HR-only and combined-role own-profile/default-preference reads. Reads create no settings rows. Null private fields, System and all four categories enabled are the defaults.
- Target-route attempts return 404; an arbitrary query target cannot select another account. Unknown JSON fields (roles, email, manager, lifecycle, credential version and target identity) and missing expectedVersion return 400 without canonical changes.
- Name/phone bounds and invalid characters fail. Trimming, blank clearing and plain-text values succeed. Private fields are absent from the employee directory.
- Stale profile saves return 409. Simultaneous saves using the same original version through separate processes yield one 200 and one 409. A concurrent HR name edit leaves the self-profile version unchanged; self-edit leaves HR/Identity/role/audit data unchanged.
- Invalid theme returns 400. Preferences rotate independently of the profile version.
- An injected late SQLite update failure returns 500 and rolls back settings and versions. This is injected failure evidence, not an expected production error contract.
- Disabled submission, decision, cancellation and reassignment categories produce acknowledged events with SuppressedAtUtc and no inbox row. Enabled new events deliver. Disabling does not hide existing inbox items. Re-enabling does not recreate suppressed events. Audit actor names remain canonical.
- Revoked sole capability returns 403; inactive, pending-activation and stale-credential accounts return 401. Disposable fixtures were restored afterward.

## Executed real Chromium interactions

Actual client/API interactions (not static rendering) verified:

- All four role combinations can open their own profile and the password-workspace link. Saving a preferred name updates the header as plain text; canonical HR details stay read-only.
- A recoverable intercepted 503 retains profile input. A real independent API save makes the open form stale; 409 retains the original draft/version. Explicit reload asks to discard; initial focus is Cancel, Escape preserves input, confirmed reload loads the current version.
- Saved Light/Dark preferences persist through fresh login. System follows actual browser media changes. Logout removes account theme; another account uses its own default.
- Actual duplicate settings clicks send one PUT. A real checkbox save persists its category flag.
- A delayed, already-committed settings response after logout and same-account relogin produces no old success notice. A different-account profile does not contain the previous account's private draft.
- Desktop and mobile screenshots have no page-level overflow. Dark employee directory, policy management and report pages render; the actual chart tooltip uses dark background/light foreground. Manager queue/team and notification routes remain reachable. No runtime page errors occurred in these checks.

Screenshots are synthetic, 36–105 kB each. They contain no activation links, passwords, tokens, private phone values or machine paths:

- [Desktop light profile](screenshots/profile-and-preferences/profile-light.png), [desktop dark profile](screenshots/profile-and-preferences/profile-dark.png), [mobile light profile](screenshots/profile-and-preferences/profile-mobile-light.png)
- [Desktop dark settings](screenshots/profile-and-preferences/settings-dark.png), [mobile dark settings](screenshots/profile-and-preferences/settings-mobile-dark.png)
- [Dark directory](screenshots/profile-and-preferences/directory-dark.png), [dark report](screenshots/profile-and-preferences/reports-dark.png), [actual report tooltip](screenshots/profile-and-preferences/report-tooltip-dark.png)

## Migration and build evidence

- Fresh disposable startup applied AddOwnAccountSettings. Rollback and re-upgrade on a consistent database copy preserved Employees, AspNetUsers, AspNetUserRoles, LeaveRequests, AuditEntries and RefreshTokens exactly. Rollback removes private settings/preferences; re-upgrade starts with implicit defaults, without fabricated canonical/history rows.
- `dotnet build HRFlow.sln --no-restore`: passed, 0 warnings/errors.
- `dotnet ef migrations has-pending-model-changes --no-build`: no model changes since the migration.
- Frontend build passed: 822 modules; main 529.25 kB / 159.10 kB gzip, report chunk 368.69 kB / 106.84 kB gzip. Existing Vite warning for chunks above 500 kB remains.
- Tracked and new-file diff whitespace checks passed; instruction files are byte-identical. Review-patch/private-configuration and API-log scans found no generated credentials or private profile contents.
- Frontend lint passed with the existing useAuth.tsx Fast Refresh warning. No dependencies changed; a new dependency audit was not run.
- Initial local build exposed duplicate chart tick attributes and lint found a control-character regex warning; both were corrected before the successful build/lint. The browser found an ambiguous Theme label; explicit label association was corrected before successful interactions.

## Explicit limitations / unexecuted checks

The implementation reuses the existing writer serialization and credential boundary checks; this phase did not separately inject revocation while a /me write waited behind a reservation. Preference-versus-worker contention, worker retry/restart with suppressed events, and suppression-specific two-process delivery races were not independently exercised. Exactly-once suppression across restart follows the persisted acknowledgment design but is not claimed as executed evidence here.

Settings-specific stale recovery, profile duplicate-click counting, delayed GET completion during different-account switching, and every keyboard focus/disabled-control contrast combination were not exhaustively exercised. The actual same-account delayed PUT and profile conflict scenarios above are narrower evidence.

Existing activation/resend, password change, deactivation, all decision/cancellation mutations, full HR create/edit workflows and every chart/dialog in both themes were not rerun end to end. Route reachability is not mutation verification. Screenshots are viewport captures, not proof of every off-screen control. No legal/production privacy-compliance claim. Existing preserved-account and notification retention remains indefinite until a separate retention policy/cleanup is implemented.
