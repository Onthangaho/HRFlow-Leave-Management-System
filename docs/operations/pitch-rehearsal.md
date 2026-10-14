# Isolated current-feature pitch rehearsal (#97 slice)

This is a Development-only, single-host synthetic demonstration. It is not production deployment,
legal compliance, statutory entitlement calculation or a completed #97 dependency set. PR #117
was confirmed merged on 14 October 2026; this branch starts from fetched main. Hosted CI remains
blocked by the owner's GitHub account billing restriction. Do not change billing/protection/workflow
settings to make this pitch appear green.

## Prerequisites and exact commands

Use .NET 8 runtime/compatible SDK, Node 24 and the existing client/Playwright dependencies.
Build **before** starting the pitch API (Windows locks loaded build DLLs).

```powershell
dotnet restore HRFlow.sln --locked-mode
dotnet build HRFlow.sln --no-restore
npm.cmd ci --prefix src/HRFlow.Client
npm.cmd ci --prefix tests/browser
Push-Location tests/browser
npx.cmd playwright install chromium
Pop-Location
$PitchRoot = Join-Path $env:TEMP ('hrflow-pitch-' + [guid]::NewGuid().ToString('N'))
node tools/pitch/pitch-demo.mjs setup $PitchRoot
node tools/pitch/pitch-demo.mjs start $PitchRoot
```

The last command stays open. Use `http://127.0.0.1:5541` in the browser. Ports 5540/5541 must be
free; never point this client at an existing API. Development loopback HTTP is deliberate, not TLS
or Production evidence. The tool forces Development, supplies generated JWT signing/refresh material
and all four seed passwords, runs current real startup migrations/Identity seeding, and puts the
database/private files outside the checkout. No account/role/lifecycle repair script or copied database
is used. HR import and activation use actual existing product flows.

**Real ClamAV is mandatory before setup/start.** Follow [scanner setup](supporting-documents.md),
using official ClamAV, real current signatures and a loopback-only clamd on port 3310. The tool's
bounded VERSION probe rejects missing/ambiguous scanner readiness; the application's real INSTREAM
adapter scans the uploaded bytes. A readiness probe alone is not malware-detection verification.
Do not replace the scanner with an always-clean server. Repair/update the scanner separately; this
tool does not install binaries, update definitions, expose the daemon or manage an existing service.

Setup accepts only a new direct child of the OS temp directory, refusing existing destinations and symlink/reparse ancestors. Its owner-only private descriptor
contains generated credentials; inspect it **privately off-screen**, never paste its contents into a
terminal, report, screenshot, chat or screen share. Use `password` for the four seed accounts and
`employeePassword` when Avery establishes their own password. The generated synthetic CSV/PDF are in
the destination's `fixtures` directory. No password CSV is created. The private root is not a public asset directory. At first startup the fixture resolves
the actual seed Manager department through current HR API access and writes that relationship into
the CSV; it does not move the Manager or weaken validation. Blank schedule code remains Unknown.

For repeatable automated interaction, keep start running and use another terminal with the same
`$PitchRoot` value:

```powershell
node --disable-warning=ExperimentalWarning tools/pitch/rehearse.mjs $PitchRoot
```

The runner uses installed Playwright Chromium by default. The verified local fallback was installed
Chrome: set `$env:HRFLOW_BROWSER_CHANNEL='chrome'` before running. It performs actual clicks/network
requests, not static rendering. Use a **fresh setup/store for every full rehearsal**; it deliberately
does not overwrite/re-activate accounts, reuse invitations, repair duplicates or replay failed writes.
It captures only protected synthetic screens under `docs/verification/screenshots/pitch-rehearsal`.
Review them before staging; no login/activation/browser-chrome screenshots, traces or videos.

## Practical 9-minute walkthrough

Prepare credentials privately and the scanner before presenting. Do not show password typing or
the invitation file/URL. All addresses are synthetic (`example.invalid` or Development-only seed
addresses). The scenario date is the South African business date captured at fresh setup.

1. **0:00–1:10 — HR onboarding.** Sign in as `hr.administrator@hrflow.local`; Administration → CSV
   onboarding. Choose `synthetic-onboarding.csv`, Inspect columns, review the explicit mapping and
   Validate preview. Point out Ready and that preview creates neither accounts nor invitations.
   Confirm selected Ready rows → Create 1 accounts. Explain row-level atomic creation and no silent
   overwrite; results show Created and the employee is Pending activation, separately from Active.
2. **1:10–2:00 — activation (off-screen secret handoff).** Open the generated private pickup invitation
   for `avery@example.invalid` from the unique `Pitch-…` profile in the OS local application-data
   `HRFlow/ActivationPickup` folder. Do not project the file or address bar. Avery chooses the private
   generated employee password in the real activation page, then uses normal login. Say: “This is a
   private local demonstration, not email delivery. Production delivery is not implemented.”
3. **2:00–3:00 — HR company requirements.** Sign back in as HR. Leave policies/types → Policies →
   New policy: `Pitch company policy`, 20 configured calendar days per linked type, overlap off. Leave types → New leave type:
   `Pitch ordinary optional`, then `Pitch ordinary required`, both linked to that policy; Optional
   description, corresponding evidence mode, Ordinary class, plain instructions excluding diagnoses.
   Say these are company submission requirements, not a statement of South African law. Required
   Medical is unsupported pending the absence-reporting/payment workflow.
