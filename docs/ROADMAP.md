# Roadmap — 12 weeks, 240 hours

Solo developer. **5 days × 4 hours = 20 h/week**, 12 weeks, **240 h total**. Plan fixed by blueprint v2.0.
Tick boxes as tasks complete; record actual hours in the *Actual* column at the end of each week.

> All data in this project is synthetic. The system is advisory only. See [README](../README.md).

## Hour budget by workstream

| Workstream | Budget (h) | Actual (h) |
|---|---:|---:|
| Foundation (repo, Compose, skeletons, CI, auth shell) | 28 | |
| Business modules (Assets, Maintenance, Inventory, Audit/Notifications, UI) | 48 | |
| Simulator + telemetry (simulator, ingest, Timescale, charts) | 40 | |
| ML (Model A, Model B, MLflow, model cards, replay report) | 44 | |
| Azure / IaC / security (Terraform, pipeline, hardening, OTel) | 32 | |
| QA / observability (resilience, E2E, load, a11y, restore rehearsal) | 20 | |
| Docs / demo (README, C4 dossier, video, release) | 16 | |
| Buffer (optional AI assistant, slippage) | 12 | |
| **Total** | **240** | |

## Gates and definitions of done

| Gate | Week | Definition of done |
|---|---|---|
| **G1 Foundation** | 2 | `docker compose up` gives Keycloak login, an asset list in the UI, and green CI. |
| **G2 Useful operations** | 4 | A planner and a technician complete a work order end to end, with every step in the audit trail. |
| **G3 Telemetry** | 6 | An injected fault is visible on the live chart within 15 s; duplicate readings are not stored. |
| **G4 AI verified** | 8 | Both model cards committed; replay evaluation report shows the baseline beaten on the untouched test split. |
| **G5 Secure cloud** | 10 | `terraform apply` from a clean subscription succeeds; cross-tenant authZ suite green; budget alert configured. |
| **G6 Portfolio release** | 12 | Public repository, hosted demo URL, five-minute video, release v1.0 tagged. |

**Slip rule:** if any gate slips by more than 3 days, cut from this list in order: AI assistant → inventory
forecasts → analytics trends → canary deployment → accessibility polish.

## Week-by-week plan

### Week 1 — Repo, local stack, skeletons (Foundation, 20 h)

> Learning notes are budgeted at about 1 h per week inside the Docs / demo and Buffer lines.
- [x] Repository scaffold: layout, README, ADRs, plant spec, roadmap, CLAUDE.md, .gitignore
- [x] Commit blueprint v2.0 PDF under `docs/blueprint/`
- [x] Protected `main`: one pull request per week, owner squash-merges, CI required (`docs/BRANCH_PROTECTION.md`)
- [x] Issue blueprint v2.1 (.NET 10, Angular 22, MIT, week-1 practices) with a reproducible HTML→PDF generator
- [x] Choose licence (MIT)
- [x] ADR-010: target .NET 10 LTS; LTS-only runtime policy
- [x] Learning track: `docs/learning/README.md` skills matrix and note 01
- [x] `docker-compose.yml`: PostgreSQL 16 + TimescaleDB, Redis, Mosquitto, Redpanda, Keycloak, MLflow
- [x] Keycloak realm export with roles, tenant claim mapper and seed users (dev only, no secrets committed)
- [x] .NET modular solution skeleton: `Platform.Api` host, module projects (Assets, Maintenance, Inventory, Audit), `Ingest.Worker`, shared `BuildingBlocks`
- [x] Angular 22 shell (ADR-011): Material layout, Transloco, OIDC PKCE login against Keycloak, authenticated home page calling `/api/me`
- [x] CI: lint + test for .NET, Angular and Python; Docker build for each deployable
- [x] DevSecOps baseline in CI: gitleaks secret scan, Dependabot (grouped weekly), Conventional Commits check
- [x] `global.json` (SDK 10.0.x) and `Directory.Build.props` (`net10.0`, nullable, warnings as errors); NuGet packages verified for .NET 10
- [x] Learning note 02 (local platform engineering)
- [x] Health endpoints on every service (`/health/live`, `/health/ready`; worker: heartbeat log)
- [x] Python skeletons: ML service and simulator (uv, FastAPI, ruff, mypy, pytest; plant.yaml validated by tests)
- [ ] **Owner verification on a laptop with Docker:** `docker compose up -d` healthy, Keycloak login from the Angular shell, `/api/me` shows tenant and roles (this container has no Docker daemon, so it was not run here)

