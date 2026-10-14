# Scoped report exports: executed verification

14 October 2026, feat/scoped-report-exports, base fdb89fdffcb00019b12be79628d619a58ad7ad9b.
PR #115 was confirmed merged (14 October 2026, 15:01:09 UTC). #96 is a partial current-data slice;
#22-dependent charged-unit work remains unimplemented. The checks below were recorded during implementation before PR finalisation.

## Environment and method

Independent disposable synthetic store created with Node SQLite-aware backup of an earlier synthetic
fixture, plus separate private storage. Original stores were unchanged. Real TLS API and Vite client,
installed Chrome controlled by temporary Playwright, not static rendering. Temporary harnesses,
CSV downloads and secrets remained ignored/private. No automated test files or product test framework.

Downloaded files were independently parsed using csv-parse 6.1.0 installed only in temporary tooling.
Python was unavailable; the initial parsing attempt failed and was replaced, not claimed as successful.
Excel/LibreOffice were not exercised. Disposable fixture TLS verification bypass was confined to
verification clients; product TLS/configuration was unchanged.

## API, actual bytes and persisted rows

- HR department and Manager team 200; combined account allowed both. Employee denied both 403,
  HR-only team denied, Manager-only HR denied. Old HR/Manager JWT after current role removal 403;
  inactive or RequiresActivation persisted state 401; synthetic states restored after checks.
- Stale credential-generation JWT denied 401. Reassignment removes former report from next export.
- Actual downloads have UTF-8 BOM, quoted CRLF CSV, generated safe filenames, attachment, no-store,
  nosniff. Independent parser retains commas, doubled quotes and newlines in one name cell.
- HR Total and Department rows match all seven metrics in fresh existing report projections;
  department sums equal Total. Independent SQLite clipping/distinct employee/inactive sums match.
  Fixture contains overlapping and month-spanning Approved requests; summed days are not unique absence.
- Team rows match the fresh authorised projection, request ordering/IDs, complete dates, Approved status
  and Active/Inactive historical labels. Existing/zero-result department filters exported successfully.
- Missing/malformed/reversed dates 400; leap-year 366 HR days accepted, 367 rejected; 62 team days
  accepted, 63 rejected; unknown department 404. Empty team contains Metadata only; empty period HR
  retains Total/zero departments. Metadata retains range, UTC generation start and explicit semantics.
- Names injected only into disposable records: =,+,-,@, leading whitespace/tab/CR/LF,
  control/zero-width characters, full-width equals, commas, quotes and embedded newline. Every returned
  untrusted name is exactly `Text: ` plus the original value. No quoting-only formula protection.
- 5,001 synthetic Approved rows return 400 with 5,000-row guidance. 5,000 rows exceeding 2 MiB
  return 400 with byte-limit guidance, not partial CSV. Extra fixture rows removed afterward.
- Request/decision/audit contents before and after API checks are SHA-256-equivalent over sorted
  persisted rows. SQLite integrity_check is ok; foreign_key_check returns no violations.
- A separate Node SQLite connection/process held BEGIN IMMEDIATE with an uncommitted manager removal.
  API export succeeded using previous committed reporting scope; after commit the next export excluded
  that report. This demonstrates read behavior without a writer reservation, not exhaustive race scheduling.

## Real browser interactions

- HR exported actual applied 1-31 October dates while an unapplied start-date draft was 10 October.
  Downloaded bytes and filename were inspected and parsed independently. Draft was not used.
- Two click calls while a real export response was held generated one GET. Cancel aborted the held
  request and announced explicit retry. Synthetic 503 gave Retry export; only explicit retry downloaded.
- Held real successful export responses across logout/different-account login and same-account relogin
  produced no old download or success notice in the replacement session.
- Export 401 produced one request, no replay and redirected to login (injected HTTP boundary response).
- Manager navigated calendar month and downloaded its displayed range including adjacent-month dates;
  metadata matches request range and remains within 62 days. Calendar stays read-only.
- Desktop 1366x1000 and mobile 390x844 in Light/Dark: no page-level overflow; export keyboard focus,
  announcements and existing filter/calendar controls usable. Reduced-motion preference enabled.
