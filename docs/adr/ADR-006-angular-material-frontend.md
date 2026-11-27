# ADR-006: Angular 20 + Angular Material front end

**Status:** Accepted (major version amended by ADR-011: Angular 22 replaces Angular 20)
**Date:** 2026-11-01 (decision fixed in blueprint v2.0)

## Context

The owner is already proficient in Angular, and the front end is not one of the target skills for this
project. Front-end time should therefore be minimised and predictable, while still producing a
professional, accessible UI.

## Decision

- **Angular 20** with **Angular Material**.
- Standalone components, **signals**, `OnPush` change detection, typed reactive forms.
- **Transloco** for i18n; **English first**, Arabic (RTL) is **Release 2**. Strings are externalised from
  day one so Release 2 is a translation task, not a refactor.
- **Desktop-first dashboard** for managers and planners; **technician pages responsive at 360 px**.
- **No PWA and no offline mode** in Release 1.
- Authentication via OIDC with PKCE against Keycloak (ADR-008).

## Consequences

- Fast, familiar delivery; UI effort is bounded in the roadmap.
- Technician flow is usable on a phone browser but does not work without connectivity.
- Accessibility polish is on the cut list; the baseline uses Material's built-in a11y.
