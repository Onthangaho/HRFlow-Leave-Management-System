import fs from 'node:fs';
import { DatabaseSync } from 'node:sqlite';
import { test, expect, type Page } from '@playwright/test';

interface Fixture {
  api: string; client: string; password: string; typeId: string;
  employees: Record<string, { id: string; email: string }>;
  authRegression: { expiredAccessToken: string; database: string };
}
const fixture = () => JSON.parse(fs.readFileSync(process.env.HRFLOW_TEST_STATE!, 'utf8')) as Fixture;
function barrier() { let resolve!: () => void; const promise = new Promise<void>(done => { resolve = done; }); return { promise, resolve }; }
const navigation = (page: Page) => page.getByRole('navigation', { name: 'Primary', exact: true });

async function signIn(page: Page, actor = 'employee', waitForOverview = true) {
  const f = fixture();
  await page.getByLabel('Email', { exact: true }).fill(f.employees[actor].email);
  await page.getByLabel('Password', { exact: true }).fill(f.password);
  const response = page.waitForResponse(r => r.url() === f.api + '/api/v1/auth/login' && r.status() === 200);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  const pair = await (await response).json();
  await expect(navigation(page)).toBeVisible();
  if (waitForOverview) {
    if (actor === 'hr') await expect(page.getByText('HR snapshot loaded.', { exact: true })).toBeVisible();
    else await expect(page.getByRole('button', { name: 'Refresh dashboard', exact: true })).toBeEnabled();
  }
  return pair.accessToken as string;
}
async function login(page: Page) { await page.goto(fixture().client + '/login'); return signIn(page); }

function refreshRows() {
  const f = fixture(), db = new DatabaseSync(f.authRegression.database, { readOnly: true });
  try {
    db.exec('PRAGMA busy_timeout=3000');
    // The descriptor identifies an Employee profile; refresh ownership is its separate linked Identity account.
    const rows = db.prepare('SELECT r.RevokedAtUtc FROM RefreshTokens r JOIN Employees e ON e.IdentityUserId=r.UserId WHERE e.Id=?')
      .all(f.employees.employee.id.toUpperCase());
    return { total: rows.length, revoked: rows.filter(r => r.RevokedAtUtc !== null).length };
  } finally { db.close(); }
}

function businessCounts() {
  const db = new DatabaseSync(fixture().authRegression.database, { readOnly: true });
  try {
    db.exec('PRAGMA busy_timeout=3000');
    return JSON.stringify({ requests: db.prepare('SELECT count(*) AS n FROM LeaveRequests').get()!.n,
      audits: db.prepare('SELECT count(*) AS n FROM AuditEntries').get()!.n });
  } finally { db.close(); }
}

async function openRequest(page: Page) { await navigation(page).getByRole('link', { name: 'Request leave', exact: true }).click(); }

/** Replaces only the initiating bearer, including cancelled mount attempts; every 401 comes from real middleware. */
async function rejectExpired(page: Page, paths: string[], initiatingToken: string) {
  const f = fixture(), remaining = new Set(paths), rejected = barrier(); let count = 0;
  page.on('requestfinished', async request => {
    const key = new URL(request.url()).pathname.replace('/api/v1', '');
    if (paths.includes(key) && (await request.response())?.status() === 401) {
      remaining.delete(key); if (remaining.size === 0) rejected.resolve();
    }
  });
  page.on('requestfailed', request => {
    if (paths.includes(new URL(request.url()).pathname.replace('/api/v1', ''))) console.log('token-refresh-stage request-failed');
  });
  await page.route(f.api + '/api/v1/**', async route => {
    const url = new URL(route.request().url()); const key = url.pathname.replace('/api/v1', '');
    if (!paths.includes(key) || route.request().headers().authorization !== 'Bearer ' + initiatingToken) { await route.continue(); return; }
    const response = await route.fetch({ headers: { ...route.request().headers(), authorization: 'Bearer ' + f.authRegression.expiredAccessToken } });
    expect(response.status()).toBe(401); count++;
    console.log('token-refresh-stage expired-rejected');
    await route.fulfill({ response });
  });
  return { rejected, count: () => count };
}

