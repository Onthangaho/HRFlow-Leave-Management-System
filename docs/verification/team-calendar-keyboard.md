# Manager team calendar keyboard: executed verification

21 October 2026. feat/team-calendar-keyboard from fetched origin/main
7dcd5f18421516c9f3d9c015d5f3760d4e0e8aa7. PR #131 confirmed merged at 18:29:37 UTC.
Initial tree clean; unrelated worktrees and ignored local artifacts preserved. At implementation handoff
there had been no commit, push, PR or issue edit. No delivery enablement or calculation change. Existing #91/#76 scope, no new issue.
[Interaction design](../adr/0031-team-calendar-keyboard-navigation.md).

## Changes and preserved boundaries

One desktop date is tabbable; focus moves independently of selection. Native Enter/Space selects the
list date. Arrows, Monday/Sunday Home/End and Ctrl+Home/End stay inside mounted cells. Month controls
clear selection explicitly without moving focus away from the control; January 1900/December 9998
boundaries are inert aria-disabled controls. Adjacent January 9999 cells are still valid date facts.
A focused grid hidden at the 40rem breakpoint transfers focus to the existing date input; widening
never steals focus. Date input/list/reset remain available on mobile. Date controls remain mounted
through loading/error, unavailable counts are not zero, and query responses never move focus.

Role guards, server authorization, account/session/range keys, API DTOs and calculations are untouched.
activeAbsent still unions active employee IDs across overlapping Approved requests. Inactive history,
full spans, history links, whole-range export and 62-day API limit remain unchanged. No backend,
schema, package/lockfile, CI, authentication, provider or statutory change. Browser clock provides
SAST today and UTC client receipt time as before; neither is a server snapshot timestamp.

## Actual local executions, kept separate

Windows x64; installed Chrome, existing Playwright/private file-backed SQLite/API fixture. Real
migrations and generated private credentials; screenshots/traces/video disabled by default. No
external transport or real employee data. Retries zero; fixture setup/cleanup owned by the existing
bounded runner. No new scanner dependency.

1. Initial focused run: **6 passed / 2 failed**, 51.76s. All keyboard/date, loading/retry, year-limit
   and session-barrier cases passed. Two new test assumptions failed: one Tab was incorrectly assumed
   to leave a native segmented date input; a broad Approve|Reject regex also matched Approved in date
   button names. These were assertions/setup issues, not evidence of failed responsive focus repair
   or product decision controls.
2. Separate isolated responsive reproduction: **1 failed**; safe numeric diagnostic pointed to the
   expected one-Tab exit assertion. Separate isolated combined-role reproduction: **1 failed**; safe
   numeric diagnostic pointed to the broad regex. No raw Playwright error, call log, HTML or payload
   was printed. Temporary numeric-location reporter remains ignored/uncommitted.
3. Corrected focused run: **8 passed**, 32.44s. Native keyboard exit is bounded to five Tabs through
   browser-owned segments and must reach Show whole range. Exact Approve/Reject names retain the
   no-decision-control assertion. No assertion skip, retry or blanket timeout change.
4. Complete default suite after range/default and breakpoint refinements: **49 passed**, 140.04s.
   This is one complete execution, separate from the eight-case focused run.
5. Next complete suite after the Today/Selected visual-label refinement: **48 passed / 1 failed**,
   173.85s. Responsive focus after narrowing failed; the prior 49-pass run was not a final guarantee.
6. Separate isolated responsive check passed, but a bounded three-execution repeat produced
   **2 passes / 1 failure**. Numeric-only diagnostics located the failure at the input-focus assertion
   immediately after narrowing. Source inspection identified an activeElement-only media callback
   that could miss focus after the browser hid/blurred a date. Grid focus ownership plus blur handling
   now covers that transition and clears ownership on actual departure. No raw browser event trace
   was captured, so exact native event scheduling is not claimed as measured evidence.
7. After that correction, a separate three-execution responsive repeat passed **3/3**, with no retries.
   Reverse native-input Tab exit is bounded too; resizing with focus outside the grid must not steal
   focus. Added a real successful background refresh check and two direct route/API non-Manager
   denials (Employee and HR-only). Retained calendar suite now has ten cases.
8. A full-run command with a missing npm argument separator did not execute tests (Unknown cli
   config); it is not counted as a test pass.
9. Corrected final complete default run: **51 passed**, zero failures/skips/retries, **144.66s**.
   This single execution contains the final ten calendar cases and the 41 existing cases after the
   focus correction. It is separate from every earlier focused/repeat/full run above. The command
   used -- --reporter ./safe-reporter.ts,../../.local-verification/calendar-location-reporter.mjs;
   the extra local reporter extracts numeric test-source locations only and is not retained or
   required to reproduce the suite with the existing safe reporter.

Frontend TypeScript/production build passed before browser checks. Latest Vite build reported 1.33s;
main chunk 360.42kB, existing lazy Manager workspace remains separate. Frontend lint and browser TypeScript checks passed, with the existing useAuth.tsx:223 Fast Refresh
warning only. Final documentation/diff checks are recorded below. No dependency changes; dependency audits were not rerun for this slice.
Backend build/tests/model checks were not rerun: backend files and contracts are unchanged. Existing
backend permission/snapshot evidence remains dated evidence, not a new backend execution.

