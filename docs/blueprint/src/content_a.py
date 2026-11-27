"""Blueprint pages 01–12: cover, document guide, summary, constraints, skills, personas, scope, plant,
faults, simulator, navigation, workflow."""

from build_blueprint import (
    FOOTER_TEXT,
    ISSUE_DATE,
    NAVY,
    ORANGE,
    TEAL,
    VERSION,
    VERSION_LABEL,
    bars,
    bullets,
    callout,
    code,
    flow,
    kpis,
    page,
    panel,
    table,
)


# ---------------------------------------------------------------------------------------------
def cover(n: int) -> str:
    cubes = """
<svg viewBox="0 0 794 420" width="100%" height="100%" xmlns="http://www.w3.org/2000/svg" fill="none" stroke-width="1.4">
  <defs><linearGradient id="g1" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#1b4a6e" stop-opacity=".55"/><stop offset="1" stop-color="#0c2a44" stop-opacity=".2"/></linearGradient></defs>
  <g stroke="#3f8fb8" stroke-opacity=".9">
    <!-- cube 1 -->
    <path d="M66 110 L166 70 L266 110 L266 230 L166 270 L66 230 Z" fill="url(#g1)"/><path d="M66 110 L166 150 L266 110 M166 150 L166 270"/>
    <rect x="96" y="146" width="40" height="26" stroke="#4fd1b8"/><path d="M104 165 L128 153" stroke="#4fd1b8"/>
    <g fill="#4fd1b8" stroke="none"><circle cx="112" cy="252" r="3.5"/><circle cx="140" cy="261" r="3.5"/><circle cx="168" cy="270" r="3.5"/></g>
    <!-- cube 2 -->
    <path d="M296 135 L400 95 L504 135 L504 255 L400 295 L296 255 Z" fill="url(#g1)"/><path d="M296 135 L400 175 L504 135 M400 175 L400 295"/>
    <rect x="330" y="172" width="40" height="26" stroke="#4fd1b8"/><path d="M338 191 L362 179" stroke="#4fd1b8"/>
    <g fill="#4fd1b8" stroke="none"><circle cx="346" cy="277" r="3.5"/><circle cx="374" cy="286" r="3.5"/><circle cx="402" cy="295" r="3.5"/></g>
    <!-- cube 3 -->
    <path d="M536 190 L636 150 L736 190 L736 310 L636 350 L536 310 Z" fill="url(#g1)"/><path d="M536 190 L636 230 L736 190 M636 230 L636 350"/>
    <rect x="568" y="227" width="40" height="26" stroke="#4fd1b8"/><path d="M576 246 L600 234" stroke="#4fd1b8"/>
    <g fill="#4fd1b8" stroke="none"><circle cx="584" cy="332" r="3.5"/><circle cx="612" cy="341" r="3.5"/><circle cx="640" cy="350" r="3.5"/></g>
  </g>
  <!-- dotted ring -->
  <g transform="translate(590 80)">
    <circle r="112" stroke="#4fd1b8" stroke-opacity=".8" stroke-dasharray="3 7"/>
    <circle r="82" stroke="#4fd1b8" stroke-opacity=".55" stroke-dasharray="3 7"/>
    <g fill="#2fb79d" stroke="none">
      <circle cx="0" cy="-82" r="6"/><circle cx="58" cy="-58" r="4"/><circle cx="82" cy="0" r="6"/><circle cx="58" cy="58" r="4"/>
      <circle cx="0" cy="82" r="6"/><circle cx="-58" cy="58" r="4"/><circle cx="-82" cy="0" r="6"/><circle cx="-58" cy="-58" r="4"/>
    </g>
  </g>
</svg>"""
    return f"""
<section class="page cover">
  <div class="grid"></div>
  <div class="ck">Solution blueprint<span class="s">/</span>02 of 06<span class="s">/</span>Version {VERSION}</div>
  <div class="art">{cubes}</div>
  <div class="ttl">
    <h1 class="big">SmartFactory AI</h1>
    <div class="st">Industrial Predictive Maintenance<br>&amp; Asset Management Platform</div>
    <div class="tags">Simulated plant<span class="s">|</span>Industrial IoT<span class="s">|</span>AI/ML<span class="s">|</span>Containers &amp; microservices<span class="s">|</span>PostgreSQL + Redis<span class="s">|</span>Azure</div>
    <div class="meta">Engineering blueprint &nbsp;•&nbsp; November 2026 &nbsp;•&nbsp; {VERSION_LABEL} &nbsp;•&nbsp; 12 weeks × 20 hours</div>
    <div class="meta2">Portfolio-grade design <span style="margin:0 .5em">|</span> Synthetic data <span style="margin:0 .5em">|</span> Decision support, not industrial control</div>
  </div>
</section>"""