test('real bearer HTTP: concurrent expired reads share one rotating refresh and eligible retries succeed', async ({ page }) => {
  const f = fixture(), token = await login(page); const before = refreshRows(); const expired = await rejectExpired(page, ['/documents', '/leave-types'], token);
  const entered = barrier(), release = barrier(); let refreshes = 0;
  await page.route('**/auth/refresh', async route => {
    refreshes++; console.log('token-refresh-stage refresh-entered'); entered.resolve(); await release.promise;
    const response = await route.fetch(); expect(response.status()).toBe(200); await route.fulfill({ response });
  });
  const documents = page.waitForResponse(r => new URL(r.url()).pathname === '/api/v1/documents' && r.status() === 200);
  const types = page.waitForResponse(r => new URL(r.url()).pathname === '/api/v1/leave-types' && r.status() === 200);
  try {
    await openRequest(page); console.log('token-refresh-stage request-opened'); await entered.promise; await expired.rejected.promise; release.resolve();
    expect((await documents).status()).toBe(200); expect((await types).status()).toBe(200);
    await page.getByLabel('Leave type', { exact: true }).selectOption(f.typeId);
  }
  finally { release.resolve(); }
  expect(refreshes).toBe(1); expect(expired.count()).toBeGreaterThanOrEqual(2);
  expect(refreshRows()).toEqual({ total: before.total + 1, revoked: before.revoked + 1 });
});

for (const outcome of ['accepted', 'invalid'] as const) for (const replacement of ['logout', 'same', 'different'] as const)
  test(`real bearer HTTP: delayed refresh ${outcome} cannot affect ${replacement} session boundary`, async ({ page }) => {
    const token = await login(page); await rejectExpired(page, ['/leave-requests/dashboard'], token);
    const entered = barrier(), release = barrier(), finished = barrier(); let refreshes = 0;
    await page.route('**/auth/refresh', async route => {
      refreshes++;
      const response = await route.fetch(outcome === 'invalid' ? { postData: JSON.stringify({ refreshToken: 'synthetic-invalid-refresh' }) } : {});
      expect(response.status()).toBe(outcome === 'accepted' ? 200 : 401); entered.resolve(); await release.promise;
      try { await route.fulfill({ response }); } catch { /* The initiating browser request may be cancelled by logout. */ }
      finally { finished.resolve(); }
    });
    try {
      await page.getByRole('button', { name: 'Refresh dashboard', exact: true }).click();
      await entered.promise;
      await page.getByRole('button', { name: 'Sign out', exact: true }).click(); await expect(page).toHaveURL(/\/login$/);
      let fresh: string | undefined;
      if (replacement !== 'logout') fresh = await signIn(page, replacement === 'same' ? 'employee' : 'hr', false);
      release.resolve(); await finished.promise;
      if (replacement === 'logout') {
        await expect(page).toHaveURL(/\/login$/); await expect(navigation(page)).toHaveCount(0);
        fresh = await signIn(page);
      }
      const isHr = replacement === 'different';
      const refreshButton = page.getByRole('button', { name: isHr ? 'Refresh HR workspace' : 'Refresh dashboard', exact: true });
      await expect(refreshButton).toBeEnabled();
      // Pair the assertion with the specific triggered request, not an older in-flight profile response.
      const requested = page.waitForRequest(r => new URL(r.url()).pathname === (isHr ? '/api/v1/reports/hr-workspace' : '/api/v1/leave-requests/dashboard'));
      await refreshButton.click(); const observed = await requested; const response = await observed.response();
      expect(response?.status()).toBe(200); expect(observed.headers().authorization === 'Bearer ' + fresh).toBe(true); expect(refreshes).toBe(1);
      await expect(navigation(page)).toBeVisible();
    } finally { release.resolve(); }
  });

