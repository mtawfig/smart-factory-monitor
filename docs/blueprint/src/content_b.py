"""Blueprint pages 13–24: domain model, telemetry, architecture, events, data engineering, datasets, Model A,
Model B, AI assistant, MLOps, dashboard concept, security."""

from build_blueprint import NAVY, ORANGE, RED, TEAL, bars, bullets, callout, code, flow, page, panel, table


def p13(n: int) -> str:
    body = table(
        ["Aggregate / entity", "Key fields and invariants", "Owner module"],
        [
            ["Tenant", "Id, name, status. Every business table carries <code>tenant_id</code>; EF Core global query filter plus PostgreSQL row-level security policy as defence in depth", "Platform"],
            ["Plant / Line / Asset", "Hierarchy, time zone, asset class, criticality, serial number, operational state, commissioning date", "Assets"],
            ["SensorMapping", "Asset, metric type, unit, calibrated unit, source topic, validity window, activation dates, nominal band", "Assets"],
            ["TelemetryReading", "Hypertable: time, tenant, asset, metric, value, quality, source, event_id; 30-day raw retention, continuous aggregates 1 min / 1 h", "Telemetry (Timescale)"],
            ["Alert", "Asset, type (risk / anomaly), score, model_version, explanation JSON, window, dedup key, review state, reviewer, linked request", "Maintenance"],
            ["MaintenanceRequest", "Reason, priority, alert reference, requester, requested date, decision state", "Maintenance"],
            ["WorkOrder", "Asset, assigned role/user, workflow status, tasks, checklist, required parts, due date, approval, disposition, version", "Maintenance"],
            ["Finding", "Work order, technician, condition label, notes, attachments (Blob URIs, magic-byte validated)", "Maintenance"],
            ["Part / Reservation", "SKU, location, on-hand, reserved, minimum balance; reservation cannot exceed available without documented override", "Inventory"],
            ["AuditEvent", "Actor, tenant, aggregate, action, before/after hash, correlation ID, UTC timestamp; append-only", "Audit"],
            ["OutboxMessage", "Id, type, payload, occurred_at, published_at; written in the same transaction as the business change", "Each module"],
            ["ModelVersion", "Registry id, task, dataset hash, metrics, approved_by, status (candidate / active / retired)", "ML service"],
        ],
        widths=["20%", "64%", "16%"],
        cls="compact",
    )
    body += "<h3>Rules enforced in code and tests</h3>" + bullets(
        [
            "Every work order belongs to exactly one asset and tenant; edits are versioned; closing requires disposition and checklist status.",
            "Inventory reservations cannot exceed available quantity without a documented override by a planner or admin.",
            "An alert can be linked to at most one open request; duplicate alerts within the dedup window update the existing alert instead of creating a new one.",
            "Cross-module reads go through query services or events, never through another module's tables (enforced by architecture tests; each module's only public type is its registration class).",
        ],
        tight=True,
    )
    body += callout(
        "Persistence decision (ADR-007)",
        "One PostgreSQL instance, one schema per module (<code>assets</code>, <code>maintenance</code>, <code>inventory</code>, <code>audit</code>, <code>telemetry</code>), "
        "TimescaleDB extension for the telemetry schema, Redis for ephemeral state. This gives real polyglot-persistence experience without the operational cost of several database engines.",
    )
    return page(n, "12", "Equipment model and work-order domain", "A normalised asset hierarchy with maintainable business records and a decided tenancy model", body)


