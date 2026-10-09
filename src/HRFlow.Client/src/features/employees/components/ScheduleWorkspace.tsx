import { useRef, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { authHttpClient } from '../../auth/api';
import { useAuth } from '../../auth/hooks/useAuth';
import { problemMessage } from '../../leave-configuration/problems';
import { ConfirmationDialog } from '../../../components/ConfirmationDialog';
/** Immutable fixed-pattern revision; minutes convert exactly to labelled hours for display. */
export interface ScheduleRevision { id: string; name: string; effectiveFrom: string; minutes: number[]; recordedAtUtc: string }
/** Dedicated schedule generation, independent of HR employment/profile edits. */
export interface ScheduleHistory { version: string; revisions: ScheduleRevision[] }
const days = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
/** Read-only employment history makes incomplete legacy inputs and calendar charging explicit. */
export function ScheduleRevisions({ history }: { history: ScheduleHistory }) {
  return <section className="ui-panel space-y-3"><h3 className="text-lg font-semibold">Weekly schedule history</h3><p className="text-sm">Fixed weekly daytime patterns only; rotating, overnight and variable shifts are unsupported. Leave remains calculated on inclusive calendar days. Each revision applies until the next effective date; history is preserved.</p>
    {!history.revisions.length && <p role="status">Unknown: no confirmed schedule recorded.</p>}
    <ul className="space-y-3">{history.revisions.map(row => <li className="rounded border p-3" key={row.id}><p className="break-words font-semibold">{row.name} · Effective from {row.effectiveFrom}</p><p className="text-sm">{row.minutes.map((m,i) => m ? `${days[i]}: ${m/60} working hours` : '').filter(Boolean).join('; ')}</p><p className="text-xs">Revision {row.id}</p></li>)}</ul></section>;
}
/** Independent append form freezes the loaded generation; explicit reload never silently rebases a draft. */
export function ScheduleWorkspace({ employeeId, readOnly = false }: { employeeId: string; readOnly?: boolean }) {
  const { user, sessionVersion, getSessionVersion } = useAuth();
  const active = () => getSessionVersion() === sessionVersion;
  const query = useQuery({ queryKey:['employment-schedules',user?.id,sessionVersion,employeeId], enabled:Boolean(user?.roles.includes('HR Administrator')),
    queryFn:async ({signal}) => (await authHttpClient.get<ScheduleHistory>(`/employees/${employeeId}/schedules`,{signal})).data, retry:false });
  const [snapshot,setSnapshot] = useState<ScheduleHistory | null>(null);
  const [name,setName] = useState(''); const [date,setDate] = useState(''); const [hours,setHours] = useState(days.map(()=> '0'));
  const [saving,setSaving] = useState(false); const busy = useRef(false); const [message,setMessage] = useState(''); const [discard,setDiscard] = useState(false); const reloadRef=useRef<HTMLButtonElement>(null);
  const dirty=Boolean(name||date||hours.some(h=>h!=='0'));
  async function reload() { const r=await query.refetch(); if(!active()) return; if(r.error||!r.data){setMessage(problemMessage(r.error));return;} setSnapshot(r.data);setName('');setDate('');setHours(days.map(()=> '0'));setMessage('Latest schedule loaded; draft discarded.');setDiscard(false); }
  return <div className="mt-6 space-y-4">
    {query.isPending && <p role="status">Loading schedule history...</p>}
    {query.error && <p role="alert" className="ui-error">{problemMessage(query.error)} <button type="button" onClick={()=>void query.refetch()}>Retry schedules</button></p>}
    {(snapshot||query.data) && <ScheduleRevisions history={(snapshot||query.data)!} />}
    {!readOnly && !snapshot && query.data && <button type="button" className="ui-secondary" onClick={()=>setSnapshot(query.data!)}>Record a schedule revision</button>}
    {!readOnly && snapshot && <form className="ui-panel space-y-4" onSubmit={async e=>{e.preventDefault();if(busy.current)return;const minutes=hours.map(h=>Number(h)*60);if(!name.trim()||name.trim().length>100||!date||date<'1900-01-01'||date>'2100-12-31'||minutes.some(m=>!Number.isInteger(m)||m<0||m>720)||minutes.every(m=>m===0)){setMessage('Enter a name, valid effective date and whole working minutes: 0 means not scheduled; selected days allow up to 12 hours.');return;}busy.current=true;setSaving(true);setMessage('');try{const r=await authHttpClient.post<ScheduleHistory>(`/employees/${employeeId}/schedules`,{expectedVersion:snapshot.version,name:name.trim(),effectiveFrom:date,minutes},{skipAuthReplay:true});if(active()){setSnapshot(r.data);setName('');setDate('');setHours(days.map(()=> '0'));setMessage('Schedule revision saved. Previous revisions preserved.');}}catch(error){if(active())setMessage(problemMessage(error));}finally{busy.current=false;if(active())setSaving(false);}}}>
      <h3 className="font-semibold">Record confirmed weekly schedule (separate save)</h3><p className="text-sm">Existing effective dates cannot be replaced. Check records carefully; this does not recalculate leave or define a statutory cycle.</p>
      <fieldset disabled={saving} className="space-y-4"><label className="ui-label">Schedule name<input className="ui-input" maxLength={100} required value={name} onChange={e=>setName(e.target.value)} /></label><label className="ui-label">Effective from<input className="ui-input" type="date" required min="1900-01-01" max="2100-12-31" value={date} onChange={e=>setDate(e.target.value)} /></label><div className="grid gap-3 sm:grid-cols-2">{days.map((day,i)=><label className="ui-label" key={day}>{day} working hours<input className="ui-input" type="number" min="0" max="12" step="any" required value={hours[i]} onChange={e=>setHours(hours.map((h,j)=>j===i?e.target.value:h))} /></label>)}</div><p className="text-sm">0 means not scheduled. Use hours corresponding to whole minutes (for example 7.5 hours = 450 minutes).</p><div className="flex flex-wrap gap-3"><button className="ui-primary" disabled={saving}>{saving?'Saving schedule...':'Save schedule revision'}</button><button type="button" ref={reloadRef} className="ui-secondary" onClick={()=>dirty?setDiscard(true):void reload()}>Reload schedule history</button></div></fieldset>
    </form>}
    {message && <p role="status" className="ui-warning">{message}</p>}
    {discard && <ConfirmationDialog title="Discard schedule draft and reload?" confirmLabel="Discard and reload" returnFocus={reloadRef.current} onCancel={()=>setDiscard(false)} onConfirm={()=>void reload()}><p>Entered schedule values will be discarded. The latest history and version will be loaded.</p></ConfirmationDialog>}
  </div>;
}
