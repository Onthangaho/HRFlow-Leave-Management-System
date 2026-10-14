import fs from 'node:fs';
import path from 'node:path';
import os from 'node:os';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import { pathToFileURL } from 'node:url';
import { DatabaseSync } from 'node:sqlite';

/** Run existing product workflows while withholding secret-bearing browser call logs. */
async function rehearsal() {
const repo = path.resolve(import.meta.dirname, '../..');
const root = path.resolve(process.argv[2] ?? '');
if (!process.argv[2] || !path.basename(root).startsWith('hrflow-pitch-') || root.startsWith(repo + path.sep)) throw Error('Use the marked private pitch destination.');
const config = JSON.parse(fs.readFileSync(path.join(root, 'private-state.json'), 'utf8'));
assert.equal(config.marker, 'HRFlowSyntheticPitchV1'); assert.equal(config.root, root);
assert.equal(config.api, 'http://127.0.0.1:5540'); assert.equal(config.client, 'http://127.0.0.1:5541');
assert.ok(config.pid && config.apiPid, 'Start the owned demonstration first.');
assert.ok(!fs.existsSync(path.join(root, 'rehearsal-result.private.json')), 'Use a fresh destination for a complete rehearsal.');
const screenshotRoot = path.join(repo, 'docs/verification/screenshots/pitch-rehearsal');
const { chromium } = await import(pathToFileURL(path.join(repo, 'tests/browser/node_modules/playwright/index.mjs')));
const { expect } = await import(pathToFileURL(path.join(repo, 'tests/browser/node_modules/@playwright/test/index.mjs')));
const browser = await chromium.launch({ channel: process.env.HRFLOW_BROWSER_CHANNEL || undefined });
const context = await browser.newContext({ viewport: { width: 1366, height: 900 }, reducedMotion: 'reduce', acceptDownloads: true });
const page = await context.newPage(); page.setDefaultTimeout(15000);
const nav = () => page.getByRole('navigation', { name: 'Primary', exact: true });
const records = {};
let phase = "HR onboarding";

/** Credentials are used only in the actual login form, never printed or captured. */
async function login(email, password) {
  await page.goto(config.client + '/login'); await page.getByLabel('Email', { exact: true }).fill(email);
  await page.getByLabel('Password', { exact: true }).fill(password); await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(nav().getByRole('link', { name: 'Overview', exact: true })).toBeVisible();
  await nav().getByRole('link', { name: 'Overview', exact: true }).click();
}

/** Change accounts without page reload, exercising the existing session boundary. */
async function switchAccount(email, password) {
  await page.getByRole('button', { name: 'Sign out', exact: true }).click();
  await page.getByLabel('Email', { exact: true }).fill(email); await page.getByLabel('Password', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(nav().getByRole('link', { name: 'Overview', exact: true })).toBeVisible();
  await nav().getByRole('link', { name: 'Overview', exact: true }).click();
}

/** Only protected synthetic pages are screenshot eligible; never activation/login or URL/browser chrome. */
async function shot(name, mobile = false) {
  assert.ok(!/activation|login/.test(new URL(page.url()).pathname));
  fs.mkdirSync(screenshotRoot, { recursive: true });
  if (mobile) {
    await page.setViewportSize({ width: 390, height: 844 });
    if (name === 'hr-trends-mobile') await page.getByLabel('Period trend: summed Approved request-days', { exact: true }).scrollIntoViewIfNeeded();
    if (name === 'team-mobile') await page.getByText('Avery Example', { exact: true }).first().scrollIntoViewIfNeeded();
  }
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth + 1), { timeout: 5000 }).toBe(true);
  await page.mouse.move(0, 0);
  await page.screenshot({ path: path.join(screenshotRoot, name + '.png'), fullPage: name.startsWith('timeline-') });
  if (mobile) await page.setViewportSize({ width: 1366, height: 900 });
}

/** Safe API verification uses generated credentials without dumping tokens or response bodies. */
async function apiLogin(email, password) {
  const response = await context.request.post(config.api + '/api/v1/auth/login', { data: { email, password } });
  assert.equal(response.status(), 200); return (await response.json()).accessToken;
}

