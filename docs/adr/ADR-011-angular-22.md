# ADR-011: Angular 22 for the web front end

**Status:** Accepted (applies ADR-010; amends the version named in ADR-006 and ADR-010 §4)
**Date:** 2026-11-01

## Context

Blueprint v2.0 and ADR-006 name Angular 20. ADR-010 established an LTS-only policy and deferred the
Angular version review to gate G5. Checking the official release schedule before writing the first line of
front-end code showed that the review cannot wait:

| Version | Released | Active support ends | LTS ends |
|---|---|---|---|
| Angular 20 | 2025-05 | 2025-11-19 | **2026-11-28** |
| Angular 21 | 2025-11 | 2026-06-03 | 2027-06 |
| Angular 22 | 2026-06-03 | 2027-06 | 2028-06 |

Angular 20 leaves support 27 days after this decision. Angular majors receive 12 months active plus
12 months LTS; the newest major therefore always has the longest remaining window. Angular 22 requires
Node 22.22+ or 24.15+, both of which are current Node LTS lines, and TypeScript 6.

## Decision

- The web front end targets **Angular 22** with Angular Material 22, standalone components, signals,
  OnPush, typed reactive forms and Transloco, exactly as ADR-006 specifies apart from the major version.
- The toolchain uses the Angular CLI defaults for v22: zoneless change detection, Vitest for unit tests,
  ESLint via angular-eslint.
- Node **22 LTS** (Compose, CI and Dockerfiles use the `22` line; `24` is acceptable).
- Major upgrades follow the Angular release train: adopt the new major within its first active quarter so
  the project is never on a version with less than 12 months of support left.

## Consequences

- `docs/blueprint/README.md` records the deviation from the PDF.
- The owner's Angular knowledge transfers directly; v20 → v22 changes are incremental (zoneless default,
  Vitest default, `@if`/`@for` control flow already standard).
- Any third-party Angular library must declare support for v22 before it is added. Verified for
  `angular-oauth2-oidc` 22.x and `@jsverse/transloco` 8.x.
- Interview-ready statement: "Front-end frameworks have support windows too. I checked Angular's schedule
  before scaffolding and chose the major with the longest remaining LTS."

## Sources

- angular.dev, "Versioning and releases" (release and support schedule)
- endoflife.date, Angular