### Week 2 — Tenant, Assets, Audit → **G1** (Foundation 8 h + Business modules 12 h)
- [x] Tenant entity, `tenant_id` claim plumbing, EF Core global query filters, PostgreSQL RLS policies
- [x] Assets module: Plant / Line / Asset / SensorMapping CRUD, seeded from `docs/plant/plant.yaml`
- [x] Audit module: append-only `AuditEvent` with before/after hash and correlation ID
- [x] OpenAPI document published and checked in CI (`contracts/openapi/platform-api.v1.json`)
- [x] Angular asset explorer (list, detail, filter by line)
- [x] Architecture tests: module boundaries, no cross-schema access, no `plant/#` publisher outside simulator
- [x] Learning note 03 (modular monolith, RLS)
- [x] **G1 check:** `docker compose up` → login → asset list; CI green. Automated as the CI job
      `Gate G1 (compose end-to-end)`: `scripts/g1-check.sh` (API, curl) and `scripts/g1-browser-check.cjs`
      (Keycloak login in Chromium, screenshot kept as a build artefact)

### Week 3 — Maintenance module (Business modules 20 h)
- [x] MaintenanceRequest entity and API
- [x] WorkOrder state machine: Requested → Approved → Scheduled → In progress → Completed → Closed; side states Rejected, On hold, Cancelled
- [x] Closing requires disposition + checklist status; versioned edits
- [x] Approval flow and RBAC policies (planner approves, technician executes)
- [x] Findings with attachment upload (magic-byte validation, size limits)
- [x] Technician page responsive at 360 px (checked in CI by `scripts/technician-360-check.cjs`)
- [x] Unit tests for every state transition
- [x] Learning note 04 (domain modelling, audit hash chain)

### Week 4 — Inventory, notifications, messaging → **G2** (Business modules 16 h + Foundation 4 h)
- [ ] Inventory module: Part, Reservation; reservation cannot exceed available stock without documented override
- [ ] Notifications (in-app first; email adapter stubbed)
- [ ] Transactional outbox + Kafka publisher (Redpanda)
- [ ] Idempotent consumer library (.NET) with dedup via Redis and dead-letter topic
- [ ] JSON Schemas for events v1 under `contracts/`, validated in CI
- [ ] Maintenance Hub UI (planner board, work-order detail, approvals)
- [ ] Learning note 05 (outbox, idempotency, schema evolution)
- [ ] **G2 check:** planner + technician complete a work order end to end; audit trail complete

### Week 5 — Simulator v1 + ingest (Simulator + telemetry 20 h)
- [ ] Simulator: load `plant.yaml`, healthy-day generation with shift load and noise, MQTT publish (QoS 1)
- [ ] Ingest Worker: MQTT subscribe, validation, dedup, write to `telemetry.readings` hypertable, Redis last value
- [ ] `TelemetryReceived` event emission
- [ ] Timescale: hypertable with 1-day chunks, compression after 7 days, 30-day retention
- [ ] Live chart in the UI (last-value + recent window per asset)
- [ ] Throttled profile for cloud demos (one reading per metric per 30 s)
- [ ] Learning note 06 (MQTT, TimescaleDB, Redis)

### Week 6 — Faults, control API, data quality → **G3** (Simulator + telemetry 20 h)
- [ ] Fault library and degradation models (health index, linear / exponential / Weibull hazards)
- [ ] Simulator control API: scenario select, fault injection, time compression, replay
- [ ] Ground-truth labels written offline only
- [ ] Data-quality flags (dropout, stuck, spike, out-of-range, clock skew) in ingest
- [ ] Continuous aggregates at 1 min and 1 h
- [ ] "Maintenance completed" reverse notification to the simulator (only reverse path, ADR-004)
- [ ] Learning note 07 (degradation models, data quality)
- [ ] **G3 check:** injected fault visible on live chart within 15 s; duplicates rejected

