# ADR 0031: focused Manager calendar keyboard interaction

21 October 2026. Branch feat/team-calendar-keyboard, fetched main
7dcd5f18421516c9f3d9c015d5f3760d4e0e8aa7; PR #131 confirmed merged.
Related existing scope: #91 (calendar keyboard/mobile) and #76 (retained regressions).
Issue searches for calendar and accessibility found no narrower matching issue. No tracker edits.

## Interaction contract

The existing inline calendar remains a read-only team view, not a date-picker dialog or decision UI.
CalendarDateGrid provides grid/row/columnheader/gridcell semantics and one native button in the Tab
sequence. Full accessible names include weekday, day, month, year, absence count or unavailable state,
and adjacent-month qualification. aria-current=date identifies the current SAST day;
aria-selected on the containing cell identifies the selected list filter. Focus has a separate visible
outline; selection has a border/surface and text. Moving focus never changes the selected filter.

- Left/Right move one date, including across week rows and into visible adjacent-month cells.
- Up/Down move seven dates. Home/End move to Monday/Sunday of that date's week.
- Ctrl+Home/End move to the first/last displayed date. Other modified keys are left to the browser.
- Enter/Space use native button activation to select and filter the list. Tab/Shift+Tab leave the
  grid in one step. A native date input can itself have multiple browser-specific Tab segments.
- Movement clamps to the first/last mounted cell, including when a seven-day move exceeds a range
  edge. It never silently changes months, queries another range or wraps to the opposite edge.
- Previous/Next month and Today explicitly clear the selected filter, as before. They leave focus on
  the initiating control. For a new month the grid Tab entry is SAST today when visible, otherwise
  the first of that month. Today in the already displayed month clears selection but retains the
  roving date; it does not select or focus today automatically. Each new month initializes only its
  date grid, not providers or sessions.
- Navigation is bounded to January 1900 through December 9998; December's visible January 9999
  cells remain usable. Every displayed week stays complete (28-42 days, below the 62-day API bound).
  At supported month boundaries controls are aria-disabled and inert but remain focusable, so reaching
  a limit does not disable/unmount the focused control. Default/Today month is clamped even for an
  out-of-range browser clock. This is not a new server-authoritative today value.
- Show whole range clears only selection, not roving focus or the export range.

## Loading, responsive and session behaviour

Date controls and the date input remain mounted during initial loading, refresh and error; absent or
failed data is unavailable, never zero. Range changes have no previous-range placeholder rows.
The last selected date and focused grid date survive ordinary background refresh/error/retry.
Existing stale-refresh messages remain; rows/counts disappear on errors. Counts/results are not an
aria-live grid: focus movement announces the focused button, while selection changes update a polite
atomic list heading. Existing query receipt times remain explicitly client timestamps.

At the existing sm breakpoint (40rem), a matchMedia listener moves focus to the date input ONLY if
focus was inside the grid that has just become hidden. Focus ownership and blur handling cover the
case where native hiding blurs the button before the media callback; actual departure clears ownership.
Widening never transfers focus automatically.
Other controls/drafts/selection remain unchanged. Unmount removes the listener and button references.
Existing account/session/range query keys, cancellation, role guards and replacement-session remount
remain authoritative; this component adds no API or mutation and does not fetch account data.

## Preserved contracts and visual design

Same component on Overview and /team-leave. Existing API scope, current Manager permissions,
current same-department direct reports, self exclusion, full request spans, inactive-history labels,
unioned distinct ACTIVE-person counts and history links are unchanged. Export always uses the full
displayed range, never the selected date. No HR approval capability or medical data added.
The existing neutral/indigo theme tokens, visible focus and reduced-motion rules are reused; no new
animation, dependency, backend, schema, calculation or email change. Mobile retains date-input/list
interaction without the hidden grid appearing in the Tab sequence.

## References and verification boundary

Official WAI APG [grid pattern](https://www.w3.org/WAI/ARIA/apg/patterns/grid/) and
[date-picker keyboard example](https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/examples/datepicker-dialog/)
were retrieved on 21 October 2026. This inline design borrows roving focus/date movement, not modal,
focus-trapping or every shortcut of the illustrative dialog. APG guidance is not proof of assistive
technology compatibility or WCAG compliance. Screen-reader and physical-device checks remain open.
See [actual runs, screenshots and limitations](../verification/team-calendar-keyboard.md).
