# Employee Edit investigation — 10 November 2026

Publication is paused on `feat/paginated-manager-approval-queue`. The original queue patch is preserved,
unstaged and uncommitted. This follow-up concerns an existing editor, own-avatar absence requests and
request-cancellation diagnostics, not a new administration workflow. No real employee store is used.

## Reproduction and confirmed cause

The new retained [browser reproduction](../../tests/browser/employee-editor.spec.ts) logs in through
real HTTP as synthetic HR, opens Employee management, scrolls to the last visible-page active row
and clicks Edit. Before correction: **rendered=1, visible=0, focused=0, consoleErrors=0,
correlations=11, detailReads=0**. The test failed its viewport assertion, 8.623 seconds;
complete invocation 28.537697 seconds, exit 1.

The handler sets the selected employee snapshot correctly. The editor is rendered **above** the
directory, leaving a below-fold caller looking at now-disabled row buttons. No overlay or browser
render exception was observed. Opening Edit intentionally uses the complete, versioned row DTO;
there is no initial detail GET. An explicit stale-conflict reload performs that GET. Adding an
automatic fresh detail read would unnecessarily change original-version/draft semantics.

The form now focuses its programmatically focusable heading and scrolls it into view on mount only.
Background directory/choice refreshes do not repeat this focus movement or replace the edit snapshot.
Save/Cancel returns focus to the opening control, with a New Employee fallback if it was removed.
Existing department/role/Manager loading and retry states remain. Explicit detail reload now announces
loading, blocks duplicate reload/save, retains conflict feedback after a failed reload and ignores
only its own aborted completion. Closing/unmounting still aborts its request.

The corrected focused HTTP observation returned **200** for department, role and Manager reads,
with **11** response correlation matches, **0** detail reads, **0** PUTs, **0** page errors and **8**
aborted browser requests. The editor was visible and focused despite those aborts. Thus cancelled
requests alone are not evidence that Edit failed. A generated safe `X-Correlation-ID` is sent by the
test and echoed by the actual API request-logging middleware; raw IDs and payloads are not printed.
The specific original terminal excerpt was supplied as a summary, not an available raw attachment;
its individual request IDs cannot be independently matched to these new fixture requests.

## Running checkout and private diagnostics

The real-browser fixture starts the regression executable/API binaries built in this checkout and
Vite with this checkout's client root. An explicit solution build succeeded before the drill.
During the drill, safe process inspection found two fixture API processes with intended-checkout
matches. This establishes the reproduced environment, not the provenance of a separately started
manual server from the earlier observation. No unrelated process is stopped.

The reporter allowlists fixed step/source labels, numeric viewport/read/correlation/status counts
only. Raw Playwright errors, page HTML, request bodies and screenshots/traces/video are not retained.
Credentials, response tokens and synthetic saved fields remain in disposable private fixtures only.

## Expected versus actionable terminal entries

The supplied excerpt summary has 93 responses with 200, ten with 499, five image-content 404s,
cancellation exceptions and some slow successful requests; no recorded 500 or employee-update PUT.
That summary does not establish the cause of every cancellation or exclude errors outside the excerpt.

- A 200 is a completed request. The existing 500 ms slow-request warning is a latency observation,
  not an update failure. It remains unchanged; no capacity claim follows.
- A 499 with `RequestAborted` set is an expected disconnected-client outcome. MVC already handles
  this case. The completion middleware previously warned for every 499; it now uses Information for
  confirmed client aborts. Its narrowly filtered catch also covers cancellation before MVC, including
  authentication's cancellable current-credential/role reads. It does not replay or complete a write.
  This is a server diagnostic; a disconnected browser need not receive an HTTP 499 response.
- An unexplained 499, cancellation without RequestAborted, a server error and other unexpected
  exceptions retain their existing warning/error/propagation behaviour. EF/JWT logging is not globally
  suppressed. The underlying cancellable validation and transaction code is unchanged. Tests inject
  boundary delegates; they do not prove the origin of the original JWT/EF stack traces.
- The header previously fetched own-image bytes even for authoritative `hasImage=false`. It now shares
  the existing session-scoped `/me/image` metadata query with Profile and fetches content only when
  present. Empty Current-row versions remain intact. HR directory hints and viewport loading remain.
