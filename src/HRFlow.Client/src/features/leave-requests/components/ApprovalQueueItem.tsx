import { useState } from 'react';
import type { PendingLeaveRequest } from '../types';

interface ApprovalQueueItemProps {
  leaveRequest: PendingLeaveRequest;
  processingAction?: 'approve' | 'reject';
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
  onApprove,
  onReject,
}: ApprovalQueueItemProps) {
  const [confirmingAction, setConfirmingAction] = useState<'approve' | 'reject' | null>(null);
  const isProcessing = processingAction !== undefined;
  const dateRange = `${formatDate(leaveRequest.startDate)} - ${formatDate(leaveRequest.endDate)}`;

  const confirmAction = () => {
    if (confirmingAction === 'approve') {
      onApprove(leaveRequest.id);
    }

    if (confirmingAction === 'reject') {
      onReject(leaveRequest.id);
    }

    setConfirmingAction(null);
  };

  return (
    <article className="group rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition duration-200 hover:-translate-y-0.5 hover:border-slate-300 hover:shadow-md">
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
            onClick={() => setConfirmingAction('approve')}
            disabled={isProcessing}
            className="inline-flex min-w-28 items-center justify-center rounded-xl bg-emerald-600 px-4 py-2.5 text-sm font-bold text-white shadow-sm transition hover:bg-emerald-500 focus:outline-none focus:ring-4 focus:ring-emerald-100 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {processingAction === 'approve' ? 'Approving...' : 'Approve'}
          </button>
          <button
            type="button"
            onClick={() => setConfirmingAction('reject')}
            disabled={isProcessing}
            className="inline-flex min-w-28 items-center justify-center rounded-xl border border-rose-200 bg-rose-50 px-4 py-2.5 text-sm font-bold text-rose-700 transition hover:border-rose-300 hover:bg-rose-100 focus:outline-none focus:ring-4 focus:ring-rose-100 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {processingAction === 'reject' ? 'Rejecting...' : 'Reject'}
          </button>
        </div>
      </div>

      {confirmingAction && (
        <div className="mt-5 flex flex-col gap-3 rounded-xl border border-amber-200 bg-amber-50 p-4 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-sm font-medium text-amber-900">
            {confirmingAction === 'approve'
              ? `Approve ${leaveRequest.employeeFullName}'s leave request?`
              : `Reject ${leaveRequest.employeeFullName}'s leave request?`}
          </p>
          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => setConfirmingAction(null)}
              className="rounded-lg px-3 py-2 text-sm font-semibold text-slate-600 transition hover:bg-white"
            >
              Cancel
            </button>
            <button
              type="button"
              onClick={confirmAction}
              className="rounded-lg bg-slate-900 px-3 py-2 text-sm font-semibold text-white transition hover:bg-slate-700"
            >
              Confirm {confirmingAction}
            </button>
          </div>
        </div>
      )}
    </article>
  );
}
