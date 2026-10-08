import { useEffect, useRef, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { ConfirmationDialog } from '../../../components/ConfirmationDialog';
import { useAuth } from '../../auth/hooks/useAuth';
import { authHttpClient } from '../../auth/api';
import { isConflict, problemMessage } from '../../leave-configuration/problems';
import { useDeactivateEmployee } from '../api';
import type { Employee } from '../types';

interface DeactivationProps {
  employee: Employee;
  returnFocus: HTMLElement | null;
  onClose: () => void;
  onSuccess: (count: number) => void;
  onInactive: (employee: Employee) => void;
  onManageReports: () => void;
}

/** Keeps an immutable version snapshot and reason until a deliberate reload, discard, or committed decision. */
export function EmployeeDeactivationDialog({ employee, returnFocus, onClose, onSuccess, onInactive, onManageReports }: DeactivationProps) {
  const [snapshot, setSnapshot] = useState(employee);
  const [reason, setReason] = useState('');
  const [validation, setValidation] = useState('');
  const [reloadError, setReloadError] = useState('');
  const [reloading, setReloading] = useState(false);
  const [discard, setDiscard] = useState<'close' | 'reload' | 'reports' | null>(null);
  const keep = useRef<HTMLButtonElement>(null);
  const inFlight = useRef(false);
  const reloadController = useRef<AbortController | null>(null);
  const mutation = useDeactivateEmployee();
  const queryClient = useQueryClient();
  const { user, sessionVersion, getSessionVersion, completeSelfDeactivation } = useAuth();
  const currentSession = () => getSessionVersion() === sessionVersion;
  const busy = mutation.isPending || reloading;
  useEffect(() => () => reloadController.current?.abort(), []);
  useEffect(() => { if (discard) keep.current?.focus(); }, [discard]);

  const reload = async () => {
    if (inFlight.current || !currentSession()) return;
    inFlight.current = true;
    setReloading(true); setReloadError('');
    const controller = new AbortController(); reloadController.current = controller;
    try {
      const latest = (await authHttpClient.get<Employee>(`/employees/${snapshot.id}`, { signal: controller.signal })).data;
      if (!currentSession()) return;
      queryClient.setQueryData<Employee[]>(['employees', user?.id], existing =>
        existing?.map(record => record.id === latest.id ? latest : record));
      if (!latest.isActive) { onInactive(latest); return; }
      setSnapshot(latest); setReason(''); setValidation(''); mutation.reset();
    } catch (error) {
      if (currentSession() && !controller.signal.aborted) setReloadError(problemMessage(error));
    } finally {
      inFlight.current = false;
      if (currentSession()) setReloading(false);
    }
  };
  const act = (action: 'close' | 'reload' | 'reports') => {
    if (busy || inFlight.current) return;
    if (action === 'reload') void reload();
    else if (action === 'reports') onManageReports();
    else onClose();
  };
  const requestAction = (action: 'close' | 'reload' | 'reports') => {
    if (busy || inFlight.current) return;
    if (discard) {
      setDiscard(null);
      requestAnimationFrame(() => document.getElementById('deactivation-reason')?.focus());
      return;
    }
    if (reason.length) setDiscard(action); else act(action);
  };
  const submit = async () => {
    if (inFlight.current || busy || discard || !currentSession()) return;
    const trimmed = reason.trim();
    if (!trimmed || trimmed.length > 500) {
      setValidation('Enter a reason between 1 and 500 characters.');
      document.getElementById('deactivation-reason')?.focus(); return;
    }
    inFlight.current = true; setValidation('');
    try {
      const result = await mutation.mutateAsync({ employee: snapshot, reason: trimmed });
      if (!currentSession()) return;
      if (snapshot.identityUserId === user?.id) {
        completeSelfDeactivation(result.cancelledRequestCount);
      } else onSuccess(result.cancelledRequestCount);
    } catch {
      // Safe server errors remain in mutation state; the reason and original version are retained.
    } finally { inFlight.current = false; }
  };

  return <ConfirmationDialog title="Deactivate employee" confirmLabel="Deactivate employee" busy={busy}
    confirmDisabled={Boolean(discard)} returnFocus={returnFocus}
    onCancel={() => requestAction('close')} onConfirm={() => void submit()}>
    <p className="break-words"><strong className="text-slate-900">{snapshot.fullName}</strong><br />{snapshot.email}</p>
    <p>Deactivation blocks account access and cancels this employee’s Pending leave requests. Their profile, reporting links, historical decisions and audits are preserved. This workflow cannot reactivate the account.</p>
    {snapshot.identityUserId === user?.id && <p className="font-semibold text-amber-800">You are deactivating your own account. You will be signed out after success.</p>}
    <label className="block font-semibold text-slate-900" htmlFor="deactivation-reason">Reason for deactivation</label>
    <textarea id="deactivation-reason" rows={4} maxLength={500} value={reason} disabled={busy || Boolean(discard)}
      aria-invalid={Boolean(validation)} aria-describedby={`reason-help${validation ? ' reason-error' : ''}${mutation.error ? ' deactivation-error' : ''}`}
      className="ui-input w-full resize-y" onChange={event => { setReason(event.target.value); setValidation(''); }} />
    <p id="reason-help">Required. Up to 500 characters after trimming. {reason.trim().length}/500</p>
    {validation && <p id="reason-error" role="alert" className="text-rose-800">{validation}</p>}
    {mutation.error && <div id="deactivation-error" role="alert" className="space-y-2 text-rose-800">
      <p>{problemMessage(mutation.error)}</p>
      {isConflict(mutation.error) && <><p>Reload to review the current record before trying again. Reload discards your entered reason; no request is retried automatically.</p>
        <button type="button" disabled={busy || Boolean(discard)} className="ui-secondary" onClick={() => requestAction('reload')}>Reload employee</button></>}
    </div>}
    {reloadError && <p role="alert" className="text-rose-800">{reloadError}</p>}
    <p>Managers with active direct reports need those reports reassigned first.</p>
    <button type="button" className="ui-secondary" disabled={busy || Boolean(discard)} onClick={() => requestAction('reports')}>Manage reporting assignments</button>
    {discard && <div className="rounded-lg border border-amber-300 bg-amber-50 p-3" role="group" aria-label="Discard entered reason">
      <p className="font-semibold text-amber-900">{discard === 'reload' ? 'Reloading discards the entered reason and loads a new version.' : 'Leave this dialog and discard the entered reason?'}</p>
      <div className="mt-2 flex flex-wrap gap-2">
        <button ref={keep} type="button" className="ui-secondary" onClick={() => { setDiscard(null); requestAnimationFrame(() => document.getElementById('deactivation-reason')?.focus()); }}>Keep reason</button>
        <button type="button" className="ui-danger" onClick={() => { const action = discard; setDiscard(null); act(action); }}>Discard reason and {discard === 'reload' ? 'reload' : 'continue'}</button>
      </div>
    </div>}
    {busy && <p role="status">{reloading ? 'Reloading employee…' : 'Deactivating employee… Please keep this dialog open.'}</p>}
  </ConfirmationDialog>;
}
