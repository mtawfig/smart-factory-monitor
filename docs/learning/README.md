# Learning track

This project is built to be **shipped and learned from at the same time**. Every week ends with a learning
note in this folder: what was built, the concept behind it, why this option and not the alternatives, what
went wrong, and how to explain it in an interview in two sentences. Notes are written in plain language and
link to the code that proves them.

> All data in this project is synthetic. The system is advisory only.

## How to use this folder

1. Before a week starts, read the note's "Concepts you will meet" section (written up front).
2. Build the week's tasks from `docs/ROADMAP.md`.
3. After the week, read the full note and answer the "Check yourself" questions without looking.
4. Add your own "What surprised me" paragraph. The note is yours.

## Notes

| # | Note | Week | Status |
|---|---|---|---|
| 01 | [Foundation decisions: monolith vs microservices, LTS runtimes, outbox, Kafka API, what "NoSQL" means here](01-foundation-decisions.md) | 0 | Written |
| 02 | [Local platform engineering: Compose graph, pinning, OIDC + PKCE, Dockerfiles, .NET 10 specifics, fitness functions, CI as a contract](02-local-platform.md) | 1 | Written |
| 03 | [Modular monolith in practice: schema per module, tenancy in three layers, RLS with `SET ROLE`, hash-chained audit, contract tests, Gate G1 as a script](03-modular-monolith-tenancy.md) | 2 | Written |
| 04 | [Domain modelling: the work-order state machine, role- and resource-based authorisation, versioned edits, secure uploads, 360 px as a tested requirement](04-domain-modelling-work-orders.md) | 3 | Written |
| 05 | Event-driven integration: outbox relay, idempotent consumers, dead letters, schema evolution | 4 | Planned |
| 06 | Industrial IoT: MQTT topic design, QoS, time-series storage with TimescaleDB, Redis as cache | 5 | Planned |
| 07 | Simulation and data quality: degradation models, hazard functions, sensor faults vs failures | 6 | Planned |
| 08 | ML engineering: baselines first, leakage, PR-AUC and calibration, SHAP, MLflow registry, model cards | 7 | Planned |
| 09 | Anomaly detection in production: windows, Isolation Forest, hysteresis, alert fatigue, replay evaluation | 8 | Planned |
| 10 | Cloud and IaC: Terraform modules, Container Apps, managed identity, cost control, scale-to-zero | 9 | Planned |
| 11 | DevSecOps: supply chain (gitleaks, CodeQL, Trivy, SBOM, signed images), authZ testing, OpenTelemetry | 10 | Planned |
| 12 | Reliability engineering: chaos scripts, E2E, load tests, backup/restore, RPO/RTO | 11 | Planned |
| 13 | Telling the story: C4 diagrams, demo narrative, honest claims, portfolio positioning | 12 | Planned |

## Skills coverage matrix

What a reviewer can verify in this repository, how deep it goes, and where it lives in the plan.
Depth: **Core** = built and tested by you; **Applied** = used as a tool with understanding of trade-offs;
**Documented** = explained and designed but not built in Release 1; **Not covered** = outside scope.