# ---------------------------------------------------------------------------------------------
def p02(n: int) -> str:
    body = callout(
        "Document intent",
        "An actionable specification for building a professional SmartFactory AI portfolio application in 240 developer "
        "hours (12 weeks, 4 hours a day, 5 days a week). It describes a simulated plant with machinery and sensors, a "
        "controlled read-only integration design and a cloud deployment on Azure. It is not a commissioned plant "
        "installation or a validated safety-control system.",
    )
    body += "<h3>Document control</h3>" + table(
        ["Field", "Specification"],
        [
            ["Document ID", "SMARTFACTORY-BP-002"],
            ["Version / issue date", f"{VERSION} / {ISSUE_DATE} (supersedes 2.0 of the same date; revision history on p.35)"],
            ["Status", "Architecture baseline and delivery plan for an independent portfolio implementation; week 1 delivered"],
            ["Primary audience", "Engineering hiring panels, solution architects, reliability engineers, security reviewers, the implementing developer"],
            [
                "Technology scope",
                "Angular 22, ASP.NET Core 10 modular monolith (.NET 10 LTS), Python 3.12 (FastAPI, scikit-learn, LightGBM), MQTT, Kafka API "
                "(Redpanda / Event Hubs), PostgreSQL + TimescaleDB, Redis, Keycloak OIDC, Docker, Azure Container Apps, Terraform, OpenTelemetry, MLflow",
            ],
            ["Licence", "MIT for the repository; UCI AI4I 2020 dataset attributed under CC BY 4.0; infrastructure components under their own licences"],
            ["Evidence labels", "Illustrative diagrams, UI, scenarios, forecast costs and targets are not measured field results"],
        ],
        widths=["22%", "78%"],
        cls="xs",
    )
    body += "<h3>What changed from v1.0</h3>" + table(
        ["Area", "v1.0", "v2.0"],
        [
            ["Service topology", "Six always-separate microservices", "Four deployables: modular .NET monolith, ingest worker, ML service, plant simulator (p.15)"],
            ["Models in scope", "Classifier, RUL, anomaly detection", "Classifier + anomaly detection core; RUL moved to Release 2 appendix"],
            ["Frontend", "React + PWA", "Angular (existing proficiency; frontend is not a target skill); responsive technician pages, no offline sync"],
            ["Plant definition", "Three example assets", "Full simulated plant: 2 lines, 8 machines, sensor catalogue, fault library, simulator design (pp.8–10)"],
            ["Open decisions", "Broker, identity, tenancy, data model undecided", "Redpanda/Event Hubs, Keycloak, shared-DB tenancy with row filters, entity list and event schema fixed"],
            ["Planning", "12 \"focused weeks\", no hour budget", "240-hour budget, weekly plan, gates with definition of done, skills map, AED cost envelope"],
        ],
        widths=["20%", "30%", "50%"],
        cls="xxs",
    )
    body += "<h3>Reading map</h3>" + table(
        ["Business / application", "Plant / data / AI", "Delivery / assurance"],
        [[
            "Pages 3–7 and 11–13: outcomes, constraints, skills map, personas, scope, navigation, workflow, domain",
            "Pages 8–10 and 14–22: simulated plant, sensors and faults, simulator, telemetry, architecture, events, data, models, MLOps",
            "Pages 23–36: UI concept, security, Azure, NFRs, cost, economics, QA, roadmap, risks, proof package, references, revision history and status",
        ]],
        cls="plain xs",
    )
    body += (
        '<p class="small"><strong>Design rule:</strong> predictive insights advise maintenance staff. The solution never commands PLCs, '
        "bypasses safety interlocks or replaces approved maintenance procedures.</p>"
    )
    return page(n, "01", "Document guide", "Governance, intended audience, what changed in v2.0 and v2.1, and reading map", body)


# ---------------------------------------------------------------------------------------------
def p03(n: int) -> str:
    body = (
        '<p class="lead">Manufacturers face an operational trade-off: preventive servicing can occur too early, while reactive servicing '
        "leaves production exposed to avoidable failure and overtime repair. SmartFactory AI combines an asset register, simulated "
        "sensor telemetry from a fully specified plant, predictive analytics, work orders and spare parts into one practical "
        "reliability product. Version 2 keeps the product ambition of v1.0 but right-sizes the engineering so the complete loop "
        "ships in 240 hours.</p>"
    )
    body += flow(
        [("Connect", "Plant + telemetry"), ("Detect", "Condition &amp; failure risk"), ("Plan", "Human-led maintenance"), ("Improve", "Measured reliability")],
        highlight={0},
        caps=True,
    )
    body += '<p class="note">Conceptual value chain; financial and uptime benefits must be measured in a real deployment.</p>'
    body += "<h3>Value proposition and observable outcomes</h3>" + table(
        ["Outcome", "Application capability", "Proof / operational measure"],
        [
            ["Improve visibility", "One workspace for plant, lines, machines, live condition and alerts", "Asset coverage and sensor freshness"],
            ["Earlier intervention", "Explainable failure scoring and anomaly detection with alert policy", "Recall at fixed alert volume / detection delay in replay"],
            ["Better planning", "Work orders, approvals, technician checklists and spare-part reservation", "Time to assign / approval-to-start"],
            ["Closed-loop learning", "Repair findings linked back to asset, alert and model version", "Outcome-label completeness / data drift"],
            ["Operational confidence", "Idempotent event processing, audit trail, tenant isolation, reproducible deployment", "Replay tests, authZ tests, one-command startup"],
        ],
        widths=["20%", "48%", "32%"],
    )
    body += "<h3>Headline engineering decisions</h3>" + bullets(
        [
            "<strong>Four deployable units, not six:</strong> a modular ASP.NET Core monolith (Assets, Maintenance, Inventory, Audit/Notifications), "
            "a telemetry ingest worker, a Python ML service and a Python plant simulator. Independent containers, one event bus, clear seams for later extraction.",
            "<strong>Polyglot persistence with purpose:</strong> PostgreSQL for business records, TimescaleDB hypertables for telemetry, Redis for live asset "
            "state and deduplication, Blob/parquet for ML datasets.",
            "<strong>Two models that earn their place:</strong> an equipment failure classifier on a public benchmark and an anomaly detector on plant telemetry, "
            "both with baselines, calibration and model cards. RUL forecasting is documented as Release 2.",
            "<strong>Cloud that costs little when idle:</strong> Azure Container Apps with scale-to-zero, a burstable PostgreSQL server that is stopped between demos, "
            "Terraform for reproducibility, budget alerts from day one.",
            "<strong>Supported runtimes only:</strong> .NET 10 LTS and Angular 22, chosen against vendor support windows (ADR-010, ADR-011), so the portfolio "
            "release never ships on an unsupported platform.",
        ]
    )
    body += callout(
        "Business boundary",
        "The product is a maintenance decision-support application, not an autonomous industrial controller. Predicted risks are not proof of "
        "impending failure. Qualified staff retain authority for inspections, lockout/tagout, scheduling and repair.",
    )
    return page(n, "02", "Executive summary", "Convert machine condition data into accountable maintenance decisions, in a scope one developer can finish", body)


