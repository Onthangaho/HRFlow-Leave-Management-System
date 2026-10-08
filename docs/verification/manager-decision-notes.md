# Manager decision notes verification

Checked 8 October 2026 on `feat/manager-decision-notes`, based on fetched main
`7e0cd8218bc60ce766e2bf1c18a59687f8a097c5`. Starting tree was clean. PR #101 was
confirmed MERGED (8 October, 20:38:28 UTC). Issue #21 was inspected, then its title/body
were corrected to optional notes and server-confirmed outcomes; it remains OPEN.

## Method

Temporary ignored tooling used fresh disposable SQLite data and two separate real API
processes/connections. Real Identity login and HTTP operations created synthetic fixtures;
SQLite row inspection checked outcomes rather than relying on HTTP alone. Private generated
configuration, credentials, databases, logs and tooling remain outside published files.
A separate copy of earlier disposable data supported migration preservation checks; no
existing application database was migrated, cleared or deleted.

Real headless Chromium drove the Vite client and API. Dialog, forms, navigation, keyboard,
mobile and session checks were interactive, not static rendering or an Axios adapter.
One browser error case deliberately injected a 503 transport response; response holds used
real completed API decisions. This distinction is retained below. No automated test files
or new dependencies were added.

## Executed HTTP and persisted-row checks

- Approve/reject with ordinary notes, no body, blank/whitespace and 500-character notes:
  204, correct trimmed/null value, actor and correlation, one matching terminal audit plus
  the original Submit event. Reason stays null. Padding around a 500-character note is
  accepted and trimmed, rather than rejected by a raw-input limit.
- 501 trimmed characters: both endpoints return 400, request remains Pending with only
  its Submit audit. Nothing is truncated or saved.
- HR-only, Employee, self, unrelated Manager and cross-department decisions: 403, no extra
  audits. Combined HR/Manager capability can decide its eligible report.
- Owner personal history and owner/current eligible Manager/HR timeline expose notes.
  Unrelated timeline returns 404. Reporting reassignment immediately changes visibility:
  former Manager 404 for history/403 for decision, new eligible Manager 200 with note.
  Assignment is restored without rewriting requests/audits.
- Repeated decision: 409 with unchanged audit count. Simultaneous approve/reject across
  two API processes sharing SQLite: one 204, one 409, exactly one terminal transition and
  its winning note. This is one exercised race, not a load/stress claim.
- A disposable AFTER UPDATE OF Status trigger injected a late persistence failure: 500,
  Pending status and original audit only, no decision note. Removing the trigger permits
  a fresh decision; no tracked/replayed audit is reused.
- Two pending 15-day requests against 20 days: approving the first makes the second fail
  400 without audit/note. Prohibited same-type overlap likewise fails at approval; Pending
  and initial audit remain. Existing policy checks have not been weakened.
- Role removal while an eligible Manager decision waits behind a held writer reservation:
  request had passed route authorization; response is the protected handler's specific
  current-Manager 403 after the fixture role removal commits. Status remains Pending with
  its initial audit. Direct SQL removal is disposable scenario setup only. Old role tokens
  are denied; an inactive former Manager's old token returns 401 without a decision audit.
- HR deactivation cancellation retains its separate original reason/actor. Manager history
  receives the generic explanation and null decision note; HR receives the original reason.
- Owner cancellation persists no note. Balance and team-summary reads return 200.

An initial temporary role-removal fixture did not match Identity's stored ID casing and
therefore removed zero rows; it was corrected to normalized UUID comparison and asserted
one affected row before the eligible-Manager check above. That initial fixture result is
not evidence of revocation enforcement.

## Executed real browser checks

- HR policy/type create and employee create/edit forms work; manager assignment is preserved.
  Employee submits through the actual form and sees personal history/balances. Additional
  Pending fixtures are created through real HTTP, not described as form verification.
- Approval modal starts on Cancel; Tab/Shift+Tab wrap through textarea and confirmation.
  Idle Escape closes/restores the initiating button. Dirty Escape offers keep/discard;
  note survives Keep and focus returns to the textarea. Client 501-character validation disables confirmation; a padded 500-character note remains valid.
- Injected browser 503 retains input and causes no mutation retry. Two immediate confirmation
  clicks send one real POST. During a held real response, fields/actions remain disabled,
  Escape cannot dismiss and no optimistic success is shown. Release closes the modal and
  shows the server-confirmed outcome; one persisted approval/note audit exists.
- Mobile rejection with a note works. Real approve/reject with whitespace-only notes persist
  null. Desktop 1440×960 and mobile 390×844 have no document-level horizontal overflow.
