# SmartFactory AI — Software Project Specification Document (SPSD)

> **Synthetic data only.** Everything in this document describes a platform that runs on a fully simulated,
> fictional plant ("Al Safa Industrial"). The system is **advisory only**: it never commands equipment.
> Targets in this document are engineering targets, not measured field results.

## Document control

| Field | Value |
|---|---|
| Document ID | SMARTFACTORY-SPSD-001 |
| Version | 1.0 |
| Issue date | 22 November 2026 |
| Owner | Project owner (solo developer) |
| Product | SmartFactory AI: predictive maintenance and asset management for industrial plants |
| Scope of this document | Project overview, business requirements, functional specification and technical specification for Release 1, with the Release 2 backlog named for context |
| Authoritative sources | Solution blueprint v2.1 (`docs/blueprint/`), architecture decision records ADR-001 to ADR-011 (`docs/adr/`), plant specification (`docs/plant/plant.yaml`), delivery plan (`docs/ROADMAP.md`) |
| Precedence | When this document conflicts with an ADR, the ADR wins and this document is corrected. When it conflicts with `plant.yaml` on plant data, `plant.yaml` wins. |
| Licence | MIT for the repository. The UCI AI4I 2020 dataset is used under CC BY 4.0 with attribution. |

### Conventions

- **MUST / SHOULD / MAY** follow RFC 2119: MUST is mandatory for Release 1, SHOULD is expected unless a documented reason exists, MAY is optional.
- **Requirement identifiers:**

  | Prefix | Requirement type |
  |---|---|
  | `BR-` | Business requirement |
  | `BRU-` | Business rule |
  | `FR-<module>-` | Functional requirement |
  | `NFR-` | Non-functional requirement |
  | `SEC-` | Security requirement |
  | `DR-` | Data requirement |
  | `MLR-` | Machine-learning requirement |

- **R1 / R2** mark Release 1 (the 12-week deliverable) and Release 2 (documented backlog, not scheduled).
- Times are UTC unless a plant time zone is stated. Units follow SI unless stated (bar, rpm, L/min, mm/s RMS).

---

# Part A — Project overview

## 1. Purpose of this document

This document is the single reference for **what** SmartFactory AI must do, **why**, and **how** it is built. It
consolidates the blueprint, the ADRs and the plant specification into numbered, testable requirements. It serves:

- **the developer**, as the build contract;
- **reviewers and hiring panels**, as the evidence map from requirement to code and test;
- **anyone extending the platform**, as the description of its boundaries and contracts.

## 2. Product summary

### 2.1 Problem

Maintenance teams face a costly trade-off:
- **Preventive servicing on a fixed calendar** replaces parts that still have life in them.
- **Reactive servicing** leaves production exposed to unplanned stops, overtime repair and secondary damage.

Condition data exists in most plants but rarely becomes an accountable maintenance decision. Sensor readings, risk
signals, work orders, spare parts and repair outcomes live in separate tools, and nobody can trace a repair back to
the evidence that triggered it.

### 2.2 Vision

**AI Smart Factory PDM** turns machine condition data into accountable, human-approved maintenance decisions. It
combines in one product:
- an asset register;
- live sensor telemetry;
- predictive AI;
- anomaly detection;
- a maintenance workflow with spare parts;
- a tamper-evident audit trail.

Every prediction is traceable to the asset, the evidence window, the model version, the human decision and the
eventual repair outcome.

### 2.3 Value chain

```
 Connect              Detect                    Plan                        Improve
 plant + telemetry →  condition & failure risk → human-led maintenance  →  measured reliability
 (sensors, MQTT)      (predictive AI, anomaly    (requests, approvals,      (findings feed model
                       detection, explanations)   work orders, parts)        evaluation)
```

Financial and uptime benefits can only be established in a real deployment with a measured baseline. This product
makes no such claim.

### 2.4 Product positioning

| Aspect | Position |
|---|---|
| Category | Maintenance decision support (CMMS with predictive analytics) |
| Not | An industrial controller, a SCADA/PLC system, a safety system, a historian replacement or an ERP |
| Data | A fully specified simulated plant; a public benchmark dataset for one model |
| AI approach | Applied AI with MLOps discipline: baselines first, calibrated and explained models, human-gated promotion, drift monitoring |
| Automation | Workflow automation from alert to work order to outcome label, with a human decision at every authorising step |

## 3. Objectives and observable outcomes

| ID | Objective | Product capability | Observable measure |
|---|---|---|---|
| OBJ-1 | Improve visibility | One workspace for plant, lines, machines, live condition and alerts | Asset coverage; sensor freshness per asset |
| OBJ-2 | Enable earlier intervention | Explainable failure scoring and anomaly detection with an alert policy | Recall at fixed alert volume; detection delay in scenario replay |
| OBJ-3 | Improve planning | Work orders, approvals, technician checklists, spare-part reservation | Time to assign; approval-to-start time |
| OBJ-4 | Close the learning loop | Repair findings linked back to asset, alert and model version | Outcome-label completeness; data drift indicators |
| OBJ-5 | Build operational confidence | Idempotent event processing, audit trail, tenant isolation, reproducible deployment | Replay tests; authorisation tests; one-command start-up |

## 4. Scope

### 4.1 In scope (Release 1)

| Module | Release 1 content |
|---|---|
| Asset Registry | Plants, lines, machines, metadata, criticality, sensor mapping |
| Live Condition | Simulator-fed readings, data-quality flags, health status, per-sensor trends, freshness |
| Risk Center | Model scores, anomaly alerts, explanations, thresholds, alert history, review state |
| Maintenance | Requests, work orders, state machine, checklists, assignment, approvals |
| Technician Portal | Assigned work orders, checklist, findings, photo/PDF upload, completion (responsive at 360 px) |
| Spare Parts | Inventory, reservation against work orders, minimum-balance warnings |
| Notifications | In-app notifications; email adapter stubbed |
| Analytics | Health heatmap, open work orders, downtime by asset, MTBF / MTTR from simulated history |
| Data & Model Health | Device freshness, pipeline errors, model comparison, drift |
| Administration | Identity and roles (Keycloak), audit log, source configuration, system health |
| AI Assistant | Optional stretch scope only, if gates G1–G4 close on time (ADR-009) |

### 4.2 Release 2 backlog (named, not scheduled)

- Remaining-useful-life model (NASA C-MAPSS).
- OPC UA edge adapter.
- PWA offline sync and signatures.
- Scheduling optimisation.
- Supplier and lead-time forecasts.
- OEE and cost scenarios.
- Calibrated alert policies per asset family.
- SCIM and ABAC.
- Arabic localisation.
- Conversational history Q&A beyond the bounded assistant.
- A production-supervisor role.

### 4.3 Out of scope

- Any write access to industrial control systems, automated machine stops or safety-interlock bypass.
- Claims of reliability improvement, savings, uptime or field accuracy.
- ERP procurement, MES replacement, heavy historian ingestion.
- Deep-learning models, native mobile apps.

## 5. Stakeholders and personas

| Persona (realm role) | Objectives | Daily workflow in the application |
|---|---|---|
| Reliability manager (`reliability-manager`) | Monitor fleet health, avoid lost production | Risk overview, alert policy proposals, escalation, approval of requests, weekly health report |
| Maintenance planner (`maintenance-planner`) | Prioritise work and coordinate resources | Review alerts, raise and approve requests, schedule technicians, reserve spares, close work |
| Technician (`technician`) | Inspect and repair safely, capture findings | Assigned jobs on a phone, checklist, notes, photos, condition label, completion |
| Data / ML engineer (`ml-engineer`) | Keep evidence quality and model behaviour sound | Pipeline monitoring, evaluation reports, drift, threshold tuning, model promotion |
| Administrator (`administrator`) | Control tenancy, roles, connectors and compliance | Provision access, inspect audit trail, configure plant sources, manage assets and sensors |

Other stakeholders: reviewers and hiring panels (consumers of the evidence package) and security reviewers (consumers of
the threat model and test results).

**Accountability model:**
- **The model** provides evidence and estimated risk. It can never create an authorised maintenance action.
- **The planner** owns prioritisation, resources, approval and scheduling.
- **The technician** owns inspection results and condition confirmation.
- **The administrator** governs access, connectors and the immutable change history.

## 6. Constraints and assumptions

| ID | Constraint / assumption |
|---|---|
| CON-1 | One developer, 12 weeks × 20 hours = 240 hours, including a 12-hour buffer. |
| CON-2 | Cash envelope AED 700–1,600 for the build plus three months of demo hosting; idle cloud cost below AED 60/month. |
| CON-3 | All plant data is synthetic. Model A uses the public UCI AI4I 2020 dataset and says so. |
| CON-4 | LTS runtimes only: .NET 10, Angular 22, Node 22 LTS, Python 3.12 (ADR-010, ADR-011). Adopting an STS release requires a new ADR. |
| CON-5 | Exactly four deployables (ADR-002). Kubernetes is documentation only. |
| CON-6 | No secrets in the repository, including Compose files, Terraform, realm exports, notebooks and fixtures. |
| CON-7 | Pre-agreed cut list if a gate slips by more than three days: AI assistant → inventory forecasts → analytics trends → canary deployment → accessibility polish beyond keyboard navigation. |
| ASM-1 | Reviewers run the stack with Docker on a laptop or use the hosted demo during announced windows. |
| ASM-2 | Simulated faults are physically plausible but not physics-validated. They test pipeline behaviour, not real-world generalisation. |

## 7. Glossary

| Term | Meaning |
|---|---|
| Tenant | A customer organisation. Every business record belongs to exactly one tenant (`tenant_id`). |
| Plant / Line / Asset | Site → production line → instrumented machine (asset code pattern `ABC-123`). |
| SensorMapping | The link between an asset, a metric, its unit, nominal band, sampling interval and MQTT source topic. |
| TelemetryReading | One time-stamped sensor value with quality flag, source and `event_id`. |
| Health index h(t) | Simulator-internal value in [0, 1] (1 = healthy) that drives fault signatures. |
| Alert | A reviewed signal raised from a risk score or anomaly, with dedup key, review state, model version and explanation. |
| MaintenanceRequest | Why maintenance is wanted: reason, priority, requester, optional alert reference, decision state. |
| WorkOrder | The planned and executed job, governed by the work-order state machine. |
| Finding | What the technician observed: condition label, notes, attachments. |
| Disposition | How a work order ended: Repaired, NoFaultFound or Deferred. |
| Part / Reservation | Stock item and a quantity held against a work order. |
| AuditEvent | Append-only, hash-chained record of a state change with before/after hashes and correlation ID. |
| OutboxMessage | Event row written in the same transaction as the business change, later relayed to the bus. |
| ModelVersion | A registered model with metrics, dataset hash, approver and lifecycle stage. |
| PR-AUC | Area under the precision-recall curve; preferred over accuracy for rare failures. |
| Brier score | Calibration measure: does a 70 % risk mean roughly 70 % observed frequency? |
| PSI | Population Stability Index, used to detect feature drift. |
| RLS | PostgreSQL row-level security. |
| OT | Operational technology: PLCs, sensors and plant control systems. |

