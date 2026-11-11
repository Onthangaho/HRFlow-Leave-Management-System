import fs from 'node:fs';
import { test, expect, type Page } from '@playwright/test';
interface Fixture { password: string; api: string; client: string; typeId: string; employees: Record<string, { email: string; id: string }> }
const fixture = () => JSON.parse(fs.readFileSync(process.env.HRFLOW_TEST_STATE!, 'utf8')) as Fixture;
const nav = (page: Page) => page.getByRole('navigation', { name: 'Primary', exact: true });
const rows = (page: Page) => page.locator('article').filter({ has: page.getByRole('button', { name: 'Approve', exact: true }) });
const deferred = () => { let resolve!: () => void; const promise = new Promise<void>(r => resolve = r); return { promise, resolve }; };
const queueUrl = (url: string) => new URL(url).pathname === '/api/v1/leave-requests';
async function login(page: Page, actor = 'senior', inPlace = false) {
  const f = fixture(); if (!inPlace) await page.goto(f.client + '/login');
  await page.getByLabel('Email', { exact: true }).fill(f.employees[actor].email); await page.getByLabel('Password', { exact: true }).fill(f.password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click(); await expect(nav(page)).toBeVisible();
  await nav(page).getByRole('link', { name: 'Overview', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Current configured balances', exact: true })).toBeVisible();
}
async function queue(page: Page) {
  const response = page.waitForResponse(r => queueUrl(r.url()) && r.request().method() === 'GET');
  await nav(page).getByRole('link', { name: 'Approval queue', exact: true }).click(); await response;
  await expect(page.getByRole('button', { name: 'Refresh queue', exact: true })).toBeEnabled();
}
async function create(page: Page, count: number, date = '2042-02-28') {
  const f = fixture(); const auth = await page.request.post(f.api + '/api/v1/auth/login', { data: { email: f.employees.unrelated.email, password: f.password } });
  expect(auth.status()).toBe(200); const headers = { Authorization: 'Bearer ' + (await auth.json()).accessToken };
  const type = (await (await page.request.get(f.api + '/api/v1/leave-types', { headers })).json()).find((t: { id: string }) => t.id === f.typeId);
  const ids: string[] = [];
  for (let i = 0; i < count; i++) {
    const response = await page.request.post(f.api + '/api/v1/leave-requests', { headers, data: { submissionKey: crypto.randomUUID(), leaveTypeId: type.id, startDate: date, endDate: date, expectedTypeVersion: type.version, expectedPolicyVersion: type.policyVersion } });
    expect(response.status()).toBe(200); ids.push(await response.json());
  }
  return ids;
}
async function apply(page: Page) {
  const response = page.waitForResponse(r => queueUrl(r.url()) && r.request().method() === 'GET');
  await page.getByRole('button', { name: 'Apply queue filters', exact: true }).click(); await response;
  await expect(page.getByRole('button', { name: 'Refresh queue', exact: true })).toBeEnabled();
}

for (const action of ['approve', 'reject']) test(`HTTP Manager queue: applied filters, notes and final-page ${action} on desktop/mobile`, async ({ page }) => {
  const date = action === 'approve' ? '2041-02-28' : '2042-02-28';
  await create(page, 26, date); await login(page); await queue(page);
  await page.getByLabel('Queue period start', { exact: true }).fill(date); await page.getByLabel('Queue period end', { exact: true }).fill(date);
  await page.getByLabel('Queue leave type', { exact: true }).selectOption(fixture().typeId);
  await page.getByLabel('Pending direct report', { exact: true }).selectOption(fixture().employees.unrelated.id); await apply(page);
  await expect(rows(page)).toHaveCount(25); await expect(page.getByRole('status').filter({ hasText: '26 matching requests.' })).toBeVisible();
  await page.getByLabel('Queue period start', { exact: true }).fill('2042-03-01');
  await expect(page.getByLabel('Applied queue filters')).toContainText(date);
  const next = page.waitForResponse(r => queueUrl(r.url()) && new URL(r.url()).searchParams.get('page') === '2');
  await page.getByRole('button', { name: 'Next queue page', exact: true }).click(); await next; await expect(rows(page)).toHaveCount(1);
  const target = await rows(page).getByRole('link', { name: 'View history', exact: true }).getAttribute('href');
  await rows(page).getByRole('button', { name: action === 'approve' ? 'Approve' : 'Reject', exact: true }).click();
  await page.getByLabel('Decision note (optional)', { exact: true }).fill('Synthetic retained review.');
  await page.getByRole('button', { name: 'Refresh decision availability', exact: true }).click();
  await expect(page.getByLabel('Decision note (optional)', { exact: true })).toHaveValue('Synthetic retained review.');
  await page.getByRole('dialog').getByRole('button', { name: 'Confirm ' + action, exact: true }).click();
  await expect(page.getByRole('dialog')).toHaveCount(0); await expect(rows(page)).toHaveCount(25);
  await expect(page.getByRole('status').filter({ hasText: '25 matching requests. Page 1 of 1.' })).toBeVisible();
  await expect(page.locator(`a[href="${target}"]`)).toHaveCount(0);
  await page.setViewportSize({ width: 390, height: 844 }); await page.emulateMedia({ colorScheme: 'dark', reducedMotion: 'reduce' });
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.getByRole('button', { name: 'Reset queue filters', exact: true }).focus(); await expect(page.getByRole('button', { name: 'Reset queue filters', exact: true })).toBeFocused();
  await page.keyboard.press('Shift+Tab'); await expect(page.getByRole('button', { name: 'Apply queue filters', exact: true })).toBeFocused();
});

test('controlled queue refresh: original decision and note survive page replacement; authoritative stale decision feedback', async ({ page }) => {
  const [id] = await create(page, 1, '2043-02-28'); await login(page); await queue(page);
  await page.getByLabel('Queue period start', { exact: true }).fill('2043-02-28'); await page.getByLabel('Queue period end', { exact: true }).fill('2043-02-28'); await apply(page);
  const entered = deferred(), release = deferred(); let held = false;
  await page.route('**/api/v1/leave-requests?**', async route => {
    if (held || route.request().method() !== 'GET') { await route.continue(); return; }
    held = true; const response = await route.fetch(); entered.resolve(); await release.promise;
    await route.fulfill({ response, json: { ...(await response.json()), items: [], totalCount: 0, totalPages: 1 } }).catch(() => {});
  });
  try {
    await page.getByRole('button', { name: 'Refresh queue', exact: true }).click(); await entered.promise;
    await rows(page).getByRole('button', { name: 'Approve', exact: true }).click(); await page.getByLabel('Decision note (optional)', { exact: true }).fill('Retained despite refreshed rows');
    release.resolve(); await expect(rows(page)).toHaveCount(0);
    await expect(page.getByLabel('Decision note (optional)', { exact: true })).toHaveValue('Retained despite refreshed rows');
    const f = fixture(); const auth = await page.request.post(f.api + '/api/v1/auth/login', { data: { email: f.employees.senior.email, password: f.password } });
    const token = (await auth.json()).accessToken;
    expect((await page.request.post(f.api + `/api/v1/leave-requests/${id}/reject`, { headers: { Authorization: 'Bearer ' + token }, data: {} })).status()).toBe(204);
    await page.getByRole('dialog').getByRole('button', { name: 'Confirm approve', exact: true }).click();
    await expect(page.getByRole('alert')).toContainText('Only pending leave requests');
    await page.getByRole('button', { name: 'Refresh decision availability', exact: true }).click();
    await expect(page.getByRole('button', { name: 'Confirm approve', exact: true })).toBeDisabled();
    await expect(page.getByLabel('Decision note (optional)', { exact: true })).toHaveValue('Retained despite refreshed rows');
  } finally { release.resolve(); await page.unrouteAll({ behavior: 'wait' }); }
});

test('controlled decision 401: no refresh or mutation replay, real timeline unchanged', async ({ page }) => {
  const [id] = await create(page, 1, '2047-02-28'); await login(page); await queue(page);
  await page.getByLabel('Queue period start', { exact: true }).fill('2047-02-28'); await page.getByLabel('Queue period end', { exact: true }).fill('2047-02-28'); await apply(page);
  let decisions = 0, refreshes = 0;
  page.on('request', request => { if (request.url().endsWith('/auth/refresh')) refreshes++; });
  await page.route(`**/api/v1/leave-requests/${id}/approve`, route => { decisions++; return route.fulfill({ status: 401, contentType: 'application/problem+json', body: JSON.stringify({ title: 'Unauthorized' }) }); });
  await rows(page).getByRole('button', { name: 'Approve', exact: true }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Confirm approve', exact: true }).click();
  await expect(page).toHaveURL(/\/login$/); expect(decisions).toBe(1); expect(refreshes).toBe(0);
  const f = fixture(); const auth = await page.request.post(f.api + '/api/v1/auth/login', { data: { email: f.employees.unrelated.email, password: f.password } });
  const timeline = await page.request.get(f.api + `/api/v1/leave-requests/${id}/timeline`, { headers: { Authorization: 'Bearer ' + (await auth.json()).accessToken } });
  expect(timeline.status()).toBe(200); const persisted = await timeline.json(); expect(persisted.status).toBe('Pending');
  expect(persisted.events).toHaveLength(1); expect(persisted.events[0].action).toBe('Submit');
});

test('controlled queue filters: out-of-order data, error/retry and unapplied draft retention', async ({ page }) => {
  await login(page); await queue(page); const entered = deferred(), release = deferred(), finished = deferred();
  await page.route('**/api/v1/leave-requests?**', async route => {
    if (new URL(route.request().url()).searchParams.get('start') !== '2044-01-01') { await route.continue(); return; }
    const response = await route.fetch(); entered.resolve(); await release.promise;
    await route.fulfill({ response, json: { ...(await response.json()), totalCount: 9876 } }).catch(() => {}); finished.resolve();
  });
  try {
    await page.getByLabel('Queue period start', { exact: true }).fill('2044-01-01');
    await page.getByRole('button', { name: 'Apply queue filters', exact: true }).click(); await entered.promise;
    await page.getByLabel('Queue period start', { exact: true }).fill('2045-01-01'); await apply(page);
    await expect(page.getByLabel('Applied queue filters')).toContainText('2045-01-01');
    release.resolve(); await finished.promise; await expect(page.getByRole('status').filter({ hasText: '9876' })).toHaveCount(0);
  } finally { release.resolve(); await page.unrouteAll({ behavior: 'wait' }); }
  await page.getByLabel('Queue period start', { exact: true }).fill('2046-01-01');
  await page.route('**/api/v1/leave-requests?**', route => route.fulfill({ status: 503, contentType: 'application/problem+json', body: JSON.stringify({ title: 'Temporarily unavailable' }) }));
  await page.getByRole('button', { name: 'Refresh queue', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Approval queue unavailable', exact: true })).toBeVisible(); await expect(rows(page)).toHaveCount(0);
  await expect(page.getByLabel('Applied queue filters')).toContainText('2045-01-01');
  await page.unrouteAll({ behavior: 'wait' }); await page.getByRole('button', { name: 'Try again', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Refresh queue', exact: true })).toBeEnabled();
  await expect(page.getByLabel('Queue period start', { exact: true })).toHaveValue('2046-01-01');
});

for (const replacement of ['senior', 'combined']) test(`controlled delayed queue cannot cross replacement login as ${replacement}`, async ({ page }) => {
  await login(page); await queue(page); const entered = deferred(), release = deferred(), finished = deferred(); let held = false;
  await page.route('**/api/v1/leave-requests?**', async route => {
    if (held || route.request().method() !== 'GET') { await route.continue(); return; }
    held = true; const response = await route.fetch(); entered.resolve(); await release.promise;
    await route.fulfill({ response, json: { ...(await response.json()), totalCount: 9999 } }).catch(() => {}); finished.resolve();
  });
  try {
    await page.getByRole('button', { name: 'Refresh queue', exact: true }).click(); await entered.promise;
    await page.getByRole('button', { name: 'Sign out', exact: true }).click(); await login(page, replacement, true); await queue(page);
    release.resolve(); await finished.promise; await expect(page.getByRole('status').filter({ hasText: '9999' })).toHaveCount(0);
  } finally { release.resolve(); await page.unrouteAll({ behavior: 'wait' }); }
});
