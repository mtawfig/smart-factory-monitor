// Gate G1, browser half: a real Authorization Code + PKCE login in Chromium against the Compose stack, then the
// asset explorer. Complements scripts/g1-check.sh, which checks the API with curl.
//   npm i --no-save playwright && npx playwright install chromium
//   DEV_USER_PASSWORD=... node scripts/g1-browser-check.cjs
// Why CommonJS: CI installs Playwright outside the repository and points NODE_PATH at it, which only require() honours.
const { chromium } = require('playwright');

const WEB = process.env.WEB_URL ?? 'http://localhost:4200';
const PASSWORD = process.env.DEV_USER_PASSWORD;
const SCREENSHOT = process.env.G1_SCREENSHOT ?? 'g1-asset-explorer.png';

async function main() {
  if (!PASSWORD) throw new Error('set DEV_USER_PASSWORD');
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage({ viewport: { width: 1366, height: 900 } });
    page.setDefaultTimeout(30_000);

    // Deep link while signed out: the guard sends us to /login and remembers where we were going.
    await page.goto(`${WEB}/assets`);
    await page.getByTestId('login-button').click();

    // Keycloak's own login page (PKCE: the browser never sees a client secret).
    await page.locator('#username').fill('planner');
    await page.locator('#password').fill(PASSWORD);
    await page.locator('#kc-login').click();

    await page.waitForURL((url) => url.origin === new URL(WEB).origin);
    await page.getByTestId('user-menu').waitFor();
    console.log('  ✔ signed in as planner through Keycloak (PKCE)');

    if (!page.url().includes('/assets')) {
      await page.getByRole('link', { name: 'Assets' }).click();
    }
    const rows = page.getByTestId('asset-row');
    await rows.nth(7).waitFor();
    const codes = await page.locator('[data-testid="asset-row"] a.code').allTextContents();
    for (const code of ['MTR-042', 'CVR-012', 'CMP-007', 'FAN-034', 'CNC-018', 'PMP-211', 'PRS-105', 'CHL-003']) {
      if (!codes.map((c) => c.trim()).includes(code)) throw new Error(`asset ${code} missing; saw ${codes.join(', ')}`);
    }
    console.log(`  ✔ asset explorer lists the seeded machines: ${codes.map((c) => c.trim()).join(' ')}`);

    if (!(await page.getByTestId('synthetic-banner').isVisible())) throw new Error('synthetic-data banner not visible');
    console.log('  ✔ synthetic-data banner visible');

    await page.locator('[data-testid="asset-row"] a.code', { hasText: 'MTR-042' }).click();
    await page.getByTestId('sensor-row').first().waitFor();
    const topic = await page.getByTestId('sensor-table').textContent();
    if (!topic?.includes('plant/tnt-alsafa/plant-01/line-a/MTR-042/')) throw new Error('MQTT topic not shown on detail page');
    console.log('  ✔ asset detail shows the sensors and their MQTT topics');

    await page.goBack();
    await rows.nth(7).waitFor();
    await page.screenshot({ path: SCREENSHOT, fullPage: true });
    console.log(`G1 browser check passed. Screenshot: ${SCREENSHOT}`);
  } finally {
    await browser.close();
  }
}

main().catch((error) => {
  console.error(`  ✘ ${error.message}`);
  process.exit(1);
});
