import fs from 'node:fs';
import path from 'node:path';
import { test, expect, type Page } from '@playwright/test';

const fixture = () => JSON.parse(fs.readFileSync(process.env.HRFLOW_TEST_STATE!, 'utf8'));
const calendar = (page: Page) => page.getByRole('region', { name: 'Read-only team calendar', exact: true });
const grid = (page: Page) => calendar(page).getByRole('grid');
const input = (page: Page) => calendar(page).getByLabel('Show date (optional)', { exact: true });
const date = (page: Page, day: string) => grid(page).locator(`[data-calendar-date="${day}"]`);
const nav = (page: Page) => page.getByRole('navigation', { name: 'Primary', exact: true });
const teamResponse = (url: string) => new URL(url).pathname.endsWith('/leave-requests/team-summary');
function barrier() { let resolve!: () => void; const promise = new Promise<void>(r => { resolve = r; }); return { promise, resolve }; }

async function login(page: Page, actor = 'manager', direct = false, inPlace = false) {
  const data = fixture();
  if (!inPlace) await page.goto(data.client + (direct ? '/team-leave' : '/login'));
  await page.getByLabel('Email', { exact: true }).fill(data.employees[actor].email);
  await page.getByLabel('Password', { exact: true }).fill(data.password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(nav(page)).toBeVisible();
  if (actor !== 'employee') await expect(calendar(page).getByText(/^Team snapshot received /)).toBeVisible();
}
async function fixedFebruary(page: Page) {
  await page.clock.setFixedTime(new Date('2024-02-15T10:00:00Z'));
  await page.setViewportSize({ width: 1280, height: 960 });
}
async function expectFocus(page: Page, day: string) {
  await expect(date(page, day)).toBeFocused();
  await expect(grid(page).locator('button[tabindex="0"]')).toHaveCount(1);
}
async function key(page: Page, keyName: string, day: string) { await page.keyboard.press(keyName); await expectFocus(page, day); }
async function loaded(page: Page) { await expect(calendar(page).getByText(/^Team snapshot received /)).toBeVisible(); }
async function nextMonth(page: Page, name: string) {
  const response = page.waitForResponse(r => teamResponse(r.url()) && new URL(r.url()).searchParams.get('start') === '2024-02-26');
  await calendar(page).getByRole('button', { name, exact: true }).click();
  expect((await response).status()).toBe(200); await loaded(page);
}

test('HTTP calendar: one Tab stop, arrows, leap week boundaries, focus versus selection and reset on Overview', async ({ page }) => {
  await fixedFebruary(page); await login(page);
  await test.step('calendar: tab-entry', async () => {
    await calendar(page).getByRole('button', { name: 'Export CSV', exact: true }).focus();
    await page.keyboard.press('Tab'); await expectFocus(page, '2024-02-15');
    await page.keyboard.press('Tab'); await expect(input(page)).toBeFocused();
    await page.keyboard.press('Shift+Tab'); await expectFocus(page, '2024-02-15');
    await expect(date(page, '2024-02-15')).toHaveAttribute('aria-current', 'date');
    await expect(date(page, '2024-02-15')).toHaveAccessibleName(/Thursday.*15.*February.*2024/);
    await expect(grid(page).getByRole('columnheader')).toHaveCount(7);
  });
  await test.step('calendar: arrows-selection', async () => {
    await key(page, 'ArrowLeft', '2024-02-14'); await key(page, 'ArrowUp', '2024-02-07');
    await key(page, 'ArrowDown', '2024-02-14'); await key(page, 'Home', '2024-02-12');
    await key(page, 'End', '2024-02-18'); await key(page, 'ArrowRight', '2024-02-19');
    await expect(input(page)).toHaveValue('');
    await page.keyboard.press('Enter'); await expect(input(page)).toHaveValue('2024-02-19');
    await expect(grid(page).locator('[aria-selected="true"]')).toHaveCount(1);
    await key(page, 'ArrowRight', '2024-02-20'); await expect(input(page)).toHaveValue('2024-02-19');
    await page.keyboard.press('Space'); await expect(input(page)).toHaveValue('2024-02-20');
    await key(page, 'ArrowDown', '2024-02-27'); await key(page, 'ArrowRight', '2024-02-28');
    await key(page, 'ArrowRight', '2024-02-29'); await key(page, 'ArrowRight', '2024-03-01');
    await expect(date(page, '2024-03-01')).toHaveAccessibleName(/adjacent month/);
    await expect(input(page)).toHaveValue('2024-02-20');
  });
  await test.step('calendar: range-edges', async () => {
    await key(page, 'Control+Home', '2024-01-29'); await key(page, 'ArrowLeft', '2024-01-29');
    await key(page, 'ArrowUp', '2024-01-29'); await key(page, 'Home', '2024-01-29');
    await key(page, 'Control+End', '2024-03-03'); await key(page, 'ArrowRight', '2024-03-03');
    await key(page, 'ArrowDown', '2024-03-03'); await key(page, 'End', '2024-03-03');
    await expect(calendar(page).getByRole('heading', { level: 2 })).toContainText('February 2024');
    await calendar(page).getByRole('button', { name: 'Show whole range', exact: true }).click();
    await expect(input(page)).toHaveValue(''); await expect(grid(page).locator('[aria-selected="true"]')).toHaveCount(0);
    await expect(grid(page).locator('button[tabindex="0"]')).toHaveAttribute('data-calendar-date', '2024-03-03');
    await nextMonth(page, 'Next month');
    await expect(calendar(page).getByRole('button', { name: 'Next month', exact: true })).toBeFocused();
    await expect(grid(page).locator('button[tabindex="0"]')).toHaveAttribute('data-calendar-date', '2024-03-01');
    const response = page.waitForResponse(r => teamResponse(r.url()) && new URL(r.url()).searchParams.get('start') === '2024-01-29');
    await calendar(page).getByRole('button', { name: 'Today', exact: true }).click(); expect((await response).status()).toBe(200);
    await loaded(page); await expect(input(page)).toHaveValue('');
    await expect(grid(page).locator('button[tabindex="0"]')).toHaveAttribute('data-calendar-date', '2024-02-15');
  });
});

const syntheticEntries = [
  { requestId: '11111111-1111-4111-8111-111111111111', employeeId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', employeeName: 'Synthetic team member', isActive: true },
  { requestId: '22222222-2222-4222-8222-222222222222', employeeId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', employeeName: 'Synthetic team member', isActive: true },
  { requestId: '33333333-3333-4333-8333-333333333333', employeeId: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb', employeeName: 'Synthetic former team member', isActive: false },
].map(e => ({ ...e, leaveTypeId: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc', leaveTypeName: 'Synthetic ordinary leave', startDate: '2024-02-10', endDate: '2024-02-20' }));

test('controlled calendar data and real HTTP export: distinct people, inactive history, range and responsive focus transfer', async ({ page }) => {
  await fixedFebruary(page);
  await page.route('**/leave-requests/team-summary?**', route => route.fulfill({ json: syntheticEntries }));
  await login(page, 'manager', true); await expect(page).toHaveURL(/\/team-leave$/);
  await date(page, '2024-02-15').focus(); await page.keyboard.press('Space');
  await expect(date(page, '2024-02-15').getByText('Today', { exact: true })).toBeVisible();
  await expect(date(page, '2024-02-15').getByText('Selected', { exact: true })).toBeVisible();
  await expect(calendar(page).getByRole('heading', { level: 3 })).toContainText('1 distinct active people');
  await expect(calendar(page).getByText('Inactive - historical leave', { exact: true })).toHaveCount(1);
  await expect(calendar(page).getByRole('link', { name: 'View history', exact: true })).toHaveCount(3);
  await expect(date(page, '2024-02-15')).toHaveAccessibleName(/1 active people/);
  const exported = page.waitForResponse(r => new URL(r.url()).pathname.endsWith('/team-summary/export'));
  const downloaded = page.waitForEvent('download');
  await calendar(page).getByRole('button', { name: 'Export CSV', exact: true }).click();
  const response = await exported; expect(response.status()).toBe(200);
  expect(new URL(response.url()).searchParams.get('start')).toBe('2024-01-29');
  expect(new URL(response.url()).searchParams.get('end')).toBe('2024-03-03');
  const stream = await (await downloaded).createReadStream(); const bytes: Buffer[] = [];
  for await (const chunk of stream) bytes.push(Buffer.from(chunk)); expect(Buffer.concat(bytes).length).toBeGreaterThan(0);
  // The real export is a fresh scoped HTTP snapshot, not the synthetic transport's three display rows.
  await date(page, '2024-02-15').focus(); await key(page, 'ArrowRight', '2024-02-16');
  await expect(input(page)).toHaveValue('2024-02-15');
  if (process.env.HRFLOW_CALENDAR_SCREENSHOTS === '1') await calendar(page).screenshot({ path: path.resolve('../../.local-verification/team-calendar-desktop.png') });
  await page.setViewportSize({ width: 390, height: 844 });
  await expect(input(page)).toBeFocused(); await expect(grid(page)).not.toBeVisible();
  await input(page).fill('2024-02-17'); await expect(input(page)).toHaveValue('2024-02-17');
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  const reset = calendar(page).getByRole('button', { name: 'Show whole range', exact: true });
  // Native date inputs have browser-specific day/month/year segments; verify bounded keyboard exit.
  for (let tabs = 0; tabs < 5 && !(await reset.evaluate(button => button === document.activeElement)); tabs++) await page.keyboard.press('Tab');
  await expect(reset).toBeFocused();
  await page.evaluate(() => document.documentElement.dataset.theme = 'dark');
  if (process.env.HRFLOW_CALENDAR_SCREENSHOTS === '1') {
    await calendar(page).evaluate(element => element.scrollIntoView({ block: 'start' }));
    await page.screenshot({ path: path.resolve('../../.local-verification/team-calendar-mobile-dark.png') });
  }
  await page.setViewportSize({ width: 1024, height: 960 });
  await expect(calendar(page).getByRole('button', { name: 'Show whole range', exact: true })).toBeFocused();
  await input(page).focus();
  // Native input segments apply in reverse too; the grid itself still has exactly one Tab stop.
  for (let tabs = 0; tabs < 5 && !(await date(page, '2024-02-16').evaluate(button => button === document.activeElement)); tabs++) await page.keyboard.press('Shift+Tab');
  await expectFocus(page, '2024-02-16');
  await expect(input(page)).toHaveValue('2024-02-17');
  await page.keyboard.press('Enter'); await expect(input(page)).toHaveValue('2024-02-16');
  const exportButton = calendar(page).getByRole('button', { name: 'Export CSV', exact: true });
  await exportButton.focus(); await page.setViewportSize({ width: 390, height: 844 });
  await expect(exportButton).toBeFocused();
});

test('HTTP calendar with controlled barriers: month loading, background refresh, error and retry retain selection and mounted focus', async ({ page }) => {
  await fixedFebruary(page); await login(page, 'manager', true);
  const entered = barrier(), release = barrier(); let first = true;
  await page.route('**/leave-requests/team-summary?**', async route => {
    if (!first) return route.continue(); first = false; entered.resolve(); await release.promise; await route.continue();
  });
  try {
    await calendar(page).getByRole('button', { name: 'Next month', exact: true }).click(); await entered.promise;
    await expect(calendar(page).getByRole('heading', { level: 2 })).toContainText('March 2024');
    await expect(grid(page).getByRole('button').first()).toHaveAccessibleName(/absence count unavailable/);
    await expect(calendar(page).getByText('No Approved team leave in this range.', { exact: true })).toHaveCount(0);
    await expect(calendar(page).getByRole('button', { name: 'Next month', exact: true })).toBeFocused();
    await date(page, '2024-03-01').focus(); await page.keyboard.press('Enter');
    const response = page.waitForResponse(r => teamResponse(r.url()) && r.status() === 200);
    release.resolve(); await response; await loaded(page); await expectFocus(page, '2024-03-01');
  } finally { release.resolve(); }
  await page.unroute('**/leave-requests/team-summary?**');
  const refreshed = page.waitForResponse(r => teamResponse(r.url()) && r.status() === 200);
  await calendar(page).getByRole('button', { name: 'Refresh calendar', exact: true }).evaluate((button: HTMLButtonElement) => button.click());
  await refreshed; await loaded(page); await expectFocus(page, '2024-03-01');
  await expect(input(page)).toHaveValue('2024-03-01');
  const refreshing = barrier(), finish = barrier();
  await page.route('**/leave-requests/team-summary?**', async route => { refreshing.resolve(); await finish.promise; await route.fulfill({ status: 503, json: { title: 'Synthetic unavailable', status: 503 } }); });
  try {
    await calendar(page).getByRole('button', { name: 'Refresh calendar', exact: true }).evaluate((button: HTMLButtonElement) => button.click());
    await refreshing.promise; await expectFocus(page, '2024-03-01');
    await expect(input(page)).toHaveValue('2024-03-01');
    await expect(calendar(page).getByText('Refreshing; previously loaded results are not yet confirmed current.', { exact: true })).toBeVisible();
    finish.resolve(); await expect(calendar(page).getByRole('button', { name: 'Retry calendar', exact: true })).toBeVisible();
    await expectFocus(page, '2024-03-01'); await expect(date(page, '2024-03-01')).toHaveAccessibleName(/absence count unavailable/);
    await expect(calendar(page).getByRole('heading', { level: 3 })).toContainText('absence count unavailable');
  } finally { finish.resolve(); }
  await page.unroute('**/leave-requests/team-summary?**');
  const retry = page.waitForResponse(r => teamResponse(r.url()) && r.status() === 200);
  await calendar(page).getByRole('button', { name: 'Retry calendar', exact: true }).click(); await retry; await loaded(page);
  await expect(input(page)).toHaveValue('2024-03-01');
  await expect(grid(page).locator('button[tabindex="0"]')).toHaveAttribute('data-calendar-date', '2024-03-01');
});

for (const year of [1900, 9998]) test(`HTTP calendar: supported ${year} boundary retains navigation focus and clamps date movement`, async ({ page }) => {
  await page.clock.setFixedTime(new Date(`${year}-${year === 1900 ? '01' : '12'}-15T10:00:00Z`));
  await page.setViewportSize({ width: 1280, height: 960 }); await login(page, 'manager', true);
  const edge = calendar(page).getByRole('button', { name: year === 1900 ? 'Previous month' : 'Next month', exact: true });
  await expect(edge).toHaveAttribute('aria-disabled', 'true'); await edge.focus(); await page.keyboard.press('Enter');
  await expect(edge).toBeFocused();
  await expect(calendar(page).getByRole('heading', { level: 2 })).toContainText(String(year));
  const focused = grid(page).locator('button[tabindex="0"]'); await focused.focus();
  await page.keyboard.press(year === 1900 ? 'Control+Home' : 'Control+End');
  const lastDate = await grid(page).locator('button[tabindex="0"]').getAttribute('data-calendar-date');
  await page.keyboard.press(year === 1900 ? 'ArrowLeft' : 'ArrowRight');
  await expectFocus(page, lastDate!);
  expect(year === 1900 ? lastDate === '1900-01-01' : lastDate!.startsWith('9999-01-')).toBe(true);
  await page.keyboard.press('Enter'); await expect(input(page)).toHaveValue(lastDate!);
  const away = calendar(page).getByRole('button', { name: year === 1900 ? 'Next month' : 'Previous month', exact: true });
  await away.click(); await loaded(page); await edge.click(); await loaded(page);
  await expect(edge).toBeFocused(); await expect(edge).toHaveAttribute('aria-disabled', 'true');
  await expect(input(page)).toHaveValue('');
});

for (const actor of ['employee', 'manager']) test(`controlled calendar transport: delayed date rows cannot cross replacement login as ${actor}`, async ({ page }) => {
  await fixedFebruary(page); await login(page, 'manager', true);
  await date(page, '2024-02-15').click();
  const entered = barrier(), release = barrier(), handled = barrier(); let hold = true;
  await page.route('**/leave-requests/team-summary?**', async route => {
    if (!hold) return route.continue(); hold = false;
    const response = await route.fetch(); entered.resolve(); await release.promise;
    try { await route.fulfill({ response, json: syntheticEntries }).catch(error => { if (!route.request().failure()) throw error; }); }
    finally { handled.resolve(); }
  });
  try {
    await calendar(page).getByRole('button', { name: 'Refresh calendar', exact: true }).click(); await entered.promise;
    await page.getByRole('button', { name: 'Sign out', exact: true }).click(); await expect(page).toHaveURL(/\/login$/);
    // Return to Overview explicitly; Employee cannot revisit the protected Team leave route.
    await page.goto(fixture().client + '/login'); await login(page, actor, false, true);
    release.resolve(); await handled.promise;
    await page.evaluate(() => new Promise<void>(r => requestAnimationFrame(() => requestAnimationFrame(() => r()))));
    await expect(page.getByText('Synthetic former team member', { exact: true })).toHaveCount(0);
    if (actor === 'manager') { await expect(input(page)).toHaveValue(''); await expect(grid(page).locator('button[tabindex="0"]')).toHaveAttribute('data-calendar-date', '2024-02-15'); }
    else await expect(calendar(page)).toHaveCount(0);
  } finally { release.resolve(); }
});

test('HTTP calendar: combined roles retain Manager calendar, personal workspace and HR navigation without decision controls', async ({ page }) => {
  await fixedFebruary(page); await login(page, 'combined');
  await expect(page.getByRole('region', { name: 'Personal leave dashboard', exact: true })).toBeVisible();
  await expect(nav(page).getByRole('link', { name: 'Employee management', exact: true })).toBeVisible();
  await expect(calendar(page).getByRole('button', { name: /^(Approve|Reject)$/ })).toHaveCount(0);
  await expect(grid(page).locator('button[tabindex="0"]')).toHaveCount(1);
  await nav(page).getByRole('link', { name: 'Team leave', exact: true }).click(); await loaded(page);
  await expect(page).toHaveURL(/\/team-leave$/);
});

for (const actor of ['employee', 'hr']) test(`HTTP calendar route: ${actor} without Manager cannot mount team queries or retrieve team data`, async ({ page }) => {
  const data = fixture(); let teamQueries = 0;
  page.on('request', request => { if (teamResponse(request.url())) teamQueries++; });
  await page.goto(data.client + '/team-leave');
  await page.getByLabel('Email', { exact: true }).fill(data.employees[actor].email);
  await page.getByLabel('Password', { exact: true }).fill(data.password);
  const signedIn = page.waitForResponse(r => new URL(r.url()).pathname.endsWith('/auth/login') && r.status() === 200);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  const token = (await (await signedIn).json()).accessToken;
  await expect(page.getByRole('heading', { name: 'You do not have permission to view this page.', exact: true })).toBeVisible();
  await expect(calendar(page)).toHaveCount(0); expect(teamQueries).toBe(0);
  const response = await page.request.get(data.api + '/api/v1/leave-requests/team-summary?start=2024-02-01&end=2024-02-29', { headers: { Authorization: 'Bearer ' + token } });
  expect(response.status()).toBe(403);
});
