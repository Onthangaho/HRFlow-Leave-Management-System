import fs from 'node:fs';
import path from 'node:path';
import { test, expect } from '@playwright/test';

// Opt-in production measurement; output contains only asset basenames, bytes and timing values.
for (const actor of ['employee', 'manager', 'hr']) test(`production loading measurement: ${actor}`, async ({ page }) => {
  const fixture = JSON.parse(fs.readFileSync(process.env.HRFLOW_TEST_STATE!, 'utf8'));
  const results = [];
  for (const cache of ['cold', 'warm']) {
    const started = Date.now();
    await page.goto(fixture.client + '/');
    await page.getByLabel('Email', { exact: true }).fill(fixture.employees[actor].email);
    await page.getByLabel('Password', { exact: true }).fill(fixture.password);
    await page.getByRole('button', { name: 'Sign in', exact: true }).click();
    await expect(page.getByRole('navigation', { name: 'Primary', exact: true })).toBeVisible();
    if (actor === 'hr') {
      await expect(page.getByText('HR snapshot loaded.', { exact: true })).toBeVisible();
      await expect(page.getByText('Period trend: summed Approved request-days', { exact: true })).toBeVisible();
    } else await expect(page.getByRole('heading', { name: 'Current configured balances', exact: true })).toBeVisible();
    await page.waitForLoadState('networkidle');
    const initial = await page.evaluate(() => performance.getEntriesByType('resource')
      .filter(entry => new URL(entry.name).pathname.endsWith('.js'))
      .map(entry => { const resource = entry as PerformanceResourceTiming; return { asset: new URL(entry.name).pathname.split('/').pop(), encodedBytes: resource.encodedBodySize, transferBytes: resource.transferSize, durationMs: resource.duration }; }));
    expect(initial.every(resource => resource.asset && /^[A-Za-z0-9_-]+\.js$/.test(resource.asset))).toBe(true);
    const overviewMs = Date.now() - started;
    const route = actor === 'hr' ? '/admin/employees' : actor === 'manager' ? '/team-leave' : '/account/profile';
    const before = initial.length;
    const transition = Date.now();
    await page.locator(`nav[aria-label="Primary"] a[href="${route}"]`).click();
    if (actor === 'hr') await expect(page.getByRole('region', { name: 'Directory results', exact: true })).toHaveAttribute('aria-busy', 'false');
    else if (actor === 'manager') await expect(page.getByRole('region', { name: 'Read-only team calendar', exact: true })).toBeVisible();
    else await expect(page.getByLabel('Preferred display name (optional)', { exact: true })).toBeVisible();
    await page.waitForLoadState('networkidle');
    const subsequent = await page.evaluate(() => performance.getEntriesByType('resource')
      .filter(entry => new URL(entry.name).pathname.endsWith('.js'))
      .map(entry => { const resource = entry as PerformanceResourceTiming; return { asset: new URL(entry.name).pathname.split('/').pop(), encodedBytes: resource.encodedBodySize, transferBytes: resource.transferSize, durationMs: resource.duration }; }));
    results.push({ cache, overviewMs, transitionMs: Date.now() - transition, initial, subsequentlyLoaded: subsequent.slice(before) });
    await page.getByRole('button', { name: 'Sign out', exact: true }).click();
    await expect(page).toHaveURL(/\/login$/);
  }
  const destination = process.env.HRFLOW_LOADING_OUTPUT;
  const outputRoot = path.resolve(import.meta.dirname, '../../.local-verification');
  if (!destination || path.dirname(path.resolve(destination)) !== outputRoot || !/^client-loading-[a-z0-9-]+\.json$/.test(path.basename(destination)) || !fs.statSync(outputRoot).isDirectory()) throw new Error('Use an ignored client-loading JSON file in .local-verification.');
  const data = fs.existsSync(destination) ? JSON.parse(fs.readFileSync(destination, 'utf8')) : {};
  if (Object.keys(data).some(key => !['employee', 'manager', 'hr'].includes(key))) throw new Error('Refusing unrelated measurement output.');
  data[actor] = results;
  fs.writeFileSync(destination, JSON.stringify(data, null, 2));
});