def p14(n: int) -> str:
    body = panel(
        flow([("Plant sensors", "Simulator (OPC UA bridge later)"), ("MQTT broker", "Mosquitto, QoS 1, TLS"), ("Ingest worker", ".NET worker: validate, dedupe, enrich"),
              ("Stores", "Timescale + Redis last value"), ("Event bus", "TelemetryReceived (Kafka API)")], highlight={1, 3}, caps=True)
        + '<div class="flow" style="justify-content:center"><div class="fc" style="flex:0 0 60%"><div class="ft caps">Offline buffer (concept)</div><div class="fs">Edge store-and-forward; replay preserves event time; dedup by event_id</div></div></div>'
    )
    body += "<h3>Event schema: TelemetryReceived v1</h3>" + table(
        ["Field", "Type / validation", "Example"],
        [
            ["event_id / schema_version", "UUID v7 / semantic version", "019a... / 1.0"],
            ["tenant_id / asset_id", "Opaque IDs; mapping must exist and be active", "tnt-alsafa / MTR-042"],
            ["event_time_utc / received_at_utc", "ISO 8601 UTC; skew bounded to ±120 s or flagged", "2026-11-01T10:08:02Z"],
            ["metric / value / unit", "Allow-listed metric; finite number; unit converted to calibrated unit", "vibration_rms / 3.4 / mm/s"],
            ["quality / source", "good, questionable, missing; simulator, replay, edge", "good / simulator"],
            ["trace_id / sequence", "W3C trace context; monotonic per asset+metric for ordering", "4bf9... / 93442"],
        ],
        widths=["24%", "54%", "22%"],
        cls="compact",
    )
    body += "<h3>Ingestion rules</h3>" + bullets(
        [
            "<strong>Validate:</strong> payload size, schema version, device identity (client certificate or token), unit, finite value, asset mapping active at event time.",
            "<strong>Deduplicate:</strong> Redis set of recent event_ids (TTL 10 min) plus (asset, metric, sequence) check; duplicates are counted, not stored.",
            "<strong>Order:</strong> out-of-order events within the window are accepted (TimescaleDB handles late inserts); older than retention are rejected with a metric.",
            "<strong>Enrich:</strong> attach line, plant, asset class and nominal band for downstream consumers; publish <code>TelemetryReceived</code> through the Kafka API.",
            "<strong>Freshness:</strong> last-value hash per asset in Redis with timestamp; dashboard shows \"stale\" after 3 intervals; scoring is suppressed on stale data.",
            "<strong>Subscribe only:</strong> the worker's topic filter must start with <code>plant/</code>; configuration validation and an architecture test enforce that no platform component publishes under <code>plant/#</code> (ADR-004).",
        ],
        tight=True,
    )
    body += callout(
        "Why MQTT plus a Kafka-API bus",
        "MQTT is the lightweight protocol real gateways speak; the Kafka API gives durable, replayable, partitioned streams for services. Locally the bus is Redpanda "
        "(single container, Kafka-compatible). In Azure it is Event Hubs Standard with the Kafka endpoint, so no client code changes. Azure IoT Hub is a documented alternative, not a dependency. [R4, R5, R11, R15]",
    )
    return page(n, "13", "Telemetry ingestion and edge strategy", "Reliable time-series ingestion without dangerous OT write-back", body)


def p15(n: int) -> str:
    diagram = (
        '<div class="flow" style="justify-content:center"><div class="fc hi" style="flex:0 0 58%"><div class="ft caps">Angular web (dashboard + technician pages)</div><div class="fs">Static Web App / Nginx container</div></div></div>'
        '<div class="flow" style="justify-content:center"><div class="fc" style="flex:0 0 58%"><div class="ft caps">Keycloak (OIDC) + API ingress</div><div class="fs">Tokens, roles, tenant claim, rate limiting</div></div></div>'
        + '<div class="flow">'
        '<div class="fc hi"><div class="ft caps">Platform API</div><div class="fs">ASP.NET Core 10 modular monolith: Assets • Maintenance • Inventory • Audit/Notifications</div></div>'
        '<div class="fc"><div class="ft caps">Ingest worker</div><div class="fs">.NET 10 worker: MQTT → validate → Timescale, Redis, bus</div></div>'
        '<div class="fc"><div class="ft caps">ML service</div><div class="fs">Python FastAPI: scoring, anomaly, explanation, model registry</div></div>'
        '<div class="fc"><div class="ft caps">Plant simulator</div><div class="fs">Python: signals, faults, control API</div></div></div>'
        '<div class="flow">'
        '<div class="fc"><div class="ft caps">Event bus</div><div class="fs">Redpanda local / Event Hubs Kafka</div></div>'
        '<div class="fc"><div class="ft caps">PostgreSQL + TimescaleDB</div><div class="fs">Module schemas, hypertables, outbox</div></div>'
        '<div class="fc"><div class="ft caps">Redis</div><div class="fs">Last value, dedup, rate limits</div></div>'
        '<div class="fc"><div class="ft caps">Blob + MLflow</div><div class="fs">Datasets, models, artifacts</div></div></div>'
    )
    diagram = diagram.replace('class="flow" style="justify-content:center"', 'class="flow" style="justify-content:center;gap:8px"')
    diagram = diagram.replace('<div class="flow">', '<div class="flow" style="gap:8px">').replace('<div class="arr">›</div>', "")
    body = panel(diagram)
    body += "<h3>Service ownership</h3>" + table(
        ["Deployable", "Technology", "Responsibility", "Scaling profile"],
        [
            ["Platform API", "ASP.NET Core 10 (.NET 10 LTS), EF Core 10, Npgsql, minimal APIs, OpenAPI + Scalar", "Business modules behind one HTTP surface; each module owns its schema and publishes events via outbox", "0–2 replicas, scale on HTTP"],
            ["Ingest worker", ".NET 10 worker, MQTTnet, Npgsql, StackExchange.Redis, Confluent.Kafka", "Telemetry validation, dedup, storage, enrichment, TelemetryReceived publication", "1 replica during demos, 0 idle"],
            ["ML service", "Python 3.12, FastAPI, scikit-learn, LightGBM, SHAP, MLflow client; built with uv", "Consumes TelemetryReceived windows, scores risk and anomalies, publishes FailureRiskScored / AnomalyDetected", "0–1 replica, CPU inference"],
            ["Plant simulator", "Python 3.12, paho-mqtt, FastAPI control API; built with uv", "Generates plant telemetry, injects faults, replays history", "0–1 replica, demo only"],
        ],
        widths=["15%", "28%", "40%", "17%"],
        cls="xxs",
    )
    body += "<h3>Why not six services, and why not one</h3>" + bullets(
        [
            "<strong>Modular monolith for business logic:</strong> Assets, Maintenance, Inventory and Audit change together, share transactions (reserve part when approving work order) "
            "and are tiny at demo scale. Modules are isolated by namespace, schema and architecture tests; extraction is a refactor, not a rewrite. A shared BuildingBlocks "
            "library (tenancy, authentication, telemetry, outbox primitives) is the only code the host and the ingest worker have in common.",
            "<strong>Separate processes where the runtime differs:</strong> Python for ML, a long-running MQTT subscriber for ingestion, a load generator for simulation. Each has its own image, lifecycle, scaling and failure domain.",
        ],
        tight=True,
    )
    body += callout(
        "Architecture decision (ADR-001, ADR-002, ADR-010)",
        ".NET for workflow services, Python for ML; a modular monolith plus three single-purpose services, deployed as Azure Container Apps with scale-to-zero. "
        "Runtimes are LTS releases only: .NET 10 (supported to November 2028) replaces the .NET 8 named in v2.0, whose support ends 10 November 2026. Kubernetes is the documented migration path.",
    )
    return page(n, "14", "Right-sized microservices architecture", "Four deployable units with hard module boundaries and one event bus", body)


