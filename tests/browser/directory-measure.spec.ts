import fs from 'node:fs';
import path from 'node:path';
import { test, expect } from '@playwright/test';

/** Real HTTP measurement; optional outputs contain counts only, never fixture state or image bytes. */
test('directory measurement: real HTTP rows and thumbnail attempts', async ({ page }) => {
  test.setTimeout(120_000);
  const state = JSON.parse(fs.readFileSync(process.env.HRFLOW_TEST_STATE!, 'utf8'));
  let images = 0;
  page.on('request', r => { if (/\/employees\/[\w-]+\/image$/.test(r.url())) images++; });
  await page.goto(state.client + '/login');
  await page.getByLabel('Email', { exact: true }).fill(state.employees.hr.email);
  await page.getByLabel('Password', { exact: true }).fill(state.password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await page.getByRole('navigation', { name: 'Primary', exact: true }).getByRole('link', { name: 'Employee management', exact: true }).click();
  await expect(page.getByRole('table', { name: 'Employees and their current departments, roles, and managers' })).toBeVisible();
  await page.waitForLoadState('networkidle');
  const metrics = { rows: await page.locator('tbody tr').count(), directoryImageRequests: images, viewport: page.viewportSize() };
  expect(metrics.rows).toBeLessThanOrEqual(25); expect(images).toBe(0);
  if (process.env.HRFLOW_DIRECTORY_MEASURE) {
    const name = `directory-${process.env.HRFLOW_DIRECTORY_MEASURE}-${process.env.HRFLOW_DIRECTORY_FIXTURE_ROWS ?? '75'}.json`;
    fs.writeFileSync(path.resolve('../../.local-verification', name), JSON.stringify(metrics));
  }
});
