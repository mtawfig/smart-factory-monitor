// Renders the generated blueprint HTML to an A4 PDF with the pre-installed Playwright Chromium and
// reports any page whose content overflows its fixed A4 box (which would otherwise be silently clipped).
//   node render.js <input.html> <output.pdf>
const path = require('node:path');
const { chromium } = require('playwright');

(async () => {
  const [input, output] = process.argv.slice(2);
  if (!input || !output) {
    console.error('usage: node render.js <input.html> <output.pdf>');
    process.exit(2);
  }
  const browser = await chromium.launch();
  const page = await browser.newPage();
  await page.goto('file://' + path.resolve(input), { waitUntil: 'networkidle' });
  await page.evaluate(() => document.fonts.ready);

  const overflow = await page.evaluate(() =>
    Array.from(document.querySelectorAll('.page')).map((p, i) => {
      const main = p.querySelector('main');
      const footer = p.querySelector('footer');
      if (!main || !footer) return null;
      const mainBottom = main.getBoundingClientRect().bottom;
      const footerTop = footer.getBoundingClientRect().top;
      return { page: i + 1, spare: Math.round(footerTop - mainBottom) };
    }).filter(Boolean),
  );
  const bad = overflow.filter((o) => o.spare < 0);
  for (const o of overflow) console.log(`page ${String(o.page).padStart(2, '0')}: spare ${o.spare}px`);
  if (bad.length) {
    console.error(`OVERFLOW on pages: ${bad.map((b) => b.page).join(', ')}`);
  }

  await page.pdf({ path: output, format: 'A4', printBackground: true, preferCSSPageSize: true, margin: { top: 0, right: 0, bottom: 0, left: 0 } });
  await browser.close();
  console.log(`wrote ${output}; ${overflow.length} pages`);
  process.exit(bad.length ? 1 : 0);
})();
