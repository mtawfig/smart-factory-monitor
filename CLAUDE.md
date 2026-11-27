# CLAUDE.md — conventions for AI-assisted development

This file is read by Claude Code (and humans) at the start of every session. It is the short version of the
blueprint; when it conflicts with an ADR, the ADR wins and this file must be fixed.

## Project in one paragraph

SmartFactory AI is a solo, 12-week portfolio project: a cloud-native predictive-maintenance and asset-management
platform for a **fully simulated** factory ("Al Safa Industrial", fictional). Four deployables (Platform API,
Ingest Worker, ML Service, Plant Simulator), one PostgreSQL/TimescaleDB, Redis, a Kafka-API bus, Keycloak, an
Angular front end, all on Azure Container Apps via Terraform. Plan and gates: `docs/ROADMAP.md`. Decisions:
`docs/adr/`. Plant: `docs/plant/plant.yaml`. Handbook: `docs/blueprint/smartfactory-ai-blueprint-v2.1.pdf` (regenerate it
when an ADR changes the stack; see `docs/blueprint/README.md`).

## Non-negotiable content rules

1. **Advisory only.** The system never commands equipment. No PLC write path exists or may be added. The only
   reverse message is "maintenance completed" to the *simulator*. Humans approve work orders and model promotions.
2. **Synthetic data, labelled as such.** Every screen, chart, dataset, report and model card carries a visible
   "synthetic data" label. Model A uses the public UCI AI4I 2020 dataset and says so.
3. **No performance claims.** Never write that the system achieved savings, uptime, ROI or field accuracy.
   Metrics are reported on synthetic or public data only, with that caveat attached.
4. **No secrets in the repository.** No keys, passwords, connection strings or tokens, including in Compose files,
   Terraform, Keycloak exports, notebooks or test fixtures. Use `.env.example` with placeholders and Key Vault.

## Branching and commits

- **`main` is protected. All work arrives through a pull request that the owner merges.** Never push to `main`,
  never merge a pull request, never approve one, and never bypass or change the protection rule.
- **One branch per week**, cut from the latest `main`: `week-NN-<topic>` (for example `week-02-assets`). Small
  changes outside the weekly plan get their own branch: `fix/<topic>`, `docs/<topic>`, `chore/<topic>`.
- Push with `git push -u origin <branch>`, open the pull request early as a draft, and mark it ready for review
  when the week's checklist is done and CI is green. The description follows `.github/pull_request_template.md`.
- Merge method is **squash**: the pull request title becomes the single commit on `main`, so the title must be a
  valid Conventional Commit (for example `feat(assets): tenant and asset registry with row-level security`).
- After the owner merges, start the next branch from the updated `main`. Never stack new work on a merged branch.
- **Conventional Commits** with a scope in parentheses:
  `feat(assets): …`, `fix(ingest): …`, `docs(adr): …`, `chore(ci): …`, `refactor(maintenance): …`,
  `test(authz): …`, `ci(deploy): …`. Scopes: `repo`, `compose`, `platform-api`, `assets`, `maintenance`,
  `inventory`, `audit`, `ingest`, `ml`, `simulator`, `web`, `contracts`, `infra`, `ci`, `docs`, `adr`, `plant`,
  `deps`, `learning`, `blueprint`, `security`, `authz`.
- One logical change per commit; the body explains *why* when it is not obvious.
- Never include model identifiers or tool names in commit messages, code comments or pushed files.

## Architecture rules (summary of the ADRs)

