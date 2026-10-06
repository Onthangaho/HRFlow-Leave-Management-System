import { useRef, useState, type FormEvent } from 'react';
import type { LeavePolicy, ManagedLeaveType, PolicyInput, TypeInput } from './types';
import { problemMessage, isConflict } from './problems';
import { ConfirmationDialog } from '../../components/ConfirmationDialog';

interface FormActions { busy: boolean; error: unknown; reloadError: string; onCancel: () => void; onReload: () => void }
function FormFooter({ busy, error, reloadError, onCancel, onReload, dirty, saveDisabled }: FormActions & { dirty: boolean; saveDisabled?: boolean }) {
  const [discard, setDiscard] = useState<'cancel' | 'reload' | null>(null);
  return <>
    {error != null && <div role="alert" className="ui-error"><p>{problemMessage(error)}</p>
      {isConflict(error) && <><p className="mt-2">Nothing was saved. Reload the current record and choices before trying again. Reload discards unsaved changes.</p>
        <button type="button" className="mt-3 ui-secondary" disabled={busy} onClick={() => setDiscard('reload')}>Reload current record</button></>}
    </div>}
    {reloadError && <p role="alert" className="ui-error">{reloadError}</p>}
    <div className="flex flex-wrap gap-3">
      <button className="ui-primary" type="submit" disabled={busy || saveDisabled}>{busy ? 'Saving…' : 'Save changes'}</button>
      <button className="ui-secondary" type="button" disabled={busy} onClick={() => dirty ? setDiscard('cancel') : onCancel()}>Cancel</button>
    </div>
    {discard && <ConfirmationDialog title="Discard unsaved changes?" confirmLabel={discard === 'reload' ? 'Discard and reload' : 'Discard changes'}
      onCancel={() => setDiscard(null)} onConfirm={() => { const action = discard; setDiscard(null); if (action === 'reload') onReload(); else onCancel(); }}>
      <p>Your unsaved form values will be lost.</p></ConfirmationDialog>}
  </>;
}

/** Shows shared-rule effects and holds input independently of background query snapshots. */
export function PolicyForm({ policy, onSave, ...actions }: FormActions & { policy: LeavePolicy | null; onSave: (values: PolicyInput) => void }) {
  const [name, setName] = useState(policy?.name ?? '');
  const [days, setDays] = useState(String(policy?.defaultBalance ?? 20));
  const [overlap, setOverlap] = useState(policy?.allowOverlap ?? false);
  const [validation, setValidation] = useState('');
  const nameRef = useRef<HTMLInputElement>(null);
  const dirty = name !== (policy?.name ?? '') || days !== String(policy?.defaultBalance ?? 20) || overlap !== (policy?.allowOverlap ?? false);
  const submit = (event: FormEvent) => {
    event.preventDefault();
    if (actions.busy) return;
    if (!name.trim() || name.trim().length > 100 || Array.from(name).some(character => character.charCodeAt(0) < 32 || character.charCodeAt(0) === 127)) { setValidation('Enter a name of 1–100 characters without control characters.'); nameRef.current?.focus(); return; }
    const entitlement = Number(days);
    if (!days.trim() || !Number.isInteger(entitlement) || entitlement < 0 || entitlement > 2147483647) { setValidation('Enter a whole number of calendar days from 0 to 2147483647.'); return; }
    setValidation(''); onSave({ name: name.trim(), defaultBalance: entitlement, allowOverlap: overlap });
  };
  return <section className="ui-panel" aria-labelledby="policy-form-title">
    <h2 id="policy-form-title" className="break-words text-xl font-bold">{policy ? `Edit policy: ${policy.name}` : 'New policy'}</h2>
    <form onSubmit={submit} className="mt-5 space-y-5">
      <fieldset disabled={actions.busy} className="space-y-5">
        <label className="ui-label">Policy name<input ref={nameRef} className="ui-input" value={name} onChange={e => setName(e.target.value)} required maxLength={100} /></label>
        <label className="ui-label">Entitlement (calendar days)<input className="ui-input" type="number" min="0" max="2147483647" step="1" value={days} onChange={e => setDays(e.target.value)} required aria-describedby="entitlement-help" /></label>
        <p id="entitlement-help" className="text-sm text-slate-600">Zero entitlement blocks positive-day requests. Entitlement does not reset annually.</p>
        <label className="flex items-start gap-3 text-sm font-semibold"><input className="mt-1 size-4 accent-indigo-600" type="checkbox" checked={overlap} onChange={e => setOverlap(e.target.checked)} />Allow approved requests of the same leave type to overlap</label>
      </fieldset>
      {policy && <div className="ui-warning"><p>Changes affect current balances, new submissions, and pending approvals for every linked type. Historical decisions are preserved.</p>
        <p className="mt-2 font-semibold">Linked types ({policy.linkedLeaveTypes.length})</p>
        {policy.linkedLeaveTypes.length ? <ul className="mt-1 list-inside list-disc">{policy.linkedLeaveTypes.map(type => <li className="break-words" key={type.id}>{type.name} — {type.requestCount} requests</li>)}</ul> : <p>No linked types.</p>}
      </div>}
      {validation && <p role="alert" className="ui-error">{validation}</p>}
      <FormFooter {...actions} dirty={dirty} />
    </form>
  </section>;
}