# ---------------------------------------------------------------------------------------------
def p04(n: int) -> str:
    body = kpis(
        [
            ("Calendar", "12 weeks", "Mon–Fri, fixed cadence"),
            ("Daily focus", "4 hours", "One deliverable per week"),
            ("Total effort", "240 h", "Including 12 h buffer"),
            ("Cash envelope", "AED 700–1,600", "Details p.27"),
        ]
    )
    body += "<h3>Hour allocation by workstream</h3>"
    rows = [
        ("Foundation &amp; platform", [(28 / 48, TEAL)], "28 h"),
        ("Business modules", [(48 / 48, TEAL)], "48 h"),
        ("Simulator &amp; telemetry", [(40 / 48, TEAL)], "40 h"),
        ("AI models &amp; serving", [(44 / 48, TEAL)], "44 h"),
        ("Azure, IaC &amp; security", [(32 / 48, NAVY)], "32 h"),
        ("QA &amp; observability", [(20 / 48, NAVY)], "20 h"),
        ("Docs, demo &amp; proof", [(16 / 48, ORANGE)], "16 h"),
        ("Buffer / AI assistant", [(12 / 48, ORANGE)], "12 h"),
    ]
    body += panel(
        bars(rows)
        + '<p class="note" style="margin:8px 0 0">Bars are relative to the largest workstream (48 h). Total 240 h. Buffer is spent on the GenAI assistant (p.21) '
        "only if gates G1–G4 close on time. About one hour per week inside the docs and buffer lines is reserved for the learning note (p.30).</p>"
    )
    body += "<h3>Scope rules that protect the budget</h3>" + table(
        ["Rule", "Practical meaning"],
        [
            ["Vertical slice first", "Weeks 1–4 produce a usable maintenance system without any ML. Everything later adds to a working product."],
            ["One frontend framework, already known", "Angular with Angular Material. No design-system building, no custom charting library; use a mature chart component."],
            ["Boring infrastructure choices", "Docker Compose locally, Terraform modules copied from official examples, managed Azure services where the free or burstable tier exists."],
            ["Two models, documented limits", "Classifier and anomaly detector with baselines. No deep learning, no RUL, no hyper-parameter marathons."],
            ["Cut list, pre-agreed", "If a gate slips by more than three days, drop in this order: AI assistant, inventory forecasts, analytics trends, canary deployment, accessibility polish beyond keyboard navigation."],
            ["Definition of done per gate", "Each gate (p.30) has observable completion criteria. A gate is not \"mostly done\"."],
        ],
        widths=["25%", "75%"],
        cls="xs",
    )
    body += callout(
        "What \"practical\" means here",
        "The application exercises the same contracts a real plant integration would: MQTT topics per asset and metric, validated time-stamped readings, "
        "read-only ingestion, alerts that require human review, work orders with parts and audit. Swapping the simulator for a real edge gateway changes "
        "configuration, not architecture.",
    )
    return page(n, "03", "Delivery constraints and effort budget", "240 hours, one developer, one finished product", body)


