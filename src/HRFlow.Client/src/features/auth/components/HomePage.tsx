import { Link } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth.tsx';

/**
 * Acts as a simple protected landing page to demonstrate auth-state rendering and role-gated navigation.
 */
export function HomePage() {
  const { user, logout } = useAuth();

  return (
    <main className="mx-auto mt-16 w-full max-w-3xl space-y-6 rounded-2xl border border-slate-200 bg-white p-8 shadow-sm">
      <header className="space-y-1">
        <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
          Protected route
        </p>
        <h1 className="text-3xl font-bold text-slate-900">HRFlow Dashboard</h1>
      </header>

      <section className="rounded-lg bg-slate-50 p-4">
        <p className="text-sm text-slate-600">Signed in as</p>
        <p className="text-lg font-semibold text-slate-900">{user?.email}</p>
        <p className="mt-2 text-sm text-slate-700">
          Roles: {user?.roles.length ? user.roles.join(', ') : 'none'}
        </p>
      </section>

      <nav className="flex flex-wrap gap-3">
        <Link
          to="/admin"
          className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-900 hover:bg-slate-100"
        >
          Open role-gated /admin page
        </Link>
        <button
          type="button"
          onClick={logout}
          className="rounded-lg bg-rose-600 px-4 py-2 text-sm font-semibold text-white hover:bg-rose-500"
        >
          Sign out
        </button>
      </nav>
    </main>
  );
}