try {
  await login('hr.administrator@hrflow.local', config.password);
  await nav().getByRole('link', { name: 'CSV onboarding', exact: true }).click();
  await page.getByLabel('UTF-8 CSV file').setInputFiles(path.join(root, 'fixtures/synthetic-onboarding.csv'));
  await page.getByRole('button', { name: 'Inspect columns', exact: true }).click();
  await expect(page.getByLabel('FirstName column mapping', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Validate preview', exact: true }).click();
  await expect(page.getByText('Ready', { exact: true })).toBeVisible();
  await shot('onboarding-desktop');
  const confirmResponse = page.waitForResponse(response => response.url().endsWith('/confirm') && response.request().method() === 'POST');
  await page.getByRole('button', { name: /Confirm selected/ }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Create 1 accounts', exact: true }).click();
  assert.equal((await confirmResponse).status(), 200);
  await expect(page.getByText('Created', { exact: true })).toBeVisible();
  console.log('PASS real browser CSV mapping, preview and confirmation.');

  phase = "Private activation";
  // Private Development pickup is not email delivery. No invitation URL enters screenshots, logs or output.
  const pickupBase = process.platform === 'win32' ? process.env.LOCALAPPDATA : path.join(os.homedir(), '.local/share');
  const pickup = path.join(pickupBase, 'HRFlow/ActivationPickup', config.pickupProfile);
  const invitations = fs.readdirSync(pickup).map(file => fs.readFileSync(path.join(pickup, file), 'utf8').split(/\r?\n/));
  const invitation = invitations.find(lines => lines[0] === 'avery@example.invalid'); assert.ok(invitation);
  await page.getByRole('button', { name: 'Sign out', exact: true }).click();
  await page.goto(invitation[1]); await page.getByLabel('New password', { exact: true }).fill(config.employeePassword);
  await page.getByLabel('Confirm password', { exact: true }).fill(config.employeePassword);
  await page.getByRole('button', { name: 'Activate account', exact: true }).click();
  await expect(page.getByText('Your password has been established. Sign in normally to continue.', { exact: true })).toBeVisible();
  console.log('PASS real private pickup -> anonymous activation form; no automatic login.');

  phase = 'HR policy configuration';
  await login('hr.administrator@hrflow.local', config.password);
  await nav().getByRole('link', { name: 'Leave policies/types', exact: true }).click();
  await page.getByRole('button', { name: 'Policies', exact: true }).click();
  await page.getByRole('button', { name: 'New policy', exact: true }).click();
  await page.getByLabel('Policy name', { exact: true }).fill('Pitch company policy');
  await page.getByLabel('Entitlement (calendar days)', { exact: true }).fill('20');
  const policyResponse = page.waitForResponse(r => r.url().endsWith('/management/leave-policies') && r.request().method() === 'POST');
  await page.getByRole('button', { name: 'Save changes', exact: true }).click();
  const policy = await (await policyResponse).json(); records.policyId = policy.id;
  await page.getByRole('button', { name: 'Leave types', exact: true }).click();
  for (const mode of ['Optional', 'Required']) {
    await page.getByRole('button', { name: 'New leave type', exact: true }).click();
    phase = 'type name';
    await page.getByLabel('Leave type name', { exact: true }).fill('Pitch ordinary ' + mode.toLowerCase());
    phase = 'type policy select';
    await page.getByLabel('Policy', { exact: true }).selectOption(policy.id);
    phase = 'description mode';
    await page.getByLabel('Description requirement').selectOption('Optional');
    phase = 'evidence mode';
    await page.getByLabel('Evidence requirement').selectOption(mode);
    await page.getByLabel('Configured evidence class').selectOption('Ordinary');
    await page.getByLabel('Submission instructions (optional)', { exact: true }).fill('Company demonstration: attach harmless ordinary evidence. Do not include diagnoses.');
    const typeResponse = page.waitForResponse(r => r.url().endsWith('/management/leave-types') && r.request().method() === 'POST');
    await page.getByRole('button', { name: 'Save changes', exact: true }).click();
    const type = await (await typeResponse).json(); records[mode.toLowerCase() + 'TypeId'] = type.id;
    await expect(page.getByRole('button', { name: 'New leave type', exact: true })).toBeEnabled();
  }
  await shot('requirements-desktop'); console.log('PASS HR actual optional/required Ordinary configuration forms.');

  phase = 'Employee clean upload and submission';
  await switchAccount('avery@example.invalid', config.employeePassword);
  await nav().getByRole('link', { name: 'Request leave', exact: true }).click();
  await page.getByLabel('Leave type', { exact: true }).selectOption(records.requiredTypeId);
  await page.getByLabel('Start date', { exact: true }).fill(config.date); await page.getByLabel('End date', { exact: true }).fill(config.date);
  await page.getByLabel('Description (optional)', { exact: true }).fill('Synthetic pitch absence; no private medical information.');
  await page.getByLabel('Evidence class', { exact: true }).selectOption('Ordinary');
  await page.getByLabel('Choose document', { exact: true }).setInputFiles(path.join(root, 'fixtures/synthetic-evidence.pdf'));
  await expect(page.getByText(/Document .*Ordinary.*Clean/)).toBeVisible({ timeout: 45000 });
  const attach = page.getByLabel('Attach to request', { exact: true }); if (!(await attach.isChecked())) await attach.check();
  await shot('evidence-desktop'); await shot('evidence-mobile', true);
  const submitResponse = page.waitForResponse(r => r.url().endsWith('/api/v1/leave-requests') && r.request().method() === 'POST');
  await page.getByRole('button', { name: 'Submit request', exact: true }).click();
  const submitted = await submitResponse; assert.equal(submitted.status(), 200); records.requestId = await submitted.json();
  await expect(page.getByRole('heading', { name: 'Request submitted', exact: true })).toBeVisible();
  console.log('PASS actual clean ClamAV upload, explicit binding and required-evidence submission.');

  await switchAccount('hr.administrator@hrflow.local', config.password);
  await nav().getByRole('link', { name: 'Pending leave monitoring', exact: true }).click();
  await expect(page.locator(`a[href="/leave-requests/${records.requestId}/history"]`)).toBeVisible();
  await expect(page.getByRole('button', { name: /Approve|Reject/ })).toHaveCount(0); await expect(page.getByRole('button', { name: 'Refresh monitoring', exact: true })).toBeEnabled(); await shot('monitoring-desktop');
  const hrToken = await apiLogin('hr.administrator@hrflow.local', config.password);
  assert.equal((await context.request.post(config.api + `/api/v1/leave-requests/${records.requestId}/approve`, { headers: { Authorization: 'Bearer ' + hrToken }, data: {} })).status(), 403);

  await switchAccount('manager@hrflow.local', config.password);
  await nav().getByRole('link', { name: 'Approval queue', exact: true }).click();
  const row = page.locator('article').filter({ has: page.locator(`a[href="/leave-requests/${records.requestId}/history"]`) });
  await row.getByRole('button', { name: 'Approve', exact: true }).click();
  await page.getByRole('dialog').getByRole('textbox').fill('Synthetic coverage reviewed.');
  await shot('decision-desktop');
  await page.getByRole('dialog').getByRole('button', { name: 'Confirm approve', exact: true }).click(); await expect(row).toHaveCount(0);
  await nav().getByRole('link', { name: 'Team leave', exact: true }).click(); await expect(page.getByText('Avery Example', { exact: true }).first()).toBeVisible();
  await shot('team-desktop'); await shot('team-mobile', true);
  console.log('PASS assigned Manager note decision and updated read-only team view.');

  await nav().getByRole('link', { name: 'Request leave', exact: true }).click();
  await page.getByLabel('Leave type', { exact: true }).selectOption(records.optionalTypeId);
  await page.getByLabel('Start date', { exact: true }).fill(config.date); await page.getByLabel('End date', { exact: true }).fill(config.date);
  const selfSubmitted = page.waitForResponse(r => r.url().endsWith('/api/v1/leave-requests') && r.request().method() === 'POST');
  await page.getByRole('button', { name: 'Submit request', exact: true }).click(); const selfResponse = await selfSubmitted;
  assert.equal(selfResponse.status(), 200); records.managerRequestId = await selfResponse.json();
  const managerToken = await apiLogin('manager@hrflow.local', config.password);
  assert.equal((await context.request.post(config.api + `/api/v1/leave-requests/${records.managerRequestId}/approve`, { headers: { Authorization: 'Bearer ' + managerToken }, data: {} })).status(), 403);
  console.log('PASS real Manager personal submission; own API approval denied; HR API approval denied.');

  phase = 'Owner result and notification';
  await switchAccount('avery@example.invalid', config.employeePassword);
  await nav().getByRole('link', { name: 'My leave and balances', exact: true }).click();
  await expect(page.locator('article').filter({ has: page.locator(`a[href="/leave-requests/${records.requestId}/history"]`) }).getByText('Approved', { exact: true })).toBeVisible();
  await page.locator(`a[href="/leave-requests/${records.requestId}/history"]`).click();
  await expect(page.getByText('Manager note: Synthetic coverage reviewed.', { exact: true })).toBeVisible(); await expect(page.getByText(/Document .*Ordinary.*Clean/)).toBeVisible(); await shot('timeline-desktop'); await shot('timeline-mobile', true);
  await nav().getByRole('link', { name: 'Notifications', exact: true }).click();
  phase = 'Owner inbox refresh';
  await expect.poll(async () => {
    const refresh = page.getByRole('button', { name: 'Refresh', exact: true });
    if (await refresh.isEnabled()) await refresh.click();
    return page.getByText('Your leave request was approved.', { exact: true }).count();
  }, { timeout: 20000 }).toBe(1);
  await shot('notifications-desktop');

  phase = 'HR reports and export';
  await switchAccount('hr.administrator@hrflow.local', config.password);
  await nav().getByRole('link', { name: 'Overview', exact: true }).click(); await expect(page.getByText('Your HR workspace', { exact: true })).toBeVisible(); await shot('hr-desktop'); await shot('hr-mobile', true);
  await page.getByText('Period trend: summed Approved request-days', { exact: true }).scrollIntoViewIfNeeded();
  await expect(page.getByLabel('Period trend: summed Approved request-days', { exact: true }).locator('svg').first()).toBeVisible();
  await shot('hr-trends-desktop'); await shot('hr-trends-mobile', true);
  await nav().getByRole('link', { name: 'Department leave reports', exact: true }).click();
  const downloaded = page.waitForEvent('download'); await page.getByRole('button', { name: 'Export CSV', exact: true }).click();
  const csv = fs.readFileSync(await (await downloaded).path(), 'utf8'); assert.ok(csv.includes('Approved')); assert.ok(!csv.includes('Synthetic coverage reviewed.'));
  console.log('PASS owner history, audit timeline, notifications, HR workspace/report and actual safe CSV download.');

  phase = 'Persisted-row evidence';
  const db = new DatabaseSync(path.join(root, 'pitch.sqlite'), { readOnly: true });
  const request = db.prepare('SELECT Status, EmployeeId, ProcessedById FROM LeaveRequests WHERE lower(Id)=lower(?)').get(records.requestId);
  assert.equal(request.Status, 1);
  const audits = db.prepare('SELECT Action, ActorId, DecisionNote FROM AuditEntries WHERE lower(LeaveRequestId)=lower(?) ORDER BY Timestamp,Id').all(records.requestId);
  assert.equal(audits.length, 2); assert.deepEqual(audits.map(a => a.Action), ['Submit', 'Approve']); assert.equal(audits[0].ActorId, request.EmployeeId); assert.equal(audits[1].ActorId, request.ProcessedById); assert.equal(audits[1].DecisionNote, 'Synthetic coverage reviewed.');
  const documents = db.prepare('SELECT Id,Status,Class,Checksum FROM SupportingDocuments WHERE lower(RequestId)=lower(?)').all(records.requestId);
  assert.equal(documents.length, 1); assert.equal(documents[0].Status, 'Clean'); assert.equal(documents[0].Class, 'Ordinary');
  const notifications = db.prepare('SELECT e.Kind,e.RecipientId FROM LeaveNotifications n JOIN LeaveNotificationEvents e ON e.Id=n.EventId WHERE lower(e.RequestId)=lower(?)').all(records.requestId);
  assert.equal(notifications.length, 2); assert.equal(notifications.find(n => n.Kind === 'Approve').RecipientId, request.EmployeeId); assert.equal(notifications.find(n => n.Kind === 'Submit').RecipientId, request.ProcessedById);
  const self = db.prepare('SELECT Status FROM LeaveRequests WHERE lower(Id)=lower(?)').get(records.managerRequestId); assert.equal(self.Status, 0);
  assert.equal(db.prepare('SELECT count(*) n FROM AuditEntries WHERE lower(LeaveRequestId)=lower(?)').get(records.managerRequestId).n, 1);
  const blob = fs.readFileSync(path.join(root, 'private/documents/clean', documents[0].Id.replaceAll('-', '').toLowerCase() + '.blob'));
  assert.equal(crypto.createHash('sha256').update(blob).digest('hex').toUpperCase(), documents[0].Checksum.toUpperCase());
  assert.deepEqual(blob, fs.readFileSync(path.join(root, 'fixtures/synthetic-evidence.pdf'))); db.close();
  fs.writeFileSync(path.join(root, 'rehearsal-result.private.json'), JSON.stringify(records), { mode: 0o600 });
  console.log('PASS persisted owner/decision actors, Approved state, exactly Submit+Approve audits, note, clean Ordinary binding and delivered notifications.');
} catch {
  console.error(`Rehearsal failed during ${phase}; private call logs and payloads withheld.`);
 process.exitCode = 1;
} finally { await context.close(); await browser.close(); }

}
rehearsal().catch(() => { console.error("Private rehearsal prerequisites failed; no call logs or secret paths reported."); process.exitCode = 1; });
