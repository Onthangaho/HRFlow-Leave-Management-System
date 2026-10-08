import { Link } from 'react-router-dom';
import type { PendingLeaveRequest } from '../types';

interface ApprovalQueueItemProps {
  leaveRequest: PendingLeaveRequest;
  processingAction?: 'approve' | 'reject';
  decisionPending?: boolean;
  onApprove: (leaveRequestId: string) => void;
  onReject: (leaveRequestId: string) => void;
}

function formatDate(date: string): string {
  return new Intl.DateTimeFormat('en-US', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  }).format(new Date(date));
}

/**
 * Keeps a single pending request's context and destructive decision controls together, preventing
 * accidental action selection across rows on narrow screens.
 */
export function ApprovalQueueItem({
  leaveRequest,
  processingAction,
  decisionPending,
  onApprove,
  onReject,
}: ApprovalQueueItemProps) {
  const isProcessing = decisionPending || processingAction !== undefined;
  const dateRange = `${formatDate(leaveRequest.startDate)} - ${formatDate(leaveRequest.endDate)}`;

  return (
    <article className="group rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition duration-200 hover:-translate-y-0.5 hover:border-slate-300 hover:shadow-md">
      <Link className="mb-4 inline-block font-semibold text-indigo-700 underline" to={`/leave-requests/${leaveRequest.id}/history`}>View history</Link>
      <div className="flex flex-col gap-5 lg:flex-row lg:items-center lg:justify-between">
        <div className="min-w-0 space-y-3">
          <div className="flex flex-wrap items-center gap-3">
            <div className="flex size-11 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-indigo-500 to-violet-600 text-sm font-bold text-white shadow-sm">
              {leaveRequest.employeeFullName
                .split(' ')
                .map((name) => name[0])
                .join('')
                .slice(0, 2)
                .toUpperCase()}
            </div>
            <div className="min-w-0">
              <h2 className="truncate text-base font-bold text-slate-900">{leaveRequest.employeeFullName}</h2>
              <p className="truncate text-sm text-slate-500">{leaveRequest.employeeEmail}</p>
            </div>
          </div>

          <dl className="grid gap-3 sm:grid-cols-2">
            <div className="rounded-xl bg-indigo-50 px-3 py-2">
              <dt className="text-xs font-semibold uppercase tracking-wide text-indigo-600">Leave type</dt>
              <dd className="mt-1 text-sm font-semibold text-slate-800">{leaveRequest.leaveTypeName}</dd>
            </div>
            <div className="rounded-xl bg-slate-50 px-3 py-2">
              <dt className="text-xs font-semibold uppercase tracking-wide text-slate-500">Requested dates</dt>
              <dd className="mt-1 text-sm font-semibold text-slate-800">{dateRange}</dd>
            </div>
          </dl>
        </div>

        <div className="flex shrink-0 flex-col gap-2 sm:flex-row">
          <button
            type="button"
            onClick={() => onApprove(leaveRequest.id)}
            disabled={isProcessing}
            className="ui-primary min-w-28"
          >
            {processingAction === 'approve' ? 'Approving...' : 'Approve'}
          </button>
          <button
            type="button"
            onClick={() => onReject(leaveRequest.id)}
            disabled={isProcessing}
            className="ui-danger min-w-28"
          >
            {processingAction === 'reject' ? 'Rejecting...' : 'Reject'}
          </button>
        </div>
      </div>

    </article>
  );
}
