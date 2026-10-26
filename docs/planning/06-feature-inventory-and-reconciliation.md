> **22 October 2026 local implementation update:** PR #134 is merged. Its owner supplemental-evidence slice is authorised and implemented locally for review on feat/pending-request-evidence. [Design/API/migration](../adr/0032-pending-request-supplemental-evidence.md), [executed checks and limits](../verification/pending-request-evidence.md). Pending-only captured requirements, atomic five-file binding/idempotency and separate addition trace; no retention, statutory/payment or delivery expansion. Historical reconciliation and unpublished tracker proposals below remain dated evidence, not an issue update.

# Current feature inventory — after merged PR #133

**21 October 2026.** Fetched main 59d20de0deb34ae20abf5b00fd43090edb2813a3.
This updates the living inventory; dated verification reports are unchanged.
[Issue criteria/status](09-issue-reconciliation.md), [roadmap](05-product-roadmap.md) and
[readiness/remaining work](08-current-readiness-review.md) distinguish feature delivery from release gates.
No product tests, benchmarks, legal-source checks or scanner runs were executed during this review.

## Shipped capabilities and their boundaries

| Capability | Confirmed current behaviour | Exact boundary / evidence |
|---|---|---|
| Account access | Expiring signed JWT, 30-second skew, current role/credential checks, active/activated gate, same-session bounded refresh | [JWT inventory/evidence](../verification/jwt-current-permissions.md); no persistent browser sessions |
| Decisions/history | Current eligible Manager, no self/HR decision, current balance/overlap checks, optional 500-character note, atomic transition audit/outbox, scoped timeline | [Decision notes](../verification/manager-decision-notes.md); no audit edit API |
| Shell and loading | Stable providers/shell, responsive additive navigation, lazy routes/workspaces, module error recovery | [Route loading](../verification/client-route-loading.md); smaller entry is not universal faster navigation |
| Employment/directory | Confirmed unique numbers/start dates, Unknown legacy facts, weekly revisions, versioned HR changes; paged search and independent Manager lookup | [Employment](../verification/employment-data-foundation.md), [directory](../verification/bounded-employee-directory.md); future start does not defer activated access; no holiday-source engine |
| Activation/import | Passwordless create, hash-only invitation, explicit resend, per-row CSV idempotency and shared creation core, server-held preview and preview PII cleanup | [Coordination](../verification/activation-delivery-coordination.md), [CSV](../verification/hr-csv-onboarding.md); production transport disabled; nonblank Gender rejected explicitly |
| Password/profile/preferences | Own Identity password change revokes old credentials; allowlisted preferred name/private phone, separate versions, theme and delivery-time optional preferences | [Password](../verification/password-change-session-revocation.md), [preferences](../verification/profile-and-preferences.md); no HR password editing or mandatory security-notification channel |
| Supporting evidence | Private PDF/JPEG/PNG, real fail-closed ClamAV adapter, quarantine, clean-byte binding and publication fences, scoped content/access audit | [Documents](../verification/private-supporting-documents.md); initial submission plus local Pending owner additions for review; draft-only removal, no approved bound retention/holds |
| Requirements | Leave type modes/class/instructions authoritative, bounded plain text, immutable submission snapshot, version-conflict recovery | [Requirements](../verification/request-evidence-requirements.md); legacy Unknown, Required Medical rejected; no statutory conditions/pay review |
| Images | Private processed/scanned 256px image, own mutations/current HR reads, versions, removal markers/reconciliation | [Images](../verification/private-profile-images.md); no originals/public URLs; [removed rows omit thumbnail hints](../verification/removed-profile-image-hints.md) |
| Notifications | Atomic minimal outbox and idempotent worker, recipient inbox/count/read, scoped deep links, future delivery suppression | [In-app](../verification/in-app-leave-notifications.md); no email/SMS or automatic retention cleanup |
| Personal workspace | Owner balances/Pending/upcoming/history, explicit SAST periods/units, refresh/session guards | [Personal](../verification/personal-leave-dashboard.md); configured balance minus Approved calendar days, not statutory annual accrual |
| Manager workspace | Eligible queue attention, current reports, inactive history labels, distinct active people per date, calendar/list/export | [Workspace](../verification/manager-workspace-calendar.md), [keyboard](../verification/team-calendar-keyboard.md); one date Tab stop, focus separate from selection; browser-clock today is not server authority |
| HR workspace | All-date operational attention and period trends/reports; fresh current-HR snapshot, aggregated employee/schedule/activation attention | [Aggregation](../verification/hr-workspace-query-efficiency.md); fixed attention query count, report/trend/import cardinalities remain |
| Exports | Applied fresh scope, 366/62-day limits, counts/people/calendar-days separated, formula-safe Text: fields, row/byte rejection, session-safe download | [Exports](../verification/scoped-report-exports.md); no preserved statutory charge units; Excel/LibreOffice not exercised |
| Operations | Fail-safe single-host configuration, offline first-admin, encrypted isolated recovery, existing-identity password recovery | [First-admin](../operations/offline-first-administrator.md), [backup/restore](../operations/backup-restore.md); neither is production invitation delivery |
| Recovery safety | Real encrypted tool, sequential original/restored lineages, invalidated old authority over HTTP, no automatic dispatch/CSV recreation, explicit fresh resend | [Retained drill](../verification/restored-activation-safety.md), PR #133; no target-host guarantee |
| Tests/demo | Maintained xUnit/Playwright, private migrated stores, real/controlled cases labelled, synthetic real-ClamAV demo/runbook | [Tests](../../tests/README.md), [pitch](../operations/pitch-rehearsal.md); blanket test deferral is superseded |

## Incomplete or blocked functionality

- #22: effective-dated reviewed calculations, cycle/allocation inputs, fractional units, holiday sources,
  approval charges and truthful cutover. Current computed totals do not store historical debits.
- #75: 16 unsigned decisions and 16 Pending review examples. Missing authoritative retrieval/reconciliation
  is a blocker, not evidence of approved law. #85-#88 category implementations are blocked/not started.
- Closed #78 still lacks the holiday-source criterion. Only confirmed facts/fixed schedules shipped.
- #79: approved production provider/authentication/sender/processing/operational ownership absent.
  Shipped coordination does not supply transport. Test SMTP and Development pickup are not fallbacks.
- #83/#84: later Pending evidence attachment is locally implemented for review; retention/holds and reviewed conditional
  evidence/payment-versus-absence workflows remain separate gaps. No bound-evidence auto-deletion.
- #89: production onboarding depends on #79; optional nonblank Gender support is not implemented.
  Existing explicit rejection must be accepted in the tracker or separately scoped, not hidden.
- #90/#92/#96: cycles/paid-unpaid/preserved charge-unit completion depends on #75/#22.
- #76: some real token expiry/refresh boundaries lack maintained fixtures; controlled 401/refresh
  tests are not equivalent. Broader target-host/AT/scanner/load checks are verification, not new UI.

## Current versus historical evidence

Current source was inspected now. Existing final #133 hosted Ubuntu 115 backend / 51 Chromium result
was checked read-only. No application build/test/audit was run now. Earlier report failures and dates
remain intact: billing is historical; original #120 50/4, #121 55/9 and #133's unresolved local timeout
are not rewritten. Source safety plus focused evidence supports closure recommendations; a green job
alone does not complete a legal/operational parent.

The original PR #73 inventory is available in Git history and the dated reports/ADRs. Its
ValidateLifetime=false, initial-password creation, no retained tests, unbounded directory and
many-Tab calendar findings are superseded by the implementation links above, not current regressions.
This file and the roadmap are living summaries, not a new competing planning document.