# ---------------------------------------------------------------------------------------------
def p05(n: int) -> str:
    body = table(
        ["Skill area", "Where it is exercised in SmartFactory", "Verifiable evidence in the repository", "CV / job keywords"],
        [
            ["AI / ML engineering", "Failure classifier (LightGBM), anomaly detector (Isolation Forest), calibration, SHAP explanations, MLflow registry, model cards, drift monitoring",
             "Reproducible training scripts, experiment tracking, holdout reports, served model with versioned API", "Predictive maintenance, MLOps, scikit-learn, LightGBM, SHAP, MLflow, FastAPI"],
            ["Generative AI (bounded)", "Maintenance assistant that summarises alert evidence and drafts work-order text with citations, no actions",
             "Prompt templates, grounding tests, cost cap, refusal cases", "LLM integration, RAG, prompt engineering, AI guardrails"],
            ["Docker &amp; containers", "Multi-stage Dockerfiles for .NET, Python and Angular; health-gated Compose stack; non-root images; signed images",
             "<code>docker compose up</code> one-command start, five images built in CI, image scan", "Docker, Compose, OCI images, container security"],
            ["Microservices &amp; events", "Four deployables communicating via Kafka-API events; outbox; idempotent consumers; dead-letter queue; versioned schemas",
             "Event catalogue, contract tests, replay test proving no duplicate work orders", "Event-driven architecture, Kafka, outbox pattern, DDD, modular monolith"],
            ["PostgreSQL + time-series", "Business schema with EF Core migrations; TimescaleDB hypertables, compression and continuous aggregates for telemetry",
             "Migrations, query plans in docs, retention policy", "PostgreSQL, TimescaleDB, EF Core, SQL performance"],
            ["NoSQL / caching", "Redis for last-known asset state, alert deduplication windows and rate limiting; parquet in Blob; JSONB for semi-structured records",
             "Key design doc, TTL strategy, failure behaviour tests", "Redis, caching, key-value stores"],
            ["Azure cloud", "Container Apps, PostgreSQL Flexible Server, Event Hubs (Kafka endpoint), Blob Storage, Key Vault, Static Web Apps, Monitor, Budgets",
             "Terraform modules, deploy pipeline, cost dashboard screenshot", "Azure, Terraform, IaC, cloud cost management"],
            ["Security &amp; identity", "Keycloak OIDC, RBAC with tenant scope, rate limits, secret scanning, OT read-only boundary",
             "AuthZ test suite, threat model, SAST results", "OIDC, OAuth2, RBAC, OWASP, zero-trust"],
            ["Observability &amp; SRE", "OpenTelemetry traces from sensor event to work order; metrics; structured logs; alerts",
             "Trace screenshot of one end-to-end flow, SLO table", "OpenTelemetry, Grafana, Azure Monitor, SLOs"],
            ["Industrial IoT", "MQTT topic design, sensor catalogue, data quality flags, edge buffering concept, OPC UA extension point",
             "Simulator config, ingestion validation tests", "IIoT, MQTT, OPC UA, Industry 4.0, time-series"],
            ["Delivery engineering (DevSecOps)", "GitHub Actions: lint, tests, secret scan, commit lint, SAST, image build, Terraform plan/apply, model validation gate; Dependabot",
             "Green pipeline badge, release tags, rollback runbook", "CI/CD, GitHub Actions, DevOps, DevSecOps, supply chain"],
        ],
        widths=["15%", "33%", "30%", "22%"],
        cls="xs",
    )
    body += callout(
        "Positioning statement for the portfolio",
        "\"I designed and built an end-to-end predictive-maintenance platform: a simulated plant streaming sensor data over MQTT, event-driven .NET and "
        "Python services in containers, PostgreSQL/TimescaleDB and Redis, two documented ML models behind a versioned API, and a Terraform-provisioned Azure "
        "deployment with budget controls.\" Every clause is backed by code a reviewer can run.",
    )
    return page(n, "04", "Skills and marketability map", "Every major component maps to a skill in current demand and to evidence a reviewer can verify", body)


# ---------------------------------------------------------------------------------------------
def p06(n: int) -> str:
    body = table(
        ["Persona", "Objectives", "Daily workflow in the application"],
        [
            ["Reliability manager", "Monitor fleet health and avoid lost production", "Risk overview, alert policy, escalation, weekly health report"],
            ["Maintenance planner", "Prioritise work and coordinate resources", "Review recommendation, create and approve work order, reserve spares"],
            ["Technician", "Perform inspection safely and capture findings", "Assigned jobs on a phone-friendly page, checklist, notes, photos, completion with condition label"],
            ["Data / ML engineer", "Maintain evidence quality and model behaviour", "Pipeline monitoring, evaluation reports, drift, model promotion"],
            ["Administrator", "Control tenancy, roles and connectors", "Provision access, inspect audit trail, configure plant sources and thresholds"],
        ],
        widths=["20%", "28%", "52%"],
    )
    body += ('<p class="note">The production-supervisor role from v1.0 (downtime-window approval) is folded into the planner for Release 1 and listed in the Release 2 backlog. '
             "In the Keycloak realm the five personas are the realm roles <code>reliability-manager</code>, <code>maintenance-planner</code>, <code>technician</code>, "
             "<code>ml-engineer</code> and <code>administrator</code>.</p>")
    body += "<h3>Accountability and escalation</h3>" + bullets(
        [
            "<strong>Model:</strong> provides evidence and estimated risk; it cannot generate an authorised maintenance action.",
            "<strong>Planner:</strong> owns work-order prioritisation, resource allocation, approval and production-window choice.",
            "<strong>Technician:</strong> owns actual inspection results and equipment condition confirmations.",
            "<strong>Administrator:</strong> governs model access, connector permissions and immutable change history.",
        ],
        tight=True,
    )
    tick = "✓"
    dash = "—"
    body += "<h3>Role-to-permission matrix (RBAC, tenant-scoped)</h3>" + table(
        ["Capability", "Manager", "Planner", "Technician", "ML engineer", "Admin"],
        [
            ["View plant, assets, telemetry, alerts", tick, tick, "assigned assets", tick, tick],
            ["Create / approve work order", "approve", tick, dash, dash, tick],
            ["Execute checklist, record findings", dash, dash, tick, dash, dash],
            ["Reserve / issue parts", dash, tick, "issue only", dash, tick],
            ["Change alert thresholds", "propose", dash, dash, tick, tick],
            ["Promote / roll back model", dash, dash, dash, f"{tick} (with approval)", tick],
            ["Manage users, roles, connectors", dash, dash, dash, dash, tick],
        ],
        widths=["34%", "11%", "11%", "16%", "17%", "11%"],
        cls="compact",
    )
    body += callout(
        "Success principle",
        "Make every prediction traceable to an asset, an event window, a model version, a human decision and an eventual outcome. The permission matrix above "
        "is implemented as policy-based authorisation and verified by automated tests (p.29).",
    )
    return page(n, "05", "Personas and responsibility model", "Five roles linked by a traceable maintenance workflow", body)


