import { useRef, useState } from 'react';
import { useAuth } from '../auth/hooks/useAuth';
import { useConfigurationWrite, useLeaveConfiguration } from './api';
import { LeaveTypeForm, PolicyForm } from './ConfigurationForms';
import { ConfirmationDialog } from '../../components/ConfirmationDialog';
import { problemMessage, isConflict } from './problems';
import type { ConfigurationWrite, LeavePolicy, ManagedLeaveType } from './types';

type Editing = { kind: 'leave-types'; record: ManagedLeaveType | null } | { kind: 'leave-policies'; record: LeavePolicy | null };
/** HR workspace uses immutable edit/delete snapshots instead of rebasing unsaved input on refetch. */
export function LeaveConfigurationPage() {
  const { user } = useAuth();
  const account = useRef(user?.id); account.current = user?.id;
  const { types, policies } = useLeaveConfiguration();
  const write = useConfigurationWrite();
  const saving = useRef(false);
  const [tab, setTab] = useState<'leave-types' | 'leave-policies'>('leave-types');
  const [search, setSearch] = useState('');
  const [editing, setEditing] = useState<Editing | null>(null);
  const [nestedPolicy, setNestedPolicy] = useState(false);
  const [deleting, setDeleting] = useState<Editing | null>(null);
  const [notice, setNotice] = useState('');
  const [reloadError, setReloadError] = useState('');
  const [formRevision, setFormRevision] = useState(0);
  const formAnchor = useRef<HTMLDivElement>(null);
  const deleteOpener = useRef<HTMLButtonElement | null>(null);
  const active = tab === 'leave-types' ? types : policies;
  const open = (value: Editing) => {
    setEditing(value); setNotice(''); setReloadError(''); write.reset();
    requestAnimationFrame(() => { formAnchor.current?.scrollIntoView({ block: 'start' }); formAnchor.current?.focus(); });
  };
  const save = async (operation: ConfigurationWrite, nested = false) => {
    const initiatingAccount = user?.id;
    if (!initiatingAccount || saving.current) return;
    saving.current = true;
    try {
      await write.mutateAsync({ ...operation, accountId: initiatingAccount });
      if (account.current !== initiatingAccount) return;
      setNotice(operation.method === 'delete' ? 'Record deleted.' : operation.method === 'post' ? 'Record created.' : 'Changes saved.');
      if (nested) { setNestedPolicy(false); write.reset(); } else { setEditing(null); setDeleting(null); }
    } catch { /* Mutation owns the safe error display and preserves the draft. */ }
    finally { saving.current = false; }
  };
  const reload = async () => {
    if (!editing || write.isPending) return;
    const initiatingAccount = user?.id;
    const results = await Promise.all([types.refetch(), policies.refetch()]);
    if (account.current !== initiatingAccount) return;
    const result = editing.kind === 'leave-types' ? results[0] : results[1];
    const record = result.data?.find(row => row.id === editing.record?.id);
    if (results.some(result => result.error) || (editing.record && !record)) { setReloadError('Unable to reload this record and its policies. It may have been deleted or access may have changed. Your input is still kept.'); return; }
    if (editing.kind === 'leave-types') setEditing({ kind: editing.kind, record: (record as ManagedLeaveType | undefined) ?? null });
    else setEditing({ kind: editing.kind, record: (record as LeavePolicy | undefined) ?? null });
    setFormRevision(value => value + 1); setReloadError(''); write.reset();
  };
  const actions = { busy: write.isPending, error: write.error, reloadError, onReload: () => void reload(),
    onCancel: () => { setEditing(null); setNestedPolicy(false); write.reset(); setReloadError(''); } };
  const rows = active.data?.filter(row => row.name.toLocaleLowerCase().includes(search.trim().toLocaleLowerCase())) ?? [];
  return <div className="workspace-page space-y-6">
    <section className="page-intro ui-panel">
      <div className="mt-5 flex flex-wrap items-start justify-between gap-4"><div><p className="text-xs font-bold uppercase tracking-widest text-indigo-700">HR administration</p>
        <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-600">Organize leave types and the rules they share. Keep entitlements and approval rules clear.</p></div>
        <button className="ui-primary" disabled={!!editing || !!deleting || write.isPending || active.isPending || !!active.error} onClick={() => open({ kind: tab, record: null })}>{tab === 'leave-types' ? 'New leave type' : 'New policy'}</button>
      </div>
    </section>
    <nav aria-label="Leave configuration sections" className="flex gap-2 rounded-xl border border-slate-200 bg-white p-2">
      {(['leave-types', 'leave-policies'] as const).map(value => <button key={value} type="button" aria-current={tab === value ? 'page' : undefined}
        className={tab === value ? 'ui-primary' : 'ui-secondary'} onClick={() => { setTab(value); setSearch(''); }}>{value === 'leave-types' ? 'Leave types' : 'Policies'}</button>)}
    </nav>
    {notice && <p role="status" className="rounded-xl border border-emerald-200 bg-emerald-50 p-4 text-emerald-800">{notice}</p>}
    {editing && <div ref={formAnchor} tabIndex={-1} className="space-y-4">
      {editing.kind === 'leave-types' ? <LeaveTypeForm key={`type-${formRevision}-${editing.record?.id ?? 'new'}`} {...actions}
        busy={write.isPending || nestedPolicy} error={nestedPolicy ? null : write.error} type={editing.record} policies={policies.data ?? []}
        policiesUnavailable={policies.isPending || !!policies.error} onRetryPolicies={() => void policies.refetch()}
        onCreatePolicy={() => { setNestedPolicy(true); write.reset(); }}
        onSave={body => void save({ kind: 'leave-types', method: editing.record ? 'put' : 'post', id: editing.record?.id,
          body: { ...body, ...(editing.record ? { expectedVersion: editing.record.version } : {}) } })} />
        : <PolicyForm key={`policy-${formRevision}-${editing.record?.id ?? 'new'}`} {...actions} policy={editing.record}
          onSave={body => void save({ kind: 'leave-policies', method: editing.record ? 'put' : 'post', id: editing.record?.id,
            body: { ...body, ...(editing.record ? { expectedVersion: editing.record.version } : {}) } })} />}
      {nestedPolicy && <PolicyForm {...actions} policy={null} onCancel={() => { setNestedPolicy(false); write.reset(); }}
        onSave={body => void save({ kind: 'leave-policies', method: 'post', body }, true)} />}
    </div>}
    <section className="ui-panel" aria-labelledby="list-title">
      <div className="flex flex-wrap items-end justify-between gap-4"><h2 id="list-title" className="text-xl font-bold">{tab === 'leave-types' ? 'Leave types' : 'Policies'}</h2>
        <label className="ui-label w-full sm:w-72">Search {tab === 'leave-types' ? 'leave types' : 'policies'}<input type="search" className="ui-input" value={search} onChange={e => setSearch(e.target.value)} /></label></div>
      {active.isPending ? <p role="status" className="py-8">Loading records…</p> : active.error ? <div role="alert" className="mt-5 ui-error">{problemMessage(active.error)} <button className="underline" onClick={() => void active.refetch()}>Try again</button></div>
        : !rows.length ? <div className="py-12 text-center"><h3 className="font-semibold">{search ? 'No matching records' : tab === 'leave-types' ? 'No leave types yet' : 'No policies yet'}</h3><p className="mt-2 text-sm text-slate-600">{search ? 'Try a different search.' : 'Use the New action above to create your first record.'}</p></div>
        : <ul className="mt-5 divide-y divide-slate-200">{rows.map(row => {
          const isType = 'leavePolicyId' in row;
          return <li key={row.id} className="grid gap-4 py-5 md:grid-cols-[minmax(0,1fr)_minmax(0,1fr)_auto]">
            <div className="min-w-0"><h3 className="break-words font-bold text-slate-900">{row.name}</h3><p className="mt-1 break-words text-sm text-slate-600">{isType ? row.policyName : `${row.linkedLeaveTypes.length} linked types`}</p>
              {!isType && row.linkedLeaveTypes.length > 0 && <p className="mt-2 break-words text-sm text-slate-600">{row.linkedLeaveTypes.map(type => type.name).join(', ')}</p>}</div>
            <div className="text-sm text-slate-600"><p className="font-semibold text-slate-900">{row.defaultBalance} calendar days</p><p className="mt-1">{row.allowOverlap ? 'Same-type overlap allowed' : 'Same-type overlap prohibited'}</p>
              {row.defaultBalance === 0 && <p>Positive-day requests blocked</p>}
              <p className="mt-2">{isType ? `${row.requestCount} requests (all statuses)` : `${row.linkedLeaveTypes.reduce((sum, type) => sum + type.requestCount, 0)} requests across linked types`}</p>
              {!row.canDelete && <p className="mt-1 text-amber-800">{isType ? 'Request history prevents deletion.' : 'Linked leave types prevent deletion.'}</p>}</div>
            <div className="flex items-start gap-2"><button className="ui-secondary" disabled={!!editing || !!deleting} onClick={() => open(isType ? { kind: 'leave-types', record: row } : { kind: 'leave-policies', record: row })}>Edit<span className="sr-only"> {row.name}</span></button>
              <button className="ui-danger" disabled={!!editing || !!deleting} onClick={event => { deleteOpener.current = event.currentTarget; setDeleting(isType ? { kind: 'leave-types', record: row } : { kind: 'leave-policies', record: row }); write.reset(); setNotice(''); }}>Delete<span className="sr-only"> {row.name}</span></button></div>
          </li>;
        })}</ul>}
    </section>
    {deleting?.record && <ConfirmationDialog title={`Delete ${deleting.record.name}?`} confirmLabel="Delete record" busy={write.isPending} returnFocus={deleteOpener.current}
      onCancel={() => { setDeleting(null); write.reset(); }} onConfirm={() => {
        if (deleting.record) void save({ kind: deleting.kind, method: 'delete', id: deleting.record.id, expectedVersion: deleting.record.version });
      }}>
      <p className="break-words">This permanently deletes “{deleting.record.name}”.</p><p>{deleting.kind === 'leave-types' ? 'Deletion is allowed only if no request references this type, including rejected or cancelled requests. Request and audit history will never be deleted.' : 'Deletion is allowed only if no leave type uses this policy. Linked types are never reassigned or deleted automatically.'}</p>
      {!deleting.record.canDelete && <p className="font-semibold text-amber-800">This record currently has references and cannot be deleted. Reassign linked types where appropriate; request history must be preserved.</p>}
      {write.error && <div role="alert" className="ui-error">{problemMessage(write.error)}{isConflict(write.error) && <p className="mt-2">Cancel and refresh the list before starting a new deletion. This confirmation retains its original version.</p>}<button type="button" className="mt-3 ui-secondary" disabled={write.isPending} onClick={() => { setDeleting(null); write.reset(); void types.refetch(); void policies.refetch(); }}>Cancel deletion and reload lists</button></div>}
    </ConfirmationDialog>}
    <button data-focus-fallback type="button" className="ui-secondary" disabled={!!editing || !!deleting || types.isFetching || policies.isFetching} onClick={() => { void types.refetch(); void policies.refetch(); }}>Refresh lists</button>
  </div>;
}