---

# Part B — Business requirements

## 8. Business context

The target user is a mid-sized discrete manufacturer with mixed rotating and hydraulic equipment and a small
maintenance team. That team cannot review unlimited alarms, so it needs:
- **trustworthy signals**, explained and rate-limited;
- **one place** to turn a signal into a planned job;
- **a phone-friendly way** for technicians to report what they found;
- **a defensible record** of who decided what, and when.

The product demonstrates the full predict-to-maintain loop on a simulated plant. Its integration contracts are
realistic enough that a real edge gateway would change configuration, not architecture.

## 9. Business requirements

| ID | Requirement | Rationale | Acceptance measure | Release |
|---|---|---|---|---|
| BR-01 | The product MUST give each tenant one workspace showing its plants, lines, machines, live condition, alerts and open work. | Visibility is the first step to earlier intervention. | Asset explorer, live condition and operations home show only the caller's tenant data. | R1 |
| BR-02 | The product MUST detect abnormal machine behaviour from sensor data and raise explainable, rate-limited alerts. | Teams need early, trustworthy signals without alarm fatigue. | Replay report shows detection delay per fault type and precision at the alert cap; zero persistent alerts on the healthy-day-with-peak-load scenario. | R1 |
| BR-03 | The product MUST estimate the failure risk of equipment with a calibrated, explained model. | Risk scores support prioritisation. | Model A card reports PR-AUC, recall at fixed precision, Brier score; baseline beaten on the untouched test split. | R1 |
| BR-04 | Every maintenance action MUST be authorised by a human; no prediction may create or approve work by itself. | Safety and accountability (advisory-only rule). | No code path creates an approved work order without a user action; tests prove it. | R1 |
| BR-05 | The product MUST support the full work-order lifecycle: request, approval, scheduling, execution, completion, closure, with rejection, hold and cancellation. | This is a usable CMMS before any ML exists. | Gate G2: a planner and a technician complete a work order end to end with every step audited. | R1 |
| BR-06 | Technicians MUST be able to execute assigned jobs, record findings and attach evidence from a phone. | Field data capture is where the outcome labels come from. | The technician flow works at 360 px width with 44 px touch targets; checked by an automated browser test. | R1 |
| BR-07 | The product MUST reserve spare parts against approved work and warn when stock falls below the minimum balance. | Planning includes materials. | Reservations never exceed available stock without a documented override; warning raised at minimum balance. | R1 |
| BR-08 | Repair outcomes MUST flow back as labels linked to the alert and model version. | Closed-loop learning and honest evaluation. | Closed work orders publish `MaintenanceClosed`; the label is linked to the originating alert. | R1 |
| BR-09 | Every critical state change MUST be recorded in a tamper-evident audit trail. | Compliance and trust. | 100 % audit coverage of critical state changes; hash-chain verification detects tampering. | R1 |
| BR-10 | Tenants MUST be strictly isolated from each other. | Multi-tenant SaaS trust. | Cross-tenant tests on every endpoint return 404 for foreign resources. | R1 |
| BR-11 | Every screen, chart, dataset, report and model card MUST carry a visible "synthetic data" label. | Honesty about evidence. | Banner in the application shell; `X-SmartFactory-Data` header on every API response; label on each page. | R1 |
| BR-12 | Models MUST be promoted to production only by a human after beating the baseline. | Governance of AI. | Promotion requires the ML-engineer role, is audited and publishes `ModelPromoted`. | R1 |
| BR-13 | The platform MUST run locally with one command and in Azure at low idle cost. | Reproducibility and budget. | `docker compose up` gives a working stack; idle Azure cost below AED 60/month with budget alerts. | R1 |
| BR-14 | The product MUST be operable and observable: health, freshness, pipeline errors and one trace per end-to-end flow. | Operational confidence. | One OpenTelemetry trace shows sensor event → alert → work order. | R1 |
| BR-15 | The product MAY offer a bounded AI assistant that explains alerts, drafts work orders and answers history questions, read-only and cited. | Demonstrates guarded generative AI. | Golden-set evaluation in CI; a response citing a non-existent record fails the build. | R1 (stretch) |
| BR-16 | The product MUST NOT claim savings, uptime, ROI or field accuracy. | Integrity of the portfolio. | Content review; the ROI screen is a user-input scenario labelled as hypothetical. | R1 |

## 10. Business rules

| ID | Rule |
|---|---|
| BRU-01 | The system never commands equipment. The only reverse message is "maintenance completed" sent to the **simulator**, which resets the simulated health index. |
| BRU-02 | A work order belongs to exactly one asset and one tenant. |
| BRU-03 | Only planners, reliability managers or administrators approve or reject requests. Only planners or administrators schedule, close or cancel work. |
| BRU-04 | Only the technician assigned to a work order may start it, record its checklist and findings, and complete it. |
| BRU-05 | Rejecting, holding or cancelling a work order requires a reason. |
| BRU-06 | Completing a work order requires every checklist item to have an outcome (Done, Not applicable or Failed). |
| BRU-07 | Closing a work order requires a disposition (Repaired, NoFaultFound or Deferred) and a resolved checklist. |
| BRU-08 | The plan (title, description, priority, due date, checklist) may be edited only before work starts (Requested, Approved, Scheduled). |
| BRU-09 | Cancelled is a state, not a deletion. Work orders, requests, history, findings and attachments are never deleted by the application. |
| BRU-10 | A part reservation cannot exceed available quantity without a documented override by a planner or administrator. |
| BRU-11 | An alert can be linked to at most one open request. Duplicate alerts within the dedup window update the existing alert. |
| BRU-12 | Disposition "Repaired" resets the simulated health index to 0.95–1.0; "NoFaultFound" leaves it unchanged, producing honest false-positive cases. |
| BRU-13 | Scoring is suppressed on stale data; sensor faults are data-quality issues and never enter the risk queue as failures. |
| BRU-14 | A model is eligible for promotion only if it beats the committed baseline on the untouched test split. A human promotes it. |
| BRU-15 | A foreign record (another tenant's, or another technician's job for a technician) is reported as "not found", never "forbidden", so its existence is not confirmed. |

## 11. Role-to-permission matrix (tenant-scoped)

| Capability | Manager | Planner | Technician | ML engineer | Admin |
|---|---|---|---|---|---|
| View plant, assets, telemetry, alerts | ✓ | ✓ | assigned assets | ✓ | ✓ |
| Raise a maintenance request | ✓ | ✓ | ✓ | — | ✓ |
| Approve / reject a request | ✓ | ✓ | — | — | ✓ |
| Schedule, edit plan, close, cancel work orders | — | ✓ | — | — | ✓ |
| Start, record checklist and findings, complete | — | — | assigned only | — | — |
| Hold / resume work | — | ✓ | assigned only | — | ✓ |
| Reserve parts | — | ✓ | — | — | ✓ |
| Issue parts | — | ✓ | ✓ (issue only) | — | ✓ |
| Change alert thresholds | propose | — | — | ✓ | ✓ |
| Promote / roll back a model | — | — | — | ✓ (with approval) | ✓ |
| Read audit trail | ✓ | — | — | — | ✓ |
| Verify audit chain | — | — | — | — | ✓ |
| Manage assets and sensor mappings | — | — | — | — | ✓ |
| Manage users, roles, connectors | — | — | — | — | ✓ |

The matrix is implemented as policy-based authorisation on routes plus resource-based rules in the domain, and is
verified by automated tests.

## 12. Content and compliance rules

1. **Advisory only.** No PLC write path exists or may be added. Humans approve work orders and model promotions.
2. **Synthetic data, labelled.** A persistent banner in the application shell reads "Synthetic data … advisory only", so no page renders without it. The API repeats the label in the `X-SmartFactory-Data: synthetic; advisory-only; no-plc-control` response header.
3. **No performance claims.** Metrics are reported on synthetic or public data only, with that caveat attached.
4. **No secrets in the repository.** Use `.env.example` placeholders locally and Key Vault with managed identity in Azure. The Keycloak realm export contains no users and no credentials.
5. **Alignment, not certification.** The design aligns conceptually with NIST SP 800-82 Rev. 3 (OT security), ISO 55000:2024 (asset management) and ISA-95 (enterprise/control integration). It is not certified against any of them.

## 13. Success metrics

| Area | Metric | Target |
|---|---|---|
| Detection | Detection delay (fault injection → alert), per fault type | Reported in simulated time; cavitation ≤ 15 simulated minutes |
| Alert quality | False persistent alerts on healthy-day with peak load | 0 |
| Alert quality | Precision at alert cap; false alerts per asset-day | Reported in the replay report |
| Risk model | PR-AUC, recall at fixed precision, Brier score vs baseline | Beats baseline on untouched test split |
| Workflow | Approval-to-start time, time to assign | Measured on demo data |
| Learning loop | Share of closed work orders with a condition label linked to an alert | 100 % of alert-originated work orders |
| Trust | Cross-tenant test suite | 100 % of tenant-scoped endpoints covered and green |
| Audit | Critical state changes audited | 100 % |
| Cost | Idle Azure monthly cost | < AED 60 |

---

# Part C — Functional specification

## 14. System context

```
             ┌──────────────── Users (browser / phone) ────────────────┐
             │ manager · planner · technician · ML engineer · admin    │
             └───────────────────────────┬─────────────────────────────┘
                                         │ HTTPS, OIDC (PKCE)
┌───────────────┐  MQTT QoS 1   ┌────────▼────────────────────────────────────────┐   ┌──────────────┐
│ Plant         │──────────────▶│ SmartFactory AI platform                         │◀──│ Keycloak      │
│ simulator     │  plant/#      │ web app · Platform API · ingest worker · ML      │   │ (identity)    │
│ (fictional    │◀──────────────│ service · PostgreSQL/Timescale · Redis · bus     │   └──────────────┘
│ plant)        │ "maintenance  └────────┬──────────────────────────────┬──────────┘
└───────────────┘  completed" only       │                              │
                                  ┌──────▼──────┐               ┌───────▼────────┐
                                  │ MLflow      │               │ Blob storage    │
                                  │ registry    │               │ (datasets,      │
                                  └─────────────┘               │ attachments)    │
                                                                └────────────────┘
```