def p16(n: int) -> str:
    body = table(
        ["Event (topic)", "Producer → consumers", "Payload essentials (all carry event_id, tenant_id, asset_id, event_time_utc, trace_id, schema v1)"],
        [
            ["TelemetryReceived", "Ingest → ML, API", "metric, value, unit, quality, nominal band, sequence"],
            ["FailureRiskScored", "ML → API", "model_version, probability, calibrated band, top features, window, TTL"],
            ["AnomalyDetected", "ML → API", "detector_version, score, persistence count, affected metrics, window"],
            ["AlertRaised / AlertUpdated", "API → API (notifications), ML (feedback)", "alert_id, type, severity, dedup key, review state"],
            ["WorkOrderApproved", "API → API (inventory, notifications)", "work_order_id, parts, due date, approver"],
            ["MaintenanceClosed", "API → ML (labels), Simulator (health reset)", "work_order_id, disposition, condition label, finding summary"],
            ["ModelPromoted", "ML → API", "model_version, task, metrics summary, approved_by"],
        ],
        widths=["22%", "22%", "56%"],
        cls="compact",
    )
    body += "<h3>Integration patterns</h3>" + bullets(
        [
            "<strong>API gateway / ingress:</strong> verifies the Keycloak token, tenant claim and role for interactive operations; applies per-client rate limits.",
            "<strong>Transactional outbox:</strong> business commit and event record in one transaction; a background publisher relays to the bus with retry and exactly-once semantics at the producer side.",
            "<strong>Idempotent consumers:</strong> processed event IDs stored per consumer; replay cannot create duplicate alerts, work orders or reservations.",
            "<strong>Dead-letter topics:</strong> poison messages quarantined with reason and payload hash; an admin screen lists them and allows authorised replay.",
            "<strong>Versioned schemas:</strong> JSON Schema per event under <code>/contracts</code>; additive changes only within a major version; consumer contract tests run in CI.",
            "<strong>Trace context:</strong> W3C trace ID propagates from MQTT payload to Kafka headers to HTTP; one OpenTelemetry trace shows sensor event → alert → work order.",
        ],
        tight=True,
    )
    body += code(
        '{ "event_id": "019a3b2c-7e1a-7c3d-9b1e-2f4a5c6d7e8f", "schema_version": "1.0", "type": "AnomalyDetected",\n'
        '  "tenant_id": "tnt-alsafa", "asset_id": "MTR-042", "event_time_utc": "2026-11-01T10:09:10Z", "trace_id": "4bf92f3577b34da6a3ce929d0e0e4736",\n'
        '  "detector_version": "anomaly-iforest@1.3.0", "score": 0.91, "persistence": 3,\n'
        '  "metrics": ["vibration_rms"], "window": {"from": "2026-11-01T10:04:00Z", "to": "2026-11-01T10:09:00Z"} }'
    )
    body += callout(
        "Data ownership",
        "Assets module owns asset records; Maintenance owns alerts and work orders; Telemetry owns readings; ML owns model versions. Cross-module state is propagated "
        "through events or read-only query services, never through shared writes.",
    )
    return page(n, "15", "Service contracts and event choreography", "Make distributed processing reliable, observable and testable", body)


