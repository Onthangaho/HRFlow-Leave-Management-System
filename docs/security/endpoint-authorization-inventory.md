# Protected endpoint authorization inventory

Issue #74, checked against all ten controllers and the live Swagger operation list on
8 October 2026. There are 29 protected method/path combinations. Paths below are relative
to `/api/v1`; ASP.NET routing is case-insensitive, including Departments and Roles.

## Shared gates and authoritative boundaries

Every protected endpoint validates signed JWT issuer, audience, lifetime and signing key.
`Program.cs` requires expiry and allows 30 seconds of clock skew. `OnTokenValidated`
checks the persisted active profile/Identity account and replaces JWT role claims with
current Identity memberships for the route's early role gate. There is no positive access cache.

The early gate is not the operation's authoritative permission check. Each read below
rechecks active status and relevant memberships in its deferred read snapshot. Each write
checks after `SqliteWriteTransaction` acquires `BEGIN IMMEDIATE`, before validation or
persistence. Controller-resolved employee IDs are server-derived identifiers, not trusted
permission or department snapshots. The manager queue reloads the actor's current department.

Definitions: **Personal** means current Employee OR Manager; **HR** means current HR
Administrator; **Manager** means current Manager. All require an active account. Combined
roles are additive. HR alone never confers personal submission or decision authority.

## Personal, decision and history endpoints

- `GET /leave-types`: any currently supported Employee/Manager/HR capability;
  minimal selector only. `GetLeaveTypesQueryHandler` + `CurrentAccountAuthorization`,
  within `ILeaveReportingReadTransaction`.
- `POST /leave-requests`: Personal, own employee ID only; valid current assigned active
  Manager in the same department. `SubmitLeaveRequestCommandHandler` checks personal
  capability through `CurrentAccountAuthorization`, then existing
  `ILeaveApprovalAuthorizationService`, inside `ILeaveConfigurationTransaction`.
- `GET /leave-requests/balances`: Personal, own approved history/current policies only.
  `GetLeaveBalancesQueryHandler` + `CurrentAccountAuthorization`, deferred snapshot.
- `GET /leave-requests/history`: Personal, own requests/audits only; no raw deactivation
  reasons/correlation metadata. `GetEmployeeLeaveHistoryQueryHandler` +
  `CurrentAccountAuthorization`, deferred snapshot.
- `POST /leave-requests/{id}/cancel`: Personal, own Pending request only.
  `CancelLeaveRequestCommandHandler` + `CurrentAccountAuthorization`, inside
  `ILeaveDecisionTransaction`; ownership/status checked before domain cancellation.
- `GET /leave-requests?status=Pending`: Manager, current same-department direct reports,
  excluding self. `GetPendingLeaveRequestsQueryHandler` reloads the actor/capability
  and scopes data in the same deferred snapshot; no client-supplied department scope.
- `POST /leave-requests/{id}/approve`: Manager, current same-department direct report,
  never self; Pending/current policy/history validation. `ApproveLeaveRequestCommandHandler`
  + `LeaveApprovalAuthorizationService.EnsureManagerCanDecideAsync`, inside
  `ILeaveDecisionTransaction`. No HR override.
- `POST /leave-requests/{id}/reject`: same Manager/reporting/self restriction;
  `RejectLeaveRequestCommandHandler` + the same authorization service/reservation.
- `GET /leave-requests/team-summary`: Manager, current same-department direct reports'
  Approved leave only, excluding self; inactive history retained. Live checks and data
  in `GetTeamLeaveSummaryQueryHandler` / `ITeamLeaveReadTransaction`; 62-day inclusive range.
- `GET /leave-requests/{id}/timeline`: Personal owner OR eligible current Manager OR HR.
  `GetLeaveRequestTimelineQueryHandler` authorizes/scopes/projects in the same deferred
  snapshot. Missing/out-of-scope IDs share 404; role-less readers get 403. Only current HR
  receives raw deactivation reasons and correlation metadata. Read-only; no self-decision implication.

## HR monitoring and employees

- `GET /leave-requests/monitoring/pending`: HR, organisation-wide read-only Pending
  monitoring. `GetPendingLeaveRequestsQueryHandler` + `CurrentAccountAuthorization`,
  within the same deferred snapshot as the projection.
- `GET /reports/department-leave`: HR, organisation-wide or validated department filter.
  `GetDepartmentLeaveReportQueryHandler`, deferred snapshot for active status/membership
  and report data; 366-day inclusive range. No employee-sensitive payload or decisions.
- `GET /employees`: HR, organisation directory including inactive lifecycle details.
  `GetEmployeesQueryHandler` + `CurrentAccountAuthorization`, deferred snapshot covering
  profiles, reporting/lifecycle information, current actor names and complete roles.
- `GET /employees/{id}`: same HR authority/snapshot, one employee or 404.
- `POST /employees`: HR, profile/Identity/roles/manager creation.
  Application command delegates to `EmployeeManagementService.EnsureCurrentHrAsync`
  inside `IEmployeeManagementTransaction`; existing validation/atomicity retained.
- `PUT /employees/{id}`: same live HR authority/reservation, route target and matching
  expected version. Existing manager/role/last-active-HR safeguards retained; cannot reactivate.
- `PATCH /employees/{id}/deactivate`: same live HR authority/reservation, expected version
  and reason; last-active-HR/reporting safeguards; lifecycle plus Pending cancellations and
  existing audits persist atomically. The client explicitly prohibits authentication replay.
- `GET /Departments`: any currently supported capability; names/IDs only.
  `ReferenceDataService.GetDepartmentsAsync` + `CurrentAccountAuthorization`, deferred snapshot.
