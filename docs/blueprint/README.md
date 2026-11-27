# Blueprint

The authoritative design document is **SmartFactory AI — Solution Blueprint v2.1** (36 pages, PDF).

| File | Status |
|---|---|
| [`smartfactory-ai-blueprint-v2.1.pdf`](smartfactory-ai-blueprint-v2.1.pdf) | **Current.** Issued 01 November 2026. Page numbers of v2.0 are preserved; pages 35–36 (revision history, week-1 status) are new. |
| [`smartfactory-ai-blueprint-v2.0.pdf`](smartfactory-ai-blueprint-v2.0.pdf) | Superseded. Kept for the record; the v2.0 → v2.1 differences are listed on p.35 of v2.1. |

Blueprint v1.0 (29 pages) was reviewed as a team-sized plan and is not in the repository.

## What v2.1 changed

Runtime lifecycle corrections made when implementation started: .NET 10 LTS instead of .NET 8 (ADR-010),
Angular 22 instead of Angular 20 (ADR-011), MIT licence, the plant metric count (32), the test stack
(xUnit v3 on Microsoft Testing Platform, AwesomeAssertions, Vitest), the DevSecOps baseline pulled forward
to week 1, credential-free Keycloak realm import with a seed job, the synthetic-data response header, the
learning track, ADR-010/011, and the week-1 delivery status. Everything else is unchanged on purpose.

## Relationship to the rest of the repository

The decisions in the PDF are mirrored in reviewable form:

- `docs/adr/` — ADR-001 to ADR-011
- `docs/plant/plant.yaml` — simulated plant and fault library (validated by the simulator's tests)
- `docs/ROADMAP.md` — 12-week plan, budgets, gates, checkboxes
- `docs/learning/` — weekly learning notes and the skills coverage matrix
- `CLAUDE.md` — conventions

If the PDF and these files disagree, the PDF wins and the files are corrected in a `docs:` commit. If a
decision changes, write the ADR first, then issue a new blueprint revision.

## Rebuilding the PDF

The document is generated from HTML so revisions keep the same design (Inter typography, navy/teal palette,
A4, 17 mm margins) without a desktop publishing tool. Source lives in [`src/`](src/):

| File | Role |
|---|---|
| `src/build_blueprint.py` | Design tokens (CSS), page chrome, table/callout/flow/KPI/bar helpers, document metadata (`VERSION`, `ISSUE_DATE`) |
| `src/content_a.py`, `content_b.py`, `content_c.py` | One function per page, in order; each returns the page body |
| `src/render.js` | Prints the HTML to PDF with the Playwright Chromium already used by the web tests and fails if any page overflows its A4 box |

```bash
cd docs/blueprint/src
python3 build_blueprint.py --out ../build/blueprint.html          # needs only the standard library
node render.js ../build/blueprint.html ../smartfactory-ai-blueprint-v2.1.pdf
```

Chromium must see the Inter font: it is installed system-wide in the dev container; elsewhere pass
`--fonts-css` with an `@font-face` file (Inter is OFL-licensed). To issue a new revision: bump `VERSION`
and `ISSUE_DATE`, add a row to the revision history on the last page, keep page numbers stable where other
pages reference them, and render to a new file name. The `build/` folder is git-ignored.

All content is a portfolio concept on synthetic data; the system is advisory only.
