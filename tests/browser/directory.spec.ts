import fs from 'node:fs';
import { test, expect, type Page } from '@playwright/test';
const fixture = () => JSON.parse(fs.readFileSync(process.env.HRFLOW_TEST_STATE!, 'utf8'));
async function directory(page: Page) {
  const data = fixture(); await page.goto(data.client + '/login');
  await page.getByLabel('Email', { exact: true }).fill(data.employees.hr.email);
  await page.getByLabel('Password', { exact: true }).fill(data.password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await page.getByRole('navigation', { name: 'Primary', exact: true }).getByRole('link', { name: 'Employee management', exact: true }).click();
  await expect(page.locator('tbody tr').first()).toBeVisible();
}
async function search(page: Page, value: string) {
  await page.getByLabel('Search employees', { exact: true }).fill(value);
  await page.getByRole('button', { name: 'Apply filters', exact: true }).click();
  await expect(page.getByRole('region', { name: 'Directory results' })).not.toHaveAttribute('aria-busy', 'true');
}

test('HTTP directory: applied pages, drafts, attention links and desktop/mobile layouts', async ({ page }) => {
  await directory(page); await expect(page.locator('tbody tr')).toHaveCount(25);
  const firstName = await page.locator('tbody tr').first().innerText();
  await page.getByRole('button', { name: 'Next page', exact: true }).click();
  await expect(page.locator('tbody tr').first()).not.toHaveText(firstName);
  await expect(page.getByText('Page 2 of 4', { exact: true })).toBeVisible();
  await page.getByLabel('Search employees', { exact: true }).fill('DIR-0074');
  await expect(page.getByText('Page 2 of 4', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Apply filters', exact: true }).click();
  await expect(page.locator('tbody tr')).toHaveCount(1);
  await expect(page.getByRole('button', { name: 'Edit Directory person 0074', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Edit Directory person 0074', exact: true }).click();
  await page.getByLabel('Full name', { exact: true }).fill('Retained pagination draft');
  await page.getByRole('button', { name: 'Refresh directory', exact: true }).click();
  await expect(page.getByLabel('Full name', { exact: true })).toHaveValue('Retained pagination draft');
  await page.getByRole('button', { name: 'Cancel', exact: true }).click();
  await page.getByRole('navigation', { name: 'Primary', exact: true }).getByRole('link', { name: 'Overview', exact: true }).click();
  await page.locator('a[href="/admin/employees?attention=schedule"]').click();
  await expect(page.getByRole('combobox', { name: 'Administrative attention', exact: true })).toHaveValue('schedule');
  await expect(page.getByText(/75 matching employees/)).toBeVisible();
  await page.setViewportSize({ width: 1280, height: 1000 });
  if (process.env.HRFLOW_DIRECTORY_SCREENSHOTS === '1') await page.screenshot({ path: '../../docs/verification/screenshots/bounded-directory/desktop.png' });
  await page.setViewportSize({ width: 390, height: 844 });
  await page.emulateMedia({ colorScheme: 'dark', reducedMotion: 'reduce' });
  await page.evaluate(() => document.documentElement.classList.add('dark'));
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.getByLabel('Search employees', { exact: true }).focus(); await page.keyboard.press('Tab');
  await expect(page.getByLabel('Employment status', { exact: true })).toBeFocused();
  await page.getByRole('region', { name: 'Directory results', exact: true }).evaluate(element => element.scrollIntoView({ block: 'start' }));
  if (process.env.HRFLOW_DIRECTORY_SCREENSHOTS === '1') await page.screenshot({ path: '../../docs/verification/screenshots/bounded-directory/mobile-dark.png' });
});

test('HTTP directory: create, Manager lookup beyond page, Preserve edit, resend, schedule and deactivation', async ({ page }) => {
  const data = fixture(); await directory(page);
  await page.getByRole('button', { name: 'New Employee', exact: true }).click();
  await page.getByLabel('Employee number', { exact: true }).fill('BROWSER-DIR');
  await page.getByLabel('Confirmed employment start date', { exact: true }).fill('2020-01-01');
  await page.getByLabel('Full name', { exact: true }).fill('Directory browser employee');
  await page.getByLabel('Email', { exact: true }).fill('directory-browser@synthetic.invalid');
  await page.locator('#employee-department').selectOption({ label: 'Synthetic Engineering' });
  await page.getByRole('checkbox', { name: 'Employee', exact: true }).check();
  await page.getByLabel('Search eligible Managers', { exact: true }).fill('DIR-0074');
  await expect(page.locator('#employee-manager option')).toHaveCount(2);
  await page.locator('#employee-manager').selectOption({ label: 'Directory person 0074' });
  const created = page.waitForResponse(r => r.url() === data.api + '/api/v1/employees' && r.request().method() === 'POST');
  await page.getByRole('button', { name: 'Save Employee', exact: true }).click(); expect((await created).status()).toBe(201);
  await search(page, 'directory-browser@synthetic.invalid'); await expect(page.locator('tbody tr')).toHaveCount(1);
  await page.getByRole('button', { name: 'Edit Directory browser employee', exact: true }).click();
  await expect(page.getByText('Current manager: Directory person 0074', { exact: true })).toBeVisible();
  await expect(page.getByLabel('Manager assignment', { exact: true })).toHaveValue('Preserve');
  await page.getByLabel('Full name', { exact: true }).fill('Directory browser edited');
  await page.getByRole('button', { name: 'Save Employee', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Edit Directory browser edited', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Resend invitation', exact: true }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Resend invitation', exact: true }).click();
  await expect(page.getByRole('dialog')).not.toBeVisible();
  await page.getByRole('button', { name: 'Edit Directory browser edited', exact: true }).click();
  await page.getByRole('button', { name: 'Record a schedule revision', exact: true }).click();
  await page.getByLabel('Schedule name', { exact: true }).fill('Synthetic weekly');
  await page.getByLabel('Effective from', { exact: true }).fill('2020-01-01');
  await page.getByLabel('Monday working hours', { exact: true }).fill('8');
  await page.getByRole('button', { name: 'Save schedule revision', exact: true }).click();
  await expect(page.getByText('Schedule revision saved. Previous revisions preserved.', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Cancel', exact: true }).click();
  await page.getByRole('button', { name: 'Deactivate Directory browser edited', exact: true }).click();
  await page.getByLabel('Reason for deactivation', { exact: true }).fill('Synthetic directory verification');
  await page.getByRole('dialog').getByRole('button', { name: 'Deactivate employee', exact: true }).click();
  await expect(page.getByRole('button', { name: 'View details for Directory browser edited', exact: true })).toBeVisible();
  const login = await page.request.post(data.api + '/api/v1/auth/login', { data: { email: data.employees.hr.email, password: data.password } });
  const headers = { Authorization: 'Bearer ' + (await login.json()).accessToken };
  const result = await (await page.request.get(data.api + '/api/v1/employees?search=directory-browser', { headers })).json();
  expect(result.items[0].managerName).toBe('Directory person 0074'); expect(result.items[0].isActive).toBe(false);
  expect((await (await page.request.get(data.api + `/api/v1/employees/${result.items[0].id}/schedules`, { headers })).json()).revisions).toHaveLength(1);
});

test('controlled directory transport: visible image bounds, URL cleanup and permission denial', async ({ page }) => {
  let created = 0, revoked = 0, requests = 0;
  await page.addInitScript(() => {
    const create = URL.createObjectURL.bind(URL), revoke = URL.revokeObjectURL.bind(URL);
    (window as unknown as { imageUrls: { created: number; revoked: number } }).imageUrls = { created: 0, revoked: 0 };
    URL.createObjectURL = blob => { (window as unknown as { imageUrls: { created: number } }).imageUrls.created++; return create(blob); };
    URL.revokeObjectURL = url => { (window as unknown as { imageUrls: { revoked: number } }).imageUrls.revoked++; revoke(url); };
  });
  await page.route(/\/api\/v1\/employees(?:\?.*)?$/, async route => {
    if (route.request().method() !== 'GET') return route.continue();
    const response = await route.fetch(); const body = await response.json();
    body.items = body.items.map((item: object) => ({ ...item, profileImageVersion: '00000000-0000-0000-0000-000000000001' }));
    await route.fulfill({ response, json: body });
  });
  await page.route(/\/api\/v1\/employees\/[\w-]+\/image$/, async route => {
    requests++; await route.fulfill({ contentType: 'image/png', body: Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScLbtAAAAABJRU5ErkJggg==', 'base64') });
  });
  await directory(page); await expect(page.locator('tbody tr img').first()).toBeVisible();
  expect(requests).toBeGreaterThan(0); expect(requests).toBeLessThan(25);
  await page.getByRole('button', { name: 'Next page', exact: true }).click();
  await expect(page.getByText('Page 2 of 4', { exact: true })).toBeVisible();
  const urls = await page.evaluate(() => (window as unknown as { imageUrls: { created: number; revoked: number } }).imageUrls); created = urls.created; revoked = urls.revoked;
  expect(created).toBeGreaterThan(0); expect(revoked).toBeGreaterThan(0);
  await page.route(/\/api\/v1\/employees(?:\?.*)?$/, route => route.fulfill({ status: 403, contentType: 'application/problem+json', json: { detail: 'Current HR access was revoked.' } }));
  await page.getByRole('button', { name: 'Refresh directory', exact: true }).click();
  await expect(page.getByText('You no longer have HR directory access. Protected employee content is hidden.', { exact: true })).toBeVisible();
  await expect(page.locator('tbody tr')).toHaveCount(0);
  await expect.poll(async () => page.evaluate(() => {
    const urls = (window as unknown as { imageUrls: { created: number; revoked: number } }).imageUrls;
    return urls.created - urls.revoked;
  })).toBe(0);
});

for (const replacement of ['hr', 'employee']) test(`controlled directory transport: delayed page cannot cross replacement login as ${replacement}`, async ({ page }) => {
  await directory(page);
  let release!: () => void, entered!: () => void;
  const barrier = new Promise<void>(resolve => { release = resolve; });
  const ready = new Promise<void>(resolve => { entered = resolve; }); let held = false;
  await page.route(/\/api\/v1\/employees(?:\?.*)?$/, async route => {
    if (new URL(route.request().url()).searchParams.get('page') !== '2' || held) return route.continue();
    held = true; const response = await route.fetch(); const body = await response.json(); entered(); await barrier;
    body.items = body.items.map((item: object) => ({ ...item, fullName: 'Old session directory marker' }));
    try { await route.fulfill({ response, json: body }); } catch { /* Initiating session cancelled its request. */ }
  });
  await page.getByRole('button', { name: 'Next page', exact: true }).click(); await ready;
  await page.getByRole('button', { name: 'Sign out', exact: true }).click();
  await page.getByLabel('Email', { exact: true }).fill(fixture().employees[replacement].email);
  await page.getByLabel('Password', { exact: true }).fill(fixture().password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  const navigation = page.getByRole('navigation', { name: 'Primary', exact: true });
  await expect(navigation).toBeVisible();
  if (replacement === 'hr') {
    await navigation.getByRole('link', { name: 'Employee management', exact: true }).click();
    release(); await expect(page.locator('tbody tr')).toHaveCount(25);
    await expect(page.getByText('Page 1 of 4', { exact: true })).toBeVisible();
  } else {
    release(); await expect(navigation.getByRole('link', { name: 'Employee management', exact: true })).toHaveCount(0);
    await expect(page.locator('tbody tr')).toHaveCount(0);
  }
  await expect(page.getByText('Old session directory marker', { exact: true })).toHaveCount(0);
});

test('HTTP directory: mutations empty the last filtered page and refresh clamps to the last valid page', async ({ page }) => {
  await directory(page); await page.getByLabel('Employment status', { exact: true }).selectOption('Active'); await search(page, 'DIR-00');
  await page.getByRole('button', { name: 'Next page', exact: true }).click(); await expect(page.getByText('Page 2 of 3', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Next page', exact: true }).click(); await expect(page.getByText('Page 3 of 3', { exact: true })).toBeVisible();
  const data = fixture(); const login = await page.request.post(data.api + '/api/v1/auth/login', { data: { email: data.employees.hr.email, password: data.password } });
  const headers = { Authorization: 'Bearer ' + (await login.json()).accessToken };
  const last = await (await page.request.get(data.api + '/api/v1/employees?search=DIR-00&status=Active&page=3', { headers })).json();
  expect(last.items).toHaveLength(25);
  for (const employee of last.items) expect((await page.request.patch(data.api + `/api/v1/employees/${employee.id}/deactivate`, { headers, data: { expectedVersion: employee.version, reason: 'Synthetic pagination verification' } })).status()).toBe(200);
  await page.getByRole('button', { name: 'Refresh directory', exact: true }).click();
  await expect(page.getByText('Page 2 of 2', { exact: true })).toBeVisible(); await expect(page.locator('tbody tr')).toHaveCount(25);
  const inactive = await (await page.request.get(data.api + '/api/v1/employees?search=DIR-00&status=Inactive', { headers })).json(); expect(inactive.totalCount).toBe(25);
});

test('HTTP directory: CSV onboarding still previews and confirms then refreshes the bounded list', async ({ page }) => {
  await directory(page);
  await page.getByRole('navigation', { name: 'Primary', exact: true }).getByRole('link', { name: 'Overview', exact: true }).click();
  await page.getByRole('link', { name: 'CSV onboarding', exact: true }).click();
  const csv = 'FirstName,LastName,EmailAddress,EmployeeNumber,Roles,Department,ManagerEmail,EmploymentStartDate,WorkScheduleCode,Gender\nSynthetic,CSV Directory,csv-directory@synthetic.invalid,CSV-DIR,Employee,Synthetic Engineering,,2020-01-01,,\n';
  await page.getByLabel('UTF-8 CSV file', { exact: true }).setInputFiles({ name: 'synthetic.csv', mimeType: 'text/csv', buffer: Buffer.from(csv) });
  await page.getByRole('button', { name: 'Inspect columns', exact: true }).click();
  await page.getByRole('button', { name: 'Validate preview', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Confirm selected Ready rows', exact: true })).toBeEnabled();
  await page.getByRole('button', { name: 'Confirm selected Ready rows', exact: true }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Create 1 accounts', exact: true }).click();
  await expect(page.getByRole('cell', { name: 'Created', exact: true })).toBeVisible();
  await page.getByRole('navigation', { name: 'Primary', exact: true }).getByRole('link', { name: 'Employee management', exact: true }).click();
  await search(page, 'csv-directory@synthetic.invalid');
  await expect(page.getByRole('button', { name: 'Edit Synthetic CSV Directory', exact: true })).toBeVisible();
});