| Skill area | Depth | What you will actually do | Weeks |
|---|---|---|---|
| **Software architecture** | Core | ADRs, C4 model, modular monolith with enforced boundaries, bounded contexts, contracts-first integration, fitness functions (architecture tests) | 1–12 |
| **Microservices patterns** | Core | 4 deployables; transactional outbox, idempotent consumers, dead-letter topics, versioned event schemas, health checks, scale-to-zero. Honest framing: *modular monolith + 3 services*, which is what most real teams should build first | 2, 4, 5 |
| **Event streaming / Kafka** | Core | Kafka API producers and consumers (Redpanda locally, Azure Event Hubs Kafka endpoint in the cloud), consumer groups, partitions by `asset_id`, offsets, replay, schema compatibility checks in CI | 4, 5, 8 |
| **Containerisation** | Core | Multi-stage Dockerfiles for .NET, Python and Angular; Compose with 10 services and health checks; image size budgets; signed images; Azure Container Apps. Kubernetes: **Documented** as migration path only | 1, 9 |
| **DevOps / CI-CD** | Core | GitHub Actions: lint, type checks, unit and architecture tests, Docker builds, Terraform plan/apply, smoke tests, release tags, rollback runbook, nightly jobs | 1, 9, 12 |
| **DevSecOps** | Core | gitleaks (secrets), Dependabot and dependency review, CodeQL (SAST), Trivy (container scan), SBOM, cosign image signing, Key Vault, OIDC federation for the deploy pipeline (no cloud keys in GitHub), cross-tenant authZ test suite, threat model | 1, 10 |
| **Infrastructure as Code** | Core | Terraform modules for resource group, Container Apps environment, PostgreSQL Flexible Server, Event Hubs, Blob, Key Vault, Static Web App, Monitor, Budgets; remote state; plan review in PRs | 9 |
| **Azure** | Applied | Container Apps, PostgreSQL Flexible Server, Event Hubs, Blob Storage, Key Vault, Static Web Apps, Azure Monitor, Budgets, managed identity | 9–10 |
| **.NET / C#** | Core | ASP.NET Core 10 minimal APIs, EF Core 10 with multiple schemas and global query filters, worker services, policy-based authorisation, OpenTelemetry, xUnit + architecture tests | 1–5 |
| **Python** | Core | FastAPI services, pydantic models, paho-mqtt, pandas/pyarrow pipelines, scikit-learn, LightGBM, SHAP, MLflow, pytest, ruff/mypy, uv | 5–8 |
| **PostgreSQL / SQL** | Core | Schema-per-module, migrations, row-level security, JSONB for explanations, query plans, indexes, retention | 2–6 |
| **Time-series data** | Core | TimescaleDB hypertables, chunking, compression, continuous aggregates, retention policies | 5–6 |
| **NoSQL** | Applied | **Redis** as a key-value store (last value per asset, dedup sets with TTL, rate limits) and **Blob/parquet** as object storage. There is **no document database** in Release 1; PostgreSQL JSONB covers semi-structured data. Cosmos DB is a Release 2 item in the blueprint. See note 01 for how to talk about this honestly | 4–7 |
| **Machine learning** | Core | Supervised classification on tabular data, baselines, stratified splits, leakage checks, PR-AUC, recall at fixed precision, Brier calibration, SHAP; unsupervised anomaly detection, windowing, hysteresis; replay evaluation | 7–8 |
| **MLOps** | Core | MLflow tracking and registry, quality gate vs champion, human approval, model cards, drift monitoring (PSI), reproducible training with fixed seeds | 7–8, 10 |
| **Identity and access** | Core | OIDC with PKCE, JWT validation, roles and tenant claims, RBAC matrix, policy-based authorisation, multi-tenant isolation in three layers | 1–2, 10 |
| **Observability / SRE** | Core | OpenTelemetry traces, metrics and logs; W3C trace context through events; RED metrics; SLO table; Azure Monitor dashboards | 5, 10 |
| **Industrial IoT** | Applied | MQTT topic design, QoS 1 semantics, sensor catalogue, data-quality flags, read-only OT boundary, OPC UA extension point (**Documented**) | 4–6 |
| **Front end (Angular)** | Applied | Angular 22, Material, signals, OIDC login, charts, responsive technician flow. Not a target skill; kept minimal | 1–8 |
| **GenAI engineering** | Documented (optional build) | Bounded, cited, token-budgeted assistant; only if the buffer allows | 10 |
| **Kubernetes** | Documented | Migration path from Container Apps; not built | 12 |
| **Streaming analytics (Flink, Spark)** | Not covered | Out of scope; aggregations are done in TimescaleDB | — |
| **Mobile / PWA** | Not covered | Release 2 | — |

## Interview framing you can use honestly

- "I built a predictive-maintenance platform end to end: simulated plant → MQTT → .NET ingest → TimescaleDB,
  two documented ML models behind a versioned API, event-driven integration over the Kafka API, and a
  Terraform-provisioned Azure deployment with budget controls and a DevSecOps pipeline."
- "It is a modular monolith plus three services, on purpose. I can explain exactly which seam I would cut
  first and why, and the code already has the schema and contract boundaries to do it."
- "Everything runs on synthetic data and the system is advisory only. I make no accuracy or savings claims."
