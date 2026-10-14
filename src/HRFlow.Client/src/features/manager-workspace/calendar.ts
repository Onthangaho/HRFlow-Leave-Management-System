import type { TeamLeave } from '../team-leave/api';
/** Calendar arithmetic uses UTC midnights, avoiding host daylight-saving shifts. */
export const dayMs = 86_400_000;
/** Serializes calendar facts without browser offsets. */
export const iso = (date: Date) => date.toISOString().slice(0, 10);
/** Parses only the calendar portion, not an API timestamp offset. */
export const dateOf = (value: string) => new Date(value.slice(0, 10) + 'T00:00:00Z');
/** Formats date-only facts with stable South African labels. */
export const dateLabel = (value: string) => new Intl.DateTimeFormat('en-ZA', { dateStyle: 'medium', timeZone: 'UTC' }).format(dateOf(value));
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
/** Unioning active employee IDs prevents same-type/cross-type overlaps inflating daily absence. */
export function activeAbsent(entries: TeamLeave[], date: string) {
  return new Set(entries.filter(e => e.isActive && e.startDate.slice(0, 10) <= date && e.endDate.slice(0, 10) >= date).map(e => e.employeeId)).size;
}
