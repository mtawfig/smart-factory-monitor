# ADR-007: Single PostgreSQL 16 with one schema per module, TimescaleDB for telemetry, Redis and Blob

**Status:** Accepted
**Date:** 2026-11-01 (decision fixed in blueprint v2.0)

## Context

One database per service is the microservice ideal but multiplies cost and operations for a solo
project. Telemetry at roughly 560k readings/day needs time-series features (compression, retention,
rollups) that plain PostgreSQL handles poorly. ML needs columnar datasets, not row stores.

## Decision

- **One PostgreSQL 16 instance** (Flexible Server B1ms in Azure) with **one schema per module**:
  `assets`, `maintenance`, `inventory`, `audit`, `telemetry`. A module's code may only touch its own
  schema; this is enforced by separate EF Core `DbContext`s and database roles.
- **TimescaleDB** extension for telemetry: hypertable `telemetry.readings` with **1-day chunks**,
  **compression after 7 days**, **30-day raw retention**, and **continuous aggregates at 1 minute and
  1 hour** for charts and features.
- **Redis** for the last value per asset, event deduplication (`event_id` set, 10-minute TTL) and
  rate limits.
- **Blob storage with parquet** in bronze / silver / gold layers for ML training datasets and the
  offline ground-truth labels.

## Consequences

- Cheapest credible production shape; one backup/restore story (restore rehearsal is in Week 11).
- Module extraction later only needs a schema move, not a data model redesign.
- Timescale features must be present locally (Compose image) and in Azure (extension allow-list).
- Redis is a cache, never a source of truth; losing it degrades latency, not correctness.
