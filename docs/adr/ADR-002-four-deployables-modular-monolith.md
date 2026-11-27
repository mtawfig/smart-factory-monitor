# ADR-002: Four deployables on Azure Container Apps; the Platform API is a modular monolith

**Status:** Accepted (runtime version amended by ADR-010: .NET 10 LTS replaces .NET 8)
**Date:** 2026-11-01 (decision fixed in blueprint v2.0)

## Context

Blueprint v1.0 proposed six microservices, three ML models, Kafka, Terraform and a PWA in 12 weeks.
The feasibility review concluded that this was a team plan, not a solo 240-hour plan. Each extra
deployable costs CI, deployment, observability, security and documentation time, with little
portfolio upside beyond the second or third service.

## Decision

Exactly **four deployables**, each a container on **Azure Container Apps** with scale-to-zero:

| # | Deployable | Technology |
|---|---|---|
| a | **Platform API** | ASP.NET Core 10 **modular monolith** with modules Assets, Maintenance, Inventory, Audit/Notifications |
| b | **Ingest Worker** | .NET 10 worker service (MQTTnet, Npgsql, StackExchange.Redis) |
| c | **ML Service** | Python 3.12 FastAPI (scikit-learn, LightGBM, SHAP, MLflow client) |
| d | **Plant Simulator** | Python 3.12 (paho-mqtt publisher + FastAPI control API) |

Module boundaries inside the Platform API are enforced with architecture tests (no cross-module
project references except through public contracts), one database schema per module (ADR-007) and
events through the outbox (ADR-003). Kubernetes is **documented as a migration path only**, not built.

## Consequences

- One deployment pipeline per deployable, four in total; Compose locally mirrors the cloud.
- A module can later be extracted into its own service without rewriting: it already has its own schema,
  contracts and events.
- Fewer network hops and no distributed transactions inside the business core.
- Scale-to-zero keeps the hosted demo cheap; cold starts are accepted for a portfolio demo.
