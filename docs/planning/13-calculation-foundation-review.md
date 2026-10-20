# Proposed #22 calculation foundation and historical-data contract

**20 October 2026 — proposal only.** Depends on the [canonical requirements](04-south-african-leave-requirements.md),
[decision register](11-south-african-rule-decisions.md) and [pending examples](12-south-african-rule-examples.md).
Issue #22 is the existing foundation issue; do not create a replacement or treat its old entity-only
wording as approval for statutory accrual. This document changes no code/schema or issue text.

## Existing contracts to preserve

[LeaveBalanceCalculator](../../src/HRFlow.Application/Features/LeaveRequests/LeaveBalanceCalculator.cs)
uses current policy entitlement minus Approved inclusive calendar days, per type. Pending reserves
nothing. [Approval handler](../../src/HRFlow.Application/Features/LeaveRequests/Commands/ApproveLeaveRequest/ApproveLeaveRequestCommandHandler.cs)
rechecks current Manager/reporting/credential state, Pending state, balance and overlap inside SQLite
writer protection. Existing [requirements snapshots](../../src/HRFlow.Domain/Entities/RequestRequirements.cs)
are evidence/description facts, not immutable approved charge records. Confirmed start and append-only
fixed weekly revisions exist; holiday calendars, actual-work/pay provenance, cycles and statutory
charging do not. Do not imply those fields already implement #85–#88.

## Proposed input/output boundary

A single framework-free Domain calculation contract should receive immutable inputs supplied by
Application: rule ID/version/authority and effective interval; verified applicability/agreements;
confirmed employment/cycle anchors; requested local dates; effective schedule and holiday/exchange
versions; qualifying actual-work/paid inputs with provenance; approved charges/reversals and evidenced
opening adjustments; supported minimal category/event facts. Never derive actor/employee from an
untrusted calculation payload. Layered Infrastructure retrieves persistence; no HTTP/EF in Domain.

Proposed result:

- Status Calculated / NeedsReview / Unsupported / InsufficientInputs, with stable explanation codes.
- Rule/source/input versions, business-date basis, as-of UTC, per-date breakdown and cycle partition.
- Separately labelled calendar span, scheduled minutes, charge units and qualifying accrual units.
  Preserve exact rational or approved decimal precision; include approved rounding method/boundary.
- Entitlement earned/granted, approved debits, reversals, opening adjustments and available units.
  Never sum mixed hours/days or incompatible rule pools into one balance.
- Separate absence/decision eligibility, evidence sufficiency, employer-pay review and informational
  UIF status; calculation output is not a benefit award or medical decision.

Dates use Africa/Johannesburg; events/audits UTC. Effective-from inputs need a nonoverlapping interval
and stable revision. Recorded-at differs from effective-at. Applicability by absence date, event date
or transitional decision date requires L01/L02/L07 approval; engineering must not pick one silently.
Expose missing input names/provenance safely, never diagnoses or raw evidence in broad outputs.

## Approval, charges and consistency

Proposal: preview at submission without a statutory Pending reservation initially (C02). Preserve
submitted requirement snapshots. At approval acquire existing SQLite writer reservation **before**
authoritative reads; recheck credentials, current eligible Manager relationship, no self-decision,
Pending status, input/rule versions, available entitlement and legally incompatible overlaps. Re-run
the central calculation over protected input facts. Explain differences from preview; do not add new
submission-evidence demands from a later type edit. If facts cannot be calculated, provide reviewed
absence/HR resolution semantics rather than pretending zero or auto-approving pay.

Successful approval atomically persists status, exactly one existing AuditEntry, immutable charge
components and notification outbox event. Proposed charge components reference request/decision,
cycle/pool, rule/input versions, per-date units, rounding and explanation. Use a uniqueness boundary
per decision/component to prevent duplicate debit. They are accounting facts, **not a second audit
system**. Failure rolls all back, and callbacks are never automatically replayed.

A later policy/schedule edit affects future applicable evaluations, never changes a posted charge.
Current authenticated deferred snapshots serve balances/dashboards/reports/exports from the same
central results; display request counts separately. Existing clipped calendar request-day metrics
can remain, labelled as such. A statutory charged-unit report needs explicit date allocation rules,
unit columns and reconciled totals, not reinterpretation of existing calendar-day columns.

Pending owner/HR-deactivation cancellation retains existing transitions and produces no refund when
nothing was reserved. Future Approved amendments/cancellation require separately authorised workflow,
linked immutable reversal and replacement; no deletion or negative balance repair by side effect.
Current HR still cannot approve/reject. Deactivation preserves approved historical charges. C02
blocks unreviewed restoration of entitlement for already taken leave or conflicting statutory pools.

## Safe cutover options and consequences

1. **Preferred proposal, C03:** prospective reviewed rule cutover plus evidenced opening entries,
   separated from legacy calendar history. HR confirms source/provenance, employee correction/dispute
   path and effective date. Reconciliation is explicit, not a fake historical decision or submission.
2. Keep affected employees/categories on clearly labelled legacy mode until required history is
   confirmed. Consequence: statutory balance remains unavailable, not claimed compliant.
3. Documented manual reconstruction from reliable records with reviewer approval. Preserve original
   approvals alongside adjustment provenance; never infer old schedules from current revisions.

No account-creation-derived hire dates, zero-default missing balances or automatically recreated
holiday histories. Existing calendar totals are observable derivations, not proof of historically
charged units. LegacyUnknown is a proposed future provenance state, not a field present today.
No retroactive recalculation. Backup and dry-run exceptions before migration; rollback of new charge
records loses later accounting facts and needs reconciliation, not blind downgrade in a live store.

## Dependency-ordered implementation proposal

1. **#75 gate:** assign legal/company/privacy owners; approve L01/L02/L05, C02/C03 and P01/P02 for the
   first supported category. Retrieve missing sources; freeze approved boundary examples. Blocked now.
2. **Remaining #78 inputs:** approved versioned holiday/proclamation/exchange source; minimal rule/
   cycle/applicability and actual-work provenance where needed. Fixed schedules/start facts already
   exist; no rotating-shift or attendance engine by assumption. #78 is Closed but the criterion remains.
3. **#22 first usable calculation slice:** one approved category, central preview/result explanations,
   atomic approval charge posting, explicit unknown legacy handling and balance UI together. Keep
   legacy contract side by side until reviewed cutover; avoid unusable backend-only rollout.
4. **#85 annual workflow:** approved methods/cycles/fractions and reconciled balances/reports. Extend
   retained SQLite/concurrency/browser gates, with expected results from signed-off examples only.
5. **#84/#86 sick workflow:** actual-work provenance and absence/proof/pay separation before conditional
   Medical requirements. Missing proof cannot prevent recording. Maintain medical status-only Managers.
6. **#87 family and C01 enhancements/unpaid:** eligibility, events, part days and separate company pools.
7. **#88 parental cases:** only after L07–L11 allocation/transition/UIF/notice questions resolved; no
   gender-only eligibility. Recheck live court/remedial status immediately before implementation.

Each slice must keep current role/credential protection, version conflicts, session-isolated query
keys, immutable requirement history and existing audit/outbox semantics. No legal rule becomes live
just because a technical schema or example is available.

## Required later verification, not executed here

Approved-example unit/boundary tests; file-backed migrated SQLite charge/audit/outbox rollback;
two-process competing approvals, policy/schedule changes and cancellations; legacy before/after and
backup/restore comparisons; actor/medical access; consistent balance/dashboard/report/export totals;
real owner/Manager/HR workflows and stale session responses. No new calculation tests are written in
this research branch. Existing maintained #76 convention remains authorised, not reverted to deferral.
