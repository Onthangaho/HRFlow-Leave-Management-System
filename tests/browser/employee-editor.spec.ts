import fs from 'node:fs';
import { DatabaseSync } from 'node:sqlite';
import { test, expect, type Page } from '@playwright/test';
const fixture = () => JSON.parse(fs.readFileSync(process.env.HRFLOW_TEST_STATE!, 'utf8'));
async function directory(page: Page) {
  const f = fixture(); await page.goto(f.client + '/login');
  await page.getByLabel('Email', { exact: true }).fill(f.employees.hr.email);
  await page.getByLabel('Password', { exact: true }).fill(f.password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  if (page.viewportSize()!.width < 1024) {
    await page.getByRole('button', { name: 'Open navigation', exact: true }).click();
    await page.getByRole('navigation', { name: 'Mobile primary', exact: true }).getByRole('link', { name: 'Employee management', exact: true }).click();
  } else await page.getByRole('navigation', { name: 'Primary', exact: true }).getByRole('link', { name: 'Employee management', exact: true }).click();
  await expect(page.locator('tbody tr')).toHaveCount(25);
}
test('HTTP employee Edit: below-fold action reveals and focuses the original editor', async ({ page }) => {
  let errors = 0, correlations = 0, detailReads = 0;
  let departments = 0, roles = 0, managers = 0, aborted = 0, updates = 0;
  const correlation = 'edit-drill-' + crypto.randomUUID(); await page.setExtraHTTPHeaders({ 'X-Correlation-ID': correlation });
  page.on('pageerror', () => errors++);
  page.on('requestfailed', request => { if (request.failure()?.errorText.includes('ERR_ABORTED')) aborted++; });
  page.on('response', response => { if (new URL(response.url()).pathname.startsWith('/api/')) {
    const path = new URL(response.url()).pathname;
    if (path === '/api/v1/departments') departments = response.status();
    if (path === '/api/v1/roles') roles = response.status();
    if (path === '/api/v1/employees/managers') managers = response.status();
    if (response.request().method() === 'PUT') updates++;
    if (response.headers()['x-correlation-id'] === correlation) correlations++;
    if (/\/employees\/[a-f0-9-]{36}$/.test(new URL(response.url()).pathname)) detailReads++;
  } });
  await page.setViewportSize({ width: 1280, height: 720 }); await directory(page);
  const action = page.getByRole('button', { name: /^Edit Directory person/ }).last();
  const selectedName = (await action.getAttribute('aria-label'))!.slice('Edit '.length);
  const managerRead = page.waitForResponse(r => new URL(r.url()).pathname === '/api/v1/employees/managers');
  await action.scrollIntoViewIfNeeded(); await action.click();
  expect((await managerRead).status()).toBe(200);
  await expect(page.getByRole('button', { name: 'Save Employee', exact: true })).toBeEnabled();
  const editor = page.getByRole('region', { name: 'Edit Employee', exact: true }); await expect(editor).toBeAttached();
  const position = await editor.boundingBox(); const focused = await editor.evaluate(e => e.contains(document.activeElement));
  const visible = !!position && position.y >= 0 && position.y < 720;
  console.log(`editor-probe rendered=1 visible=${Number(visible)} focused=${Number(focused)} consoleErrors=${errors} correlations=${correlations} detailReads=${detailReads}`);
  console.log(`editor-read-status departments=${departments} roles=${roles} managers=${managers} aborted=${aborted} updates=${updates}`);
  expect(visible).toBe(true); await expect(page.getByRole('heading', { name: 'Edit Employee', exact: true })).toBeFocused();
  await expect(page.getByLabel('Full name', { exact: true })).toHaveValue(selectedName);
  expect(errors).toBe(0); expect(correlations).toBeGreaterThan(0);
  expect([departments, roles, managers]).toEqual([200, 200, 200]); expect(updates).toBe(0);
  await page.getByRole('button', { name: 'Cancel', exact: true }).click();
  await expect(action).toBeFocused();
});

test('HTTP employee editor: mobile keyboard create, save, edit and cancel preserve persisted facts', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 }); await directory(page);
  const f = fixture(), suffix = crypto.randomUUID().replaceAll('-', '').slice(0, 12);
  const email = `editor-${suffix}@synthetic.invalid`, number = `EDIT-${suffix}`;
  const create = page.getByRole('button', { name: 'New Employee', exact: true }); await create.focus(); await page.keyboard.press('Enter');
  await expect(page.getByRole('heading', { name: 'New Employee', exact: true })).toBeFocused();
  await page.getByLabel('Employee number', { exact: true }).fill(number);
  await page.getByLabel('Confirmed employment start date', { exact: true }).fill('2024-02-29');
  await page.getByLabel('Full name', { exact: true }).fill('Synthetic editor creation');
  await page.getByLabel('Email', { exact: true }).fill(email);
  const department = page.getByRole('region', { name: 'New Employee', exact: true }).getByLabel('Department', { exact: true });
  await expect(department.locator('option')).not.toHaveCount(1);
  const departmentId = await department.locator('option').nth(1).getAttribute('value'); await department.selectOption(departmentId!);
  await page.getByRole('checkbox', { name: 'Employee', exact: true }).check();
  const created = page.waitForResponse(r => new URL(r.url()).pathname === '/api/v1/employees' && r.request().method() === 'POST');
  await page.getByRole('button', { name: 'Save Employee', exact: true }).click(); expect((await created).status()).toBe(201);
  await expect(page.getByRole('region', { name: 'New Employee', exact: true })).toHaveCount(0);
  await expect(create).toBeFocused();
  await page.getByLabel('Search employees', { exact: true }).fill(email);
  const filtered = page.waitForResponse(r => new URL(r.url()).pathname === '/api/v1/employees' && r.request().method() === 'GET' && new URL(r.url()).searchParams.get('search') === email);
  await page.getByRole('button', { name: 'Apply filters', exact: true }).click(); await filtered;
  const edit = page.getByRole('button', { name: 'Edit Synthetic editor creation', exact: true }); await edit.focus(); await page.keyboard.press('Enter');
  await expect(page.getByRole('heading', { name: 'Edit Employee', exact: true })).toBeFocused();
  await expect(page.getByLabel('Manager assignment', { exact: true })).toHaveValue('Preserve');
  await page.getByLabel('Full name', { exact: true }).fill('Synthetic editor saved');
  const updated = page.waitForResponse(r => /\/employees\/[a-f0-9-]{36}$/.test(new URL(r.url()).pathname) && r.request().method() === 'PUT');
  await page.getByRole('button', { name: 'Save Employee', exact: true }).click(); expect((await updated).status()).toBe(200);
  const savedEdit = page.getByRole('button', { name: 'Edit Synthetic editor saved', exact: true }); await savedEdit.click();
  await page.getByLabel('Full name', { exact: true }).fill('Unsaved synthetic draft');
  await page.getByRole('button', { name: 'Cancel', exact: true }).click(); await expect(savedEdit).toBeFocused();
  const db = new DatabaseSync(f.authRegression.database, { readOnly: true });
  try {
    db.exec('PRAGMA busy_timeout=3000');
    const row = db.prepare('SELECT FullName, EmployeeNumber, EmploymentStartDate, DepartmentId, ManagerId FROM Employees WHERE Email=?').get(email)!;
    expect(row.FullName).toBe('Synthetic editor saved'); expect(row.EmployeeNumber).toBe(number.toUpperCase());
    expect(String(row.EmploymentStartDate).slice(0, 10)).toBe('2024-02-29'); expect(String(row.DepartmentId).toLowerCase()).toBe(departmentId!.toLowerCase()); expect(row.ManagerId).toBeNull();
  } finally { db.close(); }
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
});