- A declared-present image whose content fails still makes the authenticated request and shows a
  generic unavailable diagnostic with initials fallback; genuine missing-file errors are not converted
  into successful absence. A controlled inconsistent metadata/actual HTTP 404 test covers this branch,
  not a claim of a corrupt-file incident in the user's store.

## Separate executions

Commands use generated private fixtures and `HRFLOW_BROWSER_CHANNEL=msedge` on Windows. Browser retries
remain zero. Redirected Node SQLite ExperimentalWarning can be formatted by PowerShell as
NativeCommandError; native exit codes are captured separately.

```powershell
# Repository root
dotnet build HRFlow.sln --no-restore
dotnet test tests/HRFlow.RegressionTests --no-build --filter FullyQualifiedName~RequestCancellationLoggingTests --logger "console;verbosity=minimal"
dotnet test tests/HRFlow.RegressionTests --no-build --filter "FullyQualifiedName~ManagerQueueTests|FullyQualifiedName~StaleEmployee|FullyQualifiedName~RequestCancellationLoggingTests" --logger "console;verbosity=minimal"
dotnet test tests/HRFlow.RegressionTests --no-build --filter "FullyQualifiedName~ProfileImageTests|FullyQualifiedName~EmployeeDirectoryTests.ImageHintsDistinguishContentFromRetainedAbsenceVersions|FullyQualifiedName~EmployeeDirectoryTests.HttpAuthorizationAndBounds|FullyQualifiedName~RequestCancellationLoggingTests" --logger "console;verbosity=minimal"
# tests/browser
npm.cmd test -- employee-editor.spec.ts
npm.cmd test
npm.cmd run typecheck
# src/HRFlow.Client
npm.cmd run build
npm.cmd run lint
```

1. Baseline: **0/1**, 28.537697 seconds. Confirmed offscreen/no-focus defect, above.
2. Initial corrected single test: **1/1**, 27.921403 seconds, exit 0; visible/focused,
   12 correlation responses, no page errors/detail reads.
3. Expanded first run: **2/3**, 108.635803 seconds, exit 1. New mobile test tried hidden desktop
   navigation; timed out at 60.260 seconds. Changed the test to use the actual mobile drawer.
4. Next run: **2/3**, 22.579850 seconds, exit 1. The new test's unscoped Department selector matched
   both editor and directory-filter controls. Scoped to the New Employee region; assertions retained.
5. Corrected three-test run: **3/3**, 16.845258 seconds, exit 0. Real mobile keyboard create/save/edit/
   cancel; independent read-only SQLite checked saved FullName, normalized employee number, leap-day
   employment date, department and null Manager. Cancelled draft did not persist.
6. Cancellation boundary tests: **5/5**, 445 ms, exit 0. Confirmed-abort throw and completed-499 cases,
   unexpected cancellation propagation, unexplained499 warning and 500 error remain distinguished.
   The test project links the actual middleware source; no new dependency or alternative middleware.
7. Separate affected backend run: **15/15**, 48 seconds, exit 0: Manager queue filters/scope/concurrency,
   stale employee writes and the five cancellation classifications. This is not a complete backend run.
8. First complete browser run: **100/101**, 299.247947 seconds, exit 1. The below-fold test reached
   a visible, focused editor with successful reads, but incorrectly hardcoded person 0024. Earlier
   directory tests insert a row and shift page membership. Corrected the test to capture the selected
   button's original employee name and compare that exact snapshot; no product assertion was removed.
   All other cases, including the seven Manager queue tests and actual stale-conflict workflow, passed.
9. Final middleware-only rerun after explicit namespace imports: **5/5**, 59 ms, exit 0.
10. Fresh complete browser run on the final executable changes: **102/102**, **253.066639 seconds**,
    exit 0, zero failed/skipped/retries. Includes all six editor/avatar follow-up cases, seven Manager
    queue cases, real HR create/edit/resend/schedule/deactivation and stale conflicts, directory paging,
    light/dark responsive views, same-/different-account delayed responses, real bearer refresh,
    submission/history/evidence and Manager decisions. The below-fold probe reported successful
    department/role/Manager reads, zero page errors/PUTs and seven aborted requests. Actual mobile save
    used POST 201 / PUT 200; read-only SQLite assertions confirmed saved facts and discarded draft.
    Controlled role-response, 409/503 reload and inconsistent image-metadata cases remain explicitly
    controlled transport tests, not server-failure reproductions.