| Rule | Source |
|---|---|
| C# / .NET 10 for Platform API and Ingest Worker; Python 3.12 for ML Service and Simulator. No shared code across the language boundary; contracts only. | ADR-001 |
| Exactly four deployables. Platform API is a modular monolith with modules Assets, Maintenance, Inventory, Audit/Notifications. Modules talk through public contracts and events, never through each other's internals or schemas. Kubernetes is documentation only. | ADR-002 |
| State changes publish through a transactional outbox. Consumers are idempotent (dedup on `event_id`) and have dead-letter topics. Bus is the Kafka API (Redpanda locally, Event Hubs in Azure). Events v1 and their envelope (`event_id` UUID v7, `schema_version`, `tenant_id`, `asset_id`, `event_time_utc`, `trace_id`) are defined by JSON Schemas in `contracts/`. | ADR-003 |
| Read-only OT boundary. Simulator publishes MQTT QoS 1 on `plant/{tenant}/{plant}/{line}/{asset}/{metric}`. Nothing else publishes on `plant/#`. | ADR-004 |
| Models are registered in MLflow, must beat the baseline on the untouched test split, and are promoted only by a human. Every model has a model card. | ADR-005 |
| Angular 22 + Material (ADR-011), standalone components, signals, OnPush, typed reactive forms, Transloco. English first. Desktop-first; technician pages work at 360 px. No PWA. | ADR-006 |
| One PostgreSQL 16, one schema per module (`assets`, `maintenance`, `inventory`, `audit`, `telemetry`). TimescaleDB hypertable `telemetry.readings` (1-day chunks, compress after 7 days, 30-day retention, 1 min / 1 h continuous aggregates). Redis is cache only. Parquet bronze/silver/gold in Blob for ML. | ADR-007 |
| Keycloak OIDC with PKCE; roles + `tenant_id` claim. Tenancy enforced three ways: EF Core global filters, PostgreSQL RLS, policy-based authorisation. Cross-tenant authZ tests are mandatory for every endpoint. | ADR-008 |
| GenAI assistant is optional, read-only, cited, token-budgeted, and built only from the buffer if G1–G4 are on time. | ADR-009 |
| LTS-only runtimes. .NET 10 (`net10.0`, SDK 10.0.x) for all .NET code. Adopting an STS release needs a new ADR. | ADR-010 |
| Angular 22, Node 22 LTS; adopt each new Angular major within its first active quarter. | ADR-011 |

## Domain vocabulary

Tenant · Plant · Line · Asset · SensorMapping · TelemetryReading · Alert (dedup key, review state,
`model_version`, explanation JSON) · MaintenanceRequest · WorkOrder (Requested → Approved → Scheduled →
In progress → Completed → Closed; side states Rejected, On hold, Cancelled) · Finding · Part · Reservation ·
AuditEvent (append-only, before/after hash, correlation ID) · OutboxMessage · ModelVersion.
Personas: reliability manager, maintenance planner, technician, data/ML engineer, administrator.

## Engineering conventions

- **.NET:** target `net10.0` (ADR-010), nullable enabled, warnings as errors, `TreatWarningsAsErrors` in CI, xUnit + FluentAssertions,
  architecture tests (NetArchTest or ArchUnitNET), EF Core migrations checked in, no raw SQL outside
  repositories except Timescale DDL in dedicated migration files.
- **Python:** `uv` for dependencies, `ruff` + `mypy`, `pytest`, type hints everywhere, no notebooks committed
  without cleared outputs.
- **Angular:** strict TypeScript, ESLint, Prettier, no `any`, no `ngModel`, no NgModules; selector prefix `sf`; Vitest for unit tests; zoneless.
- **Contracts:** change a schema → bump `schema_version` → update both producers and consumers → CI validates.
- **Tests before gates:** each gate's definition of done in `docs/ROADMAP.md` is checked by an automated test or
  a documented manual script before the gate is ticked.
- **Observability:** OpenTelemetry in every service; W3C `trace_id` propagated through events.

## Commands

To be filled in as they appear. Keep this list accurate; CI uses the same commands.

