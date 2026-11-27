// Renders the SPSD (smartfactory-ai-spsd.md) to an A4 PDF in the blueprint's visual style: cover page, table of
// contents, navy/teal palette, running header and page-numbered footer. The Markdown stays the source of truth;
// regenerate the PDF whenever it changes.
//
//   npm i --no-save --prefix /tmp/spsd marked@14.1.4        # Playwright's Chromium is assumed to be installed
//   NODE_PATH=/tmp/spsd/node_modules node docs/specifications/render-spsd.cjs
// Needs `pdfunite` (poppler-utils): the cover is rendered without the running header and footer, then joined.
//
// Why CommonJS + NODE_PATH: the repository has no Node project at its root, and the two tools are needed only here.
const fs = require('node:fs');
const os = require('node:os');
const { execFileSync } = require('node:child_process');
const path = require('node:path');
const { marked } = require('marked');
const { chromium } = require('playwright');

const dir = __dirname;
const source = path.join(dir, 'smartfactory-ai-spsd.md');
const output = path.join(dir, 'smartfactory-ai-spsd.pdf');

const NAVY = '#0a2138', INK = '#1d2a37', MUTED = '#5c6a78', TEAL = '#1a9c86', TEAL_SOFT = '#e9f6f2';
const RULE = '#d9e0e6', ZEBRA = '#f6f8fa', CODE_BG = '#0f2a40', CODE_FG = '#dae7f0';

const markdown = fs.readFileSync(source, 'utf8');
const control = /\| Version \| ([^|]+) \|[\s\S]*?\| Issue date \| ([^|]+) \|/.exec(markdown);
const version = control ? control[1].trim() : '1.0';
const issued = control ? control[2].trim() : '';

// Slugged headings so the table of contents can link to them.
const toc = [];
const slugs = new Map();
const renderer = new marked.Renderer();
renderer.heading = ({ tokens, depth }) => {
  const text = marked.Parser.parseInline(tokens);
  const plain = text.replace(/<[^>]+>/g, '');
  let slug = plain.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, '') || 'section';
  const n = slugs.get(slug) ?? 0;
  slugs.set(slug, n + 1);
  if (n) slug += `-${n}`;
  const isPart = depth === 1 && /^Part [A-D]/.test(plain);
  if (isPart || (depth === 2 && toc.length)) toc.push({ depth: isPart ? 1 : 2, text: plain, slug });
  const cls = isPart ? ' class="part"' : '';
  return `<h${depth} id="${slug}"${cls}>${text}</h${depth}>`;
};

