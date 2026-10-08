import { Link } from 'react-router-dom';
import { navigationFor } from '../../../components/layout/navigation';
import { useAuth } from '../hooks/useAuth';

/** Honest quick actions for current capabilities; later dashboards supply API-backed metrics. */
export function HomePage() {
  const { user } = useAuth();
  const groups = navigationFor(user?.roles ?? []);
  return <div className="space-y-8">
    <section className="ui-panel">
      <h2 className="text-2xl font-bold text-slate-900">Welcome to HRFlow</h2>
      <p className="mt-3 max-w-2xl leading-7 text-slate-600">Manage leave and keep your workplace moving. Choose a task below, or use the navigation to return to a workspace.</p>
    </section>
    {groups.filter(group => group.items.some(item => item.path !== '/')).map(group => <section key={group.label} aria-label={group.label + ' quick actions'}>
      <h2 className="mb-4 text-lg font-bold text-slate-900">{group.label}</h2>
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {group.items.filter(item => item.path !== '/').map(item => <Link key={item.path} to={item.path} className="quick-action ui-panel">
          <span className="font-semibold text-indigo-800">{item.label}<span aria-hidden="true" className="ml-2">→</span></span>
          <p className="mt-2 text-sm leading-6 text-slate-600">{item.description}</p>
        </Link>)}
      </div>
    </section>)}
    {groups.every(group => group.items.length === 1) && <section className="ui-panel"><h2 className="font-semibold">No workspaces available</h2><p className="mt-2 text-slate-600">Contact HR if you need access to a leave or administration workspace.</p></section>}
  </div>;
}