External systems are Keycloak (identity), MLflow (model registry), Blob storage, the optional hosted LLM API for the
assistant, and Azure Monitor. The simulator stands in for the plant. A real deployment would replace it with an edge
gateway (for example an OPC UA client bridged to MQTT) in the OT DMZ.

## 15. Functional modules

### 15.1 Identity, access and tenancy

| ID | Requirement |
|---|---|
| FR-IAM-01 | Users MUST sign in through Keycloak with OIDC Authorization Code + PKCE. The SPA is a public client with no secret. |
| FR-IAM-02 | Access tokens MUST carry realm roles (`realm_access.roles`), the `tenant_id` claim and the Platform API audience. ID tokens MUST carry roles so the UI can adapt navigation. |
| FR-IAM-03 | Every API endpoint except health probes MUST require authentication. Tenant-scoped endpoints MUST require a `tenant_id` claim (403 without it). |
| FR-IAM-04 | `GET /api/me` MUST return the caller's subject, username, tenant and roles as the API sees them. |
| FR-IAM-05 | The UI MUST show only navigation items the caller's roles can use. The API enforces access regardless. |
| FR-IAM-06 | Five persona users MUST be seeded for local development from a git-ignored `.env` (never from the realm export). |
| FR-IAM-07 | Tokens are short-lived (15-minute access token for the SPA) and refreshed through the token endpoint. |

**Acceptance:**
- An anonymous call to a tenant-scoped endpoint returns 401.
- A token without a tenant claim returns 403.
- A user of tenant A requesting a tenant B id receives 404.

### 15.2 Asset registry

| ID | Requirement |
|---|---|
| FR-AST-01 | The registry MUST model Tenant → Plant → Line → Asset → SensorMapping. |
| FR-AST-02 | Asset codes MUST match `^[A-Z]{3}-[0-9]{3}$` and be unique per tenant. Asset classes: MOTOR, CONVEYOR, COMPRESSOR, FAN, CNC, PUMP, PRESS, CHILLER. Criticality: Low, Medium, High. Status: InService, OutOfService, Decommissioned. |
| FR-AST-03 | Sensor mappings MUST hold metric (`^[a-z][a-z0-9_]{1,40}$`), unit, interval, optional nominal band, kind, and the derived source topic `plant/{tenant}/{plant}/{line}/{asset}/{metric}`. A metric is unique per asset. |
| FR-AST-04 | The plant MUST be seeded idempotently from `docs/plant/plant.yaml`. The seeder refuses a specification without `synthetic: true` and audits what it creates as actor `system:seed`. |
| FR-AST-05 | Users MUST list plants with lines and asset counts, list assets filtered by line, criticality and free text, and view an asset with its sensors. |
| FR-AST-06 | Administrators MUST create and update assets and add or remove sensor mappings. Updates carry the version read and fail with 409 on a concurrent change. |
| FR-AST-07 | The registry MUST answer other modules' existence checks through the `IAssetDirectory` contract, scoped to the caller's tenant. |

**Seeded plant (synthetic):**

| Line | Asset | Class | Criticality | Sensors (interval) |
|---|---|---|---|---|
| Line A — Packaging | MTR-042 Filler drive motor (15 kW) | MOTOR | High | vibration_rms, bearing_temp, current, speed (5 s) |
| | CVR-012 Belt conveyor | CONVEYOR | Medium | belt_speed, motor_current, gearbox_temp (5 s) |
| | CMP-007 Screw compressor | COMPRESSOR | High | discharge_pressure, oil_temp, vibration_rms, run_hours (5 s) |
| | FAN-034 Extraction fan | FAN | Low | vibration_rms, airflow, current (10 s) |
| Line B — Machining | CNC-018 3-axis CNC mill | CNC | High | spindle_load, spindle_temp, vibration_rms, tool_wear, feed_rate (2 s) |
| | PMP-211 Coolant pump | PUMP | Medium | flow, suction_pressure, discharge_pressure, vibration_rms, temp (5 s) |
| | PRS-105 Hydraulic press | PRESS | High | hydraulic_pressure, oil_temp, cycle_time, cycles (per cycle, ~8 s) |
| | CHL-003 Process chiller | CHILLER | Medium | supply_temp, return_temp, compressor_current, refrigerant_pressure (10 s) |

The design load is 8 assets and 32 metrics, about 7 readings/s and ~560,000 readings/day at full rate. A throttled
cloud profile runs at ~90,000/day.

### 15.3 Telemetry ingestion and live condition

| ID | Requirement |
|---|---|
| FR-TEL-01 | The ingest worker MUST subscribe (never publish) to `plant/#` over MQTT QoS 1. |
| FR-TEL-02 | Each reading MUST be validated: payload size, schema version, device identity, allow-listed metric, finite value, unit conversion to the calibrated unit, and an asset mapping active at event time. |
| FR-TEL-03 | Clock skew beyond ±120 s MUST be flagged `questionable`. Out-of-order events inside the retention window are accepted; older events are rejected and counted. |
| FR-TEL-04 | Duplicates MUST be discarded by `event_id` (Redis set, 10-minute TTL) and by (asset, metric, sequence). They are counted, never stored. |
| FR-TEL-05 | Accepted readings MUST be stored in `telemetry.readings`, update the Redis last-value hash per asset, be enriched with line, plant, asset class and nominal band, and be published as `TelemetryReceived`. |
| FR-TEL-06 | Each reading MUST carry a quality flag (`good`, `questionable`, `missing`) and a source (`simulator`, `replay`, `edge`). |
| FR-TEL-07 | The UI MUST show live per-sensor trends with the alarm band, the reading timestamp, and whether data is live, replayed or generated. |
| FR-TEL-08 | An asset MUST show "stale" after three missed intervals. Scoring is suppressed on stale data. |
| FR-TEL-09 | Sensor faults (dropout, stuck value, spike, out of range, clock skew) MUST raise data-quality flags shown in a data-health view, not risk alerts. |

**Acceptance (Gate G3):**
- An injected fault is visible on the live chart within 15 s.
- Duplicate messages are not stored.

### 15.4 Plant simulator

| ID | Requirement |
|---|---|
| FR-SIM-01 | The simulator MUST read `plant.yaml` (spec version, seed, shifts, assets, sensors, faults) and refuse an unsynthetic or invalid specification. |
| FR-SIM-02 | Signals MUST be generated as nominal baseline + load effect + degradation × (1 − h) + noise. Load follows two shifts (A 06:00–14:00 at 1.0, B 14:00–22:00 at 0.9) and idle at 0.15. |
| FR-SIM-03 | The fault library MUST provide BEARING, THERMAL, CAVITATION, MECHANICAL, TOOL_WEAR, CLOGGING and HYDRAULIC, with linear, exponential or Weibull hazards. SENSOR_FAULT is a quality flag, not a failure. |
| FR-SIM-04 | Readings MUST be published on `plant/{tenant}/{plant}/{line}/{asset}/{metric}` with `event_id`, `schema_version`, `event_time_utc`, value, unit, quality, `source="simulator"` and a monotonic sequence. Status goes on `…/{asset}/status`. |
| FR-SIM-05 | A clearly labelled simulation control API MUST support injecting and clearing faults, setting speed (time compression 1×–120×), pausing, reseeding, and named scenarios. |
| FR-SIM-06 | Named scenarios MUST include `healthy-day`, `bearing-wear-mtr042`, `cavitation-pmp211`, `tool-wear-cnc018`, `sensor-dropout` and `demo-5-minutes`. |
| FR-SIM-07 | Every injected fault MUST be written to an offline ground-truth table (asset, fault, onset, detectable-from, failure time) that is never visible to online scoring. |
| FR-SIM-08 | A replay mode MUST replay parquet history at a chosen speed with the original event times. |
| FR-SIM-09 | On a `MaintenanceClosed` event, the simulator MUST reset the asset's health index according to the disposition (BRU-12). |
| FR-SIM-10 | For robustness tests, the simulator MUST inject bounded clock skew (±2 s) and occasional out-of-order delivery. |

### 15.5 Risk Center: scoring, anomaly detection and alerts

| ID | Requirement |
|---|---|
| FR-RSK-01 | The ML service MUST consume `TelemetryReceived` windows and publish `FailureRiskScored` (Model A) and `AnomalyDetected` (Model B). Each publication carries the model version, the window and the top contributing features. |
| FR-RSK-02 | The Platform API MUST raise an alert from a risk score above the alert-policy threshold or from a persistent anomaly. The alert carries type, severity, score, model version, explanation JSON, evidence window and dedup key. |
| FR-RSK-03 | Alerts MUST be deduplicated: a repeat within the dedup window updates the existing alert (`AlertUpdated`) instead of creating one. |
| FR-RSK-04 | Alerts MUST have a review state: New → Under review → Confirmed issue / Benign event / Unknown. Review labels feed evaluation and threshold tuning. |
| FR-RSK-05 | From an alert, a planner MUST be able to raise a maintenance request pre-filled with the asset, reason and alert reference (BRU-11 applies). |
| FR-RSK-06 | Risk cards MUST show age, model version, severity reason, data quality and review state. Explanations use readable operating factors and name model limitations. |
| FR-RSK-07 | If the ML service is unavailable, threshold baselines MUST keep raising anomaly alerts flagged "baseline mode", and risk scores show "unavailable", never stale values. |
| FR-RSK-08 | ML engineers and administrators MUST change alert thresholds. Managers may propose a change. Every change is audited. |

### 15.6 Maintenance: requests and work orders

**Lifecycle.** A maintenance request creates a work order in state *Requested*. The request keeps the reason, priority,
requester, optional alert reference and decision state (Pending, Approved, Rejected). The work order owns the lifecycle.

```
Requested ──approve──▶ Approved ──schedule──▶ Scheduled ──start──▶ In progress ──complete──▶ Completed ──close──▶ Closed
    │                                          │   ▲  │                │
    └──reject──▶ Rejected                       │   └──┘ re-schedule    │
                                                ├──hold──▶ On hold ◀──hold┘
                                                │           │ resume (to the state it was held from)
  Cancel (reason required) from any open state: Requested, Approved, Scheduled, In progress, On hold ──▶ Cancelled
```

