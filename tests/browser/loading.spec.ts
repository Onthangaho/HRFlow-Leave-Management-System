import fs from 'node:fs';
import { test, expect, type Page } from '@playwright/test';

const fixture = () => JSON.parse(fs.readFileSync(process.env.HRFLOW_TEST_STATE!, 'utf8'));
const navigation = (page: Page) => page.getByRole('navigation', { name: 'Primary', exact: true });
async function signIn(page: Page, actor: string) {
  const data = fixture();
  await page.getByLabel('Email', { exact: true }).fill(data.employees[actor].email);
  await page.getByLabel('Password', { exact: true }).fill(data.password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(navigation(page)).toBeVisible();
}
async function login(page: Page, actor: string) { await page.goto(fixture().client + '/'); await signIn(page, actor); }
function barrier() { let resolve!: () => void; const promise = new Promise<void>(done => { resolve = done; }); return { promise, resolve }; }

const roleCases = [
  { actor: 'employee', roles: ['Employee'] }, { actor: 'manager', roles: ['Manager'] },
  { actor: 'hr', roles: ['HR Administrator'] }, { actor: 'combined', roles: ['Employee', 'Manager', 'HR Administrator'] },
  { actor: 'unrelated', roles: ['Employee', 'Manager'] }, { actor: 'unrelated', roles: ['Employee', 'HR Administrator'] },
  { actor: 'senior', roles: ['Manager', 'HR Administrator'] },
];
for (const { actor, roles } of roleCases) test(`HTTP route loading: additive workspaces and navigation for ${roles.join('+')}`, async ({ page }) => {
  if (['unrelated', 'senior'].includes(actor)) {
    // Configure only disposable fixture accounts through the real versioned HR operation.
    const data = fixture();
    const signedIn = await page.request.post(data.api + '/api/v1/auth/login', { data: { email: data.employees.hr.email, password: data.password } });
    expect(signedIn.status()).toBe(200);
    const headers = { Authorization: 'Bearer ' + (await signedIn.json()).accessToken };
    const current = await page.request.get(data.api + '/api/v1/employees/' + data.employees[actor].id, { headers });
    expect(current.status()).toBe(200); const employee = await current.json();
    const updated = await page.request.put(data.api + '/api/v1/employees/' + employee.id, { headers, data: {
      fullName: employee.fullName, email: employee.email, departmentId: employee.departmentId, roles,
      managerAssignment: 'Preserve', expectedVersion: employee.version,
    } });
    expect(updated.status()).toBe(200);
  }
  let logins = 0, profileSaves = 0; const modules: string[] = [];
  page.on('request', request => { if (request.resourceType() === 'script') modules.push(new URL(request.url()).pathname); });
  page.on('request', request => { if (new URL(request.url()).pathname.endsWith('/auth/login')) logins++; });
  page.on('request', request => { if (new URL(request.url()).pathname.endsWith('/me/profile') && request.method() === 'PUT') profileSaves++; });
  await login(page, actor);
  const personal = roles.some(role => role === 'Employee' || role === 'Manager'), manager = roles.includes('Manager'), hr = roles.includes('HR Administrator');
  await expect(page.getByRole('region', { name: 'Personal leave dashboard', exact: true })).toHaveCount(personal ? 1 : 0);
  await expect(page.getByRole('region', { name: 'Manager workspace', exact: true })).toHaveCount(manager ? 1 : 0);
  await expect(page.getByRole('region', { name: 'HR workspace', exact: true })).toHaveCount(hr ? 1 : 0);
  await expect(navigation(page).getByRole('link', { name: 'Overview', exact: true })).toHaveCount(1);
  await expect(navigation(page).getByRole('link', { name: 'Request leave', exact: true })).toHaveCount(personal ? 1 : 0);
  await expect(navigation(page).getByRole('link', { name: 'Approval queue', exact: true })).toHaveCount(manager ? 1 : 0);
  await expect(navigation(page).getByRole('link', { name: 'Employee management', exact: true })).toHaveCount(hr ? 1 : 0);
  expect(modules.some(module => module.includes('HrWorkspace'))).toBe(hr);
  expect(modules.some(module => module.includes('ManagerWorkspace'))).toBe(manager);
  const shell = await page.locator('header.shell-header').elementHandle();
  await navigation(page).getByRole('link', { name: 'Profile', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Profile image', exact: true })).toBeVisible();
  await page.getByLabel('Preferred display name (optional)', { exact: true }).fill('Retained route-loading draft');
  await page.getByLabel('Preferred display name (optional)', { exact: true }).blur();
  await expect(page.getByLabel('Preferred display name (optional)', { exact: true })).toHaveValue('Retained route-loading draft');
  const saved = page.waitForResponse(response => new URL(response.url()).pathname.endsWith('/me/profile') && response.request().method() === 'PUT');
  await page.getByRole('button', { name: 'Save profile', exact: true }).click();
  const response = await saved; expect(response.status()).toBe(200);
  await expect(page.getByText('Profile saved. Your canonical HR details are unchanged.', { exact: true })).toBeVisible();
  const persisted = await page.request.get(fixture().api + '/api/v1/me', { headers: { Authorization: response.request().headers().authorization } });
  expect(persisted.status()).toBe(200);
  const data = await persisted.json();
  expect(data.preferredDisplayName).toBe('Retained route-loading draft');
  expect(data.canonicalName).toBe(fixture().employees[actor].name); expect(profileSaves).toBe(1);
  expect(await shell!.evaluate(element => element === document.querySelector('header.shell-header'))).toBe(true);
  await navigation(page).getByRole('link', { name: 'Change password', exact: true }).click();
  await expect(page.getByLabel('Current password', { exact: true })).toBeVisible();
  await page.goBack(); await expect(page.getByRole('heading', { name: 'Profile image', exact: true })).toBeVisible();
  await page.goForward(); await expect(page.getByLabel('Current password', { exact: true })).toBeVisible();
  expect(logins).toBe(1);
  await expect(page.locator('main')).toHaveCount(1);
});

test('HTTP route loading: protected direct URL returns after login and denies missing capability', async ({ page }) => {
  await page.goto(fixture().client + '/account/profile'); await signIn(page, 'employee');
  await expect(page).toHaveURL(/\/account\/profile$/);
  await expect(page.getByRole('heading', { name: 'Profile image', exact: true })).toBeVisible();
  await page.goto(fixture().client + '/admin/onboarding'); await signIn(page, 'employee');
  await expect(page.getByRole('heading', { name: 'You do not have permission to view this page.', exact: true })).toBeVisible();
  await expect(page.getByRole('main').getByRole('heading', { name: 'CSV employee onboarding', exact: true })).toHaveCount(0);
});

test('controlled module transport: failure retries opening only without document reload or mutations', async ({ page }) => {
  await login(page, 'employee');
  let requests = 0, writes = 0;
  page.on('request', request => { if (new URL(request.url()).pathname.includes('/api/v1/') && !['GET', 'HEAD'].includes(request.method())) writes++; });
  await page.route('**/*AccountPages*', async route => { requests++; if (requests === 1) await route.abort('failed'); else await route.continue(); });
  await page.evaluate(() => { (window as unknown as { routeMarker: number }).routeMarker = 1; });
  await navigation(page).getByRole('link', { name: 'Profile', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Profile could not open', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Try opening again', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Profile image', exact: true })).toBeVisible();
  expect(requests).toBe(2); expect(writes).toBe(0);
  expect(await page.evaluate(() => (window as unknown as { routeMarker: number }).routeMarker)).toBe(1);
});

for (const replacement of ['employee', 'hr']) test(`controlled module transport: delayed feature cannot cross replacement login as ${replacement}`, async ({ page }) => {
  await login(page, 'employee');
  const entered = barrier(), release = barrier();
  await page.route('**/*AccountPages*', async route => { const response = await route.fetch(); entered.resolve(); await release.promise; await route.fulfill({ response }); });
  const delivered = page.waitForResponse(response => new URL(response.url()).pathname.includes('AccountPages') && response.status() === 200);
  const shell = await page.locator('header.shell-header').elementHandle();
  try {
    await navigation(page).getByRole('link', { name: 'Profile', exact: true }).click();
    await entered.promise;
    await expect(page.getByRole('status').filter({ hasText: 'Loading profile' })).toBeVisible();
    expect(await shell!.evaluate(element => element === document.querySelector('header.shell-header'))).toBe(true);
    await page.getByRole('button', { name: 'Sign out', exact: true }).click();
    await expect(page).toHaveURL(/\/login$/); await signIn(page, replacement);
    await navigation(page).getByRole('link', { name: 'Overview', exact: true }).click();
    release.resolve();
    const response = await delivered; await response.finished();
    // Await actual module evaluation, not just its first network byte, before checking the replacement UI.
    await page.evaluate(async url => { await import(url); }, response.url());
    await expect(page.getByRole('heading', { name: 'Overview', exact: true })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Profile image', exact: true })).toHaveCount(0);
    await expect(page.getByRole('main').getByLabel('Preferred display name (optional)', { exact: true })).toHaveCount(0);
    await expect(page.getByRole('region', { name: 'Personal leave dashboard', exact: true })).toHaveCount(replacement === 'employee' ? 1 : 0);
    await expect(page.getByRole('region', { name: 'HR workspace', exact: true })).toHaveCount(replacement === 'hr' ? 1 : 0);
  } finally { release.resolve(); }
});

test('controlled module transport: mobile dark loading preserves keyboard skip destination and width', async ({ page }) => {
  await page.emulateMedia({ colorScheme: 'dark', reducedMotion: 'reduce' });
  await login(page, 'employee'); await page.setViewportSize({ width: 390, height: 844 });
  const entered = barrier(), release = barrier();
  await page.route('**/*AccountPages*', async route => { entered.resolve(); await release.promise; await route.continue(); });
  try {
    await page.getByRole('button', { name: 'Open navigation', exact: true }).click();
    await page.getByRole('navigation', { name: 'Mobile primary', exact: true }).getByRole('link', { name: 'Profile', exact: true }).click();
    await entered.promise;
    await expect(page.getByRole('status').filter({ hasText: 'Loading profile' })).toBeVisible();
    await expect(page.getByRole('main')).toBeFocused();
    await page.getByRole('link', { name: 'Skip to content', exact: true }).focus(); await page.keyboard.press('Enter');
    await expect(page.getByRole('main')).toBeFocused();
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark');
    release.resolve();
    await expect(page.getByRole('heading', { name: 'Profile image', exact: true })).toBeVisible();
  } finally { release.resolve(); }
});

test('HTTP route loading: HR configuration, settings, inbox, reports and export remain reachable', async ({ page }) => {
  await login(page, 'hr');
  await navigation(page).getByRole('link', { name: 'Leave policies/types', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Leave types', exact: true })).toBeVisible();
  await navigation(page).getByRole('link', { name: 'Settings', exact: true }).click();
  await expect(page.getByLabel('Theme', { exact: true })).toBeVisible();
  await navigation(page).getByRole('link', { name: 'Notifications', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Your inbox', exact: true })).toBeVisible();
  await navigation(page).getByRole('link', { name: 'Department leave reports', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Department breakdown', exact: true })).toBeVisible();
  const downloaded = page.waitForEvent('download');
  await page.getByRole('button', { name: 'Export CSV', exact: true }).click();
  const file = await downloaded;
  expect(file.suggestedFilename()).toMatch(/^hr-department-leave-[0-9-]+\.csv$/);
  expect(await file.failure()).toBeNull();
});
