import type { TeamLeave } from '../team-leave/api';
/** Calendar arithmetic uses UTC midnights, avoiding host daylight-saving shifts. */
export const dayMs = 86_400_000;
/** Serializes calendar facts without browser offsets. */
export const iso = (date: Date) => date.toISOString().slice(0, 10);
/** Parses only the calendar portion, not an API timestamp offset. */
export const dateOf = (value: string) => new Date(value.slice(0, 10) + 'T00:00:00Z');
/** Formats date-only facts with stable South African labels. */
export const dateLabel = (value: string) => new Intl.DateTimeFormat('en-ZA', { dateStyle: 'medium', timeZone: 'UTC' }).format(dateOf(value));
/** Date buttons announce weekday, month and year without depending on visual column position. */
export const fullDateLabel = (value: string) => new Intl.DateTimeFormat('en-ZA', { dateStyle: 'full', timeZone: 'UTC' }).format(dateOf(value));
/** Month controls stay within these years; visible adjacent cells may extend into the following year. */
export const firstCalendarYear = 1900;
/** December 9998 leaves its adjacent January cells inside the API's DateOnly range. */
export const lastCalendarYear = 9998;
/** Even an out-of-range browser clock must not create a month outside supported navigation bounds. */
export function calendarMonth(date: Date) {
  if (date.getUTCFullYear() < firstCalendarYear) return new Date(Date.UTC(firstCalendarYear, 0, 1));
  if (date.getUTCFullYear() > lastCalendarYear) return new Date(Date.UTC(lastCalendarYear, 11, 1));
  return new Date(Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), 1));
}
/** Labels the selected month independently of host timezone. */
export const monthLabel = (date: Date) => new Intl.DateTimeFormat('en-ZA', { month: 'long', year: 'numeric', timeZone: 'UTC' }).format(date);
const southAfricaUtcOffsetHours = 2;
/** Current SAST business date, independent of browser timezone; this is not a workday calendar. */
export function southAfricanToday() { return iso(new Date(Date.now() + southAfricaUtcOffsetHours * 60 * 60 * 1000)); }
/** Monday-first visible month cells include adjacent dates, always 28-42 days within the API's 62-day bound. */
export function calendarRange(month: Date) {
  const first = new Date(Date.UTC(month.getUTCFullYear(), month.getUTCMonth(), 1));
  const start = new Date(first.getTime() - ((first.getUTCDay() + 6) % 7) * dayMs);
  const last = new Date(Date.UTC(month.getUTCFullYear(), month.getUTCMonth() + 1, 0));
  const end = new Date(last.getTime() + (6 - ((last.getUTCDay() + 6) % 7)) * dayMs);
  const days = Array.from({ length: Math.round((end.getTime() - start.getTime()) / dayMs) + 1 }, (_, n) => iso(new Date(start.getTime() + n * dayMs)));
  return { start: iso(start), end: iso(end), days };
}
/** Roving focus stops at mounted range edges; changing the queried month is always an explicit control action. */
export function calendarKeyDate(day: string, key: string, start: string, end: string, control = false) {
  const date = dateOf(day);
  const weekday = (date.getUTCDay() + 6) % 7;
  const offsets: Record<string, number> = { ArrowLeft: -1, ArrowRight: 1, ArrowUp: -7, ArrowDown: 7, Home: -weekday, End: 6 - weekday };
  if (!Object.hasOwn(offsets, key)) return null;
  const target = control && key === 'Home' ? start : control && key === 'End' ? end
    : iso(new Date(date.getTime() + offsets[key] * dayMs));
  return target < start ? start : target > end ? end : target;
}
/** Unioning active employee IDs prevents same-type/cross-type overlaps inflating daily absence. */
export function activeAbsent(entries: TeamLeave[], date: string) {
  return new Set(entries.filter(e => e.isActive && e.startDate.slice(0, 10) <= date && e.endDate.slice(0, 10) >= date).map(e => e.employeeId)).size;
}