- A second API process decides a request while the dialog is open. The attempted decision
  returns real 409; input stays, confirmation is disabled, explicit availability refresh
  detects the already-decided request and retains input. Deliberate discard returns to the
  queue; only the other process's audit/note exists.
- Personal history and timeline show notes. Literal `<b>…</b>` text remains text, with no
  generated HTML element. Timeline keeps explicit UTC times and read-only controls.
- Held successful decision responses complete after navigation away and logout, different
  account login, and same-account relogin. No old note/modal/success notice appears; current
  account/session stays intact. Persisted server outcomes remain. Navigation away is used
  because the native saving modal intentionally prevents background pointer interaction.
- Manager team view, HR Pending monitoring and department report routes remain usable;
  HR monitoring contains no approval controls. No browser runtime errors occurred.

Screenshot review caught a calendar-date timezone conversion in the new confirmation.
It was fixed to format the date portion at UTC midnight; the browser workflow was rerun
with each dialog's dates asserted against SQLite StartDate/EndDate. Screenshots below are
from that corrected run. A first browser run also stopped on an ambiguous locator in the
verification script; no product defect was inferred from that tooling failure.

## Migration/build/diff checks

- `dotnet build HRFlow.sln`: passed, 0 warnings/errors (first implementation
  build found a missing factory parameter; corrected before verification).
- Frontend build and lint: passed. Existing Fast Refresh warning in useAuth.tsx:210 and
  Vite main-chunk warning remain. Final main-bundle figures are recorded below.
- Generated migration Up contains only nullable DecisionNote addition. On a disposable
  prior-schema copy, Up preserves 8 audits, 4 requests and 6 employees byte-for-value in
  row projections; all legacy notes are null. Down removes the new column while preserving
  those existing audit values; reapply succeeds. No new-note rollback retention is claimed.
- `dotnet ef migrations has-pending-model-changes --no-build`: no model changes since the
  generated migration. A final built-API approval/timeline check also passed with Information
  logging enabled: request/actor/correlation were recorded; sentinel note, issued tokens and
  generated secrets were absent from the captured logs.
- Final instruction equality, new-file checks, privacy/scope scan and complete diff export
  are recorded below after final review. No dependency audit is rerun because no package
  manifest or lockfile changes.

## Synthetic screenshots

- [Desktop approval](screenshots/manager-decision-notes/approve-desktop.png)
- [Mobile rejection](screenshots/manager-decision-notes/reject-mobile.png)
- [Explicit stale recovery](screenshots/manager-decision-notes/stale-desktop.png)
- [Desktop timeline](screenshots/manager-decision-notes/timeline-desktop.png)
- [Mobile timeline](screenshots/manager-decision-notes/timeline-mobile.png)

Only seeded/generated synthetic names, example.invalid addresses, artificial dates and
ordinary synthetic notes appear. No credentials, tokens, medical information, real personal
data or machine paths appear. Images are individually below 150 KiB; desktop captures are 1440×960 and mobile captures
are 390×844 or a full-length 390-wide timeline so its note is visible. Dimensions/sizes
are checked during final review.

## Final review checks

Final backend build: passed, 0 warnings/errors. Frontend build/lint: passed; main JS
503.97 kB (152.88 kB gzip), with the existing Vite >500 kB and useAuth.tsx:210
Fast Refresh warnings. Instruction files are byte-identical. Tracked/new-file whitespace
and encoding checks pass; no package/auth/transaction implementation changes or temporary
verification files are in the 36-file intended diff. Synthetic screenshots were visually
reviewed and fit the size limit. Complete binary diff (including new files/images) is exported
to ignored local verification tooling and reverse-apply checked. At implementation review, nothing was staged.

## Remaining limitations / unexecuted checks

No formal screen-reader/assistive-technology audit, physical mobile device, other-browser
matrix, production load or exhaustive race/interleaving suite was run. Delayed successful
responses were exercised, not every delayed error/refresh permutation (shared auth code is
unchanged). Same-request approve/reject was exercised across processes; all approve/approve,
reject/cancel/deactivation contention permutations were not repeated for this note-only
change. A new forced contention-timeout response and note retention for every individual
403/400 response were not separately browser-injected; real stale 409 and injected 503 were.
Existing owner cancellation was checked via HTTP/persisted rows, not rerun through its
browser confirmation. HR deactivation privacy was checked via HTTP/SQLite, not rerun in
its full browser workflow. Statutory accrual, attachments/medical evidence and notification
work remain out of scope. Automated tests remain deferred by repository convention.

The implementation verification above predates publication. Finalisation is authorised separately to stage, commit, push and open a review PR; no additional build/browser verification is claimed. No merge or automatic issue closure is authorised.
