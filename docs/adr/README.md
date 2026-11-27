# Architecture Decision Records

Decisions fixed by blueprint v2.0 and amended by v2.1 (ADR-010, ADR-011). Each ADR is short on purpose: context, decision, consequences.
A decision is changed only by a new ADR that supersedes it; existing ADRs are never edited in substance.

| ADR | Title | Status |
|---|---|---|
| [ADR-001](ADR-001-dotnet-for-workflow-python-for-ml.md) | .NET for workflow services, Python for ML | Accepted |
| [ADR-002](ADR-002-four-deployables-modular-monolith.md) | Four deployables on Azure Container Apps, modular monolith for the platform API | Accepted |
| [ADR-003](ADR-003-outbox-idempotent-consumers-kafka-api.md) | Transactional outbox, idempotent consumers and dead-letter topics over a Kafka-API bus | Accepted |
| [ADR-004](ADR-004-read-only-ot-boundary.md) | Read-only OT boundary over MQTT | Accepted |
| [ADR-005](ADR-005-model-registry-quality-gate-human-approval.md) | MLflow registry, quality gate and human approval before model promotion | Accepted |
| [ADR-006](ADR-006-angular-material-frontend.md) | Angular 20 + Angular Material front end | Accepted |
| [ADR-007](ADR-007-single-postgres-schema-per-module-timescale.md) | Single PostgreSQL with one schema per module, TimescaleDB for telemetry, Redis, Blob | Accepted |
| [ADR-008](ADR-008-keycloak-oidc-shared-db-tenancy.md) | Keycloak OIDC and shared-database tenancy with row-level security | Accepted |
| [ADR-009](ADR-009-optional-bounded-genai-assistant.md) | Optional bounded GenAI maintenance assistant | Accepted (conditional) |
| [ADR-010](ADR-010-lts-only-runtime-policy-dotnet-10.md) | LTS-only runtime policy; target .NET 10 (amends the runtime version in ADR-001/002) | Accepted |
| [ADR-011](ADR-011-angular-22.md) | Angular 22 for the web front end (applies ADR-010; amends ADR-006) | Accepted |

Template for new ADRs:

```markdown
# ADR-NNN: Title

**Status:** Proposed | Accepted | Superseded by ADR-MMM
**Date:** YYYY-MM-DD

## Context
## Decision
## Consequences
```
