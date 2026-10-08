import { useRef, useState } from 'react';
import { isAxiosError } from 'axios';
import { useAuth } from '../hooks/useAuth';
import { authHttpClient } from '../api';

const maximumPasswordLength = 256;
/** Own-account, non-replayable credential change; completion belongs only to the initiating login epoch. */
export function ChangePasswordPage() {
  const { getSessionVersion, completePasswordChange } = useAuth();
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmation, setConfirmation] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const busy = useRef(false);
  return <section className="ui-panel mx-auto max-w-xl" aria-labelledby="password-title">
    <h2 id="password-title" className="text-xl font-semibold text-slate-900">Change your password</h2>
    <p className="mt-3 text-sm text-slate-600">Changing your password signs you out of all existing sessions. Sign in again with your new password.</p>
    <p id="password-guidance" className="mt-2 text-sm text-slate-600">Use at least 8 characters, with uppercase and lowercase letters, a number and a symbol. Maximum 256 characters.</p>
    <form className="mt-6 space-y-5" aria-busy={saving} onSubmit={async event => {
      event.preventDefault();
      if (busy.current) return;
      setError(null);
      if (!currentPassword || !newPassword || !confirmation) { setError('Complete all password fields.'); return; }
      if (newPassword !== confirmation) { setError('New passwords must match.'); return; }
      if ([currentPassword, newPassword, confirmation].some(value => value.length > maximumPasswordLength)) { setError('Passwords must not exceed 256 characters.'); return; }
      const epoch = getSessionVersion();
      busy.current = true; setSaving(true);
      try {
        await authHttpClient.post('/auth/password', { currentPassword, newPassword }, { skipAuthReplay: true });
        if (getSessionVersion() !== epoch) return;
        setCurrentPassword(''); setNewPassword(''); setConfirmation('');
        completePasswordChange();
      } catch (failure) {
        if (getSessionVersion() !== epoch) return;
        setError(isAxiosError(failure) && failure.response
          ? failure.response.data?.detail ?? 'The password change was rejected. Review your input before trying again.'
          : 'We could not confirm the outcome. Do not automatically resubmit. Sign out and try signing in with the new password; if it does not work, try the previous password.');
      } finally {
        busy.current = false;
        if (getSessionVersion() === epoch) setSaving(false);
      }
    }}>
      {[
        { id: 'current-password', label: 'Current password', value: currentPassword, change: setCurrentPassword, auto: 'current-password' },
        { id: 'new-password', label: 'New password', value: newPassword, change: setNewPassword, auto: 'new-password' },
        { id: 'confirm-password', label: 'Confirm new password', value: confirmation, change: setConfirmation, auto: 'new-password' },
      ].map(field => <div key={field.id}>
        <label className="ui-label" htmlFor={field.id}>{field.label}</label>
        <input id={field.id} className="ui-input mt-1 w-full" type="password" required maxLength={maximumPasswordLength}
          autoComplete={field.auto} value={field.value} disabled={saving} onChange={event => field.change(event.target.value)}
          aria-invalid={Boolean(error)} aria-describedby={error ? 'password-guidance password-error' : 'password-guidance'} />
      </div>)}
      {error && <p id="password-error" role="alert" className="text-sm text-rose-800">{error}</p>}
      <button className="ui-primary" type="submit" disabled={saving}>{saving ? 'Changing password…' : 'Change password and sign out'}</button>
    </form>
  </section>;
}