def p17(n: int) -> str:
    body = panel(
        flow([("Sources", "UCI AI4I rows • simulator telemetry • work-order outcomes"), ("Validation", "Type, range, duplicates, clock"), ("Bronze", "Immutable parquet in Blob, provenance tags"),
              ("Silver", "Timescale hypertable + 1 min / 1 h aggregates"), ("Gold", "Window features, point-in-time training sets")], highlight={1, 4}, caps=True)
    )
    body += "<h3>Data quality contract</h3>" + table(
        ["Quality dimension", "Control and handling"],
        [
            ["Completeness", "Expected event count per asset and interval; gaps surfaced as freshness warnings and missing-event-rate feature"],
            ["Validity", "Physical sensor bounds from the sensor catalogue, unit conversions, finite numerical types"],
            ["Consistency", "Asset mapping active at event time, schema version, device identity, bounded clock skew"],
            ["Freshness", "Current vs stale shown on dashboard; scoring suppressed when stale"],
            ["Uniqueness", "Deduplicate by event_id and (asset, metric, sequence) within a bounded window"],
            ["Lineage", "Source URI, ingestion job ID, code commit, dataset hash and version recorded on every training output"],
        ],
        widths=["20%", "80%"],
        cls="compact",
    )
    body += "<h3>TimescaleDB usage</h3>" + bullets(
        [
            "Hypertable <code>telemetry.readings</code> partitioned by time (1-day chunks) and space (asset_id); native compression after 7 days; raw retention 30 days for the demo.",
            "Continuous aggregates per asset and metric at 1 minute and 1 hour (avg, min, max, stddev, count) feed dashboards and ML window features cheaply.",
            "Point-in-time feature extraction uses only readings with event_time earlier than the prediction time; tested with a leakage check that shifts labels and expects performance to collapse.",
        ],
        tight=True,
    )
    body += callout(
        "Keep bronze immutable",
        "Bronze data is never edited. Silver and gold are rebuildable from bronze plus code at a given commit. The demo clearly marks the synthetic source on every dataset, chart and model card.",
    )
    return page(n, "16", "Data engineering and provenance", "Reproducible ingestion for synthetic telemetry and ML datasets", body)


def p18(n: int) -> str:
    body = table(
        ["Dataset / source", "Supported task", "Key limitation / usage"],
        [
            ["UCI AI4I 2020", "Machine failure classification and failure-mode studies (Model A)", "10,000 synthetic rows; several failure modes are deterministic functions of features; cannot validate a production deployment [R1]"],
            ["SmartFactory plant simulator", "Anomaly detection, alert policy, detection delay, pipeline and workflow testing (Model B)", "Developer-controlled patterns; evaluates engineering behaviour, not ML generalisation to real faults"],
            ["Work-order outcomes", "Closed-loop label collection design; precision of reviewed alerts", "Outcomes are simulated or entered by the developer; no genuine field truth"],
            ["NASA C-MAPSS (Release 2)", "Remaining-useful-life research notebook", "Simulated turbofan degradation; cross-domain; documented as a separate experiment only [R2]"],
        ],
        widths=["20%", "35%", "45%"],
        cls="compact",
    )
    body += "<h3>Evaluation leakage prevention</h3>" + bullets(
        [
            "For AI4I, exclude deterministic label-derived fields from inference features, use stratified held-out evaluation with repeated seeds and explain the synthetic generation mechanism in the model card.",
            "For the simulator, split by scenario run (whole injected-fault episodes), never by shuffled windows; train the detector only on windows labelled healthy.",
            "Record experiment seed, dataset hash, preprocessing version, feature list, code commit, model binary and licence attribution for every run.",
            "Distinguish failure presence now from probability of failure within a future horizon; the AI4I label does not encode alert lead time.",
        ],
        tight=True,
    )
    body += "<h3>Metrics that matter for maintenance teams</h3>" + table(
        ["Metric", "Why it matters", "Reported where"],
        [
            ["PR-AUC, recall at fixed precision", "Failures are rare; accuracy is misleading", "Model A card, Model Health screen"],
            ["Brier score, reliability diagram", "A 70 % risk should mean roughly 70 % observed frequency", "Model A card"],
            ["Precision at alert cap, false alerts per asset-day", "Teams cannot review unlimited alarms", "Model B card, Risk Center"],
            ["Detection delay (injection to alert)", "A warning must be actionable", "Replay test report"],
        ],
        widths=["28%", "48%", "24%"],
        cls="compact",
    )
    body += callout(
        "Important modelling constraint",
        "A high classification score on AI4I cannot be presented as predicting failure several days early. Anomaly detection on the simulator demonstrates pipeline capability and alert engineering, not field accuracy.",
    )
    return page(n, "17", "Dataset plan and experimental integrity", "Separate evidence for classification, anomaly detection and future prognostics", body)


