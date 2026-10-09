# Employment-data foundation verification

9 October 2026. Branch feat/employment-data-foundation; base ae9a27dad5c6fd4205872048dc9ed8e65a56a2a7.
PR #110 confirmed merged at 2026-10-09T20:05:29Z. Initial working tree was clean; branch created from
fetched origin/main. Scope is #78 confirmed facts and fixed weekly schedules only, not full closure.

## Executed migration and persisted-state checks

A new disposable synthetic store/private-file copy was used; no existing database was reset or
removed. Offline migration preserved original columns/rows in all 19 existing tables exactly.
Employee facts were all null; no schedule rows were invented. SQLite integrity_check was ok and
foreign_key_check empty. Model check reports no changes since the migration.

Actual API calls and SQLite inspection verified:
- Legacy profile-only edit retained Unknown number/date, complete roles and manager.
- Employee/Manager direct HR edit/schedule reads denied 403; own projections for Employee, Manager
  and HR returned 200 with explicit Unknown fields/history. Combined Employee/Manager/HR profile
  and HR schedule read also returned 200.
- Space-padded/case variants normalized to the same uppercase number. A valid 2024-02-29 persisted;
  invalid 2025-02-29, malformed and out-of-bound dates failed 400. Future 2027-01-01 remained a fact
  and the already-activated Manager still accessed Profile; no invented access gate.
- Two API processes sharing SQLite competed to create distinct accounts with the same normalized
  number: one 201, one 409, exactly one profile/account. The resulting inactive number could not
  be reused. Two-process duplicate-number updates likewise yielded one 200 and one 409.
- Stale employment version failed 409. Concurrent same-record requests (one API, separate scoped
  operations) using the original version yielded one 200 and one 409.
- Schedule append persisted exact integer minutes, stable IDs and UTC metadata. Empty/all-zero,
  negative, over-720 and fractional-minute inputs failed 400. Duplicate dates and old stream
  versions failed 409. Earlier revision remained exactly unchanged after later append.
- Two API processes appended the same loaded schedule generation: one 200, one 409, one row.
- Final contract check: missing expectedVersion failed 400 with no schedule row; explicit zero
  remains the valid empty-stream generation.
- Injected Identity UPDATE failure returned deliberate 500 with full employee/name/facts/version
  and Identity row rollback. Injected schedule INSERT failure returned deliberate 500 with no new
  revision and unchanged ScheduleVersion. Triggers were temporary, removed from the fixture.
- Current direct-report Manager role/department safeguards, self-assignment and unknown-role
  rejection retained their existing 400 behaviour. HR still cannot approve: 403.
- A separate SQLite connection held writer reservation and revoked HR membership while HTTP edit
  competed; after commit the request was denied 403 and target row matched exactly. Fixture role
  mapping was restored explicitly for continued disposable verification.
- Existing inclusive one-calendar-day submission and assigned-manager approval succeeded with
  exactly one Submit and one Approve audit. An existing clean owned document bound and downloaded
  through the real authenticated endpoint; no new ClamAV scan/outage matrix was run.
- HR employment/profile update left the dedicated private self-profile version unchanged.
- Process restart preserved inactive state, numbers/start dates and revision history.

## Actual browser interactions

Real Chrome through temporary Playwright used the TLS development client/API and synthetic records;
not static rendering. HR created a pending-activation employee via the real form with a normalized
number and future leap date, edited it and searched by its number. SQLite rows and linked Identity
state were inspected. HR saved a weekly pattern (7.5 hours = 450 minutes), tried a duplicate effective
date, retained input after 409, cancelled the discard confirmation and then explicitly reloaded.
A real stale HR edit retained entered number/date until explicit Reload latest values adopted the
fresh snapshot. Inactive details showed read-only schedule history with no append controls.

Employee, HR-only and Manager own Profile interactions showed read-only confirmed/Unknown facts.
Different-account switching removed previous schedule content. Combined Employee/Manager/HR Profile
and HR editing worked in Light theme; Employee/HR Dark desktop/mobile screenshots were captured.
Checked field keyboard focus, reduced-motion setting and settled desktop/mobile widths (1366x900,
390x844) without page overflow. Screenshots represent their capture-time synthetic records.

Screenshot review found delayed department choices could display a placeholder in an existing
edit. Binding the selector to its current React Hook Form value fixed it without rebasing drafts
on background refetch. Fresh-session browser verification then confirmed the saved department.
Several temporary harness attempts failed due to full reload of the memory-only session, ambiguous
link selectors, asynchronous reload/startup timing and hidden option visibility. Corrected interactive
runs passed; those failed attempts are not counted as product passes. A fixture copy initially lacked
restricted private-storage ACLs; correcting only the disposable ACLs enabled document regression.