# ---------------------------------------------------------------------------------------------
def p07(n: int) -> str:
    body = table(
        ["Functional module", "Release 1 (12 weeks, in scope)", "Release 2 (backlog)"],
        [
            ["Asset Registry", "Plant, lines, machines, metadata, criticality, sensor mapping, attachments", "Failure-mode library, lifecycle / warranty"],
            ["Live Condition", "Simulator-fed readings, data-quality flags, health status, trends per sensor", "OPC UA edge adapter, richer sensor profiles"],
            ["Risk Center", "Model scores, anomaly alerts, explanation, thresholds, alert history, review state", "Calibrated alert policies per asset family, RUL forecasts"],
            ["Maintenance", "Requests, work orders, state machine, tasks, assignment, approvals", "Scheduling optimisation, production-window negotiation"],
            ["Technician Portal", "Assigned work orders, checklist, findings, photo upload, completion label (responsive web)", "PWA offline sync, signatures"],
            ["Spare Parts", "Inventory, reservation against work order, minimum balance warning", "Supplier and lead-time forecasts"],
            ["Analytics", "Health heatmap, open work orders, downtime by asset, MTBF / MTTR from simulated history", "OEE, cost scenarios, reliability trends"],
            ["AI Assistant", "Stretch (buffer hours): alert summary and work-order draft with cited evidence", "Conversational history Q&amp;A, manual retrieval"],
            ["Administration", "Keycloak identity, RBAC, audit log, source configuration, system health", "SCIM, fine-grained ABAC, SSO federation"],
        ],
        widths=["20%", "48%", "32%"],
    )
    body += "<h3>Out of scope for the portfolio release</h3>" + bullets(
        [
            "Write access to industrial control systems or automated machine stops.",
            "Claims of production reliability improvement without a field trial and reliable baseline.",
            "Full ERP procurement, MES replacement, heavy industrial historian ingestion.",
            "Deep-learning models, remaining-useful-life forecasting, mobile native apps.",
        ],
        tight=True,
    )
    body += "<h3>Minimum demonstrable product (end of week 4)</h3>" + flow(
        [("Register plant", "Lines, machines, sensors"), ("Raise request", "Manual or from alert"), ("Approve WO", "Parts reserved"), ("Execute", "Checklist + findings"), ("Audit", "Immutable trail")],
        highlight={2},
    )
    body += '<p class="note">Before any sensor or model exists, the application is already a usable computerised maintenance management system. This protects the project against ML-side delays.</p>'
    return page(n, "06", "Product scope and feature inventory", "Release 1 is the 12-week deliverable; Release 2 is a documented backlog, not a promise", body)


