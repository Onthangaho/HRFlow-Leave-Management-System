import { useEffect, useRef, useState, type FormEvent } from 'react';
import { useApproveLeaveRequest, usePendingLeaveRequests, useRejectLeaveRequest, queueDefaults, useQueuePeople, useQueueTypes, checkQueueDecision, type QueueFilters } from '../api';
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
  const { user, sessionVersion } = useAuth();
  return <QueueWorkspace key={user?.id + ':' + sessionVersion} />;
}

function QueueWorkspace() {
  const [draft, setDraft] = useState<QueueFilters>({ ...queueDefaults });
  const [applied, setApplied] = useState<QueueFilters>({ ...queueDefaults });
  const [searchDraft, setSearchDraft] = useState('');
  const [peopleSearch, setPeopleSearch] = useState('');
  const people = useQueuePeople(peopleSearch);
  const types = useQueueTypes();
  const queue = usePendingLeaveRequests(applied);
  const page = queue.data;
  const refreshAbort = useRef<AbortController | null>(null);
  useEffect(() => () => refreshAbort.current?.abort(), []);
  useEffect(() => {
    if (page && !queue.isFetching && applied.page !== page.page)
      setApplied(current => current === applied ? { ...current, page: page.page } : current);
  }, [page, queue.isFetching, applied]);
  function apply(event: FormEvent) { event.preventDefault(); setApplied({ ...draft, page: 1 }); }
  function field(name: keyof QueueFilters, value: string) { setDraft(current => ({ ...current, [name]: name === 'pageSize' ? Number(value) : value })); }
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
    const request = queue.data?.items.find(item => item.id === requestId);
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
      refreshAbort.current?.abort();
      refreshAbort.current = new AbortController();
      const available = await checkQueueDecision(decision!.request.id, refreshAbort.current.signal);
      if (!currentSession()) return;
      setUnavailable(!available); setNeedsRefresh(false);
      setFailure(available ? '' : 'This request is no longer in your Pending queue. It may have been decided, cancelled or reassigned. Your note has been kept. Check the request history for its recorded outcome.');
    } catch (error) { if (currentSession()) setFailure(problemMessage(error)); }
    finally { if (currentSession()) setRefreshing(false); }
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
      <p className="text-sm">Matching Pending requests: {queue.isLoading ? 'Loading…' : queue.error ? 'Unavailable' : queue.data?.totalCount ?? 'Unavailable'}</p>
      <button type="button" data-focus-fallback className="ui-secondary self-start" disabled={queue.isFetching || busy} onClick={() => void queue.refetch()}>Refresh queue</button>
    </section>
    <form className="ui-panel space-y-4" aria-label="Approval queue filters" onSubmit={apply}>
      <p>Inclusive date intersection includes requests overlapping either boundary. Filters do not change your reporting permissions.</p>
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <label className="ui-label">Find Pending direct report<input className="ui-input" maxLength={100} value={searchDraft} onChange={e => setSearchDraft(e.target.value)} /></label>
        <button type="button" className="ui-secondary self-end" disabled={people.isFetching} onClick={() => { setPeopleSearch(searchDraft.trim()); if (peopleSearch === searchDraft.trim()) void people.refetch(); }}>Search direct reports</button>
        <div className="ui-label"><label htmlFor="queue-person">Pending direct report</label><select id="queue-person" className="ui-input" value={draft.employeeId} onChange={e => setDraft(current => ({ ...current, employeeId: e.target.value, employeeName: people.data?.find(p => p.id === e.target.value)?.name ?? '' }))}><option value="">All eligible direct reports</option>{draft.employeeId && !people.data?.some(p => p.id === draft.employeeId) && <option value={draft.employeeId}>{draft.employeeName}</option>}{people.data?.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}</select></div>
        <div className="ui-label"><label htmlFor="queue-type">Queue leave type</label><select id="queue-type" className="ui-input" value={draft.leaveTypeId} onChange={e => field('leaveTypeId', e.target.value)}><option value="">All leave types</option>{types.data?.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}</select></div>
        <label className="ui-label">Queue period start<input className="ui-input" type="date" value={draft.start} onChange={e => field('start', e.target.value)} /></label>
        <label className="ui-label">Queue period end<input className="ui-input" type="date" min={draft.start || undefined} value={draft.end} onChange={e => field('end', e.target.value)} /></label>
        <div className="ui-label"><label htmlFor="queue-size">Queue requests per page</label><select id="queue-size" className="ui-input" value={draft.pageSize} onChange={e => field('pageSize', e.target.value)}>{[25,50,100].map(size => <option key={size}>{size}</option>)}</select></div>
      </div>
      <p className="text-sm">Direct-report search shows up to 25 eligible Pending owners, across all pages. Name substring matching is case-sensitive; refine your search if needed.</p>
      <p role="status">{people.isFetching ? 'Searching eligible Pending owners.' : people.error ? 'Direct-report search unavailable.' : 'Direct-report choices loaded; refine the search for other matches.'}</p>
      {people.error && <p role="alert">Direct-report search unavailable. Use Search direct reports to retry.</p>}
      {types.error && <p role="alert">Leave types unavailable. <button type="button" className="ui-secondary" onClick={() => void types.refetch()}>Retry queue types</button></p>}
      <div className="flex flex-wrap gap-3"><button className="ui-primary" disabled={busy}>Apply queue filters</button><button type="button" className="ui-secondary" disabled={busy} onClick={() => { setDraft({ ...queueDefaults }); setApplied({ ...queueDefaults }); }}>Reset queue filters</button></div>
    </form>
    <p aria-label="Applied queue filters">Applied: {applied.employeeName || 'All eligible direct reports'}; {types.data?.find(t => t.id === applied.leaveTypeId)?.name || (applied.leaveTypeId ? 'Selected type' : 'All leave types')}; {applied.start || 'Any start'} to {applied.end || 'Any end'}</p>
    <p role="status">{queue.isFetching ? (page ? 'Refreshing the previously loaded queue.' : 'Loading approval queue.') : queue.error ? 'Queue unavailable.' : page ? page.totalCount + ' matching requests. Page ' + page.page + ' of ' + page.totalPages + '.' : ''}</p>
    <div className="flex flex-wrap items-center gap-3"><button type="button" className="ui-secondary" disabled={!page || queue.isFetching || !!queue.error || busy || page.page <= 1} onClick={() => setApplied(current => ({ ...current, page: page!.page - 1 }))}>Previous queue page</button><button type="button" className="ui-secondary" disabled={!page || queue.isFetching || !!queue.error || busy || page.page >= page.totalPages} onClick={() => setApplied(current => ({ ...current, page: page!.page + 1 }))}>Next queue page</button>{page && !queue.error && <p>Server snapshot: {new Date(page.capturedAtUtc).toISOString()} (UTC){queue.isFetching ? ' - previously loaded snapshot' : ''}</p>}</div>
    {notice && <p role="status" className="ui-panel text-emerald-800">{notice}</p>}
    {queue.isError && <section role="alert" className="ui-error"><h2 className="font-bold">Approval queue unavailable</h2><p>{problemMessage(queue.error)}</p><button className="ui-secondary mt-3" disabled={queue.isFetching} onClick={() => void queue.refetch()}>Try again</button></section>}
    {!queue.isLoading && !queue.isError && queue.data?.totalCount === 0 && <section className="ui-panel"><h2 className="text-xl font-bold">Your queue is clear</h2><p className="mt-2 text-slate-600">No Pending requests match the applied filters. Reset filters or refresh to check for new requests.</p></section>}
    {!queue.isError && <section className="space-y-4" aria-label="Pending leave requests">{queue.data?.items.map(request => <ApprovalQueueItem key={request.id} leaveRequest={request} decisionPending={busy} processingAction={busy && decision?.request.id === request.id ? decision.action : undefined} onApprove={id => open(id, 'approve')} onReject={id => open(id, 'reject')} />)}</section>}
    {decision && <ConfirmationDialog title={decision.action === 'approve' ? 'Approve leave request' : 'Reject leave request'} confirmLabel={`Confirm ${decision.action}`} neutral={decision.action === 'approve'} returnFocus={opener.current} busy={busy || refreshing} confirmDisabled={tooLong || needsRefresh || unavailable || discarding} onCancel={close} onConfirm={() => void confirm()}>
      <p className="font-semibold text-slate-900">{decision.request.employeeFullName} · {decision.request.leaveTypeName}</p>
      <p>{calendarDate(decision.request.startDate)} – {calendarDate(decision.request.endDate)} (inclusive calendar dates)</p>
      <p>The note is visible to the employee, eligible Managers and HR in authorised history. Do not include medical details or other sensitive personnel information.</p>
      <label htmlFor="decision-note" className="block font-semibold text-slate-900">Decision note (optional)</label>
      <textarea ref={noteField} id="decision-note" rows={4} className="ui-input" disabled={busy || refreshing} value={note} onChange={event => setNote(event.target.value)} aria-invalid={tooLong} aria-describedby={`decision-note-guidance${tooLong ? ' decision-note-error' : ''}`} />
      <p id="decision-note-guidance">{note.trim().length}/{MAX_DECISION_NOTE_LENGTH} trimmed characters. Blank notes are not recorded. A rejection note is optional.</p>
      {tooLong && <p id="decision-note-error" role="alert" className="text-rose-700">Shorten your note to 500 trimmed characters.</p>}
      {failure && <div role="alert" className="ui-error"><p>{failure}</p><p className="mt-2">Your note has been kept. No automatic retry is made.</p></div>}
      {!unavailable && <button type="button" className="ui-secondary" disabled={busy || refreshing} onClick={() => void refreshDecision()}>Refresh decision availability</button>}
      {unavailable && <button type="button" className="ui-secondary" onClick={close}>Return to queue</button>}
      {discarding && <section className="ui-warning" aria-label="Discard decision note"><p>Discard your unsaved note and close?</p><div className="mt-3 flex flex-wrap gap-3"><button type="button" className="ui-secondary" onClick={() => { setDiscarding(false); noteField.current?.focus(); }}>Keep note</button><button type="button" className="ui-danger" onClick={() => { setDecision(null); setNote(''); }}>Discard note</button></div></section>}
    </ConfirmationDialog>}
  </div></div>;
}
