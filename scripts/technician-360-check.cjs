// Week 3 end-to-end check: a technician does a whole job on a 360 px phone against the Compose stack.
// The planner creates and schedules the work through the API; the technician signs in through Keycloak (PKCE) in
// Chromium with a phone viewport, starts the job, records the checklist and a finding with a photo, and completes it.
// Fails if any page scrolls sideways or a control is smaller than a 44 px touch target. Keeps screenshots.
//   DEV_USER_PASSWORD=... node scripts/technician-360-check.cjs      (Playwright installed as for g1-browser-check)
// Why CommonJS: CI installs Playwright outside the repository and points NODE_PATH at it, which only require() honours.
const { chromium } = require('playwright');

const WEB = process.env.WEB_URL ?? 'http://localhost:4200';
const API = process.env.API_URL ?? 'http://localhost:5080';
const KEYCLOAK = process.env.KEYCLOAK_URL ?? 'http://localhost:8180';
const PASSWORD = process.env.DEV_USER_PASSWORD;
const SHOTS = process.env.SCREENSHOT_PREFIX ?? 'technician-360';
const PHONE = { width: 360, height: 740 };

// A PNG signature is all the server checks (it never decodes images); the rest is padding.
const PNG = Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 0, 0, 0, 0x0d, 0x49, 0x48, 0x44, 0x52]);

async function token(username) {
  const response = await fetch(`${KEYCLOAK}/realms/smartfactory/protocol/openid-connect/token`, {
    method: 'POST',
    body: new URLSearchParams({ grant_type: 'password', client_id: 'smartfactory-authz-tests', username, password: PASSWORD, scope: 'openid' }),
  });
  if (!response.ok) throw new Error(`${username} could not sign in: ${response.status}`);
  return (await response.json()).access_token;
}

async function api(bearer, method, path, body) {
  const response = await fetch(`${API}${path}`, {
    method,
    headers: { Authorization: `Bearer ${bearer}`, 'Content-Type': 'application/json' },
    body: body ? JSON.stringify(body) : undefined,
  });
  if (!response.ok) throw new Error(`${method} ${path} → ${response.status} ${await response.text()}`);
  return response.json();
}

/** Every page must fit the phone: no sideways scrolling. */
async function assertFits(page, what) {
  const { scroll, viewport } = await page.evaluate(() => ({ scroll: document.documentElement.scrollWidth, viewport: window.innerWidth }));
  if (scroll > viewport) throw new Error(`${what} scrolls sideways at ${viewport} px (content is ${scroll} px wide)`);
}

/** Controls the technician taps must be at least 44 px high (WCAG 2.5.5 target size). */
async function assertTouchTargets(page, selector, what) {
  const heights = await page.locator(selector).evaluateAll((els) => els.map((e) => Math.round(e.getBoundingClientRect().height)));
  const small = heights.filter((h) => h < 44);
  if (heights.length === 0) throw new Error(`no ${what} found`);
  if (small.length > 0) throw new Error(`${what}: ${small.length} of ${heights.length} are under 44 px (${small.join(', ')})`);
}