**Transition table (authoritative; anything not listed is refused):**

| From | Action | To | Allowed actor | Preconditions |
|---|---|---|---|---|
| Requested | Approve | Approved | planner, reliability manager, admin | — |
| Requested | Reject | Rejected | planner, reliability manager, admin | reason |
| Approved | Schedule | Scheduled | planner, admin | assignee, scheduled date |
| Scheduled | Schedule | Scheduled | planner, admin | assignee, scheduled date (re-schedule) |
| Scheduled | Start | In progress | assigned technician | — |
| Scheduled / In progress | Hold | On hold | planner, admin, assigned technician | reason |
| On hold | Resume | held-from state | planner, admin, assigned technician | — |
| In progress | Complete | Completed | assigned technician | every checklist item resolved |
| Completed | Close | Closed | planner, admin | disposition and resolved checklist |
| any open state | Cancel | Cancelled | planner, admin | reason |

Closed, Rejected and Cancelled are terminal.

| ID | Requirement |
|---|---|
| FR-MNT-01 | Users with a maintenance role MUST raise a request for an asset of their tenant with title (3–200 characters), reason (3–2,000), priority (Low, Medium, High, Urgent), optional checklist (≤ 30 items, 1–300 characters each) and optional due date. An unknown or foreign asset answers 404. |
| FR-MNT-02 | Every transition MUST be checked in this order: (1) legal for the current state and allowed for this caller; (2) invariants; (3) apply. An illegal move reports `illegal-transition` (409) even when details are also missing. |
| FR-MNT-03 | Every change MUST raise the work order's revision by 1, append a history entry (revision, action, from, to, actor, note, time) and write an audit record in the same database transaction. |
| FR-MNT-04 | Every write MUST carry the version the client read. A stale version answers 409 `stale-version`. |
| FR-MNT-05 | The work order MUST return `availableActions` for the caller, so the UI shows only possible actions. |
| FR-MNT-06 | Planners MUST edit the plan while it is editable (BRU-08). Unchanged checklist lines keep their status; removed lines are deleted; positions are renumbered. |
| FR-MNT-07 | Technicians who are not also planners or approvers MUST see only work orders assigned to them. Other ids answer 404. |
| FR-MNT-08 | Lists MUST be filterable by status and asset, ordered by last update, and capped at 500 rows. |
| FR-MNT-09 | Approving a work order MUST publish `WorkOrderApproved` (work order, parts, due date, approver) through the outbox. Closing MUST publish `MaintenanceClosed` (work order, disposition, condition label, finding summary). |
| FR-MNT-10 | Rule violations MUST map to HTTP: validation 400, not allowed / not assigned 403, not found 404, other rule conflicts 409, with the stable code in the problem `title`. |
| FR-MNT-11 | The work order MUST keep a snapshot of the asset code at creation, so lists need no call to the Assets module. |

### 15.7 Technician portal

| ID | Requirement |
|---|---|
| FR-TEC-01 | "My work" MUST list the caller's assigned jobs as cards: in progress first, then scheduled by date, then on hold. Recently finished jobs follow. |
| FR-TEC-02 | The job page MUST show the asset, title, priority, revision, reason for hold, schedule and due date, and the actions the server offers (Start, Hold with reason, Resume, Complete). |
| FR-TEC-03 | Checklist items MUST be recorded with three large buttons (Done, N/A, Failed) of at least 44 px height. Complete is disabled until every item has an outcome. |
| FR-TEC-04 | Technicians MUST record findings (condition Good, Worn, Degraded, Failed; notes 3–4,000 characters) and attach photos or PDFs. On a phone, the file picker opens the camera. |
| FR-TEC-05 | If someone else changed the job, the page MUST reload it and explain why the action failed. |
| FR-TEC-06 | The pages MUST work at 360 px width with no horizontal scrolling. Below 720 px the navigation becomes an overlay. |
| FR-TEC-07 | Attachments MUST be downloaded with the bearer token (fetched as a blob), never through unauthenticated links. |

### 15.8 Findings and attachments

| ID | Requirement |
|---|---|
| FR-FND-01 | Only the assigned technician MUST add findings or attachments, and only while the work order is in progress. |
| FR-FND-02 | Uploads MUST be limited to 5 MB per file and 5 files per finding. Size is checked before the content is read. |
| FR-FND-03 | File type MUST be detected from magic bytes against an allow-list: JPEG (`FF D8 FF`), PNG (`89 50 4E 47 0D 0A 1A 0A`), PDF (`%PDF-`). The declared Content-Type and the file name are ignored. Others answer 415. |
| FR-FND-04 | Files MUST be stored under generated keys (`{tenant}/{yyyy}/{MM}/{guid}.{ext}`) outside the web root. The display name is stripped of directories and control characters. The SHA-256 digest is recorded. |
| FR-FND-05 | Downloads MUST be served with the detected content type, `Content-Disposition: attachment` and `X-Content-Type-Options: nosniff`. |
| FR-FND-06 | Bytes MUST be written before the database transaction and deleted if it fails, so no record points at a missing file. |
| FR-FND-07 | Findings and attachments are evidence. The application role MUST be able to insert and read them only. |

### 15.9 Inventory and reservations

| ID | Requirement |
|---|---|
| FR-INV-01 | The catalogue MUST hold parts with SKU, name, location, on-hand quantity, reserved quantity and minimum balance. |
| FR-INV-02 | Planners and administrators MUST reserve parts against an approved work order. Available = on hand − reserved. |
| FR-INV-03 | A reservation exceeding the available quantity MUST be refused unless a planner or administrator records an override with a reason. The override is audited. |
| FR-INV-04 | Approving a work order with required parts MUST reserve them in the same transaction, or via the `WorkOrderApproved` consumer with an idempotent reservation. |
| FR-INV-05 | Parts MUST be issued by technicians (issue only), planners or administrators. Issuing converts a reservation into consumption. |
| FR-INV-06 | A minimum-balance warning MUST be raised as a notification when available stock falls to or below the minimum balance. |
| FR-INV-07 | Cancelling or closing a work order MUST release unissued reservations. |
| FR-INV-08 | Event replay MUST never duplicate a reservation (idempotent consumer on `event_id`). |

### 15.10 Notifications

| ID | Requirement |
|---|---|
| FR-NTF-01 | In-app notifications MUST be raised for: work order assigned to me, approval needed, alert raised on a high-criticality asset, part below minimum balance, model promoted. |
| FR-NTF-02 | Notifications MUST be per user, tenant-scoped and marked read or unread. They are produced by idempotent consumers of domain events. |
| FR-NTF-03 | An email adapter MUST exist as an interface with a logging stub. No email is sent in Release 1. |

### 15.11 Audit trail

| ID | Requirement |
|---|---|
| FR-AUD-01 | Every critical state change MUST write an audit event: tenant, sequence, UTC time (microsecond precision), actor id and name, aggregate type and id, action, before/after state JSON, before/after SHA-256 hashes, correlation id (trace id), chain hash. |
| FR-AUD-02 | The chain hash MUST include the previous event's chain hash for the same tenant. Writers for one tenant are serialised with a transaction-scoped advisory lock. |
| FR-AUD-03 | Audit records MUST be written in the same transaction as the change, through the `IAuditTrail` contract. |
| FR-AUD-04 | A database trigger MUST reject UPDATE, DELETE and TRUNCATE on audit events, even for the table owner. The application role has only SELECT and INSERT. |
| FR-AUD-05 | Reliability managers and administrators MUST query events by aggregate type and id (1–500 per page). |
| FR-AUD-06 | Administrators MUST verify the chain. Verification returns intact or not, events checked, and the first broken sequence. |

### 15.12 Operations home and analytics

| ID | Requirement |
|---|---|
| FR-ANL-01 | Operations home MUST show a plant switcher, assets monitored, at-risk machines, open work orders, alert trend, outstanding jobs and data freshness. Users can drill to the worst asset in two clicks. |
| FR-ANL-02 | Analytics MUST show a health heatmap, open work orders by status, downtime by asset, and MTBF / MTTR computed from simulated history, labelled as simulated. |
| FR-ANL-03 | An ROI scenario screen MAY let a user enter their own downtime cost, baseline downtime and assumed avoidance. It is labelled as a hypothetical scenario, never an achieved result. |

### 15.13 Data and model health (MLOps)

| ID | Requirement |
|---|---|
| FR-MLO-01 | The view MUST show device freshness, pipeline errors, dead-letter counts, model versions in production and staging, champion-vs-challenger metrics, and drift (PSI) per feature. |
| FR-MLO-02 | ML engineers MUST promote a registered candidate from Staging to Production, only when the quality gate passed. The action is audited and publishes `ModelPromoted`. |
| FR-MLO-03 | Rollback MUST switch the registry alias back to the last approved model in one action, also audited. |
| FR-MLO-04 | Administrators MUST list dead-letter messages (reason, payload hash) and replay them after authorisation. |

### 15.14 Administration and settings

| ID | Requirement |
|---|---|
| FR-ADM-01 | Administrators MUST manage assets and sensor mappings (15.2), plant source configuration and alert thresholds (with ML engineers). |
| FR-ADM-02 | User and role management MUST happen in Keycloak (the admin console). The platform exposes no user-management API in Release 1. |
| FR-ADM-03 | System health MUST show liveness and readiness of each deployable and its dependencies. |
| FR-ADM-04 | The settings and compliance view MUST show audit events and their chain verification. |

### 15.15 AI assistant (optional, bounded; ADR-009)

| ID | Requirement |
|---|---|
| FR-AIA-01 | The assistant MAY be built only from the buffer and only if gates G1–G4 close on schedule. |
| FR-AIA-02 | It MUST offer exactly three capabilities: alert explanation, work-order draft, and history Q&A over the tenant's own maintenance records. |
| FR-AIA-03 | It MUST be read-only. No tool changes a work order, threshold or model. Drafts are saved only by a planner. |
| FR-AIA-04 | Every statement MUST cite a platform record. A citation check rejects responses that cite non-existent records. |
| FR-AIA-05 | Retrieval MUST be tenant-scoped and role-checked before the prompt is built. Questions outside maintenance records are refused. |
| FR-AIA-06 | Usage MUST be token-budgeted per tenant per day, with caching of identical requests and an estimated AED 10–30 for the whole demo period. |
| FR-AIA-07 | Output MUST be labelled AI-generated and advisory. The audit log records the prompt hash, model name and version. |
| FR-AIA-08 | A 20-case golden set MUST run in CI. |