def p19(n: int) -> str:
    body = panel(
        bars([("Motor MTR-042", [(0.84, RED)], "84%"), ("CNC CNC-018", [(0.65, ORANGE)], "65%"), ("Pump PMP-211", [(0.44, ORANGE)], "44%"),
              ("Fan FAN-034", [(0.25, TEAL)], "25%"), ("Conveyor CVR-012", [(0.09, TEAL)], "9%")])
        + '<p class="note" style="margin:8px 0 0">Illustrative risk values; not predictions produced by a trained model.</p>'
    )
    body += "<h3>Model specification</h3>" + table(
        ["Dimension", "Design decision"],
        [
            ["Prediction target", "Binary synthetic machine-failure label; optional multi-class on documented failure modes"],
            ["Model candidates", "Dummy and logistic baseline → Random Forest → LightGBM; pick by PR-AUC on held-out data, not training fit"],
            ["Input features", "Air and process temperature, rotational speed, torque, tool wear, product type; engineered deltas (temperature difference, power proxy)"],
            ["Validation", "Stratified holdout for benchmark; repeated seeds; subgroup analysis by product type"],
            ["Primary metrics", "PR-AUC; recall at fixed precision / alert load; Brier calibration; confusion matrix at chosen threshold"],
            ["Explainability", "Global feature influence; local SHAP values with caution against causal claims"],
            ["Serving", "FastAPI <code>/score</code> endpoint, model loaded from MLflow registry by alias, schema-validated input, p95 &lt; 300 ms CPU; every response carries <code>model_version</code> and the synthetic-data header"],
            ["Mapping to the plant", "Simulator exposes AI4I-like features for CNC-018 (speed, torque, tool wear, temperatures) so the classifier can score a live asset with the limitation stated on screen"],
            ["Release gate", "Beat baseline on untouched test; measured latency; stable schema; model card with documented limits; human approval before promotion (ADR-005)"],
        ],
        widths=["20%", "80%"],
        cls="compact",
    )
    body += (
        '<p class="small"><strong>Alert policy:</strong> choose the risk threshold from the cost of false positives and missed failures; report expected monthly alert counts under the simulated '
        "asset population; do not claim field lead time from the AI4I benchmark. <strong>Budget:</strong> 16 hours including card and serving.</p>"
    )
    return page(n, "18", "Model A — equipment failure classifier", "Baseline first; optimise for useful alerts rather than headline accuracy", body)


def p20(n: int) -> str:
    body = "<h3 style='margin-top:0'>Detection pipeline</h3>" + bullets(
        [
            "<strong>Window features (5-minute rolling):</strong> median, variance, rate of change, deviation from nominal band, missing-event rate; optional spectral features for vibration when justified.",
            "<strong>Baseline:</strong> engineering thresholds from the sensor catalogue and robust z-scores by operating condition (idle / normal / peak).",
            "<strong>Model:</strong> Isolation Forest per asset class trained on declared healthy windows; versioned training period; optional reconstruction-based method if time allows.",
            "<strong>Post-processing:</strong> persistence (N consecutive windows), threshold hysteresis, cooldown per asset, suppression during planned maintenance and stale data.",
            "<strong>Review loop:</strong> operators label an alert as confirmed issue, benign event or unknown; labels feed evaluation and threshold tuning.",
        ],
        tight=True,
    )
    body += "<h3>Alert quality versus alert fatigue</h3>" + table(
        ["Measure", "Why it matters", "Measurement design"],
        [
            ["Precision at alert cap", "Maintenance teams cannot review unlimited false alarms", "Correct reviewed alerts / reviewed alerts, with a cap of alerts per asset-day"],
            ["Detection delay", "A warning must be actionable", "Injection-to-alert lag in scenario replay, per fault type"],
            ["False alerts per asset-day", "Compares alerting approaches fairly", "Count with exposure / uptime denominator on healthy-day scenarios"],
            ["Load robustness", "Peak production must not look like a fault", "Scenario with load cycles and no fault; target zero persistent alerts"],
            ["Sensor-fault separation", "Flat-lines and spikes are data problems, not failures", "Quality flags route to data-health, not to the risk queue"],
        ],
        widths=["22%", "34%", "44%"],
        cls="compact",
    )
    body += "<h3>Illustrative replay results table (what the demo will produce)</h3>" + table(
        ["Scenario", "Baseline thresholds", "Isolation Forest + persistence", "Target"],
        [
            ["bearing-wear-mtr042", "Detected at ~70 % of degradation", "Detected earlier with ≤ 1 false alert", "Delay reported in simulated hours"],
            ["cavitation-pmp211", "Oscillation missed by static bands", "Detected within 3 windows", "≤ 15 simulated minutes"],
            ["healthy-day with peak load", "Several nuisance alerts", "0 persistent alerts", "0"],
        ],
        widths=["22%", "26%", "27%", "25%"],
        cls="compact",
    )
    body += '<p class="note">Cells describe the expected shape of results; the actual numbers come from the replay test report committed to the repository.</p>'
    body += callout(
        "Safety and scientific validity",
        "Synthetic anomaly injections test pipeline detection. They do not establish performance on rare real equipment faults or demonstrate actual avoided downtime. "
        "<strong>Budget:</strong> 18 hours including features, evaluation and alert engineering.",
    )
    return page(n, "19", "Model B — anomaly detection and alert intelligence", "The model that works directly on plant telemetry, with separate evaluation from failure prediction", body)


