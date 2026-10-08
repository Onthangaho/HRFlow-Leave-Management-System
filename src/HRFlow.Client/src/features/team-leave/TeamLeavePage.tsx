import { useState } from 'react';
import axios from 'axios';
import { Link } from 'react-router-dom';
import { useAuth } from '../auth/hooks/useAuth';
import { useTeamLeave } from './api';

const monthFormatter = new Intl.DateTimeFormat('en', { month: 'long', year: 'numeric', timeZone: 'UTC' });
const dateFormatter = new Intl.DateTimeFormat('en', { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC' });
const formatDate = (value: string) => dateFormatter.format(new Date(value.slice(0, 10) + 'T00:00:00Z'));
const monthStart = (date: Date) => new Date(Date.UTC(date.getFullYear(), date.getMonth(), 1));

function errorMessage(error: unknown) {
  if (axios.isAxiosError(error)) {
    if (error.response?.status === 403) return 'Your account no longer has permission to view team leave.';
    if (error.response?.status === 409 && typeof error.response.data?.detail === 'string') return error.response.data.detail;
  }
  return 'Team leave could not be loaded. Check your connection and try again.';
}

/** Remounts local month state on session changes so a delayed response cannot expose another account's team. */
export function TeamLeavePage() {
  const { user, sessionVersion } = useAuth();
  return <TeamLeaveWorkspace key={`${user?.id}:${sessionVersion}`} />;
}

function TeamLeaveWorkspace() {
  const [month, setMonth] = useState(() => monthStart(new Date()));
  const start = month.toISOString().slice(0, 10);
  const end = new Date(Date.UTC(month.getUTCFullYear(), month.getUTCMonth() + 1, 0)).toISOString().slice(0, 10);
  const summary = useTeamLeave(start, end);
  const entries = summary.data ?? [];
  const activePeople = new Set(entries.filter(entry => entry.isActive).map(entry => entry.employeeId)).size;
  const groups = new Map<string, typeof entries>();
  for (const entry of entries) {
    const group = groups.get(entry.employeeId) ?? [];
    group.push(entry);
    groups.set(entry.employeeId, group);
  }
  const changeMonth = (offset: number) => setMonth(current => new Date(Date.UTC(current.getUTCFullYear(), current.getUTCMonth() + offset, 1)));

  return <main className="min-h-screen bg-slate-50 px-4 py-8 sm:px-6">
    <div className="mx-auto max-w-5xl space-y-6">
      <header className="space-y-3">
        <Link to="/" className="font-semibold text-indigo-700 underline">Back to dashboard</Link>
        <p className="pt-3 text-xs font-bold uppercase tracking-widest text-slate-500">Coverage planning</p>
        <h1 className="text-3xl font-bold text-slate-900">Team leave</h1>
        <p className="max-w-2xl text-slate-600">Approved leave for your current direct reports in your department. Dates include both endpoints and all calendar days.</p>
        <Link to="/leave-requests/approvals" className="inline-block font-semibold text-indigo-700 underline">Review Pending requests in the approval queue</Link>
      </header>

      <section className="ui-panel space-y-4" aria-label="Selected month">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <h2 className="text-xl font-bold text-slate-900" aria-live="polite">{monthFormatter.format(month)}</h2>
          <button className="ui-secondary" disabled={summary.isFetching} onClick={() => void summary.refetch()}>Refresh</button>
        </div>
        <nav aria-label="Month navigation" className="flex flex-wrap gap-2">
          <button className="ui-secondary" aria-label="Previous month" disabled={month.getUTCFullYear() <= 1900 && month.getUTCMonth() === 0} onClick={() => changeMonth(-1)}>← Previous</button>
          <button className="ui-secondary" onClick={() => setMonth(monthStart(new Date()))}>This month</button>
          <button className="ui-secondary" aria-label="Next month" disabled={month.getUTCFullYear() >= 9999 && month.getUTCMonth() === 11} onClick={() => changeMonth(1)}>Next →</button>
        </nav>
        <p className="text-sm text-slate-600">{formatDate(start)} – {formatDate(end)} · Complete request dates appear below, including leave that extends beyond this month.</p>
      </section>

      <div role="status" aria-live="polite" className="text-sm text-slate-600">
        {summary.isFetching ? 'Refreshing team leave…' : !summary.isError ? 'Team leave is up to date.' : ''}
      </div>
      {summary.isPending && <section className="ui-panel" role="status">Loading approved team leave…</section>}
      {summary.isError && <section className="ui-panel space-y-3" role="alert">
        <h2 className="text-lg font-bold">Unable to load team leave</h2>
        <p className="ui-error">{errorMessage(summary.error)}</p>
        <p className="text-sm text-slate-600">Your selected month is unchanged. Refresh before relying on coverage information.</p>
        <button className="ui-primary" disabled={summary.isFetching} onClick={() => void summary.refetch()}>Try again</button>
      </section>}
      {!summary.isPending && !summary.isError && <>
        <section className="ui-panel">
          <p className="text-3xl font-bold text-indigo-700">{activePeople}</p>
          <h2 className="mt-1 font-semibold text-slate-900">Active team members with approved leave this month</h2>
          <p className="mt-2 text-sm text-slate-600">Each person is counted once, even with overlapping requests. This is not a daily absence count. Inactive reports are shown for history and excluded from this count.</p>
        </section>
        {entries.length === 0 ? <section className="ui-panel py-12 text-center">
          <h2 className="text-xl font-bold text-slate-900">No approved team leave this month</h2>
          <p className="mt-3 text-slate-600">Your current team has no matching approved requests. Choose another month or check the approval queue for Pending requests.</p>
        </section> : <section aria-label="Approved leave by employee" className="space-y-4">
          {[...groups.values()].map(group => <article key={group[0].employeeId} className="ui-panel min-w-0">
            <header className="flex flex-wrap items-center gap-3">
              <h2 className="break-words text-lg font-bold text-slate-900">{group[0].employeeName}</h2>
              {!group[0].isActive && <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-700">Inactive · historical leave</span>}
            </header>
            <ul className="mt-4 divide-y divide-slate-200">
              {group.map(entry => <li key={entry.requestId} className="flex flex-col gap-2 py-4 first:pt-0 last:pb-0 sm:flex-row sm:items-center sm:justify-between">
                <p className="min-w-0 break-words font-medium text-slate-800">{entry.leaveTypeName}</p>
                <p className="text-sm text-slate-600"><time dateTime={entry.startDate.slice(0, 10)}>{formatDate(entry.startDate)}</time> – <time dateTime={entry.endDate.slice(0, 10)}>{formatDate(entry.endDate)}</time></p>
              </li>)}
            </ul>
          </article>)}
        </section>}
      </>}
    </div>
  </main>;
}
