import fs from 'node:fs';
import { test, expect, type Page } from '@playwright/test';
interface Fixture { password: string; api: string; client: string; typeId: string; employees: Record<string, { email: string; id: string }> }
const state = () => JSON.parse(fs.readFileSync(process.env.HRFLOW_TEST_STATE!, 'utf8')) as Fixture;
async function login(page: Page, actor = 'employee') {
  const f = state(); await page.goto(f.client + '/login');
  await page.getByLabel('Email', { exact: true }).fill(f.employees[actor].email);
  await page.getByLabel('Password', { exact: true }).fill(f.password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(page.getByRole('navigation', { name: 'Primary', exact: true }).getByRole('link', { name: 'Overview', exact: true })).toBeVisible();
  await page.getByRole('navigation', { name: 'Primary', exact: true }).getByRole('link', { name: 'Overview', exact: true }).click();
  if (actor === 'hr') await expect(page.getByText('HR snapshot loaded.', { exact: true })).toBeVisible();
  else await expect(page.getByRole('heading', { name: 'Current configured balances', exact: true })).toBeVisible();
}
async function history(page: Page) {
  await page.getByRole('navigation', { name: 'Primary', exact: true }).getByRole('link', { name: 'My leave and balances', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Refresh history', exact: true })).toBeEnabled();
}
function appliedResponse(page: Page, action: () => Promise<unknown>) {
  const response = page.waitForResponse(r => r.url().includes('/api/v1/leave-requests/history?') && r.request().method() === 'GET');
  return action().then(() => response);
}
const cards = (page: Page) => page.locator('article').filter({ has: page.getByRole('link', { name: 'View history', exact: true }) });
const deferred = () => { let resolve!: () => void; const promise = new Promise<void>(r => resolve = r); return { promise, resolve }; };

test('HTTP history: applied filters, pages, retained cancellation and empty-last-page recovery on desktop/mobile', async ({ page }) => {
  const f = state(); const auth = await page.request.post(f.api + '/api/v1/auth/login', { data: { email: f.employees.unrelated.email, password: f.password } });
  expect(auth.status()).toBe(200); const headers = { Authorization: 'Bearer ' + (await auth.json()).accessToken };
  const types = await (await page.request.get(f.api + '/api/v1/leave-types', { headers })).json(); const type = types.find((entry: { id: string }) => entry.id === f.typeId); expect(type).toBeTruthy();
  for (let i = 0; i < 26; i++) {
    const response = await page.request.post(f.api + '/api/v1/leave-requests', { headers, data: {
      submissionKey: crypto.randomUUID(), leaveTypeId: type.id, startDate: '2040-02-28', endDate: '2040-03-01',
      expectedTypeVersion: type.version, expectedPolicyVersion: type.policyVersion,
    } }); expect(response.status()).toBe(200);
  }
  await login(page, 'unrelated'); await history(page);
  await page.getByLabel('Request status', { exact: true }).selectOption('Pending');
  await page.getByLabel('History leave type', { exact: true }).selectOption(type.id);
  await page.getByLabel('Period start', { exact: true }).fill('2040-02-29');
  await page.getByLabel('Period end', { exact: true }).fill('2040-02-29');
  await appliedResponse(page, () => page.getByRole('button', { name: 'Apply history filters', exact: true }).click());
  await expect(page.getByRole('status').filter({ hasText: '26 matching requests.' })).toBeVisible();
  await expect(cards(page)).toHaveCount(25);
  await page.getByLabel('Request status', { exact: true }).selectOption('Cancelled');
  await expect(page.getByLabel('Applied history filters')).toContainText('Pending'); await expect(cards(page)).toHaveCount(25);
  await appliedResponse(page, () => page.getByRole('button', { name: 'Next history page', exact: true }).click());
  await expect(cards(page)).toHaveCount(1); await expect(page.getByRole('status').filter({ hasText: 'Page 2 of 2.' })).toBeVisible();
  const target = await cards(page).getByRole('link', { name: 'View history', exact: true }).getAttribute('href');
  await cards(page).getByRole('button', { name: 'Cancel request', exact: true }).click();
  await appliedResponse(page, () => page.getByRole('button', { name: 'Refresh history', exact: true }).click());
  await expect(page.getByRole('button', { name: 'Confirm cancellation', exact: true })).toBeVisible();
  await page.route('**/api/v1/leave-requests/history?**', route => route.fulfill({ status: 503, contentType: 'application/problem+json', body: JSON.stringify({ title: 'Temporarily unavailable' }) }));
  await page.getByRole('button', { name: 'Refresh history', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Retry history', exact: true })).toBeVisible();
  await page.unrouteAll({ behavior: 'wait' });
  await page.getByRole('button', { name: 'Retry history', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Confirm cancellation', exact: true })).toBeVisible();
  await page.setViewportSize({ width: 390, height: 844 });
  await page.emulateMedia({ colorScheme: 'dark', reducedMotion: 'reduce' });
  const response = page.waitForResponse(r => r.url().endsWith('/cancel') && r.request().method() === 'POST');
  await page.getByRole('button', { name: 'Confirm cancellation', exact: true }).click(); expect((await response).status()).toBe(204);
  await expect(page.getByRole('status').filter({ hasText: '25 matching requests. Page 1 of 1.' })).toBeVisible();
  await expect(cards(page)).toHaveCount(25);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  const id = target!.split('/')[2];
  const timeline = await (await page.request.get(f.api + '/api/v1/leave-requests/' + id + '/timeline', { headers })).json();
  expect(timeline.events.filter((e: { action: string }) => e.action === 'Submit')).toHaveLength(1);
  expect(timeline.events.filter((e: { action: string }) => e.action === 'Cancel')).toHaveLength(1);
  await page.getByRole('button', { name: 'Reset history filters', exact: true }).focus();
  await page.keyboard.press('Enter');
  await expect(page.getByLabel('Request status', { exact: true })).toHaveValue('');
  await expect(page.getByLabel('Applied history filters')).toContainText('All statuses');
});

test('controlled history transport: superseded filters, error retry and page loading never relabel previous rows', async ({ page }) => {
  await login(page); await history(page);
  const entered = deferred(), release = deferred(), finished = deferred();
  let held = false;
  await page.route('**/api/v1/leave-requests/history?**', async route => {
    if (new URL(route.request().url()).searchParams.get('status') !== 'Rejected' || held) { await route.continue(); return; }
    held = true; entered.resolve();
    try { const response = await route.fetch({ timeout: 15_000 }); await release.promise; await route.fulfill({ response }); } catch { /* Aborted obsolete request. */ } finally { finished.resolve(); }
  });
  try {
    await page.getByLabel('Request status', { exact: true }).selectOption('Rejected');
    await page.getByRole('button', { name: 'Apply history filters', exact: true }).click(); await entered.promise;
    await expect(cards(page)).toHaveCount(0);
    await page.getByLabel('Request status', { exact: true }).selectOption('Cancelled');
    await appliedResponse(page, () => page.getByRole('button', { name: 'Apply history filters', exact: true }).click());
    await expect(page.getByRole('button', { name: 'Refresh history', exact: true })).toBeEnabled();
    release.resolve(); await finished.promise;
    await expect(page.getByLabel('Applied history filters')).toContainText('Cancelled');
    await expect(cards(page).getByText('Rejected', { exact: true })).toHaveCount(0);
  } finally { release.resolve(); if (held) await finished.promise; await page.unrouteAll({ behavior: 'wait' }); }
  await page.route('**/api/v1/leave-requests/history?**', route => route.fulfill({ status: 503, contentType: 'application/problem+json', body: JSON.stringify({ title: 'Temporarily unavailable' }) }));
  await page.getByRole('button', { name: 'Refresh history', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Retry history', exact: true })).toBeVisible(); await expect(cards(page)).toHaveCount(0);
  await page.unrouteAll({ behavior: 'wait' });
  await page.getByRole('button', { name: 'Retry history', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Retry history', exact: true })).toHaveCount(0);
  await expect(page.getByLabel('Applied history filters')).toContainText('Cancelled');
});

for (const actor of ['employee', 'hr']) test(`controlled history response: cannot cross replacement login as ${actor}`, async ({ page }) => {
  const f = state(); await login(page); const entered = deferred(), release = deferred(), finished = deferred();
  let held = false;
  await page.route('**/api/v1/leave-requests/history?**', async route => {
    if (held) { await route.continue(); return; } held = true; entered.resolve();
    try { const response = await route.fetch({ timeout: 15_000 }); const old = await response.json(); await release.promise; await route.fulfill({ response, json: { ...old, totalCount: 9999 } }); } catch { /* Old session cancellation is expected. */ } finally { finished.resolve(); }
  });
  try {
    await page.getByRole('navigation', { name: 'Primary', exact: true }).getByRole('link', { name: 'My leave and balances', exact: true }).click(); await entered.promise;
    await page.getByRole('button', { name: 'Sign out', exact: true }).click(); await expect(page).toHaveURL(/\/login$/);
    await login(page, actor);
    if (actor === 'employee') { await history(page); await expect(page.getByRole('button', { name: 'Refresh history', exact: true })).toBeEnabled(); }
    else { await expect(page.getByRole('navigation', { name: 'Primary', exact: true }).getByRole('link', { name: 'My leave and balances', exact: true })).toHaveCount(0); }
    release.resolve(); await finished.promise;
    await expect(page.getByText('9999 matching requests.', { exact: false })).toHaveCount(0);
    await expect(page.getByRole('heading', { name: 'Overview', exact: true })).toHaveCount(actor === 'hr' ? 1 : 0);
  } finally { release.resolve(); if (held) await finished.promise; await page.unrouteAll({ behavior: 'wait' }); }
});


test('HTTP history: selector labels identify exactly one accessible control', async ({ page }) => {
  await login(page); await history(page);
  const exact = await page.getByLabel('Request status', { exact: true }).count();
  const selects = await page.locator('select').count();
  console.log(`history-controls exact=${exact} selects=${selects}`);
  expect(exact).toBe(1);
  await expect(page.getByLabel('History leave type', { exact: true })).toHaveCount(1);
  await expect(page.getByLabel('Requests per page', { exact: true })).toHaveCount(1);
});