- `GET /Roles`: HR; currently provisioned Identity role names only, not Identity entities.
  `ReferenceDataService.GetRolesAsync` + `CurrentAccountAuthorization`, deferred snapshot.
  The employee-management form/service still validates the supported Employee/Manager/HR contract;
  this selector does not invent availability when a role has not been provisioned.

## HR leave configuration (ten operations)

Every operation requires current active HR. Shared policies, reference integrity, versions,
historical requests/audits and existing calculation semantics are unchanged.

- `GET /management/leave-types`: `LeaveConfigurationService.GetTypesAsync` checks live HR
  and projects type/policy/reference snapshots in `ILeaveReportingReadTransaction`.
- `GET /management/leave-types/{id}`: same protection, one record or 404.
- `POST /management/leave-types`: `SaveTypeAsync` / `EnsureHrAsync`, writer reservation;
  explicit policy/normalized unique name, atomic creation and response projection.
- `PUT /management/leave-types/{id}`: same live authority/reservation plus expected version.
- `DELETE /management/leave-types/{id}`: `DeleteTypeAsync` / `EnsureHrAsync`, writer
  reservation; expected version; any referenced request blocks deletion.
- `GET /management/leave-policies`: `GetPoliciesAsync`, live HR/data in deferred snapshot.
- `GET /management/leave-policies/{id}`: same protection, one record or 404.
- `POST /management/leave-policies`: `SavePolicyAsync` / `EnsureHrAsync`, writer reservation.
- `PUT /management/leave-policies/{id}`: same protection plus expected version.
- `DELETE /management/leave-policies/{id}`: `DeletePolicyAsync` / `EnsureHrAsync`, writer
  reservation; expected version; linked types block deletion.

Save responses reuse private projection methods within the already-held write transaction,
rather than opening nested read transactions. Public management reads always authorize.

## Activation follow-up (#79)

`POST /employees` now creates a passwordless pending account. Existing current-HR validation
inside writer protection remains; no password/token is returned. HR directory/detail reads
project safe activation status in the same deferred snapshot.

`POST /employees/{id}/activation/resend`: current active, activated HR; target original
version, active profile and pending passwordless Identity checked by AccountActivationService
after the existing SQLite writer reservation. No automatic auth replay.

`POST /auth/activation`: anonymous account-bound opaque invitation proof, no client actor ID;
active target, UTC expiry, pending state and absent password checked inside that reservation.
Identity password save and invitation consumption are atomic; no role restoration or login.
Redemption/resend have bounded bodies and per-process/IP limits.

IAccountAccessService now requires activated eligibility as well as active employment.
Bearer validation and CurrentAccountAuthorization repeat this guard; pending accounts
cannot issue/refresh tokens or use protected operations. Existing roles remain visible to HR
without treating pending users as available Managers/last administrators.

See [activation design](../adr/0011-secure-account-activation.md) and
[executed checks](../verification/secure-account-activation.md).

## Anonymous endpoints and limits

`POST /auth/login` and `POST /auth/refresh` deliberately do not require an access JWT.
`AuthService` verifies credentials or single-use unexpired refresh-token proof, current active
state and current roles under the existing writer reservation. Refresh rotation and replacement
token persistence are atomic. These endpoints do not accept an actor ID.

`GET /health` is anonymous and returns no protected data. Development Swagger/UI documents
routes, not employee data; it is not mapped in production. No other protected controller route
was found. Unrouted legacy handlers do not create additional HTTP access paths.

An already-authorized read may finish against its earlier coherent snapshot during a role
change/deactivation. Subsequent reads deny; this is not retroactive revocation of an in-flight
response. A write waiting for the writer must use newly committed permissions afterward.
Client logout/relogin boundaries separately reject stale completions. Current roles are not
pushed into other tabs: navigation may still reflect the last issued JWT until refresh, but
the API remains authoritative and returns 403 for lost capability, never a refresh loop.

See [design decisions](../adr/0009-jwt-current-permissions.md) and
[executed verification and limitations](../verification/jwt-current-permissions.md).

## Password-change revocation extension — #80

Checked 9 October 2026. POST /auth/password is authenticated own-account only, active and
activated; no target ID or HR override. PasswordChangeService uses Identity and rechecks
original credential_version inside IEmployeeManagementTransaction. Status 204 means all
prior access/refresh proof revoked, including caller. Body/password/rate bounds are documented
in [ADR 0012](../adr/0012-password-change-session-revocation.md).

Every protected endpoint listed above now additionally checks persisted credential_version
at the bearer gate AND at the beginning of its shared read snapshot/write reservation.
Missing/mismatched generations deny 401; current capability loss remains 403. These checks
include personal submit/cancel/history/balances, Manager queue/decisions/team, HR employees/
activation resend/configuration/monitoring/reports/references and scoped timelines. Anonymous
login/refresh/activation retain their own serialized proof checks; migration revokes legacy
refreshes so pre-upgrade sessions require fresh login. Existing ownership/privacy scopes remain.

## Issue #93 addition — 9 October 2026

The original dated endpoint count above is historical. The notification controller adds:

- GET /notifications: any supported current capability, own recipient only; active/activated account and initiating credential checked in the same deferred snapshot as read filter, paging and current request-link scope.
- GET /notifications/unread-count: same own-recipient/current-capability snapshot; unavailable unread items are counted without exposing their request IDs.
- PATCH /notifications/{id}/read: own recipient only, current credentials and capabilities rechecked after the SQLite writer reservation; another recipient's ID and missing ID both return 404. Idempotent read time, no status/audit mutation.

Authoritative service: LeaveNotificationService through CurrentAccountAuthorization, ILeaveReportingReadTransaction and IEmployeeManagementTransaction. Existing timeline deep links independently check current owner/Manager/HR scope; alerts never grant access. No client recipient or actor ID is accepted.
