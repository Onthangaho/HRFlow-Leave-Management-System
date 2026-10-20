# South African leave requirements and review baseline

Retrieved/checked **20 October 2026** for #75. This updates the 8 October research baseline;
this file remains the requirements source of truth. It is **not legal advice, qualified approval,
#75 completion or authority to activate statutory calculations**. Every implementation below is
proposed until its named decisions and applicability are approved. No reviewer has been assigned.

Review pack:

- [Decision register and sign-off checklist](11-south-african-rule-decisions.md).
- [Dated worked examples, all pending review](12-south-african-rule-examples.md).
- [Dependency-ordered #22 contract and historical migration proposal](13-calculation-foundation-review.md).
- [Source/repository verification and retrieval limits](../verification/south-african-rule-review.md).

## Plain-English change from the current product

Today HRFlow subtracts all Approved inclusive **calendar days** for a type from its current configured
integer entitlement. Pending does not reserve balance. Employment facts and fixed weekly schedules
are recorded, but do not drive charges. Submission evidence requirements are snapshotted; statutory
charge records, accrual, cycles, holiday sources and proof/payment adjudication are not implemented.
See [calculator](../../src/HRFlow.Application/Features/LeaveRequests/LeaveBalanceCalculator.cs),
[approval](../../src/HRFlow.Application/Features/LeaveRequests/Commands/ApproveLeaveRequest/ApproveLeaveRequestCommandHandler.cs)
and [weekly revisions](../../src/HRFlow.Domain/Entities/WeeklyScheduleRevision.cs).

A reviewed future model needs different calendars, cycles and qualifying facts for different rules.
A month is not a fixed number of days; scheduled time is not actual worked time; time off is not a
promise of employer pay or UIF benefit. None of those changes is made in this documentation phase.

## Source register and authority

Research-phase retrieval/attempt dates below are **2026-10-20**; finalisation reviews that evidence, not a new retrieval of every source. Publication is not commencement. Primary instruments
control over explanatory pages; company enhancements are separate. Government's original BCEA PDF
is not a current consolidation. Sources assembled here are not a certified exhaustive legal update.

### S01 — Basic Conditions of Employment Act 75 of 1997 (BCEA)

[Government text, Gazette 18491, 5 December 1997](https://www.gov.za/sites/default/files/gcis_document/201409/a75-97.pdf)
and [government amendment/commencement register](https://www.gov.za/documents/basic-conditions-employment-act).
**Operative legislation as amended**: ss3, 6, 18–24, 26–27, 31, 40, 49–50. The register gives
1 December 1998 for remaining provisions, including leave (Gazette 19453, 13 November 1998);
other sections commenced earlier. The underlying 1998 commencement notice was not independently
retrieved in this pass. Applicability includes Chapter III s19's under-24-hours/month exclusion;
s6 earnings/work-time exclusions are not a blanket exclusion from Chapter III. Review sector,
collective agreement, employee status and lawful variations before applying a rule (L01).

### S02 — Amendments and commencement, not a guessed consolidated text

[Basic Conditions of Employment Amendment Act 11 of 2002](https://www.gov.za/sites/default/files/gcis_document/201409/a11-02.pdf),
published Gazette 23539, **24 June 2002**; [register](https://www.gov.za/documents/basic-conditions-employment-amendment-act)
gives **1 August 2002** commencement (Gazette 23610, 25 July 2002). Relevant s2 replaces BCEA s8's
working-time interpretation; s4 corrects BCEA s27(5) reasonable proof before payment to refer to s27(2). Neither is authority to treat parental calendar months as workdays.

[Labour Laws Amendment Act 10 of 2018](https://www.gov.za/sites/default/files/gcis_document/201811/42062gon1305act10of2018.pdf),
Gazette 42062, **27 November 2018**, ss2–5 amend parental/family/variation provisions;
s4 removes childbirth from s27(2)(a). [Commencement proclamation, Gazette 42925, R1699,
23 December 2019](https://www.gov.za/sites/default/files/gcis_document/201912/42925rg11021gon1699.pdf)
brings ss1–7 into operation **1 January 2020**. [Government register](https://www.gov.za/documents/acts/labour-laws-amendment-act-10-2018-english-afrikaans-27-nov-2018)
records separate UIF-related commencement phases (1 March/1 November 2019 and 1 April 2020);
these must not be collapsed into the BCEA date. The 2018 pre-court parental durations are subject
to S03. Government lists additional BCEA amendments; complete consolidation/applicability review
remains L01, not an inference that only the listed enactments exist. Its linked SAFLII 2020
consolidation returned 403; no unavailable consolidation is claimed to have been read.

### S03 — Van Wyk [2025] ZACC 20, CCT308/23 and CCT309/23

[Full Constitutional Court judgment and operative order](https://collections.concourt.org.za/bitstreams/efc9dec0-7c6b-45f9-ae87-e231eb7e866f/download),
delivered/published **3 October 2025**. **Interim court order as retrieved**, effective from that order pending
remedial legislation: order **2–5**, PDF pp3–8; order **6–7** reporting/supplementary-relief mechanism.
The reasoning on relief/UIF is at PDF pp33–36; the full order, not the Court media summary, was read in the research phase. Later legislative/order reconciliation remains blocked in L01/L07; this is not a certified statement of all currently operative text.
Invalidity is suspended 36 months. Track the 3 October 2028 anniversary and preceding six-/four-month
report/application windows, subject to counsel's deadline calculation and any later order (L07).
There is no equivalent interim UIF reading-in. Adoption age is expressly unresolved: order 3,
order 4 and order 5(c)'s retained s25B(1) wording must be reconciled, not silently edited by software.

### S04 — Proposed Labour Law Amendment Bill, 2025 and subsequent-status search

[Official draft notice landing page](https://www.gov.za/documents/notices/labour-law-amendment-bill-draft-comments-invited-26-feb-2026),
[Gazette 54220, notice 3801, 26 February 2026](https://www.gov.za/sites/default/files/gcis_document/202602/54220gen3801.pdf),
and [Department announcement, 27 February 2026](https://www.labour.gov.za/Media-Desk/Media-Statements/Pages/Labour-Law-Amendment-Bills-and-Notice-published-by-the-Department-benefitting-Employees-and-Workers--.aspx).
**Proposed change, no established operative date.** Landing page and announcement retrieved;
full Gazette and Department mirror failed direct retrieval. Exact Bill clause reconciliation is
therefore **unresolved**. Announcement's adoption expansion is a proposal, not an implemented age gate.

Targeted government/Department/Parliament and Court searches for 2026 remedial enactment,
commencement and supplementary Van Wyk orders found no later operative instrument. A retrieved
[Parliament record, NCOP Hansard, 2 June 2026, p48](https://www.parliament.gov.za/storage/app/media/Docs/hansard/01mfgh4vtbu2eew3dswralh3h6mrkyuxrr.pdf)
describes a public-comment Bill; it is not an Act or proof of status in October. Absence from these
searches is **not proof that no development exists**. L01/L07 require an updated legislative/court
status check and missing primary instruments before approval/release; do not activate proposed text.

### S05 — Department explanatory guides

[Annual](https://www.labour.gov.za/DocumentCenter/Pages/Basic-Guide-to-Annual-Leave.aspx),
[sick](https://www.labour.gov.za/DocumentCenter/Pages/Basic-Guide-to-Sick-Leave.aspx),
[family responsibility](https://www.labour.gov.za/DocumentCenter/Pages/Basic-Guide-to-Family-Responsibility-Leave.aspx),
last modified **6 August 2012**. **Guidance, no independent commencement**. Read sections Number,
Timing, Proof/Application. The family guide still includes childbirth and simplifies service
eligibility: S02 and actual s27 prevail. Older maternity guidance cannot displace S03.

### S06 — Public Holidays Act and approved calendar provenance

[Public Holidays Act 36 of 1994](https://www.gov.za/sites/default/files/gcis_document/201409/act36of1994.pdf),
Gazette 16136, **7 December 1994**, ss2, 5–6/Schedule 1; effective **1 January 1995**.
[Amendment Act 48 of 1995](https://www.gov.za/documents/public-holidays-amendment-act), ss1–2 insert
s2A proclamation power; government gives **4 October 1995** commencement (publication date
not independently confirmed here). **Operative legislation**. [Government 2026/2027 holiday list](https://www.gov.za/about-sa/public-holidays)
retrieved; page publication date unstated, list is guidance, not the legal instrument for every
special holiday. A complete year/proclamation/territorial/exchange audit is still L05 and #78's
remaining holiday-source criterion, despite that issue's Closed tracker state.

### S07 — UIF distinction and missing benefit consolidation

[Unemployment Insurance Act 63 of 2001 register](https://www.gov.za/documents/unemployment-insurance-act),
intended review of ss24, 26A, 27, 29A as amended. Direct page and linked retrieval timed out in
this pass; original publication/commencement and current benefit rules are **not newly verified**.
S02 confirms separate amendment commencement; S03 order 2–4 and reasoning PDF pp35–36 explicitly
separate the UIF remedy. **Historically enacted scheme; current consolidation and applicability unresolved**. No current benefit amount,
duration, contribution or payment eligibility is certified by this pack (L11).

### S08 — Privacy and retention

[POPIA 4 of 2013, Gazette 37067, 26 November 2013](https://www.gov.za/sites/default/files/gcis_document/201409/3706726-11act4of2013popi.pdf),
ss8–25, 26–27, 32, 34–35, 71–72. [Government commencement register](https://www.gov.za/documents/protection-personal-information-act):
core processing provisions **1 July 2020** (Gazette 43461, 22 June 2020), further stages separately.
**Operative legislation**. [Information Regulator guidance on special personal information,
28 June 2021](https://inforegulator.org.za/wp-content/uploads/2020/07/InfoRegSA-GuidanceNote-Processing-SpecialPersonalInformation-20210628.pdf),
paragraphs 2.1–2.4 and 4.1 (general exceptions versus Regulator authorisation): **guidance**, not standalone employer consent or a
retention period. Review ss10–14 purpose/minimisation/retention, ss19–22 security/operators/incidents,
s32 employer necessity/confidentiality, and children's information for parental cases (P01/P02).
No blanket diagnosis collection or permanent retention is authorised.

## Rule contracts for qualified review

Each contract must return separate **absence record**, **decision eligibility**, **entitlement/charge**,
**evidence status**, **employer-pay review** and **UIF information status**. Missing inputs return
Unknown/NeedsReview with reasons, not zero or automatic refusal to record absence. Proposed future
contracts do not bypass current authenticated permissions or activate new statutory workflows.

### Annual leave: cycle, accrual and charging

A01 (#22/#85; S01 ss20–21): employment-anchored twelve-month cycles, not January resets;
21 consecutive days minimum, or agreed 1:17 qualifying days or hours. Record agreement/provenance
and exact units; no silent hourly/day conversion. Qualifying annual inputs include worked **or
entitled-to-pay** units. Section 20(4) requires granting leave within six months after cycle end;
that text alone does not settle carry-forward/forfeiture disputes. Those conclusions require L02 review, including relevant case law not comprehensively retrieved here. Grant timing, public-holiday adjustment, occasional paid leave, concurrent
leave/notice restrictions and termination-only cash substitution require explicit outcomes under
ss20(4)–(11), 21/40. Do not implement automatic year-end expiry while L02 is blocked. Fifteen/five-day-week and eighteen/
six-day-week figures are explanatory schedule equivalents, not universal stored constants.

Inputs: confirmed service/cycle anchor, applicable rule/agreements, schedule/holiday revisions,
qualifying time/pay records and opening history. Output per-date/unit explanation and cycle totals.
Carry-forward, fractional precision, service interruption, variable schedules and conversion between
accrual methods remain L02/L05. Existing HRFlow schedules are not proof of worked/paid hours.

### Sick leave and proof/payment

SICK01 (#22/#86; S01 ss22–24): 36-month employment cycles; normal six-week work pattern;
first six calendar months use actual-worked-day provenance and 1:26 entitlement, with first-cycle
use accounted for once. Agreement variants under s22(6) need explicit lawful review. An employer
proof request under s23 is distinct from a submission upload setting; absence beyond two consecutive
days or more than two occasions in eight weeks is not a universal certificate prerequisite.
Issuer qualification, incapacity/duration, reasonable assistance on employer premises and occupational
compensation exceptions require review (L03/L04). Evidence sufficiency is not a diagnosis field.

Accept recording while proof/pay is unresolved in the future supported workflow; expose HR review
and safe owner status. Manager approval is not certification of medical proof or payroll payment.
Current Required Medical submission stays unsupported until this distinction can work end to end.

### Family responsibility and enhancements

FAM01 (#87; S01 s27 as amended by S02 s4): more-than-four-month service and at-least-four-day
weekly eligibility; three paid days each annual cycle; qualifying sick-child/death relationships,
part days, reasonable proof and lapse/collective variation must be approved (L06). Childbirth is
not the superseded statutory family event. Company compassionate leave may cover other events,
relations or schedules through separately identified C01 enhancements, never a relabelled minimum.
Death relationships in s27(2)(c) are spouse/life partner, parent/adoptive parent, grandparent,
child/adopted child, grandchild or sibling. Collect categories, not unnecessary relatives' names or diagnoses.

### Parental, birth recovery, adoption and commissioning parents

PAR01 (#88; S03 order 5): distinguish single/sole-employed from two-employed-parent allocation;
calendar-month periods, shared pool and each person's protected recovery must remain separate.
Exact interim clauses: substituted s25(1), (2)–(4D), (5)–(7); deleted s25A; substituted
s25B(1)–(7) and s25C(1)–(7). Record notice and event dates, minimal responsibility/allocation
facts and external-employer declaration provenance. Gender alone is never an eligibility gate.

The retrieved order grants single/sole-employed cases at least four consecutive calendar months;
two employed parties share four months plus ten days, including birth preparation/recovery (s25(4A)).
Section 25(4B) permits agreed concurrent/consecutive sharing with a single consecutive sequence per
party; (4C) supplies near-half division on disagreement and its four-month completion condition.
Do not apply the s25 sequencing language mechanically to differently worded adoption/commissioning
clauses without L08 approval. These sourced durations are not a reviewed numeric allocation engine.

Time-off start, birth-related fitness protection, third-trimester loss, s26 safe-work duties,
continuity, concurrency, unequal/agreed allocation, disagreement deadline and one-month versus
four-week notice are separate validations. Exceptional notice remains possible. Adoption starts
at earlier competent-court placement/order; commissioning cases use birth. Do not use birth date
for adoption placement or infer parental responsibility from surname, gender or marital status.
The birth-related pre-birth window is four weeks (unless otherwise agreed/medical necessity);
the post-birth no-work period is six weeks unless fitness is certified; third-trimester miscarriage
or stillbirth carries six weeks' protection. Section 25 notice is four weeks; s25B/C use one month,
with impracticability exceptions. A general paid benefit does not follow from any of these periods.

L07–L10 block executable allocation: adoption-age conflict; differing wording of continuity in
s25 versus s25B/C; mixed employed/self-employed cases; calendar-month pooling and near-half division;
completion window; transitions across rule dates; and external allocation verification. S03 contains
no automatic matching UIF entitlement (L11). Section 26 hazardous/night-work support is not a leave balance.

### Schedules, holidays, units, unpaid leave and legacy truth

SCH01 (S01 s18/20(8), S06): evaluate each local date using the effective fixed weekly revision and
versioned applicable holiday/exchange. Public-holiday Sunday/Monday and special proclamations need
provenance. No automatic Saturday-to-Monday substitution. Split schedule changes by effective date;
never rewrite approved historical charges. Existing fixed minutes do not represent rotating,
overnight, on-call or variable patterns; unsupported patterns return NeedsReview (L05).

Use calendar dates in Africa/Johannesburg; events/audits retain UTC. Keep elapsed calendar span,
scheduled minutes, charge days, qualifying accrual units and distinct people separate. Preserve
exact fractional values until approved rounding; round only at an agreed boundary, never repeatedly
per request without review. Resolve leap anniversaries and month-end arithmetic in L02/L05/L08.

C01 **company-policy option**: unpaid absence is explicit no-entitlement-consumption, not a zero
balance (today zero blocks positive days). No automatic employer pay or UIF award. Agreements,
authorisation and interaction with BCEA s20(6) need review; company-paid enhancements retain their
own effective dates/floors. Unknown legacy dates, schedules, agreements, actual-worked records and
parental facts remain Unknown. Account creation never supplies employment commencement.

## Privacy, ownership and review checklist

Keep owner/authorised-HR medical content access and eligible Manager **status-only** access, including
safe metadata. HR configures/monitors and reviews evidence/pay questions; it gains no request
approval/rejection authority. Combined roles remain additive; no self-decision. Read snapshots and
protected writes continue to check active/activated Identity capabilities and credential versions.

P01/P02 require Information Officer/privacy and labour-law review of lawful basis, minimal family/
third-party facts, notices, access/correction, medical confidentiality, record-class retention,
legal holds, backup expiry and secure deletion. No arbitrary retention duration or automatic bound-
evidence purge is proposed. Leave-state AuditEntry remains the transition mechanism; document access
records are not fake state transitions. No medical contents enter logs/notifications/summary exports.

Reviewer checklist: verify S01–S08 and missing instruments; decide L01–L11, C01–C03 and P01–P02;
annotate each example; approve the #22 units/history contract; record applicability/effective dates,
exceptions and transition plan; obtain independent legal/company/privacy sign-offs in the register.
All reviewer identities, dates and signatures remain Unassigned/blank. No reviewer was contacted.