# ---------------------------------------------------------------------------------------------
def p08(n: int) -> str:
    line_a = table(["Asset", "Class", "Criticality"], [["MTR-042", "Filler drive motor, 15 kW", "High"], ["CVR-012", "Belt conveyor, geared", "Medium"], ["CMP-007", "Screw air compressor", "High"], ["FAN-034", "Extraction fan", "Low"]], cls="compact")
    line_b = table(["Asset", "Class", "Criticality"], [["CNC-018", "3-axis CNC mill", "High"], ["PMP-211", "Coolant centrifugal pump", "Medium"], ["PRS-105", "Hydraulic press", "High"], ["CHL-003", "Process chiller", "Medium"]], cls="compact")
    body = f"""
<div class="plant">
  <div class="root"><div class="ft">Plant 01 — "Al Safa Industrial"</div><div class="fs">Fictional site • Asia/Dubai time zone • 2 shifts • tenant <code>tnt-alsafa</code></div></div>
  <div class="lines">
    <div><div class="line"><div class="ft">Line A — Packaging</div><div class="fs">Filling, conveying, compressed air</div></div>{line_a}</div>
    <div><div class="line"><div class="ft">Line B — Machining</div><div class="fs">Cutting, hydraulics, cooling</div></div>{line_b}</div>
  </div>
</div>"""
    body += "<h3>Machine instrumentation</h3>" + table(
        ["Asset", "Sensors (metric / unit)", "Interval", "Nominal operating band (illustrative)"],
        [
            ["MTR-042", "vibration_rms mm/s, bearing_temp °C, current A, speed rpm", "5 s", "1.0–2.8 mm/s, 45–70 °C, 22–28 A, 1,470–1,490 rpm"],
            ["CVR-012", "belt_speed m/s, motor_current A, gearbox_temp °C", "5 s", "0.8–1.2 m/s, 6–9 A, 40–60 °C"],
            ["CMP-007", "discharge_pressure bar, oil_temp °C, vibration_rms mm/s, run_hours h", "5 s", "7.0–8.0 bar, 60–85 °C, 1.5–3.5 mm/s"],
            ["FAN-034", "vibration_rms mm/s, airflow m³/h, current A", "10 s", "1.0–3.0 mm/s, 4,000–5,500 m³/h, 8–11 A"],
            ["CNC-018", "spindle_load %, spindle_temp °C, vibration_rms mm/s, tool_wear min, feed_rate mm/min", "2 s", "30–75 %, 35–55 °C, 0.8–2.5 mm/s, 0–200 min"],
            ["PMP-211", "flow L/min, suction_pressure bar, discharge_pressure bar, vibration_rms mm/s, temp °C", "5 s", "80–120 L/min, 0.2–0.6 bar, 3.0–4.0 bar, 1.0–3.0 mm/s"],
            ["PRS-105", "hydraulic_pressure bar, oil_temp °C, cycle_time s, cycles count", "per cycle (~8 s)", "180–210 bar, 40–65 °C, 7.5–9.0 s"],
            ["CHL-003", "supply_temp °C, return_temp °C, compressor_current A, refrigerant_pressure bar", "10 s", "6–9 °C, 11–15 °C, 14–20 A, 8–12 bar"],
        ],
        widths=["12%", "46%", "13%", "29%"],
        cls="compact",
    )
    body += (
        '<p class="small">Design load: 8 machines, <strong>32 metrics</strong> (v2.0 stated 31; the sensor table above has always listed 32, PMP-211 carrying five), '
        "roughly 7 readings per second plant-wide, about <strong>560,000 readings per day</strong> at full rate. The simulator supports a throttled profile "
        "(one reading per metric every 30 s, about 90,000 per day) for cloud demos. The machine-readable source of this page is "
        "<code>docs/plant/plant.yaml</code>, validated by the simulator's test suite. Vibration bands follow the ISO 20816 zone concept for illustration only [R14].</p>"
    )
    return page(n, "07", "Simulated plant specification", "A fictional packaging and machining plant with two lines and eight instrumented machines", body)


# ---------------------------------------------------------------------------------------------
def p09(n: int) -> str:
    body = table(
        ["Fault scenario", "Applies to", "Sensor signature", "Onset profile", "Label"],
        [
            ["Bearing wear", "MTR-042, FAN-034, PMP-211", "vibration_rms rises 1.0 → 7+ mm/s with growing variance; bearing_temp +10–20 °C late", "Exponential over 6–48 simulated hours", "BEARING"],
            ["Overheating", "MTR-042, CMP-007, CHL-003", "Temperature drift above band, current +8–15 %, no vibration change", "Linear ramp 1–6 h", "THERMAL"],
            ["Cavitation", "PMP-211", "Suction pressure drops, flow oscillates ±15 %, broadband vibration spikes", "Step onset with oscillation", "CAVITATION"],
            ["Belt slip / misalignment", "CVR-012", "belt_speed falls while motor_current rises; gearbox_temp creeps", "Intermittent then persistent", "MECHANICAL"],
            ["Tool wear", "CNC-018", "spindle_load +20–40 % at same feed, tool_wear counter beyond 180 min, vibration harmonics", "Monotonic with usage", "TOOL_WEAR"],
            ["Filter / cooler clogging", "CMP-007, CHL-003", "Pressure differential grows, oil_temp rises, cycle efficiency falls", "Slow drift over days", "CLOGGING"],
            ["Hydraulic leak", "PRS-105", "hydraulic_pressure sags during cycle, cycle_time lengthens, oil_temp rises", "Gradual with cycle count", "HYDRAULIC"],
            ["Sensor faults", "Any", "Flat-line, stuck value, random spikes, dropouts, clock skew, unit mismatch", "Random, configurable rate", "SENSOR_FAULT (quality flag, not failure)"],
        ],
        widths=["16%", "16%", "35%", "17%", "16%"],
        cls="compact",
    )
    body += "<h3>Degradation and schedule models</h3>"
    left = bullets(
        [
            "<strong>Health index h(t) ∈ [0,1]</strong> per asset; sensors are generated as nominal baseline + load effect + degradation term × (1 − h) + noise.",
            "<strong>Hazard shapes:</strong> linear, exponential and Weibull-style accelerating wear, chosen per fault; parameters stored in YAML with a fixed random seed for reproducibility.",
            "<strong>Operating context:</strong> two shifts, planned stops, production load cycles (idle / normal / peak) that modulate current, load and temperature so models must separate load from fault.",
        ]
    )
    right = bullets(
        [
            "<strong>Maintenance effect:</strong> a completed work order with disposition \"repaired\" resets h to 0.95–1.0; \"inspected, no fault\" leaves h unchanged, producing honest false-positive cases.",
            "<strong>Time compression:</strong> simulator clock runs 1× for live demos and up to 120× to generate weeks of history in minutes; event_time_utc always reflects simulated time.",
            "<strong>Ground truth log:</strong> every injected fault is written to a labels table (asset, fault, onset, detectable-from, failure time) used only for offline evaluation, never exposed to the models at inference.",
        ]
    )
    body += f'<div class="cols2"><div>{left}</div><div>{right}</div></div>'
    body += callout(
        "Safety and scientific validity",
        "Synthetic faults test pipeline behaviour, detection delay and alert quality under controlled conditions. They do not establish performance on rare real "
        "equipment faults or demonstrate actual avoided downtime. Every screen and report labels the data source as simulated.",
    )
    return page(n, "08", "Fault library and degradation models", "Scripted failure modes with physically plausible signatures, labelled for evaluation", body)


