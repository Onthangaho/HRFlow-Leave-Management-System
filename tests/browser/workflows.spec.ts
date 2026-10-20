import fs from 'node:fs';
import { test, expect, type Page, type Route } from '@playwright/test';

interface Fixture { password: string; api: string; client: string; typeId: string; employees: Record<string, { id: string; email: string; name: string }> }
const state = () => JSON.parse(fs.readFileSync(process.env.HRFLOW_TEST_STATE!, 'utf8')) as Fixture;
const navigation = (page: Page) => page.getByRole('navigation', { name: 'Primary', exact: true });
async function login(page: Page, actor: string) {
  const fixture = state(); await page.goto(fixture.client + '/login');
  await page.getByLabel('Email', { exact: true }).fill(fixture.employees[actor].email);
  await page.getByLabel('Password', { exact: true }).fill(fixture.password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(navigation(page).getByRole('link', { name: 'Overview', exact: true })).toBeVisible();
}
async function logout(page: Page) { await page.getByRole('button', { name: 'Sign out', exact: true }).click(); await expect(page).toHaveURL(/\/login$/); }
async function loginInPlace(page: Page, actor: string) {
  const fixture = state(); await page.getByLabel('Email', { exact: true }).fill(fixture.employees[actor].email);
  await page.getByLabel('Password', { exact: true }).fill(fixture.password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(navigation(page).getByRole('link', { name: 'Overview', exact: true })).toBeVisible();
  await navigation(page).getByRole('link', { name: 'Overview', exact: true }).click();
}
async function apiLogin(page: Page, actor: string) {
  const fixture = state(); const response = await page.request.post(fixture.api + '/api/v1/auth/login', { data: { email: fixture.employees[actor].email, password: fixture.password } });
  expect(response.status()).toBe(200); return (await response.json()).accessToken as string;
}
function deferred() { let resolve!: () => void; const promise = new Promise<void>(r => resolve = r); return { promise, resolve }; }
async function requestForm(page: Page) {
  const fixture = state(); await navigation(page).getByRole('link', { name: 'Request leave', exact: true }).click();
  await page.getByLabel('Leave type', { exact: true }).selectOption(fixture.typeId);
  const date = new Date(); date.setUTCDate(date.getUTCDate() + 10); const value = date.toISOString().slice(0, 10);
  await page.getByLabel('Start date', { exact: true }).fill(value); await page.getByLabel('End date', { exact: true }).fill(value);
}

test('HTTP integration: Employee form to assigned Manager decision to updated owner history', async ({ page }) => {
  const fixture = state(); await login(page, 'employee'); await requestForm(page);
  const submitted = page.waitForResponse(r => r.url() === fixture.api + '/api/v1/leave-requests' && r.request().method() === 'POST');
  await page.getByRole('button', { name: 'Submit request', exact: true }).click(); const response = await submitted;
  expect(response.status()).toBe(200); const requestId = await response.json();
  await expect(page.getByRole('heading', { name: 'Request submitted', exact: true })).toBeVisible();
  await logout(page); await loginInPlace(page, 'manager'); await navigation(page).getByRole('link', { name: 'Approval queue', exact: true }).click();
  const row = page.locator('article').filter({ has: page.locator(`a[href="/leave-requests/${requestId}/history"]`) });
  await row.getByRole('button', { name: 'Approve', exact: true }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Confirm approve', exact: true }).click();
  await expect(row).toHaveCount(0); await logout(page); await loginInPlace(page, 'employee');
  await navigation(page).getByRole('link', { name: 'My leave and balances', exact: true }).click();
  const history = page.locator('article').filter({ has: page.locator(`a[href="/leave-requests/${requestId}/history"]`) });
  await expect(history.getByText('Approved', { exact: true })).toBeVisible();
  const hrToken = await apiLogin(page, 'hr');
  const timeline = await page.request.get(fixture.api + `/api/v1/leave-requests/${requestId}/timeline`, { headers: { Authorization: 'Bearer ' + hrToken } });
  expect(timeline.status()).toBe(200);
  const data = await timeline.json(); expect(data.status).toBe('Approved'); expect(data.events.filter((e: { action: string }) => e.action === 'Submit')).toHaveLength(1); expect(data.events.filter((e: { action: string }) => e.action === 'Approve')).toHaveLength(1); expect(data.events.find((e: { action: string }) => e.action === 'Approve').actorId).toBe(fixture.employees.manager.id);
});

test('HTTP integration: HR monitoring is read-only and retains role separation', async ({ page }) => {
  const fixture = state(), token = await apiLogin(page, 'employee'), headers = { Authorization: 'Bearer ' + token };
  const selector = await (await page.request.get(fixture.api + '/api/v1/leave-types', { headers })).json();
  const type = selector.find((entry: { id: string }) => entry.id === fixture.typeId);
  const date = new Date(); date.setUTCDate(date.getUTCDate() + 30); const value = date.toISOString().slice(0, 10) + 'T00:00:00';
  const response = await page.request.post(fixture.api + '/api/v1/leave-requests', { headers, data: { leaveTypeId: type.id,
    startDate: value, endDate: value, expectedTypeVersion: type.version, expectedPolicyVersion: type.policyVersion } });
  expect(response.status()).toBe(200); const requestId = await response.json();
  await login(page, 'hr'); await navigation(page).getByRole('link', { name: 'Pending leave monitoring', exact: true }).click();
  await expect(page.locator(`a[href="/leave-requests/${requestId}/history"]`)).toBeVisible();
  await expect(page.getByRole('button', { name: /Approve|Reject/ })).toHaveCount(0);
  await expect(navigation(page).getByRole('link', { name: 'Request leave', exact: true })).toHaveCount(0);
  await expect(navigation(page).getByRole('link', { name: 'Approval queue', exact: true })).toHaveCount(0);
});

for (const actor of ['employee', 'hr']) test(`controlled transport: old data cannot cross logout/relogin as ${actor}`, async ({ page }) => {
  const fixture = state(); await login(page, 'employee'); const entered = deferred(), release = deferred(), finished = deferred();
  let captured = false;
  await page.route('**/leave-requests/dashboard', async route => {
    if (captured) { await route.continue(); return; } captured = true;
    const response = await route.fetch(); entered.resolve(); await release.promise;
    await route.fulfill({ response, json: { ...(await response.json()), pendingCount: 987654 } }).catch(() => {}); finished.resolve();
  });
  await page.getByRole('button', { name: /Refresh personal|Refresh dashboard/ }).click(); await entered.promise;
  await logout(page); await loginInPlace(page, actor); release.resolve(); await finished.promise; await page.unroute('**/leave-requests/dashboard');
  await expect(page.getByText(/987654/)).toHaveCount(0);
  if (actor === 'hr') await expect(page.getByRole('region', { name: 'Personal leave dashboard', exact: true })).toHaveCount(0);
  expect(fixture.client).toMatch(/^http:\/\/127\.0\.0\.1:/);
});

for (const failed of [false, true]) test(`controlled transport: stale refresh ${failed ? 'failure' : 'success'} cannot clear/replace a newer same-account session`, async ({ page }) => {
  const fixture = state(); await login(page, 'employee'); const entered = deferred(), release = deferred(), finished = deferred();
  let refreshes = 0;
  await page.route('**/auth/refresh', async route => {
    refreshes++; const response = failed ? undefined : await route.fetch(); entered.resolve(); await release.promise;
    await (failed ? route.fulfill({ status: 401, json: {} }) : route.fulfill({ response })).catch(() => {}); finished.resolve();
  });
  let intercepted = false;
  await page.route('**/leave-requests/dashboard', async route => {
    if (!intercepted) { intercepted = true; await route.fulfill({ status: 401, json: {} }); } else await route.continue();
  });
  await page.getByRole('button', { name: /Refresh personal|Refresh dashboard/ }).click(); await entered.promise;
  await logout(page); const signedIn = page.waitForResponse(r => r.url() === fixture.api + '/api/v1/auth/login' && r.status() === 200); await loginInPlace(page, 'employee'); const newAccessToken = (await (await signedIn).json()).accessToken; release.resolve(); await finished.promise;
  await page.unroute('**/auth/refresh'); await page.unroute('**/leave-requests/dashboard');
  await expect(navigation(page).getByRole('link', { name: 'Overview', exact: true })).toBeVisible(); expect(refreshes).toBe(1);
  const fresh = page.waitForResponse(r => r.url() === fixture.api + '/api/v1/leave-requests/dashboard' && r.status() === 200);
  await page.getByRole('button', { name: /Refresh personal|Refresh dashboard/ }).click(); const observed = await fresh; expect(observed.request().headers().authorization === 'Bearer ' + newAccessToken).toBe(true);
});

test('controlled transport: submission 401 is not replayed and ends the initiating session', async ({ page }) => {
  const fixture = state(); await login(page, 'employee'); await requestForm(page); let mutations = 0, refreshes = 0;
  await page.route('**/auth/refresh', route => { refreshes++; return route.fulfill({ status: 401, json: {} }); });
  await page.route(fixture.api + '/api/v1/leave-requests', route => { mutations++; return route.fulfill({ status: 401, json: {} }); });
  await page.getByRole('button', { name: 'Submit request', exact: true }).click(); await expect(page).toHaveURL(/\/login$/);
  expect(mutations).toBe(1); expect(refreshes).toBe(0);
});

test('controlled transport: delayed non-replayable mutation failure cannot end a replacement session', async ({ page }) => {
  const fixture = state(); await login(page, 'employee'); await requestForm(page);
  const entered = deferred(), release = deferred(), finished = deferred(); let mutations = 0, refreshes = 0;
  await page.route('**/auth/refresh', route => { refreshes++; return route.fulfill({ status: 401, json: {} }); });
  await page.route(fixture.api + '/api/v1/leave-requests', async route => {
    mutations++; entered.resolve(); await release.promise; await route.fulfill({ status: 401, json: {} }).catch(() => {}); finished.resolve();
  });
  await page.getByRole('button', { name: 'Submit request', exact: true }).click(); await entered.promise;
  await logout(page); await loginInPlace(page, 'hr'); release.resolve(); await finished.promise;
  await expect(page.getByRole('region', { name: 'HR workspace', exact: true })).toBeVisible();
  expect(mutations).toBe(1); expect(refreshes).toBe(0);
});

test('HTTP integration: stale employee form retains draft until explicit reload', async ({ page }) => {
  const fixture = state(); await login(page, 'hr'); await navigation(page).getByRole('link', { name: 'Employee management', exact: true }).click();
  const owner = fixture.employees.unrelated;
  await page.getByLabel('Search employees', { exact: true }).fill(owner.email);
  await page.getByRole('button', { name: 'Apply filters', exact: true }).click();
  await page.getByRole('button', { name: 'Edit ' + owner.name, exact: true }).click(); await page.getByLabel('Full name', { exact: true }).fill('Retained synthetic draft');
  const token = await apiLogin(page, 'hr'), headers = { Authorization: 'Bearer ' + token };
  const employee = await (await page.request.get(fixture.api + '/api/v1/employees/' + owner.id, { headers })).json();
  const update = await page.request.put(fixture.api + '/api/v1/employees/' + owner.id, { headers, data: { fullName: 'Server confirmed synthetic name', email: employee.email,
    departmentId: employee.departmentId, roles: employee.roles, managerAssignment: 'Preserve', expectedVersion: employee.version } }); expect(update.status()).toBe(200);
  const conflict = page.waitForResponse(r => r.url().endsWith('/employees/' + owner.id) && r.request().method() === 'PUT' && r.status() === 409);
  await page.getByRole('button', { name: 'Save Employee', exact: true }).click(); await conflict; await expect(page.getByLabel('Full name', { exact: true })).toHaveValue('Retained synthetic draft');
  await expect(page.getByText(/changed|conflict|Reload/, { exact: false }).first()).toBeVisible();
  await page.getByRole('button', { name: 'Reload latest values', exact: true }).click();
  await expect(page.getByLabel('Full name', { exact: true })).toHaveValue('Server confirmed synthetic name');
});