// The first H1 is the document title (shown on the cover), and the document-control block belongs on page 2.
const body = marked.parse(markdown.replace(/^# .*\n/, ''), { renderer, gfm: true });

const tocHtml = toc
  .map((t) => `<li class="l${t.depth}"><a href="#${t.slug}">${t.text}</a></li>`)
  .join('\n');

const head = `<!doctype html><html lang="en"><head><meta charset="utf-8">
<title>SmartFactory AI — Software Project Specification Document</title>
<style>
@page { size: A4; margin: 20mm 16mm 18mm 16mm; }
* { box-sizing: border-box; }
html, body { margin: 0; color: ${INK}; font-family: 'Inter', system-ui, sans-serif; font-size: 10.2px; line-height: 1.5; }
a { color: ${TEAL}; text-decoration: none; }
strong { color: ${NAVY}; }
code { font-family: 'DejaVu Sans Mono', monospace; font-size: 9px; background: #eef2f5; padding: 1px 3px; border-radius: 3px; }
pre { background: ${CODE_BG}; color: ${CODE_FG}; padding: 9px 11px; border-radius: 6px; font-size: 8.4px; line-height: 1.4;
      white-space: pre-wrap; word-break: break-word; break-inside: avoid; }
pre code { background: none; color: inherit; padding: 0; font-size: inherit; }
h1, h2, h3, h4 { color: ${NAVY}; break-after: avoid; line-height: 1.25; }
h1.part { break-before: page; font-size: 24px; margin: 0 0 14px; padding: 14px 16px; color: #fff;
          background: linear-gradient(90deg, ${NAVY}, #13385c); border-left: 6px solid ${TEAL}; border-radius: 4px; }
h2 { font-size: 15.5px; margin: 20px 0 7px; padding-bottom: 4px; border-bottom: 2px solid ${TEAL}; }
h3 { font-size: 12.4px; margin: 14px 0 5px; }
h4 { font-size: 11px; margin: 10px 0 4px; }
p { margin: 4px 0 7px; }
ul, ol { margin: 3px 0 8px; padding-left: 18px; }
li { margin: 1.5px 0; }
hr { display: none; }
blockquote { margin: 8px 0; padding: 8px 12px; background: ${TEAL_SOFT}; border-left: 4px solid ${TEAL}; border-radius: 4px; }
blockquote p { margin: 0; }
table { width: 100%; border-collapse: collapse; margin: 6px 0 11px; font-size: 8.9px; }
thead { display: table-header-group; }
tr { break-inside: avoid; }
th { background: ${NAVY}; color: #fff; text-align: left; font-weight: 600; padding: 5px 7px; vertical-align: bottom; }
td { padding: 4px 7px; border-bottom: 1px solid ${RULE}; vertical-align: top; }
tbody tr:nth-child(even) td { background: ${ZEBRA}; }
td:first-child { font-weight: 600; color: ${NAVY}; min-width: 58px; }
td code, th code { font-size: 8.2px; }

/* Cover */
.cover { height: 297mm; width: 210mm; position: relative; color: #fff; overflow: hidden; break-after: page;
         background: linear-gradient(160deg, ${NAVY} 0%, #0f2f4f 62%, #124a5c 100%); }
.cover .band { position: absolute; left: 0; right: 0; top: 0; height: 9mm; background: ${TEAL}; }
.cover .inner { position: absolute; left: 20mm; right: 20mm; top: 62mm; }
.cover .kicker { font-size: 11px; letter-spacing: .16em; font-weight: 700; color: #8fe0cf; text-transform: uppercase; }
.cover h1 { color: #fff; font-size: 40px; line-height: 1.08; margin: 10px 0 8px; letter-spacing: -.01em; }
.cover .sub { font-size: 16px; color: #cfe1ec; margin: 0 0 26px; max-width: 150mm; }
.cover .tags span { display: inline-block; margin: 0 6px 6px 0; padding: 4px 9px; border: 1px solid #3f6a87;
                    border-radius: 999px; font-size: 10px; color: #dbe9f2; }
.cover .meta { position: absolute; left: 20mm; right: 20mm; bottom: 34mm; display: grid; grid-template-columns: 1.6fr .6fr 1fr 1fr;
               gap: 10px; border-top: 1px solid #3f6a87; padding-top: 12px; }
.cover .meta div { font-size: 10px; color: #9fbdd0; }
.cover .meta b { display: block; color: #fff; font-size: 12.5px; margin-top: 2px; }
.cover .notice { position: absolute; left: 20mm; right: 20mm; bottom: 16mm; font-size: 9.6px; color: #a9c4d4; }

/* Table of contents */
.toc { break-after: page; }
.toc h2 { margin-top: 0; }
.toc ol { list-style: none; padding: 0; columns: 2; column-gap: 12mm; }
.toc li { break-inside: avoid; }
.toc li.l1 { margin-top: 9px; font-weight: 700; color: ${NAVY}; font-size: 11px; }
.toc li.l1 a { color: ${NAVY}; }
.toc li.l2 { padding-left: 10px; font-size: 9.8px; }
.toc li.l2 a { color: ${INK}; }
</style></head><body>`;

const coverHtml = `${head.replace('@page { size: A4; margin: 20mm 16mm 18mm 16mm; }', '@page { size: A4; margin: 0; }')}
<section class="cover">
  <div class="band"></div>
  <div class="inner">
    <div class="kicker">SmartFactory AI · Project 02</div>
    <h1>Software Project<br>Specification Document</h1>
    <p class="sub">Project overview, business requirements, functional specification and technical specification
      for the AI Smart Factory PDM predictive-maintenance and asset-management platform.</p>
    <div class="tags"><span>Predictive AI</span><span>Anomaly detection</span><span>Applied AI &amp; MLOps</span>
      <span>Workflow automation</span><span>Industrial IoT</span><span>Multi-tenant SaaS</span></div>
  </div>
  <div class="meta">
    <div>Document ID<b>SMARTFACTORY-SPSD-001</b></div>
    <div>Version<b>${version}</b></div>
    <div>Issue date<b>${issued}</b></div>
    <div>Classification<b>Portfolio · MIT</b></div>
  </div>
  <div class="notice">Synthetic data only: a fully simulated, fictional plant ("Al Safa Industrial"). Advisory only: the
    system never commands equipment. Targets are engineering targets, not measured field results.</div>
</section></body></html>`;

const bodyHtml = `${head}
<section class="toc"><h2>Contents</h2><ol>${tocHtml}</ol></section>
${body}
</body></html>`;

(async () => {
  const work = fs.mkdtempSync(path.join(os.tmpdir(), 'spsd-'));
  const coverPdf = path.join(work, 'cover.pdf');
  const bodyPdf = path.join(work, 'body.pdf');
  const browser = await chromium.launch();
  const page = await browser.newPage();

  await page.setContent(coverHtml, { waitUntil: 'load' });
  await page.evaluate(() => document.fonts.ready);
  await page.pdf({ path: coverPdf, format: 'A4', printBackground: true, preferCSSPageSize: true });

  await page.setContent(bodyHtml, { waitUntil: 'load' });
  await page.evaluate(() => document.fonts.ready);
  const label = `<span style="color:${NAVY};font-weight:700;letter-spacing:.08em">SMARTFACTORY AI</span>
    <span style="color:${MUTED}"> · SOFTWARE PROJECT SPECIFICATION · v${version}</span>`;
  await page.pdf({
    path: bodyPdf,
    format: 'A4',
    printBackground: true,
    preferCSSPageSize: true,
    displayHeaderFooter: true,
    headerTemplate: `<div style="width:100%;font-family:Inter,sans-serif;font-size:7.5px;padding:0 16mm;display:flex;
      justify-content:space-between;border-bottom:0.5px solid ${RULE};padding-bottom:3px;margin-top:6mm">
      <span>${label}</span><span style="color:${MUTED}">SMARTFACTORY-SPSD-001</span></div>`,
    footerTemplate: `<div style="width:100%;font-family:Inter,sans-serif;font-size:7.5px;color:${MUTED};padding:0 16mm;
      display:flex;justify-content:space-between;margin-bottom:5mm">
      <span>Synthetic data · Advisory only · No autonomous machinery control</span>
      <span>Page <span class="pageNumber"></span> of <span class="totalPages"></span></span></div>`,
  });
  await browser.close();

  execFileSync('pdfunite', [coverPdf, bodyPdf, output]);
  fs.rmSync(work, { recursive: true, force: true });
  console.log(`wrote ${path.relative(process.cwd(), output)}`);
})().catch((error) => {
  console.error(error);
  process.exit(1);
});
