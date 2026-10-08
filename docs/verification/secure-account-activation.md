# Secure account activation verification

Checked 8 October 2026 on feat/secure-account-activation, based on origin/main
d3e2163323b05e9d6cc400d438688a6889066fa8. PR #102 was confirmed merged before branch
creation. Starting working tree was clean. No existing application database was modified.
Issue #79 wording was corrected. These implementation checks preceded PR finalisation;
no issue closure or deployment was performed.

## Executed HTTP and persisted-row checks

Temporary tooling and generated configuration stayed outside the review diff. Two actual
API processes used separate scoped DbContexts/connections against the same disposable
SQLite database. Inspections covered AspNetUsers, AspNetUserRoles, Employees, RefreshTokens,
LeaveRequests and AuditEntries, not HTTP status alone.

- HR creation: 201 with safe PickupReady state, passwordless Identity, linked active
  profile, pending flag, assigned roles and hash-only invitation persistence. No raw
  token in management DTOs. Employee direct creation returned 403.
- Pending login returned 401. A locally signed otherwise-valid access token was denied
  with 401. Synthetic valid stored refresh tokens for a pending account returned 401;
  persisted rows were revoked with no replacement refresh token.
- Missing/tampered invitations returned safe 400. Weak Identity password validation
  returned 400 while preserving password=null, pending state and the invitation. The
  same invitation then activated (204) and normal login succeeded. Reuse returned 400.
- Resend returned 200, changed the token and profile version; old token returned 400,
  stale resend 409, and the replacement token activated successfully.
- Separate API-process concurrent redemption: exactly one 204 and one 400; persisted
  first password, pending=false, hash=null. No established password was overwritten.
- Expired UTC invitation returned 400 and remained passwordless. Inactive target redemption
  returned 400; resend returned 409, without changing employment state or password.
- Pending Manager assignment failed 400; no orphan Identity was inserted. Duplicate email
  differing by case against an inactive account returned 409 without overwrite/reactivation.
- Injected SQLite trigger failure on final activation state update returned generic 500.
  Identity password, pending flag and token consumption all rolled back. Removing the
  disposable trigger allowed the SAME invitation to succeed.
- Pending HR did not count towards last-available-HR safeguards: original HR deactivation
  and role removal returned 409; pending HR itself could be deactivated. Activation after
  replacing Employee with Manager preserved the current roles, without restoring Employee.
  The final API build projected Expired accurately.
- Pending recipient-email edit retained roles/manager, invalidated the old token and
  required explicit resend. The replacement invitation worked for the updated account.
- Two-process redemption/deactivation race: deactivation returned 200 and redemption
  returned 400; persisted inactive, pending, passwordless state prevented login.
- Two-process resend/deactivation with the same original version: deactivation returned
  200, resend returned 409, and the persisted account remained inactive/passwordless.
- HR membership was removed in a disposable SQL transaction while creation waited for
  SQLite's writer. After release, request returned 403 and inserted no account/profile.
  Synthetic membership was restored for subsequent verification only.
- A third real API process used a privately isolated pickup profile obstructed by a
  disposable file: creation committed with DeliveryFailed, pending=true, password=null.
  Explicit resend through the working provider recovered and activated the account.
- Ordinary API logs were checked against generated passwords and private pickup secrets:
  no raw password, token or activation-link fragment was present. No such values appear
  in these review artifacts.
- Eleven bounded invalid redemption calls produced 400/429, proving the real rate limit.

## Migration evidence

A COPY of pre-activation disposable data contained 6 profiles, 4 requests and 8 audits.
Migration up retained all original Identity/password/role, employee, request, audit and
refresh rows exactly, with legacy RequiresActivation=false, hash=null and activation
date=null. Down retained all pre-existing rows and removed the activation columns/index;
repeat up succeeded. This was a disposable migration round-trip, not production rollback.

## Real interactive Chromium evidence

Chrome/Playwright drove the actual Vite client/API, not static rendering:

- HR New Employee form had no initial-password control; creation reached private pickup
  and a Pending activation badge. Pending Manager was absent from assignment choices.
- Resend started on Cancel, wrapped Tab/Shift+Tab, dismissed with Escape and restored
  opener focus. Two immediate confirm clicks produced ONE POST.
- Activation removed its token fragment from the address bar. Password mismatch and
  Identity validation failure preserved input and pending state. Valid activation
  committed, displayed success and offered normal login WITHOUT signing in automatically.
- Desktop 1440×960 and mobile 390×844 had no page-level overflow for captured workflows.
- Newly activated Employee logged in, used the actual leave form and saw personal history.
  Manager used the real approval dialog with a note. SQLite retained exactly one initial
  submission and one decision audit with the note. Team view remained usable.
- HR profile edit succeeded. Policy management, Pending monitoring and department reports
  remained reachable/rendered through actual navigation. No browser runtime errors were
  observed in that regression run. These page visits do not claim new exhaustive CRUD tests.
- A committed resend response was held while logout/login changed account, then repeated
  for same-account relogin: no old employee content or success notice appeared.
- Injected current-session resend 401 caused ONE request, no refresh/replay, and login.

Initial temporary verification scripts needed correction for the existing refresh-column
mapping and queue article selector; their partial output was not treated as completion.
Continuation checks proved the remaining assertions against the persisted records.

## Screenshots — synthetic data only

- [Desktop directory](screenshots/account-activation/directory-desktop.png)
- [Mobile directory](screenshots/account-activation/directory-mobile.png)
- [Desktop resend](screenshots/account-activation/resend-desktop.png)
- [Mobile resend](screenshots/account-activation/resend-mobile.png)
- [Desktop activation](screenshots/account-activation/activation-desktop.png)
- [Mobile activation validation](screenshots/account-activation/activation-mobile.png)
- [Mobile activation success](screenshots/account-activation/activation-success-mobile.png)

Images are approximately 12–80 KB each and contain synthetic profiles only, with no
pickup URL, token, password, private configuration or machine-specific path.

## Executed checks and limits

- Backend build: passed, 0 warnings / 0 errors.
- Frontend build: passed; main JS 509.13 KB (154.19 KB gzip), report chunk 368.31 KB
  (106.74 KB gzip). Existing >500 KB Vite warning remains; no unrelated optimisation.
- Frontend lint: passed with existing useAuth.tsx:210 Fast Refresh export warning.
- EF pending-model check: no changes since the last migration.
- Dependencies/lockfile unchanged; dependency audit not rerun because no package changed.
- git diff --check and new-file whitespace checks: passed. Both instruction files are byte
  identical. Relative documentation links, credential/path scans and the complete patch
  reverse-application check passed. Synthetic screenshot contents/sizes were reviewed.

Unexecuted: real email/production delivery, production rollout/rollback, multi-node shared
filesystems and distributed rate limiting, exhaustive race/load permutations, manual screen
reader testing, other browsers/physical devices, full regression of every leave/HR mutation,
client offline upload-style cancellation (no uploads added), browser stress of all failure
statuses. No automated test files were introduced; #76 convention remains deferred.
Delivery may remain PendingDelivery after process crash/cancellation/lost acknowledgement;
HR must inspect status and explicitly resend. Pickup retention cleanup is operational and
not automated. Do not claim production onboarding or legal compliance.
