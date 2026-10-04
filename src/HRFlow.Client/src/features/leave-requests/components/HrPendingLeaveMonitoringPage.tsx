import { Link } from 'react-router-dom';
import { useOrganisationPendingLeaveRequests } from '../api';

/**
 * Gives HR Administrators an organisation-wide operational view without exposing manager decision controls.
 */
export function HrPendingLeaveMonitoringPage() {
  const { data: leaveRequests, isLoading, error } = useOrganisationPendingLeaveRequests();

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
              <p className="text-xs font-bold uppercase tracking-[0.18em] text-indigo-200">HR monitoring</p>
              <h1 className="mt-2 text-3xl font-bold tracking-tight sm:text-4xl">Pending leave requests</h1>
              <p className="mt-3 max-w-2xl text-sm leading-6 text-indigo-100">
                Monitor organisation-wide pending leave. Assigned managers make approval decisions.
              </p>
            </div>
            <div className="rounded-2xl border border-white/15 bg-white/10 px-4 py-3 backdrop-blur-sm">
              <p className="text-xs font-semibold uppercase tracking-wide text-indigo-200">Pending</p>
              <p className="mt-1 text-2xl font-bold">{leaveRequests?.length ?? 0}</p>
            </div>
          </div>
        </header>

        {isLoading && (
          <section aria-label="Loading pending leave monitoring" className="space-y-4">
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
            <h2 className="text-lg font-bold text-slate-900">Unable to load pending leave monitoring</h2>
            <p className="mt-2 text-sm text-rose-600">{error.message}</p>
          </section>
        )}

        {!isLoading && !error && leaveRequests?.length === 0 && (
          <section className="rounded-3xl border border-dashed border-slate-300 bg-white px-6 py-16 text-center shadow-sm">
            <h2 className="text-xl font-bold text-slate-900">No pending leave requests</h2>
            <p className="mx-auto mt-2 max-w-md text-sm leading-6 text-slate-600">
              New requests will appear here for monitoring while their assigned managers review them.
            </p>
          </section>
        )}

        {!isLoading && !error && (leaveRequests?.length ?? 0) > 0 && (
          <section className="space-y-4" aria-label="Organisation pending leave requests">
            {leaveRequests?.map((leaveRequest) => (
              <article
                key={leaveRequest.id}
                className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm"
              >
                <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                  <div>
                    <h2 className="text-base font-bold text-slate-900">{leaveRequest.employeeFullName}</h2>
                    <p className="mt-1 text-sm text-slate-500">{leaveRequest.employeeEmail}</p>
                  </div>
                  <div className="flex gap-3 text-sm">
                    <span className="rounded-lg bg-indigo-50 px-3 py-2 font-semibold text-indigo-700">
                      {leaveRequest.leaveTypeName}
                    </span>
                    <span className="rounded-lg bg-slate-100 px-3 py-2 font-semibold text-slate-700">
                      {new Intl.DateTimeFormat('en-US', {
                        day: 'numeric',
                        month: 'short',
                        year: 'numeric',
                      }).format(new Date(leaveRequest.startDate))}
                      {' - '}
                      {new Intl.DateTimeFormat('en-US', {
                        day: 'numeric',
                        month: 'short',
                        year: 'numeric',
                      }).format(new Date(leaveRequest.endDate))}
                    </span>
                  </div>
                </div>
              </article>
            ))}
          </section>
        )}
      </div>
    </main>
  );
}
