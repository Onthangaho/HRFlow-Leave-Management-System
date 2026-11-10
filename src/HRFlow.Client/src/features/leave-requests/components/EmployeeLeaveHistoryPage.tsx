import { useEffect, useState, type FormEvent } from 'react';
import { useAuth } from '../../auth/hooks/useAuth';
import { Link } from 'react-router-dom';
import {
  useCancelLeaveRequest,
  useEmployeeLeaveHistory,
  useLeaveBalances, historyDefaults, type HistoryFilters,
} from '../api';
import { LeaveRequestHistoryItem } from './LeaveRequestHistoryItem';

/**
 * Gives employees a single self-service view of server-derived balances and their own leave
 * timeline, including confirmation-gated cancellation for requests still awaiting review.
 */
export function EmployeeLeaveHistoryPage() {
  const { user, sessionVersion } = useAuth();
  return <HistoryWorkspace key={user?.id + ':' + sessionVersion} />;
}

function HistoryWorkspace() {
  const balancesQuery = useLeaveBalances();
  const [confirmingId, setConfirmingId] = useState<string | null>(null);
  const [draft, setDraft] = useState<HistoryFilters>({ ...historyDefaults });
  const [applied, setApplied] = useState<HistoryFilters>({ ...historyDefaults });
  const historyQuery = useEmployeeLeaveHistory(applied);
  function apply(event: FormEvent) { event.preventDefault(); setConfirmingId(null); setApplied({ ...draft, page: 1 }); }
  const rows = historyQuery.data?.items;
  const page = historyQuery.data;
  useEffect(() => {
    if (page && !historyQuery.isFetching && applied.page !== page.page)
      setApplied(current => current === applied ? { ...current, page: page.page } : current);
  }, [page, historyQuery.isFetching, applied]);
  const typeOptions = balancesQuery.data ?? [];
  function field(name: keyof HistoryFilters, value: string) { setDraft(current => ({ ...current, [name]: name === 'pageSize' ? Number(value) : value })); }
  const cancelMutation = useCancelLeaveRequest();

  return (
    <div className="workspace-page space-y-6">
      <div className="mx-auto w-full max-w-5xl space-y-6">
        <section className="page-intro ui-panel">

          <div className="mt-6">
            <p className="text-xs font-bold uppercase tracking-[0.18em] text-indigo-200">Leave management</p>

            <Link to="/leave-requests/new" className="mt-4 inline-flex rounded-lg bg-white px-4 py-2 font-semibold text-indigo-900">Request leave</Link>
            <p className="mt-3 max-w-2xl text-sm leading-6 text-indigo-100">
              Keep track of your available leave and follow every request from submission through its decision.
            </p>
          </div>
        </section>

        {cancelMutation.error && (
          <div role="alert" className="rounded-2xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-700">
            Unable to cancel the leave request: {cancelMutation.error.message}
          </div>
        )}

        <section aria-labelledby="balances-heading">
          <div className="mb-4 flex items-end justify-between">
            <div>
              <p className="text-xs font-bold uppercase tracking-[0.16em] text-indigo-600">Current entitlement</p>
              <h2 id="balances-heading" className="mt-1 text-2xl font-bold text-slate-900">Leave balances</h2>
            </div>
          </div>

          {balancesQuery.isLoading && (
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {[1, 2, 3].map((item) => (
                <div key={item} className="animate-pulse rounded-2xl bg-white p-5 shadow-sm">
                  <div className="h-4 w-1/2 rounded bg-slate-200" />
                  <div className="mt-5 h-12 rounded bg-slate-100" />
                </div>
              ))}
            </div>
          )}

          {balancesQuery.error && (
            <div role="alert" className="rounded-2xl border border-rose-200 bg-white p-6 text-sm text-rose-700 shadow-sm">
              Unable to load leave balances: {balancesQuery.error.message}<button type="button" className="ui-secondary mt-3 block" disabled={balancesQuery.isFetching} onClick={() => void balancesQuery.refetch()}>Retry balances</button>
            </div>
          )}

          {!balancesQuery.isLoading && !balancesQuery.error && (
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {balancesQuery.data?.map((balance) => (
                <article key={balance.leaveTypeId} className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
                  <p className="text-sm font-bold text-slate-900">{balance.leaveTypeName}</p>
                  <p className="mt-4 text-3xl font-bold text-indigo-700">
                    {balance.remainingDays}
                    <span className="ml-1 text-base font-semibold text-slate-500">days remaining</span>
                  </p>
                  <dl className="mt-5 grid grid-cols-2 gap-3 border-t border-slate-100 pt-4 text-sm">
                    <div>
                      <dt className="text-slate-500">Entitled</dt>
                      <dd className="mt-1 font-bold text-slate-800">{balance.entitledDays} days</dd>
                    </div>
                    <div>
                      <dt className="text-slate-500">Used</dt>
                      <dd className="mt-1 font-bold text-slate-800">{balance.usedDays} days</dd>
                    </div>
                  </dl>
                </article>
              ))}
            </div>
          )}
        </section>

        <section aria-labelledby="history-heading">
          <div className="mb-4">
            <p className="text-xs font-bold uppercase tracking-[0.16em] text-indigo-600">Your timeline</p>
            <h2 id="history-heading" className="mt-1 text-2xl font-bold text-slate-900">Leave request history</h2>
          </div>

          <form aria-label="History filters" onSubmit={apply} className="ui-panel space-y-4">
            <p>Filter by inclusive date intersection: requests overlapping either boundary are included. Balances use all Approved history, regardless of these filters.</p>
            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
              <div className="ui-label"><label htmlFor="history-status">Request status</label><select id="history-status" className="ui-input" value={draft.status} onChange={e => field('status', e.target.value)}><option value="">All statuses</option>{['Pending','Approved','Rejected','Cancelled'].map(status => <option key={status}>{status}</option>)}</select></div>
              <div className="ui-label"><label htmlFor="history-type">History leave type</label><select id="history-type" className="ui-input" value={draft.leaveTypeId} onChange={e => field('leaveTypeId', e.target.value)}><option value="">All leave types</option>{typeOptions.map(type => <option key={type.leaveTypeId} value={type.leaveTypeId}>{type.leaveTypeName}</option>)}</select></div>
              <label className="ui-label">Period start<input className="ui-input" type="date" value={draft.start} onChange={e => field('start', e.target.value)} /></label>
              <label className="ui-label">Period end<input className="ui-input" type="date" min={draft.start || undefined} value={draft.end} onChange={e => field('end', e.target.value)} /></label>
              <div className="ui-label"><label htmlFor="history-size">Requests per page</label><select id="history-size" className="ui-input" value={draft.pageSize} onChange={e => field('pageSize', e.target.value)}>{[25,50,100].map(size => <option key={size}>{size}</option>)}</select></div>
            </div>
            <div className="flex flex-wrap gap-3"><button className="ui-primary" disabled={cancelMutation.isPending}>Apply history filters</button><button type="button" className="ui-secondary" disabled={cancelMutation.isPending} onClick={() => { setConfirmingId(null); setDraft({ ...historyDefaults }); setApplied({ ...historyDefaults }); }}>Reset history filters</button><button type="button" className="ui-secondary" disabled={historyQuery.isFetching || cancelMutation.isPending} onClick={() => void historyQuery.refetch()}>Refresh history</button></div>
          </form>
          <p className="text-sm" aria-label="Applied history filters">Applied: {applied.status || 'All statuses'} ? {typeOptions.find(t => t.leaveTypeId === applied.leaveTypeId)?.leaveTypeName || (applied.leaveTypeId ? 'Selected leave type' : 'All leave types')} ? {applied.start || 'Any start'} to {applied.end || 'Any end'}</p>
          <p role="status" aria-live="polite">{historyQuery.isFetching ? (page ? 'Refreshing history snapshot?' : 'Loading history?') : historyQuery.isError ? 'History unavailable. Retry to load results.' : page ? page.totalCount + ' matching requests. Page ' + page.page + ' of ' + page.totalPages + '.' : ''}</p>
          <div className="flex flex-wrap items-center gap-3">
            <button type="button" className="ui-secondary" disabled={!page || historyQuery.isFetching || !!historyQuery.error || cancelMutation.isPending || page.page <= 1} onClick={() => { if (!page) return; setConfirmingId(null); setApplied(current => ({ ...current, page: page.page - 1 })); }}>Previous history page</button>
            <button type="button" className="ui-secondary" disabled={!page || historyQuery.isFetching || !!historyQuery.error || cancelMutation.isPending || page.page >= page.totalPages} onClick={() => { if (!page) return; setConfirmingId(null); setApplied(current => ({ ...current, page: page.page + 1 })); }}>Next history page</button>
            {page && !historyQuery.error && <p className="text-sm">Server snapshot: {new Date(page.capturedAtUtc).toISOString()} (UTC){historyQuery.isFetching ? ' ? previously loaded snapshot' : ''}</p>}
          </div>

          {historyQuery.isLoading && (
            <div className="space-y-4">
              {[1, 2].map((item) => (
                <div key={item} className="animate-pulse rounded-2xl bg-white p-5 shadow-sm">
                  <div className="h-5 w-1/3 rounded bg-slate-200" />
                  <div className="mt-4 h-4 w-1/2 rounded bg-slate-100" />
                </div>
              ))}
            </div>
          )}

          {historyQuery.error && (
            <div role="alert" className="rounded-2xl border border-rose-200 bg-white p-6 text-sm text-rose-700 shadow-sm">
              Unable to load leave history: {historyQuery.error.message}<button type="button" className="ui-secondary mt-3 block" disabled={historyQuery.isFetching} onClick={() => void historyQuery.refetch()}>Retry history</button>
            </div>
          )}

          {!historyQuery.isLoading && !historyQuery.error && rows?.length === 0 && (
            <div className="rounded-3xl border border-dashed border-slate-300 bg-white px-6 py-16 text-center shadow-sm">
              <div className="mx-auto flex size-14 items-center justify-center rounded-2xl bg-indigo-100 text-2xl text-indigo-700">
                +
              </div>
              <h3 className="mt-5 text-xl font-bold text-slate-900">No matching leave requests</h3>
              <p className="mx-auto mt-2 max-w-md text-sm leading-6 text-slate-600">
                Try changing or resetting the applied filters. Your submitted requests and decisions remain available.
              </p>
            </div>
          )}

          {!historyQuery.isLoading && !historyQuery.error && (rows?.length ?? 0) > 0 && (
            <div className="space-y-4">
              {rows?.map((leaveRequest) => (
                <LeaveRequestHistoryItem
                  key={leaveRequest.id}
                  leaveRequest={leaveRequest}
                  isConfirmingCancellation={confirmingId === leaveRequest.id}
                  setIsConfirmingCancellation={value => setConfirmingId(value ? leaveRequest.id : null)}
                  isCancelling={cancelMutation.isPending && cancelMutation.variables === leaveRequest.id}
                  onCancel={(leaveRequestId) => cancelMutation.mutate(leaveRequestId)}
                />
              ))}
            </div>
          )}
        </section>
      </div>
    </div>
  );
}
