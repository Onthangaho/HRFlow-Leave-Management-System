import fs from 'node:fs';
import { randomUUID } from 'node:crypto';
import { test, expect, type Page } from '@playwright/test';

interface Fixture { api: string; client: string; password: string; typeId: string; employees: Record<string, { email: string }>; authRegression: { expiredAccessToken: string } }
const fixture = () => JSON.parse(fs.readFileSync(process.env.HRFLOW_TEST_STATE!, 'utf8')) as Fixture;
const navigation = (page: Page) => page.getByRole('navigation', { name: 'Primary', exact: true });
function barrier() { let resolve!: () => void; const promise = new Promise<void>(done => { resolve = done; }); return { promise, resolve }; }
async function signIn(page: Page, actor: string) {
  const f = fixture(); await page.getByLabel('Email', { exact: true }).fill(f.employees[actor].email);
  await page.getByLabel('Password', { exact: true }).fill(f.password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click(); await expect(navigation(page)).toBeVisible();
}
async function open(page: Page, actor = 'employee') {
  await page.goto(fixture().client + '/login'); await signIn(page, actor);
  await navigation(page).getByRole('link', { name: 'Request leave', exact: true }).click();
  await page.getByLabel('Leave type', { exact: true }).selectOption(fixture().typeId);
  await page.getByLabel('Start date', { exact: true }).fill('2028-02-28');
  await page.getByLabel('End date', { exact: true }).fill('2028-03-01');
}
const result = (page: Page) => page.getByRole('group', { name: 'Request preview result', exact: true });

test('HTTP preview: actual expired bearer rejection permits one bounded read-only refresh and retry', async ({ page }) => {
  await open(page); const f = fixture(); let refreshes = 0, previews = 0;
  page.on('request', request => {
    if (request.url() === f.api + '/api/v1/auth/refresh') refreshes++;
    if (request.url() === f.api + '/api/v1/leave-requests/preview') previews++;
  });
  // Only the first bearer is replaced with the existing signed expiry probe; 401 is real middleware, not a synthesized transport response.
  await page.route('**/api/v1/leave-requests/preview', async route => {
    const response = await route.fetch({ headers: { ...route.request().headers(), authorization: 'Bearer ' + f.authRegression.expiredAccessToken } });
    expect(response.status()).toBe(401); await route.fulfill({ response });
  }, { times: 1 });
  const rejected = page.waitForResponse(r => r.url().endsWith('/leave-requests/preview') && r.status() === 401);
  await page.getByRole('button', { name: 'Preview request', exact: true }).click(); await rejected;
  await expect(result(page)).toContainText('2028-02-28 through 2028-03-01');
  expect(refreshes).toBe(1); expect(previews).toBe(2);
  await expect(page.getByLabel('Start date', { exact: true })).toHaveValue('2028-02-28');
});

test('HTTP preview: policy change requires explicit review while dates and description survive', async ({ page }) => {
  const f = fixture();
  const login = await page.request.post(f.api + '/api/v1/auth/login', { data: { email: f.employees.hr.email, password: f.password } });
  expect(login.status()).toBe(200); const headers = { Authorization: 'Bearer ' + (await login.json()).accessToken };
  const policyResponse = await page.request.post(f.api + '/api/v1/management/leave-policies', { headers, data: { name: 'Preview synthetic policy', allowOverlap: false, defaultBalance: 20 } });
  expect(policyResponse.status()).toBe(201); const policy = await policyResponse.json();
  const typeResponse = await page.request.post(f.api + '/api/v1/management/leave-types', { headers, data: {
    name: 'Preview synthetic type', leavePolicyId: policy.id, descriptionMode: 'Optional', evidenceMode: 'Optional', evidenceClass: 'Ordinary', requirementInstructions: 'Plain synthetic instructions.' } });
  expect(typeResponse.status()).toBe(201); const type = await typeResponse.json();
  await open(page); await page.getByLabel('Leave type', { exact: true }).selectOption(type.id);
  await page.getByRole('textbox', { name: 'Description (optional)', exact: true }).fill('Synthetic retained draft.');
  await page.getByRole('button', { name: 'Preview request', exact: true }).click();
  await expect(result(page)).toContainText('2028-02-28 through 2028-03-01');
  const update = await page.request.put(f.api + '/api/v1/management/leave-policies/' + policy.id, { headers, data: { name: policy.name, allowOverlap: false, defaultBalance: 21, expectedVersion: policy.version } });
  expect(update.status()).toBe(200);
  await page.getByRole('button', { name: 'Preview request', exact: true }).click();
  await expect(result(page)).toContainText('This preview has not refreshed your reviewed versions');
  const denied = page.waitForResponse(r => r.url() === f.api + '/api/v1/leave-requests' && r.request().method() === 'POST');
  await page.getByRole('button', { name: 'Submit request', exact: true }).click();
  expect((await denied).status()).toBe(409);
  await page.getByRole('button', { name: 'Reload and review changed requirements', exact: true }).click();
  await expect(page.getByText('Current requirements loaded.', { exact: false })).toBeVisible();
  await expect(page.getByText('Current requirements loaded.', { exact: false })).toBeFocused();
  const textareas = page.locator('textarea'), exactDescription = page.getByLabel('Description (optional)', { exact: true });
  const textareaCount = await textareas.count();
  const valueMatches = textareaCount === 1 && await textareas.inputValue() === 'Synthetic retained draft.';
  console.log(`preview-review-field count=${textareaCount} valueMatches=${valueMatches} labelCount=${await exactDescription.count()} roleCount=${await page.getByRole('textbox', { name: 'Description (optional)', exact: true }).count()}`);
  await expect(page.getByRole('textbox', { name: 'Description (optional)', exact: true })).toHaveValue('Synthetic retained draft.');
  await expect(page.getByLabel('Start date', { exact: true })).toHaveValue('2028-02-28');
  const committed = page.waitForResponse(r => r.url() === f.api + '/api/v1/leave-requests' && r.request().method() === 'POST');
  await page.getByRole('button', { name: 'Submit request', exact: true }).click();
  const response = await committed; expect(response.status()).toBe(200); const id = await response.json();
  const timeline = await page.request.get(f.api + `/api/v1/leave-requests/${id}/timeline`, { headers });
  expect(timeline.status()).toBe(200); const history = await timeline.json();
  expect(history.events.filter((e: { action: string }) => e.action === 'Submit')).toHaveLength(1);
});

test('controlled document/configuration metadata with HTTP preview: keyboard and retained drafts without writes', async ({ page }) => {
  const f = fixture(); let reviewed = false;
  const originalPolicyVersion = randomUUID();
  const draft = { id: randomUUID(), class: 'Ordinary', status: 'Clean', version: randomUUID(), canDownload: false, mediaType: 'image/png' };
  // UI-only metadata has no blob and never goes through Submit. Real binding/scanning is covered separately.
  await page.route('**/api/v1/documents', async route => {
    if (route.request().method() !== 'GET') { await route.continue(); return; }
    await route.fulfill({ json: [draft] });
  });
  await page.route('**/api/v1/leave-types', async route => {
    const response = await route.fetch();
    if (reviewed) { await route.fulfill({ response }); return; }
    const rows = await response.json();
    await route.fulfill({ response, json: rows.map((row: { id: string }) => row.id === f.typeId ? { ...row, policyVersion: originalPolicyVersion } : row) });
  });
  await open(page); const choices = page.getByRole('checkbox');
  await expect(choices.first()).toBeVisible(); await choices.first().check();
  let submissions = 0, documentWrites = 0;
  page.on('request', request => {
    if (request.url() === f.api + '/api/v1/leave-requests' && request.method() === 'POST') submissions++;
    if (request.url().includes('/api/v1/documents') && request.method() !== 'GET') documentWrites++;
  });
  const response = page.waitForResponse(r => r.url().endsWith('/leave-requests/preview') && r.status() === 200);
  await page.getByRole('button', { name: 'Preview request', exact: true }).focus(); await page.keyboard.press('Enter');
  const captured = await response, data = await captured.json(); expect(data.requestedDays).toBe(3);
  expect(Object.keys(captured.request().postDataJSON()).sort()).toEqual(['endDate', 'leaveTypeId', 'startDate']);
  await expect(result(page)).toBeFocused();
  await expect(page.getByRole('region', { name: 'Request preview', exact: true })).toContainText('Pending requests reserve no balance');
  await expect(choices.first()).toBeChecked();
  reviewed = true;
  await page.getByRole('button', { name: 'Refresh and review requirements', exact: true }).click();
  await expect(page.getByText('Current requirements loaded.', { exact: false })).toBeVisible();
  await expect(page.getByText('Current requirements loaded.', { exact: false })).toBeFocused();
  await expect(choices.first()).toBeChecked();
  await expect(page.getByLabel('Start date', { exact: true })).toHaveValue('2028-02-28');
  expect(submissions).toBe(0);
  expect(documentWrites).toBe(0);
});

test('HTTP preview: Manager personal capability and HR-only navigation denial', async ({ page }) => {
  await open(page, 'manager'); await page.getByRole('button', { name: 'Preview request', exact: true }).click();
  await expect(result(page)).toContainText('2028-02-28 through 2028-03-01');
  await page.getByRole('button', { name: 'Sign out', exact: true }).click(); await signIn(page, 'hr');
  await expect(navigation(page).getByRole('link', { name: 'Request leave', exact: true })).toHaveCount(0);
});

test('controlled transport over HTTP preview: stale response cannot relabel changed inputs', async ({ page }) => {
  await open(page); const entered = barrier(), release = barrier(), finished = barrier();
  let held = false;
  await page.route('**/api/v1/leave-requests/preview', async route => {
    if (held) { await route.continue(); return; }
    held = true;
    try { const response = await route.fetch(); entered.resolve(); await release.promise; await route.fulfill({ response }); }
    finally { finished.resolve(); }
  });
  try {
    await page.getByRole('button', { name: 'Preview request', exact: true }).click(); await entered.promise;
    await page.getByLabel('End date', { exact: true }).fill('2028-03-02');
    await expect(page.getByText('Previous preview is outdated.', { exact: false })).toBeVisible();
    await expect(result(page)).toHaveCount(0);
    await page.getByRole('button', { name: 'Preview request', exact: true }).click();
    await expect(result(page)).toContainText('2028-02-28 through 2028-03-02');
    release.resolve(); await finished.promise;
    await expect(result(page)).toContainText('2028-02-28 through 2028-03-02');
    await expect(page.getByLabel('End date', { exact: true })).toHaveValue('2028-03-02');
  } finally { release.resolve(); }
});

test('controlled preview error: explicit retry keeps dates and has mobile/light/dark presentation', async ({ page }) => {
  await open(page); await page.setViewportSize({ width: 390, height: 844 });
  await page.route('**/api/v1/leave-requests/preview', route => route.fulfill({ status: 503, contentType: 'application/problem+json', body: JSON.stringify({ title: 'Preview temporarily unavailable.' }) }), { times: 1 });
  await page.getByRole('button', { name: 'Preview request', exact: true }).click();
  await expect(result(page).getByRole('alert')).toBeVisible();
  await expect(page.getByLabel('Start date', { exact: true })).toHaveValue('2028-02-28');
  await page.getByRole('button', { name: 'Retry preview', exact: true }).click();
  await expect(result(page)).toContainText('2028-02-28 through 2028-03-01');
  for (const scheme of ['light', 'dark'] as const) {
    await page.emulateMedia({ colorScheme: scheme, reducedMotion: 'reduce' });
    await expect(page.locator('html')).toHaveAttribute('data-theme', scheme);
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
    await expect(result(page)).toBeVisible();
  }
});

for (const actor of ['employee', 'hr']) test(`controlled HTTP preview: late completion cannot cross replacement session as ${actor}`, async ({ page }) => {
  await open(page); const entered = barrier(), release = barrier(), finished = barrier();
  await page.route('**/api/v1/leave-requests/preview', async route => {
    try { const response = await route.fetch(); entered.resolve(); await release.promise; await route.fulfill({ response }); }
    finally { finished.resolve(); }
  });
  try {
    await page.getByRole('button', { name: 'Preview request', exact: true }).click(); await entered.promise;
    await page.getByRole('button', { name: 'Sign out', exact: true }).click(); await signIn(page, actor);
    release.resolve(); await finished.promise;
    await expect(result(page)).toHaveCount(0);
    if (actor === 'employee') {
      await navigation(page).getByRole('link', { name: 'Request leave', exact: true }).click();
      await expect(page.getByText('Choose a type and dates, then preview explicitly.', { exact: true })).toBeVisible();
      await expect(page.getByLabel('Start date', { exact: true })).toHaveValue('');
    }
  } finally { release.resolve(); }
});
