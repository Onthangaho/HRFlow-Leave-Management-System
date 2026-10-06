# Leave policy management UI verification (#24)

## Scope and design

PR #67 was confirmed merged before creating `feat/leave-policy-management-ui` from current
`origin/main`. Issue #24 is open with title "HR Admin: leave type/policy management UI";
its acceptance criterion requires an HR-created type to appear in the actual request dropdown.
The working tree was clean. No backend, migration, dependency, or existing database changes.

`/admin/leave-policies` is HR-only and provides Leave types / Policies navigation, search,
responsive records, creation/editing, policy previews, shared-rule warnings and deletion.
`/leave-requests/new` is Employee/Manager-only. This connected scope restores the missing
minimal submission form necessary for #24: type and inclusive calendar dates only. Identity
comes from the server, and policy calculations remain in the backend. HR-only accounts have
no personal request navigation or access. This is actual form verification, superseding the
API/browser-request-only limitation of #23's historical verification record.

Edit snapshots and original versions survive background refetches. PUT/DELETE send the
original expectedVersion. Conflict errors preserve input; explicit reload confirms discarding
unsaved values. Deletion checks server responses even when the read snapshot says canDelete.
Counts include all statuses; referenced requests/audits are never removed by this UI.
Creating a policy alongside a type draft retains the unfinished type values.

New queries use authenticated Axios, account-scoped keys, enabled guards and AbortSignals.
Mutation callbacks use the initiating account's namespace. Management changes invalidate
management, selector, balance/history, queue and monitoring keys only in that namespace.
Submission invalidates personal and applicable same-account monitoring/reference snapshots.
Other accounts cannot be invalidated from this tab: the selector uses staleTime 0 and
refetchOnMount 'always', disables submission during refresh, and refreshes on re-entry.
The existing auth provider cancels/clears caches on account changes; sessions remain in memory.
Dialogs start on Cancel, explicitly wrap Tab/Shift+Tab, handle Escape and restore focus.
Inputs retain values after recoverable errors; dirty management cancellation requires confirmation.
Immediate guards and disabled controls prevent duplicate writes before React rerenders.

## Actual browser and persisted-state checks

Temporary verification tooling and all databases/logs stayed outside the repository. No
permanent automated test files were added. Connected browser surfaces were unavailable;
isolated headless Chromium exercised the real Vite client and API against a fresh disposable
SQLite database. Desktop viewport: 1280 x 1000. Mobile viewport: 390 x 844.

Passed through actual UI controls unless explicitly noted:

- HR created/edited a policy, created/edited a type and switched its policy with a warning.
- Nested policy creation retained the type draft and made the new policy selectable.
- Duplicate normalized type name returned a conflict, keeping input; dirty cancellation worked.
- A concurrent API update made the open type form stale: save sent its original version,
  kept the draft, and explicit confirmed reload loaded the current record.
- Unused type and policy deletions succeeded; referenced type and policy deletions failed
  with meaningful server errors. Stale delete retained its original version and offered reload.
- A policy deleted via API while selected in a new type draft became unavailable without
  losing input. A temporarily intercepted 503 policy load disabled saving, retained input,
  displayed retry, and recovered after restoring the real endpoint.
- Actual Identity HR membership was temporarily removed using an independent SQLite
  connection. The existing JWT's write returned 403 with clear feedback and retained input;
  membership was restored in cleanup. No application authorization changes were required.
- An independent SQLite connection held BEGIN IMMEDIATE. The form's write returned a
  recoverable contention conflict, retained input and persisted no insert. The lock was released.
- Two immediate save clicks issued exactly one POST. A zero-entitlement policy with overlap
  enabled persisted DefaultBalance 0 and AllowOverlap true, then was deleted unused.
- Search displayed a clear no-match state.
- Employee and Manager direct HR URLs were denied. HR-only submission URL/navigation was denied.
- HR created a type, signed out, signed in as Employee without reloading, and selected the
  new type from the actual dropdown. The actual form submitted a two-day request as Pending;
  history showed it and the remaining balance stayed 14 because Pending reserves nothing.
- Two immediate submission clicks issued one POST and created one additional one-day request.
- Delayed HR mutation completion after logout/Employee login exposed no old protected
  content or success notice; the selector fetched the newly persisted type. Re-entry refreshed it.
- Existing employee edit/save, Manager approval and HR pending monitoring remained usable.
- Dialog keyboard checks passed Cancel initial focus, Tab wrapping, Escape cancellation and
  invoking Delete-button focus restoration. Mobile records, request and policy forms had no
  page-level horizontal overflow. Screenshots were visually inspected.
- Main workflow and persisted-check runs recorded no browser page errors.

Independent raw SQLite inspection after Manager approval showed exactly two requests for
the disposable form type: the two-day request Approved and the one-day request Pending,
with entitlement 14. Exactly one corresponding Approve audit row (Pending -> Approved)
existed. The Pending request had no decision audit. This inspected persisted rows rather
than relying only on HTTP responses. No real employee data is included in this report.

