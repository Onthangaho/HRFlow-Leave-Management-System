# follow-redirects security patch — 10 November 2026

## Scope and source

Branch `fix/follow-redirects-security` starts from fetched `origin/main`
`49f3373477e9052f2d4f68d1317f3e6ce8dbe81d`. PR #139 and its branch remain unchanged.
Only the client lockfile resolution and this report are intended changes; no manifests, overrides,
audit suppression, business source, tests, schema or CI changes.

[GitHub-reviewed GHSA-mrwp-f3q8-3g9p](https://github.com/advisories/GHSA-mrwp-f3q8-3g9p)
(CVE-2026-116689), retrieved 10 November, lists 1.16.0–1.16.1 as affected and **1.16.2** as patched.
It concerns cross-realm `sensitiveHeaders` arrays allowing custom credentials through cross-origin
Node redirects. Built-in header protections remained effective in the advisory's reproduction.
The advisory did not reproduce the problem through exact Axios 1.20.0, which normalizes the array;
that qualification is not a reason to retain an audited vulnerable dependency.

Installed path before patch: client → Axios **1.20.0** → follow-redirects **1.16.1**.
Axios's existing `^1.16.0` range accepts **1.16.2**. Both installed and locked versions now agree at
1.16.2. Axios remains 1.20.0. Registry tarball/integrity metadata was retrieved with
`npm.cmd view follow-redirects@1.16.2 dist --json`; `npm ci` validated the lockfile install.
The browser test tree contains no follow-redirects entry and its lockfile is unchanged.

This is a production dependency by npm classification through Axios, but its redirect implementation
is Node-specific. The installed Axios browser mapping replaces the Node HTTP adapter with a null
module and maps the Node bundle to the browser bundle. The application SPA does not thereby acquire
the Node redirect adapter. Node consumers/tooling remain relevant; this patch does not claim a
browser credential leak or prove absence of every possible Node usage.

## Historical evidence preserved

[PR #139 hosted run 42660224388](https://github.com/Onthangaho/HRFlow-Leave-Management-System/actions/runs/42660224388)
passed **196 backend** tests but failed its client audit on this advisory. Browser setup/tests were
**not executed**. That run remains failed; these new local results do not change its outcome or
constitute hosted verification of this patch. October zero-advisory evidence is historical.

## Executed locally on Windows, Node 24.12.0 / npm 11.6.2

Commands use `npm.cmd` from the indicated project directory:

- Client and `tests/browser`: separate `npm.cmd ci` executions passed. Client installed 130 packages;
  browser installed three. Both install-time audits reported zero vulnerabilities.
- Each project: `npm.cmd audit --json` and `npm.cmd audit --omit=dev --json` passed: **0 vulnerabilities
  in all four explicit audits**. These are time-specific registry results, not a permanent guarantee.
- Client: `npm.cmd run build` passed (TypeScript project build and Vite 8.2.4; Vite 4.41 seconds).
  `npm.cmd run lint` passed with the existing `useAuth.tsx` fast-refresh warning. Vite emitted a
  plugin-timing advisory; no chunk-limit warning was emitted in this run.
- Root: `dotnet build HRFlow.sln --no-restore` passed with **0 warnings / 0 errors**, 44.23 seconds,
  to rebuild the disposable browser fixture from current main rather than stale PR #139 binaries.
  The complete backend test suite was not rerun for a Node lockfile-only correction.
- Browser: `npm.cmd run typecheck` passed. Complete default gate command:
  `$env:HRFLOW_BROWSER_CHANNEL='msedge'; npm.cmd test`. **78/78 passed in 245.307721 seconds**, exit 0, no skipped/retried tests. The enabled count was independently checked with `npm.cmd test -- --list --reporter=list`: 78 tests in 10 files.
  One worker, zero configured retries, screenshots/traces/video disabled, disposable synthetic store.

## Resolution attempts and limitations

An initial root-directory `npm explain` found no dependencies (wrong project context); repeating in
client confirmed the path above. Initial restricted-cache npm update failed with EPERM before
successful resolution. A subsequent targeted npm update selected 1.16.3 and unrelated optional WASM
metadata. Those changes were discarded: the final lockfile starts from main and changes only the
version, registry URL and integrity of follow-redirects to the specifically requested 1.16.2.
No forced fix, override, manifest change or unrelated upgrade is retained.

Positive real ClamAV, hosted Linux/Chromium and the separate production-built browser suite are not
claimed by this local default gate. No provider connection, external delivery or submission change.

The gate exercised actual server expiry/refresh, shared refresh, session replacement, non-replayable
submission, Employee submission/Manager decision, HR monitoring, lazy-module recovery and authorised
report/export navigation. Controlled transport barriers remain labelled separately from actual HTTP.
Node emitted its existing experimental SQLite warning. This is an Edge/development-client run on
Windows, not hosted Chromium or production-built client verification.

An auxiliary root-context test-list command hit restricted local-artifact traversal and returned no
usable test count. It did not run tests or affect the complete gate. A correct project-context
list with the explicit list reporter subsequently confirmed the enabled count. No test failures or
cancellations occurred in the complete gate.

Final review: the only changed dependency entry is `node_modules/follow-redirects`; both manifests
and the browser lockfile match main. Instruction files remain unchanged and byte-identical. Staged
whitespace, UTF-8/encoding and sensitive-pattern checks cover the two intended files; no private
fixture data or generated output is included. Publication was not requested during implementation; subsequently authorised for independent review on 10 November. Hosted results will be recorded separately in the PR.