11. After browser fixture shutdown, separate affected image/directory/cancellation backend run:
    **25/25**, **30 seconds**, exit 0. Includes unchanged retained absence versions/lifecycle rows,
    read-only hint projections, current HR revocation/inactive/pending denials, raster processing,
    scanner-adverse/unavailable handling, publication/removal/restart safeguards and logging cases.
    Scanner protocol fixtures and outage checks do not prove positive real ClamAV scanning.

Solution builds: initial **20.47 seconds**, final **4.79 seconds**, both zero warnings/errors.
Client production build includes TypeScript and passed, Vite **7.72 seconds**, exit 0.
Client lint and browser typecheck passed; existing auth fast-refresh/Node SQLite warnings remain.
Final browser typecheck passed after the last test additions. No frontend executable changes followed
the successful client build/lint. The full backend suite was not rerun in this follow-up; the earlier
215-run remains separate. No migrations or dependencies changed; earlier model/audit results in the
queue report are not new executions here.

Final review: **26 changed/new files**, **59 relative links/anchors** checked, strict UTF-8, balanced
Markdown fences, tracked-diff/new-file whitespace and sensitive-pattern/manual source checks passed.
The existing tracked queue validator's inherited missing final newline is preserved, not a new
formatting regression. Instruction copies remain byte-identical. Nothing is staged. The complete
combined review patch includes new files; ignored local tooling/logs are excluded. Final numeric
process inspection after fixture cleanup found **0 API/browser fixture hosts**. No unrelated process
was terminated. Branch and HEAD are unchanged, and no files were staged, committed or published.

## Limitations

The previous queue report's 215-backend and 96-browser results remain earlier evidence, not new runs
of these corrections. No hosted/Linux, positive real-ClamAV, production-built browser, physical-device
or screen-reader result is claimed here. No schema, dependency, permission, API listening/origin or
calculation change. Original request-abort origins and individual original image 404s cannot be proven
from the supplied excerpt counts alone. No real store is reset or modified.

## Finalisation, 11 November 2026

Publication of the combined patch is now authorised; the pause and results above remain dated history.
Review covered all 26 intended files, including new query/selector handlers, tests, caller migration,
logging boundary and avatar state. Current fetched main matches the original branch base. References
#21 and #76 remain open; no issue closure or tracker edit is authorised.

One additional review correction makes editor-close focus recovery reject disconnected, disabled,
inert, aria-hidden and CSS-hidden targets. It falls back to the visible New Employee button or the
focusable employee-management introduction. Focus restoration only follows explicit Save/Cancel;
background refetch still neither focuses the editor nor resets its snapshot. A controlled layout test
hides the original controls and verifies the stable introduction receives focus in the viewport.

The new test initially failed browser typecheck because Playwright's element type also permits SVG;
using the equivalent `setAttribute('hidden', '')` corrects this without changing the DOM scenario.
Typecheck then passed. Frontend build passed (Vite **6.09 seconds**), and lint passed with the existing
auth fast-refresh warning. No backend source changed during this finalisation; the earlier **215/215
complete backend**, separate **15/15** and **25/25 affected** executions and **102/102 browser** execution
remain distinct. Affected backend runs are not a new full-backend result.

Separate complete browser invocation during finalisation: **103/103 passed**, **322.353905 seconds**,
exit 0, zero failures/skips/retries. This includes the additional hidden-target regression. The new
test's type-only DOM attribute correction was made during that invocation before its case executed;
the test then passed, as did browser typecheck. No production code changed during the run. This is
additional evidence, not a relabelling of the earlier 102-case execution. Command from `tests/browser`:
`HRFLOW_BROWSER_CHANNEL=msedge npm.cmd test` (set the environment variable using PowerShell `$env:`).

The queue changes the Manager GET response from array to envelope. API and client must deploy together;
all known production consumers and retained tests use `items`/count/page metadata. The historical issue
creation script merely describes the route, and pitch tooling navigates the UI. The measurement test's
array branch is solely for running the same measurement against old binaries, not an API fallback.
HR monitoring retains its separate unchanged array contract. No schema/dependency/CI/calculation change.

Hosted checks will be inspected on the published commit and reported on the PR. Local real-ClamAV,
production-built browser, screen-reader/device and target-host checks are not new evidence here.
Original individual cancellation/image-404 origins and the old manual server checkout remain uncertain.
