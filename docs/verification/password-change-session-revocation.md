# Password change and session revocation verification

Executed 9 October 2026 (Africa/Johannesburg) on feat/password-change-session-revocation,
base 0e9b8defa9e496ff5d87de8484762934871b5ca9. PR #103 was confirmed MERGED. Initial working
tree was clean. These checks were performed during implementation, before PR finalisation.
No deployment or issue closure was performed.

## Method

Generated private credentials/configuration, two API processes sharing a disposable SQLite
file, separate HTTP connections, and persisted Identity/refresh/profile/request/audit queries.
Temporary scripts and databases are excluded from the diff. Chromium interaction used the
real Vite client and API, not static rendering or an Axios substitute. Synthetic addresses use
hrflow.local. Pickup secrets were read privately only for the activation regression.

To reproduce: provision isolated seeded Employee, Manager, HR and combined accounts; log in
twice per account; retain both pairs privately; POST /auth/password using one pair; inspect
PasswordHash/CredentialVersion and refresh revocation without printing values; call protected
GET and refresh with every old pair; log in with old/new passwords. Use separate API processes
for races and a disposable SQLite abort trigger on CredentialVersion for late rollback.
Never run this verification against existing employee data or publish private values.

## Executed HTTP and persisted-state evidence

- Employee, Manager, HR-only and combined Manager/HR: password change 204; both previous
  access tokens rejected 401 across processes; both previous refreshes rejected 401; old
  password login 401, new password login 200. Fresh JWT claim matches persisted generation.
- Wrong current password and weak replacement 400: password hash, Identity stamps, generation
  and all refresh rows unchanged. Oversized 257-character input 400; eleventh attempt within
  the per-process/IP window 429.
- Validly signed missing, malformed and stale credential claims rejected 401; signature
  tampering rejected. Current valid generation reaches the authenticated selector 200.
- Injected late generation-write failure 500: Identity password/stamps and refresh revocation
  rolled back; prior password/session remained usable. No extra leave audits.
- Two API processes changing with one old session: exactly one 204 and one 401; committed
  password logs in and all prior proof is rejected.
- Concurrent refresh/change: refresh 200 before change 204; its newly returned access and
  refresh pair both rejected afterward. Concurrent old-password login/change: login 200
  before change 204; returned access proof rejected after commit.
- Real external writer contention: password change 204 and competing submission 200 with
  exactly one request/submission audit, valid ordering before revocation. Separate real race,
  change started first and submission 20 ms later: change 204, submission 401, zero request/
  audit delta. This verifies safe outcomes; precise lock acquisition timing was not instrumented.
- Additional deterministic boundary simulation: hold BEGIN IMMEDIATE, begin submission,
  rotate generation inside that reservation, release; submission 401, no request/audit writes.
  This is an injected generation rotation, explicitly separate from actual password changes.
- Injected pending/inactive profile/account states reject login and old JWT use. Removed
  current Employee membership rejects old-token submission 403 without persisted changes.
- HR account creation/resend/private pickup/activation/login regression passed; previous
  invitation rejected and activated login includes current generation.
- Real submission, Manager approval with note, owner cancellation, balances, history and
  owner/HR timeline calls passed. Approval and cancellation have exactly their submission
  and transition audits; HR/Employee direct decision attempts 403.
- Employee/policy/type/monitoring/department-report/Manager-team reads returned 200 using
  their actual existing routes. Password operations preserve lifecycle/reporting/history.

## Executed browser evidence

- All four role combinations reach Account → Change password.
- Desktop 1440×960 and mobile 390×844: no page overflow; visible labelled fields; keyboard
  Tab order current → new → confirmation. Confirmation mismatch and incorrect current
  password show errors; recoverable failure retains input.
- Two clicks produce exactly one POST. Real successful change removes protected shell,
  reaches login with generic success notice and accepts normal new-password login.
- Aborted request shows uncertain-outcome guidance with retained input, no automatic replay.
- Injected 401 produces one POST, no refresh/replay, and clears the session.
- Real committed response delayed across same-account relogin and different-account login:
  replacement session remains; no old success notice or protected content appears.
- Actual leave form submission and Manager approve/note dialog persisted one submission and
  one decision audit. Timeline was subsequently checked through the real browser using its
  prefixed note text. HR edit persisted; policies, monitoring and reports loaded by navigation.

Temporary verification errors were corrected: MVC record metadata initially caused 500 and
was fixed; the HTTP helper needed existing plain-text refresh-error parsing; browser selectors
needed async type-load waiting and prefixed note matching. A database-copy verifier was changed
from raw file copying to SQLite VACUUM INTO to include WAL, and row comparison was corrected
for SQLite null-prototype objects while suppressing private values. Successful reruns are the
evidence above; failed/unfinished harness attempts are not additional passing scenarios.

## Migration and builds

Disposable down/up snapshot: five profiles, three requests and six audits. Down removes only
CredentialVersion and preserves all other Identity, activation, role, profile, request, audit
and refresh fields. Upgrade assigns distinct nonzero random generations and revokes previously
outstanding refresh rows; all unrelated fields and historical revoked timestamps are unchanged.
This snapshot preceded additional contention verification requests.

- dotnet build HRFlow.sln: passed, zero warnings/errors.
- dotnet ef migrations has-pending-model-changes: no changes since last migration.
- Frontend build: passed, 818 modules; main JS 512.23 kB / 154.94 kB gzip (baseline recorded
  in #79: 509.13 / 154.19 kB). Existing Vite >500 kB warning remains.
- Frontend lint: exit 0; existing useAuth Fast Refresh warning remains.
- Dependencies unchanged; npm audit not rerun.
- git diff --check and separate new-file whitespace checks passed for 32 intended files.
  Both instruction files are byte-identical; 20 private generated values were absent from
  intended source/docs/images. The index was empty at the end of implementation verification;
  temporary artifacts/build output excluded.

## Synthetic screenshots

- [Desktop form](screenshots/password-change/desktop.png)
- [Mobile form](screenshots/password-change/mobile.png)
- [Success at login](screenshots/password-change/success.png)

Fields are empty in form screenshots; no activation links, tokens, passwords or real employee
information. Browser chrome/local machine paths are absent.

## Limitations and unexecuted checks

No production deployment/downgrade rehearsal, exhaustive stress/interleavings, distributed
rate-limiter/perimeter checks, browser matrix, formal screen-reader/contrast audit or cross-provider
verification. Token expiry/clock boundary suite from #74 was not rerun; its settings are unchanged.
Delayed refresh-completion scenarios from #74 were not repeated; password-response session
boundaries were exercised as described above. Inactive/pending denial used injected disposable
states rather than a new concurrent deactivation
suite. Permission revocation during contention for every individual legacy route, and combined
role removal variations, were not exhaustively rerun. Direct-report self/cross-department denial
was source-reviewed rather than re-exercised in this slice. Full policy CRUD, employee deactivation,
resend-browser and every HR dialog regression were not repeated. In-flight deferred-snapshot/WAL
completion and remote-tab visual clearing were not interaction-instrumented; subsequent old proof
is denied, not instant cross-tab UI clearing. Automated test files remain deferred under #76.
