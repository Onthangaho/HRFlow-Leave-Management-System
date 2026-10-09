import { useState } from 'react';
import axios from 'axios';
import { Link } from 'react-router-dom';
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { useAuth } from '../auth/hooks/useAuth';
import { useDepartmentLeaveReport, useReportDepartments } from './api';
import type { DepartmentLeaveReport, LeaveReportFilters } from './api';

const maximumRangeDays = 366;
const millisecondsPerDay = 86_400_000;
const dateFormatter = new Intl.DateTimeFormat('en', { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC' });
const formatDate = (value: string) => dateFormatter.format(new Date(`${value}T00:00:00Z`));

function currentMonth(): LeaveReportFilters {
  const today = new Date();
  return { start: new Date(Date.UTC(today.getFullYear(), today.getMonth(), 1)).toISOString().slice(0, 10),
    end: new Date(Date.UTC(today.getFullYear(), today.getMonth() + 1, 0)).toISOString().slice(0, 10), departmentId: '' };
}

function failureMessage(error: unknown) {
  if (axios.isAxiosError(error)) {
    if (error.response?.status === 403) return 'Your account no longer has permission to view leave reports.';
    if (error.response && [400, 404, 409].includes(error.response.status) && typeof error.response.data?.detail === 'string') return error.response.data.detail;
  }
  return 'Unable to load leave reports. Check your connection and try again.';
}

/** Remounts both draft and applied selections when the login session changes, even for the same account. */
export function LeaveReportsPage() {
  const { user, sessionVersion } = useAuth();
  return <LeaveReportsWorkspace key={`${user?.id}:${sessionVersion}`} />;
}

function LeaveReportsWorkspace() {
  const [applied, setApplied] = useState(currentMonth);
  const [draft, setDraft] = useState(applied);
  const [validation, setValidation] = useState('');
  const report = useDepartmentLeaveReport(applied);
  const departments = useReportDepartments();
  const dirty = draft.start !== applied.start || draft.end !== applied.end || draft.departmentId !== applied.departmentId;
  const apply = (event: React.SubmitEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!draft.start || !draft.end) { setValidation('Choose both start and end dates.'); return; }
    const days = (Date.parse(`${draft.end}T00:00:00Z`) - Date.parse(`${draft.start}T00:00:00Z`)) / millisecondsPerDay + 1;
    if (!Number.isFinite(days) || days < 1 || days > maximumRangeDays) {
      setValidation(`Choose an ordered range of 1–${maximumRangeDays} inclusive calendar days.`); return;
    }
    setValidation('');
    if (!dirty) void report.refetch();
    else setApplied({ ...draft });
  };
  const reset = () => { const filters = currentMonth(); setDraft(filters); setApplied(filters); setValidation(''); };

  return <div className="workspace-page space-y-6">
    <div className="mx-auto max-w-6xl space-y-6">
      <section className="page-intro ui-panel">

        <p className="pt-3 text-xs font-bold uppercase tracking-widest text-slate-500">HR reporting</p>

        <p className="text-slate-600">Understand leave activity by current department, with preserved inactive history.</p>
        <Link to="/admin/leave-monitoring" className="inline-block font-semibold text-indigo-700 underline">Open read-only Pending monitoring</Link>
      </section>
      <form className="ui-panel space-y-4" onSubmit={apply} aria-label="Report filters">
        <div className="grid gap-4 sm:grid-cols-3">
          <label className="ui-label">Start date<input className="ui-input" type="date" required value={draft.start} onChange={event => setDraft({ ...draft, start: event.target.value })} aria-describedby="report-filter-help report-validation" /></label>
          <label className="ui-label">End date<input className="ui-input" type="date" required value={draft.end} onChange={event => setDraft({ ...draft, end: event.target.value })} aria-describedby="report-filter-help report-validation" /></label>
          <label className="ui-label">Department<select aria-label="Department" className="ui-input" value={draft.departmentId} disabled={departments.isPending || !!departments.error} onChange={event => setDraft({ ...draft, departmentId: event.target.value })}>
            <option value="">All departments</option>{departments.data?.map(department => <option key={department.id} value={department.id}>{department.name}</option>)}
          </select></label>
        </div>
        <p id="report-filter-help" className="text-sm text-slate-600">Inclusive calendar dates, up to {maximumRangeDays} days. Edit filters, then Apply to change the report.</p>
        <p id="report-validation" role={validation ? 'alert' : undefined} className={validation ? 'ui-error' : ''}>{validation}</p>
        {departments.isPending && <p role="status">Loading department choices…</p>}
        {departments.error && <p role="alert" className="ui-error">Department choices could not be loaded. Your current filters are retained. <button type="button" className="underline" onClick={() => void departments.refetch()}>Retry department choices</button></p>}
        {dirty && <p role="status" className="ui-warning">Unapplied filter changes. Displayed results still use the last applied selection.</p>}
        <div className="flex flex-wrap gap-3">
          <button type="submit" className="ui-primary" disabled={report.isFetching}>Apply</button>
          <button type="button" className="ui-secondary" onClick={reset}>Reset</button>
          <button type="button" className="ui-secondary" disabled={report.isFetching} onClick={() => void report.refetch()}>Refresh applied report</button>
        </div>
      </form>

      <section className="ui-panel space-y-2 text-sm text-slate-600" aria-label="How to read this report">
        <h2 className="font-bold text-slate-900">How to read this report</h2>
        <p>Counts include Pending or Approved requests intersecting the selected dates. Rejected and Cancelled requests are excluded.</p>
        <p>Approved request-days sum each request’s duration inside the period, including both endpoints and weekends. Same-type and cross-type overlaps count separately; this is not unique people-days absent.</p>
        <p>Departments reflect each employee’s current assignment, including for historical requests. Inactive Approved history is preserved and identified separately; these totals are not active-staff coverage.</p>
      </section>
      <div role="status" aria-live="polite" className="text-sm text-slate-600">
        {report.isFetching ? (report.data ? 'Refreshing the applied report. Previously loaded results are shown below.' : 'Loading the applied report…') : !report.isError ? 'Applied report loaded.' : ''}
      </div>
      {report.isPending && <section className="ui-panel" role="status">Loading leave report…</section>}
      {report.isError && <section className="ui-panel space-y-3" role="alert">
        <h2 className="text-lg font-bold">Unable to load the applied report</h2><p className="ui-error">{failureMessage(report.error)}</p>
        <p className="text-sm text-slate-600">Your filter inputs are retained. Try again to reload the last applied selection, or change filters and Apply.</p>
        <button className="ui-primary" disabled={report.isFetching} onClick={() => void report.refetch()}>Try again</button>
      </section>}
      {!report.isPending && !report.isError && report.data && <ReportResults report={report.data} refreshing={report.isFetching} />}
    </div>
  </div>;
}

function ReportResults({ report, refreshing }: { report: DepartmentLeaveReport; refreshing: boolean }) {
  const totals = report.totals;
  const chart = report.departments.map(department => ({ name: department.departmentName, days: department.metrics.approvedRequestDays }));
  return <section className="space-y-6" aria-label="Applied report results" aria-busy={refreshing}>
    <header className="space-y-2">
      <h2 className="text-xl font-bold text-slate-900">{formatDate(report.start)} – {formatDate(report.end)}</h2>
      <p className="break-words font-semibold text-slate-700">{report.departmentId ? report.departments[0]?.departmentName : 'All departments'}{refreshing ? ' · Refreshing previously loaded results' : ' · Applied selection'}</p>
    </header>
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
      {[['Pending requests', totals.pendingRequests, 'Intersecting this period'],
        ['Approved requests', totals.approvedRequests, `${totals.inactiveApprovedRequests} from inactive employees`],
        ['Distinct employees with Approved leave', totals.employeesWithApprovedLeave, `${totals.inactiveEmployeesWithApprovedLeave} inactive employees included`],
        ['Approved request-days (summed)', totals.approvedRequestDays, `${totals.inactiveApprovedRequestDays} request-days from inactive history`]].map(([label, value, note]) =>
        <section className="ui-panel" key={label}><h3 className="text-sm font-semibold text-slate-700">{label}</h3><p className="mt-3 text-3xl font-bold text-indigo-700">{value}</p><p className="mt-2 text-sm text-slate-600">{note}</p></section>)}
    </div>
    {totals.pendingRequests === 0 && totals.approvedRequests === 0 && <p className="ui-panel" role="status">No matching Pending or Approved leave in the applied period. Existing departments still appear with zero totals.</p>}
    {totals.approvedRequests > 0 && <figure className="ui-panel min-w-0" aria-labelledby="department-chart-title">
      <figcaption id="department-chart-title" className="text-lg font-bold text-slate-900">Approved request-days by current department</figcaption>
      <p className="mt-2 text-sm text-slate-600">Summed request durations inside the period, including inactive history and overlaps. Use arrow keys on the chart to explore; the full table follows.</p>
      <div className="mt-4 max-h-[32rem] overflow-y-auto">
        <div style={{ height: Math.max(260, chart.length * 55) }}>
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={chart} layout="vertical" accessibilityLayer margin={{ top: 12, right: 20, bottom: 25, left: 0 }}>
              <CartesianGrid stroke="var(--chart-grid)" strokeDasharray="3 3" horizontal={false} />
              <XAxis tick={{ fill: 'var(--foreground)' }} stroke="var(--chart-grid)" type="number" allowDecimals={false} label={{ fill: 'var(--foreground)', value: 'Summed approved request-days', position: 'insideBottom', offset: -20 }} />
              <YAxis stroke="var(--chart-grid)" type="category" dataKey="name" width={100} tick={{ fontSize: 11, fill: 'var(--foreground)' }} tickFormatter={(name: string) => name.length > 16 ? `${name.slice(0, 15)}…` : name} />
              <Tooltip contentStyle={{ background: 'var(--surface)', color: 'var(--foreground)', borderColor: 'var(--border)' }} labelStyle={{ color: 'var(--foreground)' }} itemStyle={{ color: 'var(--foreground)' }} cursor={{ fill: 'var(--chart-hover)' }} formatter={value => [`${value} request-days`, 'Approved duration (summed)']} isAnimationActive={false} />
              <Bar dataKey="days" name="Approved request-days (summed)" fill="var(--chart-bar)" isAnimationActive={false} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>
    </figure>}
    <section className="ui-panel min-w-0">
      <h2 className="text-lg font-bold text-slate-900">Department breakdown</h2>
      <p className="mt-2 text-sm text-slate-600">Inactive contributions are included in each total and shown underneath. Scroll the table horizontally on small screens.</p>
      <div className="mt-4 max-w-full overflow-x-auto" role="region" aria-label="Department metrics table, scroll horizontally" tabIndex={0}>
        <table className="w-full min-w-[44rem] text-left text-sm">
          <caption className="sr-only">Applied-period leave metrics attributed to current departments</caption>
          <thead className="border-b border-slate-300"><tr>{['Current department', 'Pending requests', 'Approved requests', 'Distinct employees', 'Approved request-days (summed)'].map(label => <th scope="col" className="px-3 py-3 font-semibold" key={label}>{label}</th>)}</tr></thead>
          <tbody>{report.departments.map(department => <tr key={department.departmentId} className="border-b border-slate-200 last:border-0">
            <th scope="row" className="max-w-60 break-words px-3 py-4 font-semibold">{department.departmentName}</th>
            <td className="px-3 py-4">{department.metrics.pendingRequests}</td>
            <td className="px-3 py-4">{department.metrics.approvedRequests}<span className="mt-1 block text-xs text-slate-600">{department.metrics.inactiveApprovedRequests} inactive</span></td>
            <td className="px-3 py-4">{department.metrics.employeesWithApprovedLeave}<span className="mt-1 block text-xs text-slate-600">{department.metrics.inactiveEmployeesWithApprovedLeave} inactive</span></td>
            <td className="px-3 py-4">{department.metrics.approvedRequestDays}<span className="mt-1 block text-xs text-slate-600">{department.metrics.inactiveApprovedRequestDays} inactive request-days</span></td>
          </tr>)}</tbody>
        </table>
      </div>
      {report.departments.length === 0 && <p className="mt-4 text-slate-600">No departments exist. Contact your administrator.</p>}
    </section>
  </section>;
}