## 16. Key user journeys

### 16.1 Predict to maintain: bearing wear on MTR-042

1. The simulator runs `bearing-wear-mtr042`. `vibration_rms` climbs from about 2.1 to 4.6 mm/s over simulated hours, while current and speed stay nominal.
2. The ingest worker validates each reading and discards a duplicated QoS-1 message by `event_id`. Readings land in TimescaleDB and the Redis last-value hash; `TelemetryReceived` is published.
3. Model B flags a persistent deviation (three consecutive windows above threshold). Model A scores elevated risk. The API raises one deduplicated alert with model versions and the feature explanation.
4. The planner reviews the trend, sees vibration above the alarm band, and raises a request from the alert. A reliability manager approves it, which reserves a bearing kit. The planner schedules the technician.
5. The technician starts the job on a phone, works through the checklist, records "bearing play confirmed, replaced" with a photo, and completes it.
6. The planner closes the job with disposition Repaired. `MaintenanceClosed` resets the simulated health index and links the confirmed label to the alert, which counts as a true positive with a measured detection delay.
7. One OpenTelemetry trace shows the path from sensor event to closed work order. The audit trail holds every step.

### 16.2 Technician job (phone)

My work → job card → Start → record each checklist item → add finding (condition, notes) → add photo → Complete.
If the planner edits or holds the job at the same moment, the technician's action answers 409. The page reloads the
job and explains what changed.

### 16.3 Model promotion

The ML engineer trains a candidate. It is logged to MLflow with parameters, seed, git SHA and dataset hash. CI compares
it with the champion on the untouched test split and the model card is updated. The ML engineer promotes it in the
Data & Model Health view. The promotion is audited, `ModelPromoted` is published, and scoring switches to the new
alias. Rollback restores the previous alias in one action.

### 16.4 Tenant isolation

A user of tenant B with every role requests a work order, finding, attachment or asset id from tenant A. Every read
and write answers 404, lists never include tenant A records, and nothing changes on tenant A's side.

## 17. User-interface specification

| Workspace | Essential screens | Critical interaction |
|---|---|---|
| Operations Home | Plant switcher, KPIs, alert trend, outstanding jobs, data freshness | Drill to the worst-performing asset |
| Assets | Hierarchy explorer, asset profile, sensor mapping, maintenance history, sensor trends | Filter by line, criticality, text; maintain metadata (admin) |
| Risk Center | Risk queue, anomaly alerts, explanation, model version and validity, review state | Review alert, attach finding, create request |
| Maintenance Hub | Work-order board, calendar, approval queue, job details, parts | Approve, schedule a technician, close with disposition |
| Technician | My jobs, checklist, findings, photos, completion (phone width) | Complete a job with condition confirmation |
| Inventory | On-hand stock, reservations, catalogue, threshold warnings | Reserve a part against a work order |
| Data & Model Health | Device freshness, pipeline errors, model comparison, drift, dead letters | Investigate drift, promote or roll back a model |
| Settings & Compliance | Roles (via Keycloak), connectors, thresholds, audit events | Trace a user action and change history |

**Design rules:**
- **Layout:** desktop-first, with technician pages responsive at 360 px.
- **Framework conventions:** Angular Material components; standalone components, signals, OnPush, zoneless change detection, typed reactive forms; no `ngModel`, no NgModules; selector prefix `sf`.
- **Language:** English first, with every string in Transloco (`public/i18n/en.json`).
- **Labelling:** a synthetic-data banner in the shell plus a synthetic tag on every page. Every reading shows its timestamp and whether it is live, replayed or generated.
- **Critical actions:** require permission and confirmation. No prediction triggers work without review.
- **Accessibility:** keyboard navigation, labelled controls, readable chart alternatives, touch targets ≥ 44 px on technician pages, axe checks in end-to-end tests.

## 18. API specification

**Conventions:**
- **Format:** REST over HTTPS with JSON (camelCase). OpenAPI 3.1 is generated by the running app and frozen in `contracts/openapi/platform-api.v1.json`; CI fails on any unreviewed change. API docs are served at `/scalar`.
- **Tenancy:** never a parameter. It comes from the token.
- **Errors:** RFC 7807 problem details. Validation errors carry a field map. Domain rule codes are in `title` (`illegal-transition`, `stale-version`, `not-assigned`, `checklist-incomplete`, `disposition-required`, `not-editable`, `file-type`, `file-size`, `too-many-attachments`).
- **Concurrency:** mutable resources expose `version`, and writes send it back (409 on mismatch).
- **Headers:** every response carries `X-SmartFactory-Data`. CORS allows only the configured web origins.

| Method and route | Purpose | Policy |
|---|---|---|
| `GET /health/live`, `GET /health/ready` | Liveness; readiness (PostgreSQL, Redis) | anonymous |
| `GET /api/me` | Caller's subject, username, tenant, roles | authenticated |
| `GET /api/assets/plants` | Plants with lines and asset counts | tenant member |
| `GET /api/assets/assets?lineId&criticality&search` | Asset list | tenant member |
| `GET /api/assets/assets/{id}` | Asset with sensors and topics | tenant member |
| `POST /api/assets/assets` | Register asset | administrator |
| `PUT /api/assets/assets/{id}` | Update asset (with version) | administrator |
| `POST /api/assets/assets/{id}/sensors` | Map sensor | administrator |
| `DELETE /api/assets/assets/{id}/sensors/{sensorId}` | Remove sensor mapping | administrator |
| `POST /api/maintenance/requests` | Raise request → work order in Requested | maintenance participant |
| `GET /api/maintenance/work-orders?status&assetId` | Work orders (technicians: own only) | maintenance participant |
| `GET /api/maintenance/work-orders/{id}` | Detail with checklist, history, findings, available actions | maintenance participant |
| `PUT /api/maintenance/work-orders/{id}` | Edit plan | planner |
| `POST /api/maintenance/work-orders/{id}/approve` · `/reject` | Decide request | approver |
| `POST …/{id}/schedule` · `/close` · `/cancel` | Plan, close, cancel | planner |
| `POST …/{id}/start` · `/complete` | Execute | technician (assigned, checked in domain) |
| `POST …/{id}/hold` · `/resume` | Hold or resume | participant (checked in domain) |
| `PUT …/{id}/checklist/{itemId}` | Record checklist outcome | technician (assigned) |
| `POST …/{id}/findings` | Record finding | technician (assigned) |
| `POST …/{id}/findings/{findingId}/attachments` | Upload JPEG/PNG/PDF (multipart) | technician (assigned) |
| `GET /api/maintenance/attachments/{id}` | Download attachment | participant (visibility rules) |
| `GET /api/audit/events?aggregateType&aggregateId&take` | Audit events | reliability manager, admin |
| `GET /api/audit/verify` | Verify hash chain | administrator |
| `GET /api/maintenance/alerts` · `GET …/alerts/{id}` · `POST …/alerts/{id}/review` · `POST …/alerts/{id}/request` | Risk Center | per RBAC matrix |
| `GET /api/inventory/parts` · `POST …/reservations` · `POST …/reservations/{id}/issue` · `DELETE …/reservations/{id}` | Inventory | per RBAC matrix |
| `GET /api/telemetry/assets/{id}/readings?metric&from&to&bucket` · `GET …/freshness` | Live condition and trends (1 min / 1 h aggregates) | tenant member |
| `GET /api/notifications` · `POST /api/notifications/{id}/read` | Notifications | authenticated |
| `POST /api/models/{name}/promote` · `/rollback` | Model governance | ML engineer, admin |

**ML service (internal):**
- `POST /score`: schema-validated features in; probability, calibrated band, top features and `model_version` out; p95 < 300 ms on CPU.
- `/health/live` and `/health/ready`.

**Simulator control API (labelled as a simulation control plane):**

| Endpoint | Purpose |
|---|---|
| `POST /faults` | Inject a fault |
| `DELETE /faults/{id}` | Clear a fault |
| `POST /speed` | Set time compression |
| `POST /pause` | Pause the simulation |
| `POST /seed` | Reseed |
| `POST /scenarios/{name}` | Run a named scenario |
| `GET /health/*` | Health probes |

## 19. Event catalogue

Every event carries a common envelope:

| Field | Meaning |
|---|---|
| `event_id` | UUID v7 |
| `schema_version` | Semantic version |
| `type` | Event type name |
| `tenant_id` | Owning tenant |
| `asset_id` | Asset the event concerns |
| `event_time_utc` | When it happened |
| `trace_id` | W3C trace context |

JSON Schemas live under `contracts/` and are validated in CI by both the .NET and Python sides. Within a major version
only additive changes are allowed. A breaking change requires a new `schema_version` and a new topic suffix.

| Event | Producer → consumers | Payload essentials |
|---|---|---|
| `TelemetryReceived` | Ingest → ML, API | metric, value, unit, quality, nominal band, sequence |
| `FailureRiskScored` | ML → API | model_version, probability, calibrated band, top features, window, TTL |
| `AnomalyDetected` | ML → API | detector_version, score, persistence count, affected metrics, window |
| `AlertRaised` / `AlertUpdated` | API → API (notifications), ML (feedback) | alert_id, type, severity, dedup key, review state |
| `WorkOrderApproved` | API → API (inventory, notifications) | work_order_id, parts, due date, approver |
| `MaintenanceClosed` | API → ML (labels), simulator (health reset) | work_order_id, disposition, condition label, finding summary |
| `ModelPromoted` | ML → API | model_version, task, metrics summary, approved_by |

```json
{ "event_id": "019a3b2c-7e1a-7c3d-9b1e-2f4a5c6d7e8f", "schema_version": "1.0", "type": "AnomalyDetected",
  "tenant_id": "tnt-alsafa", "asset_id": "MTR-042", "event_time_utc": "2026-11-01T10:09:10Z",
  "trace_id": "4bf92f3577b34da6a3ce929d0e0e4736", "detector_version": "anomaly-iforest@1.3.0", "score": 0.91,
  "persistence": 3, "metrics": ["vibration_rms"],
  "window": { "from": "2026-11-01T10:04:00Z", "to": "2026-11-01T10:09:00Z" } }
```

---

# Part D — Technical specification

## 20. Architecture overview

The system is four deployables with hard module boundaries and one event bus (ADR-002).