test('HTTP own avatar: absence metadata prevents content requests across navigation', async ({ page }) => {
  let content = 0, metadata = 0;
  page.on('request', request => { const path = new URL(request.url()).pathname; if (path === '/api/v1/me/image/content') content++; if (path === '/api/v1/me/image') metadata++; });
  await directory(page);
  await page.getByRole('navigation', { name: 'Primary', exact: true }).getByRole('link', { name: 'Profile', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Profile', exact: true })).toBeVisible();
  await page.getByRole('navigation', { name: 'Primary', exact: true }).getByRole('link', { name: 'Employee management', exact: true }).click();
  await expect(page.locator('tbody tr')).toHaveCount(25);
  expect(metadata).toBeGreaterThan(0); expect(content).toBe(0);
});

test('controlled employee choices: visible loading preserves the draft until real role data arrives', async ({ page }) => {
  await directory(page);
  let entered!: () => void, release!: () => void;
  const ready = new Promise<void>(resolve => { entered = resolve; });
  const barrier = new Promise<void>(resolve => { release = resolve; });
  await page.route('**/api/v1/roles', async route => {
    const response = await route.fetch(); entered(); await barrier; await route.fulfill({ response });
  });
  try {
    await page.getByRole('button', { name: /^Edit Directory person/ }).last().click(); await ready;
    await expect(page.getByRole('heading', { name: 'Edit Employee', exact: true })).toBeFocused();
    await expect(page.getByText('Loading choices...', { exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Save Employee', exact: true })).toBeDisabled();
    await page.getByLabel('Full name', { exact: true }).fill('Retained choice-loading draft');
  } finally { release(); }
  await expect(page.getByRole('button', { name: 'Save Employee', exact: true })).toBeEnabled();
  await expect(page.getByLabel('Full name', { exact: true })).toHaveValue('Retained choice-loading draft');
});

test('controlled own avatar: declared image failures remain distinguishable from absence', async ({ page }) => {
  let requests = 0;
  await page.route('**/api/v1/me/image', route => route.fulfill({ json: { version: crypto.randomUUID(), hasImage: true } }));
  page.on('response', response => { if (new URL(response.url()).pathname === '/api/v1/me/image/content' && response.status() === 404) requests++; });
  await directory(page);
  await expect(page.getByTitle('Profile image unavailable', { exact: true })).toBeVisible();
  expect(requests).toBeGreaterThan(0);
});

test('controlled stale reload: loading and failure preserve the draft and explicit recovery action', async ({ page }) => {
  await directory(page);
  await page.getByRole('button', { name: /^Edit Directory person/ }).last().click();
  await page.getByLabel('Full name', { exact: true }).fill('Retained failed-reload draft');
  let entered!: () => void, release!: () => void;
  const ready = new Promise<void>(resolve => { entered = resolve; });
  const barrier = new Promise<void>(resolve => { release = resolve; });
  await page.route(/\/api\/v1\/employees\/[a-f0-9-]{36}$/, async route => {
    if (route.request().method() === 'PUT') { await route.fulfill({ status: 409, json: { detail: 'Synthetic version conflict.' } }); return; }
    entered(); await barrier; await route.fulfill({ status: 503, json: { detail: 'Synthetic detail read unavailable.' } });
  });
  await page.getByRole('button', { name: 'Save Employee', exact: true }).click();
  const reload = page.getByRole('button', { name: 'Reload latest values', exact: true }); await expect(reload).toBeVisible();
  try {
    await reload.click(); await ready;
    await expect(page.getByText('Reloading employee details...', { exact: true })).toBeVisible();
    await expect(reload).toBeDisabled();
    await expect(page.getByLabel('Full name', { exact: true })).toHaveValue('Retained failed-reload draft');
  } finally { release(); }
  await expect(page.getByText('Unable to reload this employee. Try again or cancel the edit.', { exact: true })).toBeVisible();
  await expect(reload).toBeEnabled();
  await expect(page.getByLabel('Full name', { exact: true })).toHaveValue('Retained failed-reload draft');
  await expect(page.getByLabel('Manager assignment', { exact: true })).toHaveValue('Preserve');
  await page.getByRole('button', { name: 'Cancel', exact: true }).click();
  await expect(page.getByRole('region', { name: 'Edit Employee', exact: true })).toHaveCount(0);
});

test('controlled layout change: Cancel avoids a hidden opener and disabled fallback', async ({ page }) => {
  await directory(page);
  const action = page.getByRole('button', { name: /^Edit Directory person/ }).last();
  await action.click();
  // Simulate a control becoming unavailable while its captured editor remains open.
  await action.evaluate(element => { element.setAttribute('hidden', ''); });
  await page.getByRole('button', { name: 'New Employee', exact: true }).evaluate(element => { element.setAttribute('hidden', ''); });
  await page.getByRole('button', { name: 'Cancel', exact: true }).click();
  await expect(page.getByRole('region', { name: 'Employee management introduction', exact: true })).toBeFocused();
  await expect(page.getByRole('region', { name: 'Employee management introduction', exact: true })).toBeInViewport();
});
