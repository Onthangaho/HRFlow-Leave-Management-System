import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import axios, { isAxiosError } from 'axios';
/** Anonymous invitation redemption; tokens stay in memory and are removed from the address bar. */
export function ActivationPage() {
  const [token] = useState(() => new URLSearchParams(window.location.hash.slice(1)).get('token') ?? '');
  const [password, setPassword] = useState('');
  const [confirmation, setConfirmation] = useState('');
  const [busy, setBusy] = useState(false);
  const [success, setSuccess] = useState(false);
  const [error, setError] = useState('');
  const saving = useRef(false);
  useEffect(() => {
    window.history.replaceState(null, '', window.location.pathname);
    const meta = document.createElement('meta'); meta.name = 'referrer'; meta.content = 'no-referrer'; document.head.appendChild(meta);
    return () => meta.remove();
  }, []);
  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (saving.current) return;
    if (password !== confirmation) { setError('Passwords must match.'); return; }
    saving.current = true; setBusy(true); setError('');
    try {
      await axios.post(import.meta.env.VITE_API_BASE_URL + '/auth/activation', { token, password }, { timeout: 30000 });
      setPassword(''); setConfirmation(''); setSuccess(true);
    } catch (failure) {
      setError(isAxiosError(failure) && typeof failure.response?.data?.detail === 'string' ? failure.response.data.detail : 'Unable to activate. Try again, or ask HR for a new invitation.');
    } finally { saving.current = false; setBusy(false); }
  };
  return <main className="mx-auto max-w-lg px-4 py-12"><section className="ui-panel space-y-5"><h1 className="text-2xl font-bold">Activate your account</h1>
    {success ? <div role="status"><p>Your password has been established. Sign in normally to continue.</p><Link className="ui-primary mt-4 inline-block" to="/login">Go to login</Link></div> : <>
      <p>Choose your own password. Activation does not change your roles or employment status.</p>
      {!token && <p role="alert">No invitation was supplied. Open your private invitation or ask HR for a new one.</p>}
      {error && <p id="activation-error" role="alert" className="text-rose-800">{error}</p>}
      <form onSubmit={event => void submit(event)} className="space-y-4"><fieldset disabled={busy} className="space-y-4"><legend className="sr-only">New account password</legend>
        <p id="password-guidance" className="text-sm text-slate-600">At least 8 characters, including uppercase, lowercase, a number and a symbol. The server validates your password.</p>
        <div><label htmlFor="activation-password">New password</label><input id="activation-password" className="ui-input w-full" type="password" autoComplete="new-password" required maxLength={256} aria-describedby="password-guidance activation-error" value={password} onChange={event => setPassword(event.target.value)} /></div>
        <div><label htmlFor="activation-confirmation">Confirm password</label><input id="activation-confirmation" className="ui-input w-full" type="password" autoComplete="new-password" required maxLength={256} value={confirmation} onChange={event => setConfirmation(event.target.value)} /></div>
        <button className="ui-primary" disabled={busy || !token}>{busy ? 'Activating…' : 'Activate account'}</button>
      </fieldset></form><Link className="text-indigo-700 underline" to="/login">Back to login</Link>
    </>}
  </section></main>;
}
