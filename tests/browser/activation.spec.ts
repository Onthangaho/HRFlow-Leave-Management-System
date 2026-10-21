import fs from 'node:fs';
import { test, expect, type Page } from '@playwright/test';
const fixture = () => JSON.parse(fs.readFileSync(process.env.HRFLOW_TEST_STATE!, 'utf8'));
const nav = (p: Page) => p.getByRole('navigation', { name: 'Primary', exact: true });
async function login(p: Page, actor: string) { const f = fixture(); await p.getByLabel('Email', { exact: true }).fill(f.employees[actor].email); await p.getByLabel('Password', { exact: true }).fill(f.password); await p.getByRole('button', { name: 'Sign in', exact: true }).click(); await expect(nav(p)).toBeVisible(); }
async function directory(p: Page) { await p.goto(fixture().client + '/'); await login(p, 'hr'); await nav(p).getByRole('link', { name: 'Employee management', exact: true }).click(); await expect(p.getByRole('region', { name: 'Directory results', exact: true })).toHaveAttribute('aria-busy', 'false'); }
async function fill(p: Page, suffix: string) { await p.getByRole('button', { name: 'New Employee', exact: true }).click(); await p.getByLabel('Employee number', { exact: true }).fill('ACT-' + suffix); await p.getByLabel('Confirmed employment start date', { exact: true }).fill('2020-01-01'); await p.getByLabel('Full name', { exact: true }).fill('Synthetic invitation ' + suffix); await p.getByLabel('Email', { exact: true }).fill('act-' + suffix + '@synthetic.invalid'); await p.locator('#employee-department').selectOption({ label: 'Synthetic Engineering' }); await p.getByRole('checkbox', { name: 'Employee', exact: true }).check(); }
async function search(p: Page, suffix: string) { await p.getByLabel('Search employees', { exact: true }).fill('act-' + suffix + '@synthetic.invalid'); const response = p.waitForResponse(r => new URL(r.url()).pathname.endsWith('/employees') && r.request().method() === 'GET' && new URL(r.url()).searchParams.get('search') === 'act-' + suffix + '@synthetic.invalid'); await p.getByRole('button', { name: 'Apply filters', exact: true }).click(); expect((await response).status()).toBe(200); await expect(p.getByRole('region', { name: 'Directory results', exact: true })).toHaveAttribute('aria-busy', 'false'); }
function barrier() { let resolve!: () => void; const promise = new Promise<void>(r => { resolve = r; }); return { promise, resolve }; }
// Retire only this file's synthetic accounts through current HR authorization. Shared attention
// fixtures must keep their original active-schedule totals; no database deletion/bypass is used.
test.afterEach(async ({ request }) => {
    const f = fixture();
    const signedIn = await request.post(f.api + '/api/v1/auth/login', { data: { email: f.employees.hr.email, password: f.password } });
    expect(signedIn.status()).toBe(200);
    const headers = { Authorization: 'Bearer ' + (await signedIn.json()).accessToken };
    const response = await request.get(f.api + '/api/v1/employees?status=Active&search=ACT-&pageSize=100', { headers });
    expect(response.status()).toBe(200);
    for (const employee of (await response.json()).items) {
        if (!['ACT-recovery', 'ACT-late-hr', 'ACT-late-employee'].map(n => n.toUpperCase()).includes(employee.employeeNumber)) continue;
        const removed = await request.patch(f.api + '/api/v1/employees/' + employee.id + '/deactivate', { headers, data: { expectedVersion: employee.version, reason: 'Synthetic activation fixture cleanup' } });
        expect(removed.status()).toBe(200);
    }
});
test('controlled creation response: committed account keeps draft after interruption and recovers by explicit resend on mobile', async ({ page }) => {
    await directory(page);
    await page.setViewportSize({ width: 390, height: 844 });
    await fill(page, 'recovery');
    let creates = 0;
    await page.route('**/api/v1/employees', async (route) => { if (route.request().method() !== 'POST')
        return route.continue(); creates++; const result = await route.fetch(); expect(result.status()).toBe(201); await route.abort('failed'); });
    await page.getByRole('button', { name: 'Save Employee', exact: true }).click();
    await expect(page.getByText('If the response was interrupted, an account may already be committed.', { exact: false })).toBeVisible();
    await expect(page.getByLabel('Full name', { exact: true })).toHaveValue('Synthetic invitation recovery');
    expect(creates).toBe(1);
    await page.getByRole('button', { name: 'Cancel', exact: true }).click();
    await search(page, 'recovery');
    await expect(page.locator('tbody tr')).toHaveCount(1);
    await page.getByRole('button', { name: 'Resend invitation', exact: true }).click();
    await expect(page.getByRole('dialog')).toBeVisible();
    const response = page.waitForResponse(r => new URL(r.url()).pathname.endsWith('/activation/resend'));
    await page.getByRole('dialog').getByRole('button', { name: 'Resend invitation', exact: true }).click();
    expect((await response).status()).toBe(200);
    await expect(page.getByRole('dialog')).toHaveCount(0);
    await expect(page.getByRole('status').filter({ hasText: 'Private development invitation is ready for pickup.' })).toBeVisible();
    expect(creates).toBe(1);
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
});
for (const actor of ['hr', 'employee'])
    test('controlled resend response: late acknowledgement cannot cross replacement login as ' + actor, async ({ page }) => {
        await directory(page);
        await fill(page, 'late-' + actor);
        await page.getByRole('button', { name: 'Save Employee', exact: true }).click();
        await expect(page.getByRole('heading', { name: 'New Employee', exact: true })).toHaveCount(0);
        await search(page, 'late-' + actor);
        const entered = barrier(), release = barrier(), handled = barrier();
        await page.route('**/activation/resend', async (route) => { const response = await route.fetch(); entered.resolve(); await release.promise; try {
            await route.fulfill({ response });
        }
        finally {
            handled.resolve();
        } });
        try {
            await page.getByRole('button', { name: 'Resend invitation', exact: true }).click();
            await page.getByRole('dialog').getByRole('button', { name: 'Resend invitation', exact: true }).click();
            await entered.promise;
            await expect(page.getByRole('dialog')).toBeVisible();
            await page.goBack();
            await expect(page.getByRole('dialog')).toHaveCount(0);
            await page.getByRole('button', { name: 'Sign out', exact: true }).click();
            await expect(page).toHaveURL(/\/login$/);
            await login(page, actor);
            await nav(page).getByRole('link', { name: 'Overview', exact: true }).click();
            release.resolve();
            await handled.promise;
            await expect(page.getByRole('region', { name: actor === 'hr' ? 'HR workspace' : 'Personal leave dashboard', exact: true })).toBeVisible();
            await expect(page.getByRole('main').getByText('Synthetic invitation late-' + actor, { exact: true })).toHaveCount(0);
            await expect(page.getByRole('status').filter({ hasText: 'Private development invitation is ready for pickup.' })).toHaveCount(0);
        }
        finally {
            release.resolve();
        }
    });
test('HTTP activation page: navigation and prefetch never post or establish a password', async ({ page }) => { let posts = 0; page.on('request', r => { if (new URL(r.url()).pathname.endsWith('/auth/activation') && r.method() === 'POST')
    posts++; }); const dummy = Array.from({ length: 64 }, () => Math.floor(Math.random() * 16).toString(16)).join(''); await page.goto(fixture().client + '/activate#token=' + dummy); await expect(page.getByRole('heading', { name: 'Activate your account', exact: true })).toBeVisible(); await expect(page).toHaveURL(/\/activate$/); expect(posts).toBe(0); await expect(page.getByLabel('New password', { exact: true })).toHaveValue(''); await expect(page.getByRole('link', { name: 'Go to login', exact: true })).toHaveCount(0); });