test('real bearer HTTP: non-replayable submit is rejected once with no refresh or persisted business writes', async ({ page }) => {
  const f = fixture(); await login(page);
  await navigation(page).getByRole('link', { name: 'Request leave', exact: true }).click();
  await page.getByLabel('Leave type', { exact: true }).selectOption(f.typeId);
  const date = new Date(); date.setUTCDate(date.getUTCDate() + 55); const value = date.toISOString().slice(0, 10);
  await page.getByLabel('Start date', { exact: true }).fill(value); await page.getByLabel('End date', { exact: true }).fill(value);
  const before = businessCounts(); let writes = 0, refreshes = 0;
  await page.route('**/auth/refresh', async route => { refreshes++; await route.continue(); });
  await page.route(f.api + '/api/v1/leave-requests', async route => {
    writes++; const response = await route.fetch({ headers: { ...route.request().headers(), authorization: 'Bearer ' + f.authRegression.expiredAccessToken } });
    expect(response.status()).toBe(401); await route.fulfill({ response });
  });
  await page.getByRole('button', { name: 'Submit request', exact: true }).click(); await expect(page).toHaveURL(/\/login$/);
  expect(writes).toBe(1); expect(refreshes).toBe(0); expect(businessCounts()).toBe(before);
});

test('real bearer HTTP: permission denial never enters a refresh loop', async ({ page }) => {
  const f = fixture(); await login(page); let refreshes = 0;
  await page.route('**/auth/refresh', async route => { refreshes++; await route.continue(); });
  const db = new DatabaseSync(f.authRegression.database); db.exec('PRAGMA busy_timeout=3000');
  let identity = '', removed = false; let roles: { RoleId: string }[] = [];
  try {
    identity = db.prepare('SELECT IdentityUserId AS id FROM Employees WHERE Id=?').get(f.employees.employee.id.toUpperCase())!.id as string;
    roles = db.prepare('SELECT RoleId FROM AspNetUserRoles WHERE UserId=?').all(identity.toUpperCase()) as { RoleId: string }[];
    expect(roles.length).toBeGreaterThan(0);
    db.prepare('DELETE FROM AspNetUserRoles WHERE UserId=?').run(identity.toUpperCase());
    removed = true;
    const denied = page.waitForResponse(r => r.url() === f.api + '/api/v1/leave-requests/dashboard' && r.status() === 403);
    await page.getByRole('button', { name: 'Refresh dashboard', exact: true }).click();
    expect((await denied).status()).toBe(403);
    await expect(page.getByText('Personal leave access is unavailable. Sign in again to refresh your permissions.', { exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Retry dashboard', exact: true })).toBeEnabled();
    expect(refreshes).toBe(0); await expect(navigation(page)).toBeVisible();
  } finally {
    try { if (removed) for (const role of roles) db.prepare('INSERT INTO AspNetUserRoles (UserId,RoleId) VALUES (?,?)').run(identity.toUpperCase(),role.RoleId); }
    finally { db.close(); }
  }
});

test('real bearer HTTP: rejected refresh ends only the initiating session without retrying the protected read', async ({ page }) => {
  const token = await login(page); const before = refreshRows(); const expired = await rejectExpired(page, ['/leave-requests/dashboard'], token);
  let refreshes = 0;
  await page.route('**/auth/refresh', async route => {
    refreshes++;
    const response = await route.fetch({ postData: JSON.stringify({ refreshToken: 'synthetic-invalid-refresh' }) });
    expect(response.status()).toBe(401); await route.fulfill({ response });
  });
  await page.getByRole('button', { name: 'Refresh dashboard', exact: true }).click();
  await expect(page).toHaveURL(/\/login$/); expect(refreshes).toBe(1); expect(expired.count()).toBe(1);
  expect(refreshRows()).toEqual(before); await expect(navigation(page)).toHaveCount(0);
});
