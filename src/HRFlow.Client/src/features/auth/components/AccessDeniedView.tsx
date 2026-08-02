/**
 * Shows a denial state instead of protected content when role checks fail, which prevents accidental data exposure.
 */
export function AccessDeniedView() {
  return (
    <main className="mx-auto mt-16 w-full max-w-xl rounded-xl border border-rose-200 bg-rose-50 p-8 text-center shadow-sm">
      <p className="text-sm font-semibold uppercase tracking-wide text-rose-700">
        Access denied
      </p>
      <h1 className="mt-2 text-2xl font-bold text-rose-900">
        You do not have permission to view this page.
      </h1>
      <p className="mt-3 text-sm text-rose-800">
        Contact an HR administrator if you believe this is an error.
      </p>
    </main>
  );
}
