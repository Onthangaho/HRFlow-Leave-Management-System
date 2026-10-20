# Client audit advisories - 20 October 2026

## Scope and isolation

Branch fix/client-audit-advisories uses a separate worktree from fetched origin/main
40c2617577ebf4d661d6db1c596156426d18fe90 (merged PR #127). The original
docs/activation-delivery-design worktree and PR #128 remain unchanged. No delivery implementation,
external email, product-source, backend/schema, test, CI or authentication changes.
Only the client lockfile changes executable dependency resolution; original manifest ranges and
browser manifest/lock remain unchanged. No overrides, forced fix or audit suppression.

## Advisory investigation and exact versions

Official advisories/release pages and registry metadata retrieved 20 October 2026, independently for
each advisory. The current Vite advisory is broader than the older range printed in PR #128's run.

- [GHSA-q3jh-wrc6-jgq8](https://github.com/advisories/GHSA-q3jh-wrc6-jgq8):
  follow-redirects affected >=1.16.0 <1.16.1; patched 1.16.1. Array-form headers can bypass
  cross-origin credential stripping. [Official 1.16.1 release](https://github.com/follow-redirects/follow-redirects/releases/tag/v1.16.1).
- [GHSA-8r9p-f939-6h3c](https://github.com/advisories/GHSA-8r9p-f939-6h3c):
  separately confirms >=1.16.0 <1.16.1 and patch 1.16.1. Incomplete escaping of custom sensitive
  header names can leak credentials. One fix was not assumed to cover the other.
- [GHSA-698v-4r3x-3vcv](https://github.com/advisories/GHSA-698v-4r3x-3vcv):
  launch-editor <=2.14.1 is affected; 2.14.2 patched. The Vite 8.2 line through 8.2.3 is affected;
  8.2.4 is patched. Windows command expansion requires attacker-controlled file/environment conditions
  described by the advisory. [Vite 8.2.4](https://github.com/vitejs/vite/releases/tag/v8.2.4) and
  [launch-editor 2.14.2](https://github.com/vitejs/launch-editor/releases/tag/v2.14.2) release pages
  were retrieved. No exploit was executed against a desktop editor or real environment.

Registry version/engine/dependency/integrity metadata was checked with npm view for
[follow-redirects 1.16.1](https://registry.npmjs.org/follow-redirects/1.16.1) and
[Vite 8.2.4](https://registry.npmjs.org/vite/8.2.4). A query for Vite 8.2.3 returned E404; it was
not selected. The advisory, available registry package and installed bundle were reconciled rather
than assuming npm audit fix selected the smallest patch. Release pages are sparse; affected/fixed
claims above come from the advisories and package inspection, not invented release-note details.

Actual graph:

- hrflow-client -> axios 1.20.0 -> follow-redirects **1.16.0 -> 1.16.1** (within ^1.16.0).
  Axios stays 1.20.0 and its direct manifest stays ^1.20.0.
- hrflow-client -> vite **8.2.1 -> 8.2.4**, including the plugin-react 6.0.5 Vite peer path
  and existing tooling peers. Direct manifest stays ^8.2.0. Node engine requirement is unchanged:
  ^20.19.0 or >=22.12.0; the local Node 24.12.0 satisfies it.
- Vite embeds launch-editor **2.14.1 -> 2.14.2** in its Node distribution, not a separate
  lockfile package. Both embedded launcher/middleware source markers were inspected after installation.
- Vite 8.2.4 requires rolldown ~1.2.4 instead of ~1.2.1: selected minimum **1.2.3 -> 1.2.4**,
  its 14 platform bindings move together, and its exact @oxc-project/types becomes **0.143.0 -> 0.144.0**.
  PostCSS was already 8.5.26, meeting Vite's new ^8.5.26 floor; no PostCSS update was needed.

Exactly 18 existing lock package entries change, with no added/removed entries. Updated resolution
URLs/integrities come from npm metadata, not guessed values. No unnecessary direct transitive dependency
is retained in the manifest. Clean npm ci verifies final lock/manifest consistency.

## Runtime exposure

follow-redirects is a production dependency in npm classification because Axios has a Node HTTP
adapter. The installed Axios browser map replaces that adapter with a null helper; ordinary browser
XHR/fetch uses browser-managed redirects, not this Node module. It is still an installed Node hazard
for tooling/reuse, and production-only audit correctly reported it. No claim that being transitive or
predating PR #128 makes it safe.

Vite and its embedded editor launcher are Node development/build tooling, not served application
runtime. The launcher middleware is installed at /__open-in-editor in the development-server path.
The advisory's Windows file/environment prerequisites remain relevant for developers; do not expose
Vite development services as production hosting. Patching installed tooling matters even when a
production browser cannot invoke that Node implementation.

An actual source-map diagnostic production build inspected 531 source entries: **zero**
follow-redirects, launch-editor or Vite Node distribution entries. This supports browser-bundle scope,
not universal unreachability of every Node application or proof of exploit resistance. Maps/build
outputs stay ignored and are not published. The separate production browser gate uses a normal
private build/preview, not these diagnostic maps.

## Install and resolution history

Local Windows x64, Node 24.12.0, npm 11.6.2, SDK 9.0.305 / net8 target, installed headless Chrome.
Disposable migrated stores, private generated credentials/ports/storage; no existing employee database.

Initial --no-save lock-only resolution did not produce the intended persisted graph and was stopped
before treating it as evidence. A targeted npm resolution then introduced unrelated optional WASM
entries and selected newer-than-needed transitive packages. The first clean npm ci rejected that
intermediate graph (missing optional @emnapi/wasi-threads/tslib entries). It was not accepted as final.
Only npm-generated Vite/follow-redirects and required minimum Rolldown/type/binding records were
transplanted into the original lock graph; all other graph entries/root manifest ranges preserved.
The final clean client npm ci --no-audit installed 130 packages, browser npm ci --no-audit installed
three. --no-audit only separates installation from the explicit full/production audits below; no
findings were suppressed, ignored or omitted from those checks.

## Audit evidence

Baseline manifest/lock copied from HEAD into an ignored manifest-only directory, no installation:
full audit exit 1, two affected packages (one high follow-redirects, one moderate Vite); production-only
exit 1, one high follow-redirects. npm aggregates advisories by affected package, not individual GHSA.

Final client full and --omit=dev audits: **exit 0, zero findings in every category**.
Final browser full and --omit=dev audits: **exit 0, zero findings in every category**.
Browser's dependencies are all dev-only, so its production-only audit is not meaningful browser-runner
coverage by itself; full audit is the relevant companion. Audit results are point-in-time registry
evidence, not a guarantee against undisclosed vulnerabilities.

## Verification executions

- Audited locked .NET restore passed; solution build **0 warnings /0 errors**, **18.99s**.
- Final ordinary production build passed, 836 modules, **1.67s** (main 360.37 kB /112.55 kB gzip).
  Earlier diagnostic source-map build passed in **2.54s**; lint passed with only the existing
  useAuth.tsx:223 Fast Refresh warning; browser TypeScript check passed.
- Complete default browser invocation: **33 passed**, **136.835522s**, zero retries/skips.
  Includes real HTTP workflows plus labelled controlled transport, not live external network failure.
- Initial complete backend invocation: **69 passed /19 failed /0 skipped**, **9m15s**. It overlapped
  browser fixture APIs. Captured operator first-admin/recovery diagnostics refused active writers;
  the two-process bootstrap assertion observed exit codes [1, 1], with neither successful. No permission/assertion changed.
  A sequential complete rerun started only after browser cleanup and explicit zero-API-writer inventory.
- Sequential complete backend invocation: **88 passed /0 failed /0 skipped**, runner duration **5m25s**;
  full command wall time **328.642s**. No concurrent browser/API fixture ran. No assertions were weakened.
- Separate complete production-built browser invocation: **33 passed**, **100.07932s**, zero retries/skips.
  The private fixture built and served actual generated chunks; this is distinct from the default run.
  Both suites cover lazy-module retry, additive roles, authenticated profile/HR navigation, real leave
  submission/decision/history, controlled stale refresh and replacement-session isolation, and actual
  report download completion. They are separate executions, not a combined 66-case run.

Three additional disposable production-built browser cases passed separately, **19.217798s**:
actual downloaded bytes plus authenticated no-store/nosniff/attachment headers and safe filename,
then held export completion after same-account HR relogin and different-account Employee login.
Both replacement cases observed zero downloads and no old success notice. The commands used a
temporary audit-download.config.ts/check.ts with the existing fixture/reporter, no retries; both
owned files were removed afterward. This is not part of either 33-case retained run, and does not
claim Excel/LibreOffice execution or comprehensive formula/export coverage.

A private synthetic Node redirect harness initially failed before exercising the assertion: array-form
HTTP/1.1 omitted Host and received 400, leaving no sink request. IPv4 and safe source/count diagnostics
were tried before the missing Host was isolated; none of those failed attempts is a patch pass.
Corrected bounded loopback harness used ephemeral synthetic canaries and two servers: **two baseline
cases leaked /two patched cases did not**, covering array built-in headers and metacharacter custom
sensitiveHeaders. Only fixed labels/statuses/counts were printed; no header values/canaries. This is
additional disposable verification, not an extra retained test or part of the 33-case browser count.

## Commands

Targeted package metadata/resolution used npm view and npm install --package-lock-only --ignore-scripts,
with exact Vite/Rolldown floors; manifest/root ranges restored and unrelated optional churn excluded.
Final reproducible gates from this worktree:

~~~powershell
npm.cmd ci --prefix src/HRFlow.Client --no-audit
npm.cmd ci --prefix tests/browser --no-audit
npm.cmd audit --prefix src/HRFlow.Client
npm.cmd audit --prefix src/HRFlow.Client --omit=dev
npm.cmd audit --prefix tests/browser
npm.cmd audit --prefix tests/browser --omit=dev
npm.cmd ls --prefix src/HRFlow.Client vite follow-redirects rolldown postcss @oxc-project/types --all
npm.cmd run build --prefix src/HRFlow.Client -- --sourcemap --manifest
npm.cmd run lint --prefix src/HRFlow.Client
npm.cmd --prefix tests/browser run typecheck
dotnet restore HRFlow.sln --locked-mode -p:NuGetAuditMode=all -warnaserror:NU1901,NU1902,NU1903,NU1904
dotnet build HRFlow.sln --no-restore
# Run offline/backend gate without any concurrent browser/API fixtures.
dotnet test tests/HRFlow.RegressionTests --no-build --logger 'console;verbosity=minimal'
$env:HRFLOW_BROWSER_CHANNEL='chrome'
# Clear image/scanner/measurement overrides; default suite then separate production-built invocation.
npm.cmd --prefix tests/browser test
$env:HRFLOW_BROWSER_PRODUCTION='1'
npm.cmd --prefix tests/browser test
~~~

## Boundaries and limitations

No application source, server rules, schema, permissions, auth refresh, session generation, export
semantics, retained tests or CI changes. Existing tests remain enabled with no retries/timeout changes.
Real HTTP and controlled transport evidence are labelled in the retained suite; see
[commands/coverage](../../tests/README.md) and [loading design](../adr/0028-deferred-client-features.md).
No positive real-ClamAV rerun, new Linux/hosted workflow, real mobile hardware, comprehensive
accessibility, load/deployment/email delivery or legal-compliance claim. PR #128 stays untouched.
Final documentation/link/encoding, sensitive-literal and full diff checks passed across four intended
files (including the new report); two relative links resolve. The complete patch includes new files.
Both instruction files remain byte-identical. Temporary tools, raw audits, source maps, databases and
logs remain ignored/local; no credentials or fixtures are included in the review diff.
At the implementation evidence cutoff nothing had been staged or published. Finalisation checks
are new; the complete local suite executions above were not rerun during finalisation. Hosted
results will be reported on the PR separately, including which browser mode actually ran.

## Initial backend failures: evidence and attribution limits

Captured examples include FirstAdministratorTests.ConflictingConfigurationIsPreserved(role-duplicate)
and InvalidInputsRollBack(email): expected FirstAdministratorException, actual InvalidOperationException
from WriterQuiescence.RequireStopped, with the fixed stop-API/worker message. The concurrent browser
fixture was running real API processes. [Guard source](../../src/HRFlow.Infrastructure/Services/Operations/WriterQuiescence.cs)
checks host-wide process inventory before offline operations, not only this test database. The observed
refusal therefore prevented the intended downstream validation from being exercised. The
[two-process assertion](../../tests/HRFlow.RegressionTests/FirstAdministratorTests.cs) saw [1, 1],
not the expected exactly one successful bootstrap. Its per-process failure details were not retained.

The guard uses the same fixed message for nonzero writer count and unavailable/invalid process
inventory. Captured stack traces confirm the refusal point, but do not distinguish those branches
for each failure. Complete per-test diagnostics for the first 19 failures were not saved; attributing
every one individually to the same cause is **unresolved**. The known overlap is an invalid test
condition and the isolated rerun (zero API writers before start, 88 passes) supports that explanation,
but does not retrospectively prove each failure. Neither security checks nor assertions were altered.
