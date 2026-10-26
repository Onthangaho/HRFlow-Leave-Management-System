# Real bearer expiry and refresh regression slice

26 October 2026. Branch `test/real-token-expiry-refresh`, base
`2a58bc7e8b27d7c76d522570a4ce8829883754a6` from fetched `origin/main`.
[PR #136](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/136) was confirmed
merged at 18:27:13 UTC before branching. This focused #76 slice changes tests and documentation,
not production source, schema, dependencies, token contracts or CI. No statutory or email changes.

## Coverage and test-token provenance

Existing [workflow tests](../../tests/browser/workflows.spec.ts) retain supplementary controlled
401/stale-refresh barriers. They do not become real expiry evidence retroactively. New
[backend gates](../../tests/HRFlow.RegressionTests/TokenRefreshTests.cs) and
[browser gates](../../tests/browser/token-refresh.spec.ts) call the real bearer middleware and
refresh endpoint in independent API processes using fresh file-backed migrated SQLite/Identity.

- Real login-issued access JWT accepted; its configured default lifetime remains 15 minutes.
- Privately signed synthetic probes cover valid, expired, future-not-before, within-skew, incorrect
  issuer/audience/key/algorithm, unsigned, missing-expiry and missing/stale credential versions.
  [Signer](../../tests/HRFlow.RegressionTests/TokenTestTokens.cs) uses only the fixture's generated
  key and existing credential generation. It is not a production issuer or diagnostic endpoint.
- Refresh credentials are actually issued by login, not invented valid rows. Two separate API
  processes enter Refresh behind an explicit independent SQLite writer barrier. One rotation succeeds,
  one fails, the old token cannot replay, and persisted rows show exactly one additional pair and
  revocation. Default refresh lifetime remains seven days.
- Invalid, expired and revoked refresh credentials produce 401 and no stored refresh/business change.
  Combined-role HR revocation preserves personal access after refresh without restoring HR. Inactive,
  pending-activation and real password-change revocation deny old credentials. Current claims/gates
  continue to apply; account facts are not reset by refresh.
- Browser tests log in normally and open the actual Request Leave UI, whose type/draft queries use
  the existing Axios client and mounted AuthProvider. Test transport replaces outbound Authorization
  only while it equals the initiating bearer, using a privately
  signed expired Employee token. The actual server returns 401; no synthetic 401 substitutes for it.
  The provider's refresh credential is the real login-issued value. Eligible retries use the real
  refreshed token. This does not claim a login-issued token was aged by waiting 15 minutes.
- Two actual rejected form reads share one held refresh, then succeed. Cancelled development mount
  attempts may also reach the server; the barrier requires completed 401 responses for both paths,
  not just two started/cancelled requests. Persisted Identity-linked refresh
  rows grow by exactly one with one additional revocation. Accepted and actual-invalid refresh
  responses are held across logout, different-account login and same-account replacement login;
  fresh Authorization equality is asserted as a boolean, never printed. Late success/failure cannot
  replace or clear the newer session. Failed current-session refresh signs out without a retry loop.
- A real UI non-replayable submission with expired bearer receives one 401, no refresh, no replay,
  and unchanged persisted request/audit counts. Employee permission denial receives real 403 with
  no refresh after its fixture role is revoked (restored in finally). Existing full workflow tests
  separately cover successful business transitions.

## Clock boundary strategy

Production still specifies **30 seconds** in [API startup](../../src/HRFlow.Api/Program.cs).
HTTP probes use expiration ten seconds in the past / not-before ten seconds in the future for
acceptance, and 45 seconds outside current time for rejection. The complete matrix must finish
within ten seconds, leaving a strict margin from the skew boundary. No sleeps, exact-equality
assumptions, increased production skew or shortened production lifetime.

Version-matched official [IdentityModel 7.7.1 parameters](https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/blob/7.7.1/src/Microsoft.IdentityModel.Tokens/TokenValidationParameters.cs)
and [lifetime validator](https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/blob/7.7.1/src/Microsoft.IdentityModel.Tokens/Validators.cs)
were inspected: this version uses the system UTC clock rather than exposing a supported injectable
clock through its public validation parameters. Replacing LifetimeValidator would replace the very
validation under test. No reflection/private-clock patch or production seam was added. Exact
30-second equality/frozen-clock permutations remain outside this slice; the retained margins catch
disabled lifetime checks and zero/default-five-minute/excessive skew without wall-clock sleeps.

## Privacy, fixture lifecycle and execution boundary

Only the restricted runtime browser descriptor includes the expired test bearer and disposable
database path; signing keys never enter it. No credentials/tokens/private paths appear in assertions
or reports. Safe browser diagnostics add bounded relative source coordinates only; raw call logs,
screenshots, traces, videos and payload artifacts stay disabled. Independently opened SQLite readers
close in finally; process/fixture teardown is the existing owned-process-before-file-deletion path.
All session/transport barriers release in finally. No scanner installation or external recipient.

Final browser drivers use only UI actions, so they do not require a development-module import or
alternate client/provider. This is real HTTP plus controlled request-header/response timing, not
natural wall-clock expiry. Production-built/browser-device execution is not inferred. All tests are
enabled, retries zero, existing independent CI budgets unchanged.

## Executions, preserved separately

Commands run from repository root unless noted. Windows, .NET 8, Node 24.12.0, installed Edge via
Playwright; private generated synthetic credentials. Fixture-sensitive suites run sequentially.

1. Initial `dotnet build HRFlow.sln --no-restore -v quiet`: **failed**, 31.14 s, two compile errors
   from an incorrect CredentialClaims namespace and two xUnit2031 warnings. Corrected the test import
   and Assert.Single predicates. No production fix.
2. Same build: **passed**, 8.40 s, zero warnings/errors.
3. `dotnet test tests/HRFlow.RegressionTests --no-build --filter FullyQualifiedName~TokenRefreshTests -v minimal`:
   **9 passed / 0 failed / 0 skipped**, 1m33s. This precedes final unsigned-token/default-lifetime/
   refusal-state additions and tighter outside-skew probes; it is not final-source verification.
4. Browser `npm.cmd run typecheck`: **passed**. From `tests/browser`, with
   `HRFLOW_BROWSER_CHANNEL=msedge`, `npm.cmd test -- token-refresh.spec.ts`:
   **8 passed / 1 failed**, 73.619601 s. Refresh/eligible-retry assertions passed; persisted row
   lookup used the Employee profile ID as the Identity user ID and found no matching rows.
5. After changing only GUID case, the same command: **8 passed / 1 failed**, 50.152966 s.
   Case correction did not fix the separate identity mismatch. This is preserved, not labelled green.
6. Fixed the lookup to join Employees.IdentityUserId to RefreshTokens.UserId; strengthened exact
   before/after rotation counts. `npm.cmd test -- token-refresh.spec.ts --grep 'concurrent expired reads'`:
   **1 passed**, 24.502371 s (test 6.836 s). This confirms the narrowed correction, not the full suite.
7. Intermediate solution build: **passed**, 5.83 s, zero warnings/errors.
8. Solution build: **passed**, 17.09 s, zero warnings/errors; separate focused backend command:
   **9 passed / 0 failed / 0 skipped**, 2m08s. Final combined gates below are separate executions.
9. Browser drivers were changed from explicit development-module reads to real form/navigation
   actions; a current-session failed-refresh case was added. The earlier browser executions are
   intermediate-driver evidence, not verification of this final UI-driven version.
10. First UI-driven ten-case invocation: first case **timed out, 60.260 s**; next case **timed out,
    60.245 s**; operator cancelled the run to investigate. No aggregate pass/count/duration inferred
    for interrupted/unexecuted cases. Subsequent independent inspection found **zero API processes**.
11. Added fixed-label diagnostics. Isolated concurrent case: **1 passed**, suite **36.199131 s**,
    test **10.899 s**. This isolated pass did not explain the preceding timeouts.
12. Next UI invocation: first case **timed out, 62.494 s**, with request-opened and two server
    expired-rejected labels but no refresh-entered label. Accepted/logout case **failed, 23.714 s**;
    accepted/same-account case **failed, 22.372 s** at replacement readiness. Run cancelled for
    investigation; no aggregate result claimed. Again zero API processes before the next run.
13. Changed injection from consuming the first request per path to matching the initiating bearer;
    added completed-response barriers, and triggered session cases through the already-mounted
    dashboard refresh button. Full focused run: **8 passed / 2 failed**, **76.783207 s**. Concurrent
    case passed and diagnostics recorded failed mount requests plus four real server rejections and
    one refresh entry. This demonstrates why started/server-side requests alone are not client
    refresh readiness. Exact original cancelled attempts were not reconstructed; no product defect
    or global contention cause is claimed. Two remaining failures were at fresh-token observation:
    a mutable flag could be compared against an unrelated already-in-flight profile response.
14. Paired that assertion with the specific explicitly triggered dashboard/HR-workspace request,
    registering its wait before clicking. Full focused UI run: **10 passed / 0 failed**, **54.013183 s**.
    Assertions still require one shared refresh, successful retries, exact persisted rotation,
    fresh-token equality, non-replay and no refresh on 403. No retries or timeout increases.
15. Before final complete gates, SQLite reader/role-restoration finally scopes were tightened and
    the refreshed-token assertion was narrowed to the named credential-version claim. Permission
    denial now waits for the rendered denial/idle retry control before checking no refresh. The complete
    gates below, not execution 14 alone, verify those final edits.

## Final complete gates

- `dotnet build HRFlow.sln --no-restore -v quiet`: **passed**, **18.38 s**, zero warnings/errors.
- Separate `dotnet test tests/HRFlow.RegressionTests --no-build -v minimal`: **164 passed / 0 failed /
  0 skipped**, **11m59s**. Includes all nine final TokenRefreshTests cases (13 signed bearer probes
  plus the login-issued positive control within one matrix case). This is one complete final backend
  execution, not the sum of focused runs.
- `HRFLOW_BROWSER_CHANNEL=msedge`, `npm.cmd test` from `tests/browser`: **69 passed / 0 failed**,
  **206.306224 s**, retries zero. This is one complete final default-browser execution (the previous
  59 plus ten added cases), not the sum of focused runs. It includes the final SQLite finally scopes,
  permission-denial readiness assertion and all existing activation, evidence, directory, route,
  Manager-calendar and workflow/session regressions. After the backend gate, independent inspection
  found **zero API processes** before starting browser fixtures.
- Browser `npm.cmd run typecheck`: **passed**; final typecheck/review checks are also repeated
  after the complete suite below.

At the implementation handoff, no hosted execution had run and the branch had not been pushed;
publication was not yet authorised. Hosted finalisation results are recorded separately in the PR.
Node 24 emits its experimental `node:sqlite` warning; browser runs are not described
as warning-free. Product frontend build/lint, model checks and dependency audits were not rerun for
this test/documentation-only change: production, schema and all dependency files are unchanged.

## Final review and cleanup checks

After the complete browser suite, `npm.cmd run typecheck` passed again. Independent process-name-
filtered inspection found **zero fixture/API/browser process references**. The initial unrestricted
command-line reference search also matched its own inspection shell; that was not evidence of an
orphan. Existing teardown stopped owned processes before deleting stores. No sensitive fixture data
was collected as a review artifact.

`git diff --check` and strict UTF-8/BOM/replacement-character, EOF/trailing-whitespace, Markdown fence,
relative-link/anchor and targeted JWT/private-key/activation-URL/private-path checks passed across
**eight intended files**, including all four new files. **155 relative links / two anchors** checked.
Both instruction files are byte-identical and unchanged. No production, schema, dependency or CI
diff. These implementation-handoff checks preceded staging, commit and publication. An ignored complete patch includes the new files for local
review, not private runtime descriptors, databases, credentials, logs or generated output.

## Remaining limitations

#76 remains Partial. This is not exhaustive packet loss, natural 15-minute browser ageing, refresh
expiry equality, every authenticated endpoint, production-built chunks, target-host/Linux, scanner,
physical-device/accessibility/load or security penetration coverage. Existing earlier reports remain
dated evidence. No legal approval, production delivery or deployment readiness is inferred.
