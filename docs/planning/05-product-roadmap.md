# HRFlow current roadmap — after merged PR #133

**21 October 2026**, fetched main 59d20de0deb34ae20abf5b00fd43090edb2813a3.
Umbrella [#98](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/98) is still open.
This living roadmap supersedes the initial future-work checklist, not its dated verification records.
[All 29 issue contracts and classifications](09-issue-reconciliation.md),
[current inventory](06-feature-inventory-and-reconciliation.md),
[pitch/pilot gates](08-current-readiness-review.md) and
[unpublished tracker proposals/next slices](10-proposed-tracker-updates.md) share this baseline.

This is documentation/source review only. No product changes, new builds/tests, benchmarks,
scanner update, legal-source revalidation, issue edits/closures or board moves. Documentation
publication is separately authorised; tracker proposals remain unpublished.

## What is already delivered

- Current JWT expiry/skew/signature and live role/credential checks; guarded refresh and
  account/session-scoped caches. Own password change ends prior credentials/sessions.
- Shared responsive role-aware shell, lazy feature routes and additive personal/Manager/HR workspaces.
  Calendar keyboard focus is now roving, with selection independent from focus.
- Versioned HR employee/policy management, deactivation, confirmed employment facts and weekly
  schedule history; bounded directory/search/Manager lookup and viewport-loaded private thumbnails.
- Optional Manager decision notes, immutable transition audits and authorised timelines.
- Secure local activation, CSV mapping/preview/atomic per-row confirmation and explicit resend.
  Generation/recipient/eligibility-aware delivery coordination is shipped; production transport is not.
- Private scanned supporting documents, Medical status-only Manager projections, truthful
  submission requirement snapshots, private processed profile images, profile/preferences.
- Durable recipient-scoped in-app notifications, safe current-data reports/exports and a repeatable
  synthetic pitch. No organisation-wide email/SMS notifications.
- Single-host configuration guards, offline first-admin, encrypted operator backup/isolated restore,
  reviewed existing-HR recovery, document/image integrity and retained restored activation safety.
- Maintained xUnit/Playwright gates are authorised and running. Blanket test deferral is obsolete.

The implementation/evidence mapping below is authoritative; shipped slices do not complete every parent.

## Current issue disposition

| Disposition | Existing issues | Meaning |
|---|---|---|
| Complete, closure review recommended | #21, #36, #74, #77, #80, #82, #91, #93, #94, #95, #97 | Functional criteria supported by source plus dated evidence; caveats/wording changes in reconciliation |
| Partial | #17, #22, #76, #78, #79, #81, #83, #84, #89, #90, #92, #96, #98 | Exact missing functionality or tracker-contract remainder retained |
| Blocked | #75, #85, #86, #87, #88 | External decisions/prerequisites unresolved; category implementation is not started |
| Not started | None as a whole issue | Do not mistake Blocked categories or already delivered slices for untouched work |

These are classifications, not issue state changes or readiness percentages.
#78 is already CLOSED; its **versioned holiday-source criterion remains unimplemented**.
Only the owner can approve transferring it explicitly to #22/#85 or correcting the closed scope.

## Dependency order for unfinished work

### Can be proposed now without statutory decisions

The first recommended implementation is a **bounded owner supplemental-evidence workflow for an
existing Pending request**, within #83 and consistent with #84 snapshots. There is no such binding
endpoint/UI today: only initial submission can bind drafts. Reuse the current scanner, publication,
medical privacy and request protection. Do not add pay eligibility, retrospective demands, bound
removal or retention rules. [Acceptance and a complete prompt](10-proposed-tracker-updates.md).

This is an explicitly proposed next slice, not an assertion it was already accepted or implemented.
Owner approval to implement is still required; no qualified legal interpretation or provider purchase
is needed for this voluntary, snapshot-compatible Pending-only attachment workflow.

### Owner/provider decisions before real onboarding

#79 already has coordination/status/recovery; do not rebuild activation. ADR 0029 D1-D5
(provider/relay or API, sender/domain, authentication/custody, processing/privacy/operator and
uncertain-resend policy) remain Pending/Unassigned; D6 release approval is also unsupplied.
Then implement one approved production adapter, verify local transport and separately authorise
real provider/domain tests. #89 production creation/resend stays blocked until that channel exists.
Development pickup and the test-only loopback fixture are never production fallbacks.

### Qualified rules before authoritative balances/categories

1. #75: resolve missing authority and sign the applicable labour-law/company/privacy decisions.
   Sixteen decisions are unsigned and sixteen examples Pending review. Agent research is not approval.
2. Existing #22: approved central inputs/results, immutable approval charges and explicit legacy cutover.
   Include the closed #78 holiday-source remainder as an owner-approved allocation, not invented history.
3. #85 annual, then applicable #86 sick, #87 family/company enhancements and #88 parental cases,
   each as an approved end-to-end category. No gender-only eligibility or fixed-day calendar months.
4. Finish #84 conditional proof/payment-versus-absence and #90/#92/#96 charged/cycle outputs using the
   same reviewed results. Current-day dashboards/exports remain usable within their labelled model.

Do not infer employment dates, schedules, entitlements or historical charges from account creation,
current policies or later revisions. No retrospective recalculation of approved history.

### Privacy and target-host operation

- #83/P01/P02: approve class-specific retention/holds and implement the agreed bound-evidence
  administration. No statutory period or bound purge is invented. Current draft cleanup is different.
- #81: choose/authorise a supported host and operator; verify real proxy/TLS/local persistent volume,
  restart, migration, monitoring and expected-volume acceptance. Offline first-admin is already shipped.
- #82: operator tooling criteria are supported; target-host recovery/custody/access rehearsal remains
  mandatory before relying on real data, under #81/#98 even if #82 is closed after review.
- In-app event/inbox records and publication fencing markers currently grow without automatic
  retention cleanup. Storage budgets/legal holds are owner decisions, not a permission to delete.
- #76: extend specific missing retained boundaries, especially real expiry integration. Do not make
  repeated planning/testing the default next feature while independent core workflow work is available.

## Immutable constraints

Employee/Manager personal access remains owner-derived. Manager decisions are for eligible current
same-department reports, never self. HR administers/monitors and never approves/rejects. Combined
roles are additive. Current active/activated Identity and credentials are checked server-side.
Medical content is owner/authorised-HR only; Manager sees status. API/DTO/log privacy remains authoritative.

Reuse one deferred authorization/data snapshot for reads and SQLite writer reservation before write
validation, original edit versions, atomic AuditEntry/outbox persistence and session-isolated callbacks.
Single host with persistent locking-compatible local SQLite storage is supported; network SQLite,
multi-host writes and connection-string-only provider portability are not promised.

## Evidence and present calculation model

Current balance is current configured entitlement minus all Approved inclusive calendar days,
per type. Pending reserves nothing. No cycles/accrual/holiday deductions, immutable historical charge
ledger, explicit no-deduction unpaid mode or employer-pay/UIF determination exists. Requests,
people and summed calendar request-days are separate metrics; overlaps are not unique absence.

Final #133 [hosted Ubuntu run](https://github.com/Onthangaho/HRFlow-Leave-Management-System/actions/runs/39676852702)
passed 115 backend / 51 Chromium tests. This review checked that existing run, not reran it.
Earlier billing blocks, original #120 50/4 and #121 corrected 55/9 remain historical.
[Restored-store evidence](../verification/restored-activation-safety.md) preserves the earlier local
111/112 failure and unexplained timeouts; cleanup corrections are not proven root-cause fixes.
Green gates do not establish provider delivery, target-host readiness, positive real-ClamAV today
or qualified legal compliance.

Historical 8 October issue catalogue and discovery plans are intent/history, not current instructions.
No issue is closed or edited by this roadmap. The [proposed #98 replacement](10-proposed-tracker-updates.md)
must be reviewed and separately authorised before publication.
