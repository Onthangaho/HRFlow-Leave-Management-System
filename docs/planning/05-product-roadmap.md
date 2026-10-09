> 9 October 2026: PR #105 notifications is merged. Issue #94 own-profile and working preferences is locally implemented for review: [design](../adr/0014-own-profile-and-preferences.md), [executed verification and limitations](../verification/profile-and-preferences.md). Separate versions, private phone, server theme and delivery-time suppression; no images, email/SMS, commit or deployment. Earlier entries below are historical status.

> 9 October 2026: PR #104 is merged. Issue #93 durable in-app notifications is locally implemented for independent review: see [design](../adr/0013-durable-in-app-notifications.md) and [verification/limitations](../verification/in-app-leave-notifications.md). Preferences remain #94; no email/SMS or deployment. Earlier entries below are historical status.

> 9 October 2026 update: PR #103 is merged. #80 password change/session revocation is locally implemented for independent review on feat/password-change-session-revocation, prepared for PR review and not merged or deployed. See [ADR 0012](../adr/0012-password-change-session-revocation.md) and [executed verification/limitations](../verification/password-change-session-revocation.md). Own-account change revokes prior JWT/refresh generations atomically; broader profile/preferences remain planned. Earlier implementation notes below are dated historical status.

# HRFlow phased South African product roadmap

> Implementation update: PR #102 merged Manager decision notes (#21). Secure HR-created account activation (#79) is locally implemented on feat/secure-account-activation for independent review. Passwordless pending accounts, private Development pickup, one-time token redemption and explicit resend are documented in [ADR 0011](../adr/0011-secure-account-activation.md) and the [verification report](../verification/secure-account-activation.md). Production invitation delivery remains disabled; #79 remains open.

> Current-state update: PR #99 merged on 8 October 2026. Phase 0 item #74 is implemented and evidenced in the endpoint inventory and JWT verification report, but the issue remains open for independent review. References below to disabled lifetime validation describe the original PR #73 planning baseline, not current main. Next recommended gates are #75 legal review and #76 regression-convention approval. This PR publishes documentation only and implements no product features.