def p21(n: int) -> str:
    body = callout(
        "Scope rule",
        "Built only from the 12-hour buffer and only if gates G1–G4 close on time. It is valuable for marketability (LLM integration with guardrails) but the platform is complete without it.",
    )
    body += "<h3>Capabilities</h3>" + table(
        ["Capability", "Behaviour", "Guardrail"],
        [
            ["Alert explanation", "Plain-language summary of why an alert fired: metrics, deviation from band, model version, recent maintenance", "Only facts retrieved from the platform; every sentence cites a record"],
            ["Work-order draft", "Suggests title, tasks, checklist items and likely parts from asset class and alert type", "Draft only; planner edits and approves; nothing is saved automatically"],
            ["History Q&amp;A", "Answers \"what was done on MTR-042 in the last 90 days\" from work orders and findings", "Tenant-scoped retrieval; refuses questions outside maintenance records"],
        ],
        widths=["20%", "50%", "30%"],
        cls="compact",
    )
    body += "<h3>Implementation sketch</h3>" + flow(
        [("Context builder", "Alert, readings summary, asset profile, last 5 work orders"), ("Prompt template", "System rules, citations required, no actions"),
         ("Hosted LLM API", "Azure-hosted or vendor API, small model tier"), ("Validator", "Citation check, length, PII filter, cost cap")],
        highlight={1},
    )
    body += bullets(
        [
            "<strong>Grounding:</strong> structured context from the database, not free web search; retrieval is tenant-scoped and role-checked before the prompt is built.",
            "<strong>Evaluation:</strong> a 20-case golden set (alert → expected facts) run in CI; a response that cites a non-existent record fails the build.",
            "<strong>Cost control:</strong> per-tenant daily token budget, caching of identical requests, estimated AED 10–30 for the whole demo period.",
            "<strong>Honesty:</strong> the UI labels output as AI-generated draft; the audit log records prompt hash, model name and version.",
        ],
        tight=True,
    )
    body += callout(
        "Why it is safe to include",
        "The assistant reads and writes text only. It has no tool that changes a work order, a threshold or a model, and it never receives raw control-system access. "
        "This keeps the \"decision support, not control\" boundary intact while demonstrating current GenAI engineering practice.",
    )
    return page(n, "20", "Maintenance AI assistant (bounded, stretch scope)", "A grounded generative-AI feature that explains, drafts and cites, and never acts", body)


