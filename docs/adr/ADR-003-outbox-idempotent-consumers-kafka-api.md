# ADR-003: Transactional outbox, idempotent consumers and dead-letter topics over a Kafka-API bus

**Status:** Accepted
**Date:** 2026-11-01 (decision fixed in blueprint v2.0)

## Context

State changes in the Platform API (work order approved, maintenance closed) must reach other components
reliably, and telemetry-derived events (risk scores, anomalies, alerts) must flow between the Ingest
Worker, ML Service and Platform API. Dual writes (database then broker) lose or duplicate messages.
The bus must run locally for free and in Azure cheaply.

## Decision

- **Transactional outbox** in every producer that owns a database: the event row is written in the same
  transaction as the state change; a relay publishes it to the bus.
- **Idempotent consumers**: every consumer deduplicates on `event_id` (Redis set with 10-minute TTL
  plus a persisted processed-events table where needed) and treats redelivery as normal.
- **Dead-letter topics** per consumer group for poison messages, with an operator view.
- Bus is the **Kafka API**: **Redpanda** in Docker Compose locally, **Azure Event Hubs Standard**
  (Kafka endpoint) in the cloud. No code depends on broker-specific features.
- **Events v1**: `TelemetryReceived`, `FailureRiskScored`, `AnomalyDetected`, `AlertRaised`,
  `AlertUpdated`, `WorkOrderApproved`, `MaintenanceClosed`, `ModelPromoted`.
- **Common envelope** on every event: `event_id` (UUID v7), `schema_version`, `tenant_id`, `asset_id`,
  `event_time_utc`, `trace_id` (W3C Trace Context).
- **JSON Schemas live under `/contracts`** and are validated in CI by both the .NET and Python sides.
  Breaking changes require a new `schema_version` and a new topic suffix.

## Consequences

- At-least-once delivery everywhere; exactly-once is never assumed.
- A small shared "idempotent consumer" library in .NET and an equivalent helper in Python.
- Event Hubs Standard is the single largest fixed cloud cost; it may be stopped between demos.
- Schema discipline is a visible portfolio artefact.
