import axios from 'axios';
import { Link, useParams } from 'react-router-dom';
import { useAuth } from '../auth/hooks/useAuth';
import { useRequestTimeline } from './api';
import type { RequestTimeline } from './api';
import { SupportingDocuments } from '../leave-requests/components/SupportingDocuments';

const dateFormat = new Intl.DateTimeFormat('en', { dateStyle: 'medium', timeZone: 'UTC' });
const timestampFormat = new Intl.DateTimeFormat('en', { dateStyle: 'medium', timeStyle: 'medium', timeZone: 'UTC' });
const calendarDate = (value: string) => dateFormat.format(new Date(value + 'T00:00:00Z'));
function failure(error: unknown) {
  if (axios.isAxiosError(error)) {
    if (error.response?.status === 403) return 'Your account no longer has permission to view this history.';
    if (error.response?.status === 404) return 'This request was not found or is not available to your account.';
  }
  return 'Request history could not be loaded. Check your connection and try again.';
}

/** Read-only detail route resets protected content when either the account or login session changes. */
export function LeaveRequestTimelinePage() {
  const { id = '' } = useParams();
  const { user, sessionVersion } = useAuth();
  return <TimelineWorkspace key={user?.id + ':' + sessionVersion + ':' + id} requestId={id} />;
}
function TimelineWorkspace({ requestId }: { requestId: string }) {
  const { user } = useAuth();
  const history = useRequestTimeline(requestId);
  return <div className="workspace-page space-y-6">
    <div className="mx-auto max-w-3xl space-y-6">
      <section className="page-intro ui-panel">

        <p className="pt-3 text-xs font-bold uppercase tracking-widest text-slate-500">Read-only history</p>

        <nav aria-label="Leave workspaces" className="flex flex-wrap gap-4 text-sm font-semibold text-indigo-700">
          {user?.roles.some(role => ['Employee', 'Manager'].includes(role)) && <Link className="underline" to="/leave-requests/history">My leave</Link>}
          {user?.roles.includes('Manager') && <Link className="underline" to="/leave-requests/approvals">Approval queue</Link>}
          {user?.roles.includes('HR Administrator') && <Link className="underline" to="/admin/leave-monitoring">Pending monitoring</Link>}
        </nav>
      </section>
      {history.data && <SupportingDocuments requestId={requestId} />}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <p role="status" aria-live="polite" className="text-sm text-slate-600">{history.isFetching ? (history.data ? 'Refreshing previously loaded history…' : 'Loading request history…') : history.isSuccess ? 'Request history loaded.' : ''}</p>
        <button className="ui-secondary" disabled={history.isFetching} onClick={() => void history.refetch()}>Refresh history</button>
      </div>
      {history.isPending && <section className="ui-panel" role="status">Loading the request and recorded events…</section>}
      {history.isError && <section className="ui-panel space-y-3" role="alert"><h2 className="text-lg font-bold">History unavailable</h2><p>{failure(history.error)}</p><button className="ui-primary" disabled={history.isFetching} onClick={() => void history.refetch()}>Try again</button></section>}
      {!history.isError && history.data && <LeaveRequestTimeline request={history.data} refreshing={history.isFetching} />}
    </div>
  </div>;
}

/** Reusable semantic ordered timeline preserves event order and labels every timestamp explicitly UTC. */
export function LeaveRequestTimeline({ request, refreshing = false }: { request: RequestTimeline; refreshing?: boolean }) {
  return <section className="space-y-6" aria-label="Request summary and timeline" aria-busy={refreshing}>
    <section className="ui-panel space-y-4">
      <div className="flex flex-wrap items-center gap-3"><h2 className="break-words text-xl font-bold text-slate-900">{request.leaveTypeName}</h2><span className="rounded-full bg-indigo-50 px-3 py-1 text-sm font-semibold text-indigo-800">{request.status}</span></div>
      <p className="break-words font-semibold text-slate-800">{request.employeeName}{!request.employeeIsActive && ' · Inactive employee'}</p>
      <p className="text-slate-600">{calendarDate(request.startDate)} – {calendarDate(request.endDate)} · Inclusive calendar dates</p>
      <p className="text-sm text-slate-600">Actor names are current display names. Event times are shown in UTC. This history cannot be edited.</p>
    </section>
    <section className="ui-panel space-y-3" aria-label="Submission requirements">
      <h2 className="text-lg font-bold">Requirements at submission</h2>
      {request.submissionRequirements ? <><p>Description: {request.submissionRequirements.descriptionMode}. Evidence: {request.submissionRequirements.evidenceMode} ({request.submissionRequirements.evidenceClass}).</p>
        <p className="text-sm">Company configuration, not a statutory eligibility or payment decision. Later configuration changes do not alter these requirements.</p>
        {request.submissionRequirements.instructions && <p className="whitespace-pre-wrap break-words">{request.submissionRequirements.instructions}</p>}
        <p className="text-sm">Rule {request.submissionRequirements.ruleId} v{request.submissionRequirements.ruleVersion}</p></>
        : <p>Unknown: submission requirements were not recorded for this legacy request.</p>}
      {request.description && <><h3 className="font-semibold">Submitted description</h3><p className="whitespace-pre-wrap break-words">{request.description}</p></>}
    </section>
    {!request.submissionRecorded && <p className="ui-warning" role="status">Submission event was not recorded for this legacy request.</p>}
    <section className="ui-panel">
      <h2 className="text-lg font-bold text-slate-900">Recorded lifecycle</h2>
      {request.events.length === 0 ? <p className="mt-4 text-slate-600">No lifecycle events were recorded for this request.</p> : <ol className="mt-5 space-y-6">
        {request.events.map(event => <li key={event.id} className="relative border-l-2 border-indigo-200 pl-5">
          <span aria-hidden="true" className="absolute -left-[5px] top-1 size-2 rounded-full bg-indigo-700" />
          <h3 className="font-bold text-slate-900">{event.action === 'Submit' ? 'Submitted' : event.action === 'Cancel' ? 'Cancelled' : event.action === 'Approve' ? 'Approved' : event.action === 'Reject' ? 'Rejected' : event.action}</h3>
          <p className="mt-1 break-words text-sm text-slate-700">By {event.actorName}</p>
          <p className="mt-1 text-sm font-semibold text-indigo-800">{event.oldStatus ?? 'Initial submission'} → {event.newStatus}</p>
          <time className="mt-2 block text-sm text-slate-600" dateTime={event.timestampUtc}>{timestampFormat.format(new Date(event.timestampUtc))} UTC</time>
          {event.action === 'Submit' && <p className="mt-2 text-sm text-slate-600">Submitted by the requesting employee. Pending requests do not reserve balance.</p>}
          {event.action === 'Cancel' && !event.explanation && <p className="mt-2 text-sm text-slate-600">Withdrawn by the request owner while Pending.</p>}
          {event.decisionNote && <p className="mt-2 whitespace-pre-wrap break-words text-sm text-slate-700">Manager note: {event.decisionNote}</p>}
          {event.explanation && <p className="mt-2 whitespace-pre-wrap break-words text-sm text-slate-700">{event.explanation}</p>}
        </li>)}
      </ol>}
    </section>
  </section>;
}
