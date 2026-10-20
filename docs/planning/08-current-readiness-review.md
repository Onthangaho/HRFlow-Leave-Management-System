# Current readiness review — 15 October 2026

## South African rule review pack — 20 October 2026

PR #124 is merged. This documentation-only pass refreshes [requirements/source status](04-south-african-leave-requirements.md), adds an [unsigned reviewer register](11-south-african-rule-decisions.md), [pending worked examples](12-south-african-rule-examples.md) and [#22 implementation proposal](13-calculation-foundation-review.md). It does not remove the qualified-law/company/privacy gates below. No reviewer is assigned; missing current instruments, allocation/age transitions, worked-time provenance, precision and historical opening decisions still block statutory enablement. #78 remains Closed in GitHub, with its unimplemented approved holiday-source criterion explicitly carried forward. Source inspection/research and documentation checks are new; product tests/builds and prior CI evidence were not rerun in this pass. See [verification and retrieval limitations](../verification/south-african-rule-review.md).

## Bounded-directory follow-up — 20 October 2026

PR #122 is merged; first-admin implementation is no longer a missing feature, while its target-host and production-delivery gates remain. The focused directory/thumbnail finding below now has a bounded implementation for review: [contract](../adr/0027-bounded-employee-directory.md), [measured before/after and retained tests](../verification/bounded-employee-directory.md). At 1,007 synthetic profiles, the default response fell from 716,061 bytes/all rows to 18,548 bytes/25 rows; the local warm median fell from 385.04 to 171.68 ms. Small-store and first-call timings did not improve. These are local observations, not load capacity. The old source findings remain the dated baseline; HR aggregate/report and other unbounded reads remain outstanding. Issue search found no dedicated pagination item; none was created/edited and #77 is not declared complete.


## Offline first-administrator follow-up — 15 October 2026

PR #120 is merged. The #81 empty-store operator bootstrap is now implemented locally for review; see the [runbook](../operations/offline-first-administrator.md) and [new execution evidence](../verification/offline-first-administrator.md). This supersedes the first-admin implementation recommendation below, not the historical review evidence. Production delivery and target-host deployment/operational gates remain open. No tracker proposals were published, and #81 remains open.


## Current CI update after merged PR #121

