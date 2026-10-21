import fs from 'node:fs';
import { test, expect, type Page } from '@playwright/test';

const fixture = () => JSON.parse(fs.readFileSync(process.env.HRFLOW_TEST_STATE!, 'utf8'));
const workspace = (page: Page) => page.getByRole('region', { name: 'HR workspace', exact: true });
const attention = (page: Page) => page.getByRole('region', { name: 'All-date organisation attention', exact: true });
const isWorkspace = (url: string) => new URL(url).pathname.endsWith('/reports/hr-workspace');
const nav = (page: Page) => page.getByRole('navigation', { name: 'Primary', exact: true });
async function login(page: Page, actor = 'hr', inPlace = false) {
  const data = fixture();
  if (!inPlace) await page.goto(data.client + '/login');
  await page.getByLabel('Email', { exact: true }).fill(data.employees[actor].email);
  await page.getByLabel('Password', { exact: true }).fill(data.password);
  const response = actor === 'hr' ? page.waitForResponse(r => isWorkspace(r.url()) && r.status() === 200) : null;
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(nav(page)).toBeVisible();
  if (response) {
    const data = await (await response).json();
    await expect(page.getByText('HR snapshot loaded.', { exact: true })).toBeVisible();
    return data;
  }
  await expect(page.getByRole('region', { name: 'Personal leave dashboard', exact: true })).toBeVisible();
}
function barrier() { let resolve!: () => void; const promise = new Promise<void>(r => { resolve = r; }); return { promise, resolve }; }

test('HTTP HR workspace: applied filters, unchanged operational counts, attention destination and mobile dark layout', async ({ page }) => {
  const data = await login(page);
  const applied = page.getByRole('region', { name: 'Applied HR period results', exact: true });
  const originalHeading = await applied.getByRole('heading', { level: 3 }).textContent();
  await page.getByLabel('Period start', { exact: true }).fill('2024-02-01');
  await page.getByLabel('Period end', { exact: true }).fill('2024-02-29');
  await page.getByLabel('Report department', { exact: true }).selectOption({ label: 'Synthetic Engineering' });
  await expect(applied.getByRole('heading', { level: 3 })).toHaveText(originalHeading!);
  const response = page.waitForResponse(r => isWorkspace(r.url()) && new URL(r.url()).searchParams.get('start') === '2024-02-01');
  await page.getByRole('button', { name: 'Apply period', exact: true }).click();
  expect((await response).status()).toBe(200);
  await expect(page.getByText('HR snapshot loaded.', { exact: true })).toBeVisible();
  await expect(applied.getByRole('heading', { level: 3 })).toContainText('2024');
  await expect(attention(page).locator('article').filter({ has: page.getByRole('heading', { name: 'Active employees', exact: true }) }).locator('p')).toHaveText(String(data.activeEmployees));
  await page.setViewportSize({ width: 390, height: 844 });
  await page.emulateMedia({ colorScheme: 'dark', reducedMotion: 'reduce' });
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.getByRole('button', { name: 'Apply period', exact: true }).focus(); await page.keyboard.press('Tab');
  await expect(page.getByRole('button', { name: 'Reset to current month', exact: true })).toBeFocused();
  const directory = page.waitForResponse(r => new URL(r.url()).pathname.endsWith('/employees') && new URL(r.url()).searchParams.get('attention') === 'schedule');
  await page.locator('a[href="/admin/employees?attention=schedule"]').click();
  expect((await directory).status()).toBe(200);
  await expect(page.getByLabel('Administrative attention', { exact: true })).toHaveValue('schedule');
  await expect(page.getByRole('region', { name: 'Directory results', exact: true })).toHaveAttribute('aria-busy', 'false');
  await expect(page.getByRole('status').filter({ hasText: new RegExp('^' + data.attention.missingCurrentSchedules + ' matching employees;') })).toBeVisible();
});

test('controlled HR transport: retry retains drafts and permission denial hides cached aggregates', async ({ page }) => {
  await login(page);
  const applied = page.getByRole('region', { name: 'Applied HR period results', exact: true });
  const heading = await applied.getByRole('heading', { level: 3 }).textContent();
  await page.getByLabel('Period start', { exact: true }).fill('2024-01-01');
  let failure = 503;
  await page.route('**/reports/hr-workspace?**', route => failure ? route.fulfill({ status: failure, contentType: 'application/problem+json', body: JSON.stringify({ title: 'Synthetic request unavailable', status: failure }) }) : route.continue());
  await page.getByRole('button', { name: 'Refresh HR workspace', exact: true }).click();
  await expect(page.getByText('Refresh failed. Previously loaded results are out of date.', { exact: true })).toBeVisible();
  await expect(page.getByLabel('Period start', { exact: true })).toHaveValue('2024-01-01');
  await expect(applied.getByRole('heading', { level: 3 })).toHaveText(heading!);
  failure = 0;
  const recovered = page.waitForResponse(r => isWorkspace(r.url()) && r.status() === 200);
  await page.getByRole('button', { name: 'Retry HR workspace', exact: true }).click(); await recovered;
  await expect(page.getByText('HR snapshot loaded.', { exact: true })).toBeVisible();
  failure = 403;
  await page.getByRole('button', { name: 'Refresh HR workspace', exact: true }).click();
  await expect(page.getByText('HR access is unavailable; protected results are hidden.', { exact: true })).toBeVisible();
  await expect(attention(page)).toHaveCount(0); await expect(applied).toHaveCount(0);
});

for (const actor of ['employee', 'hr']) test('controlled HR transport: delayed aggregate cannot cross replacement login as ' + actor, async ({ page }) => {
  await login(page);
  const entered = barrier(), release = barrier(), handled = barrier(); let hold = true;
  await page.route('**/reports/hr-workspace?**', async route => {
    if (!hold) return route.continue();
    hold = false;
    const response = await route.fetch(); const data = await response.json(); entered.resolve();
    await release.promise;
    try { await route.fulfill({ response, json: { ...data, activeEmployees: 989898 } }).catch(error => { if (!route.request().failure()) throw error; }); }
    finally { handled.resolve(); }
  });
  try {
    await page.getByRole('button', { name: 'Refresh HR workspace', exact: true }).click(); await entered.promise;
    await page.getByRole('button', { name: 'Sign out', exact: true }).click();
    await expect(page).toHaveURL(/\/login$/); await login(page, actor, true);
    release.resolve(); await handled.promise;
    // Allow browser delivery/render turns after the controlled response settles; no timer sleep.
    await page.evaluate(() => new Promise<void>(resolve => requestAnimationFrame(() => requestAnimationFrame(() => resolve()))));
    if (actor === 'hr') await expect(workspace(page)).not.toContainText('989898');
    else await expect(workspace(page)).toHaveCount(0);
  } finally { release.resolve(); }
});
