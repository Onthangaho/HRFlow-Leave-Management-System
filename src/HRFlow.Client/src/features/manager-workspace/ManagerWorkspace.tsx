import { ReportExport } from '../report-exports/ReportExport';
import { useRef, useState } from 'react';
import { CalendarDateGrid } from './CalendarDateGrid';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { isAxiosError } from 'axios';
import { authHttpClient } from '../auth/api';
import { useAuth } from '../auth/hooks/useAuth';
import { useTeamLeave, useVisiblePage, type TeamLeave } from '../team-leave/api';
import { problemMessage } from '../leave-configuration/problems';

import { dayMs, iso, dateOf, dateLabel, monthLabel, southAfricanToday, calendarRange, activeAbsent, firstCalendarYear, lastCalendarYear, calendarMonth } from './calendar';
const denied = (error: unknown) => isAxiosError(error) && [401, 403].includes(error.response?.status ?? 0);
const refreshMs = 60_000;
const upcomingRangeDays = 62;
const upcomingPreviewLimit = 10;
interface Attention { asOfUtc: string; pendingCount: number; previewLimit: number; pending: { requestId: string; employeeName: string; leaveTypeName: string; startDate: string; endDate: string }[] }
function useAttention() {
  const { user, sessionVersion } = useAuth();
  const visible = useVisiblePage();
  return useQuery({ queryKey: ['manager-attention', user?.id, sessionVersion],
    queryFn: async ({ signal }) => (await authHttpClient.get<Attention>('/leave-requests/manager-attention', { signal })).data,
    enabled: Boolean(user?.id && user.roles.includes('Manager')), retry: false, refetchOnMount: 'always',
    refetchInterval: q => visible && !denied(q.state.error) ? refreshMs : false, refetchIntervalInBackground: false });
}
function LeaveList({ entries }: { entries: TeamLeave[] }) {
  return <ul className="divide-y divide-slate-200">{entries.map(e => <li key={e.requestId} className="flex flex-wrap items-start justify-between gap-3 py-4">
    <div className="min-w-0 flex-1"><p className="break-words font-semibold">{e.employeeName}{!e.isActive && <span className="ml-2 text-sm">Inactive - historical leave</span>}</p><p className="break-words">{e.leaveTypeName}</p><p className="text-sm">{dateLabel(e.startDate)} - {dateLabel(e.endDate)} (Approved)</p></div>
    <Link className="ui-secondary" to={`/leave-requests/${e.requestId}/history`}>View history</Link>
  </li>)}</ul>;
}