/** Retains the type draft while a policy is created alongside it; policy switches are explicit. */
export function LeaveTypeForm({ type, policies, policiesUnavailable, onCreatePolicy, onRetryPolicies, onSave, ...actions }: FormActions & {
  type: ManagedLeaveType | null; policies: LeavePolicy[]; policiesUnavailable: boolean;
  onCreatePolicy: () => void; onRetryPolicies: () => void; onSave: (values: TypeInput) => void;
}) {
  const [name, setName] = useState(type?.name ?? '');
  const [policyId, setPolicyId] = useState(type?.leavePolicyId ?? '');
  const [validation, setValidation] = useState('');
  const selected = policies.find(policy => policy.id === policyId);
  const dirty = name !== (type?.name ?? '') || policyId !== (type?.leavePolicyId ?? '');
  const submit = (event: FormEvent) => {
    event.preventDefault();
    if (actions.busy) return;
    if (!name.trim() || name.trim().length > 100 || Array.from(name).some(character => character.charCodeAt(0) < 32 || character.charCodeAt(0) === 127)) { setValidation('Enter a name of 1–100 characters without control characters.'); return; }
    if (policiesUnavailable || !selected) { setValidation('Choose an available policy before saving.'); return; }
    setValidation(''); onSave({ name: name.trim(), leavePolicyId: policyId });
  };
  return <section className="ui-panel" aria-labelledby="type-form-title">
    <h2 id="type-form-title" className="break-words text-xl font-bold">{type ? `Edit leave type: ${type.name}` : 'New leave type'}</h2>
    <form onSubmit={submit} className="mt-5 space-y-5">
      <fieldset disabled={actions.busy} className="space-y-5">
        <label className="ui-label">Leave type name<input className="ui-input" value={name} onChange={e => setName(e.target.value)} required maxLength={100} /></label>
        <label className="ui-label">Policy<select aria-label="Policy" className="ui-input" value={policyId} onChange={e => setPolicyId(e.target.value)} required disabled={policiesUnavailable}>
          <option value="">Choose a policy</option>
          {policyId && !selected && <option value={policyId} disabled>Selected policy is unavailable</option>}
          {policies.map(policy => <option key={policy.id} value={policy.id}>{policy.name}</option>)}
        </select></label>
        {policiesUnavailable ? <p role="alert">Policies could not be loaded. <button type="button" className="underline" onClick={onRetryPolicies}>Retry policies</button></p>
          : policies.length === 0 && <p className="text-sm text-slate-600">Create a policy first to define entitlement and overlap rules.</p>}
        <button type="button" className="ui-secondary" onClick={onCreatePolicy}>Create a policy</button>
      </fieldset>
      {selected && !policiesUnavailable && <div className="rounded-xl bg-slate-50 p-4 text-sm text-slate-700">
        <p className="font-semibold">{selected.name}</p><p className="mt-1">{selected.defaultBalance} calendar days entitlement</p>
        <p>{selected.allowOverlap ? 'Approved requests of the same type may overlap.' : 'Approved requests of the same type must not overlap.'}</p>
        {selected.defaultBalance === 0 && <p className="mt-2 font-semibold">Positive-day requests are blocked.</p>}
      </div>}
      {type && policyId !== type.leavePolicyId && <p className="ui-warning">Switching this type’s policy changes its current balance calculation, new submission rules, and pending approval rules. Historical decisions remain preserved.</p>}
      {validation && <p role="alert" className="ui-error">{validation}</p>}
      <FormFooter {...actions} saveDisabled={policiesUnavailable} dirty={dirty} />
    </form>
  </section>;
}
