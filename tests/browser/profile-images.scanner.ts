import fs from 'node:fs';
import { test, expect } from '@playwright/test';

interface Fixture { password: string; api: string; client: string; employees: Record<string, { id: string; email: string }> }
const fixture = () => JSON.parse(fs.readFileSync(process.env.HRFLOW_TEST_STATE!, 'utf8')) as Fixture;

test('Real ClamAV HTTP/browser: crop, replace, header, HR visibility and versioned removal', async ({ page }) => {
  const state = fixture();
  await page.goto(state.client + '/login');
  await page.getByLabel('Email', { exact: true }).fill(state.employees.employee.email);
  await page.getByLabel('Password', { exact: true }).fill(state.password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await page.getByRole('navigation', { name: 'Primary', exact: true }).getByRole('link', { name: 'Profile', exact: true }).click();
  const section = page.locator('section').filter({ has: page.getByRole('heading', { name: 'Profile image', exact: true }) });
  const png = Buffer.from(await page.evaluate(() => {
    const canvas = document.createElement('canvas'); canvas.width = 160; canvas.height = 100;
    const context = canvas.getContext('2d')!; context.fillStyle = '#6366f1'; context.fillRect(0, 0, 160, 100);
    context.fillStyle = '#fbbf24'; context.fillRect(80, 0, 80, 100); return canvas.toDataURL('image/png').split(',')[1];
  }), 'base64');
  await section.getByLabel('Choose an image').setInputFiles({ name: 'synthetic.png', mimeType: 'image/png', buffer: png });
  await section.getByLabel('Crop zoom').focus(); await page.keyboard.press('ArrowRight');
  if (process.env.HRFLOW_IMAGE_SCREENSHOTS === '1') {
    fs.mkdirSync('../../docs/verification/screenshots/private-profile-images', { recursive: true });
    await page.screenshot({ path: '../../docs/verification/screenshots/private-profile-images/crop-desktop.png', fullPage: true });
  }
  const upload = page.waitForResponse(r => r.url().endsWith('/me/image') && r.request().method() === 'PUT');
  let writes = 0; page.on('request',request=>{ if(request.url().endsWith('/me/image') && request.method()==='PUT') writes++; });
  await section.getByRole('button', { name: 'Upload image', exact: true }).dblclick();
  const response = await upload;
  if (response.status() !== 200) {
    const problem = await response.json();
    throw new Error(`Synthetic image rejected: ${problem.title}; ${problem.detail ?? Object.entries(problem.errors ?? {}).map(([key,value]) => `${key}:${value}`).join(',')}`);
  }
  expect(response.status()).toBe(200);
  await expect(section.getByText('Profile image updated.', { exact: true })).toBeVisible();
  expect(writes).toBe(1);
  await expect(page.locator('.shell-account img')).toBeVisible();
  if (process.env.HRFLOW_IMAGE_SCREENSHOTS === '1') {
    fs.mkdirSync('../../docs/verification/screenshots/private-profile-images', { recursive: true });
    await page.screenshot({ path: '../../docs/verification/screenshots/private-profile-images/profile-desktop-light.png', fullPage: true });
  }
  await section.getByLabel('Choose an image').setInputFiles({ name: 'replacement.png', mimeType: 'image/png', buffer: png });
  await section.getByLabel('Horizontal crop position').focus(); await page.keyboard.press('ArrowRight');
  const position = await section.getByLabel('Horizontal crop position').inputValue();
  const login = await page.request.post(state.api+'/api/v1/auth/login',{data:{email:state.employees.employee.email,password:state.password}});
  const token = (await login.json()).accessToken;
  const headers = { Authorization: `Bearer ${token}` };
  const metadata = await page.request.get(state.api+'/api/v1/me/image',{headers});
  const external = await page.request.put(state.api+'/api/v1/me/image',{ headers,multipart:{file:{name:'synthetic.png',mimeType:'image/png',buffer:png},expectedVersion:(await metadata.json()).version,x:'0',y:'0',size:'1'} });
  expect(external.status()).toBe(200);
  await section.getByRole('button', { name: 'Upload image', exact: true }).click();
  await expect(section.getByText('The image changed. Explicitly reload its version and review your retained crop before trying again.',{exact:true})).toBeVisible();
  await expect(section.getByAltText('Selected image crop preview')).toBeVisible();
  await expect(section.getByLabel('Horizontal crop position')).toHaveValue(position);
  await section.getByRole('button',{name:'Reload image version',exact:true}).click();
  await expect(section.getByRole('button',{name:'Upload image',exact:true})).toBeEnabled();
  await expect(section.getByLabel('Horizontal crop position')).toHaveValue(position);
  await section.getByRole('button', { name: 'Upload image', exact: true }).click();
  await expect(section.getByText('Profile image updated.', { exact: true })).toBeVisible();
  await expect(section.getByRole('button', { name: 'Remove image', exact: true })).toBeEnabled();
  await page.getByRole('button', { name: 'Sign out', exact: true }).click();
  await page.getByLabel('Email', { exact: true }).fill(state.employees.hr.email);
  await page.getByLabel('Password', { exact: true }).fill(state.password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await page.getByRole('navigation', { name: 'Primary', exact: true }).getByRole('link', { name: 'Employee management', exact: true }).click();
  await page.getByLabel('Search employees', { exact: true }).fill(state.employees.employee.email);
  await page.getByRole('button', { name: 'Apply filters', exact: true }).click();
  await expect(page.getByRole('row').filter({ hasText: state.employees.employee.email }).locator('img')).toBeVisible();
  if (process.env.HRFLOW_IMAGE_SCREENSHOTS === '1') await page.screenshot({ path: '../../docs/verification/screenshots/private-profile-images/hr-directory.png', fullPage: true });
  await page.getByRole('button', { name: 'Sign out', exact: true }).click();
  await page.getByLabel('Email', { exact: true }).fill(state.employees.employee.email);
  await page.getByLabel('Password', { exact: true }).fill(state.password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await page.getByRole('navigation', { name: 'Primary', exact: true }).getByRole('link', { name: 'Profile', exact: true }).click();
  await page.setViewportSize({ width: 390, height: 844 });
  await page.emulateMedia({ colorScheme: 'dark', reducedMotion: 'reduce' });
  await expect(page.locator('html')).toHaveAttribute('data-theme','dark');
  if (process.env.HRFLOW_IMAGE_SCREENSHOTS === '1') await page.screenshot({ path: '../../docs/verification/screenshots/private-profile-images/profile-mobile-dark.png', fullPage: true });
  await section.getByRole('button', { name: 'Remove image', exact: true }).click();
  const dialog = page.getByRole('dialog'); await expect(dialog.getByRole('button', { name: 'Cancel', exact: true })).toBeFocused();
  if (process.env.HRFLOW_IMAGE_SCREENSHOTS === '1') await page.screenshot({ path: '../../docs/verification/screenshots/private-profile-images/remove-mobile-dark.png' });
  await page.keyboard.press('Escape'); await expect(dialog).not.toBeVisible();
  await expect(section.getByRole('button', { name: 'Remove image', exact: true })).toBeFocused();
  await section.getByRole('button', { name: 'Remove image', exact: true }).click();
  await dialog.getByRole('button', { name: 'Remove image', exact: true }).click();
  await expect(section.getByText('Profile image removed.', { exact: true })).toBeVisible();
  await expect(page.locator('.shell-account img')).toHaveCount(0);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
});

for (const next of ['employee', 'hr']) test(`Controlled response barrier: image completion cannot notify replacement ${next} session`, async ({ page }) => {
  const state = fixture();
  const login = async (actor: string) => {
    await page.getByLabel('Email', { exact: true }).fill(state.employees[actor].email);
    await page.getByLabel('Password', { exact: true }).fill(state.password);
    await page.getByRole('button', { name: 'Sign in', exact: true }).click();
    await expect(page.getByRole('navigation', { name: 'Primary', exact: true })).toBeVisible();
  };
  await page.goto(state.client + '/login'); await login('employee');
  await page.getByRole('navigation', { name: 'Primary', exact: true }).getByRole('link', { name: 'Profile', exact: true }).click();
  const png = Buffer.from(await page.evaluate(() => { const c = document.createElement('canvas'); c.width = c.height = 16; c.getContext('2d')!.fillRect(0,0,16,16); return c.toDataURL().split(',')[1]; }), 'base64');
  await page.getByLabel('Choose an image').setInputFiles({ name: 'synthetic.png', mimeType: 'image/png', buffer: png });
  let release!: () => void; const barrier = new Promise<void>(r => { release = r; });
  let entered!: () => void; const ready = new Promise<void>(r => { entered = r; });
  await page.route('**/me/image', async route => {
    if (route.request().method() !== 'PUT') return route.continue();
    const response = await route.fetch(); entered(); await barrier;
    try { await route.fulfill({ response }); } catch { /* Unmount correctly aborts the old request. */ }
  });
  try {
    await page.getByRole('button', { name: 'Upload image', exact: true }).click(); await ready;
    await page.getByRole('button', { name: 'Sign out', exact: true }).click(); await login(next); release();
    await page.getByRole('navigation', { name: 'Primary', exact: true }).getByRole('link', { name: 'Profile', exact: true }).click();
    await expect(page.getByText('Profile image updated.', { exact: true })).toHaveCount(0);
    if (next === 'hr') await expect(page.locator('.shell-account img')).toHaveCount(0);
  } finally { release(); }
});