```bash
# Local stack (infrastructure + Platform API :5080 + web :4200)
cp .env.example .env            # once; replace placeholders
docker compose up -d --build && docker compose ps
scripts/g1-check.sh             # Gate G1 end to end (API + web, curl); CI also runs scripts/g1-browser-check.cjs
# Browser checks (Playwright installed outside the repo, see the CI job): g1-browser-check.cjs, technician-360-check.cjs

# .NET (Platform API, Ingest Worker, BuildingBlocks) — one solution at the root
dotnet build SmartFactory.slnx -c Release
dotnet test SmartFactory.slnx -c Release                             # DB tests: Docker (Testcontainers) or SF_TEST_POSTGRES
SF_TEST_POSTGRES="Host=localhost;Port=5432;Username=smartfactory;Password=..." dotnet test SmartFactory.slnx -c Release
SF_UPDATE_CONTRACTS=1 dotnet test SmartFactory.slnx -c Release       # regenerate contracts/openapi/*.json, review the diff
dotnet tool restore                                                  # dotnet-ef, pinned in dotnet-tools.json
dotnet ef migrations add <Name> -p src/platform-api/Modules/SmartFactory.Modules.<Module> -o Persistence/Migrations   # then add grants + RLS by hand
dotnet run --project src/platform-api/SmartFactory.Platform.Api      # http://localhost:5080, /scalar for API docs
dotnet run --project src/ingest-worker/SmartFactory.Ingest.Worker

# Python (ML service, simulator) — run inside each project folder
cd src/ml-service && uv sync && uv run ruff check . && uv run ruff format --check . && uv run mypy && uv run pytest
cd src/simulator  && uv sync && uv run ruff check . && uv run ruff format --check . && uv run mypy && uv run pytest
uv run uvicorn ml_service.main:app --port 8000      # from src/ml-service
uv run uvicorn simulator.main:app --port 8001       # from src/simulator

# Web (Angular 22)
cd src/web && npm ci && npm run lint && npm run format:check && npm test && npm run build
npm start                                            # http://localhost:4200

# Containers (build context noted in each Dockerfile)
docker build -f src/platform-api/Dockerfile -t smartfactory/platform-api .
docker build -f src/ingest-worker/Dockerfile -t smartfactory/ingest-worker .
docker build -t smartfactory/ml-service src/ml-service
docker build -f src/simulator/Dockerfile -t smartfactory/simulator .
docker build -t smartfactory/web src/web

# Infra (Week 9)
# terraform -chdir=infra/terraform fmt -check && terraform -chdir=infra/terraform validate

# Blueprint PDF (docs/blueprint/README.md explains revisions)
cd docs/blueprint/src && python3 build_blueprint.py --out ../build/blueprint.html && node render.js ../build/blueprint.html ../smartfactory-ai-blueprint-v2.1.pdf
```

Commit messages are checked by commitlint (`commitlint.config.mjs`); secrets by gitleaks (`.gitleaks.toml`).

## Learning track (owner is learning while building)

The owner uses this project to learn architecture, DevOps/DevSecOps, containers, Kafka, Python, ML and Azure.
Therefore:

- Every week's work ends with a learning note in `docs/learning/` (see the index there): what was built, the
  concept, why this option and not the alternatives, pitfalls met, a two-sentence interview answer, and
  "check yourself" questions. Write the "Concepts you will meet" section *before* the week's coding starts.
- When making a non-obvious technical choice in code, add a short `// Why:` comment or a line in the week's note.
  Prefer the note for anything longer than two lines.
- Keep `docs/learning/README.md` (skills coverage matrix) honest: depth levels are Core / Applied / Documented /
  Not covered. Never upgrade a depth level without code that proves it.
- Explain, do not lecture: plain language, one concept per section, link to the file that demonstrates it.

## Working style for sessions

- Start by reading `docs/ROADMAP.md` and ticking what is done; finish by updating it in the same pull request.
- Prefer the smallest change that moves the current gate; do not pre-build later weeks.
- When a gate slips more than 3 days, apply the cut list in order; do not silently extend scope.
- Ask the owner before: choosing a licence, spending money in Azure, adding a dependency with a non-permissive
  licence, or deviating from an ADR.
