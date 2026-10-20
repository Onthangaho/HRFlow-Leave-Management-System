import { Component, lazy, Suspense, useId, useState, type ComponentType, type ReactNode } from 'react';
import { useAuth } from '../features/auth/hooks/useAuth';

interface FailureProps { children: ReactNode; label: string; headingId: string; retry: (url?: string) => void }

function failedAsset(error: unknown): string | undefined {
  if (!(error instanceof Error)) return;
  const value = error.message.match(/https?:\/\/[^\s"'<>]+/)?.[0];
  if (!value) return;
  try {
    const url = new URL(value);
    // Only retry application code on this origin; never evaluate an arbitrary URL from an error.
    const applicationAsset = /^\/assets\/[A-Za-z0-9_-]+\.js$/.test(url.pathname);
    const developmentModule = import.meta.env.DEV && /^\/src\/features\/[A-Za-z0-9/_-]+\.tsx$/.test(url.pathname);
    if (url.origin !== location.origin || url.username || url.password || url.hash || !(applicationAsset || developmentModule)) return;
    url.search = ''; return url.href;
  } catch { return; }
}

/** Keeps failed code loading local to a feature; never reloads the document or retries business writes. */
class FeatureFailure extends Component<FailureProps, { failed: boolean; url?: string }> {
  state: { failed: boolean; url?: string } = { failed: false };
  static getDerivedStateFromError(error: unknown) { return { failed: true, url: failedAsset(error) }; }
  render() {
    if (!this.state.failed) return this.props.children;
    return <section className="ui-panel min-h-48 space-y-3" role="alert" aria-labelledby={this.props.headingId}>
      <h2 id={this.props.headingId} className="text-xl font-semibold">{this.props.label} could not open</h2>
      <p>Check your connection, then try again. This retries opening this feature only; it does not reload the page or resubmit changes. If the application was updated, save any drafts before manually reopening it.</p>
      <button type="button" className="ui-primary" onClick={() => this.props.retry(this.state.url)}>Try opening again</button>
    </section>;
  }
}

/** Creates a stable module-level route/workspace boundary; only explicit retry recreates its lazy import. */
export function deferredFeature<Module, Key extends keyof Module>(load: () => Promise<Module>, exportName: Key, label: string) {
  const project = (module: unknown): { default: ComponentType } => {
    const feature = (module as Record<Key, ComponentType>)[exportName];
    if (typeof feature !== 'function') throw new Error('Feature code is unavailable.');
    return { default: feature };
  };
  return function DeferredFeature() {
    const { sessionVersion } = useAuth();
    const [attempt, setAttempt] = useState(0);
    const headingId = useId();
    const [Feature, setFeature] = useState(() => lazy(() => load().then(project)));
    return <FeatureFailure key={`${sessionVersion}:${attempt}`} label={label} headingId={headingId} retry={url => {
      // Browsers can cache a rejected module fetch. Explicit retry uses a fresh URL for that built asset,
      // while its relative imports retain their original URLs and therefore share the existing providers.
      setFeature(() => lazy(() => (url ? import(/* @vite-ignore */ `${url}?hrflow-retry=${attempt + 1}`) : load()).then(project)));
      setAttempt(value => value + 1);
    }}>
      <Suspense fallback={<section className="ui-panel min-h-48" role="status" aria-live="polite" aria-busy="true">Loading {label.toLowerCase()}…</section>}>
        <Feature />
      </Suspense>
    </FeatureFailure>;
  };
}