/** Read-only scoped calendar/list shared by Overview and the existing Team leave route. */
export function TeamCalendar() {
  const [month, setMonth] = useState(() => calendarMonth(dateOf(southAfricanToday())));
  const [selected, setSelected] = useState<string | null>(null);
  const dateInput = useRef<HTMLInputElement>(null);
  const today = southAfricanToday();
  const range = calendarRange(month);
  const query = useTeamLeave(range.start, range.end);
  const entries = query.isError ? undefined : query.data;
  const previousDisabled = month.getUTCFullYear() === firstCalendarYear && month.getUTCMonth() === 0;
  const nextDisabled = month.getUTCFullYear() === lastCalendarYear && month.getUTCMonth() === 11;
  const change = (offset: number) => {
    if ((offset < 0 && previousDisabled) || (offset > 0 && nextDisabled)) return;
    setMonth(current => new Date(Date.UTC(current.getUTCFullYear(), current.getUTCMonth() + offset, 1)));
    setSelected(null);
  };
  const shown = entries?.filter(e => !selected || (e.startDate.slice(0, 10) <= selected && e.endDate.slice(0, 10) >= selected));
  return <section className="ui-panel space-y-4" aria-label="Read-only team calendar">
    <div className="flex flex-wrap items-center justify-between gap-3"><h2 className="text-xl font-bold">Team calendar - {monthLabel(month)}</h2><button className="ui-secondary" disabled={query.isFetching} onClick={() => void query.refetch()}>Refresh calendar</button></div>
    <nav aria-label="Team calendar month" className="team-calendar-month flex flex-wrap gap-2"><button className="ui-secondary" aria-disabled={previousDisabled} onClick={() => change(-1)}>Previous month</button><button className="ui-secondary" onClick={() => { setMonth(calendarMonth(dateOf(southAfricanToday()))); setSelected(null); }}>Today</button><button className="ui-secondary" aria-disabled={nextDisabled} onClick={() => change(1)}>Next month</button></nav>
    <p className="text-sm">Approved leave only for current same-department direct reports. SAST business dates; inclusive calendar dates. Displayed range {dateLabel(range.start)} - {dateLabel(range.end)} includes adjacent-month cells. Full request dates are shown below.</p>
    <p className="text-sm">Daily counts mean distinct active people with Approved leave, not staffing capacity. Overlapping requests count each active person once per date. Inactive history remains listed but is excluded from counts.</p>
    <div role="status">{query.isPending ? 'Loading team calendar...' : query.isFetching ? 'Refreshing; previously loaded results are not yet confirmed current.' : entries ? `Team snapshot received ${new Date(query.dataUpdatedAt).toISOString()} (UTC client receipt time).` : ''}</div>
    {query.isError && <div className="ui-error" role="alert"><p>{problemMessage(query.error)}</p><p>Team data is unavailable; no zero absence is assumed. Your selected month is retained.</p><button className="ui-secondary mt-2" disabled={query.isFetching} onClick={() => void query.refetch()}>Retry calendar</button></div>}
    <ReportExport kind="team" selection={{start:range.start,end:range.end}} disabled={query.isFetching || !entries} />
    <CalendarDateGrid key={iso(month)} month={month} range={range} today={today} selected={selected} entries={entries} onSelect={setSelected} mobileInput={dateInput} />
      <div className="flex flex-wrap items-end gap-3"><label className="min-w-0">Show date (optional)<input ref={dateInput} className="ui-input mt-1" type="date" min={range.start} max={range.end} value={selected ?? ''} onChange={e => setSelected(e.target.value >= range.start && e.target.value <= range.end ? e.target.value : null)}/></label><button className="ui-secondary" onClick={() => setSelected(null)}>Show whole range</button></div>
      <h3 className="font-semibold" aria-live="polite" aria-atomic="true">{selected ? `${dateLabel(selected)}: ${entries ? `${activeAbsent(entries, selected)} distinct active people with Approved leave` : 'absence count unavailable'}` : 'Accessible list - whole displayed range'}</h3>
      {entries && (shown?.length ? <LeaveList entries={shown}/> : <p>No Approved team leave {selected ? 'on this date' : 'in this range'}.</p>)}
    <Link className="font-semibold text-indigo-700 underline" to="/leave-requests/approvals">Review Pending requests in the decision queue</Link>
  </section>;
}
/** Composes independent authorized attention/team snapshots; decisions remain in the existing queue. */
export function ManagerWorkspace() {
  const attention = useAttention();
  const today = southAfricanToday();
  const end = iso(new Date(dateOf(today).getTime() + (upcomingRangeDays - 1) * dayMs));
  const upcoming = useTeamLeave(today, end);
  const data = attention.isError ? undefined : attention.data;
  const team = upcoming.isError ? undefined : upcoming.data;
  return <section className="space-y-6" aria-label="Manager workspace">
    <section className="ui-panel space-y-3"><h2 className="text-2xl font-bold">Your Manager workspace</h2><p>Current direct reports in your department; your own leave stays in the personal dashboard. Each panel is its own authorized snapshot. Refresh observes decisions and reporting changes made elsewhere.</p>
      <p role="status">{attention.isPending ? 'Loading decision attention...' : attention.isFetching ? 'Refreshing decision attention; previous results may be stale.' : data ? `Captured ${data.asOfUtc} (UTC).` : ''}</p>
      {attention.isError && <div role="alert" className="ui-error">{problemMessage(attention.error)} No count is assumed.<button className="ui-secondary ml-2" onClick={() => void attention.refetch()} disabled={attention.isFetching}>Retry attention</button></div>}
      {data && <><h3 className="font-semibold">{data.pendingCount} eligible Pending requests across all dates</h3><p className="text-sm">Showing up to {data.previewLimit}, earliest start first. Request counts are not leave days. The queue rechecks eligibility before deciding.</p><ul className="divide-y divide-slate-200">{data.pending.map(r => <li key={r.requestId} className="py-3"><p className="break-words font-semibold">{r.employeeName} - {r.leaveTypeName}</p><p>{dateLabel(r.startDate)} - {dateLabel(r.endDate)}</p></li>)}</ul>{!data.pending.length && <p>No eligible Pending requests.</p>}</>}
      <div className="flex flex-wrap gap-3"><Link className="ui-primary" to="/leave-requests/approvals">Open decision queue</Link><button className="ui-secondary" disabled={attention.isFetching} onClick={() => void attention.refetch()}>Refresh attention</button></div>
    </section>
    <section className="ui-panel space-y-3"><h2 className="text-xl font-bold">Ongoing and upcoming team Approved absences</h2><p>{dateLabel(today)} - {dateLabel(end)}, inclusive ({upcomingRangeDays} calendar dates). Showing up to {upcomingPreviewLimit} intersecting requests, earliest start first; full spans, not unique people or days.</p><p role="status">{upcoming.isPending ? 'Loading upcoming team leave...' : upcoming.isFetching ? 'Refreshing previously loaded team leave...' : team ? `Received ${new Date(upcoming.dataUpdatedAt).toISOString()} (UTC client receipt time).` : ''}</p>{upcoming.isError && <div className="ui-error" role="alert">{problemMessage(upcoming.error)} Team data is unavailable.</div>}{team && (team.length ? <LeaveList entries={team.slice(0,upcomingPreviewLimit)}/> : <p>No matching Approved team leave.</p>)}<button className="ui-secondary" disabled={upcoming.isFetching} onClick={() => void upcoming.refetch()}>{upcoming.isError ? 'Retry upcoming leave' : 'Refresh upcoming leave'}</button></section>
    <TeamCalendar/>
  </section>;
}