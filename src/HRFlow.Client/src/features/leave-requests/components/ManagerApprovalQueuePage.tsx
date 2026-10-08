import { useRef, useState } from 'react';
import { useApproveLeaveRequest, usePendingLeaveRequests, useRejectLeaveRequest } from '../api';
import { ApprovalQueueItem } from './ApprovalQueueItem';
import { ConfirmationDialog } from '../../../components/ConfirmationDialog';
import { useAuth } from '../../auth/hooks/useAuth';
import { isConflict, problemMessage } from '../../leave-configuration/problems';
import { MAX_DECISION_NOTE_LENGTH, type PendingLeaveRequest } from '../types';

type Decision = { request: PendingLeaveRequest; action: 'approve' | 'reject' };
// Request dates are calendar days, not instants; a local midnight must never shift to the previous day.
const calendarDate = (value: string) => new Intl.DateTimeFormat('en-ZA', { dateStyle: 'medium', timeZone: 'UTC' }).format(new Date(value.slice(0, 10) + 'T00:00:00Z'));

/** Keeps a loaded decision draft outside queue rows so refetches cannot discard notes or silently replay decisions. */
export function ManagerApprovalQueuePage() {
  const queue = usePendingLeaveRequests();
  const approve = useApproveLeaveRequest();
  const reject = useRejectLeaveRequest();
  const { sessionVersion, getSessionVersion } = useAuth();
  const [decision, setDecision] = useState<Decision | null>(null);
  const [note, setNote] = useState('');
  const [failure, setFailure] = useState('');
  const [needsRefresh, setNeedsRefresh] = useState(false);
  const [unavailable, setUnavailable] = useState(false);
  const [discarding, setDiscarding] = useState(false);
  const [notice, setNotice] = useState('');
  const [refreshing, setRefreshing] = useState(false);
  const saving = useRef(false);
  const opener = useRef<HTMLElement | null>(null);
  const noteField = useRef<HTMLTextAreaElement>(null);
  const busy = approve.isPending || reject.isPending;
  const tooLong = note.trim().length > MAX_DECISION_NOTE_LENGTH;
  const currentSession = () => getSessionVersion() === sessionVersion;

  function open(requestId: string, action: Decision['action']) {
    const request = queue.data?.find(item => item.id === requestId);
    if (!currentSession() || !request || saving.current) return;
    opener.current = document.activeElement as HTMLElement;
    setDecision({ request, action }); setNote(''); setFailure(''); setNeedsRefresh(false);
    setUnavailable(false); setDiscarding(false); setNotice('');
  }
  function close() {
    if (saving.current || refreshing) return;
    if (note.length > 0) { setDiscarding(true); return; }
    setDecision(null);
  }
  async function refreshDecision() {
    if (!currentSession() || refreshing || saving.current) return;
    setRefreshing(true);
    try {
      const result = await queue.refetch();
      if (!currentSession()) return;
      if (result.error) { setFailure(problemMessage(result.error)); return; }
      const available = result.data?.some(item => item.id === decision?.request.id) ?? false;
      setUnavailable(!available); setNeedsRefresh(false);
      setFailure(available ? '' : 'This request is no longer in your Pending queue. It may have been decided, cancelled or reassigned. Your note has been kept. Check the request history for its recorded outcome.');
    } finally { if (currentSession()) setRefreshing(false); }
  }
  async function confirm() {
    if (!currentSession() || !decision || saving.current || tooLong || needsRefresh || unavailable || refreshing) return;
    saving.current = true; setFailure('');
    const selected = decision;
    try {
      await (selected.action === 'approve' ? approve : reject).mutateAsync({ requestId: selected.request.id, decisionNote: note.trim() || null });
      if (!currentSession()) return;
      setDecision(null); setNote('');
      setNotice(`${selected.request.employeeFullName}'s request was ${selected.action === 'approve' ? 'approved' : 'rejected'}.`);
    } catch (error) {
      if (!currentSession()) return;
      setFailure(problemMessage(error)); setNeedsRefresh(isConflict(error));
    } finally { saving.current = false; }
  }
  return <div className="workspace-page space-y-6"><div className="mx-auto w-full max-w-5xl space-y-6">
    <section className="page-intro ui-panel">
      <p>Review Pending requests from your current direct reports. Add an optional note to explain your decision.</p>
      <p className="text-sm">Awaiting review: {queue.isLoading ? 'Loading…' : queue.error ? 'Unavailable' : queue.data?.length ?? 0}</p>
      <button type="button" data-focus-fallback className="ui-secondary self-start" disabled={queue.isFetching || busy} onClick={() => void queue.refetch()}>Refresh queue</button>
    </section>
    {notice && <p role="status" className="ui-panel text-emerald-800">{notice}</p>}
    {queue.isLoading && <p role="status" className="ui-panel">Loading approval queue…</p>}
    {queue.isFetching && !queue.isLoading && <p role="status">Refreshing the previously loaded queue…</p>}
    {queue.isError && <section role="alert" className="ui-error"><h2 className="font-bold">Approval queue unavailable</h2><p>{problemMessage(queue.error)}</p><button className="ui-secondary mt-3" disabled={queue.isFetching} onClick={() => void queue.refetch()}>Try again</button></section>}
    {!queue.isLoading && !queue.isError && queue.data?.length === 0 && <section className="ui-panel"><h2 className="text-xl font-bold">Your queue is clear</h2><p className="mt-2 text-slate-600">There are no Pending requests to review. Refresh to check for new requests.</p></section>}
    {!queue.isError && <section className="space-y-4" aria-label="Pending leave requests">{queue.data?.map(request => <ApprovalQueueItem key={request.id} leaveRequest={request} decisionPending={busy} processingAction={busy && decision?.request.id === request.id ? decision.action : undefined} onApprove={id => open(id, 'approve')} onReject={id => open(id, 'reject')} />)}</section>}
    {decision && <ConfirmationDialog title={decision.action === 'approve' ? 'Approve leave request' : 'Reject leave request'} confirmLabel={`Confirm ${decision.action}`} neutral={decision.action === 'approve'} returnFocus={opener.current} busy={busy || refreshing} confirmDisabled={tooLong || needsRefresh || unavailable || discarding} onCancel={close} onConfirm={() => void confirm()}>
      <p className="font-semibold text-slate-900">{decision.request.employeeFullName} · {decision.request.leaveTypeName}</p>
      <p>{calendarDate(decision.request.startDate)} – {calendarDate(decision.request.endDate)} (inclusive calendar dates)</p>
      <p>The note is visible to the employee, eligible Managers and HR in authorised history. Do not include medical details or other sensitive personnel information.</p>
      <label htmlFor="decision-note" className="block font-semibold text-slate-900">Decision note (optional)</label>
      <textarea ref={noteField} id="decision-note" rows={4} className="ui-input" disabled={busy || refreshing} value={note} onChange={event => setNote(event.target.value)} aria-invalid={tooLong} aria-describedby={`decision-note-guidance${tooLong ? ' decision-note-error' : ''}`} />
      <p id="decision-note-guidance">{note.trim().length}/{MAX_DECISION_NOTE_LENGTH} trimmed characters. Blank notes are not recorded. A rejection note is optional.</p>
      {tooLong && <p id="decision-note-error" role="alert" className="text-rose-700">Shorten your note to 500 trimmed characters.</p>}
      {failure && <div role="alert" className="ui-error"><p>{failure}</p><p className="mt-2">Your note has been kept. No automatic retry is made.</p></div>}
      {needsRefresh && <button type="button" className="ui-secondary" disabled={busy || refreshing} onClick={() => void refreshDecision()}>Refresh decision availability</button>}
      {unavailable && <button type="button" className="ui-secondary" onClick={close}>Return to queue</button>}
      {discarding && <section className="ui-warning" aria-label="Discard decision note"><p>Discard your unsaved note and close?</p><div className="mt-3 flex flex-wrap gap-3"><button type="button" className="ui-secondary" onClick={() => { setDiscarding(false); noteField.current?.focus(); }}>Keep note</button><button type="button" className="ui-danger" onClick={() => { setDecision(null); setNote(''); }}>Discard note</button></div></section>}
    </ConfirmationDialog>}
  </div></div>;
}
