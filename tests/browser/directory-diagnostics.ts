import type { Page, TestInfo } from '@playwright/test';

const steps = ['initial-page', 'next-page', 'draft-search', 'applied-search', 'edit-draft',
  'refetch-draft', 'attention-link', 'desktop-view', 'mobile-width', 'mobile-focus', 'mobile-results', 'refetch-barrier'] as const;
const maximumDiagnosticCount = 1_000_000;

/** Fixed labels and numeric DOM state locate failures without exposing Playwright errors or page contents. */
export async function directoryStep(page: Page, info: TestInfo, label: typeof steps[number], action: () => Promise<void>) {
  let failed = false;
  try { await action(); }
  catch (error) { failed = true; throw error; }
  finally {
    const state = await page.evaluate(() => ({
      rows: document.querySelectorAll('tbody tr').length,
      scrollWidth: document.documentElement.scrollWidth,
      innerWidth: window.innerWidth,
      loading: document.querySelector('[aria-label="Directory results"]')?.getAttribute('aria-busy') === 'true',
    })).catch(() => null);
    console.log('directory-diagnostic ' + JSON.stringify({ step: label, failed,
      source: { file: 'directory.spec.ts', line: info.line }, viewport: page.viewportSize(), state }));
  }
}

/** Reporter output is rebuilt from an allowlist, so arbitrary worker stdout and unexpected fields stay private. */
export function safeDirectoryDiagnostics(output: string | Buffer): string[] {
  return output.toString().split('\n').flatMap(line => {
    if (!line.startsWith('directory-diagnostic ')) return [];
    try {
      const value = JSON.parse(line.slice('directory-diagnostic '.length));
      const numeric = (n: unknown): n is number => Number.isSafeInteger(n) && (n as number) >= 0 && (n as number) <= maximumDiagnosticCount;
      if (!steps.includes(value.step) || typeof value.failed !== 'boolean' || value.source?.file !== 'directory.spec.ts'
        || !numeric(value.source.line) || !numeric(value.viewport?.width) || !numeric(value.viewport?.height)) return [];
      const state = value.state;
      if (state !== null && (!numeric(state?.rows) || !numeric(state?.scrollWidth) || !numeric(state?.innerWidth) || typeof state?.loading !== 'boolean')) return [];
      return ['directory-diagnostic ' + JSON.stringify({ step: value.step, failed: value.failed,
        source: { file: 'directory.spec.ts', line: value.source.line },
        viewport: { width: value.viewport.width, height: value.viewport.height },
        state: state === null ? null : { rows: state.rows, scrollWidth: state.scrollWidth, innerWidth: state.innerWidth, loading: state.loading } })];
    } catch { return []; }
  });
}