def p22(n: int) -> str:
    body = flow(
        [("Ingest", "Quality"), ("Prepare", "Features"), ("Experiment", "MLflow"), ("Validate", "Gates"), ("Register", "Version + alias"), ("Deploy", "Feature flag"), ("Monitor", "Drift + feedback")],
        highlight={3, 6},
    )
    body += '<p class="note">Promotion is approved; monitoring triggers investigation, not blind auto-promotion.</p>'
    body += "<h3>Model lifecycle requirements</h3>" + table(
        ["Lifecycle stage", "Required evidence", "Tooling"],
        [
            ["Experiment", "Parameters, seed, git SHA, dataset hash, metric report, reproducible script", "MLflow tracking (local in Compose with a PostgreSQL backend, container in Azure)"],
            ["Approval", "Champion-vs-challenger report, threshold choice, signed release decision in the pull request", "CI job + model card template"],
            ["Serving", "Model registry alias (<code>champion</code>), schema, response TTL, health probes, timeouts and fallback to thresholds", "FastAPI + MLflow registry"],
            ["Monitoring", "Data quality, feature drift (PSI), output distribution, latency, errors, realised outcomes from closed work orders", "OpenTelemetry metrics → Grafana / Azure Monitor"],
            ["Rollback", "Keep last approved model and image; one-command alias switch rehearsed in the demo", "MLflow alias + feature flag"],
            ["Retraining", "Triggered by investigation; retrain in isolation, compare, human promotion gate", "GitHub Actions workflow dispatch"],
        ],
        widths=["18%", "56%", "26%"],
        cls="compact",
    )
    body += "<h3>Model card contents (both models)</h3>" + bullets(
        [
            "Intended use and users; out-of-scope uses (no autonomous control, no field accuracy claims).",
            "Data: source, licence, synthetic nature, split strategy, dataset hash.",
            "Metrics with confidence from repeated seeds; calibration plot; subgroup results.",
            "Limitations and failure modes; monitoring plan; version history.",
        ],
        tight=True,
    )
    body += ('<p class="small">Use MLflow locally and optionally Azure Machine Learning for managed jobs later. The Microsoft MLOps v2 guidance separates data, experimentation, '
             "registry, staging, production and monitoring; this design follows it at portfolio scale. [R6]</p>")
    return page(n, "21", "MLOps and model governance", "A controlled pipeline from experimentation to deployment and monitoring, sized for one developer", body)


def p23(n: int) -> str:
    spark = (
        '<svg viewBox="0 0 300 70" width="100%" height="70" xmlns="http://www.w3.org/2000/svg" fill="none" stroke-width="1.6">'
        '<line x1="0" y1="18" x2="300" y2="18" stroke="#4fd1b8" stroke-opacity=".5" stroke-dasharray="3 3"/>'
        '<text x="4" y="14" font-size="6" fill="#df6a5b">alarm band 4.5 mm/s</text>'
        '<polyline points="0,52 30,50 60,51 90,49 120,47 150,44 180,40 210,34 240,27 270,20 300,12" stroke="#4fd1b8"/>'
        '<polyline points="0,60 30,60 60,59 90,59 120,58 150,58 180,57 210,56 240,55 270,54 300,53" stroke="#f0a24a"/></svg>'
        '<div style="font-size:8.6px;color:#9eb3bf;margin-top:2px"><span style="color:#4fd1b8">■</span> vibration_rms &nbsp; <span style="color:#f0a24a">■</span> bearing_temp</div>'
    )
    risk_rows = [("MTR-042", 0.86, RED), ("CNC-018", 0.72, ORANGE), ("PMP-211", 0.55, ORANGE), ("CMP-007", 0.34, TEAL), ("CVR-012", 0.14, TEAL)]
    risk = '<div class="risk">' + "".join(
        f'<div>{a}</div><div class="tr"><div class="fi" style="width:{p*100:.0f}%;background:{c}"></div></div><div>{p*100:.0f}%</div>' for a, p, c in risk_rows
    ) + "</div>"
    body = f"""
<div class="dash">
  <div class="top"><div><b>SMARTFACTORY</b> Production Reliability Center</div><div style="color:#9eb3bf">PLANT 01 / SIMULATION &nbsp;•&nbsp; data: live simulator &nbsp;•&nbsp; fresh 4 s ago &nbsp;•&nbsp; <span style="color:#4fd1b8">synthetic</span></div></div>
  <div class="body">
    <div class="nav"><div class="on">Overview</div><div>Asset hierarchy</div><div>Live telemetry</div><div>Risk center</div><div>Maintenance hub</div><div>Technician</div><div>Spare parts</div><div>Analytics</div><div>Model health</div><div>Administration</div></div>
    <div>
      <div class="tiles">
        <div class="tile"><div class="l">Assets monitored</div><div class="v">8</div></div>
        <div class="tile"><div class="l">At-risk machines</div><div class="v o">2</div></div>
        <div class="tile"><div class="l">Open work orders</div><div class="v">5</div></div>
        <div class="tile"><div class="l">Uptime (sim.)</div><div class="v g">97.2%</div></div>
      </div>
      <div class="row2">
        <div class="card"><div class="l">MTR-042 / vibration_rms and bearing_temp (last 60 min)</div>{spark}</div>
        <div class="card"><div class="l">Predicted failure risk (model failure-lgbm@2.1)</div>{risk}</div>
      </div>
      <div class="card"><div class="l">Maintenance recommendations / human review</div>
        <table><tr><th>Asset</th><th>Condition</th><th>Model</th><th>Work order</th><th>Status</th></tr>
        <tr><td>MTR-042</td><td class="warn">High vibration, persistent</td><td>anomaly-iforest@1.3 • failure-lgbm@2.1</td><td>WO-00087</td><td>Review needed</td></tr>
        <tr><td>CNC-018</td><td>Tool wear beyond 180 min</td><td>failure-lgbm@2.1</td><td>WO-00088</td><td>Assigned</td></tr>
        <tr><td>PMP-211</td><td>Flow oscillation</td><td>anomaly-iforest@1.3</td><td>WO-00089</td><td>Scheduled</td></tr></table>
      </div>
    </div>
  </div>
</div>
<p class="note">Original illustrative interface design. All numbers and statuses in the mockup are fabricated for the portfolio concept and are not measured results.</p>"""
    body += "<h3>Interaction design checklist</h3>" + bullets(
        [
            "Navigation from plant KPI to asset detail, telemetry history, related alerts and assigned work orders in two clicks.",
            "Risk cards show age, model version, severity reason, data quality and current review status.",
            "Technician pages allow checklist completion, findings, attachment upload and completion label at phone width.",
            "Data-health drawer reveals late or missing device events and the suppression of stale-data inferences.",
        ],
        tight=True,
    )
    body += callout(
        "Live demo target",
        "Replay sensor data into a working dashboard; inject a fault; watch the alert appear; create and approve a work order; complete it as a technician; show the correlated trace and the model inference record.",
    )
    return page(n, "22", "Operations dashboard — UI concept", "A multi-role reliability workspace, not a static prediction screenshot", body)


