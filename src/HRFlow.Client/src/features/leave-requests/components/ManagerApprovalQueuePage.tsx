import { Link } from 'react-router-dom';
import {
  useApproveLeaveRequest,
  usePendingLeaveRequests,
  useRejectLeaveRequest,
} from '../api';
import { ApprovalQueueItem } from './ApprovalQueueItem';

/**
 * Provides Managers and HR Administrators a focused, server-scoped workspace for pending decisions.
 */
export function ManagerApprovalQueuePage() {
  const { data: leaveRequests, isLoading, error } = usePendingLeaveRequests();
  const approveMutation = useApproveLeaveRequest();
  const rejectMutation = useRejectLeaveRequest();
  const mutationError = approveMutation.error ?? rejectMutation.error;
  const processingRequestId = approveMutation.isPending
    ? approveMutation.variables
    : rejectMutation.isPending
      ? rejectMutation.variables
      : undefined;
  const processingAction = approveMutation.isPending
    ? 'approve'
    : rejectMutation.isPending
      ? 'reject'
      : undefined;

  return (
    <main className="min-h-screen bg-gradient-to-b from-indigo-50 via-slate-50 to-slate-100 px-4 py-8 sm:px-6 lg:px-8">
      <div className="mx-auto w-full max-w-5xl space-y-6">
        <header className="rounded-3xl bg-gradient-to-br from-slate-950 via-indigo-950 to-violet-900 p-6 text-white shadow-xl sm:p-8">
          <Link
            to="/"
            className="inline-flex items-center text-sm font-semibold text-indigo-200 transition hover:text-white"
          >
            Back to dashboard
          </Link>
          <div className="mt-6 flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between">
            <div>
              <p className="text-xs font-bold uppercase tracking-[0.18em] text-indigo-200">Leave management</p>
              <h1 className="mt-2 text-3xl font-bold tracking-tight sm:text-4xl">Approval queue</h1>
              <p className="mt-3 max-w-2xl text-sm leading-6 text-indigo-100">
                Review pending requests from your direct reports and keep your team's leave plans moving.
              </p>
            </div>
            <div className="rounded-2xl border border-white/15 bg-white/10 px-4 py-3 backdrop-blur-sm">
              <p className="text-xs font-semibold uppercase tracking-wide text-indigo-200">Awaiting review</p>
              <p className="mt-1 text-2xl font-bold">{leaveRequests?.length ?? 0}</p>
            </div>
          </div>
        </header>

        {mutationError && (
          <div role="alert" className="rounded-2xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-700">
            Unable to update the leave request: {mutationError.message}
          </div>
        )}

        {isLoading && (
          <section aria-label="Loading approval queue" className="space-y-4">
            {[1, 2, 3].map((item) => (
              <div key={item} className="animate-pulse rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
                <div className="h-5 w-1/3 rounded bg-slate-200" />
                <div className="mt-4 h-12 rounded-xl bg-slate-100" />
              </div>
            ))}
          </section>
        )}

        {error && (
          <section role="alert" className="rounded-2xl border border-rose-200 bg-white p-8 text-center shadow-sm">
            <h2 className="text-lg font-bold text-slate-900">Unable to load the approval queue</h2>
            <p className="mt-2 text-sm text-rose-600">{error.message}</p>
          </section>
        )}

        {!isLoading && !error && leaveRequests?.length === 0 && (
          <section className="rounded-3xl border border-dashed border-slate-300 bg-white px-6 py-16 text-center shadow-sm">
            <div className="mx-auto flex size-14 items-center justify-center rounded-2xl bg-emerald-100 text-2xl text-emerald-700">
              ✓
            </div>
            <h2 className="mt-5 text-xl font-bold text-slate-900">Your queue is clear</h2>
            <p className="mx-auto mt-2 max-w-md text-sm leading-6 text-slate-600">
              There are no pending leave requests to review right now. New requests will appear here automatically.
            </p>
          </section>
        )}

        {!isLoading && !error && (leaveRequests?.length ?? 0) > 0 && (
          <section className="space-y-4" aria-label="Pending leave requests">
            {leaveRequests?.map((leaveRequest) => (
              <ApprovalQueueItem
                key={leaveRequest.id}
                leaveRequest={leaveRequest}
                processingAction={processingRequestId === leaveRequest.id ? processingAction : undefined}
                onApprove={(leaveRequestId) => approveMutation.mutate(leaveRequestId)}
                onReject={(leaveRequestId) => rejectMutation.mutate(leaveRequestId)}
              />
            ))}
          </section>
        )}
      </div>
    </main>
  );
}
