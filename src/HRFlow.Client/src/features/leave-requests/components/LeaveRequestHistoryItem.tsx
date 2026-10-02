import { useState } from 'react';
import type { EmployeeLeaveRequest } from '../types';

interface LeaveRequestHistoryItemProps {
  leaveRequest: EmployeeLeaveRequest;
  isCancelling: boolean;
  onCancel: (leaveRequestId: string) => void;
}

const statusStyles: Record<EmployeeLeaveRequest['status'], string> = {
  Pending: 'bg-amber-100 text-amber-800 ring-amber-200',
  Approved: 'bg-emerald-100 text-emerald-800 ring-emerald-200',
  Rejected: 'bg-rose-100 text-rose-800 ring-rose-200',
  Cancelled: 'bg-slate-200 text-slate-700 ring-slate-300',
};

function formatDate(date: string): string {
  return new Intl.DateTimeFormat('en-US', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  }).format(new Date(date));
}

function formatDateTime(date: string): string {
  return new Intl.DateTimeFormat('en-US', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  }).format(new Date(date));
}

/**
 * Combines a request's final status and its immutable audit timeline so employees can understand
 * what happened without offering actions that no longer match the request lifecycle.
 */
export function LeaveRequestHistoryItem({
  leaveRequest,
  isCancelling,
  onCancel,
}: LeaveRequestHistoryItemProps) {
  const [isConfirmingCancellation, setIsConfirmingCancellation] = useState(false);
  const isPending = leaveRequest.status === 'Pending';

  const confirmCancellation = () => {
    onCancel(leaveRequest.id);
    setIsConfirmingCancellation(false);
  };

  return (
    <article className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition duration-200 hover:-translate-y-0.5 hover:border-slate-300 hover:shadow-md">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <div className="flex flex-wrap items-center gap-3">
            <h2 className="text-lg font-bold text-slate-900">{leaveRequest.leaveTypeName}</h2>
            <span className={`rounded-full px-3 py-1 text-xs font-bold ring-1 ${statusStyles[leaveRequest.status]}`}>
              {leaveRequest.status}
            </span>
          </div>
          <p className="mt-2 text-sm font-medium text-slate-600">
            {formatDate(leaveRequest.startDate)} - {formatDate(leaveRequest.endDate)}
          </p>
        </div>

        {isPending && !isConfirmingCancellation && (
          <button
            type="button"
            onClick={() => setIsConfirmingCancellation(true)}
            disabled={isCancelling}
            className="inline-flex min-w-28 items-center justify-center rounded-xl border border-rose-200 bg-rose-50 px-4 py-2.5 text-sm font-bold text-rose-700 transition hover:border-rose-300 hover:bg-rose-100 focus:outline-none focus:ring-4 focus:ring-rose-100 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isCancelling ? 'Cancelling...' : 'Cancel request'}
          </button>
        )}
      </div>

      {isConfirmingCancellation && (
        <div className="mt-5 flex flex-col gap-3 rounded-xl border border-amber-200 bg-amber-50 p-4 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-sm font-medium text-amber-900">
            Cancel this pending {leaveRequest.leaveTypeName.toLowerCase()} leave request?
          </p>
          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => setIsConfirmingCancellation(false)}
              className="rounded-lg px-3 py-2 text-sm font-semibold text-slate-600 transition hover:bg-white"
            >
              Keep request
            </button>
            <button
              type="button"
              onClick={confirmCancellation}
              disabled={isCancelling}
              className="rounded-lg bg-rose-700 px-3 py-2 text-sm font-semibold text-white transition hover:bg-rose-600 disabled:cursor-not-allowed disabled:opacity-60"
            >
              Confirm cancellation
            </button>
          </div>
        </div>
      )}

      {leaveRequest.decisionHistory.length > 0 && (
        <section className="mt-5 border-t border-slate-100 pt-4" aria-label="Decision history">
          <h3 className="text-xs font-bold uppercase tracking-[0.16em] text-slate-500">Decision history</h3>
          <ol className="mt-3 space-y-3">
            {leaveRequest.decisionHistory.map((decision) => (
              <li key={`${decision.action}-${decision.timestamp}`} className="flex gap-3 text-sm">
                <span className="mt-1.5 size-2 shrink-0 rounded-full bg-indigo-500" aria-hidden="true" />
                <p className="text-slate-600">
                  <span className="font-semibold text-slate-800">{decision.action}</span> by{' '}
                  <span className="font-semibold text-slate-800">{decision.actorFullName}</span>
                  {' - '}
                  {decision.oldStatus ?? 'New'} to {decision.newStatus}
                  <span className="block text-xs text-slate-500">{formatDateTime(decision.timestamp)}</span>
                </p>
              </li>
            ))}
          </ol>
        </section>
      )}
    </article>
  );
}
