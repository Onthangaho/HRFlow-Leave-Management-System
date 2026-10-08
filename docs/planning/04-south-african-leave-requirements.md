# South African leave requirements: research baseline

Checked **8 October 2026**. This is a source-backed engineering specification for review,
not a legal opinion or a claim that HRFlow complies with South African employment law.
No calculation implementation is authorised by this planning exercise. A qualified
labour-law reviewer must approve applicability, unresolved interpretations and examples
before statutory rules are enabled. Recheck sources before each implementation/release.

## Authority and change control

Distinguish enacted legislation, operative court-ordered interim text, proposed Bills and
company enhancements. Store those distinctions, source sections, effective dates and review
status in the eventual rule catalogue. An HR setting cannot reduce an applicable minimum.
Collective agreements, bargaining-council/sectoral rules and employee applicability require
review; the working-time earnings threshold is not a blanket exclusion from statutory leave.

Primary sources consulted (all checked on the date above):

- **BCEA 75 of 1997:** [government Act and amendment register](https://www.gov.za/documents/basic-conditions-employment-act),
  [original Department text](https://www.labour.gov.za/DocumentCenter/Acts/Basic%20Conditions%20of%20Employment/Act%20-%20Basic%20Conditions%20of%20Employment.pdf).
  Chapter III, ss19–27; s18 public holidays; ss31/40 records/termination; ss49–50 variations.
  The leave provisions commenced **1 December 1998**. The original PDF is NOT a current
  consolidation: it retains birth under family responsibility and obsolete UIF references.
- **Labour Laws Amendment Act 10 of 2018:** [Act and commencement register](https://www.gov.za/documents/acts/labour-laws-amendment-act-10-2018-english-afrikaans-27-nov-2018),
  [Department-hosted amendment text](https://www.labour.gov.za/DocumentCenter/Acts/Basic%20Conditions%20of%20Employment/Labour%20Laws%20Amendment%20Act%2C%2027%20November%202018.pdf),
  [ss1–7 commencement proclamation](https://www.gov.za/sites/default/files/gcis_document/201912/42925rg11021gon1699.pdf).
  BCEA parental amendments commenced **1 January 2020**; UIF-related provisions have
  separate commencement dates. Its s4 removes childbirth from BCEA s27(2)(a).
- **Van Wyk [2025] ZACC 20**, CCT308/23 and CCT309/23, **3 October 2025**:
  [actual Constitutional Court judgment/order](https://collections.concourt.org.za/bitstreams/efc9dec0-7c6b-45f9-ae87-e231eb7e866f/download),
  [Court landing page/media summary](https://www.concourt.org.za/index.php/judgement/617-a-werner-van-wyk-and-others-v-minister-of-employment-and-labour-b-commission-for-gender-equality-and-another-v-minister-of-employment-and-labour-and-others).
  Use **order paragraphs 2–5**, not the non-binding media summary as substitute legislation.
  Invalidity is suspended for 36 months from judgment; the BCEA interim reading applies
  pending remedial legislation. The order does **not** provide an equivalent UIF reading-in.
- **Proposed Labour Law Amendment Bill, 2025:** [Gazette 54220, notice 3801, 26 February 2026](https://www.gov.za/sites/default/files/gcis_document/202602/54220gen3801.pdf),
  [Department announcement](https://www.labour.gov.za/Media-Desk/Media-Statements/Pages/Labour-Law-Amendment-Bills-and-Notice-published-by-the-Department-benefitting-Employees-and-Workers--.aspx).
  These sources describe a public-comment Bill, **not enacted replacement rules**. Searches
  of government, Department and Parliament sources found no subsequent commencement
  instrument in this review; that is not exhaustive legislative-status certification.
- **Unemployment Insurance Act 63 of 2001:** [Act/amendment register](https://www.gov.za/documents/unemployment-insurance-act),
  commenced **1 April 2002**; parental-related provisions amended in 2018 with separate
  commencement dates. Review current ss24/26A/27/29A alongside Van Wyk. Benefit administration
  and contribution conditions are distinct from BCEA time-off rights.
- **Department guides:** [annual](https://www.labour.gov.za/DocumentCenter/Pages/Basic-Guide-to-Annual-Leave.aspx),
  [sick](https://www.labour.gov.za/DocumentCenter/Pages/Basic-Guide-to-Sick-Leave.aspx),
  [family responsibility](https://www.labour.gov.za/DocumentCenter/Pages/Basic-Guide-to-Family-Responsibility-Leave.aspx).
  These pages are dated **6 August 2012**; use as explanatory guidance only. The family guide's
  childbirth reference is superseded by the 2018 amendment. Older maternity guides likewise
  cannot override Van Wyk.
- **Public Holidays Act 36 of 1994:** [Act, s2 and schedule](https://www.gov.za/sites/default/files/gcis_document/201409/act36of1994.pdf),
  effective **1 January 1995**; [official holiday list](https://www.gov.za/about-sa/public-holidays).
  [Amendment Act 48 of 1995](https://www.gov.za/documents/public-holidays-amendment-act),
  effective **4 October 1995**, adds s2A's presidential proclamation power.
  Include subsequently proclaimed holidays, not only an algorithm for the original schedule.
- **POPIA 4 of 2013:** [Act/commencement register](https://www.gov.za/documents/protection-personal-information-act),
  [statutory text](https://www.gov.za/sites/default/files/gcis_document/201409/3706726-11act4of2013popi.pdf),
  [Information Regulator special-information guidance, 28 June 2021](https://inforegulator.org.za/wp-content/uploads/2020/07/InfoRegSA-GuidanceNote-Processing-SpecialPersonalInformation-20210628.pdf).
  Core processing provisions ss2–38 commenced **1 July 2020**, with the compliance transition
  ending July 2021; other provisions commenced separately. Relevant: ss8–25, 26–27, 32, 71–72.

## Annual leave: cycle, accrual and charging

Enacted baseline: BCEA ss20–21. Each cycle is twelve months with the same employer, anchored
to employment commencement and successive cycles. Minimum is 21 consecutive days paid leave;
by agreement the alternative is one day per 17 days worked **or entitled to pay**, or one hour
per 17 such hours. Do not assume a January reset or silently choose an accrual agreement.
Leave must be granted within six months after cycle end. Payment instead of leave is restricted
to termination; payroll amounts/termination settlement are outside this roadmap.

For an ordinary five-day week, three weeks usually means 15 scheduled working days; six-day
weeks usually mean 18. These are schedule-dependent equivalents, not universal constants.
BCEA s20(8) gives an extra paid day for a holiday during annual leave on a day ordinarily worked.
Keep elapsed calendar duration, scheduled charge, accrued units and payment status separate.
Fraction rounding, carry-forward treatment and part-day handling require explicit reviewed rules.
Do not automatically erase unused statutory leave merely because a cycle ended.

## Sick leave and proof/payment

Enacted baseline: BCEA ss22–24 and Department sick guidance. A sick cycle is **36 calendar
months**, not an annual allowance. Paid entitlement equals normal working days in six weeks.
First six calendar months: one paid day per 26 days actually worked. First-cycle use under that
rule may be deducted from the cycle entitlement. Work schedules and actual worked-day inputs
therefore matter; scheduled days are not automatically proof of days worked.

Under s23, where absence exceeds two consecutive days **or** two occasions in eight weeks,
an employer may request a qualifying certificate before payment. The certificate must establish
incapacity/duration and be signed by an appropriately qualified, professionally registered issuer.
Consider s23(3)'s assistance rule for employees living on employer premises and s24 occupational
injury/disease exceptions. Do not demand a diagnosis in the request form.

**Product distinction:** recording absence, evidence requested/provided/verified, paid entitlement
and a manager decision are separate facts. Missing proof must not make it impossible to record
an absence. A conditional payment requirement is not a universal upload-before-submission rule.
HR reviews disputed proof/payment; managers normally receive evidence status, not medical files.

## Family responsibility leave

Enacted baseline: amended BCEA s27. Eligibility requires **longer than four months** with the
employer and at least four working days a week. Three paid days per annual employment cycle
cover a sick child and deaths of the specified relatives (spouse/life partner, parent/adoptive
parent, grandparent, child/adopted child, grandchild or sibling). Whole or part days and reasonable
proof are contemplated; unused entitlement lapses at cycle end. Collective agreements may vary
days/events within applicable legal constraints. Childbirth is now a parental-leave matter,
not the old family-responsibility event. An employer may provide broader compassionate leave
as a separately identified enhancement; do not label it the statutory minimum.

## Parental, birth recovery, adoption and commissioning parents

**Court interim rules:** order 5's substituted s25 grants a single/sole-employed parent at least
four consecutive calendar months. Two employed parents share four months plus ten days,
including birth preparation/recovery. Each parent's s25 leave is one consecutive sequence;
allocation may overlap. Birth-related leave can start four weeks before expected birth or
earlier on certification; no work for six weeks after birth without fitness certification.
Third-trimester miscarriage/stillbirth has six-week protection. Disagreement allocation aims
at equal totals, with the order's four-month completion condition. Parental responsibility,
not a Gender field, determines the relevant relationship.

Adoption starts at the earlier court order/placement; commissioning leave starts at birth.
Order 5 substitutes shared arrangements in ss25B/C. **Review blocker:** suspended age invalidity
and the retained under-two wording in substituted s25B(1) need legal reconciliation before
an age gate is encoded. Do not invent that interpretation. Notices and recovery safeguards
must remain explicit. Calendar months are date periods, never a hardcoded 120 days.

The Court's media summary overstates sole-employed allocation as “full” pooled leave; the
actual order is authoritative. Birth recovery/hazardous-work protections also require BCEA s26
review; they are not reducible to an entitlement balance or a gender-based permission.

## Holidays, schedules, pay and UIF

A Sunday public holiday also makes Monday a holiday; Saturday does not automatically shift
to Monday. Maintain a reviewed, versioned holiday calendar including Easter-based dates and
special proclamations; record agreed exchanges separately. BCEA s18 regulates agreed holiday
work and remuneration. A rotating/part-time schedule changes which days ordinarily would
be worked; weekends are not universally non-working days.

Annual/sick/family statutory paid leave, company-paid parental enhancements, unpaid absence
and UIF benefits must be distinct. The Van Wyk order does not automatically extend UIF
payments to match leave duration. UIF eligibility/amounts need current scheme confirmation;
HRFlow must not promise payment or implement a payroll/UIF adjudication engine in this phase.
The current zero-entitlement policy blocks every positive-day request; it is **not** a usable
unpaid-leave model. A future unpaid category needs an explicit no-entitlement-consumption mode.

Company enhancements are effective-dated, separately labelled and cannot silently weaken
applicable statutory floors. Capture governing agreement and payment category without inventing
historical contracts or requiring unnecessary medical/parental personal data.

## Worked examples and boundary cases for reviewer approval

These are proposed calculation fixtures, not current HRFlow behaviour or approved legal advice.

1. Employment starts 15 March 2026: first annual cycle is 15 March 2026–14 March 2027;
   next starts 15 March 2027. Define the leap-day anniversary convention explicitly rather
   than rounding months to days. A 1 January reset is not equivalent.
2. Five-day Monday–Friday schedule, annual leave 21–25 September 2026: five weekdays but
   24 September is Heritage Day, ordinarily worked. Proposed schedule charge is four days,
   not five; elapsed inclusive duration remains five. A six-day schedule's three ordinary
   weeks has 18 scheduled days before holiday adjustments.
3. Agreed one-per-17-day annual method, 170 qualifying worked/paid days: 10 days accrued.
   Hour method with 170 qualifying hours: 10 **hours**, not 10 days. With 169 units, retain
   precision pending the approved rounding rule; do not round up silently.
4. Five-day sick schedule: normally 30 days in six weeks per 36-month cycle. During the first
   six months, 52 days actually worked earns two paid days; using one leaves one at that stage.
   If first-cycle deduction is applied, that used day is not charged twice at month seven.
   The six-month anniversary uses date arithmetic, not 180 days.
5. Two consecutive sick days alone do not meet the more-than-two-days threshold. Three
   separate one-day sickness occasions within eight weeks can meet the occasion threshold.
   Model an employer request and proof/payment review; record the absence in both cases.
   Weekend adjacency and repeat episodes need a reviewed definition of an occasion.
6. At exactly four months' service, the “longer than” family rule is not yet met; at a reviewed
   later date with a four-day schedule it may be. A three-day schedule does not meet that
   statutory eligibility even with a year of service; company compassionate leave can differ.
7. A four-calendar-month period starting 1 February 2027 ends before 1 June: 120 elapsed days;
   starting 1 May ends before 1 September: 123. Those arithmetic examples demonstrate why
   months cannot be stored as a constant day allowance. The extra shared ten days and its
   anchor/allocation must be reviewed, not blindly appended to each parent's request.
8. Women's Day is Sunday 9 August 2026; Monday 10 August is also a holiday. Human Rights Day
   is Saturday 21 March 2026; do not manufacture a Monday holiday. For a Sunday worker the
   original date may also matter. Use the actual schedule, not a Monday–Friday assumption.
9. Two Approved requests covering the same employee/date contribute two summed request-days
   but one unique absent person-day. Preserve both metrics; never relabel a sum as unique
   staffing absence. Old calendar-day approvals retain their recorded/explicitly legacy basis.

## POPIA and evidence design requirements

Document purpose, lawful basis, notices, access/correction rights, minimisation and retention
under ss8–25. Health data is special information (ss26–27/32); a lawful employer processing
ground and confidentiality controls are needed, not just a checkbox accepting all processing.
Regulator guidance distinguishes special-data authorisations from ordinary lawful-processing
conditions. Optional Gender is not a parental-eligibility switch and is not required for CSV.

**Proposed least-privilege design:** owner and specifically authorised HR can access medical
content; eligible managers see only evidence status. Ordinary documents and profile images
have different permissions/retention. No public object URLs, diagnoses in logs, or raw files
in notifications/exports. Record access metadata without document contents. Assess operators,
security, breach response (s22), foreign transfers (s72) and consequential automated decisions
(s71). Review retention/legal holds per record class; no arbitrary “keep everything forever”
or deletion of decision audits when an attachment expires.

## Decisions requiring qualified review before coding

- Confirm current legislation/commencement and any later Van Wyk clarification. Resolve
  adoption age wording, sole-employed cases, partners with different employers, self-employment,
  allocation anchors/disagreement deadlines, continuity, and transitions between rule versions.
- Applicability, bargaining agreements, statutory versus enhanced leave; carry-forward/expiry,
  fractions/rounding, leap anniversaries, rotating schedules and employment interruptions.
- Sick episode definition, worked-day evidence, occupational compensation interaction,
  certificate exceptions, proof/payment dispute workflow and privacy notice/access/retention.
- Parental case evidence, third-party information minimisation, birth recovery and s26 protections;
  UIF versus employer pay. Do not reject cases solely because recorded gender differs.
- How to handle legacy unknown employment/schedule/cycle/rule/charged-day information. Require
  explicit HR confirmation or a visible calculation-unavailable state; never infer hire date
  from account creation or rewrite historical approvals to manufacture statutory compliance.

Implementation proposals and dependencies are in [the roadmap](05-product-roadmap.md).

Research access limitation: the government 2018 amendment page/PDF timed out on direct retrieval;
the government search index/commencement proclamation and Department-hosted amendment PDF were
available. The government's linked SAFlii consolidation and judgment URL were not accessible to
this browser tool; the actual judgment was retrieved from the Court's official repository instead.
This review assembled the baseline and amendments, not a certified current consolidated statute.
