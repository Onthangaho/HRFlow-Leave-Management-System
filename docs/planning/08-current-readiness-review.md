# Current readiness review — after merged PR #133

**21 October 2026.** Source baseline: clean fetched origin/main
59d20de0deb34ae20abf5b00fd43090edb2813a3; PR #133 confirmed merged at 21:25:12 UTC.
Branch docs/post-133-backlog-reconciliation. Unrelated worktrees/local artifacts preserved.

This is source/evidence review, not another feature or a test execution.
[All 29 issue contracts](09-issue-reconciliation.md),
[current inventory](06-feature-inventory-and-reconciliation.md),
[roadmap](05-product-roadmap.md) and
[unpublished tracker proposals/next three slices](10-proposed-tracker-updates.md) are consistent.
No GitHub issue edit/closure, board move, provider selection or reviewer contact. Documentation
publication is a separate authorised finalisation step; the proposed tracker updates remain unpublished.

## Evidence checked now versus earlier results

**Inspected now:** fetched issue states/bodies and merged PR metadata, source/control flow/DTOs,
retained assertions, ADRs/runbooks and dated reports. All 58 issues were searched; 29 roadmap items
were individually reconciled (28 OPEN, #78 CLOSED). Relevant PRs #120-#133 are merged.

**Existing result checked read-only:** final #133 head 8fc1fa0
[Ubuntu run 39676852702](https://github.com/Onthangaho/HRFlow-Leave-Management-System/actions/runs/39676852702)
succeeded: 115 backend, 51 Chromium, build/lint/audit/typecheck. These are actual hosted results,
not newly executed here and not inferred from a workflow file. No builds, product tests, package audits,
browser/network measurements, scanner updates or legal-source retrieval were performed in this review.

**Historical evidence preserved:**

- PR #119's billing-blocked job never ran; billing is not a current block.
- Original #120: 50 backend passes / 4 failures. #121 corrected fixture and production creation
  permissions without relaxing validators; final Ubuntu 55 backend / 9 Chromium passed.
- #125 original browser failure was not reconstructed. #126 confirmed separate synchronization/
  layout-settling gaps; this did not establish the original assertion or legal approval.
- #133's earlier local complete 111/112 failure and existing fixture bootstrap timeout remain
  unexplained. Cleanup gaps were corrected and safe diagnostics retained; neither contention nor
  a proven root-cause fix is claimed. Separate focused/local/hosted runs are not summed.
- Positive real-ClamAV and definition versions remain the dated local document/image/pitch evidence;
  default hosted gates do not become fresh positive scanning or target-host recovery evidence.

The earlier living review remains in merged
[PR #120](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/120) and Git history;
this update replaces its current-status recommendations rather than creating a competing roadmap.
Historical verification files were not rewritten. No green job substitutes for acceptance-criterion
inspection, missing product code, provider approval or qualified review.

## Pitch versus company-pilot matrix

| Area | Safe current synthetic demonstration | Remaining company-pilot gate |
|---|---|---|
| Personal/Manager/HR workflows | Real current-data requests, decisions/notes, history, monitoring/trends/exports | Owner accepts actual limits; use externally reconciled figures if evaluating without statutory authority |
| Calculations | Explain configured entitlement minus Approved inclusive calendar days; Pending reserves nothing | Qualified #75 and #22/#85-applicable category/cutover contracts before authoritative entitlement/pay use |
| Medical evidence | Optional private evidence and ordinary Required submission; Manager status-only | Reviewed absence/proof/payment workflow, minimum data, retention/holds. Required Medical stays unsupported |
| Onboarding | CSV/activation/explicit resend with private Development pickup; SMTP fixture synthetic-only | Approved real transport/sender/authentication/processor/operator under #79; no production pickup fallback |
| First administrator | Existing offline first-admin and normal local Production login evidence | Target-host credential custody/operator procedure. No need to implement bootstrap again |
| Security/CI | Retained current-role, session, migration, transaction and restored-authority gates; hosted Ubuntu/Chromium runs | Keep current-head gates and extend specific missing cases; no assumption every time/race/platform case is covered |
| Private data | Scanned evidence and processed images with fences/authorised downloads | Approved lawful purpose/class retention/hold administration, scanner operations, custody and incident response |
| Recovery | Real encrypted isolated restore, existing-HR recover-password, old credential/invitation denial, explicit fresh resend | Target-host restore/access/custody rehearsal, post-cutoff role/lifecycle reconciliation, realistic volume |
| Deployment | Local Production TLS/configuration/health/restart evidence; supported single-host local SQLite | Separately authorised real host, trusted proxy/TLS, private persistent volume, migration/backup/restore and load acceptance |
| Accessibility | Recorded desktop/mobile/keyboard, one-date Tab calendar and list alternative | Representative staff/screen-reader/physical-device/contrast assessment; no WCAG certification |
| Monitoring/support | Safe correlation, health/readiness, worker outcome logs and explicit failures | Named operator, thresholds/alerts, access/retention, scanner/backup/import/delivery response and capacity budget |

These gates concern a real employee-data pilot, not a synthetic pitch. A restricted evaluation must
still have privacy, access, custody/recovery and explicit calculation limitations. No readiness
percentage, legal certification, guaranteed RPO/RTO or production deployment is claimed.

## Functional state and owner decisions

### Activation coordination and restored-store safety

[AccountActivationService](../../src/HRFlow.Infrastructure/Services/Auth/AccountActivationService.cs)
commits generation/recipient authority under writer protection, performs bounded delivery outside it
and guards acknowledgement against replacement/recipient/lifecycle/expiry changes. Safe status
distinguishes PickupReady, Accepted, definite failure and uncertainty; Accepted is not mailbox delivery.
Creation/CSV success is not replayed on delivery failure. Explicit versioned resend rotates the token.
No raw token/body outbox exists.

[BackupRestoreService](../../src/HRFlow.Infrastructure/Services/Operations/BackupRestoreService.cs)
clears password/invitation hash/expiry/recipient/dispatch authority, rotates credentials, revokes refresh
and invalidates unfinished imports. [The retained drill](../verification/restored-activation-safety.md)
joins real encrypted tools, normal API/worker startup, row/HTTP/SMTP observations and stopped lineages.
Existing activated HR access uses recover-password, not first-admin; only explicit fresh resend sends.

**Still missing:** production adapter/authentication/provider integration. ADR 0029 D1-D5 remain
Pending/Unassigned and D6 release approval absent. Sender/domain, provider processing/privacy,
credential custody, operational owner and uncertain-resend policy must be approved. Default
Production delivery and Development pickup-in-Production remain denied.

### Calculation and evidence boundaries

[The calculator](../../src/HRFlow.Application/Features/LeaveRequests/LeaveBalanceCalculator.cs)
derives all Approved calendar spans from current policy. No cycle/accrual/holiday/worked-day provenance,
charge ledger, no-deduction unpaid mode or pay/UIF decision engine exists. Current policy edits can
change displayed balance; approval decisions/audits remain unchanged. Requirements snapshots do not
preserve charged units.

[Sixteen decisions](11-south-african-rule-decisions.md) remain unsigned and
[sixteen examples](12-south-african-rule-examples.md) Pending review. Missing authoritative retrievals
and unresolved Van Wyk/legislation/UIF reconciliation block relevant conclusions. No legal research
was refreshed here. Keep #22/#85-#88 blocked/partial, not completed by a seeded leave label.

Closed #78's **versioned holiday-source** criterion remains missing. Owner must allocate it explicitly,
not erase it from the backlog or infer holidays from schedules. Future employment dates do not delay
activated access; HR must see that limitation.

[Document binding](../../src/HRFlow.Application/Services/SupportingDocumentService.cs) is currently
called only by initial submission. There is no supplemental-binding endpoint in
[DocumentsController](../../src/HRFlow.Api/Controllers/DocumentsController.cs). Bound removal explicitly
refuses without approved retention policy. Draft cleanup, publication fencing and image removal do
not constitute bound-evidence retention/holds. Proposed Pending-only supplemental evidence is the
next independently useful slice; it is not full medical proof/payment review.

### Deployment, custody and operational dependencies

First-admin is implemented in PR #122, with one-time protected empty-store checks and generated private
fixtures proving HR-only login/denial boundaries. It is not an account recovery/second-admin command.
Prior readiness recommendations to build it are superseded.

Supported topology is one host with locking-compatible persistent local SQLite/private storage.
Database-wide writer serialization and current bounded contention apply. No multi-host network-file
SQLite, connection-string-only portability or selected/costed hosting is promised. Trusted target-host
proxy/TLS, volume permissions/restart/recovery, load, incident/monitoring ownership and real operator
custody remain unverified/undecided. Real deployment still requires separate authorisation.

In-app events/inbox, minimal import outcomes and publication fencing markers currently retain data
without general automatic retention cleanup. Required hold/class retention decisions are not supplied
by coding-agent research. Do not delete bound evidence or concurrency markers to improve storage.

## Performance and UX: superseded findings and remaining work

No new build or benchmark was run. These are dated local measurements plus current source facts,
not production capacity or an attribution study.

| Area | Earlier finding now addressed | Evidence and honest limits |
|---|---|---|
| Employee directory | Unbounded collection, per-row lookups, all mounted thumbnails | #123 bounded 25/default, 100/max, independent Manager search and viewport images; #124 excludes removed size-zero hints. Large-store payload 228,659 -> 18,548 bytes, warm median 385.04 -> 171.68ms. Small/first-call results did not consistently improve |
| Route loading | Eager feature/role imports, large main bundle | #127 main 610.88 -> 360.37kB raw (179.21 -> 112.55 gzip); latest #132 recorded main 360.42kB. Total packaged JS grew slightly. Cold role bytes fell, requests rose and every sampled readiness/secondary timing was slower |
| HR workforce attention | Employee/schedule materialization and per-employee activation queries | #131 handler SQL 60 -> 12 at 32 employees; 1,645 -> 12 at 1,007. Warm medians 34.91 -> 20.87ms and 519.36 -> 30.77ms. Small first-call median regressed 474.38 -> 503.62ms |
| Manager calendar | Every date in Tab order | #132 one visible date Tab stop, focus distinct from selection, bounded arrow/week movement and responsive focus repair; full screen-reader/device review not executed |

Sources/methods:
[directory measurements](../verification/bounded-employee-directory.md),
[client loading](../verification/client-route-loading.md),
[HR aggregation](../verification/hr-workspace-query-efficiency.md),
[calendar keyboard](../verification/team-calendar-keyboard.md).
Directory/HR response or SQL reductions are not a universal latency claim.
Route-loading initial encoded-body samples: Employee 177,661 -> 142,860 bytes (1 -> 6 requests),
Manager 177,661 -> 148,363 (1 -> 9), HR 280,665 -> 250,883 (3 -> 11).
These were one cold/warm sample per role, not LCP or statistically controlled results.
The old >500kB main warning was removed by splitting, not by increasing its threshold.
The separately recorded useAuth Fast Refresh lint warning remains historical/current reported debt,
not a newly executed lint result.

**Remaining source-supported performance limits:** personal balances still read owner Approved history;
62-day team dates do not cap matching row count; 366-day department reports and trend rows materialize
matches; HR import attention parses batches. Export row/byte limits do not bound all upstream work.
Combined Overview can fetch overlapping team ranges and poll multiple scoped queries. No arbitrary
truncation/cache weakening or unmeasured aggregate redesign is proposed in this review.

**Remaining UX verification:** full AT/contrast/physical-device checks, unsaved route-navigation policy
and expected-volume staff tasks. Calendar Today is browser-clock SAST; team receipt timestamps are
client receipt, not an authoritative server as-of. Source-visible small styling opportunities do not
justify prioritising dashboard decoration over core workflow/provider/calculation gaps.

## Next work and recommended prompt

1. **#83/#84 Pending supplemental evidence** — actionable with normal scope approval, no statutory or
   provider decision. Voluntary clean, owned, snapshot-compatible documents; preserve original request/
   audit/history and Medical privacy. Medium.
2. **#79 one approved transport** — valuable for real onboarding, blocked on ADR D1-D5 owner/provider/
   privacy decisions and later D6 release approval. Medium after a concrete transport contract.
3. **#22/#85 first approved calculation/history category** — highest correctness value but blocked on
   qualified/company/privacy decisions and #78 holiday-source allocation. Large, end to end.

[Scope, acceptance, verification and the exact next implementation prompt](10-proposed-tracker-updates.md)
are proposed only. Do not rebuild first-admin, add another dashboard or default to repeated testing
instead of an independently useful feature. Existing #76 gaps should be covered as relevant work lands.

## Documentation verification for this review

Only 12 existing Markdown/instruction files changed. Source/tracker/evidence inspection was performed
now; earlier test/build/scanner/measurement results are labelled above. Documentation checks passed:
260 relative links in changed files and two inbound links resolve; no local heading-fragment links
need validation in that set. UTF-8 decoding/replacement-marker, fence/EOF/trailing-whitespace and
sensitive-literal checks passed; both instruction files are byte-identical. All 29 issue sections
are unique and match fetched GitHub states; the finalisation recheck includes all 124 fetched
acceptance bullets, including multi-criterion issues. git diff --check passed, including the documentation-only
scope check. External URLs were not batch reachability-tested; relevant issues/PRs/run were fetched
read-only. Pattern scans are not a comprehensive secret-audit certification. Product source, schema,
dependencies, tests, CI and historical verification reports are unchanged. No builds/tests or legal
source revalidation were run in the documentation review. Finalisation repeats documentation-only
checks on the intended staged files before publication. The complete review patch is a local ignored
artifact, not a competing roadmap or a published tracker update.
