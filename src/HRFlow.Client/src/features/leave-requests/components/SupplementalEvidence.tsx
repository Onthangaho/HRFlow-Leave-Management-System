import { useEffect, useRef, useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '../../auth/hooks/useAuth';
import { authHttpClient } from '../../auth/api';
import { problemMessage } from '../../leave-configuration/problems';
import { SupportingDocuments } from './SupportingDocuments';
import type { Evidence } from './SupportingDocuments';

interface Addition { id: string; actorId: string; requestId: string; timestampUtc: string; documentIds: string[] }
interface Detail { canAdd: boolean; explanation: string; evidenceClass: string | null; boundCount: number; additions: Addition[] }
interface Attempt { key: string; documents: { id: string; version: string }[] }
/** Supplemental evidence is a separate owner mutation; original requirements and transitions remain read-only. */
export function SupplementalEvidence({ requestId }: { requestId: string }) {
  const { user, sessionVersion, getSessionVersion } = useAuth();
  const queries = useQueryClient();
  const [editing, setEditing] = useState(false); const [selection, setSelection] = useState<Evidence[]>([]);
  const [uploadBusy, setUploadBusy] = useState(false); const [saving, setSaving] = useState(false);
  const [attempt, setAttempt] = useState<Attempt | null>(null); const [error, setError] = useState(''); const [notice, setNotice] = useState('');
  const busy = useRef(false); const abort = useRef<AbortController | null>(null); const mounted = useRef(true);
  const message = useRef<HTMLParagraphElement | null>(null); const refresh = useRef<HTMLButtonElement | null>(null);
  useEffect(() => { mounted.current = true; return () => { mounted.current = false; abort.current?.abort(); }; }, []);
  useEffect(() => { if (notice || error) message.current?.focus(); }, [notice, error]);
  const active = () => mounted.current && getSessionVersion() === sessionVersion;
  const detail = useQuery({ queryKey: ['evidence-additions', user?.id, sessionVersion, requestId],
    queryFn: async ({ signal }) => (await authHttpClient.get<Detail>(`/documents/requests/${requestId}/additions`, { signal })).data,
    enabled: !!user, retry: false, refetchOnMount: 'always' });
  async function refreshState() {
    await Promise.all([detail.refetch(), queries.invalidateQueries({ queryKey: ['leave-request-timeline', user?.id, sessionVersion, requestId] })]);
  }
  async function bind() {
    if (busy.current || uploadBusy) return;
    const payload = attempt ?? { key: crypto.randomUUID(), documents: selection.map(d => ({ id: d.id, version: d.version })) };
    if (!payload.documents.length) return;
    setAttempt(payload); busy.current = true; setSaving(true); setError(''); setNotice(''); abort.current = new AbortController();
    try {
      await authHttpClient.post(`/documents/requests/${requestId}/additions`, payload, { skipAuthReplay: true, signal: abort.current.signal });
      if (!active()) return;
      setAttempt(null); setEditing(false); setSelection([]); setNotice('Evidence addition confirmed. Submission requirements and leave status are unchanged.');
      await Promise.all(['evidence-additions', 'supporting-documents', 'leave-request-timeline'].map(name =>
        queries.invalidateQueries({ queryKey: [name, user?.id, sessionVersion] })));
    } catch (failure) {
      if (active()) setError(`${problemMessage(failure)} If the response was lost, use Recover same addition to check the committed outcome. Do not assume it failed.`);
    } finally { busy.current = false; if (active()) setSaving(false); }
  }
  return <section className="ui-panel space-y-4" aria-label="Later evidence additions">
    <h2 className="text-lg font-semibold">Later evidence additions</h2>
    {detail.isPending && <p role="status">Loading addition eligibility and history…</p>}
    {detail.isError ? <p role="alert" className="ui-error">{problemMessage(detail.error)}</p> : detail.data && <>
      <p>{detail.data.explanation}</p><p>{detail.data.boundCount} of 5 document slots used. Draft uploads do not reserve slots.</p>
      <ol className="space-y-2">{detail.data.additions.map(row => <li key={row.id}>Later addition · {row.documentIds.length} document(s) · <time dateTime={row.timestampUtc}>{new Date(row.timestampUtc).toLocaleString(undefined, { timeZone: 'UTC' })} UTC</time></li>)}</ol>
      {detail.data.additions.length === 0 && <p>No later additions recorded.</p>}
      {detail.data.canAdd && !editing && <button className="ui-primary" type="button" onClick={() => { setEditing(true); setNotice(''); }}>Add evidence</button>}
    </>}
    <button ref={refresh} className="ui-secondary" type="button" disabled={saving || detail.isFetching} onClick={() => void refreshState()}>Refresh additions</button>
    {editing && detail.data && <div className="space-y-4" hidden={detail.isError || !detail.data.canAdd}>
      <SupportingDocuments configuredClass={detail.data.evidenceClass ?? undefined} disabled={saving || !!attempt || detail.isError || !detail.data.canAdd}
        onSelection={(_ids, processing, rows) => { setUploadBusy(processing); if (rows) setSelection(rows); }} />
      {!attempt && <button type="button" className="ui-primary" disabled={saving || uploadBusy || selection.length === 0 || detail.data.boundCount + selection.length > 5} onClick={() => void bind()}>Attach selected evidence</button>}
      {detail.data.boundCount + selection.length > 5 && <p role="alert">Choose fewer documents. The server rechecks the five-document total.</p>}
    </div>}
    {attempt && <div className="space-y-3"><p>The attempted selection is retained for explicit recovery. Nothing is automatically resubmitted.</p>
      <button type="button" className="ui-primary" disabled={saving || detail.isError} onClick={() => void bind()}>{saving ? 'Checking addition…' : 'Recover same addition'}</button>
      <button type="button" className="ui-secondary" disabled={saving} onClick={() => { setAttempt(null); setSelection([]); setError(''); setEditing(false); refresh.current?.focus(); void refreshState(); }}>Review another selection</button></div>}
    {saving && <p role="status">Saving evidence addition…</p>}{notice && <p ref={message} tabIndex={-1} role="status">{notice}</p>}{error && <p ref={message} tabIndex={-1} className="ui-error" role="alert">{error}</p>}
  </section>;
}