# ---------------------------------------------------------------------------------------------
def p10(n: int) -> str:
    body = panel(
        flow([("Plant config", "YAML: assets, sensors, bands, faults, schedule"), ("Sim engine", "Health index, hazard models, load cycles, noise, seed"),
              ("MQTT publisher", "Mosquitto, QoS 1, retained last value"), ("Ingest worker", "Validate → Timescale → Redis → Kafka")], highlight={1}, caps=True)
        + flow([("Control API (FastAPI)", "inject fault • clear fault • set speed • pause • seed • scenario presets"), ("Replay mode", "Replays parquet history at chosen speed with original event times")], caps=True)
    )
    body += "<h3>Interface contracts</h3>" + table(
        ["Element", "Specification"],
        [
            ["Topic scheme", "<code>plant/{tenant}/{plant}/{line}/{asset}/{metric}</code>, e.g. <code>plant/tnt-alsafa/plant-01/line-a/MTR-042/vibration_rms</code>; status on <code>.../{asset}/status</code>"],
            ["Payload", "JSON per the event schema on p.14: event_id, schema_version, event_time_utc, value, unit, quality, source=\"simulator\", sequence"],
            ["Quality of service", "MQTT QoS 1 (at-least-once); downstream deduplication by event_id and (asset, metric, sequence)"],
            ["Clock", "Simulated clock with configurable speed; bounded skew injection (±2 s) and occasional out-of-order delivery to test ingestion"],
            ["Scenarios", "Named presets: <code>healthy-day</code>, <code>bearing-wear-mtr042</code>, <code>cavitation-pmp211</code>, <code>tool-wear-cnc018</code>, <code>sensor-dropout</code>, <code>demo-5-minutes</code>"],
            ["Deployment", "Python 3.12 container built with uv, non-root, &lt; 100 MB image; runs locally in Compose and as an Azure Container App with min replicas 0 (started only for demos)"],
            ["Realism boundary", "Signals are physically plausible, not physics-simulated. The simulator is a test harness, not a digital twin validated against a real machine"],
        ],
        widths=["22%", "78%"],
        cls="compact",
    )
    body += "<h3>Edge gateway extension point (Release 2)</h3>" + (
        '<p class="small">The ingest worker depends only on the MQTT topic contract. A real plant would add an edge gateway (for example OPC UA client → MQTT bridge) in the OT DMZ '
        "with store-and-forward buffering. Nothing in the business, telemetry or ML services changes. This is the architectural proof that the design is practical beyond the simulation. [R3, R5]</p>"
    )
    body += code(
        """# docs/plant/plant.yaml (excerpt)
- id: MTR-042
  name: Filler drive motor        asset_class: MOTOR        rated_power_kw: 15
  sensors:
    - { metric: vibration_rms, unit: "mm/s", interval_s: 5, nominal: [1.0, 2.8] }
    - { metric: bearing_temp,  unit: "°C",   interval_s: 5, nominal: [45, 70] }
  applicable_faults: [BEARING, THERMAL, SENSOR_FAULT]
faults:
  - { id: BEARING, label: failure, hazard_shape: exponential, duration_h: [6, 48], applies_to: [MTR-042, FAN-034, PMP-211] }"""
    )
    return page(n, "09", "Plant simulator architecture", "A configurable digital stand-in for the factory floor, published over the same protocol a real gateway would use", body)


