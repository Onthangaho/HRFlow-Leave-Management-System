import { useEffect, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { isAxiosError } from 'axios';
import { useAuth } from '../../auth/hooks/useAuth';
import { useDepartments } from '../../employees/api';
import { problemMessage } from '../../leave-configuration/problems';
import { monitoringDefaults, useMonitoringTypes, useOrganisationPendingLeaveRequests, type MonitoringFilters } from '../api';

const dateLabel = (value: string) => new Intl.DateTimeFormat('en-ZA', { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC' }).format(new Date(value.slice(0, 10) + 'T00:00:00Z'));

/** A replacement login discards protected rows and drafts, while ordinary refresh preserves applied filters. */
export function HrPendingLeaveMonitoringPage() {
  const { user, sessionVersion } = useAuth();
  return <MonitoringWorkspace key={user?.id + ':' + sessionVersion} />;
}
function MonitoringWorkspace() {
  const [draft, setDraft] = useState<MonitoringFilters>({ ...monitoringDefaults });
  const [applied, setApplied] = useState<MonitoringFilters>({ ...monitoringDefaults });
  const query = useOrganisationPendingLeaveRequests(applied);
  const departments = useDepartments();
  const types = useMonitoringTypes();
  const page = query.isError ? undefined : query.data;
  useEffect(() => {
    if (page && !query.isFetching && applied.page !== page.page)
      setApplied(current => current === applied ? { ...current, page: page.page } : current);
  }, [page, query.isFetching, applied]);
  function apply(event: FormEvent) { event.preventDefault(); setApplied({ ...draft, search: draft.search.trim(), page: 1 }); }
  function field(name: keyof MonitoringFilters, value: string) {
    setDraft(current => ({ ...current, [name]: name === 'pageSize' ? Number(value) : value }));
  }
  if (isAxiosError(query.error) && [401, 403].includes(query.error.response?.status ?? 0))
    return <section className="ui-error" role="alert"><h2>HR monitoring access unavailable</h2><p>Protected monitoring data is hidden. Sign in with current HR access.</p></section>;
  return <div className="workspace-page space-y-6"><div className="mx-auto w-full max-w-5xl space-y-6">
    <section className="page-intro ui-panel">
      <p>Monitor organisation-wide Pending leave. Assigned managers make approval decisions.</p>
      <p>Matching Pending requests: {query.isPending ? 'Loading...' : query.error || !page ? 'Unavailable' : page.totalCount}</p>
      <button type="button" className="ui-secondary self-start" disabled={query.isFetching} onClick={() => void query.refetch()}>Refresh monitoring</button>
    </section>
    <form className="ui-panel space-y-4" aria-label="Pending monitoring filters" onSubmit={apply}>
      <p>Dates include any request intersecting either boundary. Departments reflect current employee membership. This view is read-only.</p>
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <label className="ui-label">Employee name or email<input type="search" className="ui-input" maxLength={100} value={draft.search} onChange={e => field('search', e.target.value)} /></label>
        <label className="ui-label">Monitoring department<select className="ui-input" value={draft.departmentId} onChange={e => field('departmentId', e.target.value)}><option value="">All departments</option>{departments.data?.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}</select></label>
        <label className="ui-label">Monitoring leave type<select className="ui-input" value={draft.leaveTypeId} onChange={e => field('leaveTypeId', e.target.value)}><option value="">All leave types</option>{types.data?.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}</select></label>
        <label className="ui-label">Monitoring period start<input className="ui-input" type="date" value={draft.start} onChange={e => field('start', e.target.value)} /></label>
        <label className="ui-label">Monitoring period end<input className="ui-input" type="date" min={draft.start || undefined} value={draft.end} onChange={e => field('end', e.target.value)} /></label>
        <label className="ui-label">Monitoring requests per page<select className="ui-input" value={draft.pageSize} onChange={e => field('pageSize', e.target.value)}>{[25, 50, 100].map(size => <option key={size}>{size}</option>)}</select></label>
      </div>
      {(departments.isPending || types.isPending) && <p role="status">Loading monitoring filter choices...</p>}
      {(departments.isError || types.isError) && <p role="alert">Some filter choices are unavailable. <button type="button" className="ui-secondary" onClick={() => { void departments.refetch(); void types.refetch(); }}>Retry monitoring choices</button></p>}
      <div className="flex flex-wrap gap-3"><button className="ui-primary">Apply monitoring filters</button><button type="button" className="ui-secondary" onClick={() => { setDraft({ ...monitoringDefaults }); setApplied({ ...monitoringDefaults }); }}>Reset monitoring filters</button></div>
    </form>
    <p aria-label="Applied monitoring filters">Applied: {applied.search || 'Any employee'}; {departments.data?.find(d => d.id === applied.departmentId)?.name || (applied.departmentId ? 'Selected department' : 'All departments')}; {types.data?.find(t => t.id === applied.leaveTypeId)?.name || (applied.leaveTypeId ? 'Selected type' : 'All leave types')}; {applied.start || 'Any start'} to {applied.end || 'Any end'}</p>
    <p role="status">{query.isFetching ? (page ? 'Refreshing the previously loaded monitoring snapshot.' : 'Loading Pending monitoring.') : query.error ? 'Monitoring unavailable.' : page ? page.totalCount + ' matching requests. Page ' + page.page + ' of ' + page.totalPages + '.' : ''}</p>
    {query.isPending && <div role="status" className="ui-panel motion-safe:animate-pulse">Loading pending leave monitoring...</div>}
    {query.error && <section role="alert" className="ui-error"><h2>Unable to load pending leave monitoring</h2><p>{problemMessage(query.error)}</p><button className="ui-secondary" disabled={query.isFetching} onClick={() => void query.refetch()}>Try again</button></section>}
    {page && <p>Server snapshot: {new Date(page.capturedAtUtc).toISOString()} (UTC){query.isFetching ? ' - previously loaded snapshot' : ''}</p>}
    <div className="flex flex-wrap items-center gap-3"><button type="button" className="ui-secondary" disabled={!page || query.isFetching || page.page <= 1} onClick={() => setApplied(current => ({ ...current, page: page!.page - 1 }))}>Previous monitoring page</button><button type="button" className="ui-secondary" disabled={!page || query.isFetching || page.page >= page.totalPages} onClick={() => setApplied(current => ({ ...current, page: page!.page + 1 }))}>Next monitoring page</button></div>
    {page?.totalCount === 0 && <section className="ui-panel"><h2>No pending leave requests</h2><p>No Pending requests match the applied filters. Reset or refresh to check for new requests.</p></section>}
    {page && <section className="space-y-4" aria-label="Organisation pending leave requests">{page.items.map(request =>
      <article key={request.id} className="ui-panel min-w-0 space-y-3">
        <div className="flex flex-col gap-3 sm:flex-row sm:justify-between"><div className="min-w-0"><h2 className="break-words font-bold">{request.employeeFullName}</h2><p className="break-all">{request.employeeEmail}</p></div><div className="min-w-0"><p className="break-words">{request.leaveTypeName}</p><p>{dateLabel(request.startDate)} - {dateLabel(request.endDate)} (inclusive calendar dates)</p><p>Pending</p></div></div>
        <Link className="inline-block font-semibold text-indigo-700 underline" to={'/leave-requests/' + request.id + '/history'}>View history</Link>
      </article>)}</section>}
  </div></div>;
}