async function main() {
  if (!PASSWORD) throw new Error('set DEV_USER_PASSWORD');

  // ── Planner, through the API: raise, approve and schedule a job for "technician" ──
  const planner = await token('planner');
  const [asset] = await api(planner, 'GET', '/api/assets/assets?search=MTR-042');
  let w = await api(planner, 'POST', '/api/maintenance/requests', {
    assetId: asset.id,
    title: 'Bearing noise on filler drive motor',
    reason: 'Vibration above the alarm band for three windows (synthetic data).',
    priority: 'High',
    checklist: ['Lock out and tag out', 'Check bearing play', 'Measure vibration after repair'],
  });
  w = await api(planner, 'POST', `/api/maintenance/work-orders/${w.id}/approve`, { version: w.version });
  const today = new Date().toISOString().slice(0, 10);
  w = await api(planner, 'POST', `/api/maintenance/work-orders/${w.id}/schedule`, { version: w.version, assignee: 'technician', scheduledFor: today });
  console.log(`  ✔ planner raised, approved and scheduled ${w.assetCode} for technician`);

  // ── Technician, in a phone-sized browser ──
  const browser = await chromium.launch();
  try {
    const context = await browser.newContext({ viewport: PHONE, deviceScaleFactor: 2, isMobile: true, hasTouch: true });
    const page = await context.newPage();
    page.setDefaultTimeout(30_000);

    await page.goto(`${WEB}/my-work`);
    await page.getByTestId('login-button').click();
    await page.locator('#username').fill('technician');
    await page.locator('#password').fill(PASSWORD);
    await page.locator('#kc-login').click();
    await page.waitForURL((url) => url.origin === new URL(WEB).origin);
    // Why wait: the redirect lands on "/?code=…" first; the app then finishes sign-in and returns to /my-work.
    await page.getByTestId('user-menu').waitFor();
    await page.waitForURL(/\/my-work/, { timeout: 5_000 }).catch(() => undefined);

    if (!page.url().includes('/my-work')) {
      await page.getByTestId('nav-toggle').click();
      await page.getByRole('link', { name: 'My work' }).click();
    }

    const card = page.locator(`a[href="/my-work/${w.id}"]`);
    await card.waitFor();
    await assertFits(page, 'My work list');
    await assertTouchTargets(page, '[data-testid="work-card"]', 'job cards');
    await page.screenshot({ path: `${SHOTS}-list.png`, fullPage: true });
    console.log('  ✔ technician signed in on a 360 px phone and sees the job');

    await card.click();
    await page.getByTestId('action-Start').click();
    await page.getByTestId('outcome-Done').first().waitFor();
    await assertTouchTargets(page, '[data-testid^="outcome-"], [data-testid^="action-"]', 'checklist and action buttons');

    const items = page.getByTestId('checklist-item');
    for (let i = 0; i < (await items.count()); i++) {
      await items.nth(i).getByTestId('outcome-Done').click();
      await page.waitForFunction((n) => document.querySelectorAll('[data-testid="checklist-item"]')[n]?.getAttribute('data-status') === 'Done', i);
    }

    await page.getByTestId('finding-condition').click();
    await page.getByRole('option', { name: 'Worn' }).click();
    await page.getByTestId('finding-notes').fill('Bearing play confirmed, replaced (synthetic).');
    await page.getByTestId('add-finding').click();
    await page.getByTestId('finding').first().waitFor();
    await page.getByTestId('file-input').first().setInputFiles({ name: 'bearing.png', mimeType: 'image/png', buffer: PNG });
    await page.locator('[data-testid^="attachment-"]').first().waitFor();
    console.log('  ✔ checklist recorded, finding and photo added');

    await assertFits(page, 'Work order page');
    await page.screenshot({ path: `${SHOTS}-job.png`, fullPage: true });
    await page.getByTestId('action-Complete').click();
    await page.getByTestId('wo-summary').filter({ hasText: 'Completed' }).waitFor();
    await assertFits(page, 'Completed work order page');
    console.log('  ✔ job completed from the phone; no page scrolls sideways; touch targets ≥ 44 px');
  } finally {
    await browser.close();
  }

  // ── Back in the API: the planner sees the outcome ──
  const done = await api(planner, 'GET', `/api/maintenance/work-orders/${w.id}`);
  if (done.status !== 'Completed') throw new Error(`expected Completed, got ${done.status}`);
  if (done.findings[0]?.attachments.length !== 1) throw new Error('expected one attachment on the finding');
  console.log(`Technician 360 px check passed (revision ${done.revision}). Screenshots: ${SHOTS}-list.png, ${SHOTS}-job.png`);
}

main().catch((error) => {
  console.error(`  ✘ ${error.message}`);
  process.exit(1);
});