# ---------------------------------------------------------------------------------------------
def p11(n: int) -> str:
    body = table(
        ["Workspace", "Essential screens", "Critical interaction"],
        [
            ["Operations Home", "Plant switcher, asset health KPIs, alert trend, outstanding jobs, data freshness", "Drill to the worst-performing asset"],
            ["Assets", "Hierarchy explorer, profile, sensor mapping, maintenance history, sensor trends", "Assign source mapping and maintain metadata"],
            ["Risk Center", "Risk queue, anomaly alerts, feature explanation, model version and validity, review state", "Review alert / attach finding / create work request"],
            ["Maintenance Hub", "Work-order board, calendar, approval queue, job details, parts", "Assign technician and record completion"],
            ["Technician", "My jobs, checklist, findings, photos, completion label (phone-width layout)", "Complete job with condition confirmation"],
            ["Inventory", "On-hand stock, reservations, catalogue, threshold warnings", "Reserve part against work order"],
            ["Data &amp; Model Health", "Device freshness, pipeline errors, model comparison, drift", "Investigate drift or roll back model"],
            ["Settings &amp; Compliance", "Roles, connectors, thresholds, audit events", "Trace user action and change history"],
        ],
        widths=["20%", "52%", "28%"],
    )
    body += "<h3>Experience design decisions</h3>" + bullets(
        [
            "Desktop-first reliability dashboard; technician pages are responsive at 360 px width rather than a separate PWA.",
            "Angular 22 with Angular Material and a mature chart component; standalone components, signals, zoneless change detection, OnPush, typed reactive forms.",
            "Every sensor reading shows its timestamp and whether the value is live, replayed or generated.",
            "A persistent \"synthetic data — advisory only\" banner is part of the application shell, so no page can render without it; the API repeats the label in an <code>X-SmartFactory-Data</code> response header.",
            "Explanations use readable operating factors and name model-specific limitations.",
            "Critical actions require permissions and confirmation; no prediction triggers work without review.",
            "English first with an i18n-ready structure (Transloco); Arabic translation is Release 2.",
        ],
        tight=True,
    )
    body += callout(
        "Frontend decision (ADR-006, version amended by ADR-011)",
        "Angular is chosen over React because the developer already works productively in it, the frontend is not one of the target skills for this portfolio "
        "piece, and 240 hours cannot absorb a framework switch. Angular 22 replaces the Angular 20 named in v2.0 because Angular 20 leaves long-term support on "
        "28 November 2026, inside the build window. The API is framework-neutral; a React client could be added later without backend change.",
    )
    return page(n, "10", "Information architecture and navigation", "Make the application usable by planners, managers and field technicians", body)


# ---------------------------------------------------------------------------------------------
def p12(n: int) -> str:
    body = flow([("01 Sensor event", "MQTT reading"), ("02 Quality gate", "Validate, dedupe, flag"), ("03 Risk score", "Classifier + anomaly")], highlight={2})
    body += flow([("04 Planner review", "Human review before action"), ("05 Work order", "Approve, parts, assign"), ("06 Outcome label", "Finding feeds evaluation")], highlight={0})
    body += '<p class="note">Real maintenance decisions remain with qualified personnel; no automatic PLC/SCADA actuation.</p>'
    body += "<h3>Illustrative scenario: bearing wear on motor MTR-042</h3>" + bullets(
        [
            "<strong>10:08</strong> Simulator runs preset <code>bearing-wear-mtr042</code>; vibration_rms climbs from 2.1 to 4.6 mm/s over the next simulated hours while current and speed stay nominal.",
            "<strong>10:08</strong> Ingest worker validates event time, unit and asset mapping; a duplicated QoS-1 message is discarded by event_id; readings land in TimescaleDB and the Redis last-value hash.",
            "<strong>10:09</strong> Anomaly detector flags a persistent deviation (three consecutive windows above threshold); failure classifier scores elevated risk; a deduplicated alert is raised with model IDs and feature explanation.",
            "<strong>10:13</strong> Planner reviews the trend, sees vibration above the ISO-style alarm band, and creates a maintenance request; approval reserves a bearing kit from inventory.",
            "<strong>11:05</strong> Technician completes the checklist, records \"bearing play confirmed, replaced\", uploads a photo; the work order closes; the simulator receives a maintenance-complete event and resets the asset health index.",
            "<strong>Later</strong> The confirmed label is linked to the alert for offline evaluation; the alert counts as a true positive with a measured detection delay.",
        ]
    )
    body += "<h3>Work-order state machine</h3>" + flow(
        [("Requested", ""), ("Approved", ""), ("Scheduled", ""), ("In progress", ""), ("Completed", ""), ("Closed", "")], highlight={4}
    )
    body += ('<p class="small">Side transitions: Rejected (from Requested), On hold (from Scheduled / In progress), Cancelled (any open state, reason required). '
             "Closing requires disposition (repaired / no fault found / deferred) and a checklist status. Every transition is versioned and audited.</p>")
    body += callout(
        "Safety requirement",
        "High-risk warning does not equal verified fault. The application is advisory and never issues actuator, PLC or emergency-shutdown commands. The only "
        "message the platform sends back toward the simulator is a \"maintenance completed\" notification, which a real plant would route to its CMMS, not to control equipment.",
    )
    return page(n, "11", "Predict-to-maintain workflow", "One lifecycle from telemetry event to verified repair outcome", body)


PAGES = [cover, p02, p03, p04, p05, p06, p07, p08, p09, p10, p11, p12]