PR #121 merged 15 October 2026 18:57:01 UTC as `5d6a9bd56c434705b2a7eee8b52b1d8fd3df1a93`.
The original review baseline below is retained; PR #120 is now updated from that fetched main.
Earlier billing blocks are historical. Original #120 run 38781690040 produced **50 backend passes /
4 failures**. #121 corrected fixture private-directory/database permissions and production snapshot/
image/archive-directory creation permissions, preserving validators and assertions. Its final
[Ubuntu run](https://github.com/Onthangaho/HRFlow-Leave-Management-System/actions/runs/38786228861) passed **55 backend and 9 Chromium tests**.
Positive real-ClamAV coverage remains separate local evidence. This proves retained Linux gates,
including the tested image recovery path, not comprehensive target-host deployment/scanner/recovery.
See [permission-fix evidence](../verification/linux-fixture-permissions.md). The new #120 run is
independent and must be inspected after this update; prior #121 success is not its result.

## Original review baseline and evidence discipline

Clean tree inspected; fetched `origin/main`; created `docs/current-readiness-review` at
`1c660c10a9dda3f8870b78292fd3d8ffe0433008`. [PR #119](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/119)
is MERGED, 14 October 2026 23:41 UTC (15 October 01:41 SAST). Of 29 requested tracker items, 28 were OPEN and #78 was CLOSED when read. No issue/board changes, product changes, builds or application tests in this review.

This review inspected source, current issue bodies, merged PR metadata, ADRs and dated reports.
[Issue-by-issue criteria and recommendations](09-issue-reconciliation.md) preserve the fetched
acceptance baseline, not a reconstructed original issue edit history. [Proposed tracker updates](10-proposed-tracker-updates.md)
are drafts only. Historical reports retain their original dates and limitations.

Current permissions remain additive: Employee/Manager personal access; Manager decisions only
for eligible current same-department reports, never self; HR administration/monitoring without
decision authority. Credential/current-role checks, deferred read snapshots, SQLite writer
reservations, original edit versions and immutable transition audits must survive all future work.

## Pitch versus company-pilot readiness matrix

| Area | Safe synthetic pitch today | Company-pilot gate | Evidence / disposition |
|---|---|---|---|
| Personal, Manager and HR workflows | Demonstrate current-data dashboards, decisions/notes, timelines and exports | Company-approved scope and acceptance of actual calculation basis | PRs #102, #110–#116; current role enforcement is shipped |
| Leave calculations | Explain inclusive calendar days, current configured balance minus all approved days, Pending reserves nothing | Mandatory before using HRFlow as authoritative statutory entitlement/pay record: qualified #75, central #22, applicable #85–#88 | No cycles/accrual, holiday deductions or preserved charged-day ledger; partial parent issues stay open |
| Absence and medical evidence | Optional private evidence, Manager status-only; ordinary Required evidence | Mandatory before medical proof/payment decisions: reviewed #86/#84 workflow | Required Medical rejected; absence must remain recordable; no diagnoses requested |
| Account onboarding | Synthetic CSV → private Development pickup → activation | Mandatory for fresh real installation: first-admin provisioning and approved production delivery | #81/#79/#89; no production pickup workaround |
| Security/regressions | Local plus retained hosted Ubuntu/Chromium gates | Retain green gates on current PR; extend uncovered boundaries and target-host checks | #119 billing historical; #120 original 50/4; merged #121 final 55 backend / 9 Chromium passed |
| Private data | Demonstrate synthetic clean/quarantined evidence and processed profile images | Mandatory: approved privacy/access/retention/legal-hold decisions, scanner operations, custody and incident process | #83/#95 local functionality; no approved bound-evidence purge or legal-hold administration |
| Recovery | Isolated authenticated encrypted restore with credential invalidation | Mandatory: deployer rehearses restore and authorised access recovery on target host | #82 plus document/image extensions; not a zero-loss promise |
| Deployment | Local Production-mode checks are evidence only | Mandatory: real TLS/proxy/storage/permissions/backup/operator rehearsal | #81; single host, local SQLite locking, no network file/multi-host promise |
| Accessibility and volume | Recorded desktop/mobile/keyboard synthetic flows | Mandatory: representative staff task/assistive-technology review and expected-volume acceptance | Comprehensive audit/load absent; do not infer capacity from build success |
| Monitoring/support | Correlation, safe errors, liveness/readiness exist | Mandatory: named operator, alerts/log access/retention, scanner/backup/worker failure response | Health does not prove business integrity; SLOs/thresholds undecided |

“Mandatory” refers to a real employee-data pilot relying on these workflows, not a synthetic
presentation. A tightly bounded evaluation may use externally verified leave figures, but must not
claim statutory/payroll authority or bypass privacy, access, recovery and deployment safeguards.
Cosmetic polish, extra chart types, more dashboard cards and exhaustive platform permutations are
improvements, not automatically missing functionality. No readiness percentage or certification.

## Company-pilot blockers inspected

- **Qualified requirements:** [research baseline](04-south-african-leave-requirements.md) is dated
  8 October, explicitly unsigned. No qualified sign-off/decision owner record found. #75 remains
  blocked on an external reviewer; this review does not revalidate legislation or issue legal advice.
- **Calculation foundation:** [calculator](../../src/HRFlow.Application/Features/LeaveRequests/LeaveBalanceCalculator.cs)
  sums approved inclusive calendar durations against current policy balance. Employment facts and
  weekly revisions exist, but not statutory cycles, holiday sources, accrual or historical charged
  units. #22/#85–#88 are functional gaps, not merely unrun tests. Current policy changes can change
  displayed balances without rewriting historical decisions.
- **Provisioning/delivery:** [configuration runbook](../operations/single-host-configuration.md)
  explicitly names first-admin tooling as unimplemented. [delivery implementation](../../src/HRFlow.Infrastructure/Services/Auth/DevelopmentActivationDelivery.cs)
  is Development-only. An approved production adapter is absent; onboarding correctly fails closed.
- **Retention:** no approved evidence retention period, bound-content expiry/legal-hold workflow,
  or scheduled backup/notification retention cleanup. Draft cleanup is not a legal policy.
  Image Removed rows/markers remain indefinitely to fence delayed publishers; never purge casually.
- **Recovery:** [runbook](../operations/backup-restore.md) supplies offline `recover-password` for
  a reviewed active/activated existing identity whose password was cleared. This is implemented
  recovery access, **not** fresh-store first-admin creation. Restore can resurrect historical roles;
  permissions must be reconciled before workers restart. No parallel original/recovered production lineage.
- **Target-host operation:** local TLS login/refresh/password change and untrusted-header tests
  were recorded in #81; trusted proxy chain, actual persistent volume permissions, realistic load,
  broader Linux/target-host recovery and production access recovery remain unexecuted gates beyond the retained image-recovery case. No vendor or
  cost commitment is inferred. SQLite writers serialize database-wide with protected three-second
  contention handling; provider replacement needs more than a connection string.

## Performance and UX findings

No new build, network trace, browser interaction, benchmark or accessibility audit was run here.
The following are source facts or explicitly dated measurements, not load-capacity findings.

1. **Bundle growth (recorded builds):** shell report records 498.43 → 501.74 kB main
   (150.10 → 151.73 kB gzip). Image report records **605.66 kB / 177.94 kB gzip**.
   Relative to the post-shell result that is +103.92 kB raw / +26.21 kB gzip. The existing ignored
   local artifact inspected now is 605,664 bytes, timestamp 14 October 22:25:45 UTC; it is not a
   fresh main build. Vite's >500-kB warning persists. [App](../../src/HRFlow.Client/src/App.tsx)
   eagerly imports most routes; reports and HR charts are already lazy. Route splitting may reduce
   initial download/parse work; no attribution or slow-device timing has been measured.
2. **Directory/thumbnail fan-out:** [directory query](../../src/HRFlow.Application/Features/Employees/Queries/GetEmployees/GetEmployeesQuery.cs)
   loads all matching employees plus **all** schedule revisions, even for detail. Each row calls
   activation status and role lookup. [table](../../src/HRFlow.Client/src/features/employees/components/EmployeeTable.tsx)
   mounts an authenticated avatar for each displayed employee; no image-presence projection or
   viewport loading. Thus N visible distinct rows can initiate N content queries, including 404
   absence checks; TanStack deduplicates identical keys, not distinct employee IDs. This is a source
   request-count model, not a measured HTTP trace. Pagination, bulk projection and presence hints
   deserve a focused future slice; never replace private images with public URLs.
3. **HR snapshot work:** [HR projection](../../src/HRFlow.Application/Features/LeaveRequests/Queries/GetHrWorkspace/GetHrWorkspaceQuery.cs)
   loads all employees/schedule dates, calls activation lookup per active employee, and separately
   reads report/trend rows. Previews are bounded but aggregate inputs are not paginated. Use SQL
   aggregation/batched lookups where equivalent; preserve a single deferred authorization/data snapshot.
4. **Personal/team reads:** personal balance needs all owner Approved requests and current types;
   previews are bounded. Team range is 62 days but matching requests are not response-row bounded.
   HR reports cap dates at 366 days, not dataset cardinality. Export byte/row limits do not bound
   all upstream report materialization. Measure representative cardinalities before capacity claims.
5. **Combined Overview:** current source composes personal, manager attention, upcoming team,
   calendar and HR queries. Most visible-page data refreshes every 60 seconds; notification counts
   also poll independently. Range-distinct team keys correctly prevent stale relabelling, but can
   fetch overlapping data twice. Profile/header share query keys rather than fetching the HR directory.
   Inspect query counts/latency with synthetic volumes before consolidating; preserve account/session keys.
6. **Concrete UX debt:** employee summary text contains a literal `?` separator between number/start;
   profile workspace links are plain links rather than shared action styling; generic repeated image
   `alt="Profile"` text could be redundant in named directory rows. These are source-supported polish
   candidates, not claims that screen-reader behavior failed. Desktop calendar exposes every date as
   a separate Tab stop; a roving-grid enhancement could reduce keyboard burden, but its list/date
   alternative already works. Full WCAG contrast/AT conformance is unverified, not assumed failed.
7. **Documented behavior limits:** team “Today” depends on browser time converted to SAST; receipt
   timestamps are client receipt, not server snapshots. Route navigation unmounts forms; no universal
   unsaved-route blocker is claimed. Image tests cover explicit stale recovery, Cancel focus/Escape
   restoration and mobile dark flow; this is not exhaustive keyboard/AT coverage of every dialog.

## Prioritised next work

| Rank | Existing issue / focused slice | Risk/value; dependency | Effort | Acceptance and verification |
|---|---|---|---|---|
| 1 | #81 offline first-admin provisioning | Removes a concrete fresh-install blocker; no legal or paid service dependency | Medium | One-time empty-store only, explicit department, Identity validators, no defaults/role repair; two-process refusal, rollback, no secrets and real login/current HR checks |
| 2 | #75 qualified decision register | Unblocks truthful entitlement work; external reviewer required | Medium review | Named reviewer, operative sources/dates, applicability and approved boundary cases; unresolved rules remain blocked |
| 3 | #76 extend release gates | Adds missing expiry/refresh and broader target-platform evidence; existing hosted Ubuntu gates now pass | Medium | Retain missing negative boundaries and current-head green CI; no weakened assertions or CI bypass |
| 4 | #83/#82 privacy/recovery operations | Protects real medical/employee data; HR/privacy retention decisions first | Large | Approved holds/retention and operator restore/access drill; no unapproved purge, reconcile metadata/blob/security outcomes |
| 5 | #79 production invitation adapter | Needed for real onboarding; provider/custody/cost decision required | Medium | Approved delivery, durable failure/retry, no raw secrets, no DB lock during external calls; no purchase in current phase |
| 6 | #77/#76 bounded directory and measured client work | Removes known fan-out / helps usable scale; define expected volume first | Medium | Paginated current-HR snapshots, scoped thumbnails, retained drafts/session checks, measured request/byte/latency before/after and keyboard regression |

After #75, implement #22 then #85 and applicable #86–#88 with approved historical charge/migration
contracts. Do not start annual math merely because employment dates now exist. #90/#92/#96 remain
honest current-data slices until those units exist. These ranks are implementation order/risk, not
promised delivery dates. No new issue needed for the selected next slice.

## One recommended next implementation prompt

> Implement only the first-administrator provisioning slice of existing #81. Inspect current main,
> instructions, single-host operations, offline recovery tooling and retained tests. Branch from clean
> fetched main; preserve unrelated work. Add an operator-only offline command, no HTTP endpoint, that
> refuses nonempty Identity/profile stores and unsafe paths, verifies all API/worker writers are stopped,
> and requires explicit initial department plus a privately supplied generated credential. Use current
> Identity validators and one protected transaction to create exactly one activated active HR/profile;
> do not restore roles, reactivate, overwrite or repair existing accounts. No default passwords or secrets
> in arguments/logs/results. Define interrupted/repeated/concurrent invocation behavior and secure
> credential custody. Reuse existing operator infrastructure; retain separate-process duplicate and late
> rollback tests, then demonstrate normal login, current HR access and HR decision denial using a
> disposable store. Keep production invitation delivery unavailable. Document migration/recovery
> distinctions and run applicable retained checks. No purchase, deployment, commit/push/PR or issue
> closure unless separately authorised. Keep #81 open for target-host/proxy/load gates.

## Checks in this documentation review

Source/tracker inspection, branch/base/merge confirmation, existing artifact byte/stat inspection,
documentation relative-link/fence/whitespace/sensitive-literal checks and instruction byte comparison.
Documentation checks passed across 14 changed/new Markdown files: all 240 relative link targets
exist, fenced blocks/newlines/trailing whitespace/encoding and sensitive-literal scans passed.
No local heading-fragment links were present in the changed documents; external URL reachability
was not batch-tested. Both instruction files are byte-identical. `git diff --check` and separate
untracked-file whitespace checks passed. A complete review patch includes all three new documents
and remains an ignored local review artifact. Product source, schema, dependencies, tests and dated
verification reports are unchanged. At the initial review capture nothing was staged, committed,
pushed or published; finalisation below prepares only these documents for independent PR review. No new builds, tests, audits,
ClamAV updates, legal-source revalidation or browser/load results are claimed.

## Independent-review finalisation

Rechecked documentation/source references, the complete changed/new-file inventory and fetched
tracker states. The #78 holiday-source remainder is explicitly preserved despite its CLOSED state;
no tracker proposal was published. At that original review, first-admin provisioning was a recommendation, not code; the dated follow-up above supersedes that implementation status.
Documentation links/formatting/whitespace/sensitive-literal checks and instruction-byte comparison
were rerun for this finalisation. Product builds/tests/audits were not rerun because executable
files did not change. Earlier dated test results remain earlier evidence.