## Coverage and evidence type

[Ten retained cases](../../tests/browser/team-calendar.spec.ts) cover:

- Real HTTP Manager Overview and protected direct Team leave route. One Tab entry/exit, full date
  names/current day, column headers, arrows/weeks, leap February, adjacent cells, range clamping,
  focus distinct from Enter/Space selection, reset, explicit month change and Today.
- Actual API reads at January 1900/December 9998 and adjacent January 9999, month-limit controls stay
  focused/inert. Combined-role personal/HR navigation stays additive; calendar has no decisions.
- Controlled synthetic display data: two overlapping requests for one active person plus one inactive
  historical person produce one daily active-person count and three history rows. This tests client
  union/list semantics, not three records persisted by the backend.
- Real authorized CSV download uses the full displayed range Jan 29-Mar 3, not the selected date.
  Download bytes read in memory. The real fresh export need not match controlled display rows; no
  spreadsheet compatibility claim. No downloaded personnel report retained.
- Explicit transport barriers: new-month loading never relabels old rows; background refresh, 503,
  unavailable counts and successful retry retain selected date and focused mounted button. A
  programmatic Refresh click simulates background invalidation without moving keyboard focus.
- Mobile 390px, desktop 1280px and tablet-width 1024px; dark/light, reduced motion, no page overflow,
  bounded forward/reverse native input Tab exit and focus repair only when the grid becomes hidden. Arrow focus
  remains separate from a date input selection after widening.
- Controlled late HTTP completion across different-account and same-account relogin: no old rows,
  selection or focus state is restored. No authentication provider or query behavior is bypassed.

The full suite separately exercises existing leave submission/decision/history, HR monitoring,
employee drafts/attention/activation, onboarding route denial, protected navigation, refresh/session isolation and
safe download-related code. Real HTTP workflows are distinct from labelled controlled transport.

## Commands

~~~powershell
npm.cmd --prefix src/HRFlow.Client run build
npm.cmd --prefix src/HRFlow.Client run lint
npm.cmd --prefix tests/browser run typecheck
$env:HRFLOW_BROWSER_CHANNEL='chrome'
npm.cmd --prefix tests/browser test -- team-calendar.spec.ts
npm.cmd --prefix tests/browser test
~~~

Optional local screenshot generation sets HRFLOW_CALENDAR_SCREENSHOTS=1 for the synthetic calendar
case. Only the calendar region/viewport is captured in ignored local storage, then reviewed copies
are placed below. Ordinary CI does not set this flag. Never capture login or real accounts.

## Reviewed screenshots

- [Desktop light: focus differs from selected date](screenshots/team-calendar-desktop.png).
- [Mobile dark: date-input/list alternative](screenshots/team-calendar-mobile-dark.png).

Images use controlled synthetic names/dates only. Client receipt time is based on the controlled
browser clock. No header/email, credentials, tokens, activation links, document contents or private
paths. No screenshot is offered as screen-reader or physical-device evidence.

## Outstanding limits

No screen-reader/assistive-technology execution, physical keyboard/touch device, forced-colors,
comprehensive contrast/WCAG audit or Safari/Firefox run. Hosted Linux was unexecuted at implementation
handoff; actual PR workflow results are reported separately after publication.
No claim of full accessibility compliance. A separate production-built browser suite was not run;
normal browser gates exercise the actual Vite development client. Local production build succeeded.
No new ClamAV, backup/recovery, load, provider delivery, deployment or legal calculation evidence.
Historical reports remain unchanged. No backend checks are inferred from frontend success.

## Final documentation and scope checks

Final build/lint/typecheck passed. The existing Fast Refresh warning is retained; no build failure
or new lint warning. Git whitespace, relative-link, UTF-8/replacement-character and sensitive-literal
checks passed, and both instruction files are byte-identical. Complete patch includes new source,
tests, design/report and reviewed synthetic screenshots. No generated output, private stores,
credentials, logs or temporary reporters belong to the review diff. These were implementation-handoff
checks, before staging or publication.

## PR finalisation review

The complete source, test, documentation and screenshot diff was reviewed again. No source/test
correction was required. One desktop date remains tabbable; focus/selection are independent, mounted
range boundaries are explicit, and loading/error/breakpoint handling preserves usable focus. Counts,
mobile alternative, exports, current role scope and session isolation remain unchanged. Both synthetic
screenshots were inspected again for secrets and private data.

Staged whitespace, UTF-8/sensitive-literal, relative-documentation-link and instruction-byte checks
were rerun for finalisation. The 51-test local browser execution above is earlier implementation
evidence, not a newly rerun finalisation suite. Backend tests were not rerun locally because backend
files were unchanged. Hosted results are recorded on the PR separately; no assistive-technology,
physical-device or comprehensive accessibility claim follows from browser test success.
