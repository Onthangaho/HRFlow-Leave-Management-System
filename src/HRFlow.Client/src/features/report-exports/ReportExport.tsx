import { useEffect, useRef, useState } from 'react';
import { isAxiosError } from 'axios';
import { authHttpClient } from '../auth/api';
import { useAuth } from '../auth/hooks/useAuth';

const maximumBytes = 2 * 1024 * 1024;
interface Selection { start: string; end: string; departmentId?: string }

/** Downloads applied selections only; aborts and discards completion across range or login-session replacement. */
export function ReportExport({ kind, selection, disabled = false }: { kind: 'department' | 'team'; selection: Selection; disabled?: boolean }) {
  const { user, sessionVersion, getSessionVersion } = useAuth();
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState('');
  const [failed, setFailed] = useState(false);
  const operation = useRef<AbortController | null>(null);
  const url = useRef<string | null>(null);
  const revocation = useRef<ReturnType<typeof setTimeout> | null>(null);
  const selectionKey = `${selection.start}:${selection.end}:${selection.departmentId ?? ''}`;
  const current = useRef({ identity: user?.id, sessionVersion, selectionKey });
  current.current = { identity: user?.id, sessionVersion, selectionKey };
  function revoke() {
    if (revocation.current) clearTimeout(revocation.current);
    revocation.current = null;
    if (url.current) URL.revokeObjectURL(url.current);
    url.current = null;
  }
  useEffect(() => {
    setBusy(false); setMessage(''); setFailed(false);
    return () => { operation.current?.abort(); operation.current = null; revoke(); };
  }, [user?.id, sessionVersion, selectionKey]);

  async function download() {
    if (operation.current || disabled || !user?.id) return;
    const controller = new AbortController(); operation.current = controller;
    const identity = user.id, epoch = sessionVersion, range = selectionKey;
    const active = () => !controller.signal.aborted && getSessionVersion() === epoch && current.current.identity === identity
      && current.current.sessionVersion === epoch && current.current.selectionKey === range;
    setBusy(true); setFailed(false); setMessage('Preparing a fresh authorized CSV snapshot...');
    try {
      const path = kind === 'department' ? '/reports/department-leave/export' : '/leave-requests/team-summary/export';
      const response = await authHttpClient.get<Blob>(path, { params: { start: selection.start, end: selection.end,
        ...(kind === 'department' && selection.departmentId ? { departmentId: selection.departmentId } : {}) },
        signal: controller.signal, responseType: 'blob', skipAuthReplay: true });
      if (!active()) return;
      if (!response.data.size || response.data.size > maximumBytes) throw new Error('Invalid export size');
      revoke(); url.current = URL.createObjectURL(response.data);
      const anchor = document.createElement('a'); anchor.href = url.current;
      anchor.download = `${kind === 'department' ? 'hr-department-leave' : 'manager-team-leave'}-${selection.start}-${selection.end}.csv`;
      document.body.appendChild(anchor); anchor.click(); anchor.remove();
      revocation.current = setTimeout(revoke, 1000);
      setMessage('CSV download started. This fresh snapshot may differ from previously displayed results.');
    } catch (error) {
      if (!active()) return;
      let detail = 'Export failed or was interrupted. No automatic retry; select Retry export when ready.';
      if (isAxiosError(error)) {
        if ([401, 403].includes(error.response?.status ?? 0)) detail = 'Export permission is unavailable. Sign in or refresh your current access.';
        else if ([400, 404, 409].includes(error.response?.status ?? 0) && error.response?.data instanceof Blob) {
          try { const problem = JSON.parse(await error.response.data.text()); if (typeof problem.detail === 'string') detail = problem.detail; } catch { /* No server payload is shown unless it is safe ProblemDetails. */ }
        }
      }
      if (active()) { setFailed(true); setMessage(detail); }
    } finally {
      if (operation.current === controller) operation.current = null;
      if (active()) setBusy(false);
    }
  }
  return <div className="space-y-2" role="region" aria-label="CSV export">
    <div className="flex flex-wrap gap-3"><button type="button" className="ui-secondary" disabled={disabled || busy} onClick={() => void download()}>{busy ? 'Preparing CSV...' : failed ? 'Retry export' : 'Export CSV'}</button>
      {busy && <button type="button" className="ui-secondary" onClick={() => { operation.current?.abort(); operation.current = null; revoke(); setBusy(false); setMessage('Export cancelled. You can explicitly retry.'); }}>Cancel export</button>}</div>
    <p className="text-sm">Exports the applied displayed range, not draft filters or a selected calendar day. Fresh authorization and data may differ from this screen. Names use a visible “Text: ” safety prefix; maximum 5,000 data rows / 2 MiB.</p>
    {message && <p role={failed ? 'alert' : 'status'} className={failed ? 'ui-error' : 'text-sm'}>{message}</p>}
  </div>;
}
