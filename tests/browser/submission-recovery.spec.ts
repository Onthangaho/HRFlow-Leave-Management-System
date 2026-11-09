import fs from 'node:fs';
import { randomUUID } from 'node:crypto';
import { test, expect, type Page } from '@playwright/test';

interface Fixture { api: string; client: string; password: string; typeId: string; employees: Record<string, { email: string }> }
const fixture = () => JSON.parse(fs.readFileSync(process.env.HRFLOW_TEST_STATE!, 'utf8')) as Fixture;
const navigation = (page: Page) => page.getByRole('navigation', { name: 'Primary', exact: true });
function barrier() { let resolve!: () => void; const promise = new Promise<void>(done => { resolve = done; }); return { promise, resolve }; }
async function signIn(page: Page, actor = 'employee') {
  const f = fixture(); await page.getByLabel('Email', { exact: true }).fill(f.employees[actor].email);
  await page.getByLabel('Password', { exact: true }).fill(f.password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click(); await expect(navigation(page)).toBeVisible();
}
async function open(page: Page) {
  const f = fixture();
  const login = await page.request.post(f.api + '/api/v1/auth/login', { data: { email: f.employees.hr.email, password: f.password } });
  expect(login.status()).toBe(200); const headers = { Authorization: 'Bearer ' + (await login.json()).accessToken };
  const policy = await page.request.post(f.api + '/api/v1/management/leave-policies', { headers, data: { name: 'Recovery ' + randomUUID(), defaultBalance: 20, allowOverlap: false } });
  expect(policy.status()).toBe(201);
  const type = await page.request.post(f.api + '/api/v1/management/leave-types', { headers, data: {
    name: 'Recovery ' + randomUUID(), leavePolicyId: (await policy.json()).id, descriptionMode: 'Optional', evidenceMode: 'Optional', evidenceClass: 'Ordinary' } });
  expect(type.status()).toBe(201);
  await page.goto(f.client + '/login'); await signIn(page);
  await navigation(page).getByRole('link', { name: 'Request leave', exact: true }).click();
  await page.getByLabel('Leave type', { exact: true }).selectOption((await type.json()).id);
  await page.getByLabel('Start date', { exact: true }).fill('2028-04-01');
  await page.getByLabel('End date', { exact: true }).fill('2028-04-02');
  await page.getByRole('textbox', { name: 'Description (optional)', exact: true }).fill('Synthetic original draft.');
}
const uncertainty = (page: Page) => page.getByRole('group', { name: 'Unconfirmed submission', exact: true });

test('HTTP submission recovery: lost committed response, retained edits, read-only outcome and one transition', async ({ page }) => {
  await open(page); const f = fixture(); let original: unknown, requestId = '', submissions = 0;
  page.on('request', request => { if (request.url() === f.api + '/api/v1/leave-requests' && request.method() === 'POST') submissions++; });
  await page.route('**/api/v1/leave-requests', async route => {
    original = route.request().postDataJSON(); const response = await route.fetch(); expect(response.status()).toBe(200); requestId = await response.json();
    await route.abort('failed');
  }, { times: 1 });
  await page.getByRole('button', { name: 'Submit request', exact: true }).click();
  await expect(uncertainty(page)).toBeVisible(); await expect(uncertainty(page)).toBeFocused();
  await expect(page.getByRole('button', { name: 'Submit request', exact: true })).toBeDisabled();
  await page.getByRole('textbox', { name: 'Description (optional)', exact: true }).fill('Synthetic edited draft.');
  await page.getByLabel('End date', { exact: true }).fill('2028-04-03');
  for (const scheme of ['light', 'dark'] as const) {
    await page.setViewportSize({ width: 390, height: 844 }); await page.emulateMedia({ colorScheme: scheme, reducedMotion: 'reduce' });
    await expect(page.locator('html')).toHaveAttribute('data-theme', scheme);
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  }
  const checked = page.waitForResponse(r => r.url().endsWith('/leave-requests/submission-outcome'));
  await uncertainty(page).getByRole('button', { name: 'Check submission outcome', exact: true }).focus(); await page.keyboard.press('Enter');
  const result = await checked; expect(result.status()).toBe(200); expect(result.request().postDataJSON()).toEqual(original);
  expect((await result.json()).requestId).toBe(requestId); await expect(page.getByRole('heading', { name: 'Request submitted', exact: true })).toBeVisible();
  expect(submissions).toBe(1);
  const login = await page.request.post(f.api + '/api/v1/auth/login', { data: { email: f.employees.employee.email, password: f.password } });
  const timeline = await page.request.get(f.api + `/api/v1/leave-requests/${requestId}/timeline`, { headers: { Authorization: 'Bearer ' + (await login.json()).accessToken } });
  expect(timeline.status()).toBe(200); expect((await timeline.json()).events.filter((e: { action: string }) => e.action === 'Submit')).toHaveLength(1);
});

test('HTTP submission recovery: absent outcome permits only explicit original retry and blocks duplicate clicks', async ({ page }) => {
  await open(page); let original: unknown; const committed = barrier(), release = barrier(); let posts = 0;
  await page.route('**/api/v1/leave-requests', async route => { posts++; original = route.request().postDataJSON(); await route.abort('failed'); }, { times: 1 });
  await page.getByRole('button', { name: 'Submit request', exact: true }).click(); await expect(uncertainty(page)).toBeVisible();
  await page.getByLabel('End date', { exact: true }).fill('2028-04-05');
  await page.getByRole('textbox', { name: 'Description (optional)', exact: true }).fill('Synthetic later draft.');
  await uncertainty(page).getByRole('button', { name: 'Check submission outcome', exact: true }).click();
  await expect(uncertainty(page)).toContainText('No committed outcome was visible');
  await expect(page.getByLabel('End date', { exact: true })).toHaveValue('2028-04-05');
  await expect(page.getByRole('textbox', { name: 'Description (optional)', exact: true })).toHaveValue('Synthetic later draft.');
  await page.route('**/api/v1/leave-requests', async route => {
    posts++; expect(route.request().postDataJSON()).toEqual(original);
    const response = await route.fetch(); expect(response.status()).toBe(200); committed.resolve(); await release.promise; await route.fulfill({ response });
  }, { times: 1 });
  try {
    await uncertainty(page).getByRole('button', { name: 'Retry original submission', exact: true }).click(); await committed.promise;
    await expect(uncertainty(page).getByRole('button', { name: 'Retry original submission', exact: true })).toBeDisabled();
    await expect(page.locator('button[type="submit"]')).toBeDisabled();
    expect(posts).toBe(2); release.resolve(); await expect(page.getByRole('heading', { name: 'Request submitted', exact: true })).toBeVisible();
  } finally { release.resolve(); }
});

test('controlled document metadata with HTTP outcome check: uncertainty preserves selected drafts without binding', async ({ page }) => {
  const draft = { id: randomUUID(), class: 'Ordinary', status: 'Clean', version: randomUUID(), canDownload: false, mediaType: 'image/png' };
  // Metadata-only UI fixture has no blob; the submission is blocked before dispatch and never called a successful scan/bind.
  await page.route('**/api/v1/documents', route => route.fulfill({ json: [draft] }));
  await open(page); await page.getByRole('checkbox').first().check(); let original: unknown;
  await page.route('**/api/v1/leave-requests', async route => { original = route.request().postDataJSON(); await route.abort('failed'); }, { times: 1 });
  await page.getByRole('button', { name: 'Submit request', exact: true }).click(); await expect(uncertainty(page)).toBeVisible();
  await expect(page.getByRole('checkbox').first()).toBeChecked();
  await page.getByLabel('Start date', { exact: true }).fill('2028-04-02');
  const checking = page.waitForResponse(r => r.url().endsWith('/leave-requests/submission-outcome'));
  await uncertainty(page).getByRole('button', { name: 'Check submission outcome', exact: true }).click();
  const response = await checking; expect(response.status()).toBe(200); expect(response.request().postDataJSON()).toEqual(original);
  expect(response.request().postDataJSON().documentIds).toEqual([draft.id]);
  await expect(page.getByRole('checkbox').first()).toBeChecked();
  await expect(page.getByLabel('Start date', { exact: true })).toHaveValue('2028-04-02');
});

for (const actor of ['employee', 'hr']) test(`HTTP submission recovery: late outcome cannot cross replacement session as ${actor}`, async ({ page }) => {
  await open(page); const entered = barrier(), release = barrier(), finished = barrier(); let checks = 0;
  await page.route('**/api/v1/leave-requests', async route => {
    const response = await route.fetch(); expect(response.status()).toBe(200); await route.abort('failed');
  }, { times: 1 });
  await page.getByRole('button', { name: 'Submit request', exact: true }).click(); await expect(uncertainty(page)).toBeVisible();
  await page.route('**/api/v1/leave-requests/submission-outcome', async route => {
    checks++;
    try { const response = await route.fetch(); expect(response.status()).toBe(200); console.log('submission-recovery-stage check-entered'); entered.resolve(); await release.promise; await route.fulfill({ response }); }
    finally { finished.resolve(); }
  });
  try {
    await uncertainty(page).getByRole('button', { name: 'Check submission outcome', exact: true }).click(); await entered.promise;
    await page.getByRole('button', { name: 'Sign out', exact: true }).click(); console.log('submission-recovery-stage signed-out'); await signIn(page, actor);
    console.log('submission-recovery-stage signed-in'); release.resolve(); console.log('submission-recovery-stage released'); await finished.promise; console.log('submission-recovery-stage settled'); expect(checks).toBe(1);
    await expect(page.getByRole('heading', { name: 'Request submitted', exact: true })).toHaveCount(0);
    await expect(uncertainty(page)).toHaveCount(0);
    if (actor === 'employee') {
      await navigation(page).getByRole('link', { name: 'Request leave', exact: true }).click();
      await expect(page.getByLabel('Start date', { exact: true })).toHaveValue('');
      await expect(page.getByRole('button', { name: 'Check submission outcome', exact: true })).toHaveCount(0);
    }
  } finally { release.resolve(); }
});