Planning baseline: **8 October 2026**, main `f47e20c` after merged PR #73.
Umbrella: [#98 HRFlow South African workplace product roadmap](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/98).
This is the explicitly authorised expansion of the original portfolio MVP, not a claim that
the features below are shipped. No product feature, migration, commit, push or deployment is
part of this documentation phase. Relative sizes replace the obsolete minute/day estimates.

## Plan boundaries and completion rules

- Preserve layered .NET/React architecture, current SQLite protection, versioned writes,
  AuditEntry atomic persistence and account/login-session query isolation. Expired access tokens can use valid same-session refresh
  and one bounded retry; permission loss must not loop. Keep explicitly non-replayable operations
  non-replayable, and isolate old-session completions.
- Employee owns personal leave; Manager has personal leave plus decisions only for eligible
  current same-department reports, never self. HR administers/monitors, **never decides**.
  Combined roles are additive; all capabilities recheck active status/current permissions.
- [Legal requirements](04-south-african-leave-requirements.md) must be reviewed before statutory
  rules are enabled. No full legal/production compliance claim, gender-only parental eligibility,
  generic annual entitlement or fixed-day calendar-month shortcut.
- New issues are complete usable slices (Application/API/database/UI where needed), not a second
  set of isolated backend/frontend tasks. Do not recreate shipped CRUD, reports, deactivation or audits.
- New authorised planning scope includes secure evidence/images, activation, CSV onboarding,
  profile/password/preferences, shell/dashboards, durable notifications and safe exports. Payroll,
  multi-tenancy, hard deletion/reactivation, full department CRUD and persistent sessions remain out.
- Real activation delivery is a reviewed dependency, not a claim that SMTP already exists. In-app
  notifications are the first channel; email/SMS status notifications remain future scope.
- Automated tests remain deferred until #76's explicit convention decision. This phase adds no
  tests. Every implementation still needs real failure/regression evidence and applicable checks.
- Use safe legacy migration with unknown inputs visible, backups and writer shutdown; do not
  silently invent history. Product/operational limits are documented choices, not legal rules.

Full copy-ready issue bodies, priorities, effort, API/database/UI, verification and privacy
contracts are in [the issue catalogue](07-implementation-issue-catalogue.md).

## Existing backlog: proposals awaiting review

[Inventory and exact acceptance-criterion evidence](06-feature-inventory-and-reconciliation.md).

- [#17 balance/cancel](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/17):
  reconcile owner-derived GET/actual POST contract, then recommend closure after review.
- [#36 seeding hardening](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/36):
  existing self-guard/config/README evidence supports closure after review; safe legacy guidance
  replaces unrestricted local deletion. No automatic repair.
- [#21 manager queue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/21):
  reuse for missing decision-note workflow. Proposed **Essential for pitch / Medium**;
  dependency #74. Keep confirmed server outcomes rather than obsolete optimistic wording.
- [#22 policies](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/22):
  reuse for remaining accrual/effective-dated central engine. Proposed **Essential for pitch / Large**;
  dependencies #75 and #78. The fixed DefaultBalance entity does not meet accrual AC.

No existing issue was edited/closed and no project-board item moved during this review.
Approve reconciliation bodies before changing those tracker contracts. The new dependent issues
assume the proposed #21/#22 scopes are accepted; otherwise adjust dependencies before implementation.

## Phase 0 — security and interpretation gates

1. [#74 JWT expiry and uniform current permissions](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/74)
   — **Essential / Medium**, first implementation. Fix `ValidateLifetime=false` and uneven old-token
   membership checks before adding more protected workflows. Audit every route, not just new reports.
2. [#75 qualified leave specification review](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/75)
   — **Essential / Medium**, can start independently. Review actual Van Wyk order/legislative status,
   statutory categories, worked examples, company rules and POPIA decisions.
3. [#76 regression gates and test-deferral decision](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/76)
   — **Essential / Large**, after #74; approve the convention change explicitly, then retain bounded
   permission/concurrency/audit/migration/session tests and extend alongside each slice.

Exit: known expired/revoked/inactive capability gaps fixed and evidenced; legal decisions have
owners, signed-off categories and visible unresolved blockers. No blanket compliance claim.

## Phase 1 — professional foundation and safe operation

4. [#77 shared responsive shell](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/77)
   — **Essential / Medium**, #74. Working role menus/shared styles/accessibility, no dummy controls.
   Local implementation on `feat/responsive-application-shell` now integrates existing routes; see
   [actual browser/persisted-state evidence and limitations](../verification/responsive-application-shell.md).
   Independent review is pending; #77 is not closed or described as merged. Real API-backed role
   dashboards remain #90–#92 work, not shell metrics.
5. [#78 employment numbers/dates/schedules](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/78)
   — **Essential / Large**, #74/#75. Versioned HR forms and honest legacy-unknown states.
6. **Existing #22** — #75/#78. Central rule results/units/cycles/effective dates across submission,
   approval, balances and reports; preserved approval charges; explicit unpaid mode rather than
   zero=unlimited. Historical policy/employee facts remain immutable or honestly unknown.
7. [#79 secure account activation](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/79)
   — **Essential / Large**, #74. No password CSV; reviewed single-use delivery/provisioning.
8. [#80 password change/session revocation](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/80)
   — **Important next / Medium**, #74/#79. Verify old JWT AND refresh invalidation; no assumed
   Identity security-stamp magic for bearer JWTs.
9. [#81 deployment configuration](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/81)
   — **Essential / Medium**, #74. Persistent storage/private configuration, no seed defaults in production.
10. [#82 backup/restore drill](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/82)
    — **Essential / Medium**, #81; repeat with file data after #83. Measure recovery, not just copy files.

Shell, activation and operational work can progress while reviewed calculations are specified;
dependencies are prerequisites, not a promise of parallel solo-developer velocity. Deployment
execution requires its own explicit authorisation. A provider change requires equivalent transaction
implementations/migrations and re-verification, not just connection-string configuration.

## Phase 2 — usable annual leave and core pitch workflows

11. [#85 annual cycles/accrual](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/85)
    — **Essential / Large**, #75/#78/#22. Actual work units/holidays, reviewed rounding and explained charges.
12. **Existing #21 decision notes** — #74; can deliver independently of statutory calculations.
    Optional bounded note by default, rejection requirement only if explicitly agreed. Same existing
    transition/audit transaction, role-scoped history and no raw note logs.
13. [#90 personal dashboard](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/90)
    — **Essential / Medium**, #77/#85. Real balances/Pending/upcoming/history and request action.
14. [#91 Manager workspace/team calendar](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/91)
    — **Important next / Medium**, #77/#90. Personal plus eligible queue/upcoming team; accessible
    calendar/list uses existing scoped data and unions overlaps for any daily people count.
15. [#92 HR workspace/trends/attention](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/92)
    — **Essential / Medium**, #77/#85. Extend reports, explicit periods/units, chart/table, no HR decisions.
16. [#97 synthetic three-role pitch](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/97)
    — **Essential / Small**, #76/#90/#91/#92/#21. Repeatable isolated data, demonstrable real flows
    and honest limitations. #91 is a pitch dependency despite its general “Important next” priority;
    do not claim this complete pitch until that dependency is done or explicitly narrow the demo.

Exit: reviewed annual leave works end-to-end; other categories remain clearly legacy/unsupported
until delivered. Pitch with bounded synthetic scope, not an all-employment-law claim.

## Phase 3 — safe onboarding, evidence and remaining statutory categories

17. [#83 private supporting documents](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/83)
    — **Important next / Large**, #74/#81. Private storage, validation/quarantine/scanning, ownership,
    failures/cleanup and scoped content; medical status only for Managers. Separate file categories.
18. [#84 configurable descriptions/evidence](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/84)
    — **Important next / Large**, #74/#83/#22. NotRequested/Optional/Required, supported conditional
    rules, real dynamic fields and immutable submission snapshots. Legacy requirements stay unknown.
19. [#86 sick cycles and proof/payment review](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/86)
    — **Important next / Large**, #75/#78/#84/#22. Record absence independently of conditional proof/pay.
20. [#87 family/compassionate leave](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/87)
    — **Important next / Medium**, #75/#78/#84/#22. Eligibility/events and labelled enhancements.
21. [#88 parental/adoption/commissioning cases](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/88)
    — **Important next / Large**, #75/#78/#84/#22. Legal blockers first, calendar-month periods,
    family allocation/recovery, no gender-only eligibility or UIF payment promise.
22. [#89 HR CSV onboarding](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/89)
    — **Important next / Large**, #74/#78/#79; independent of documents/statutory-category completion.
    Mapping→preview→confirm, 100 rows/1 MiB initial limits, Ready/Duplicate/Invalid, Identity-normalized
    email including inactive/unlinked records, existing managers only, per-row protected transaction,
    idempotent batch outcomes and safe result downloads. Never overwrite/reactivate or carry passwords.

The scope expands category by category only after signed-off cases and migration evidence; no
unreviewed “all leave types are annual balances” fallback. New private files need #82's restore drill
repeated before they can be called operationally ready.

## Phase 4 — connected convenience and reporting

23. [#93 durable in-app notifications](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/93)
    — **Important next / Large**, #74/#21. Transactional committed events/outbox, idempotent delivery,
    recipient access and safe deep links, not competing auditing or medical payloads.
24. [#94 profile and real preferences](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/94)
    — **Important next / Medium**, #77/#80/#93. HR-controlled details read-only; explicit self-service
    preferred-name/phone allowlist, working theme and delivered-notification preferences.
25. [#95 safe private profile images](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/95)
    — **Future / Medium**, #83/#94. Image reencoding/EXIF/pixel limits, distinct access and retention.
26. [#96 extended reports and safe exports](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/96)
    — **Important next / Medium**, #92/#91/#22. Existing report scopes, correct units/overlaps,
    formula-injection-safe CSV and matching accessible tables.

Dependencies across phases are authoritative; issue creation order is topological, while phase
placement expresses product value. None of the future work is needed to call already shipped CRUD
usable. Re-estimate Large items after design; split only into independently usable slices if required.

## Evidence and review gates

Before implementing a slice: current tracker scope and clean branch/base check; requirements and
legacy-data review; no unrelated edits. Before declaring it done: applicable build/lint/audit/diff,
real permission/failure/browser checks, persisted row/file/audit inspection, multi-connection/process
checks where concurrency matters, synthetic screenshots and explicit unexecuted limitations.
Do not substitute static rendering for interactive UX evidence.

The existing [verification reports](../verification/) remain dated evidence. This planning review
does not claim to rerun them. It confirmed source/tracker state, current PR73 merge, source-backed
requirements and focused issues. No issue closures, board changes, product code or publication of
the documentation branch were performed. Qualified legal interpretation, production load,
formal accessibility and cross-provider safety remain unverified gates.