```
 Angular web (dashboard + technician pages) ── Static Web App / Nginx container
                    │ OIDC (Keycloak)
 ┌──────────────────▼───────────────────────────────┐
 │ Platform API — ASP.NET Core 10 modular monolith   │     ┌──────────────────────────────┐
 │  Assets · Maintenance · Inventory · Audit/Notif.  │◀───▶│ Event bus (Kafka API)         │
 │  BuildingBlocks: tenancy, auth, persistence,      │     │ Redpanda local / Event Hubs   │
 │  telemetry, outbox primitives                     │     └──────▲───────────────▲───────┘
 └──────┬───────────────────────────────────────────┘            │               │
        │                     ┌──────────────────────────┐       │   ┌───────────┴──────────┐
        │                     │ Ingest worker (.NET 10)   │───────┘   │ ML service (Python)   │
        │                     │ MQTT → validate → stores  │           │ scoring, anomaly,     │
        │                     └──────▲───────────────────┘           │ explanations, MLflow  │
        │                            │ MQTT QoS 1 (subscribe only)   └───────────────────────┘
        │                     ┌──────┴───────────────────┐
        │                     │ Plant simulator (Python)  │
        │                     └──────────────────────────┘
 ┌──────▼──────────────────┐ ┌──────────┐ ┌───────────────────┐
 │ PostgreSQL 16 +         │ │ Redis    │ │ Blob + MLflow      │
 │ TimescaleDB             │ │ (cache)  │ │ datasets, models,  │
 │ schema per module       │ └──────────┘ │ attachments        │
 └─────────────────────────┘              └───────────────────┘
```

| Deployable | Responsibility | Scaling profile |
|---|---|---|
| Platform API | Business modules behind one HTTP surface. Each module owns its schema and publishes events via the outbox. | 0–2 replicas, scale on HTTP |
| Ingest worker | Telemetry validation, deduplication, storage, enrichment, `TelemetryReceived` publication | 1 replica during demos, 0 idle |
| ML service | Consumes telemetry windows, scores risk and anomalies, serves `/score`, publishes results | 0–1 replica, CPU inference |
| Plant simulator | Generates telemetry, injects faults, replays history, receives "maintenance completed" | 0–1 replica, demo only |

**Module rules (enforced by architecture tests):**
- Each module's only public type is its registration class.
- Modules reference BuildingBlocks only, never each other or the host.
- A module's DbContext maps only into its own schema.
- Every tenant-scoped entity has a tenant query filter.
- No MQTT client exists in the platform.
- Nothing outside the simulator publishes on `plant/#`.
- Cross-module reads use query contracts (for example `IAssetDirectory`) or events.

## 21. Technology stack

| Concern | Choice | Decision |
|---|---|---|
| Business services | C# / .NET 10 LTS, ASP.NET Core minimal APIs, EF Core 10, Npgsql, OpenAPI + Scalar | ADR-001, ADR-010 |
| Ingestion | .NET 10 worker, MQTTnet, StackExchange.Redis, Confluent.Kafka | ADR-001 |
| ML and simulation | Python 3.12, FastAPI, scikit-learn, LightGBM, SHAP, MLflow client, paho-mqtt, pydantic; uv, ruff, mypy, pytest | ADR-001, ADR-005 |
| Front end | Angular 22, Angular Material, Transloco, angular-oauth2-oidc, Vitest, ESLint, Prettier; Node 22 LTS | ADR-006, ADR-011 |
| Identity | Keycloak (OIDC, PKCE), realm roles + `tenant_id` claim | ADR-008 |
| Data | PostgreSQL 16 + TimescaleDB, Redis 8, Blob/parquet | ADR-007 |
| Messaging | MQTT (Eclipse Mosquitto) at the OT edge; Kafka API (Redpanda local, Azure Event Hubs Standard in cloud) | ADR-003, ADR-004 |
| ML lifecycle | MLflow tracking and registry with a PostgreSQL backend | ADR-005 |
| Containers and cloud | Docker, Docker Compose, Azure Container Apps (scale to zero), Static Web Apps, PostgreSQL Flexible Server B1ms, Key Vault, Azure Monitor; Terraform | ADR-002 |
| Observability | OpenTelemetry (traces, metrics, logs) → Azure Monitor / Application Insights | — |
| Tests | xUnit v3 on Microsoft Testing Platform, AwesomeAssertions, NetArchTest, WebApplicationFactory, Testcontainers, pytest, Vitest, Playwright, axe | — |
| Delivery | GitHub Actions, gitleaks, commitlint, Dependabot, CodeQL, Trivy, SBOM, cosign | — |

## 22. Data architecture

### 22.1 Database layout

One PostgreSQL 16 instance with one schema per module (ADR-007):

| Schema | Tables (principal) | Notes |
|---|---|---|
| `assets` | tenants, plants, lines, assets, sensor_mappings | Unique (tenant, code); `xmin` row version on assets |
| `maintenance` | requests, work_orders, checklist_items, work_order_history, findings, attachments; alerts | History, findings and attachments are insert-only for the app role; no delete on work orders |
| `inventory` | parts, reservations, overrides | Reservation invariants enforced in the domain and by constraints |
| `audit` | audit_events; notifications | Append-only with trigger; unique (tenant, sequence) |
| `telemetry` | readings (hypertable), continuous aggregates | Timescale DDL only in dedicated migration files |

**Conventions for every schema:**
- Each module keeps its own EF Core migrations history in `{schema}.__ef_migrations_history`.
- Names are snake_case.
- Keys are UUID v7, generated by the application.
- Every tenant-scoped table carries `tenant_id varchar(64)`.

### 22.2 Tenancy and row-level security

**Three independent layers (ADR-008):**
1. **Policy-based authorisation** on routes.
2. **EF Core global query filters** on every `ITenantScoped` entity.
3. **PostgreSQL RLS** with `ENABLE` and `FORCE ROW LEVEL SECURITY` and one policy per table:
   `tenant_id = NULLIF(current_setting('app.tenant_id', true), '')` for both `USING` and `WITH CHECK`.

**Session setup.** On every connection open, an interceptor runs:

```sql
SET ROLE "sf_app";
SELECT set_config('app.tenant_id', @tenant, false);
```

`sf_app` is a NOLOGIN role with least-privilege grants. Migrations run in an elevated scope that issues `RESET ROLE`.
In Azure, the application logs in as a non-owner and migrations run from the pipeline.

**Default outcome.** With no tenant set, the policy compares against NULL and returns no rows.

**Write protection.** A save-changes interceptor stamps `tenant_id` on new rows and refuses cross-tenant writes.

### 22.3 Telemetry storage

| Item | Specification |
|---|---|
| Hypertable | `telemetry.readings(time, tenant_id, asset_id, metric, value, unit, quality, source, event_id, sequence)` |
| Partitioning | Time, with 1-day chunks; space by `asset_id` |
| Compression | After 7 days |
| Retention | 30 days raw |
| Continuous aggregates | 1 minute and 1 hour per asset and metric: avg, min, max, stddev, count |
| Uniqueness | `event_id`, and (asset, metric, sequence) within the dedup window |

### 22.4 Redis keys (cache only; losing Redis never loses data)

| Key | Content | TTL |
|---|---|---|
| `sf:{tenant}:last:{asset}` | Hash of metric → value, quality, time | none (overwritten) |
| `sf:{tenant}:dedup:{event_id}` | Seen marker | 10 minutes |
| `sf:{tenant}:rate:{client}` | Rate-limit counter | window length |

Redis runs with `noeviction` and no persistence. If Redis is down, deduplication falls back to database unique
constraints and freshness shows "unknown".

### 22.5 Data estate for ML

| Layer | Content |
|---|---|
| Bronze | Immutable parquet in Blob with provenance tags. Never edited. |
| Silver | TimescaleDB readings and aggregates. |
| Gold | Window features and point-in-time training sets. |

Silver and gold can be rebuilt from bronze plus code at a given commit. Ground-truth labels live only in the gold
layer and are never readable by online scoring.

### 22.6 Data-quality contract

| Dimension | Control |
|---|---|
| Completeness | Expected events per asset and interval; gaps shown as freshness warnings and a missing-event-rate feature |
| Validity | Physical bounds from the sensor catalogue, unit conversion, finite numbers |
| Consistency | Active mapping at event time, schema version, device identity, bounded clock skew |
| Freshness | Current vs stale on the dashboard; scoring suppressed when stale |
| Uniqueness | Deduplicate by `event_id` and (asset, metric, sequence) |
| Lineage | Source URI, ingestion job id, code commit, dataset hash and version on every training output |

### 22.7 Concurrency and auditing

- **Optimistic concurrency** uses PostgreSQL `xmin` as the row version.
- **Audit** follows section 15.11.
- **Outbox rows** (id, type, payload, occurred_at, published_at) are written in the same transaction as the change. A background relay publishes them with retries.

## 23. Integration architecture

| Concern | Specification |
|---|---|
| OT boundary | Read-only (ADR-004). The simulator publishes MQTT QoS 1. The ingest worker subscribes only. Topic ACLs allow a device to publish only its own asset topics. The only reverse message is "maintenance completed" to the simulator. |
| Topic scheme | `plant/{tenant}/{plant}/{line}/{asset}/{metric}`, for example `plant/tnt-alsafa/plant-01/line-a/MTR-042/vibration_rms` |
| Device trust | Per-device MQTT credentials or client certificates; documented rotation |
| Bus | Kafka API; topics per event type with a major-version suffix; partition key `asset_id` for ordering per asset |
| Outbox | Business commit and event row in one transaction; relay with retry; at-least-once delivery |
| Idempotency | Consumers deduplicate on `event_id` (Redis set plus a persisted processed-events table where needed); replay cannot duplicate alerts, work orders or reservations |
| Dead letters | One per consumer group; poison messages stored with reason and payload hash; admin replay |
| Trace context | W3C `trace_id` from the MQTT payload → Kafka headers → HTTP; one trace per end-to-end flow |
| Edge extension | Release 2: OPC UA client → MQTT bridge in the OT DMZ with store-and-forward. No change to business, telemetry or ML services. |

## 24. Machine-learning specification

### 24.1 Model A: equipment failure classifier

