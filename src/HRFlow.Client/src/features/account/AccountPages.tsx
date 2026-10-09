import { useRef, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import { ConfirmationDialog } from '../../components/ConfirmationDialog';
import { authHttpClient } from '../auth/api';
import { useAuth } from '../auth/hooks/useAuth';
import { isConflict, problemMessage } from '../leave-configuration/problems';
import { useOwnPreferences, useOwnProfile, type OwnPreferences, type OwnProfile } from './api';

const preferredNameLimit = 80;
const phoneLimit = 30;

function AccountLinks() {
  return <nav aria-label="Account workspace" className="flex flex-wrap gap-4 text-sm font-semibold text-indigo-700">
    <Link className="underline" to="/account/profile">Profile</Link><Link className="underline" to="/account/settings">Settings</Link><Link className="underline" to="/account/password">Change password</Link>
  </nav>;
}

/** Canonical employment is read-only; an open private form retains its original edit generation. */
export function ProfilePage() {
  const query = useOwnProfile();
  return <div className="workspace-page mx-auto max-w-3xl space-y-5"><AccountLinks />
    {query.isPending && <p className="ui-panel" role="status">Loading your profile…</p>}
    {query.isError && <section className="ui-error" role="alert"><p>{problemMessage(query.error)}</p><button className="ui-secondary mt-3" disabled={query.isFetching} onClick={() => void query.refetch()}>Retry profile</button></section>}
    {query.data && <ProfileForm initial={query.data} />}
  </div>;
}

function ProfileForm({ initial }: { initial: OwnProfile }) {
  const { user, sessionVersion, getSessionVersion } = useAuth();
  const cache = useQueryClient();
  const [snapshot, setSnapshot] = useState(initial);
  const [name, setName] = useState(initial.preferredDisplayName ?? '');
  const [phone, setPhone] = useState(initial.contactPhone ?? '');
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState('');
  const [conflict, setConflict] = useState(false);
  const [discard, setDiscard] = useState(false);
  const busy = useRef(false);
  const reloadButton = useRef<HTMLButtonElement>(null);
  const current = () => getSessionVersion() === sessionVersion;
  const dirty = name !== (snapshot.preferredDisplayName ?? '') || phone !== (snapshot.contactPhone ?? '');
  const reload = async () => {
    if (busy.current) return;
    busy.current = true; setSaving(true); setDiscard(false);
    try {
      const fresh = (await authHttpClient.get<OwnProfile>('/me')).data;
      if (!current()) return;
      setSnapshot(fresh); setName(fresh.preferredDisplayName ?? ''); setPhone(fresh.contactPhone ?? ''); setConflict(false); setMessage('Profile reloaded; unsaved changes discarded.');
      cache.setQueryData(['own-profile', user?.id, sessionVersion], fresh);
    } catch (error) { if (current()) setMessage(problemMessage(error)); }
    finally { if (current()) { busy.current = false; setSaving(false); } }
  };
  const save = async (event: FormEvent) => {
    event.preventDefault(); if (busy.current || conflict) return;
    if (name.trim().length > preferredNameLimit || Array.from(name).some(character => { const code = character.charCodeAt(0); return code < 32 || code >= 127 && code <= 159; }) || phone.trim().length > phoneLimit
      || phone.trim() && (!/^[0-9+ ().-]+$/.test(phone.trim()) || !/[0-9]/.test(phone))) { setMessage('Check the name length and phone format before saving.'); return; }
    busy.current = true; setSaving(true); setMessage('');
    try {
      const result = (await authHttpClient.put<Pick<OwnProfile, 'preferredDisplayName' | 'contactPhone' | 'profileVersion'>>('/me/profile', { expectedVersion: snapshot.profileVersion, preferredDisplayName: name, contactPhone: phone }, { skipAuthReplay: true })).data;
      if (!current()) return;
      const fresh = { ...snapshot, ...result }; setSnapshot(fresh); setName(result.preferredDisplayName ?? ''); setPhone(result.contactPhone ?? '');
      cache.setQueryData<OwnProfile>(['own-profile', user?.id, sessionVersion], existing => ({ ...(existing ?? snapshot), ...result }));
      setMessage('Profile saved. Your canonical HR details are unchanged.');
    } catch (error) { if (current()) { setMessage(problemMessage(error)); setConflict(isConflict(error)); } }
    finally { if (current()) { busy.current = false; setSaving(false); } }
  };
  return <>
    <section className="ui-panel space-y-4"><h2 className="text-xl font-bold">Employment details</h2><p className="text-sm text-slate-600">HR controls these records. Contact HR to correct them. Audit history uses your canonical name.</p>
      <dl className="grid gap-4 sm:grid-cols-2">{[
        ['Canonical name', initial.canonicalName], ['Email', initial.email], ['Roles', initial.roles.join(', ')], ['Department', initial.departmentName],
        ['Manager', initial.managerName ?? 'Not assigned'], ['Employment status', initial.isActive ? 'Active' : 'Inactive'], ['Account status', initial.isActivated ? 'Activated' : 'Pending activation'],
      ].map(([label, value]) => <div key={label}><dt className="text-sm font-semibold text-slate-500">{label}</dt><dd className="mt-1 break-words text-slate-900">{value}</dd></div>)}</dl>
    </section>
    <form className="ui-panel space-y-4" onSubmit={save}><h2 className="text-xl font-bold">Personal display and contact</h2>
      <p id="profile-purpose" className="text-sm text-slate-600">Preferred name personalises your header only. Phone is optional and private to this account profile; it is not used for sign-in or password recovery. Leave out information you do not want stored.</p>
      <label className="ui-label">Preferred display name (optional)<input className="ui-input" value={name} maxLength={preferredNameLimit} disabled={saving} aria-describedby="profile-purpose profile-name-help profile-feedback" autoComplete="nickname" onChange={e => setName(e.target.value)} /></label>
      <p id="profile-name-help" className="text-sm text-slate-500">Up to {preferredNameLimit} characters. Blank uses your account email fallback.</p>
      <label className="ui-label">Contact phone (optional)<input type="tel" className="ui-input" value={phone} maxLength={phoneLimit} disabled={saving} autoComplete="tel" aria-describedby="profile-purpose profile-phone-help profile-feedback" onChange={e => setPhone(e.target.value)} /></label>
      <p id="profile-phone-help" className="text-sm text-slate-500">Up to {phoneLimit} characters; digits, +, spaces, parentheses, periods and hyphens. Blank removes it.</p>
      <p id="profile-feedback" role={conflict ? 'alert' : 'status'} aria-live="polite">{message}</p>
      {conflict && <p className="ui-warning">Your input and original version are retained. Reload to discard this draft and use the latest profile.</p>}
      <div className="flex flex-wrap gap-3"><button className="ui-primary" disabled={saving || conflict}>{saving ? 'Saving…' : 'Save profile'}</button><button ref={reloadButton} className="ui-secondary" type="button" disabled={saving} onClick={() => dirty ? setDiscard(true) : void reload()}>Reload profile</button></div>
    </form>
    {discard && <ConfirmationDialog title="Discard profile changes and reload?" confirmLabel="Discard and reload" returnFocus={reloadButton.current} onCancel={() => setDiscard(false)} onConfirm={() => void reload()}><p>Your entered preferred name and phone will be discarded. The latest saved profile will be loaded.</p></ConfirmationDialog>}
  </>;
}

/** Saves real theme and delivery-time toggles, never placeholders for unimplemented channels. */
export function SettingsPage() {
  const query = useOwnPreferences();
  return <div className="workspace-page mx-auto max-w-3xl space-y-5"><AccountLinks />
    {query.isPending && <p className="ui-panel" role="status">Loading settings…</p>}
    {query.isError && <section className="ui-error" role="alert"><p>{problemMessage(query.error)}</p><button className="ui-secondary mt-3" disabled={query.isFetching} onClick={() => void query.refetch()}>Retry settings</button></section>}
    {query.data && <SettingsForm initial={query.data} />}
  </div>;
}

const categories = [
  ['submissionNotifications', 'New leave submissions', 'New requests assigned to you as a manager.'],
  ['decisionNotifications', 'Leave decisions', 'Approval or rejection of your own leave.'],
  ['cancellationNotifications', 'Leave cancellations', 'Owner or HR cancellations relevant to your team.'],
  ['reassignmentNotifications', 'Reassigned Pending work', 'Pending requests assigned to you after a reporting change.'],
] as const;

function SettingsForm({ initial }: { initial: OwnPreferences }) {
  const { user, sessionVersion, getSessionVersion } = useAuth();
  const cache = useQueryClient();
  const [snapshot, setSnapshot] = useState(initial);
  const [values, setValues] = useState(initial);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState('');
  const [conflict, setConflict] = useState(false);
  const [discard, setDiscard] = useState(false);
  const busy = useRef(false); const opener = useRef<HTMLButtonElement>(null);
  const current = () => getSessionVersion() === sessionVersion;
  const dirty = JSON.stringify(values) !== JSON.stringify(snapshot);
  const apply = (fresh: OwnPreferences) => { setSnapshot(fresh); setValues(fresh); cache.setQueryData(['own-preferences', user?.id, sessionVersion], fresh); };
  const reload = async () => {
    if (busy.current) return; busy.current = true; setSaving(true); setDiscard(false);
    try { const fresh = (await authHttpClient.get<OwnPreferences>('/me/preferences')).data; if (current()) { apply(fresh); setConflict(false); setMessage('Settings reloaded; unsaved changes discarded.'); } }
    catch (error) { if (current()) setMessage(problemMessage(error)); }
    finally { if (current()) { busy.current = false; setSaving(false); } }
  };
  const save = async (event: FormEvent) => {
    event.preventDefault(); if (busy.current || conflict) return; busy.current = true; setSaving(true); setMessage('');
    try {
      const { preferencesVersion: _version, ...editable } = values; void _version;
      const fresh = (await authHttpClient.put<OwnPreferences>('/me/preferences', { ...editable, expectedVersion: snapshot.preferencesVersion }, { skipAuthReplay: true })).data;
      if (current()) { apply(fresh); setMessage('Settings saved. Delivery preferences apply when future events are processed.'); }
    } catch (error) { if (current()) { setMessage(problemMessage(error)); setConflict(isConflict(error)); } }
    finally { if (current()) { busy.current = false; setSaving(false); } }
  };
  return <>
    <form onSubmit={save} className="ui-panel space-y-6"><section className="space-y-3"><h2 className="text-xl font-bold">Appearance</h2><p id="theme-help" className="text-sm text-slate-600">Theme applies after saving. System follows your device while you are signed in. Each account has its own setting.</p>
      <label className="ui-label" htmlFor="account-theme">Theme</label><select id="account-theme" className="ui-input" value={values.theme} disabled={saving} aria-describedby="theme-help settings-feedback" onChange={e => setValues(v => ({ ...v, theme: e.target.value as OwnPreferences['theme'] }))}><option>System</option><option>Light</option><option>Dark</option></select>
    </section><fieldset className="space-y-4"><legend className="text-xl font-bold">In-app notifications</legend><p id="notification-help" className="text-sm text-slate-600">All these categories are optional. Toggles apply at delivery time, including queued events not yet processed. Existing inbox items stay accessible. Re-enabling does not resend suppressed events. Categories only deliver when your current role and assignment make you a recipient.</p>
      {categories.map(([key, label, help]) => <label key={key} className="flex items-start gap-3 rounded-lg border border-slate-200 p-3"><input type="checkbox" className="mt-1 h-5 w-5 shrink-0 accent-indigo-700" checked={values[key]} disabled={saving} aria-describedby="notification-help settings-feedback" onChange={e => setValues(v => ({ ...v, [key]: e.target.checked }))} /><span><span className="block font-semibold">{label}</span><span className="mt-1 block text-sm text-slate-600">{help}</span></span></label>)}
    </fieldset><p id="settings-feedback" role={conflict ? 'alert' : 'status'} aria-live="polite">{message}</p>
      {conflict && <p className="ui-warning">Your selections and original version are retained. Explicit reload discards this draft.</p>}
      <div className="flex flex-wrap gap-3"><button className="ui-primary" disabled={saving || conflict}>{saving ? 'Saving…' : 'Save settings'}</button><button ref={opener} type="button" className="ui-secondary" disabled={saving} onClick={() => dirty ? setDiscard(true) : void reload()}>Reload settings</button></div>
    </form>
    {discard && <ConfirmationDialog title="Discard settings changes and reload?" confirmLabel="Discard and reload" returnFocus={opener.current} onCancel={() => setDiscard(false)} onConfirm={() => void reload()}><p>Your unsaved theme and notification selections will be discarded.</p></ConfirmationDialog>}
  </>;
}
