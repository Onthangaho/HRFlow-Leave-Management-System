# Responsive application shell verification

Issue #77, checked 8 October 2026 on `feat/responsive-application-shell`.
Base: fetched `origin/main`, `fcc1d9a4a62ef774b8834365e89238e919c3d3ac`.
PR #99 was confirmed merged at 19:53:17 UTC and PR #100 at 20:00:16 UTC.
The starting working tree was clean. During implementation verification, no commit, push, PR, issue closure or deployment was
performed. Finalisation publishes this focused patch for independent review, without closing #77.

## Design and scope

- One stable React Router layout below authentication and above the existing capability guards.
  Login stays outside. One header H1 and one main landmark replace duplicate page headers/main
  elements and full-screen wrappers; useful introductions, actions and timeline context remain.
- Shared navigation supplies additive Personal, Team and Administration menus and Overview
  actions. HR-only has no personal or decision menu. There is one Overview per account and no
  placeholder Profile, Settings, Notifications or calendar control.
- Header identity and initials use existing authenticated email/roles. No HR directory query is
  made for the header. Overview provides working tasks, not invented dashboard metrics.
- Desktop sidebar; native modal mobile/tablet drawer with backdrop, initial Close focus, explicit
  Tab wrapping, Escape, opener restoration and main focus after navigation. Resize closes it.
  Background scroll locking is cleaned up. Skip link, landmarks, active links and reduced motion
  are supported. Tables retain their existing container scrolling and chart dimensions.
- Shared neutral panels and indigo primary actions, distinct destructive actions, visible focus,
  wrap-safe identity/role text, and refresh/retry controls for older personal/queue/monitoring states.
- Outlet resets only on account/login epoch changes. It has no pathname or access-token key.
  Existing page edit snapshots, query keys, mutations, audit/version safeguards and PR #99
  JWT/refresh/non-replayable/session logic are unchanged. Ordinary route changes still follow normal
  React Router mount/unmount behavior; this is not new cross-route draft storage.
- No new dependency, API, database migration, backend source, dashboard calculation or business rule.

## Setup and evidence categories

Temporary ignored tooling launched installed Chromium **154.0.8037.98** through Playwright,
the real Vite client and two API processes sharing a newly created disposable SQLite file.
Private generated provisioning passwords, signing key, refresh pepper and tokens were never
published. Existing databases were not deleted or altered. New databases were used when rerunning
fixture setup. Only recorded disposable process IDs were stopped.

The API used the already-built PR #99 backend artifact, whose source matches this branch's backend;
no new backend build is claimed. This is a client-only change. Browser checks use actual forms,
navigation and live HTTP; persisted-row checks use an independent SQLite connection. Timing/error
interception and controlled refetch triggers are identified below. No static rendering or
Axios-adapter output is substituted for interactions. No permanent automated test files are added.

## Executed real browser checks

The final primary runner exited **0** and confirmed:

- Employee, Manager, HR-only and combined-role menus; one Overview; login outside the shell.
  A synthetic combined-role account with a long email/display name was created through the API.
- Employee direct HR denial, HR-only personal-request denial; additional final checks deny Employee
  team/decision URLs. Supplemental checks deny Manager direct HR report access. Denied pages retain
  shell navigation without rendering the protected feature.
- Desktop 1440×960, tablet 768×1024 and mobile 390×844. Drawer initial Close focus, Shift+Tab from
  first to last and Tab back, Escape with opener restoration, navigation close with main focus,
  resize-to-desktop dismissal, reduced-motion transition duration and visible skip-link activation.
- Opening/closing the drawer keeps the request start-date draft. Final CSS smoke checks backdrop
  dismissal, opener restoration and removal of body scroll lock.
- HR policy/type creation through actual forms; persisted records inspected. The new type appears
  in the actual Employee selector after switching accounts without reloading the page.
- HR employee creation and profile edit; persisted version rotates and manager remains assigned.
- HR deactivation through its real dialog; reason survives discard cancellation/Escape. Native
  dialog focus wrapping includes the textarea. Success reports one cancelled Pending request;
  profile is inactive and SQLite has the correct HR actor/reason cancellation audit.
- HR Pending monitoring has no Approve button; department reports/chart/table load; personal
  timeline entry loads with its contextual links and one main/H1.
- Three actual Employee form submissions, history/balances and owner cancellation. Manager approves
  one and rejects one through the existing confirmations; Approved leave appears in team summary.
- Held **real successful history responses** released after logout/different-account login and
  same-account relogin cannot replace the current session, expose old history or leave old content
  on Overview. Route interception controls timing, not the returned payload.
- No browser runtime errors in the completed primary run.

Supplemental executed interactions:

- While editing a policy, a separate real API update rotates its version. A controlled window
  visibility event triggers background refetch; draft remains intact and the subsequent form PUT
  sends the **original expectedVersion**. Real 409 preserves input. Explicit discard/reload
  confirmation starts on Cancel; Escape retains the draft. Confirming reload then saving succeeds.
- Type edit and unused-type deletion persist. Delete confirmation starts on Cancel, wraps focus,
  supports Escape and restores its invoking Delete control. No referenced history is deleted.
