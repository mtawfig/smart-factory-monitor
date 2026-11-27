# ADR-009: Optional, bounded GenAI maintenance assistant

**Status:** Accepted (conditional: built only from the 12-hour buffer if gates G1–G4 close on time)
**Date:** 2026-11-01 (decision fixed in blueprint v2.0)

## Context

A generative assistant is a marketable feature, but an unbounded one would undermine the advisory-only
and honesty principles of the project and could consume unbounded time and money. It is the first item
on the cut list.

## Decision

If, and only if, gates G1 to G4 close on schedule, a **bounded** assistant is added with exactly three
capabilities:

1. **Alert explanation** — narrate an alert's SHAP/anomaly explanation in plain language.
2. **Work-order draft** — propose a work-order title, tasks and parts from an alert and asset history,
   for a planner to edit and approve.
3. **History Q&A** — answer questions over the tenant's own maintenance history.

Constraints: **read-only** (it never changes state), **cited** (every statement links to the record it came
from), **token-budgeted** per tenant per day, tenant-scoped by the same authorisation as the API, and
clearly labelled as AI-generated and advisory.

## Consequences

- Zero impact on the plan if it is cut; its absence does not block any gate.
- If built, prompts, budgets and evaluation examples live in the repository; no provider keys do.
- Human approval remains the only path to a real work order.