- Employee navigation still shows personal dashboard and has no report/team export menus.
- Existing dashboards, report Apply/Reset/theme navigation and team-month navigation exercised;
  no leave mutation was needed for exporting. Request/decision/audit records were not changed by exports.

The first browser locator exposed missing explicit region semantics on the export wrapper. It was
fixed to role=region and the full browser sequence passed. No static rendering substitutes.

## Builds, model, audit and review

- dotnet build HRFlow.sln --no-restore: passed, 0 warnings / 0 errors, final rebuild 8.29s (earlier successful build 5.79s) after correcting the
  initial DomainException namespace error (that first build failed, not a passing check).
- Final npm build after accessible-region change: passed, 831 modules, 1.54s. Main 597.95 kB /
  175.88 kB gzip. Existing Vite >500 kB warning remains; no unrelated optimization/dependency changes.
- Final npm lint: exit 0; existing useAuth.tsx:223 Fast Refresh component-export warning remains.
- EF pending-model check: no changes since last migration, using a separate offline snapshot.
  Initial invocation without the required explicit connection was safely rejected, then corrected.
- Solution vulnerability check: all five projects report no vulnerable packages in configured feeds.
  Client npm audit: zero in every severity, 193 audited dependencies. Product dependencies unchanged.
- Working/new-file whitespace, instruction-byte, scope and sensitive-content checks recorded below.

## Synthetic screenshots

Each image contains synthetic account data only, no browser address bar, credentials, tokens, raw
CSV rows or local paths. Captured through real interactions; screen counts are capture-time data.

- [HR desktop Light](screenshots/scoped-report-exports/hr-desktop-light.png) / [Dark](screenshots/scoped-report-exports/hr-desktop-dark.png)
- [HR mobile Light](screenshots/scoped-report-exports/hr-mobile-light.png) / [Dark](screenshots/scoped-report-exports/hr-mobile-dark.png)
- [Team desktop Light](screenshots/scoped-report-exports/team-desktop-light.png) / [Dark](screenshots/scoped-report-exports/team-desktop-dark.png)
- [Team mobile Light](screenshots/scoped-report-exports/team-mobile-light.png) / [Dark](screenshots/scoped-report-exports/team-mobile-dark.png)

## Explicit unexecuted checks and limits

Excel/LibreOffice/import-resave behavior, full assistive-technology/contrast matrix, tablet screenshot,
actual mid-byte transport interruption, load/large-organisation/Linux/deployment, exhaustive revocation
versus in-flight snapshot scheduling, complete onboarding/activation/password/document/decision
regressions and a new backup drill are unexecuted. Cancellation and network failure were exercised at
held-response/injected HTTP boundaries, not a physical connection-loss drill. No legal-compliance or
production readiness claim. Output limits do not bound reused projection database work. A completed
browser download cannot be recalled on later logout. #22 calculations and #96 completion stay open.

See [ADR/API/CSV contract](../adr/0022-scoped-current-data-report-exports.md).

## Implementation review

Explicit invariant date/filename formatting was added during review; backend rebuilt and all affected API/CSV checks plus credential/filter/separate-writer checks passed again. The immediate first post-start harness attempt encountered connection refusal before the disposable API was ready; the subsequent ready-server runs above passed. Frontend did not change after its final build/lint and full browser run. Complete diff includes new source/docs and eight visually reviewed synthetic PNGs. Instruction files are byte-identical; whitespace, UTF-8, sensitive-content and scope scans passed. Original/source fixture request and audit rows match the final disposable store. Implementation ended without staging, committing, pushing, opening a PR, merging or deploying.

## Independent PR finalisation

Reviewed all intended source/documentation/new files and eight synthetic screenshots against the current-data #96 scope. No further product changes were required. Earlier backend/frontend/model/audit/API/SQLite/Chrome results above were not rerun during finalisation. Finalisation reran working/staged whitespace checks, instruction byte equality, exact intended-file staging and sensitive-content checks. Private CSV downloads, databases, secrets, logs, temporary tooling and generated output are excluded. Excel/LibreOffice and the other outstanding checks remain unexecuted; #22-dependent charged units remain outstanding. Prepared for independent PR review, not merged or deployed.

The first staged whitespace check caught an extra blank line at the new writer file EOF; it was removed and the staged check rerun cleanly. This formatting correction does not change runtime behavior and required no build/browser rerun.