Initial disposable API setup omitted issuer/audience configuration; correcting the isolated
process fixed authentication. Temporary browser locators were corrected to existing accessible
control names/date formatting. Focus checks exposed dialog wrapping/restoration gaps, fixed
and rechecked. The final Manager approval/persisted check completed separately after correcting
a temporary date locator; an earlier aggregate script timeout was not reported as a full pass.
Node's temporary SQLite inspector emitted its experimental-module warning.

## Actual command results

- `npm.cmd --prefix src/HRFlow.Client run build`: exit 0; 231 modules transformed;
  JavaScript 473.29 kB / 144.18 kB gzip; CSS 34.48 kB / 6.70 kB gzip.
- `npm.cmd --prefix src/HRFlow.Client run lint`: exit 0; only the pre-existing
  `useAuth.tsx:184` Fast Refresh export warning.
- `git diff --check`: passed. New text files were also checked separately for whitespace.
- Both instruction files were checked byte-identical.
- Backend build was not run: no backend files changed. Browser verification used the existing
  merged API binaries against only the disposable database.

## Unexecuted checks and limitations

Other browser engines, physical mobile devices, full screen-reader/accessibility audit,
production deployment/load, and every network/cancellation race were not exercised.
The previously unexecuted Manager personal submission and fully empty configuration cases
were completed in the focused finalization checks below.
No production migration or new cross-process backend concurrency suite was run for this
frontend-only change; #23's backend evidence remains separate. Implementation verification
did not publish changes or close issues; publication is a separate authorized step. Protected content still requires
login after a full refresh, following the existing deliberate in-memory session rule.

## Focused finalization verification

Both remaining scenarios passed in isolated Chromium using two newly created disposable
SQLite stores. No source code changed during finalization; build/lint results above are
recorded implementation results, not newly executed checks.

- Manager submitted their own two-day Annual request through the actual form. Personal
  history updated without a reload; an independent read-only connection confirmed request
  ownership, Pending status, no processed actor and no decision audit before approval.
- Self-approve and self-reject returned 403. The own request was absent from the Manager's
  actionable queue. An unassigned same-department Manager and an HR-only account each
  received 403 on both approve and reject. These denied direct calls persisted no decision.
- The assigned senior Manager approved through the real queue. Independent inspection
  confirmed Approved status, the assigned manager as processed actor and exactly one
  matching Approve audit (Pending -> Approved). The submitting Manager's history showed
  Approved. A temporary empty-queue locator timeout was corrected with a separate persisted
  inspection/history run; the first script was not described as a complete pass.
- The empty case used a brand-new SQLite file populated with the current schema and only
  synthetic Identity/profile fixtures from the newly created verification store. No policy,
  type, request, audit or token rows were copied; no records were deleted. Independent
  checks confirmed zero configuration/request/audit rows before and after API startup.
- Employee saw a clear unavailable-type message and a disabled submission button. Filling
  dates, clicking the disabled button and explicitly dispatching a submit event produced
  no submission POST and no persisted request.
- HR saw "No leave types yet" and "No policies yet", with clear New actions, and created
  the first policy (8 calendar days) followed by the first linked type through actual forms.
- After HR-to-Employee switching without a full reload, the first type appeared in the
  actual dropdown. Valid dates enabled submission. Persisted counts were one policy,
  one type, zero requests and zero audits; foreign-key inspection found no orphan rows.
- Both focused verification runs recorded no browser page errors. The temporary native
  SQLite inspector emitted its experimental-module warning. API/Vite processes were stopped
  after verification. All existing databases were preserved.

Publication review confirms issue #24 and its explicitly connected submission scope only.
Both instruction files are byte-identical. Working and staged whitespace checks are recorded
in the publication output. Documentation and screenshots were inspected for credentials,
tokens, real personal data and local machine paths. Nine PNG captures are 28,468-104,884 bytes each (497,871 bytes total). Desktop captures
are 1280 pixels wide; mobile captures are 390 pixels wide, with the longest full-page
capture 2541 pixels high. Captures contain only synthetic configuration/form data.

## Screenshots

- [Desktop leave types](leave-policy-management-ui/desktop-types.png)
- [Mobile leave types](leave-policy-management-ui/mobile-types.png)
- [Desktop shared-policy edit](leave-policy-management-ui/desktop-policy.png)
- [Mobile shared-policy edit](leave-policy-management-ui/mobile-policy.png)
- [Desktop actual request form](leave-policy-management-ui/desktop-request.png)
- [Mobile actual request form](leave-policy-management-ui/mobile-request.png)
- [Empty Employee submission](leave-policy-management-ui/empty-submission.png)
- [Empty HR leave types](leave-policy-management-ui/empty-types.png)
- [Empty HR policies](leave-policy-management-ui/empty-policies.png)

Reference: [TanStack Query refetchOnMount contract](https://tanstack.com/query/latest/docs/framework/react/reference/functions/useQuery).
