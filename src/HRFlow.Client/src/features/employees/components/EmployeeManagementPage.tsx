import { useRef, useState } from 'react';
import { useEmployees } from '../api';
import { useAuth } from '../../auth/hooks/useAuth';
import type { Employee } from '../types';
import { ResendActivationDialog } from './ResendActivationDialog';
import { EmployeeForm } from './EmployeeForm';
import { EmployeeTable } from './EmployeeTable';
import { EmployeeDeactivationDialog } from './EmployeeDeactivationDialog';
import { EmployeeDetailsDialog } from './EmployeeDetailsDialog';

/** Remounts protected UI state on each login epoch, including a later login by the same account. */
export function EmployeeManagementPage() {
  const { user, sessionVersion } = useAuth();
  return <EmployeeManagementWorkspace key={`${user?.id}:${sessionVersion}`} />;
}

function EmployeeManagementWorkspace() {
  const employees = useEmployees();
  const { sessionVersion, getSessionVersion } = useAuth();
  const currentSession = () => getSessionVersion() === sessionVersion;
  const [editing, setEditing] = useState<Employee | null | undefined>(undefined);
  const [deactivating, setDeactivating] = useState<Employee | null>(null);
  const [details, setDetails] = useState<Employee | null>(null);
  const [inviting, setInviting] = useState<Employee | null>(null);
  const [notice, setNotice] = useState('');
  const [reloadError, setReloadError] = useState('');
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('All');
  const [reportsFor, setReportsFor] = useState<Employee | null>(null);
  const opener = useRef<HTMLElement | null>(null);
  const rememberFocus = () => { opener.current = document.activeElement as HTMLElement | null; };
  const openForm = (employee: Employee | null) => { setEditing(employee); setNotice(''); setReloadError(''); };
  const reload = async () => {
    if (!editing) return;
    const result = await employees.refetch();
    if (!currentSession()) return;
    const current = result.data?.find(employee => employee.id === editing.id);
    if (!result.error && current) {
      if (current.isActive) setEditing(current);
      else { rememberFocus(); setEditing(undefined); setDetails(current); }
      setReloadError('');
    } else setReloadError('Unable to reload this employee. Try again or cancel the edit.');
  };
  const query = search.trim().toLocaleLowerCase();
  const filtered = (employees.data ?? []).filter(employee =>
    (status === 'All' || employee.isActive === (status === 'Active')) &&
    (!reportsFor || employee.managerId === reportsFor.id) &&
    [employee.fullName, employee.email, employee.departmentName, employee.managerName ?? '', ...employee.roles]
      .some(value => value.toLocaleLowerCase().includes(query)),
  );
  return (
    <div className="workspace-page space-y-6">
      <section className="page-intro ui-panel">
        <div>
          <p className="mt-2 text-sm text-slate-600">Manage employee profiles, access, and reporting relationships.</p></div>
        <button type="button" data-focus-fallback disabled={editing !== undefined || employees.isPending || Boolean(employees.error)}
          onClick={() => openForm(null)} className="ui-primary">New Employee</button>
      </section>
      {notice && <p role="status" className="break-words text-emerald-800">{notice}</p>}
      {editing !== undefined && <EmployeeForm employee={editing} employees={employees.data ?? []}
        onSuccess={delivery => { if (!currentSession()) return; setNotice(editing ? 'Employee updated.' : delivery === 'PickupReady' ? 'Employee created. Private development invitation is ready for pickup.' : 'Employee created, but invitation delivery failed. Use Resend invitation to recover.'); setEditing(undefined); }}
        onCancel={() => setEditing(undefined)} onReload={reload} reloadError={reloadError} />}
      <div className="grid gap-4 sm:grid-cols-[1fr_12rem]">
        <div><label htmlFor="employee-search" className="mb-1 block text-sm font-semibold">Search employees</label>
          <input id="employee-search" type="search" className="ui-input w-full" value={search} onChange={event => setSearch(event.target.value)} placeholder="Name, email, department or role" /></div>
        <div><label htmlFor="employee-status" className="mb-1 block text-sm font-semibold">Account status</label>
          <select id="employee-status" className="ui-input w-full" value={status} onChange={event => setStatus(event.target.value)}>
            {['All', 'Active', 'Inactive'].map(value => <option key={value}>{value}</option>)}
          </select></div>
      </div>
      {reportsFor && <div className="rounded-lg bg-slate-50 p-4">
        <p>Active direct reports for <strong>{reportsFor.fullName}</strong>. Edit each report to assign another eligible manager or intentionally clear their assignment.</p>
        <button type="button" className="ui-secondary mt-2" onClick={() => setReportsFor(null)}>Show all employees</button>
      </div>}
      {employees.isPending ? <p role="status">Loading employees…</p> :
        employees.error ? <div role="alert" className="text-rose-800">
          Unable to load employees. You may no longer have HR access. <button type="button" className="underline" onClick={() => void employees.refetch()}>Try again</button>
        </div> : <><p role="status" className="text-sm text-slate-600">{filtered.length} employee{filtered.length === 1 ? '' : 's'} shown</p>
          <EmployeeTable onResend={employee => { setNotice(''); setInviting(employee); }} employees={filtered} onEdit={openForm} editing={editing !== undefined}
            onDeactivate={employee => { rememberFocus(); setNotice(''); setDeactivating(employee); }}
            onView={employee => { rememberFocus(); setDetails(employee); }} /></>}
      {deactivating && <EmployeeDeactivationDialog employee={deactivating} returnFocus={opener.current}
        onClose={() => setDeactivating(null)}
        onSuccess={count => { if (!currentSession()) return; setDeactivating(null); setNotice(`Employee deactivated. ${count} Pending request${count === 1 ? '' : 's'} cancelled.`); }}
        onInactive={employee => { setDeactivating(null); setDetails(employee); }}
        onManageReports={() => { setReportsFor(deactivating); setStatus('Active'); setSearch(''); setDeactivating(null); }} />}
      {inviting && <ResendActivationDialog employee={inviting} onClose={() => setInviting(null)} onSuccess={state => { if (!currentSession()) return; setInviting(null); setNotice(state === 'PickupReady' ? 'New private development invitation ready for pickup.' : 'Invitation delivery failed. You can explicitly resend.'); }} />}
      {details && <EmployeeDetailsDialog employee={details} returnFocus={opener.current} onClose={() => setDetails(null)} />}
    </div>
  );
}