### Week 7 — Model A end to end (ML 22 h)
- [ ] Data pipeline: UCI AI4I 2020 → bronze/silver/gold parquet
- [ ] Baselines (majority, logistic regression) → LightGBM; stratified split with untouched test set
- [ ] Metrics: PR-AUC, recall at fixed precision, Brier calibration; SHAP explanations
- [ ] MLflow tracking + registry; quality gate script
- [ ] FastAPI `/score` endpoint serving the Production model with `model_version`
- [ ] `FailureRiskScored` event; alert creation in Platform API
- [ ] Model card A committed (synthetic / public data labelled, advisory-only stated)
- [ ] Learning note 08 (ML engineering, MLflow, model cards)

### Week 8 — Model B, Risk Center → **G4** (ML 22 h)
- [ ] Window features (5-minute) from continuous aggregates
- [ ] Isolation Forest per asset class trained on healthy windows
- [ ] Persistence / hysteresis / cooldown logic; alert cap
- [ ] `AnomalyDetected` → `AlertRaised` / `AlertUpdated` with dedup key, review state, explanation JSON
- [ ] Replay evaluation report: precision at alert cap, detection delay per scenario
- [ ] Risk Center UI (ranked alerts, explanation panel, review actions)
- [ ] Model card B committed
- [ ] Human approval flow for model promotion (`ModelPromoted` event, audit)
- [ ] Learning note 09 (anomaly detection, alert fatigue, replay evaluation)
- [ ] **G4 check:** both model cards committed; replay report beats baseline

### Week 9 — Terraform + deploy pipeline (Azure / IaC / security 20 h)
- [ ] Terraform: resource group, Container Apps environment, PostgreSQL Flexible Server B1ms (+ Timescale), Event Hubs Standard (Kafka endpoint), Blob, Key Vault, Static Web App, budgets (AED 150, AED 250 alerts)
- [ ] Container Apps for the four deployables, scale-to-zero, managed identity
- [ ] GitHub Actions deploy pipeline (build → push → apply → smoke test)
- [ ] Deploy pipeline authenticates to Azure with OIDC federation (no cloud secrets stored in GitHub)
- [ ] Nightly stop/start schedule documented for the demo environment
- [ ] Learning note 10 (Terraform, Container Apps, cost control)

### Week 10 — Hardening, observability, optional assistant → **G5** (Azure / IaC / security 12 h + Buffer)
- [ ] Security hardening: headers, rate limits, secret rotation via Key Vault, dependency scanning
- [ ] Supply chain: CodeQL (SAST), Trivy container scan, SBOM generation, cosign image signing, threat model document
- [ ] Runtime version review against ADR-010 LTS policy (Angular, Python, PostgreSQL) before cloud deploy
- [ ] Cross-tenant authZ test suite in CI (every endpoint, tenant A vs tenant B)
- [ ] OpenTelemetry traces/metrics/logs → Azure Monitor; W3C trace IDs end to end
- [ ] *Optional (buffer only, if G1–G4 on time):* bounded GenAI assistant (ADR-009)
- [ ] Learning note 11 (DevSecOps, authZ testing, OpenTelemetry)
- [ ] **G5 check:** clean-subscription `terraform apply`; authZ suite green; budget alert configured

### Week 11 — Quality (QA / observability 20 h)
- [ ] Resilience tests: broker down, DB restart, duplicate storm, poison message → dead letter
- [ ] E2E tests (Playwright) for the planner and technician flows
- [ ] Load test on ingest at full profile (~560k/day) and API hot paths
- [ ] Accessibility baseline check (cut candidate: polish)
- [ ] Backup / restore rehearsal with documented RPO/RTO
- [ ] Learning note 12 (reliability engineering)

### Week 12 — Release → **G6** (Docs / demo 16 h + Buffer)
- [ ] Five-minute demo video (scenario `demo-5-minutes`)
- [ ] README final pass, C4 dossier (context, container, component), ADR index
- [ ] Model cards final review; synthetic-data labels verified on every screen and chart
- [ ] Release v1.0 tag, hosted demo URL, public repository
- [ ] Learning note 13 (telling the story) and final skills matrix review
- [ ] **G6 check:** public repo, hosted demo, video

## Out of scope for Release 1

RUL model on NASA C-MAPSS, Arabic localisation, inventory forecasts, analytics trends, PWA/offline,
Kubernetes (documented as migration path only), any PLC write path (never).

## Cost envelope

| Item | Estimate |
|---|---|
| Whole project | AED 700–1,600 |
| Hosted demo | AED 40–290 / month (PostgreSQL stopped and Container Apps at zero overnight) |
| Budget alerts | AED 150 and AED 250 |
