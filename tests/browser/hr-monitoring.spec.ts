import fs from 'node:fs';
import { DatabaseSync } from 'node:sqlite';
import { test, expect, type Page } from '@playwright/test';
const fixture = () => JSON.parse(fs.readFileSync(process.env.HRFLOW_TEST_STATE!, 'utf8'));
const nav = (page: Page) => page.getByRole('navigation', { name: 'Primary', exact: true });
const path = (url: string) => new URL(url).pathname === '/api/v1/leave-requests/monitoring/pending';
const rows = (page: Page) => page.getByRole('region', { name: 'Organisation pending leave requests', exact: true }).locator('article');
const deferred = () => { let resolve!: () => void; const promise = new Promise<void>(done => { resolve = done; }); return { promise, resolve }; };
async function login(page: Page, actor = 'hr', inPlace = false) {
  const f = fixture(); if (!inPlace) await page.goto(f.client + '/login');
  await page.getByLabel('Email', { exact: true }).fill(f.employees[actor].email);
  await page.getByLabel('Password', { exact: true }).fill(f.password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click(); await expect(nav(page)).toBeVisible();
}
async function monitoring(page: Page) {
  const response = page.waitForResponse(r => path(r.url()));
  await nav(page).getByRole('link', { name: 'Pending leave monitoring', exact: true }).click(); await response;
  await expect(page.getByRole('button', { name: 'Refresh monitoring', exact: true })).toBeEnabled();
}
async function apply(page: Page) {
  const response = page.waitForResponse(r => path(r.url()));
  await page.getByRole('button', { name: 'Apply monitoring filters', exact: true }).click(); await response;
  await expect(page.getByRole('button', { name: 'Refresh monitoring', exact: true })).toBeEnabled();
}
function facts() {
  const db = new DatabaseSync(fixture().authRegression.database, { readOnly: true });
  try { db.exec('PRAGMA busy_timeout=3000'); return JSON.stringify(db.prepare('SELECT (SELECT COUNT(*) FROM LeaveRequests) AS requests, (SELECT COUNT(*) FROM AuditEntries) AS audits, (SELECT COUNT(*) FROM SupportingDocuments WHERE RequestId IS NOT NULL) AS bound').get()); }
  finally { db.close(); }
}

test('HTTP HR monitoring: filters, stable pages, dashboard totals, themes and final-page decision recovery', async ({ page }) => {
  const f = fixture(); const auth = await page.request.post(f.api + '/api/v1/auth/login', { data: { email: f.employees.unrelated.email, password: f.password } });
  expect(auth.status()).toBe(200); const headers = { Authorization: 'Bearer ' + (await auth.json()).accessToken };
  const type = (await (await page.request.get(f.api + '/api/v1/leave-types', { headers })).json()).find((t: { id: string }) => t.id === f.typeId);
  for (let i = 0; i < 26; i++) expect((await page.request.post(f.api + '/api/v1/leave-requests', { headers, data: { submissionKey: crypto.randomUUID(), leaveTypeId: type.id,
    startDate: '2052-02-29', endDate: '2052-03-01', expectedTypeVersion: type.version, expectedPolicyVersion: type.policyVersion } })).status()).toBe(200);
  await login(page);
  const opened = page.waitForResponse(r => path(r.url()));
  await page.getByRole('link', { name: 'Open all Pending monitoring', exact: true }).click(); await opened;
  await expect(page.getByRole('button', { name: 'Refresh monitoring', exact: true })).toBeEnabled();
  const before = facts();
  console.log(`monitoring-controls exactTypeLabel=${await page.getByLabel('Monitoring leave type', { exact: true }).count()} exactTypeRole=${await page.getByRole('combobox', { name: 'Monitoring leave type', exact: true }).count()} selects=${await page.getByRole('combobox').count()}`);
  expect(await page.getByRole('combobox', { name: 'Monitoring leave type', exact: true }).count()).toBe(1);
  await page.getByLabel('Employee name or email', { exact: true }).fill(f.employees.unrelated.email.toUpperCase());
  await page.getByRole('combobox', { name: 'Monitoring leave type', exact: true }).selectOption(type.id);
  await page.getByRole('combobox', { name: 'Monitoring department', exact: true }).selectOption({ label: 'Synthetic Engineering' });
  await page.getByLabel('Monitoring period start', { exact: true }).fill('2052-03-01');
  await page.getByLabel('Monitoring period end', { exact: true }).fill('2052-03-01'); await apply(page);
  await expect(rows(page)).toHaveCount(25); await expect(page.getByRole('status').filter({ hasText: '26 matching requests.' })).toBeVisible();
  await page.getByLabel('Monitoring period start', { exact: true }).fill('2053-01-01');
  await expect(page.getByLabel('Applied monitoring filters')).toContainText('2052-03-01');
  const next = page.waitForResponse(r => path(r.url()) && new URL(r.url()).searchParams.get('page') === '2');
  await page.getByRole('button', { name: 'Next monitoring page', exact: true }).focus(); await page.keyboard.press('Enter'); const last = await (await next).json();
  await expect(rows(page)).toHaveCount(1); const id = last.items[0].id;
  await expect(rows(page).getByRole('link', { name: 'View history', exact: true })).toHaveAttribute('href', '/leave-requests/' + id + '/history');
  expect(facts()).toBe(before); await expect(page.getByRole('button', { name: /Approve|Reject/ })).toHaveCount(0);
  await page.getByLabel('Monitoring period start', { exact: true }).fill('2052-03-01');
  if (process.env.HRFLOW_MONITORING_SCREENSHOTS === '1') await page.screenshot({ path: '../../docs/verification/screenshots/hr-monitoring/desktop.png', fullPage: true });
  await page.setViewportSize({ width: 390, height: 844 }); await page.emulateMedia({ colorScheme: 'dark', reducedMotion: 'reduce' });
  await page.evaluate(() => document.documentElement.classList.add('dark'));
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.getByLabel('Employee name or email', { exact: true }).focus(); await page.keyboard.press('Tab');
  await expect(page.getByRole('combobox', { name: 'Monitoring department', exact: true })).toBeFocused();
  if (process.env.HRFLOW_MONITORING_SCREENSHOTS === '1') await page.screenshot({ path: '../../docs/verification/screenshots/hr-monitoring/mobile-dark.png', fullPage: true });
  const manager = await page.request.post(f.api + '/api/v1/auth/login', { data: { email: f.employees.senior.email, password: f.password } });
  expect(manager.status()).toBe(200);
  expect((await page.request.post(f.api + '/api/v1/leave-requests/' + id + '/reject', { headers: { Authorization: 'Bearer ' + (await manager.json()).accessToken }, data: {} })).status()).toBe(204);
  await page.getByRole('button', { name: 'Refresh monitoring', exact: true }).click();
  await expect(rows(page)).toHaveCount(25); await expect(page.getByRole('status').filter({ hasText: '25 matching requests. Page 1 of 1.' })).toBeVisible();
  const reset = page.waitForResponse(r => path(r.url()) && !new URL(r.url()).searchParams.get('search'));
  await page.getByRole('button', { name: 'Reset monitoring filters', exact: true }).click(); await reset;
  await expect(page.getByLabel('Employee name or email', { exact: true })).toHaveValue('');
  await expect(page.getByLabel('Applied monitoring filters')).toContainText('Any employee');
});

test('controlled HR monitoring: superseded responses, retry, retained drafts and permission denial', async ({ page }) => {
  await login(page); await monitoring(page);
  const entered = deferred(), release = deferred(), finished = deferred(); let held = false;
  await page.route('**/api/v1/leave-requests/monitoring/pending?**', async route => {
    if (held) { await route.continue(); return; } held = true;
    const response = await route.fetch(); entered.resolve(); await release.promise;
    await route.fulfill({ response, json: { ...(await response.json()), totalCount: 9876 } }).catch(() => {}); finished.resolve();
  });
  try {
    await page.getByLabel('Employee name or email', { exact: true }).fill('superseded-filter');
    await page.getByRole('button', { name: 'Apply monitoring filters', exact: true }).click(); await entered.promise;
    await page.getByLabel('Employee name or email', { exact: true }).fill('current-filter'); await apply(page);
    await expect(page.getByRole('heading', { name: 'No pending leave requests', exact: true })).toBeVisible();
    release.resolve(); await finished.promise; await expect(page.getByRole('status').filter({ hasText: '9876' })).toHaveCount(0);
  } finally { release.resolve(); await page.unrouteAll({ behavior: 'wait' }); }
  await page.getByLabel('Employee name or email', { exact: true }).fill('Retained unapplied draft');
  await page.route('**/api/v1/leave-requests/monitoring/pending?**', route => route.fulfill({ status: 503, json: { title: 'Temporarily unavailable' } }));
  await page.getByRole('button', { name: 'Refresh monitoring', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Unable to load pending leave monitoring', exact: true })).toBeVisible(); await expect(rows(page)).toHaveCount(0);
  await expect(page.getByLabel('Applied monitoring filters')).toContainText('current-filter');
  await page.unrouteAll({ behavior: 'wait' }); await page.getByRole('button', { name: 'Try again', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'No pending leave requests', exact: true })).toBeVisible();
  await expect(page.getByLabel('Employee name or email', { exact: true })).toHaveValue('Retained unapplied draft');
  await page.route('**/api/v1/leave-requests/monitoring/pending?**', route => route.fulfill({ status: 403, json: { title: 'Forbidden' } }));
  await page.getByRole('button', { name: 'Refresh monitoring', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'HR monitoring access unavailable', exact: true })).toBeVisible();
  await expect(page.getByLabel('Employee name or email', { exact: true })).toHaveCount(0); await expect(rows(page)).toHaveCount(0);
});

for (const actor of ['hr', 'combined', 'employee']) test('controlled delayed HR monitoring cannot cross replacement login as ' + actor, async ({ page }) => {
  await login(page); await monitoring(page); const entered = deferred(), release = deferred(), finished = deferred(); let held = false;
  await page.route('**/api/v1/leave-requests/monitoring/pending?**', async route => {
    if (held) { await route.continue(); return; } held = true;
    const response = await route.fetch(); entered.resolve(); await release.promise;
    await route.fulfill({ response, json: { ...(await response.json()), totalCount: 9999 } }).catch(() => {}); finished.resolve();
  });
  try {
    await page.getByRole('button', { name: 'Refresh monitoring', exact: true }).click(); await entered.promise;
    await page.getByRole('button', { name: 'Sign out', exact: true }).click(); await login(page, actor, true);
    if (actor !== 'employee') await monitoring(page);
    else await expect(nav(page).getByRole('link', { name: 'Pending leave monitoring', exact: true })).toHaveCount(0);
    release.resolve(); await finished.promise;
    await expect(page.getByRole('status').filter({ hasText: '9999' })).toHaveCount(0);
    if (actor !== 'employee') await expect(page.getByLabel('Employee name or email', { exact: true })).toHaveValue('');
  } finally { release.resolve(); await page.unrouteAll({ behavior: 'wait' }); }
});