def p24(n: int) -> str:
    body = panel(
        flow([("OT zone", "PLCs / sensors / plant systems (simulated)"), ("OT DMZ", "Read-only edge gateway / MQTT broker"), ("Cloud ingestion", "Tenant + identity + message validation"), ("Business zone", "APIs / AI / web / maintenance")],
             highlight={1, 3}, caps=True)
        + '<p class="tagline"><span class="otflag">Read-only OT data flow</span> No direct actuation or write-back by default; the only reverse message is a CMMS-style "maintenance completed" notification.</p>'
    )
    body += "<h3>Baseline controls</h3>" + table(
        ["Control family", "Required implementation (Release 1)"],
        [
            ["Identity / authorisation", "Keycloak OIDC with PKCE for the SPA; MFA for admin realm; roles and tenant claim in tokens; audience-restricted API tokens; policy-based authorisation per endpoint; least privilege"],
            ["Tenant isolation", "EF Core global filters + PostgreSQL row-level security; automated cross-tenant access tests on every sensitive endpoint, minted with a dev-only password-grant test client"],
            ["Industrial isolation", "Simulator and broker in an isolated Compose network / Container Apps environment; outbound-only connector; no write operations toward PLC-side components"],
            ["Device trust", "Per-device MQTT credentials or client certificates; topic ACLs so a device can publish only its own asset topics; rotation documented"],
            ["Data / secrets", "TLS everywhere; Azure Key Vault with managed identity; no secrets in images or repository (the Keycloak realm export carries no users or credentials; dev users are seeded from a git-ignored .env); gitleaks in CI"],
            ["Application defence", "Input validation, upload magic-byte sniffing, SSRF protection, rate limits, dependency scanning (Dependabot), SAST (CodeQL), container scan (Trivy), signed images, security headers"],
            ["Audit and incident", "Append-only audit with hash chain, anomalous-access alerts, token revocation, runbooks, tested backups"],
        ],
        widths=["20%", "80%"],
        cls="compact",
    )
    body += "<h3>Threat model summary (STRIDE, top items)</h3>" + table(
        ["Threat", "Example", "Mitigation"],
        [
            ["Spoofing", "Rogue device publishes fake readings for MTR-042", "Per-device credentials, topic ACL, mapping validation"],
            ["Tampering", "Replayed or altered events create duplicate alerts", "Event IDs, sequence, idempotent consumers, audit hash chain"],
            ["Information disclosure", "Tenant A reads tenant B's work orders", "Row-level security, authZ tests, IDOR checks"],
            ["Elevation of privilege", "Technician approves own work order", "Role policies, separation of duties enforced server-side"],
        ],
        widths=["20%", "40%", "40%"],
        cls="compact",
    )
    body += ('<p class="small">NIST SP 800-82 Rev. 3 emphasises the special reliability and safety considerations of OT environments; ISO 55000:2024 provides asset-management principles; '
             "ISA-95 structures enterprise/manufacturing integration. This design is aligned conceptually, not certified. [R7–R9]</p>")
    return page(n, "23", "Security, safety and OT trust boundary", "Explicit segmentation and enterprise identity as first-class requirements", body)


PAGES = [p13, p14, p15, p16, p17, p18, p19, p20, p21, p22, p23, p24]
