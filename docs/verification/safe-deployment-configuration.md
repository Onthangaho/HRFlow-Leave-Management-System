# Safe configuration local verification — issue #81

9 October 2026; `feat/safe-deployment-configuration` from main `d86db0a63ede5fb1e3b64e44af6661cc17b8b25d`, after confirmed merged PR #106. This report describes disposable loopback **Production-mode** execution, not staging, hosting or deployment. Implementation verification used no cloud resources, emails or paid services; PR finalisation is separate and does not close the issue. Temporary tooling/configuration/certificates/databases/logs are excluded from the review diff.

## Executed

- Backend build passed with zero warnings/errors. Offline EF model check found no pending changes; explicit-connection database update reported no migrations applied / already up to date, without API or worker startup. No new migration is required.
- Missing/short/demonstration signing key, copied template placeholder and default-labelled key, missing issuer, insecure Production origin, Development seed configuration, private pickup, missing/public private storage and empty proxy allowlist failed startup. Captured validation output did not contain generated signing material, pepper, certificate password or synthetic account password.
- Valid generated secrets, explicit host/origins, local certificate and absolute existing private storage started the API in Production. /health and /health/ready returned 200; unauthenticated protected access returned 401.
- Allowed HTTPS CORS preflight returned its exact origin; disallowed origin received no allow-origin header. An unconfigured forwarded-HTTPS header over HTTP could not bypass redirection/HTTPS enforcement.
- Production process restart preserved copied synthetic Employee, Identity, role, request, audit, settings and inbox rows exactly. No Development seed or startup migration ran. A valid HR create payload returned 400 at the unavailable invitation-delivery guard and left account counts unchanged.
- Actual Chromium client/API interactions over local TLS: login/logout and own-profile navigation for Employee, Manager, HR-only and combined roles. A correctly signed, expired access token sent on a protected browser request produced one valid same-session refresh and successful retry. Actual password change ended the session; the new password logged in and loaded the protected profile. The browser test ignored only its local certificate's trust error; no application/client TLS bypass was introduced.
- A separate SQLite connection held BEGIN IMMEDIATE while a real protected profile write executed. Response: 409 after **3,149 ms**, with unchanged version/profile; no callback replay or persistence. This is a narrow timeout measurement, not load/capacity evidence.

Initial CORS verification caught incomplete middleware replacement; it was corrected before the successful HTTP/browser runs. Temporary browser tooling also needed a Windows file-URL/startup wait correction and the actual password-submit label/redirect destination. These were verification-tool corrections, not product authentication changes. A manual credential-rotation experiment did not terminate the session because refresh was still valid; it is not counted as password-revocation evidence. The later real password workflow is the relevant evidence.

PR preparation reran backend build and the disposable startup/CORS/untrusted-header/restart checks after tightening known-placeholder/default-secret rejection. The earlier Chromium/password/refresh and 3,149 ms contention results remain recorded evidence; they were not rerun merely for finalisation.

## Limits and outstanding checks

- First-administrator provisioning is documented but not implemented/executed. Approved Production invitation delivery is absent. Both remain deployment gates.
- No hosting selection, public TLS issuance, firewall/proxy installation, cloud persistent-volume durability, production seed absence on a fresh migrated installation, operational backup/restore drill, migration rollback drill or load/soak testing.
- Untrusted proxy behavior was exercised; a real trusted reverse-proxy chain, forwarded client-IP/rate-limit behavior and same-origin SPA fallback were not exercised. Same-origin CORS support is source/config evidence, not hosted-browser evidence.
- Readiness was verified for a migrated reachable database; missing migrations/unreachable-database responses, permission-denied filesystem and reparse/network-mount rejection were not all injected independently. Path checks cannot certify ACL confidentiality or filesystem locking.
- Navigation/profile checks are not full mutation regressions. Submission/approval/reporting/activation/notification delivery were not rerun end to end. Browser password/refresh and HTTP contention are explicitly narrower evidence.
- Frontend source/dependencies did not change; frontend build/lint and dependency audit were not rerun in this backend configuration slice. Existing recorded frontend warnings are unchanged, not newly measured.
- Logs remain sensitive operational artifacts. Keep retention/access controls; no production privacy/security-compliance claim. Future private files are a validated reserved directory only, not implemented uploads.

[Operational contract and placeholder configuration](../operations/single-host-configuration.md). Final diff/secret checks and instruction equality are recorded by the finalisation commands, without claiming additional deployment evidence.
