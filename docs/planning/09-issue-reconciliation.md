# Issue reconciliation — 15 October 2026

Read-only tracker review at main `1c660c1`. Of 29 requested issues, 28 were fetched OPEN; #78 was CLOSED (9 October 2026, 21:18:49 UTC).
No issue edited/closed. “Complete” recommends closure after review, not automatic closure;
“Partial” preserves parent scope, “Blocked” names unresolved dependencies. No issue in this set
is recommended wholly superseded; selected stale transport/safety wording is superseded explicitly.

## Superseding CI evidence after merged #121

Tracker states/criteria below remain the fetched review baseline, not new issue edits. Original PR
#120 Linux execution was 50 backend pass / 4 fail. #121 corrected fixture and production creation
permissions without relaxing validation; [final hosted Ubuntu run](https://github.com/Onthangaho/HRFlow-Leave-Management-System/actions/runs/38786228861)
passed 55 backend and 9 Chromium tests. Earlier billing blocks are historical. Positive ClamAV
coverage remains separate local evidence; broad Linux deployment/scanner/recovery checks remain.

## Acceptance baseline

The verbatim criteria below are the **currently fetched tracker contract**, including authorised
wording changes to #21/#74/#79/#93. GitHub issue edit-history/original-at-creation bodies were not
retrieved; this is not a claim to reconstruct deleted wording. Historical original scope remains
in [the initial catalogue](07-implementation-issue-catalogue.md) and dated merged PRs.
Non-acceptance scope/dependency sections were also read; summaries below account for them.
The legal research baseline is not qualified sign-off. No product tests were rerun in this review.

## [#17: GET balance + PATCH cancel endpoints](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/17)

**Fetched GitHub state: OPEN.**

**Recommendation: Partial (contract reconciliation).**

### Tracker acceptance criteria (verbatim)

- Given a non-owner attempts to cancel another employee's request, When called, Then the API returns 403.

### Current evidence and remaining work

- **Implemented:** Owner-derived balances and POST cancellation, protected owner check returning 403 for another owner, Pending-only status and atomic audit. PRs #59/#60/#64/#99.
- **Remaining functionality:** Requested arbitrary employee balance route and PATCH route are intentionally not the current contract. Do not add aliases just to match stale wording; approve the safer owner-derived GET /leave-requests/balances and POST /leave-requests/{id}/cancel contract first.
- **Verification only:** Existing regression cancellation/audit checks; no workflow rerun now.
- **Dependencies/decision:** Product owner accepts revised transport contract, then recommend closure; current literal route criteria do not match.
- **Source:** [inspected contract/source](../../src/HRFlow.Application/Features/LeaveRequests/Commands/CancelLeaveRequest/CancelLeaveRequestCommandHandler.cs). **Dated evidence:** [leave-request-audit-timeline](../verification/leave-request-audit-timeline.md). **Merged evidence:** [PR #59](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/59), [PR #60](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/60), [PR #64](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/64), [PR #99](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/99).

## [#21: Manager approval queue and optional decision notes](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/21)

**Fetched GitHub state: OPEN.**

**Recommendation: Complete.**

### Tracker acceptance criteria (verbatim)

- Accessible approve/reject confirmation dialogs identify the employee, type and inclusive calendar dates, offer an optional labelled note, and retain input after recoverable errors.
- Current active Manager membership, current same-department direct-report scope and Pending status are rechecked inside the existing SQLite writer reservation. No self-decisions or HR-only override; combined capabilities remain additive.
- Approval retains current-policy balance and same-type overlap validation.
- Status, audit and note commit together. Failed, repeated, forbidden or rolled-back attempts add no decision audit.
- Owner personal history and authorized owner/eligible Manager/HR timelines show notes as plain text. Existing HR-only sensitive-reason/correlation filtering remains intact; no audit-edit endpoint.
- The queue updates after a server-confirmed successful response, without full-page reload. No optimistic approval/rejection is displayed.
- Duplicate submissions are blocked; stale decisions require explicit refresh; late callbacks cannot affect a replacement account/session, including same-account relogin.
- Logs contain identifiers/action/correlation, never note contents. Document limits, visibility, migration and legacy behavior.

### Current evidence and remaining work

- **Implemented:** Queue and optional 500-trimmed-character plain-text approve/reject notes, immutable decision audit, personal history/timeline visibility, safe recovery dialogs and session guards; PR #102.
- **Remaining functionality:** None found against the current optional-note contract. Mandatory rejection reason is excluded.
- **Verification only:** Dated browser/row/rollback evidence plus retained decision/audit gates; broader device/AT permutations do not reopen this functional slice.
- **Dependencies/decision:** Recommend closure after independent evidence review. Replace obsolete test-deferral wording with current #76 convention.
- **Source:** [inspected contract/source](../../src/HRFlow.Domain/Entities/AuditEntry.cs). **Dated evidence:** [manager-decision-notes](../verification/manager-decision-notes.md). **Merged evidence:** [PR #102](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/102).

## [#22: LeaveType and LeavePolicy entities](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/22)

**Fetched GitHub state: OPEN.**

**Recommendation: Partial.**

### Tracker acceptance criteria (verbatim)

- Given a LeavePolicy is created, When a balance is calculated, Then the calculation reads accrual rules from this entity, not a hardcoded value.

### Current evidence and remaining work

- **Implemented:** LeaveType references shared LeavePolicy; policy-configured entitlement and same-type overlap consumed by the shared balance calculator; versioned CRUD PR #67/#68.
- **Remaining functionality:** Accrual-rule entity/engine, effective-dated rules, cycle allocation, holiday/work units and preserved charged-day ledger are absent. Fixed DefaultBalance is not an accrual rule.
- **Verification only:** Existing CRUD/current-calendar-day evidence does not verify statutory accrual.
- **Dependencies/decision:** Keep open; agree revised central calculation contract following qualified #75, employment #78 and category decisions.
- **Source:** [inspected contract/source](../../src/HRFlow.Application/Features/LeaveRequests/LeaveBalanceCalculator.cs). **Dated evidence:** [leave-policy-management](../verification/leave-policy-management.md). **Merged evidence:** [PR #67](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/67), [PR #68](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/68).

## [#36: Harden dev admin seeding per CodeRabbit review](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/36)

**Fetched GitHub state: OPEN.**

**Recommendation: Complete (accepted safety correction).**

### Tracker acceptance criteria (verbatim)

Follow-up from CodeRabbit review on PR for issue #3.

- Move the Development-only environment guard inside SeedDevelopmentAdministratorAsync itself (resolve IHostEnvironment, return early if not IsDevelopment()), so the method is self-guarded rather than relying on the caller in Program.cs to remember to wrap it.
- Make the seeded password configurable via IConfiguration["Seeding:HrAdministratorPassword"], with the current literal retained as the fallback default. Update the README to note it's a default, overridable via configuration.
- Do NOT build automatic legacy-database migration/repair logic for the EnsureCreated-to-MigrateAsync transition - that's disproportionate for a project with no real deployment. Instead, add a one-line README troubleshooting note: if a local .db predates this change and throws a migration error, delete the file and restart.

### Current evidence and remaining work

- **Implemented:** Self-guarded DevelopmentIdentitySeeder and DatabaseSeeder, configurable Development passwords, no existing-account role restoration or reactivation. Historical PR #37; later #69/#107 hardening.
- **Remaining functionality:** None for safe Development seeding. Unrestricted delete-and-restart guidance should be superseded by backed-up disposable-store-only remediation, not implemented literally.
- **Verification only:** Production seed denial recorded in #107 report; no fresh seed execution now.
- **Dependencies/decision:** Recommend closure after approving safer README guidance and renamed SeedIdentityAsync equivalent to old method wording. First-admin provisioning is separate #81.
- **Source:** [inspected contract/source](../../src/HRFlow.Infrastructure/Seeding/DevelopmentIdentitySeeder.cs). **Dated evidence:** [safe-deployment-configuration](../verification/safe-deployment-configuration.md). **Merged evidence:** [PR #37](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/37), [PR #69](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/69), [PR #107](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/107).

## [#74: Enforce JWT expiry and current permissions across protected HRFlow APIs](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/74)

**Fetched GitHub state: OPEN.**

**Recommendation: Complete.**

### Tracker acceptance criteria (verbatim)

- [ ] Expired JWTs fail with documented clock skew; valid active accounts still work; rotating refresh remains bounded and single-use.
- [ ] Removed Employee/Manager/HR permissions deny all relevant old-token routes, including monitoring, employee reads, selectors, submit/cancel and queues.
- [ ] Authorization and data use the same read snapshot; write authorization occurs after writer reservation. Current eligible managers retain additive personal capability; HR-only never decides.
- [ ] Expired access tokens may use the existing valid same-session refresh and at most one retry; issuer, audience, signature, expiry and a documented small clock skew remain enforced.
- [ ] Invalid, expired or revoked/replayed refresh tokens terminate only their initiating session; inactive accounts remain denied.
- [ ] Current permission loss returns an appropriate denial, not a refresh loop. Combined accounts retain any other current valid capability.
- [ ] Old-session requests cannot refresh, clear or retry against a new session, including logout/login as the same account; delayed success cannot expose old data.
- [ ] Explicitly non-replayable operations remain non-replayable. Logs include identifiers/correlation, never credentials or tokens.

### Current evidence and remaining work

- **Implemented:** JWT lifetime/30-second skew, issuer/audience/signature, current active capabilities at gate and snapshot/write boundaries, bounded same-session refresh, non-replayable exceptions; PR #99 and #104 credential strengthening.
- **Remaining functionality:** No functional omission found against current criteria.
- **Verification only:** Not every clock/skew permutation is retained/executed; retain as #76 verification debt, not an invented missing auth feature. Endpoint inventory is linked in jwt-current-permissions report.
- **Dependencies/decision:** Recommend closure after security review of linked evidence; platform gate remains independent.
- **Source:** [inspected contract/source](../../src/HRFlow.Application/Services/CurrentAccountAuthorization.cs). **Dated evidence:** [jwt-current-permissions](../verification/jwt-current-permissions.md). **Merged evidence:** [PR #99](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/99), [PR #104](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/104).

## [#75: Approve the South African leave specification and calculation boundary cases](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/75)

**Fetched GitHub state: OPEN.**

**Recommendation: Blocked.**

### Tracker acceptance criteria (verbatim)

- [ ] Resolve adoption-age retained wording, sole-employed/shared allocation, recovery/notice/deadline rules and transitions; do not use court media summary in place of the order.
- [ ] Approve annual/sick/family cycle anchors, fractional units, leap boundaries, incomplete worked-day records and contractual variations.
- [ ] Define certificate proof/payment separately from recording absence; parental eligibility is never solely Gender.
- [ ] Approve minimum-data access/retention and company enhancements; record unresolved decisions as blocked, not assumed compliant.

### Current evidence and remaining work

- **Implemented:** Source-backed South African requirements, authoritative-source links, interpretation questions and examples (PR #100).
- **Remaining functionality:** Qualified approval register, named reviewer and approved calculation contracts absent.
- **Verification only:** No qualified sign-off or current legislative revalidation performed here.
- **Dependencies/decision:** External labour-law/privacy review; do not equate agent research with approval.
- **Source:** [inspected contract/source](../../docs/planning/04-south-african-leave-requirements.md). **Merged evidence:** [PR #100](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/100).

## [#76: Introduce focused regression gates with an explicit test-convention decision](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/76)

**Fetched GitHub state: OPEN.**

**Recommendation: Partial.**

### Tracker acceptance criteria (verbatim)

- [ ] Cover live role/lifecycle access, no self/HR decisions, policy balance/overlap, version conflicts, status/audit rollback and cross-process SQLite serialization.
- [ ] Cover JWT expiry/refresh, account/same-account session epochs, stale forms and no mutation replay.
- [ ] Retain migration preservation/legacy fixtures and add reviewed calculation examples as features land.
- [ ] Document actual running gates versus manual accessibility/load/legal review; do not call an unexecuted checklist coverage.

### Current evidence and remaining work

- **Implemented:** Maintained xUnit and Playwright, disposable migrated SQLite, actual two-process barriers, persisted assertions, defect experiments and CI; PR #117, image extension #119.
- **Remaining functionality:** Incremental coverage remains: full expiry/skew/refresh boundary matrix and broader failure workflows. Test convention has already been authorised.
- **Verification only:** Final #121 hosted Ubuntu/Chromium gates passed (55 backend / 9 browser). Earlier billing blocks are historical; comprehensive scanner/AT/load/deployment remains unexecuted.
- **Dependencies/decision:** Keep open for bounded follow-up gates; current retained hosted gates now pass without weakening CI.
- **Source:** [inspected contract/source](../../tests/README.md). **Dated evidence:** [critical-workflow-regressions](../verification/critical-workflow-regressions.md). **Merged evidence:** [PR #117](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/117), [PR #119](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/119).

## [#77: Add a responsive role-aware application shell around existing workflows](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/77)

**Fetched GitHub state: OPEN.**

**Recommendation: Complete.**

### Tracker acceptance criteria (verbatim)

- [ ] Employee/Manager personal links and eligible Manager/HR sections are clear; HR never receives decision actions.
- [ ] Desktop/mobile has no page-level overflow; keyboard/focus/contrast/reduced-motion and menu announcements work.
- [ ] Loading/empty/error/retry and duplicate-action styling is consistent without replacing existing safeguards.
- [ ] Logout/account switching removes protected content; direct URL denials remain authoritative.

### Current evidence and remaining work

- **Implemented:** Shared responsive protected shell, grouped additive menus, titles, drawer focus/Escape/restoration, skip link; PR #101. Working Account/notification additions are now real.
- **Remaining functionality:** None against shell criteria; route splitting/polish are improvement candidates.
- **Verification only:** Recorded desktop/mobile/keyboard/session workflow evidence; full AT audit remains release verification.
- **Dependencies/decision:** Recommend closure for delivered shell; do not require exhaustive device permutations as new functionality.
- **Source:** [inspected contract/source](../../src/HRFlow.Client/src/components/layout/ApplicationShell.tsx). **Dated evidence:** [responsive-application-shell](../verification/responsive-application-shell.md). **Merged evidence:** [PR #101](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/101).

## [#78: Add versioned employment dates, employee numbers and work schedules](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/78)

**Fetched GitHub state: CLOSED.**

**Recommendation: Partial.**

### Tracker acceptance criteria (verbatim)

- [ ] HR can configure required inputs with units/effective dates; employee numbers are normalized/unique under concurrent writes.
- [ ] Legacy null/unknown dates and schedules remain visible as incomplete; account creation is never treated as hire date.
- [ ] Department/role/report safeguards, Preserve/Assign/Clear and stale edit conflicts still work atomically with Identity.
- [ ] Holiday sources and schedule revisions are versioned; a future schedule change does not rewrite approved charged days.

### Current evidence and remaining work

- **Implemented:** Unique normalized employee numbers, confirmed nullable legacy dates, append-only fixed weekly revisions, HR forms and read-only own details; PR #111. Future starts do not delay activated-account access.
- **Remaining functionality:** Versioned holiday sources and remaining calculation/cycle inputs are not implemented. Rotating/overnight patterns excluded.
- **Verification only:** Two-process duplicate/version, browser, migration/recovery evidence recorded. Broader platform checks remain verification only.
- **Dependencies/decision:** #78 is already CLOSED despite the original holiday-source remainder. Propose explicit allocation to #22/#85 or owner review of #78 scope; do not reopen it automatically. #75/#22 are prerequisites.
- **Source:** [inspected contract/source](../../docs/adr/0017-confirmed-employment-data.md). **Dated evidence:** [employment-data-foundation](../verification/employment-data-foundation.md). **Merged evidence:** [PR #111](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/111).

## [#79: Provision HR-created accounts through secure one-time activation](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/79)

**Fetched GitHub state: OPEN.**

**Recommendation: Partial.**

### Tracker acceptance criteria (verbatim)

- [ ] Tokens are purpose/account-bound, expiring and single-use; repeated/concurrent redemption yields one safe outcome.
- [ ] Account creation and invitation resend require current active HR. The intended employee anonymously redeems a valid account-bound activation token without HR membership or an existing login; activation never grants or restores roles. Activation cannot overwrite, reactivate or restore roles to an existing account.
- [ ] No credential in preview/report/log; activation secrets are delivered only via the approved channel, not downloadable CSV.
- [ ] Document delivery failure/retry and account state; initial credential flow forces password establishment before normal access.

### Current evidence and remaining work

- **Implemented:** Passwordless Pending activation, hashed opaque single-use expiring token, resend invalidation, atomic password/token establishment and private Development pickup; PR #103.
- **Remaining functionality:** Approved production delivery adapter absent; channel approval and operational recovery remain.
- **Verification only:** Recorded real local activation/concurrent redemption and failure evidence; external real-delivery execution absent.
- **Dependencies/decision:** Keep open for production channel; no external sending or purchase authorised by this review.
- **Source:** [inspected contract/source](../../src/HRFlow.Infrastructure/Services/Auth/AccountActivationService.cs). **Dated evidence:** [secure-account-activation](../verification/secure-account-activation.md). **Merged evidence:** [PR #103](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/103).

## [#80: Add Identity password change with verified session revocation](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/80)

**Fetched GitHub state: OPEN.**

**Recommendation: Complete.**

### Tracker acceptance criteria (verbatim)

- [ ] Wrong current password, invalid new password and revoked/inactive actor fail without partial changes.
- [ ] After commit, old JWTs and all prior refresh tokens cannot access/refresh; changing Identity SecurityStamp alone is not assumed to revoke bearer tokens.
- [ ] Concurrent refresh/change follows a valid serial order; 401 does not replay writes.
- [ ] No password logs; accessible confirmation/loading/error, duplicate prevention and retained safe form state.

### Current evidence and remaining work

- **Implemented:** Own-account Identity ChangePassword, atomic credential-version rotation/all refresh revocation, API/transaction-boundary checks, non-replayable UI sign-out; PR #104.
- **Remaining functionality:** None against password-change slice; forgotten-password delivery intentionally excluded.
- **Verification only:** Recorded API/browser/process/rollback evidence; broader matrix/platform gaps stay verification debt.
- **Dependencies/decision:** Recommend closure after reviewing evidence; rollout requires fresh login and old snapshots have documented cutoff.
- **Source:** [inspected contract/source](../../docs/adr/0012-password-change-session-revocation.md). **Dated evidence:** [password-change-session-revocation](../verification/password-change-session-revocation.md). **Merged evidence:** [PR #104](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/104).

## [#81: Define and verify a safe single-organisation deployment configuration](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/81)

**Fetched GitHub state: OPEN.**

**Recommendation: Partial.**

### Tracker acceptance criteria (verbatim)

- [ ] Production starts without dev seed credentials/default keys and fails safely on missing security settings.
- [ ] Persistent database/files survive restart; storage is private and outside public web roots.
- [ ] Structured logs/correlation and safe errors are useful without credentials, document contents or medical context.
- [ ] Document capacity/timeout/load limits and costs; no unsupported production/compliance claim or deployment performed by this issue's planning step.

### Current evidence and remaining work

- **Implemented:** Fail-safe signing/origin/proxy/private-storage config, readiness, offline migration and single-host SQLite runbook/local TLS verification; PR #107.
- **Remaining functionality:** First-admin command absent; no target hosting/operator deployment, production invitation provider or agreed capacity/cost envelope.
- **Verification only:** Trusted-proxy integration, actual volume/TLS/restart rehearsal, load and operational monitoring acceptance not demonstrated.
- **Dependencies/decision:** Keep open; next recommended bounded first-admin slice needs no paid host/legal rules.
- **Source:** [inspected contract/source](../../docs/operations/single-host-configuration.md). **Dated evidence:** [safe-deployment-configuration](../verification/safe-deployment-configuration.md). **Merged evidence:** [PR #107](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/107).

## [#82: Implement and rehearse consistent database and private-file backup/restore](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/82)

**Fetched GitHub state: OPEN.**

**Recommendation: Complete local tooling; partial operational acceptance.**

### Tracker acceptance criteria (verbatim)

- [ ] Restored profile/Identity/roles/versions/lifecycle/request/audit rows agree; inactive access stays disabled.
- [ ] Files and metadata are coherent, quarantined objects remain inaccessible and missing blobs are reported safely.
- [ ] Secrets/keys required for restore are secured separately; migration rollback losing lifecycle metadata is not routine.
- [ ] Drill records measured recovery time and unrecovered window rather than promising zero loss.

### Current evidence and remaining work

- **Implemented:** Operator encrypted package, cross-process stopped-writer coordination, isolated no-overwrite restore, integrity/file manifest, credential invalidation and reviewed recover-password; PR #108, document/image extensions #109/#119.
- **Remaining functionality:** None found in the implemented operator-only local tooling contract. Scheduled backup/retention automation is not part of that slice; company custody/retention/access procedure still needs approval.
- **Verification only:** Local database/document/image recovery exists; broader target-host/Linux, large-store and restored full-role browser rehearsal outstanding beyond the retained Linux image-recovery case. Historical 1.470s backup/0.922s restore and 2.580s post-cutoff window are local only.
- **Dependencies/decision:** Recommend closure of local tooling criteria after operator evidence review, or retain only an explicitly agreed operational acceptance remainder. Target-host/Linux/load permutations alone are verification gaps, not new functionality. Actual evidence/image blobs now have local recovery tests.
- **Source:** [inspected contract/source](../../docs/operations/backup-restore.md). **Dated evidence:** [verified-backup-restore](../verification/verified-backup-restore.md). **Merged evidence:** [PR #108](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/108), [PR #109](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/109), [PR #119](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/119).

## [#83: Add private supporting documents with quarantine and scoped access](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/83)

**Fetched GitHub state: OPEN.**

**Recommendation: Partial.**

### Tracker acceptance criteria (verbatim)

- [ ] Manager sees medical evidence status only; owner and explicitly authorised current HR get medical content. Ordinary documents use documented owner/current-scope/HR permissions.
- [ ] Cross-user upload binding/download, revoked/inactive access and quarantined content are denied without existence leakage.
- [ ] Stage file before DB binding, commit metadata/status, compensate or schedule cleanup on failure; transactions do not pretend database/blob writes are atomic.
- [ ] Retention/removal/legal hold and access auditing reuse existing conventions without competing transition audits; log actor/object/action/result, never content or diagnosis.
- [ ] Cancelled/failed uploads cannot bind; retry uses scoped idempotency, scan timeout/failure remains quarantined, safe download headers and limited previews.

### Current evidence and remaining work

- **Implemented:** Private PDF/JPEG/PNG, actual fail-closed ClamAV/quarantine, binding ownership/version checks, medical status-only Manager access, publication fencing/reconciliation and file-aware restore; PR #109.
- **Remaining functionality:** Approved bound-evidence retention/legal-hold/expiry administration absent. Draft removal exists; no automatic statutory retention invented.
- **Verification only:** Scanner/storage/HTTP/process/recovery evidence recorded; full outage/load/AT/platform fault matrices not exhausted.
- **Dependencies/decision:** Keep open for privacy-approved retention/hold decisions; no unapproved bound purge.
- **Source:** [inspected contract/source](../../docs/adr/0015-private-supporting-documents.md). **Dated evidence:** [private-supporting-documents](../verification/private-supporting-documents.md). **Merged evidence:** [PR #109](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/109).

## [#84: Configure request descriptions and evidence requirements with submission snapshots](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/84)

**Fetched GitHub state: OPEN.**

**Recommendation: Partial.**

### Tracker acceptance criteria (verbatim)

- [ ] Type selection shows correct fields and explanations; server rejects missing required descriptions/clean evidence independently of client state.
- [ ] Snapshot rule version/requirements at submission; later config changes affect future submissions but do not fabricate requirements/evidence on legacy records.
- [ ] Approval rechecks reviewed applicable entitlement and only the explicitly defined evidence/payment workflow; no silent retroactive upload demand.
- [ ] Preserve drafts on configuration/version conflicts, require explicit refresh, prevent duplicates and use session-scoped invalidation.
- [ ] Descriptions are bounded plain text, never requested diagnoses; manager medical access remains status-only.

### Current evidence and remaining work

- **Implemented:** Leave types authoritative for modes/class/instructions; immutable null/Unknown legacy snapshots; fresh type/policy versions; ordinary Required and descriptions, stale draft recovery; PR #110.
- **Remaining functionality:** Conditional supported statutory rules and proof/payment review absent. Required Medical is explicitly rejected server-side, not a missing hidden UI control.
- **Verification only:** Real form/row/concurrency/recovery checks recorded; broader evidence cases remain dated limits.
- **Dependencies/decision:** Keep open; #75/#22/#86 resolve absence versus proof/payment before enabling medical submission requirements.
- **Source:** [inspected contract/source](../../docs/adr/0016-request-evidence-requirements.md). **Dated evidence:** [request-evidence-requirements](../verification/request-evidence-requirements.md). **Merged evidence:** [PR #110](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/110).

## [#85: Deliver reviewed annual leave cycles and schedule-based accrual end to end](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/85)

**Fetched GitHub state: OPEN.**

**Recommendation: Blocked.**

### Tracker acceptance criteria (verbatim)

- [ ] Employment-anniversary cycles, 21 consecutive-day baseline/schedule equivalents and agreed 1/17 day/hour methods use explicit units and real qualifying inputs.
- [ ] Holiday and schedule boundaries, fractions/rounding and spanning-cycle allocation match approved examples; months never become constant day counts.
- [ ] Approved charges and rule/schedule references are stored as historical facts; future edits cannot silently recalculate them.
- [ ] Legacy incomplete inputs display unavailable/legacy basis with an explicit HR remediation path; no fabricated dates or audits.
- [ ] Pending reserves nothing unless separately re-scoped; competing approvals cannot exceed the reviewed allocation.

### Current evidence and remaining work

- **Implemented:** Employment facts/schedules and shared current calendar-day calculator are prerequisites only.
- **Remaining functionality:** Anniversary cycles, reviewed accrual methods, schedule/holiday/fraction boundaries and immutable historical charges absent.
- **Verification only:** No statutory calculation fixture approval or engine tests; do not retrofit guessed legacy values.
- **Dependencies/decision:** Qualified #75 and central #22 contract first; then implement category slice.
- **Source:** [inspected contract/source](../../docs/planning/04-south-african-leave-requirements.md).

## [#86: Deliver sick-leave cycles with conditional evidence and payment review](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/86)

**Fetched GitHub state: OPEN.**

**Recommendation: Blocked.**

### Tracker acceptance criteria (verbatim)

- [ ] Six-week schedule entitlement and first-cycle deduction avoid double charge; distinguish actual worked inputs from scheduled estimates.
- [ ] More-than-two consecutive days or more-than-two occasions/eight weeks trigger the reviewed employer-request workflow, including exceptions.
- [ ] Absence can be recorded when proof/pay eligibility is unresolved; unavailable data is visible, not silently zero.
- [ ] Medical access is owner/authorised HR only; safe notices and disputes keep managers out of raw content.
- [ ] Historical charges, reasons/audits and pending current-rule validation remain truthful and atomic.

### Current evidence and remaining work

- **Implemented:** Optional medical evidence/privacy and absence submission exist without statutory proof gate.
- **Remaining functionality:** Sick cycles/actual worked-day inputs, first-six-month rules and employer proof/payment review/disputes not implemented.
- **Verification only:** No approved sick-rule boundary tests.
- **Dependencies/decision:** Depends #75/#22/#78/#84; proof/payment is distinct from recording absence.
- **Source:** [inspected contract/source](../../docs/planning/04-south-african-leave-requirements.md).

## [#87: Deliver event-based family responsibility and company compassionate leave](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/87)

**Fetched GitHub state: OPEN.**

**Recommendation: Blocked.**

### Tracker acceptance criteria (verbatim)

- [ ] Longer-than-four-months and at-least-four-days/week boundaries, three statutory days and reviewed lapse/variation rules are explicit.
- [ ] Qualifying versus enhanced events are labelled without unnecessarily collecting relatives' personal details.
- [ ] Proof/payment and absence recording stay distinct; medical content is not revealed to managers.
- [ ] Cycle-spanning/part-day charge and historical rule snapshots agree across submission/approval/balance/report.

### Current evidence and remaining work

- **Implemented:** Generic company leave type configuration only.
- **Remaining functionality:** Reviewed family responsibility eligibility/events/cycles, separate compassionate enhancement and charge/payment contract absent.
- **Verification only:** No statutory family-event fixtures executed.
- **Dependencies/decision:** Depends #75/#22/#78/#84; minimise relatives information.
- **Source:** [inspected contract/source](../../docs/planning/04-south-african-leave-requirements.md).

## [#88: Deliver reviewed parental, adoption and commissioning leave cases](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/88)

**Fetched GitHub state: OPEN.**

**Recommendation: Blocked.**

### Tracker acceptance criteria (verbatim)

- [ ] Actual court order rather than media summary governs single/sole-employed versus shared allocation; do not hardcode calendar months as days.
- [ ] Recovery/fitness and miscarriage/stillbirth safeguards, continuity and reviewed disagreement deadlines are enforced without Gender being the sole eligibility input.
- [ ] Other parent's external employment/allocation is a minimised declaration with HR review, not claimed verified interoperable data.
- [ ] Policy/rule changes preserve charged history and case evidence; current pending validation shows applicable rule and any unresolved review.
- [ ] UI distinguishes time-off rights, employer pay and uncertain UIF benefits; no unsupported legal-compliance promise.

### Current evidence and remaining work

- **Implemented:** Generic types and optional evidence do not implement parental cases.
- **Remaining functionality:** Reviewed Van Wyk/interim allocation, calendar-month/recovery/notice/disagreement cases, pay/UIF distinctions absent.
- **Verification only:** No approved parental boundary engine or category tests.
- **Dependencies/decision:** External qualified review #75 before #22/category implementation; Gender is not eligibility.
- **Source:** [inspected contract/source](../../docs/planning/04-south-african-leave-requirements.md).

## [#89: Add HR CSV onboarding with mapping, preview and safe row confirmation](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/89)

**Fetched GitHub state: OPEN.**

**Recommendation: Partial.**

### Tracker acceptance criteria (verbatim)

- [ ] Use Identity email normalizer; detect duplicates within file and against profile/Identity NormalizedEmail/NormalizedUserName, including inactive/unlinked accounts and employee numbers.
- [ ] Validate departments, existing active Manager/same-department/cycle rules, full roles and employment/schedules; optional Gender neither required nor parental gate.
- [ ] Revalidate each row under writer reservation/current HR; per-row transaction commits all Identity/profile/roles/reporting or rolls back all. Other valid rows can succeed, no orphan accounts.
- [ ] Confirmation has actor-scoped idempotency and stable row outcomes; repeated requests do not re-create successes. Contention/failure rows require explicit retry after refreshed validation; imports race safely.
- [ ] Activation replaces passwords; no secrets in logs/previews/results. Formula-injection-safe downloads contain minimal authorised row errors; delete temporary upload/preview by documented retention.

### Current evidence and remaining work

- **Implemented:** CSV 1MiB/100 rows, parser/mapping/server preview, normalized duplicates incl inactive/unlinked, per-row creation/outcome atomicity/idempotency, cancellation, immutable schedule copies, formula-safe results, secure activation; PR #112.
- **Remaining functionality:** Production invitation channel absent. Nonblank Gender deliberately rejected, not silently discarded; template explains unsupported field.
- **Verification only:** Real mapping/partial/concurrency/recovery evidence recorded; additional interruption/volume/platform regressions remain.
- **Dependencies/decision:** Keep open until activation delivery dependency #79 resolved or tracker explicitly accepts Development-only scope.
- **Source:** [inspected contract/source](../../docs/adr/0018-hr-csv-onboarding.md). **Dated evidence:** [hr-csv-onboarding](../verification/hr-csv-onboarding.md). **Merged evidence:** [PR #112](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/112).

## [#90: Build an API-backed personal leave dashboard with explicit periods and units](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/90)

**Fetched GitHub state: OPEN.**

**Recommendation: Partial.**

### Tracker acceptance criteria (verbatim)

- [ ] Counts/dates/balances are real API data; Paid/unpaid/cycle/legacy units are labelled accurately.
- [ ] Pending and upcoming periods are documented, overlapping requests never imply unique absence metrics.
- [ ] Loading/empty/error/retry/refresh and accessible desktop/mobile links work.
- [ ] Account/session keys, cancellation and initiating-session invalidation prevent delayed leaks; entry refreshes.

### Current evidence and remaining work

- **Implemented:** Owner snapshot, shared balances, bounded Pending/upcoming/recent, SAST current/+90-day dates, real Overview and session refresh; PR #113.
- **Remaining functionality:** Cycle/paid/unpaid/statutory units depend on #85/#22/#75; current calendar labels are truthful, not annual allowance.
- **Verification only:** Recorded browser/rows/date/session evidence; no rerun in this review.
- **Dependencies/decision:** Keep parent open for calculation-dependent criteria; current-data slice shipped.
- **Source:** [inspected contract/source](../../src/HRFlow.Application/Features/LeaveRequests/Queries/GetPersonalDashboard/GetPersonalDashboardQuery.cs). **Dated evidence:** [personal-leave-dashboard](../verification/personal-leave-dashboard.md). **Merged evidence:** [PR #113](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/113).

## [#91: Build the Manager workspace and read-only team calendar from scoped data](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/91)

**Fetched GitHub state: OPEN.**

**Recommendation: Complete.**

### Tracker acceptance criteria (verbatim)

- [ ] Own requests appear in personal section but never eligible queue; current live Manager scope is authoritative.
- [ ] Inactive approved history is labelled and excluded from active coverage; count distinct active people per date when shown, unioning overlaps.
- [ ] Counts have explicit date periods/units; month navigation, retry/refresh and keyboard/mobile work.
- [ ] Reassignment/decisions refresh this session's views; delayed account responses cannot appear.

### Current evidence and remaining work

- **Implemented:** Personal plus scoped Manager attention/upcoming and read-only month/list, current reports, no self, inactive labels, distinct active daily counts, bounded 62-day range; PR #114.
- **Remaining functionality:** None against current-data calendar criteria. Staffing capacity/holiday calendar explicitly excluded.
- **Verification only:** Recorded month/leap/range/role/browser/session evidence; browser-clock defaults and receipt time limitations documented.
- **Dependencies/decision:** Recommend closure after evidence review; do not block this read-only calendar on statutory accrual.
- **Source:** [inspected contract/source](../../src/HRFlow.Client/src/features/manager-workspace/ManagerWorkspace.tsx). **Dated evidence:** [manager-workspace-calendar](../verification/manager-workspace-calendar.md). **Merged evidence:** [PR #114](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/114).

## [#92: Build the HR workspace with scoped monitoring trends and administrative attention](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/92)

**Fetched GitHub state: OPEN.**

**Recommendation: Partial.**

### Tracker acceptance criteria (verbatim)

- [ ] Selected period/request counts/distinct people/calendar durations/charged units are separate; inactive contributions and current-department attribution are explicit.
- [ ] Draft filters never relabel old results; refresh/loading/error/empty and table alternative work.
- [ ] Combined HR users get capability; revoked/inactive HR and non-HR direct URLs/API are denied.
- [ ] Administrative attention reflects real incomplete inputs or failed onboarding, not speculative notifications.

### Current evidence and remaining work

- **Implemented:** HR snapshot with all-date monitoring/employment/onboarding attention and selected-period clipped-day trends/department tables/charts, correct filters; PR #115.
- **Remaining functionality:** Preserved statutory charged-unit reporting unavailable until #22/#85/#75. Current request-days are not unique absence.
- **Verification only:** Recorded reconciliation/links/browser/session evidence; realistic volume, complete AT and platform checks remain.
- **Dependencies/decision:** Keep parent open for charged units; administrative current-data slice shipped.
- **Source:** [inspected contract/source](../../src/HRFlow.Application/Features/LeaveRequests/Queries/GetHrWorkspace/GetHrWorkspaceQuery.cs). **Dated evidence:** [hr-workspace](../verification/hr-workspace.md). **Merged evidence:** [PR #115](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/115).

## [#93: Deliver durable scoped in-app notifications from committed leave events](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/93)

**Fetched GitHub state: OPEN.**

**Recommendation: Complete.**

### Tracker acceptance criteria (verbatim)

- [ ] Only committed events deliver; late rollback delivers none and retries never duplicate a notification.
- [ ] Current active recipients can read only their own notifications; deep links recheck current scope and can safely be unavailable after reassignment.
- [ ] Medical content, diagnoses, raw deactivation reasons and note text do not appear in notification payloads/logs.
- [ ] Read/unread behaviour works across retries and sessions with bounded polling; general preferences remain #94; no automatic replay of leave decisions.

### Current evidence and remaining work

- **Implemented:** Atomic minimal outbox, idempotent multi-process recipient delivery, reassignment privacy, inbox/count/read and session-isolated UI; PR #105.
- **Remaining functionality:** No missing functional criterion found. Preferences shipped separately #94. Retention cleanup is unimplemented operational improvement with explicit storage-growth cost.
- **Verification only:** Recorded process/rollback/browser/rows evidence; broad regressions explicitly unexecuted, not assumed missing behavior.
- **Dependencies/decision:** Recommend closure for durable in-app slice; retention decision remains company-pilot work, not email expansion.
- **Source:** [inspected contract/source](../../docs/adr/0013-durable-in-app-notifications.md). **Dated evidence:** [in-app-leave-notifications](../verification/in-app-leave-notifications.md). **Merged evidence:** [PR #105](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/105).

## [#94: Add permitted self-service profile fields and working preferences](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/94)

**Fetched GitHub state: OPEN.**

**Recommendation: Complete (clarify security-notice wording).**

### Tracker acceptance criteria (verbatim)

- [ ] Only allowlisted fields are accepted server-side; unknown privilege fields cannot change employee management state.
- [ ] Stale edits conflict, inactive/revoked accounts fail and values are private/minimised.
- [ ] Theme actually applies with contrast/reduced-motion; notification toggles affect delivered categories but cannot hide mandatory security notices.
- [ ] Profile,password and preferences use session-scoped callbacks; no old-account settings/success after logout.

### Current evidence and remaining work

- **Implemented:** Own-account allowlisted preferred name/private phone, independent versions, System/Light/Dark and worker-time optional category suppression with no replay; PR #106.
- **Remaining functionality:** No mandatory security-notification channel exists. Criterion must not imply otherwise; toggles expose only implemented optional categories.
- **Verification only:** Recorded browser/theme/session/worker/migration evidence; exhaustive contrast/AT/device checks outstanding.
- **Dependencies/decision:** Recommend closure after wording clarification: cannot hide mandatory notices is vacuous today, not a fabricated new security feature.
- **Source:** [inspected contract/source](../../docs/adr/0014-own-profile-and-preferences.md). **Dated evidence:** [profile-and-preferences](../verification/profile-and-preferences.md). **Merged evidence:** [PR #106](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/106).

## [#95: Add safe private profile image upload and removal](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/95)

**Fetched GitHub state: OPEN.**

**Recommendation: Complete.**

### Tracker acceptance criteria (verbatim)

- [ ] Actual content/dimensions/decompression limits are checked; malformed/polyglot/oversize images cannot become public content.
- [ ] Cross-user bind/remove and inactive access fail; old blobs are cleaned after successful replacement with safe failure compensation.
- [ ] Images carry no EXIF/location data; medical retention/access is never reused.
- [ ] Session change cancels/hides upload completion and duplicate actions are blocked.

### Current evidence and remaining work

- **Implemented:** Private own image pipeline, bounded JPEG/PNG orientation/crop/re-encode/metadata removal, exact output scanning, protected version switch, permanent cross-process tombstones, cleanup and HR view-only thumbnails; PR #119.
- **Remaining functionality:** None found against implemented image criteria. No original/public image path. Marker compaction/retention not implemented and must never be unsafe.
- **Verification only:** Finalisation records 19 image tests, combined 12 browser checks, actual recovery and updated daily28153 definitions; exhaustive EXIF/malware/AT/Linux/load matrices outstanding.
- **Dependencies/decision:** Recommend closure after independent security review; missing exhaustive permutations are verification limits, not proof of missing functionality.
- **Source:** [inspected contract/source](../../docs/adr/0025-private-profile-images.md). **Dated evidence:** [private-profile-images](../verification/private-profile-images.md). **Merged evidence:** [PR #119](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/119).

## [#96: Extend existing leave reports with scoped formula-safe CSV exports](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/96)

**Fetched GitHub state: OPEN.**

**Recommendation: Partial.**

### Tracker acceptance criteria (verbatim)

- [ ] Exports repeat live current authorization, active lifecycle and existing scope/range filters; no arbitrary manager or unrestricted employee ID accepted.
- [ ] Request counts, distinct employees, summed calendar request-days and preserved charged units have separate headers/units; overlaps remain explicit.
- [ ] Empty/invalid/stale permission and interrupted downloads are safe; filenames and cells disclose no secrets or raw personnel context.
- [ ] Export access logs contain actor/filter identifiers/counts, not row payloads; results remain session-bound.

### Current evidence and remaining work

- **Implemented:** HR/Manager current scopes, applied period limits, fresh snapshot, bounded formula-protected CSV with Text: prefixes, cancellable session downloads; PR #116.
- **Remaining functionality:** Preserved statutory charged units absent pending #22.
- **Verification only:** Independent downloaded-byte parsing recorded; Excel/LibreOffice not exercised. Additional volume/interruption/platform cases remain.
- **Dependencies/decision:** Keep open for charged-unit contract; do not claim spreadsheet-app compatibility from parser tests.
- **Source:** [inspected contract/source](../../docs/adr/0022-scoped-current-data-report-exports.md). **Dated evidence:** [scoped-report-exports](../verification/scoped-report-exports.md). **Merged evidence:** [PR #116](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/116).

## [#97: Create a repeatable synthetic three-role pitch demonstration](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/97)

**Fetched GitHub state: OPEN.**

**Recommendation: Complete (current-feature pitch scope).**

### Tracker acceptance criteria (verbatim)

- [ ] Three-role walkthrough works without manual SQL fixes or page reloads; HR never decides and Manager cannot self-approve.
- [ ] Only synthetic names/addresses/documents are used; screenshots contain no secrets/local paths.
- [ ] Each metric's units/date period and legal/legacy limitations are spoken explicitly; no full production/compliance claim.
- [ ] Fixture restart cannot reactivate existing accounts or overwrite roles; source database is untouched.

### Current evidence and remaining work

- **Implemented:** Isolated refuse-existing synthetic fixture, real CSV/activation/ClamAV/decisions/history/calendar/reports walkthrough, restart/cleanup guards and truthful 8–10-minute speaking plan; PR #118.
- **Remaining functionality:** No current-feature walkthrough gap found; future statutory demonstration not delivered or claimed.
- **Verification only:** Fresh actual browser/persisted-state rehearsal recorded; human presentation untimed; retained hosted gates subsequently passed via #121, while broad AT/devices remain untested.
- **Dependencies/decision:** Recommend closure only for explicit current-feature pitch criteria; if umbrella intended statutory/production pitch, retain that portion under #98 rather than claim it delivered.
- **Source:** [inspected contract/source](../../docs/operations/pitch-rehearsal.md). **Dated evidence:** [pitch-rehearsal](../verification/pitch-rehearsal.md). **Merged evidence:** [PR #118](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/118).

## [#98: HRFlow South African workplace product roadmap](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/98)

**Fetched GitHub state: OPEN.**

**Recommendation: Partial umbrella.**

### Tracker acceptance criteria (verbatim)

- [ ] Review existing #17/#21/#22/#36 evidence before any closure or scope update.
- [ ] Approve legal requirements and unresolved interpretations before statutory calculation code.
- [ ] Complete dependency-ordered usable slices with explicit migration/privacy/history decisions.
- [ ] Retain actual HTTP/row/file/browser evidence and focused regression gates after the explicit test-convention decision; state limitations honestly.
- [ ] Demonstrate synthetic three-role workflows and operational restore readiness without claiming full legal/production compliance.

### Current evidence and remaining work

- **Implemented:** Dependency-ordered roadmap and merged usable slices through #119; security, shell, identity, private files, CSV, dashboards, exports, tests and current-feature pitch.
- **Remaining functionality:** Qualified rules, statutory engine/categories and pilot operations gates remain. Tracker embeds obsolete planning baseline and deferral statements.
- **Verification only:** Local dated evidence and #121 hosted 55/9 gates exist; earlier billing block is historical and external sign-off is not provided.
- **Dependencies/decision:** Keep open; replace stale body with proposed concise live umbrella, not copied historical roadmap.
- **Source:** [inspected contract/source](../../docs/planning/08-current-readiness-review.md). **Merged evidence:** [PR #119](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/119).