| Dimension | Specification |
|---|---|
| Data | UCI AI4I 2020 (10,000 synthetic rows, CC BY 4.0), attributed |
| Target | Binary machine-failure label; optional multi-class on documented failure modes |
| Features | Air and process temperature, rotational speed, torque, tool wear, product type; engineered temperature difference and power proxy. Label-derived deterministic fields are excluded. |
| Candidates | Dummy and logistic baselines → Random Forest → LightGBM, chosen by PR-AUC on held-out data |
| Validation | Stratified holdout with repeated seeds; subgroup analysis by product type |
| Metrics | PR-AUC; recall at fixed precision / alert load; Brier score and reliability diagram; confusion matrix at the chosen threshold |
| Explainability | Global feature influence; local SHAP values, with a caution against causal claims |
| Serving | FastAPI `/score`, model loaded from the MLflow registry by alias, schema-validated input, p95 < 300 ms on CPU; every response carries `model_version` and the synthetic-data header |
| Plant mapping | The simulator exposes AI4I-like features for CNC-018 so a live asset can be scored, with the limitation stated on screen |
| Alert policy | Threshold chosen from the cost of false positives vs missed failures; expected monthly alert counts reported for the simulated population; no field lead-time claim |

### 24.2 Model B: anomaly detection and alert intelligence

| Dimension | Specification |
|---|---|
| Data | Simulator telemetry, split by whole scenario runs (never by shuffled windows) |
| Features | 5-minute rolling windows: median, variance, rate of change, deviation from nominal band, missing-event rate; optional spectral features for vibration |
| Baseline | Engineering thresholds from the sensor catalogue and robust z-scores by operating condition (idle, normal, peak) |
| Model | Isolation Forest per asset class, trained on healthy windows; versioned training period |
| Post-processing | Persistence (N consecutive windows, default 3), hysteresis, per-asset cooldown, suppression during planned maintenance and on stale data |
| Review loop | Confirmed issue / benign event / unknown labels feed evaluation and threshold tuning |
| Metrics | Precision at alert cap; false alerts per asset-day; detection delay per fault type; load robustness (0 persistent alerts on peak-load healthy day); sensor-fault separation |

### 24.3 Lifecycle and governance (ADR-005)

| Stage | Required evidence | Tooling |
|---|---|---|
| Experiment | Parameters, seed, git SHA, dataset hash, metric report, reproducible script | MLflow tracking |
| Approval | Champion-vs-challenger report, threshold choice, release decision recorded in the pull request | CI job + model card template |
| Serving | Registry alias (champion), schema, response TTL, health probes, timeouts, fallback to thresholds | FastAPI + MLflow registry |
| Monitoring | Data quality, feature drift (PSI), output distribution, latency, errors, realised outcomes from closed work orders | OpenTelemetry → Azure Monitor |
| Rollback | Last approved model and image kept; one-command alias switch rehearsed | MLflow alias + feature flag |
| Retraining | Triggered by investigation, never automatic promotion | GitHub Actions workflow dispatch |

Model cards are mandatory and live next to the training code. They state:
- intended use and users, and out-of-scope uses (no autonomous control, no field accuracy);
- data source, licence, synthetic nature, split strategy and dataset hash;
- metrics with confidence from repeated seeds, calibration and subgroup results;
- limitations and failure modes;
- the monitoring plan and version history.

| ID | Requirement |
|---|---|
| MLR-01 | No model is promoted without beating its committed baseline on the untouched split and without human approval. |
| MLR-02 | Training is deterministic for a given seed and dataset hash. |
| MLR-03 | Feature extraction uses only readings with event time before the prediction time. A leakage test shifts labels and expects performance to collapse. |
| MLR-04 | Every score and alert records the model version that produced it. |
| MLR-05 | A drift alert fires on deliberately shifted data in tests. |

## 25. Security architecture

**Trust zones:**

```
OT zone (simulated PLCs/sensors) → OT DMZ (read-only edge / MQTT broker) → Cloud ingestion (tenant, identity,
message validation) → Business zone (APIs, AI, web, maintenance). No actuation or write-back toward the OT zone.
```

| ID | Control family | Requirement |
|---|---|---|
| SEC-01 | Identity | OIDC + PKCE for the SPA; MFA for the admin realm; audience-restricted tokens; roles and tenant claim; least privilege |
| SEC-02 | Tenant isolation | Three layers (22.2); cross-tenant tests for every endpoint, with tokens minted by a dev-only password-grant test client |
| SEC-03 | Separation of duties | Technicians cannot approve; only the assigned technician executes; enforced server-side |
| SEC-04 | Industrial isolation | Simulator and broker in an isolated network; outbound-only connector; no write operation toward PLC-side components; architecture test on `plant/#` |
| SEC-05 | Device trust | Per-device credentials or client certificates; topic ACLs |
| SEC-06 | Secrets | TLS everywhere; Key Vault with managed identity; GitHub OIDC federation to Azure (no cloud keys in GitHub); gitleaks in CI; no secrets in images or the repository |
| SEC-07 | Application defence | Input validation; magic-byte upload checks; size limits; generated storage keys; `nosniff` downloads; SSRF protection; rate limits; security headers; CORS allow-list |
| SEC-08 | Supply chain | Dependabot (grouped, majors gated by policy), CodeQL, Trivy, SBOM, cosign-signed images, pinned image tags, checksum-verified tools |
| SEC-09 | Database least privilege | App role `sf_app` with per-table grants; evidence tables insert-only; audit trigger blocks rewrites even for the owner |
| SEC-10 | Audit and incident | Hash-chained audit, anomalous-access alerts, token revocation, runbooks, tested backups |
| SEC-11 | Runtime lifecycle | LTS runtimes only. An unsupported runtime is a security finding; STS majors (for example .NET 11) are not adopted without a new ADR. |

**Threat model (STRIDE, top items):**

| Threat | Example | Mitigation |
|---|---|---|
| Spoofing | Rogue device publishes fake readings for MTR-042 | Per-device credentials, topic ACL, mapping validation |
| Tampering | Replayed or altered events create duplicate alerts | Event ids, sequence, idempotent consumers, audit hash chain |
| Repudiation | A user denies approving a work order | Audit with actor, correlation id and chain hash |
| Information disclosure | Tenant A reads tenant B's work orders | RLS, query filters, authorisation tests, 404 for foreign ids |
| Denial of service | Oversized uploads or message floods | Size limits before reading, rate limits, bounded queues |
| Elevation of privilege | Technician approves their own work order | Role policies plus resource-based checks in the domain |

## 26. Non-functional requirements

| ID | Attribute | Requirement / target | Verification |
|---|---|---|---|
| NFR-01 | Performance (UI reads) | API read p95 < 500 ms on a warm demo workload | Load test |
| NFR-02 | Performance (ML) | `/score` p95 < 300 ms, CPU, single request | Benchmark in CI |
| NFR-03 | Telemetry latency | Sensor event to dashboard p95 < 15 s | G3 check |
| NFR-04 | Capacity | 8 assets, 32 metrics, ~560k readings/day at full rate; load test at 3× full rate for 10 minutes | Load test |
| NFR-05 | Availability | 99 % during announced demo windows; not a contractual industrial SLA | Monitoring |
| NFR-06 | Data integrity | No accepted event lost after broker outage or worker restart; duplicates never create duplicate alerts, work orders or reservations | Resilience scripts, replay test |
| NFR-07 | Recovery | Automated backups; RPO ≤ 24 h, RTO ≤ 4 h for the demo; restore rehearsal documented | Rehearsal |
| NFR-08 | Security coverage | 100 % of tenant-scoped endpoints covered by cross-tenant tests (fitness test fails on an uncovered endpoint) | CI |
| NFR-09 | Audit coverage | 100 % of critical state changes audited | Integration tests |
| NFR-10 | Observability | One trace per end-to-end flow; RED metrics per service; structured logs with sampling and cost cap (0.5 GB/day ingestion cap) | Trace screenshot |
| NFR-11 | Usability | Technician flow at 360 px with no horizontal scroll and touch targets ≥ 44 px; keyboard navigation | Playwright check, axe |
| NFR-12 | Maintainability | Warnings as errors; lint and format checks; migrations checked in; contract tests; architecture tests; Conventional Commits | CI |
| NFR-13 | Portability | One-command local start (`docker compose up`); Kafka-API and MQTT contracts identical locally and in Azure | G1 check |
| NFR-14 | Cost | Idle Azure < AED 60/month; budget alerts at AED 150 and 250; nightly stop of PostgreSQL and the simulator | Budget dashboard |
| NFR-15 | Supportability | Runtimes on vendor LTS only, with end-of-support dates recorded | Lifecycle review |
| NFR-16 | Transparency | Synthetic-data label on 100 % of screens and API responses | E2E checks |

## 27. Deployment and environments

| Layer | Local (Docker Compose) | Azure portfolio deployment |
|---|---|---|
| Front end | Angular dev server or Nginx container (`compose` build) on :4200 | Azure Static Web Apps (Free) |
| Services | Platform API :5080, ingest worker, ML service, simulator | Azure Container Apps, consumption plan; min replicas 0 (simulator, ML) or 0–1 (API, ingest) |
| Identity | Keycloak with imported realm and seeded dev users | Keycloak as a Container App with a PostgreSQL schema |
| Transport | Mosquitto + Redpanda (+ console) | Mosquitto Container App + Event Hubs Standard (Kafka endpoint, 1 TU) |
| Database | PostgreSQL 16 + TimescaleDB image | PostgreSQL Flexible Server B1ms with timescaledb, stopped when idle |
| Cache | Redis container | Redis Container App or Azure Cache for Redis Basic C0 |
| Data and ML | Local parquet; MLflow container (PostgreSQL backend) | Blob Storage (Hot, LRS); MLflow Container App backed by PostgreSQL + Blob |
| Attachments | Named volume `attachments` | Blob container behind the same `IAttachmentStore` port |
| Observability | OpenTelemetry collector (optional profile) | Azure Monitor + Application Insights |
| Secrets and IaC | Git-ignored `.env`; `.env.example` with placeholders | Key Vault + managed identity; Terraform with remote state in Blob; GitHub OIDC federation |

**Local start.**
- Commands: `cp .env.example .env`, then `docker compose up -d --build`.
- Start-up is gated on health: PostgreSQL and Redis come first, then Keycloak, the API and the web app.
- The API migrates its schemas and seeds the plant idempotently on start.

**CI/CD (GitHub Actions):**

| Trigger | What runs |
|---|---|
| Pull request and push | Secret scan; commit-message lint; .NET build with warnings as errors and tests (Testcontainers PostgreSQL); Angular lint, format, tests and production build with bundle budgets; Python ruff, mypy, pytest; Compose and plant-spec validation; container builds with smoke tests; end-to-end job on a fresh Compose stack (API/web checks, browser login, technician job at 360 px, screenshots kept) |
| Merge to main | Sign images, push to the container registry, Terraform plan and apply to the demo environment, smoke test |
| Model candidates | Validate holdout results, compare with champion, update model card, manual approval before alias promotion |
| Nightly / weekly | Replay-based resilience test, grouped Dependabot updates, cost report |

