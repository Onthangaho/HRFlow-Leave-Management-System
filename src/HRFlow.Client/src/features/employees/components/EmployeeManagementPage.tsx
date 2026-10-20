import { useSearchParams } from 'react-router-dom';
import { useEffect, useRef, useState } from 'react';
import { isAxiosError } from 'axios';
import { useDepartments, useEmployees, type DirectoryFilter } from '../api';
import { authHttpClient } from '../../auth/api';
import { useAuth } from '../../auth/hooks/useAuth';
import type { Employee } from '../types';
import { ResendActivationDialog } from './ResendActivationDialog';
import { EmployeeForm } from './EmployeeForm';
import { EmployeeTable } from './EmployeeTable';
import { EmployeeDeactivationDialog } from './EmployeeDeactivationDialog';
import { EmployeeDetailsDialog } from './EmployeeDetailsDialog';

const attentions = ['all', 'employment', 'schedule', 'future-schedule', 'activation', 'delivery-failed', 'delivery-pending'];
const initial = (attention: string): DirectoryFilter => ({ search: '', status: 'All', departmentId: '', attention: attentions.includes(attention) ? attention : 'all', reportsTo: '', sort: 'name', page: 1, pageSize: 25 });
/** A replacement login remounts drafts; ordinary list refreshes never replace a loaded edit snapshot. */
export function EmployeeManagementPage() {
  const { user, sessionVersion } = useAuth();
  return <EmployeeManagementWorkspace key={`${user?.id}:${sessionVersion}`} />;
}
function EmployeeManagementWorkspace() {
  const [params] = useSearchParams();
  const attentionLink = params.get('attention') ?? 'all';
  const [filter, setFilter] = useState(() => initial(attentionLink));
  const [draft, setDraft] = useState(filter);
  const employees = useEmployees(filter);
  const departments = useDepartments();
  const { sessionVersion, getSessionVersion } = useAuth();
  const currentSession = () => getSessionVersion() === sessionVersion;
  const [editing, setEditing] = useState<Employee | null | undefined>(undefined);
  const [deactivating, setDeactivating] = useState<Employee | null>(null);
  const [details, setDetails] = useState<Employee | null>(null);
  const [inviting, setInviting] = useState<Employee | null>(null);
  const [notice, setNotice] = useState('');
  const [reloadError, setReloadError] = useState('');
  const [reportsFor, setReportsFor] = useState<Employee | null>(null);
  const opener = useRef<HTMLElement | null>(null);
  const controller = useRef<AbortController | null>(null);
  useEffect(() => () => controller.current?.abort(), [sessionVersion]);
  useEffect(() => { const next = initial(attentionLink); setFilter(next); setDraft(next); setReportsFor(null); }, [attentionLink]);
  useEffect(() => {
    if (!employees.isFetching && !employees.isError && employees.data && filter.page > 1 && !employees.data.items.length) {
      const last = Math.max(1, Math.ceil(employees.data.totalCount / filter.pageSize));
      if (filter.page > last) setFilter(previous => ({ ...previous, page: last }));
    }
  }, [employees.data, employees.isError, employees.isFetching, filter.page, filter.pageSize]);
  const rememberFocus = () => { opener.current = document.activeElement as HTMLElement | null; };
  const openForm = (employee: Employee | null) => { setEditing(employee); setNotice(''); setReloadError(''); };
  const reload = async () => {
    if (!editing) return;
    controller.current?.abort(); controller.current = new AbortController();
    try {
      const current = (await authHttpClient.get<Employee>(`/employees/${editing.id}`, { signal: controller.current.signal })).data;
      if (!currentSession()) return;
      if (current.isActive) setEditing(current); else { rememberFocus(); setEditing(undefined); setDetails(current); }
      setReloadError('');
    } catch { if (currentSession()) setReloadError('Unable to reload this employee. Try again or cancel the edit.'); }
  };
  const data = employees.isError ? undefined : employees.data;
  const pages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1;
  if (isAxiosError(employees.error) && employees.error.response?.status === 403)
    return <div className="ui-panel" role="alert">You no longer have HR directory access. Protected employee content is hidden.</div>;
  return <div className="workspace-page space-y-6">
    <section className="page-intro ui-panel"><p className="text-sm">Manage employee profiles, access and reporting relationships.</p>
      <button type="button" data-focus-fallback disabled={editing !== undefined || employees.isPending || employees.isError} onClick={() => openForm(null)} className="ui-primary">New Employee</button></section>
    {notice && <p role="status" className="break-words text-emerald-800">{notice}</p>}
    {editing !== undefined && <EmployeeForm employee={editing}
      onSuccess={delivery => { if (!currentSession()) return; setNotice(editing ? 'Employee updated.' : delivery === 'PickupReady' ? 'Employee created. Private development invitation is ready for pickup.' : 'Employee created, but invitation delivery failed. Use Resend invitation to recover.'); setEditing(undefined); }}
      onCancel={() => setEditing(undefined)} onReload={reload} reloadError={reloadError} />}
    <form className="ui-panel space-y-4" aria-label="Directory filters" onSubmit={event => { event.preventDefault(); setFilter({ ...draft, page: 1 }); }}>
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        <label className="ui-label">Search employees<input type="search" className="ui-input block w-full" value={draft.search} maxLength={100} onChange={e => setDraft({ ...draft, search: e.target.value })} placeholder="Name, email or employee number" /></label>
        <label className="ui-label">Employment status<select aria-label="Employment status" className="ui-input block w-full" value={draft.status} onChange={e => setDraft({ ...draft, status: e.target.value })}>{['All', 'Active', 'Inactive'].map(value => <option key={value}>{value}</option>)}</select></label>
        <label className="ui-label">Department<select aria-label="Department" className="ui-input block w-full" value={draft.departmentId} onChange={e => setDraft({ ...draft, departmentId: e.target.value })}><option value="">All departments</option>{departments.data?.map(d => <option value={d.id} key={d.id}>{d.name}</option>)}</select></label>
        <label className="ui-label">Administrative attention<select aria-label="Administrative attention" className="ui-input block w-full" value={draft.attention} onChange={e => setDraft({ ...draft, attention: e.target.value })}>
          <option value="all">All employee records</option><option value="employment">Active: Unknown employment facts</option><option value="schedule">Active starters: No schedule effective today</option><option value="future-schedule">Active starters: Future schedule only</option><option value="activation">Active employment: Pending activation</option><option value="delivery-failed">Active pending accounts: Delivery failed</option><option value="delivery-pending">Active pending accounts: Delivery acknowledgement pending</option>
        </select></label>
        <label className="ui-label">Sort by<select aria-label="Sort by" className="ui-input block w-full" value={draft.sort} onChange={e => setDraft({ ...draft, sort: e.target.value })}><option value="name">Name</option><option value="email">Email</option><option value="number">Employee number</option></select></label>
        <label className="ui-label">Rows per page<select aria-label="Rows per page" className="ui-input block w-full" value={draft.pageSize} onChange={e => setDraft({ ...draft, pageSize: Number(e.target.value) })}>{[25, 50, 100].map(size => <option value={size} key={size}>{size}</option>)}</select></label>
      </div>
      <p className="text-sm">Search matches literal text in names, emails and employee numbers (ASCII case-insensitive). Attention uses current server SAST states, not legal violations.</p>
      {departments.isError && <p role="alert">Department choices unavailable. <button type="button" className="underline" onClick={() => void departments.refetch()}>Retry departments</button></p>}
      <div className="flex flex-wrap gap-3"><button className="ui-primary" type="submit">Apply filters</button><button className="ui-secondary" type="button" onClick={() => { const next = initial('all'); setDraft(next); setFilter(next); setReportsFor(null); }}>Reset filters</button><button type="button" className="ui-secondary" disabled={employees.isFetching} onClick={() => void employees.refetch()}>Refresh directory</button></div>
    </form>
    {reportsFor && <div className="ui-panel"><p>Active direct reports for <strong>{reportsFor.fullName}</strong>. Edit each report to reassign or intentionally clear their manager.</p><button type="button" className="ui-secondary" onClick={() => { setReportsFor(null); setFilter(previous => ({ ...previous, reportsTo: '', page: 1 })); setDraft(previous => ({ ...previous, reportsTo: '' })); }}>Show all employees</button></div>}
    <section aria-label="Directory results" aria-busy={employees.isFetching} className="space-y-3">
      <p className="text-sm">Applied: {filter.search ? `“${filter.search}”` : 'all search text'}; {filter.status}; attention {filter.attention}; {filter.departmentId ? departments.data?.find(d => d.id === filter.departmentId)?.name ?? 'selected department' : 'all departments'}; sort {filter.sort}; page {filter.page}.</p>
      {employees.isPending ? <p role="status">Loading employees…</p> : employees.isError ? <div role="alert">Unable to load employees. You may no longer have HR access. <button className="ui-secondary" onClick={() => void employees.refetch()}>Try again</button></div> : data && <>
        <p role="status">{data.totalCount} matching employees; page {data.page} of {pages}. {employees.isFetching ? 'Refreshing previously loaded results…' : `Snapshot ${new Date(data.asOfUtc).toLocaleString()}.`}</p>
        <p className="text-sm sm:hidden">Scroll the table sideways for department, roles, Manager and actions.</p>
        <EmployeeTable employees={data.items} editing={editing !== undefined} onEdit={openForm} onResend={employee => { setNotice(''); setInviting(employee); }} onDeactivate={employee => { rememberFocus(); setNotice(''); setDeactivating(employee); }} onView={employee => { rememberFocus(); setDetails(employee); }} />
        <nav aria-label="Directory pages" className="flex flex-wrap items-center gap-3"><button className="ui-secondary" disabled={data.page === 1 || employees.isFetching} onClick={() => setFilter({ ...filter, page: filter.page - 1 })}>Previous page</button><span>Page {data.page} of {pages}</span><button className="ui-secondary" disabled={data.page >= pages || employees.isFetching} onClick={() => setFilter({ ...filter, page: filter.page + 1 })}>Next page</button></nav>
      </>}
    </section>
    {deactivating && <EmployeeDeactivationDialog employee={deactivating} returnFocus={opener.current} onClose={() => setDeactivating(null)}
      onSuccess={count => { if (!currentSession()) return; setDeactivating(null); setNotice(`Employee deactivated. ${count} Pending request${count === 1 ? '' : 's'} cancelled.`); }}
      onInactive={employee => { setDeactivating(null); setDetails(employee); }} onManageReports={() => { const next = { ...initial('all'), status: 'Active', reportsTo: deactivating.id }; setReportsFor(deactivating); setFilter(next); setDraft(next); setDeactivating(null); }} />}
    {inviting && <ResendActivationDialog employee={inviting} onClose={() => setInviting(null)} onSuccess={state => { if (!currentSession()) return; setInviting(null); setNotice(state === 'PickupReady' ? 'New private development invitation ready for pickup.' : 'Invitation delivery failed. You can explicitly resend.'); }} />}
    {details && <EmployeeDetailsDialog employee={details} returnFocus={opener.current} onClose={() => setDetails(null)} />}
  </div>;
}
