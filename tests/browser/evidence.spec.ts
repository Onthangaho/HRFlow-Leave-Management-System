import fs from 'node:fs';
import { test, expect, type Page } from '@playwright/test';
import { safeEvidenceLocations } from './evidence-diagnostics.js';

interface Fixture { password: string; api: string; client: string; typeId: string; employees: Record<string, { email: string }> }
const fixture = () => JSON.parse(fs.readFileSync(process.env.HRFLOW_TEST_STATE!, 'utf8')) as Fixture;

test('evidence diagnostic contract: source coordinates exclude raw error contents', () => {
  const result = safeEvidenceLocations([{ stack: 'private canary payload\n at /unreported/evidence.spec.ts:42:7\n at /unreported/other.ts:9:2' },
    { stack: ' at /unreported/evidence.spec.ts:0:9\n at /unreported/evidence.spec.ts:100001:3' }, {}]);
  expect(result).toEqual(['evidence-diagnostic {"source":{"file":"evidence.spec.ts","line":42,"column":7}}']);
  expect(result.join('')).not.toContain('canary');
  expect(result.join('')).not.toContain('unreported');
});
async function login(page: Page, actor = 'employee') {
  const f = fixture(); await page.goto(f.client + '/login');
  await page.getByLabel('Email', { exact: true }).fill(f.employees[actor].email);
  await page.getByLabel('Password', { exact: true }).fill(f.password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(page.getByRole('navigation', { name: 'Primary', exact: true })).toBeVisible();
}
async function request(page: Page) {
  const f = fixture(); const login = await page.request.post(f.api + '/api/v1/auth/login', { data: { email: f.employees.employee.email, password: f.password } });
  const headers = { Authorization: 'Bearer ' + (await login.json()).accessToken };
  const selector = await (await page.request.get(f.api + '/api/v1/leave-types', { headers })).json();
  const type = selector.find((t: { id: string }) => t.id === f.typeId);
  const date = new Date(); date.setUTCDate(date.getUTCDate() + 16);
  const response = await page.request.post(f.api + '/api/v1/leave-requests', { headers, data: { leaveTypeId: type.id, startDate: date.toISOString(), endDate: date.toISOString(), expectedTypeVersion: type.version, expectedPolicyVersion: type.policyVersion } });
  expect(response.status()).toBe(200); return await response.json() as string;
}
async function editor(page: Page, id: string) {
  // Auth is deliberately memory-only: use the real SPA links rather than a session-destroying reload.
  await page.getByRole('navigation', { name: 'Primary', exact: true }).getByRole('link', { name: 'My leave and balances', exact: true }).click();
  await page.locator(`a[href="/leave-requests/${id}/history"]`).click();
  await expect(page).toHaveURL(new RegExp(`/leave-requests/${id}/history$`));
  const area = page.getByRole('region', { name: 'Later evidence additions', exact: true });
  await area.getByRole('button', { name: 'Add evidence', exact: true }).click();
  await area.getByRole('checkbox', { name: 'Attach to request', exact: true }).first().check();
  return area;
}

test('HTTP evidence with controlled clean fixture: bind, separate history, download and mobile keyboard', async ({ page }) => {
  const id = await request(page); await login(page); const area = await editor(page, id);
  const response = page.waitForResponse(r => r.url().endsWith(`/requests/${id}/additions`) && r.request().method() === 'POST');
  await area.getByRole('button', { name: 'Attach selected evidence', exact: true }).focus(); await page.keyboard.press('Enter');
  expect((await response).status()).toBe(200);
  await expect(area.getByText(/Evidence addition confirmed/)).toBeVisible(); await expect(area.getByText(/Later addition · 1 document/)).toBeVisible();
  await expect(area.getByText(/Evidence addition confirmed/)).toBeFocused();
  await expect(page.locator('html')).toHaveAttribute('data-theme', 'light');
  const documents = page.getByRole('region', { name: 'Supporting documents', exact: true });
  await expect(documents.getByRole('button', { name: 'Download', exact: true })).toHaveCount(1);
  const download = page.waitForEvent('download'); await documents.getByRole('button', { name: 'Download', exact: true }).click();
  const downloaded = await download; expect(downloaded.suggestedFilename()).toBe('supporting-document.png'); await downloaded.delete();
  await page.setViewportSize({ width: 390, height: 844 }); await page.emulateMedia({ colorScheme: 'dark', reducedMotion: 'reduce' });
  await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark');
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await expect(page.getByRole('heading', { name: 'Recorded lifecycle', exact: true })).toBeVisible();
});

test('controlled transport over real HTTP bind: lost response recovers one committed addition', async ({ page }) => {
  const id = await request(page); await login(page); let calls = 0;
  await page.route(`**/documents/requests/${id}/additions`, async route => {
    if (route.request().method() !== 'POST') { await route.continue(); return; }
    calls++; if (calls !== 1) { await route.continue(); return; }
    const response = await route.fetch(); expect(response.status()).toBe(200);
    await route.fulfill({ status: 503, json: { title: 'Synthetic lost acknowledgement' } });
  });
  const area = await editor(page, id); await area.getByRole('button', { name: 'Attach selected evidence', exact: true }).click();
  await expect(area.getByRole('alert')).toBeVisible(); await expect(area.getByText('The attempted selection is retained for explicit recovery. Nothing is automatically resubmitted.')).toBeVisible();
  await area.getByRole('button', { name: 'Recover same addition', exact: true }).click();
  await expect(area.getByText(/Evidence addition confirmed/)).toBeVisible(); expect(calls).toBe(2);
  await expect(area.getByText(/Later addition · 1 document/)).toHaveCount(1);
});

test('HTTP upload without scanner: inaccessible draft cannot be selected or bound', async ({ page }) => {
  const id = await request(page); await login(page); const area = await editor(page, id);
  const png = Buffer.from(await page.evaluate(() => {
    const canvas = document.createElement('canvas'); canvas.width = 8; canvas.height = 8;
    canvas.getContext('2d')!.fillRect(0, 0, 8, 8); return canvas.toDataURL('image/png').split(',')[1];
  }), 'base64');
  const response = page.waitForResponse(r => r.url().endsWith('/documents') && r.request().method() === 'POST');
  await area.getByLabel('Choose document', { exact: true }).setInputFiles({ name: 'synthetic.png', mimeType: 'image/png', buffer: png });
  expect((await response).status()).toBe(200);
  const row = area.locator('li').filter({ hasText: 'ScanUnavailable' }); await expect(row).toHaveCount(1);
  await expect(row.getByRole('checkbox')).toHaveCount(0); await expect(row.getByRole('button', { name: 'Download', exact: true })).toHaveCount(0);
  await expect(row.getByRole('button', { name: 'Retry scan', exact: true })).toBeVisible();
});

test('controlled eligibility refresh failure preserves visible reviewed selection on explicit retry', async ({ page }) => {
  const id = await request(page); await login(page); const area = await editor(page, id);
  let fail = true;
  await page.route(`**/documents/requests/${id}/additions`, async route => {
    if (route.request().method() === 'GET' && fail) { await route.fulfill({ status: 503, json: { title: 'Synthetic refresh failure' } }); return; }
    await route.continue();
  });
  await area.getByRole('button', { name: 'Refresh additions', exact: true }).click(); await expect(area.getByRole('alert')).toBeVisible();
  await expect(area.getByRole('checkbox')).toHaveCount(0); fail = false;
  await area.getByRole('button', { name: 'Refresh additions', exact: true }).click();
  await expect(area.getByRole('checkbox', { name: 'Attach to request', exact: true }).first()).toBeChecked();
  await area.getByRole('button', { name: 'Attach selected evidence', exact: true }).click(); await expect(area.getByText(/Evidence addition confirmed/)).toBeVisible();
});

for (const actor of ['employee', 'hr']) test(`controlled evidence response cannot cross replacement session as ${actor}`, async ({ page }) => {
  const id = await request(page); await login(page);
  let entered!: () => void; const started = new Promise<void>(r => { entered = r; });
  let resume!: () => void; const barrier = new Promise<void>(r => { resume = r; });
  let finished!: () => void; const ended = new Promise<void>(r => { finished = r; });
  await page.route(`**/documents/requests/${id}/additions`, async route => {
    if (route.request().method() !== 'POST') { await route.continue(); return; }
    const response = await route.fetch(); entered(); await barrier;
    await route.fulfill({ response }).catch(() => {}); finished();
  });
  try {
    const area = await editor(page, id); await area.getByRole('button', { name: 'Attach selected evidence', exact: true }).click(); await started;
    await page.getByRole('button', { name: 'Sign out', exact: true }).click(); await login(page, actor); resume(); await ended;
    await expect(page.getByText(/Evidence addition confirmed/)).toHaveCount(0);
    await expect(page.getByRole('region', { name: 'Later evidence additions', exact: true })).toHaveCount(0);
  } finally { resume(); }
});
