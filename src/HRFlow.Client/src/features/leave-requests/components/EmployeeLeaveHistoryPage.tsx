import { Link } from 'react-router-dom';
import {
  useCancelLeaveRequest,
  useEmployeeLeaveHistory,
  useLeaveBalances,
} from '../api';
import { LeaveRequestHistoryItem } from './LeaveRequestHistoryItem';

/**
 * Gives employees a single self-service view of server-derived balances and their own leave
 * timeline, including confirmation-gated cancellation for requests still awaiting review.
 */
export function EmployeeLeaveHistoryPage() {
  const balancesQuery = useLeaveBalances();
  const historyQuery = useEmployeeLeaveHistory();
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

          {!historyQuery.isLoading && !historyQuery.error && historyQuery.data?.length === 0 && (
            <div className="rounded-3xl border border-dashed border-slate-300 bg-white px-6 py-16 text-center shadow-sm">
              <div className="mx-auto flex size-14 items-center justify-center rounded-2xl bg-indigo-100 text-2xl text-indigo-700">
                +
              </div>
              <h3 className="mt-5 text-xl font-bold text-slate-900">No leave requests yet</h3>
              <p className="mx-auto mt-2 max-w-md text-sm leading-6 text-slate-600">
                Your submitted requests and their decisions will appear here.
              </p>
            </div>
          )}

          {!historyQuery.isLoading && !historyQuery.error && (historyQuery.data?.length ?? 0) > 0 && (
            <div className="space-y-4">
              {historyQuery.data?.map((leaveRequest) => (
                <LeaveRequestHistoryItem
                  key={leaveRequest.id}
                  leaveRequest={leaveRequest}
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
