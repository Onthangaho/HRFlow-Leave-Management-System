import { expect, type Page } from '@playwright/test';

/** Waits for the applied directory request and its rendered idle state, rather than accepting cached draft values as completion. */
export async function readDirectory(page: Page, query: Readonly<Record<string, string>>, action: () => Promise<unknown>) {
  const [response] = await Promise.all([
    page.waitForResponse(response => {
      const url = new URL(response.url());
      return response.request().method() === 'GET' && url.pathname === '/api/v1/employees'
        && Object.entries(query).every(([key, value]) => url.searchParams.get(key) === value);
    }),
    action(),
  ]);
  expect(response.status()).toBe(200);
  await expect(page.getByRole('region', { name: 'Directory results', exact: true })).toHaveAttribute('aria-busy', 'false');
}