## Encrypted recovery

All fixture API/worker processes were stopped before using existing operator backup/restore tools.
Restored Employees (including facts/versions), WeeklyScheduleRevisions, request descriptions/snapshots,
supporting-document metadata/access rows and clean/quarantine bytes matched snapshot values exactly.
Restored integrity/foreign keys passed. Missing required blob refused backup without publishing a
package. Original store remained separate; subsequent edits are beyond the snapshot cutoff.

Inspected all restored Identity accounts: passwords cleared, credential generations changed,
activation hashes cleared, and every outstanding refresh token revoked. Role mappings matched,
inactive state remained inactive. No invitation sent, account reactivated or role repaired. Full
restored API/browser permission matrix and operator access recovery were NOT rerun in this slice;
follow the existing recovery runbook. This is local evidence, not production/Linux/load proof.

## Final executed build/audit checks

- Backend build HRFlow.sln --no-restore: succeeded, 0 warnings, 0 errors (final rerun).
- Model check: no changes have been made to the model since the last migration.
- Frontend build after selector fix: 824 modules; main JS 550.30 kB (164.38 kB gzip), passed.
- Frontend lint: exit 0 with existing useAuth.tsx:223 Fast Refresh export warning.
- Existing Vite >500 kB chunk warning remains. No dependency upgrades/optimisations added.
- Solution transitive vulnerability check: no vulnerable packages in all five projects with current
  configured feeds. npm audit: 0 vulnerabilities. No package or lockfile changes.
- One intermediate backend build was blocked by fixture-process DLL locks (30 warnings/6 copy
  errors); stopping only those fixture APIs and rerunning produced the clean final result above.
- Working diff/new-text whitespace, sensitive-content review and byte-identical instructions checked
  after documentation completion. No automated test files/framework added; #76 convention unchanged.

## Synthetic screenshots

Reviewed screenshots contain synthetic identifiers only, no passwords/tokens/private phone data,
activation URLs or local machine paths. Each PNG is below 300 kB.

- [HR desktop, dark](screenshots/employment-data-foundation/hr-desktop.png)
- [HR mobile, dark](screenshots/employment-data-foundation/hr-mobile.png)
- [Employee Profile desktop](screenshots/employment-data-foundation/profile-desktop.png)
- [Employee Profile mobile](screenshots/employment-data-foundation/profile-mobile.png)
- [Combined Profile mobile, light](screenshots/employment-data-foundation/combined-profile-light-mobile.png)
- [HR employment/schedule form desktop, light](screenshots/employment-data-foundation/hr-light-desktop.png)

## Explicit outstanding checks/scope

Not executed: rollback/re-upgrade; exhaustive inactive/pending/stale-credential access combinations
for the new routes; concurrent deactivation versus schedule append; all last-HR/cycle/department
regressions; delayed profile/schedule completions across same-account relogin; measured duplicate-click
or network-outcome browser trials; comprehensive screen-reader/contrast/keyboard-dialog matrix;
new upload/scanner-failure regressions; full notifications/reports/activation/password regressions;
full restored access/provisioning workflow; Linux, production, load and power-loss drills.

#75 qualified statutory specification, #22 shared calculations, cycles, accrual, public holidays,
workday deductions, historical charged-day conversion and #78 remaining configuration are unimplemented.
No legal compliance claim, CSV onboarding, rotating/overnight schedule support, deployment or issue
closure. #78 remains open. See [ADR](../adr/0017-confirmed-employment-data.md) and
[contract/operations](../operations/employment-data-foundation.md).

## Independent-review finalisation

Finalisation review corrected an XML comment: weekly revision storage uses minutes, not hours.
No runtime behaviour changed during finalisation. Backend build was rerun with `dotnet build
HRFlow.sln --no-restore`: succeeded, zero warnings/errors. The EF pending-model check was rerun
against a disposable connection: no changes since the last migration. Issue #78 title/scope was
confirmed; it remains open. Working-tree whitespace checks passed.

The API, concurrency, browser, recovery, frontend and vulnerability results above are earlier
implementation verification, not finalisation reruns. Their outstanding checks remain outstanding.
Staging is restricted to reviewed source, migration, documentation and synthetic screenshots;
private fixtures, databases, documents, logs, credentials and temporary tools are excluded.
