import { useEffect, useRef, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { authHttpClient } from '../../auth/api';
import { useAuth } from '../../auth/hooks/useAuth';
import { problemMessage } from '../../leave-configuration/problems';

interface Evidence { id: string; class: string; status: string; version: string; canDownload: boolean; mediaType: string | null }
/** Private evidence uses generated identifiers, guarded callbacks and explicit non-replayable operations. */
export function SupportingDocuments({ requestId, onSelection, disabled = false }: { requestId?: string; onSelection?: (ids: string[], busy: boolean) => void; disabled?: boolean }) {
  const { user, sessionVersion } = useAuth();
  return <DocumentWorkspace key={`${user?.id}:${sessionVersion}:${requestId ?? 'drafts'}`} requestId={requestId} onSelection={onSelection} disabled={disabled} />;
}
function DocumentWorkspace({ requestId, onSelection, disabled }: { requestId?: string; onSelection?: (ids: string[], busy: boolean) => void; disabled: boolean }) {
  const { user, sessionVersion, getSessionVersion } = useAuth();
  const [classification, setClassification] = useState('Medical');
  const [error, setError] = useState(''); const [progress, setProgress] = useState<number | null>(null);
  const [selected, setSelected] = useState<string[]>([]);
  const selection = useRef<string[]>([]); const [busy, setBusy] = useState(false);
  const working = useRef(false); const controller = useRef<AbortController | null>(null);
  useEffect(() => () => controller.current?.abort(), []);
  const active = () => getSessionVersion() === sessionVersion;
  const evidence = useQuery({ queryKey: ['supporting-documents', user?.id, sessionVersion, requestId ?? 'drafts'], enabled: !!user,
    queryFn: async ({ signal }) => (await authHttpClient.get<Evidence[]>('/documents', { params: { requestId }, signal })).data,
    refetchOnMount: 'always', staleTime: 0 });
  async function action(task: () => Promise<unknown>) {
    if (working.current || disabled) return; working.current = true; setBusy(true); setError(''); onSelection?.(selection.current, true);
    try { await task(); if (active()) await evidence.refetch(); }
    catch (failure) { if (active()) setError(failure instanceof Error && failure.name === 'CanceledError'
      ? 'Upload cancelled. Refresh evidence and remove any unfinished draft before choosing the file again.' : problemMessage(failure)); }
    finally { working.current = false; if (active()) { setProgress(null); setBusy(false); onSelection?.(selection.current, false); } }
  }
  function choose(row: Evidence, checked: boolean) {
    const next = checked ? [...selected, row.id] : selected.filter(id => id !== row.id);
    if (next.length > 5) { setError('Choose at most five documents.'); return; }
    selection.current = next; setSelected(next); onSelection?.(next, working.current);
  }
  async function download(row: Evidence) {
    await action(async () => {
      const response = await authHttpClient.get<Blob>(`/documents/${row.id}/download`, { responseType: 'blob', skipAuthReplay: true });
      if (!active()) return;
      const url = URL.createObjectURL(response.data); const anchor = document.createElement('a'); anchor.href = url;
      anchor.download = 'supporting-document' + (row.mediaType === 'application/pdf' ? '.pdf' : row.mediaType === 'image/png' ? '.png' : '.jpg');
      anchor.click(); setTimeout(() => URL.revokeObjectURL(url), 1000);
    });
  }
  return <section className="ui-panel space-y-4" aria-label="Supporting documents">
    <h2 className="text-lg font-semibold">Supporting documents</h2>
    <p className="text-sm">Optional PDF, JPEG or PNG, up to 10 MiB each and five per request. Medical evidence is private to you and authorised HR; Managers receive status only. Choose Medical whenever the content might include health information. No certificate is required by this form.</p>
    {!requestId && <div className="space-y-3">
      <label className="ui-label">Evidence class<select className="ui-input" value={classification} disabled={busy || disabled} onChange={e => setClassification(e.target.value)}><option value="Medical">Medical (restricted content)</option><option value="Ordinary">Ordinary (eligible manager may download)</option></select></label>
      <label className="ui-label">Choose document<input className="ui-input" type="file" accept=".pdf,.png,.jpg,.jpeg" disabled={busy || disabled} onChange={e => {
        const file = e.target.files?.[0]; e.target.value = ''; if (!file) return;
        if (file.size === 0 || file.size > 10 * 1024 * 1024) { setError('Choose a nonempty file no larger than 10 MiB.'); return; }
        const key = crypto.randomUUID(); const abort = new AbortController(); controller.current = abort; setProgress(0);
        void action(async () => {
          // Some browsers provide no useful MIME type. The server still independently parses the actual bytes.
          const extension = file.name.split('.').pop()?.toLowerCase();
          const types: Record<string, string> = { pdf: 'application/pdf', png: 'image/png', jpg: 'image/jpeg', jpeg: 'image/jpeg' };
          const transport = (!file.type || file.type === 'application/octet-stream') && extension && types[extension]
            ? new File([file], file.name, { type: types[extension] }) : file;
          const data = new FormData(); data.append('file', transport); data.append('classification', classification); data.append('uploadKey', key);
          await authHttpClient.post('/documents', data, { headers: { 'Content-Type': undefined }, skipAuthReplay: true, signal: abort.signal, onUploadProgress: event => { if (active()) setProgress(event.total ? Math.round(event.loaded * 100 / event.total) : 0); } }); });
      }} /></label>
      {progress !== null && <div><p role="status" aria-live="polite">Upload {progress}% · Waiting for validation and scan.</p><button type="button" className="ui-secondary" onClick={() => controller.current?.abort()}>Cancel upload</button></div>}
    </div>}
    {evidence.isPending && <p role="status">Loading evidence…</p>}
    {(error || evidence.error) && <p role="alert" className="ui-error">{error || problemMessage(evidence.error)}</p>}
    <button type="button" className="ui-secondary" disabled={busy || disabled} onClick={() => void evidence.refetch()}>Refresh evidence</button>
    {evidence.data?.length === 0 && <p>No supporting documents recorded.</p>}
    <ul className="space-y-3">{evidence.data?.map((row, index) => <li className="rounded border p-3 flex flex-wrap gap-3 items-center" key={row.id}>
      <span>Document {index + 1} · {row.class} · {row.status}</span>
      {!requestId && row.status === 'Clean' && <label><input type="checkbox" disabled={busy || disabled} checked={selected.includes(row.id)} onChange={e => choose(row, e.target.checked)} /> Attach to request</label>}
      {row.canDownload && <button type="button" className="ui-secondary" disabled={busy || disabled} onClick={() => void download(row)}>Download</button>}
      {!requestId && ['Quarantined', 'ScanUnavailable'].includes(row.status) && <button type="button" className="ui-secondary" disabled={busy || disabled} onClick={() => void action(() => authHttpClient.post(`/documents/${row.id}/scan`, {}, { skipAuthReplay: true }))}>Retry scan</button>}
      {!requestId && <button type="button" className="ui-danger" disabled={busy || disabled} onClick={() => void action(async () => { await authHttpClient.delete(`/documents/${row.id}`, { params: { expectedVersion: row.version }, skipAuthReplay: true }); if (active()) choose(row, false); })}>Remove draft</button>}
    </li>)}</ul>
  </section>;
}