**Branching:**
- One branch per week (`week-NN-topic`), plus `fix/`, `docs/` and `chore/` branches for changes outside the plan.
- Everything arrives through a pull request that the owner squash-merges.
- Each PR title is a Conventional Commit.

## 28. Observability

- OpenTelemetry SDKs in every service: traces, metrics and logs with a W3C trace context propagated across MQTT, Kafka and HTTP.
- RED metrics (rate, errors, duration) per endpoint and consumer.
- Domain metrics:
  - readings accepted, rejected and duplicated;
  - stale assets;
  - outbox lag;
  - dead letters;
  - alerts raised and updated;
  - scoring latency;
  - drift (PSI).
- Health endpoints: `/health/live` (process up) and `/health/ready` (dependencies). The container health check calls the running instance.
- The correlation id of each audit event is the trace id, so an auditor can jump from a record to its trace.

## 29. Resilience and failure behaviour

| Injected failure | Expected behaviour |
|---|---|
| Broker unavailable for 2 minutes | Ingest worker reconnects with backoff; simulator buffers and replays with original event times; no gaps after recovery |
| Duplicate delivery (QoS 1) | Duplicates counted in a metric, not stored; downstream alerts unchanged |
| ML service down | Threshold baseline keeps raising anomaly alerts flagged "baseline mode"; risk scores show "unavailable", never stale values |
| Redis down | Dedup falls back to database unique constraints; freshness shows unknown; API stays available |
| Database restart | Outbox publishes pending events after recovery; UI shows a degraded banner |
| Concurrent edits | Second writer gets 409 with an explanation; nothing is overwritten |
| Upload storage failure | Transaction aborted or file deleted; no record points at a missing file |

## 30. Testing and quality strategy

| Layer | Coverage | Tooling |
|---|---|---|
| Unit / domain | Work-order state machine (every state × action pair against an independently written expected lifecycle), permissions, inventory invariants, alert dedup, feature calculations, claims parsing | xUnit v3, AwesomeAssertions, pytest, Vitest |
| Architecture | Module boundaries, schema ownership, tenant filters on every tenant-scoped entity, single public type per module, no MQTT client in the platform, no `plant/#` publisher outside the simulator, every API endpoint requires authorisation | NetArchTest, source scans |
| Contract / integration | OpenAPI compatibility, JSON Schemas for events, outbox publication, Timescale queries, real PostgreSQL with RLS | WebApplicationFactory, Testcontainers |
| Tenancy | Every tenant-scoped endpoint attacked from a second tenant with identical data (404 only); raw SQL as the app role proves RLS and insert-only grants | AuthZ suite |
| End to end | Login → asset list (G1); planner and technician complete a work order (G2); technician job at 360 px | Playwright against Compose |
| ML / data | Schema and leakage checks, baseline comparison, deterministic training, drift alert on shifted data, `plant.yaml` validated | pytest, MLflow |
| Resilience | Duplicate events, broker outage, delayed telemetry, ML down, Redis down | Compose fault scripts |
| Security | Tenant isolation, IDOR, upload abuse (disguised executables, path traversal), secret scan, dependency scan, SAST, container scan, image signing | AuthZ suite, gitleaks, CodeQL, Trivy, cosign |
| Accessibility | Keyboard order, labels, 360 px technician flow | axe in Playwright |

**Release blockers:**
- a role can access another tenant;
- data lineage is unknown;
- a model breaks its baseline gate;
- an event replay can duplicate a work order or reservation;
- a screen lacks the synthetic-data label.

## 31. Delivery plan and gates

Effort is 240 hours over 12 weeks at 20 hours per week:

| Workstream | Hours |
|---|---|
| Foundation | 28 |
| Business modules | 48 |
| Simulator and telemetry | 40 |
| ML | 44 |
| Azure, IaC and security | 32 |
| QA and observability | 20 |
| Docs and demo | 16 |
| Buffer | 12 |

| Week | Deliverable | Gate |
|---|---|---|
| 1 | Repository, Compose stack, .NET solution skeleton with architecture tests, Angular shell with OIDC login, Python skeletons, CI with DevSecOps baseline | — |
| 2 | Tenant and Assets module, RLS, Audit module, OpenAPI contract, asset explorer | **G1 Foundation**: `docker compose up` gives login, asset list and green CI |
| 3 | Maintenance module: requests, state machine, approvals, technician page, findings with upload | — |
| 4 | Inventory, notifications, outbox and Kafka publisher, idempotent consumers, event schemas, Maintenance Hub UI | **G2 Useful operations**: planner and technician complete a work order end to end with full audit |
| 5 | Simulator v1, ingest worker, live chart | — |
| 6 | Fault library, degradation, control API, replay, data-quality flags, aggregates, freshness UI | **G3 Telemetry**: injected fault visible within 15 s; duplicates not stored |
| 7 | Model A pipeline, baselines, LightGBM, calibration, SHAP, registry, `/score`, model card | — |
| 8 | Model B, alerts, Risk Center UI, replay evaluation report | **G4 AI verified**: both model cards; replay report shows baseline beaten |
| 9 | Terraform and deploy pipeline with OIDC federation, budgets | — |
| 10 | Security hardening, CodeQL, Trivy, SBOM, cosign, OpenTelemetry to Azure Monitor; optional assistant from buffer | **G5 Secure cloud**: apply from a clean subscription; authZ suite green; budget alert configured |
| 11 | Resilience, E2E, load and accessibility tests; restore rehearsal | — |
| 12 | Demo script and video, README, C4 dossier, model cards, release v1.0 | **G6 Portfolio release**: public repository, hosted demo, five-minute video |

## 32. Risks

| Risk | Likelihood / impact | Mitigation |
|---|---|---|
| Scope growth | High / critical | Vertical slice by week 4; cut list; weekly deliverable; gates with definition of done |
| Synthetic-data overclaim | High / high | Labels on UI, API, cards and README; no field-accuracy language |
| Weak model generalisation | High / high | Scenario-level splits, baseline gates, calibration, limitations sections |
| False-positive alert fatigue | Medium / high | Persistence, hysteresis, cooldown, load-aware baselines, human review |
| Event duplication or ordering | Medium / medium | Outbox, idempotency, event-time windows, dead-letter topics |
| Runtime leaves support | Medium / medium | LTS-only policy; lifecycle review before cloud deploy |
| Local stack too heavy | Medium / medium | Compose profiles; Redpanda; single PostgreSQL |
| Cloud bill surprises | Medium / medium | Budgets, scale to zero, nightly stop, cost dashboard |
| OT write-back | Low / critical | No PLC write path in code; read-only connector; architecture test |
| Terraform / Azure friction | Medium / medium | Official examples; budgeted hours; documented manual fallback |

## 33. Traceability

| Business requirement | Functional / technical requirements | Verified by |
|---|---|---|
| BR-01 Visibility | FR-AST-01..07, FR-TEL-07..08, FR-ANL-01 | Asset API tests, G1 check |
| BR-02 Anomaly alerts | FR-TEL-01..09, FR-RSK-01..07, 24.2, MLR-03..05 | Replay report, G3 and G4 checks |
| BR-03 Failure risk | FR-RSK-01, FR-RSK-06, 24.1, MLR-01..04 | Model A card, CI gate |
| BR-04 Human authority | BRU-01, BRU-03, FR-MNT-02, FR-AIA-03, SEC-03 | State-machine unit tests, role tests |
| BR-05 Work-order lifecycle | 15.6, FR-MNT-01..11 | Exhaustive transition tests, API tests, G2 |
| BR-06 Technician on phone | FR-TEC-01..07, FR-FND-01..07, NFR-11 | Technician 360 px browser check |
| BR-07 Spare parts | FR-INV-01..08 | Inventory invariant tests, replay test |
| BR-08 Learning loop | FR-MNT-09, FR-SIM-09, FR-RSK-04 | Event contract tests, scenario replay |
| BR-09 Audit | FR-AUD-01..06, FR-MNT-03, NFR-09 | Audit integration tests, tamper test |
| BR-10 Tenant isolation | FR-IAM-03, 22.2, SEC-02, NFR-08 | Cross-tenant suite, RLS raw-SQL tests |
| BR-11 Synthetic label | Section 12, NFR-16 | E2E label checks, header tests |
| BR-12 Model governance | FR-MLO-02..03, 24.3, MLR-01 | Promotion audit tests |
| BR-13 Run anywhere, low cost | Section 27, NFR-13, NFR-14 | G1 and G5 checks, budget alerts |
| BR-14 Observability | Section 28, NFR-10 | End-to-end trace |
| BR-15 Bounded assistant | FR-AIA-01..08 | Golden-set evaluation |
| BR-16 No claims | Section 12, FR-ANL-03 | Content review |

## 34. References

**Decisions and specifications in this repository:**

| Reference | Location |
|---|---|
| ADR-001 to ADR-011 | `docs/adr/` |
| Solution blueprint v2.1 | `docs/blueprint/smartfactory-ai-blueprint-v2.1.pdf` |
| Plant specification | `docs/plant/plant.yaml` |
| Delivery plan | `docs/ROADMAP.md` |
| API contract | `contracts/openapi/platform-api.v1.json` |
| Learning notes | `docs/learning/` |

**External sources:**

| Source | Location |
|---|---|
| UCI AI4I 2020 predictive-maintenance dataset | https://archive.ics.uci.edu/dataset/601/ai4i+2020+predictive+maintenance+dataset |
| NASA PCoE data repository (C-MAPSS, Release 2) | https://www.nasa.gov/intelligent-systems-division/discovery-and-systems-health/pcoe/pcoe-data-set-repository/ |
| MQTT v5 specification (OASIS) | https://docs.oasis-open.org/mqtt/mqtt/v5.0/mqtt-v5.0.html |
| OPC UA specification | https://reference.opcfoundation.org/ |
| Microsoft MLOps v2 architecture | https://learn.microsoft.com/en-us/azure/architecture/ai-ml/guide/machine-learning-operations-v2 |
| NIST SP 800-82 Rev. 3, Operational Technology Security | https://csrc.nist.gov/pubs/sp/800/82/r3/final |
| ISO 55000:2024 asset management | https://www.iso.org/standard/83053.html |
| ISA-95 enterprise-control system integration | https://www.isa.org/standards-and-publications/isa-standards/isa-95-standard |