- Inactive read-only details, directory table and reports remain within the viewport at 390px
  and 320px; tablet report view at 768px also has no page-level horizontal overflow.
  Screenshot review caught inherited wrap-anywhere squeezing the employee columns: corrected to
  normal word wrapping plus an 832px minimum table width inside a focusable scroll region. Real
  keyboard ArrowRight scroll and inactive-details access passed; affected screenshots were recaptured.
- Empty Manager queue, controlled HTTP 503, useful error and Try again followed by real API recovery.
- A controlled visibility refetch plus an injected initial selector 401 causes **one real refresh
  and successful retry**. All selected type/date values survive; neither shell nor form remounts.
  This is not a new natural-expiry wait; natural expiry was separately verified for PR #99.
- Focused final runner exits 0; shared primary action contrast computes to **8.09:1** after converting
  browser OKLCH colors to sRGB. Reduced-motion CSS also disables existing card hover translation.

Temporary verifiers were corrected for ambiguous Manager label matching, native dialog control
order, the existing deactivation reason prefix, login return-location/submission success behavior,
window rather than document visibility events, and browser OKLCH color parsing. These were tooling
assumptions, not application defects. The supplemental runner completed its listed functional cases
before its original color-parser assertion stopped; the corrected focused refresh/contrast runner
passed. No blanket claim of a complete historical regression suite is made.

## Persisted outcomes

Final independent inspection: **4 requests, 8 AuditEntries**:

- One Approved, one Rejected and two Cancelled (owner cancellation and HR auto-cancellation).
- Each has exactly one initial null → Pending submission and one matching terminal transition.
- HR auto-cancellation actor matches the acting HR employee and reason is
  `Employee deactivation: Synthetic shell verification` (synthetic fixture only).
- Employee creation/edit and manager assignment, inactive lifecycle/version, policy entitlement/edit,
  type rename and unused-type deletion were checked against API projections or persisted rows.

## Actual checks

- `npm run build`: passed; **814 modules**. Main JS **501.74 kB** (151.73 kB gzip), report chunk
  **368.31 kB** (106.74 kB gzip). Vite warns about the main chunk exceeding 500 kB; not suppressed.
- Finalisation baseline build from an isolated archive of the unchanged base, with the same
  installed dependencies: **812 modules**, main JS **498.43 kB** (150.10 kB gzip). After: **501.74 kB**
  (151.73 kB gzip), an increase of **3.31 kB** (1.63 kB gzip). The baseline did not emit the 500 kB
  warning; the implementation now crosses that threshold. No optimisation or warning suppression
  is introduced during finalisation. No additional browser checks are claimed here.
- `npm run lint`: exit 0; existing `react(only-export-components)` warning at `useAuth.tsx:210`.
- `git diff --check`: final check recorded at completion, including separate new-file whitespace and
  complete-patch reverse-apply checks. Instruction copies are identical.
- No dependency changes: no new dependency audit was executed. Backend source is unchanged, so no
  backend build was rerun. Generated build output, private tooling, logs and databases are excluded.

## Screenshots

All images show synthetic development records only, without browser chrome, credentials, tokens
or machine paths. Viewport captures keep files reasonably small (under 150 kB each).

- [Combined-role desktop Overview](screenshots/responsive-application-shell/combined-desktop.png)
- [Employee mobile Overview](screenshots/responsive-application-shell/employee-mobile.png)
- [Mobile drawer](screenshots/responsive-application-shell/employee-mobile-navigation.png)
- [Mobile leave form](screenshots/responsive-application-shell/request-mobile.png)
- [Manager desktop team view](screenshots/responsive-application-shell/manager-desktop.png)
- [HR policies desktop](screenshots/responsive-application-shell/policies-desktop.png)
- [HR deactivation dialog](screenshots/responsive-application-shell/deactivation-desktop.png)
- [Inactive mobile details](screenshots/responsive-application-shell/inactive-details-mobile.png)
- [Mobile directory](screenshots/responsive-application-shell/directory-mobile.png)
- [Desktop reports](screenshots/responsive-application-shell/reports-desktop.png)
- [Mobile reports](screenshots/responsive-application-shell/reports-mobile.png)
- [Tablet reports](screenshots/responsive-application-shell/reports-tablet.png)

## Limits and unexecuted scenarios

- No screen-reader session, formal complete contrast/accessibility audit, touch-device trial,
  Safari/Firefox suite, production deployment/load measurement or new SQLite concurrency exercise.
- No fresh complete role-revocation/security matrix, self-deactivation, delayed 401/refresh failure,
  every conflict/permission failure or full policy CRUD variant. PR #99 and existing feature reports
  remain dated evidence; this change retains their code and does not claim to rerun every case.
- Controlled refetch/error/timing cases are distinct from naturally occurring expiry/network failures.
  Client navigation still reflects issued roles until refresh; current API authorization is decisive.
- Session persistence, cross-route draft persistence and API-backed role dashboards remain out of scope.
  The new shell does not make statutory leave or production-compliance claims.
