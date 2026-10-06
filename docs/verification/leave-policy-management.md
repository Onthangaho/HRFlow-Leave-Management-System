# Leave type/policy API verification — 6 October 2026

## Baseline and isolation

The working tree was clean on main. Origin was fetched, issue #23 was verified open
with matching title and scope, and `feat/leave-policy-management-api` was created from
origin/main at `44ece4e` (merged dependency-security PR #66) before edits.

Temporary C# tooling outside the repository made real authenticated HTTP calls to two
API processes on separate local ports, sharing a fresh disposable SQLite database. Each
request has its own DI scope/DbContext/connection. Independent DbContexts and raw SQLite
connections inspected persisted records. Existing application databases were not migrated
or written. Earlier disposable data was opened read-only and backed up for migration checks.
No repository automated test files, frontend feature code, credentials or verification
tooling were added. Temporary server logs, JSON evidence, databases and browser screenshots
remain outside the repository.

## Actual API and persisted-state results

- HR list/detail/create/update/delete worked for both resources; create returned 201
  with detail Location and DTO, updates 200, unused deletions 204, and missing details 404.
- Employee-only and Manager-only tokens were denied on list, detail, create, update
  and delete for both resources. Missing authentication returned 401. Management
  authorization denials carried useful ProblemDetails. Removing Identity HR membership
  blocked an existing HR JWT on every write path; membership was restored afterward.
- Empty/blank/overlong/control-containing names, negative entitlement, missing required
  fields, empty policy IDs and empty/missing edit versions failed with 400. Missing
  policy references failed with 404. Invalid updates preserved the previous version/data.
  Names were trimmed. Case/whitespace duplicates and Unicode invariant-case duplicates
  failed with 409; a failed duplicate rename left the original version unchanged.
- Shared-policy detail listed both linked types. Policy reduction changed both types'
  current entitlement/balances and blocked a subsequent oversized submission and a
  previously valid Pending approval. Failed approval remained Pending with no audit.
  Historical approved decision fields and audit rows compared equal before/after edits.
  Reducing entitlement from 20 to 3 after five approved days produced the unchanged
  calculator's -2 remaining days, rather than rewriting history or clamping balance.
- Increasing entitlement allowed a subsequent Pending approval. Enabling overlap
  allowed same-type overlapping approval; disabling it blocked a later overlapping
  submission. Zero entitlement blocked a one-day request. Pending still reserved no balance.
- Deletion of types referenced by Approved, Cancelled or Rejected requests returned 409.
  Linked-policy deletion returned 409. Unused type/policy deletion succeeded. Explicit
  type policy reassignment allowed deletion of its now-unused former policy.
- Stale type/policy edits and deletes returned 409. Simultaneous policy edits using the
  same original version produced one 200 and one 409, without overwriting the winner.

## Two-process concurrency evidence

Final full verification used five iterations of each race, with competing requests sent
to separate API processes and persisted rows inspected after both responses:

- Normalized duplicate creates: each iteration returned one 201 and one 409, with
  exactly one normalized-name row.
- Normalized duplicate renames of different types: five completed supplemental
  iterations returned one 200 and one 409; only one row used the target normalized name.
- Approval versus entitlement reduction (20 to 10, request for 15 days): four iterations
  returned approval 204/update 200 with Approved status and one audit. One returned
  approval 400/update 200 with Pending status and no audit. These are both legal serial
  orders: a later reduction does not revoke an earlier historical decision.
- Submission versus unused-type deletion: three iterations returned submission 200 /
  deletion 409 and retained one request and its type. Two returned submission 409 /
  deletion 204 and persisted neither a request nor its type. No orphan records remained.
- Holding a separate writer reservation forced a safe 409 in **3.03 seconds** in the
  final full run (3.13 seconds in the earlier run), with no inserted policy afterward.
  There is no application replay; no stale tracked entity or audit was reused.

A temporary late reference-insert trigger exercised the actual SQLite RESTRICT error
(extended code 1811): the API returned sanitized 409 and rolled back the inserted reference.
An unrelated temporary trigger constraint stayed on the sanitized 500 path and left
version/rules unchanged. Both triggers were removed. These probes also passed when the
configured connection string requested `Foreign Keys=False`; Infrastructure enforces true.

Development startup was restarted after deleting an unused default type/policy and
performing a case-only default-type rename. The deletion and rename survived, with no
duplicate/default resurrection. Final independent inspection found **22 policies,
43 types, 14 requests and 9 audits**, with no orphaned type/request references, and confirmed
the browser-created Pending request used Annual's persisted 20-day policy with no audit.
Extra disposable records include supplemental verification attempts; they are not seed data.

## Migration evidence

A read-only backup of an earlier disposable database at the preceding migration was
upgraded. It contained three types, three policies, two requests and two audits, plus
Identity/profile data. Every existing table row and original column compared equal
before/after; only new policy labels, normalized names and initial versions were added.
The inspected baseline had no invalid legacy names, negative balances or orphan references.

Separate disposable copies containing a normalized-name collision, blank type name,
negative entitlement and a valid Unicode type name each stopped in migration preflight
before schema/data changes, with a specific legacy-review message. The Unicode case is
an explicit migration limitation: SQLite cannot backfill invariant Unicode keys itself.
No existing type was silently renamed/deleted. A reviewed .NET backfill/remediation is
required for such legacy databases. Overlong/control-containing/orphan legacy preflight
branches were reviewed but not separately injected; no production/external database was
inventoried or migrated. See ADR 0004 for inspection queries and migration precautions.

## Browser and build evidence

The connected browser tool returned no available apps/browsers. Isolated headless Chromium
ran the real Vite client against the final disposable API:

- Browser login/logout passed; My leave rendered current balances and Pending history.
- The actual authenticated Axios module fetched the selector's `{id,name}` DTOs and
  submitted a one-day Annual request from the browser. API returned 200; Pending reserved
  no balance. The history/balance UI rendered it without document reload.
- No browser page errors or failed API responses occurred. Independent persisted-row
  inspection confirmed that request and its policy/status/audit outcome.

**UI limitation:** current App routes/source contain no leave submission form or type
selector component. Those interactive form workflows could not be executed; browser-side
Axios requests are not claimed as form verification. Restoring a submission UI or adding
the HR management UI is outside #23. Other browser engines, production load/deployment,
full accessibility review and all possible error/cancellation races were not executed.

Actual final commands/results:

- `dotnet build HRFlow.sln --no-restore`: succeeded, **0 warnings, 0 errors**.
  NuGet network restore initially failed; installed packages were restored using an
  explicit local package source. A later build hit running-API file locks; stopping those
  disposable processes and rebuilding resolved them. Final build used the restored cache.
- `npm run build`: succeeded, **225 modules**, JavaScript **450.12 kB / 138.22 kB gzip**.
- `npm run lint`: exit 0; only the existing `useAuth.tsx:184` Fast Refresh export warning.
- `git diff --check`: passed. Instruction files were checked identical.

SQLite's database-wide single writer, per-command timeout and synchronous native waits
remain throughput limitations. Another provider or filesystem requires separate evaluation.
Repository automated tests remain intentionally deferred. During implementation verification,
no commit, push, PR, merge,
issue closure or issue #24 work was performed.
