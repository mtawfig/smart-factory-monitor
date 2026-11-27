# SmartFactory AI — Industrial Predictive Maintenance & Asset Management Platform

> **Portfolio project 02 of 06.** A solo, 12-week (240 hour) build of a cloud-native predictive-maintenance
> platform for a **fully simulated** factory. Everything here is designed to be demonstrated, read and
> reasoned about; nothing here is connected to a real plant.

[![CI](https://github.com/mtawfig/smartfactory-ai-pdm/actions/workflows/ci.yml/badge.svg)](https://github.com/mtawfig/smartfactory-ai-pdm/actions/workflows/ci.yml)
[![Status](https://img.shields.io/badge/status-week%201%20foundation-blue)](docs/ROADMAP.md)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

---

## Two boundaries that apply everywhere

| Boundary | What it means in this repository |
|---|---|
| **Advisory only** | The system observes, scores and recommends. It never commands equipment. There is **no PLC write path in the code**, and the only message that flows back towards the plant is a "maintenance completed" notification to the *simulator*. Humans approve every work order and every model promotion. |
| **Synthetic data only** | All telemetry comes from the in-repo plant simulator. Model A is trained on the public UCI AI4I 2020 dataset. Every screen, chart, dataset and model card is labelled *synthetic*. The project makes **no claims of achieved savings, uptime gains or field accuracy**. |

These rules are repeated in [CLAUDE.md](CLAUDE.md), in the ADRs and in the model cards. They are not negotiable.

---

## What it does

SmartFactory AI turns a stream of machine sensor readings into **ranked, explained maintenance advice** and
tracks the resulting work from request to closure:

1. **Simulated plant** — "Al Safa Industrial" (fictional, Asia/Dubai): 2 lines, 8 machines, 32 metrics,
   a fault library (bearing wear, overheating, cavitation, belt slip, tool wear, clogging, hydraulic leak,
   sensor faults) and scenario presets. See [`docs/plant/plant.yaml`](docs/plant/plant.yaml).
2. **Telemetry ingest** — MQTT → .NET worker → TimescaleDB hypertable + Redis last-value cache, with
   deduplication and data-quality flags.
3. **Two ML models** —
   *Model A*: failure classifier (LightGBM vs. baselines, PR-AUC, calibration, SHAP explanations), served at `/score`.
   *Model B*: per-asset-class anomaly detection (Isolation Forest on 5-minute window features, hysteresis and cooldown).
   Both are gated by a quality bar and a human approval step before promotion (MLflow registry, model cards).
4. **Maintenance operations** — alerts with review state → maintenance requests → work orders with a
   full state machine, approvals, findings with attachments, part reservations and an append-only audit trail.
5. **Multi-tenant, role-based** — Keycloak OIDC, five personas (reliability manager, maintenance planner,
   technician, data/ML engineer, administrator), row-level security in PostgreSQL.

---

## Architecture at a glance

Four deployables, one database, one event bus. Kubernetes is documented only as a migration path.

```
                 MQTT (QoS 1, read-only OT boundary)
 ┌───────────────┐  plant/{tenant}/{plant}/{line}/{asset}/{metric}  ┌────────────────┐
 │ Plant         │ ───────────────────────────────────────────────▶ │ Ingest Worker  │
 │ Simulator     │ ◀──── "maintenance completed" (only reverse msg) │ (.NET 10)       │
 │ (Python 3.12) │                                                  └───────┬────────┘
 └───────────────┘                                                          │ Npgsql / Redis
                                                                            ▼
 ┌───────────────┐   REST / OpenAPI    ┌─────────────────────┐   ┌──────────────────────────┐
 │ Web           │ ◀─────────────────▶ │ Platform API        │◀─▶│ PostgreSQL 16            │
 │ (Angular 20)  │   OIDC (Keycloak)   │ ASP.NET Core 10      │   │ + TimescaleDB            │
 └───────────────┘                     │ modular monolith:   │   │ schemas: assets,         │
                                       │ Assets · Maintenance│   │ maintenance, inventory,  │
                                       │ Inventory · Audit/  │   │ audit, telemetry         │
                                       │ Notifications       │   └──────────────────────────┘
                                       └──────────┬──────────┘   ┌──────────────────────────┐
                                                  │ outbox       │ Redis (last value, dedup,│
                                                  ▼              │ rate limits)             │
                                   ┌──────────────────────────┐  └──────────────────────────┘
                                   │ Kafka API bus            │
                                   │ Redpanda (local)         │
                                   │ Azure Event Hubs (cloud) │
                                   └──────────┬───────────────┘
                                              ▼
                                   ┌──────────────────────────┐   ┌──────────────────────────┐
                                   │ ML Service (FastAPI)     │◀─▶│ MLflow registry          │
                                   │ /score · anomaly scoring │   │ Blob/parquet bronze/     │
                                   │ scikit-learn · LightGBM  │   │ silver/gold datasets     │
                                   │ SHAP                     │   └──────────────────────────┘
                                   └──────────────────────────┘
```

### The four deployables

| Deployable | Path | Stack | Role |
|---|---|---|---|
| **Platform API** | `src/platform-api` | ASP.NET Core 10, EF Core, Npgsql | Modular monolith: Assets, Maintenance, Inventory, Audit/Notifications. Owns the outbox. |
| **Ingest Worker** | `src/ingest-worker` | .NET 10 worker, MQTTnet, Npgsql, StackExchange.Redis | Subscribes to MQTT, validates, deduplicates, writes telemetry and last values, emits `TelemetryReceived`. |
| **ML Service** | `src/ml-service` | Python 3.12, FastAPI, scikit-learn, LightGBM, SHAP, MLflow client | Trains and serves Model A and Model B; emits `FailureRiskScored` and `AnomalyDetected`. |
| **Plant Simulator** | `src/simulator` | Python 3.12, paho-mqtt, FastAPI control API | Generates the synthetic plant, injects faults, replays scenarios. |

Plus the **Web** front end (`src/web`, Angular 22 + Angular Material, standalone components, signals, OnPush,
Transloco i18n-ready; English first, Arabic in Release 2) and **infrastructure** (`infra/terraform`, Azure
Container Apps with scale-to-zero, PostgreSQL Flexible Server, Event Hubs Standard, Blob, Key Vault,
Static Web App, budgets).

### Tech stack summary

| Concern | Choice |
|---|---|
| Workflow services | .NET 10 / C# (ASP.NET Core, EF Core) |
| ML training and inference | Python 3.12 (scikit-learn, LightGBM, SHAP, MLflow) |
| Front end | Angular 22 + Angular Material (ADR-011) |
| Database | PostgreSQL 16 + TimescaleDB, one schema per module, row-level security |
| Cache | Redis |
| Messaging | MQTT (Mosquitto) at the OT edge; Kafka API (Redpanda locally, Azure Event Hubs in the cloud) |
| Identity | Keycloak OIDC (PKCE for the SPA), roles + tenant claim |
| Containers and cloud | Docker Compose locally; Azure Container Apps, Terraform |
| Observability | OpenTelemetry → Azure Monitor |
| Contracts | JSON Schemas under `contracts/`, events versioned `v1` |

Design decisions are recorded as ADRs in [`docs/adr`](docs/adr). Runtime versions follow an LTS-only policy (ADR-010).
The full product specification (overview, business requirements, functional and technical specification) is the
[Software Project Specification Document](docs/specifications/smartfactory-ai-spsd.md)
([PDF](docs/specifications/smartfactory-ai-spsd.pdf)).

**Learning track:** every week ships a note in [`docs/learning`](docs/learning) explaining what was built, why,
and how to talk about it in an interview. Start with the [skills coverage matrix](docs/learning/README.md).

---

## Repository layout

```
.
├── src/
│   ├── platform-api/     ASP.NET Core 10 modular monolith
│   ├── ingest-worker/    .NET 10 MQTT → TimescaleDB worker
│   ├── ml-service/       Python FastAPI model training and serving
│   ├── simulator/        Python plant simulator + control API
│   └── web/              Angular 22 front end
├── infra/terraform/      Azure infrastructure as code
├── contracts/            JSON Schemas for events and API payloads
├── docs/
│   ├── adr/              Architecture decision records (ADR-001 … ADR-011)
│   ├── learning/         Weekly learning notes and skills coverage matrix
│   ├── blueprint/        Blueprint v2.1 PDF (+ generator source)
│   ├── plant/            Simulated plant specification (plant.yaml)
│   └── ROADMAP.md        Week-by-week plan, hour budgets, gates
├── .github/workflows/    CI/CD
├── docker-compose.yml    Local stack (to be added in Week 1)
├── CLAUDE.md             Conventions for AI-assisted development
└── README.md
```

---

## 12-week plan summary

Solo developer, 5 days × 4 hours per week, **240 hours total**. Full detail with checkboxes in
[`docs/ROADMAP.md`](docs/ROADMAP.md).

| Weeks | Focus | Gate |
|---|---|---|
| 1–2 | Repo, Docker Compose stack, .NET solution skeleton, Angular shell with OIDC login, CI; Tenant + Assets + Audit modules | **G1 Foundation** — `docker compose up` gives login, asset list and green CI |
| 3–4 | Maintenance module (requests, work-order state machine, approvals, findings); Inventory, notifications, outbox + Kafka publisher, idempotent consumers, Maintenance Hub UI | **G2 Useful operations** — planner and technician complete a work order end to end with audit |
| 5–6 | Simulator v1, ingest worker, live chart; fault library, degradation models, control API, replay, data-quality flags, continuous aggregates | **G3 Telemetry** — injected fault visible on live chart within 15 s; duplicates not stored |
| 7–8 | Model A end to end + model card; Model B + Risk Center UI + replay evaluation report | **G4 AI verified** — both model cards committed; replay report shows baseline beaten |
| 9–10 | Terraform + deploy pipeline; security hardening, OpenTelemetry → Azure Monitor, optional AI assistant | **G5 Secure cloud** — Terraform apply from a clean subscription; authZ suite green; budget alert configured |
| 11–12 | Resilience, E2E, load and accessibility tests, restore rehearsal; demo video, README, C4 dossier, model cards, release v1.0 | **G6 Portfolio release** — public repo, hosted demo URL, five-minute video |

Hour budget by workstream: foundation 28 · business modules 48 · simulator + telemetry 40 · ML 44 ·
Azure/IaC/security 32 · QA/observability 20 · docs/demo 16 · buffer 12.

**Cut list** if any gate slips by more than 3 days: AI assistant, inventory forecasts, analytics trends,
canary deployment, accessibility polish.

**Cost envelope:** AED 700–1,600 for the whole project; hosted demo AED 40–290/month with PostgreSQL stopped
and Container Apps scaled to zero overnight. Budget alerts at AED 150 and AED 250.

---

## Getting started

Prerequisites: Docker Desktop (or Engine + Compose v2), .NET 10 SDK, Node 22, [uv](https://docs.astral.sh/uv/).

```bash
cp .env.example .env                  # replace the placeholders (local-only passwords)
docker compose up -d --build          # infrastructure + Platform API (:5080) + web app (:4200)
docker compose ps                     # wait for "healthy"; keycloak-seed exits 0 after creating the demo users
scripts/g1-check.sh                   # optional: the Gate G1 end-to-end check
```

Open http://localhost:4200 and sign in with one of the seeded personas (`manager`, `planner`, `technician`,
`mlengineer`, `admin-sf`) and the `DEV_USER_PASSWORD` from your `.env`. **Assets** lists the simulated plant's
eight machines. On first start the API creates its schemas, row-level security policies and seed data itself.
Sign in as `technician` for **My work**, the phone-sized page for jobs a planner has scheduled for you (the
planner's hub arrives in week 4; until then, jobs are raised and scheduled through the API at `/scalar`).

For coding, stop the two app containers and run them from source on the same ports:

```bash
docker compose stop platform-api web
dotnet run --project src/platform-api/SmartFactory.Platform.Api   # API on http://localhost:5080 (docs at /scalar)
cd src/web && npm ci && npm start                                  # UI on http://localhost:4200
```

Service URLs and ports: [`infra/compose/README.md`](infra/compose/README.md). All commands: [`CLAUDE.md`](CLAUDE.md).

**Workflow:** each roadmap week is delivered as one pull request that must pass CI and is squash-merged by the
owner; nobody pushes to `main`. Settings: [`docs/BRANCH_PROTECTION.md`](docs/BRANCH_PROTECTION.md).

---

## Release scope

**Release 1 (this plan):** everything above.
**Release 2 (not in scope):** Remaining-useful-life model on NASA C-MAPSS, Arabic localisation, inventory
forecasts, analytics trends.

## Licence

MIT. See [LICENSE](LICENSE). Datasets and third-party components keep their own licences.