4. **3:00–4:10 — Employee submission.** Sign out and sign in as Avery without reloading. Request leave,
   choose `Pitch ordinary required`; select the printed scenario date for both endpoints. Add the
   harmless description, upload `synthetic-evidence.pdf`, wait for **Clean**, explicitly tick Attach
   to request and Submit request. Explain quarantine/fail-closed scanning, immutable requirements
   snapshot, inclusive calendar days and that Pending requests do not reserve balance.
5. **4:10–5:15 — Manager decision.** Sign in as `manager@hrflow.local`. Approval queue → Avery's
   Approve dialog. Review type/dates/evidence, enter `Synthetic coverage reviewed.` and Confirm approve.
   Explain current role/reporting checks and approval-time balance/overlap validation; the committed
   decision and one matching audit/note are atomic. There is no optimistic approval before success.
6. **5:15–6:20 — Employee result.** Avery → My leave and balances → View history. Show Approved,
   submission/approval actors, UTC event timestamps and the plain-text note. Notifications shows the
   committed decision alert; deep links recheck current access. No medical/note contents in previews.
7. **6:20–7:15 — team planning.** Manager → Overview and Team leave. Show today's approved absence,
   month calendar and list alternative. Counts mean distinct active people absent on a date, not
   staffing capacity or summed requests; inactive historical records have explicit labels.
8. **7:15–8:20 — HR monitoring/reports.** Before approval, HR Pending monitoring can show Avery;
   afterward it no longer appears. The Manager's personal optional request remains Pending. HR
   Overview shows operational attention/trends; Department leave reports → Export CSV uses applied
   filters and a fresh authorised snapshot. Explain clipped summed calendar request-days, overlapping
   requests counted separately, current departments and the visible `Text: ` spreadsheet safety prefix.
9. **8:20–9:00 — permission/limits.** HR monitoring has no decision controls and direct HR approval
   receives 403. Manager submits their own optional-evidence request but cannot approve it (403);
   only their assigned eligible Manager can decide it. Say explicitly: statutory cycles/accrual,
   qualified legal review, production delivery/deployment and hosted Linux/Chromium remain outstanding.

The automated runner exercises the direct 403 checks and validates persisted actors/state/audits,
ordinary document binding and notification delivery. Do not paste bearer tokens into a live console
to demonstrate permissions; show the safe check result and the role-separated interface instead.

## Stop, restart and cleanup

In the start terminal type `stop` and Enter, or press Ctrl+C. It checks both recorded process IDs
before another start and stops only the owned API and Vite. Client closure and API exit are bounded;
if closure fails, ownership is retained and cleanup refuses live processes.
No files are removed by shutdown. Restart the same marked destination with the start command while
the captured date remains today's scenario; restarting does not replace passwords, restore roles or
reactivate profiles. Fresh pitch runs use new destinations rather than resetting this one.

```powershell
node tools/pitch/pitch-demo.mjs cleanup $PitchRoot
```

Cleanup refuses live owned parent/API PIDs, validates the destination marker and pickup identity,
then removes only the disposable store/files/descriptor and its unique pickup invitations. It does
not stop/delete an unrelated scanner or temp tree. If interrupted/OS-killed, inspect the recorded
owned PIDs privately, stop those processes and verify exit before cleanup; PID reuse causes a safe
refusal. Never clear process ownership merely to bypass that refusal. Keep any needed safe evidence
before cleanup. No database, CSV, attachment, invitation, log or credential file belongs in Git.

## Recovery and troubleshooting

- **Expired invitation:** HR Employee management → explicit versioned Resend invitation; use the new
  private pickup. Do not recreate the account or reuse the consumed/old token. Delivery failure uses
  the same recovery, never another import. No external email sending.
- **Scanner absent/outage:** setup/start refuse absence; during an upload ScanUnavailable stays
  inaccessible and cannot bind. Repair real ClamAV, then use Retry scan explicitly. Do not claim the
  error is a clean verdict, auto-resubmit or classify medical data as Ordinary to bypass privacy.
- **Empty/wrong period:** verify the signed-in role, applied month and captured South African date.
  A fresh store is expected to be empty before rehearsal. Refresh the page's data using its control,
  not a full reload or manual SQL changes; other accounts observe changes through refresh/polling.
- **Stale form:** retain dates/description/drafts, explicitly Reload and review changed requirements,
  or use the employee/policy form's confirmed discard/reload. Do not silently rebase expectedVersion.
- **Unknown facts/schedules:** seed accounts and blank imported schedule are incomplete synthetic
  examples, not legal violations. Do not infer hire dates from account creation or invent schedules.
- **Build/port failure:** stop the owned demo before rebuilding on Windows; do not kill unrelated
  listeners or reuse another API. Fix prerequisites, choose a new store if rehearsal partially committed.

## Truthful pitch boundaries

Current charging is configured entitlement minus Approved **inclusive calendar days**, with no annual
reset, statutory cycles, accrual, workday/public-holiday deductions or qualified compliance claim.
Medical Required submission is rejected server-side; payment/proof rules remain #22/#75-dependent.
Current department/reporting attribution is used, not reconstructed history. Private pickup is local
only; production invitation delivery, deployment/backup/load gates and hosted CI remain unresolved.
The full #97 issue stays open. See [actual evidence](../verification/pitch-rehearsal.md) and
[retained regression commands](../../tests/README.md), keeping local and hosted results separate.
