"""Blueprint pages 25–35: Azure topology, NFRs, cost, economics, QA, roadmap, risks and ADRs, proof package,
references, appendix, revision history."""

from build_blueprint import NAVY, ORANGE, TEAL, TEAL_DARK, bars, bullets, callout, code, kpis, page, panel, table


def p25(n: int) -> str:
    body = table(
        ["Layer", "Local (Docker Compose)", "Azure portfolio deployment", "Enterprise extension (documented only)"],
        [
            ["Frontend", "Angular dev server / Nginx container", "Azure Static Web Apps (Free)", "Front Door + WAF, enterprise SSO"],
            ["Services", "4 app containers", "Azure Container Apps, consumption plan, min replicas 0 (simulator, ML) or 0–1 (API, ingest)", "Dedicated plan or AKS, VNet, HA"],
            ["Identity", "Keycloak container with imported realm and seeded dev users", "Keycloak as Container App with Postgres schema, or Microsoft Entra External ID free tier", "Entra ID federation, Conditional Access"],
            ["Telemetry transport", "Mosquitto + Redpanda (console included)", "Mosquitto Container App + Event Hubs Standard (Kafka endpoint, 1 TU)", "IoT Hub / IoT Operations, edge onboarding"],
            ["Database", "PostgreSQL 16 + TimescaleDB image", "Azure Database for PostgreSQL Flexible Server, Burstable B1ms, timescaledb extension enabled, stopped when idle", "HA, zone redundancy, PITR 35 days"],
            ["Cache / state", "Redis container", "Redis Container App (demo) or Azure Cache for Redis Basic C0", "Premium tier, geo-replication"],
            ["Data estate", "Local parquet", "Blob Storage (Hot, LRS) for bronze/gold and MLflow artifacts", "ADLS Gen2, Purview catalogue"],
            ["ML experiments", "MLflow container (PostgreSQL backend)", "MLflow Container App backed by Postgres + Blob", "Azure Machine Learning workspace"],
            ["Observability", "OTel collector / Grafana/Tempo", "Azure Monitor + Application Insights, daily cap 0.5 GB", "Alert rules, on-call, SLO dashboards"],
            ["Secrets / IaC", ".env (git-ignored), .env.example with placeholders", "Key Vault + managed identity; Terraform with remote state in Blob; GitHub Actions OIDC federation to Azure (no cloud keys in GitHub)", "Private endpoints, policies, landing zone"],
        ],
        widths=["15%", "22%", "40%", "23%"],
        cls="compact",
    )
    body += "<h3>Environment separation</h3>" + bullets(
        [
            "<strong>Local development:</strong> one Compose file with health-gated start-up order; profiles (<code>core</code>, <code>ml</code>, <code>observability</code>) arrive with the observability stack so a laptop can run the minimum set.",
            "<strong>Cloud demo:</strong> a single resource group, seeded data, Azure Budget with alerts at AED 150 and AED 250, and a scheduled \"stop\" workflow for Postgres and the simulator at night.",
            "<strong>Enterprise target:</strong> described, not built: segregated OT network, controlled edge ingress, dedicated production security and capacity planning.",
        ],
        tight=True,
    )
    body += callout(
        "Why Container Apps",
        "Consumption metering and scale-to-zero suit intermittent portfolio demos, and the platform is Kubernetes-based under the hood, so the Dapr, KEDA and ingress concepts transfer. "
        "It is not a substitute for an industrial availability or safety design. [R10–R12]",
    )
    return page(n, "24", "Azure hosting and environment topology", "Low-cost portfolio deployment with named services, provisioned by Terraform", body)


def p26(n: int) -> str:
    body = kpis([("UI read API", "p95 &lt; 500 ms", "Warm demo workload"), ("ML scoring", "p95 &lt; 300 ms", "CPU inference, single request"),
                 ("Telemetry lag", "p95 &lt; 15 s", "Sensor event to dashboard"), ("Audit coverage", "100 %", "Critical state changes")])
    body += table(
        ["Quality attribute", "Recommended target / test"],
        [
            ["Availability", "Demo goal 99 % during announced demo windows; explicit maintenance windows; not a contractual industrial SLA"],
            ["Data integrity", "No lost accepted events after simulated broker outage and worker restart; bounded replay backlog; duplicates never create duplicate alerts or work orders"],
            ["Scalability", "Design workload 8 assets, 32 metrics, ~560k readings/day at full rate; throttled cloud profile ~90k/day; load test at 3× full rate for 10 minutes"],
            ["Durability / recovery", "Automated Postgres backups; RPO ≤ 24 h, RTO ≤ 4 h for the demo; restore rehearsal documented"],
            ["Security", "100 % of sensitive endpoints tenant-scoped and covered by authorisation tests"],
            ["Observability", "One trace per end-to-end flow; RED metrics per service; log sampling with cost cap"],
            ["Maintainability", "Build pipelines, lint and static checks, migrations, contract tests, grouped weekly Dependabot updates, architecture tests on module boundaries, Conventional Commits enforced"],
            ["Supportability", "Runtimes on vendor LTS releases only (ADR-010); end-of-support dates recorded; lifecycle review before the cloud deployment"],
            ["Accessibility", "Keyboard navigation, readable chart alternatives, 360 px layout for technician pages"],
            ["Cost", "Budget alerts; idle monthly cost below AED 60; demo monthly cost within the p.27 envelope"],
        ],
        widths=["20%", "80%"],
        cls="compact",
    )
    body += "<h3>Failure behaviour to demonstrate</h3>" + table(
        ["Injected failure", "Expected behaviour"],
        [
            ["Broker unavailable 2 minutes", "Ingest worker reconnects with backoff; simulator buffers and replays with original event times; no gaps after recovery"],
            ["Duplicate delivery (QoS 1)", "Duplicates counted in a metric, not stored; downstream alerts unchanged"],
            ["ML service down", "Threshold baseline continues to raise anomaly alerts flagged \"baseline mode\"; risk scores show \"unavailable\", never stale values"],
            ["Redis down", "Dedup falls back to database unique constraint; freshness shows unknown; API remains available"],
            ["Database failover / restart", "Outbox guarantees events are published after recovery; UI shows degraded banner"],
        ],
        widths=["24%", "76%"],
        cls="compact",
    )
    body += ('<p class="small">The full-rate scenario exceeds Azure IoT Hub Free Edition limits (8,000 messages/day), which is why the design uses a self-hosted MQTT broker plus Event Hubs '
             "or a throttled profile for cloud demos. [R11]</p>")
    return page(n, "25", "Reliability and non-functional design", "Set measurable engineering targets and test failure behaviour", body)


def p27(n: int) -> str:
    rows = [
        ("Container Apps (6 apps)", [(0.09, TEAL), (0.51, ORANGE)], "0–60"),
        ("PostgreSQL B1ms + 32 GB", [(0.31, TEAL), (0.39, ORANGE)], "30–70"),
        ("Event Hubs Standard 1 TU", [(0.05, TEAL), (0.40, ORANGE)], "0–45"),
        ("Redis (container or C0)", [(0.09, TEAL), (0.51, ORANGE)], "0–60"),
        ("Storage, Key Vault, logs", [(0.10, TEAL), (0.25, ORANGE)], "10–35"),
        ("LLM API (stretch)", [(0.0, TEAL), (0.20, ORANGE)], "0–20"),
    ]
    body = panel(
        bars(rows, label_width=165)
        + '<p class="note" style="margin:8px 0 0">Teal = free-tier / idle portion, orange = active-demo portion. Monthly total AED 40–290 depending on how many hours the demo stack runs; '
        "stopping Postgres and scaling apps to zero at night keeps the idle floor near AED 40.</p>",
        title="Hosted demo footprint per month (AED, planning only)",
    )
    body += table(
        ["Cost component", "Estimated AED", "Assumption"],
        [
            ["Weeks 1–8 (local development)", "0–40", "Docker Compose only; optional domain registration"],
            ["Weeks 9–12 (cloud build and demo)", "150–450", "Roughly 1.5 months of the hosted footprint while Terraform and tests are developed"],
            ["3 months interview hosting", "120–600", "AED 40–200/month with night-time shutdown; higher if left running"],
            ["Occasional compute / overage", "50–150", "Load tests, training bursts, mistakes"],
            ["Tooling", "0", "GitHub Free (Actions, Dependabot, CodeQL for public repos), VS Code, Docker Desktop personal, .NET 10 SDK, Node 22 LTS, uv, Terraform CLI, MLflow are free for this use"],
            ["Expected project envelope", "<strong>700–1,600</strong>", "Set aside AED 1,800 for contingency; excludes labour, devices and professional audits"],
        ],
        widths=["28%", "16%", "56%"],
        cls="compact",
    )
    body += callout(
        "Budget guidance",
        "Planning budget AED 700–1,600 out of pocket for the 12-week build plus a quarter of interview hosting. Not an Azure quote and not a production run-rate. Vendor pricing: "
        "Container Apps consumption free grant per subscription; Flexible Server burstable compute can be stopped for up to 7 days at a time; Event Hubs Standard bills per throughput unit hour; "
        "Static Web Apps Free. Verify region, currency, tax and account eligibility in the pricing calculator. [R10–R13]",
    )
    return page(n, "26", "Hosting costs and self-build investment", "An explicit low-usage envelope in AED, by phase, rather than a production Azure quote", body)


def p28(n: int) -> str:
    body = (
        '<p class="lead">The business case should be expressed as a testable scenario, not as a claim that a machine-learning algorithm automatically saves money. A practical formula '
        'estimates avoided downtime value minus added maintenance work, implementation and operating costs. The application ships an "ROI scenario" screen that lets a plant enter its own numbers.</p>'
    )
    body += panel(
        bars([("Conservative", [(0.32, ORANGE)], "8%"), ("Base scenario", [(0.60, TEAL)], "15%"), ("Optimistic", [(1.0, ORANGE)], "25%")])
        + '<p class="note" style="margin:8px 0 0">Illustrative assumed reduction of baseline unplanned downtime (not an achieved result).</p>'
    )
    body += "<h3>Worked fictional example</h3>" + table(
        ["Assumption", "Value", "Notes"],
        [
            ["Baseline unplanned downtime", "100 hours/year", "Illustrative single plant / line scope"],
            ["Value of downtime", "AED 5,000/hour", "Replace with verified finance-approved cost"],
            ["Assumed avoidance scenario", "15 %", "Unproven planning assumption"],
            ["Gross theoretical avoided cost", "AED 75,000/year", "100 × 5,000 × 15 %"],
            ["Extra maintenance + ops cost", "AED 25,000/year", "Illustrative increment from intervention"],
            ["Net hypothetical benefit", "AED 50,000/year", "Before platform implementation, support and risk costs"],
        ],
        widths=["28%", "20%", "52%"],
        cls="compact",
    )
    body += "<h3>Portfolio-specific return</h3>" + table(
        ["Investment", "Return the developer can expect"],
        [
            ["240 hours + AED 700–1,600", "One flagship project covering eleven skill areas (p.5), a five-minute demo, a public architecture dossier, two model cards and a learning track of thirteen notes"],
            ["Reusable assets", "Terraform modules, Compose stack, outbox and idempotency libraries, authZ test harness, simulator framework reusable in projects 03–06"],
            ["Interview material", "Concrete stories on service boundaries, event reliability, ML evaluation honesty, cloud cost control, runtime lifecycle decisions and OT security"],
        ],
        widths=["28%", "72%"],
        cls="compact",
    )
    body += callout(
        "Evidence requirement",
        "A real ROI claim requires observed pre/post plant data, a valid comparison period, attribution of interventions, operator acceptance, maintenance cost changes and controlled confounders. "
        "The fictional scenario must never be presented as achieved savings.",
    )
    return page(n, "27", "Economic case and sensitivity analysis", "Illustrative downtime model, transparent about every assumption", body)


def p29(n: int) -> str:
    body = table(
        ["Test layer", "Coverage and acceptance", "Tooling"],
        [
            ["Unit / domain", "Work-order state machine, permissions, inventory invariants, alert dedup, feature calculations, claims parsing", "xUnit v3 on Microsoft Testing Platform + AwesomeAssertions, pytest, Vitest"],
            ["Architecture", "Module boundaries, no cross-schema access, no EF types outside persistence, no MQTT client in the platform, single public type per module", "NetArchTest (14 fitness functions from week 1)"],
            ["Contract / integration", "OpenAPI compatibility, JSON Schema for events, outbox publish, Timescale queries, host smoke tests in-process", "WebApplicationFactory, Testcontainers (Postgres, Redpanda, Redis)"],
            ["Workflow E2E", "Create asset → inject fault → alert → request → approve → execute → close; runs against Compose", "Playwright"],
            ["ML / data tests", "Schema and leakage checks, baseline comparison, deterministic training, drift alert fires on shifted data, plant.yaml validated by pydantic", "pytest, MLflow"],
            ["Resilience", "Duplicate events, broker outage, delayed telemetry, ML service down, Redis down", "Compose fault scripts"],
            ["Security", "Tenant isolation, IDOR, auth abuse, secret scan, dependency scan, SAST, container scan, image signing", "AuthZ test suite, gitleaks, Dependabot, CodeQL, Trivy, cosign"],
            ["Hygiene", "Conventional Commits with the agreed scope list; formatting (Prettier, ruff format, .editorconfig)", "commitlint, ruff, Prettier, ESLint"],
            ["Accessibility / UX", "Keyboard tab order, labels, 360 px technician flow", "axe in Playwright"],
            ["Deployment", "Terraform plan review, automated migrations, release tags, rollback and restore rehearsal", "GitHub Actions, Terraform"],
        ],
        widths=["17%", "53%", "30%"],
        cls="compact",
    )
    body += "<h3>Build pipeline: GitHub Actions</h3>" + bullets(
        [
            "<strong>Pull request and push to main:</strong> secret scan, commit-message lint, .NET build with warnings as errors and tests, Angular lint, format, tests and production build with bundle budgets, Python ruff, mypy and pytest, Compose and plant-spec validation; "
            "container builds for all five images run only after the test jobs pass; under 10 minutes (first run: 2.5 minutes).",
            "<strong>Merge to main (from week 9):</strong> sign Docker images, push to GitHub Container Registry, Terraform plan, deploy to the Azure demo environment, smoke test.",
            "<strong>Candidate ML model:</strong> validate holdout results, compare with champion, write model card, require manual approval before alias promotion.",
            "<strong>Nightly and weekly:</strong> replay-based resilience test, grouped Dependabot updates per ecosystem (NuGet, npm, uv, Docker, Compose, Actions), cost report comment on the tracking issue.",
        ],
        tight=True,
    )
    body += callout(
        "Pass / fail",
        "Do not release if a role can access another tenant, if data lineage is unknown, if a model breaks its baseline gate, or if a work order can be duplicated by event replay.",
    )
    return page(n, "28", "Quality assurance and continuous delivery", "Test the product, the event pipeline and the models independently, within 20 budgeted hours", body)


def p30(n: int) -> str:
    def gantt_row(label: str, start: int, end: int, color: str) -> str:
        left = (start - 1) / 12 * 100
        width = (end - start + 1) / 12 * 100
        return f'<div class="gl">{label}</div><div class="gt"><div class="gb" style="left:{left:.2f}%;width:{width:.2f}%;background:{color}"></div></div>'

    gantt = '<div class="gantt"><div></div>' + "".join(f'<div class="gh">{i:02d}</div>' for i in range(1, 13))
    gantt += gantt_row("Foundation &amp; platform", 1, 2, TEAL) + gantt_row("Business modules", 2, 4, TEAL) + gantt_row("Simulator &amp; telemetry", 5, 6, TEAL)
    gantt += gantt_row("AI models &amp; serving", 7, 8, TEAL) + gantt_row("Azure, IaC &amp; security", 9, 10, NAVY) + gantt_row("QA, docs, demo", 11, 12, NAVY) + "</div>"
    body = gantt
    body += table(
        ["Week", "Hours", "Deliverable (observable at the end of the week)", "Gate / status"],
        [
            ["1", "20", "Repository, Compose stack (Postgres+Timescale, Redis, Mosquitto, Redpanda, Keycloak, MLflow), .NET 10 modular solution skeleton with architecture tests, Angular 22 shell with OIDC login, Python skeletons, CI with DevSecOps baseline", "<strong style='color:%s'>Delivered 01 Nov 2026, CI green</strong>" % TEAL_DARK],
            ["2", "20", "Tenant + Assets module: plant/line/asset CRUD, sensor mapping, audit module, OpenAPI, Angular asset explorer, row-level security", "G1 Foundation"],
            ["3", "20", "Maintenance module: requests, work-order state machine, approvals, technician page, findings with upload", ""],
            ["4", "20", "Inventory module, notifications, outbox + Kafka publisher, idempotent consumer library, Maintenance Hub UI", "G2 Useful operations"],
            ["5", "20", "Simulator v1 (plant YAML, nominal signals, MQTT), ingest worker (validate, Timescale, Redis, TelemetryReceived), live chart", ""],
            ["6", "20", "Fault library, degradation models, control API, replay, data-quality flags, continuous aggregates, freshness UI", "G3 Telemetry"],
            ["7", "20", "Model A: AI4I pipeline, baselines, LightGBM, PR-AUC/calibration, SHAP, MLflow registry, FastAPI scoring, model card", ""],
            ["8", "20", "Model B: window features, Isolation Forest, persistence/hysteresis, AlertRaised, Risk Center UI, replay evaluation report", "G4 AI verified"],
            ["9", "20", "Terraform: resource group, Container Apps env, Flexible Server, Event Hubs, Blob, Key Vault, Static Web App, budgets; deploy pipeline with OIDC federation", ""],
            ["10", "20", "Security hardening: authZ and tenant tests, rate limits, CodeQL, Trivy, SBOM, cosign, OTel to Azure Monitor; runtime lifecycle review; AI assistant from buffer if on time", "G5 Secure cloud"],
            ["11", "20", "Resilience tests, E2E workflow test, load test, accessibility pass, bug fixing, restore rehearsal", ""],
            ["12", "20", "Demo script and video, README, architecture dossier (C4), model cards, screenshots, release tag v1.0", "G6 Portfolio release"],
        ],
        widths=["8%", "8%", "62%", "22%"],
        cls="compact",
    )
    body += (
        '<p class="small"><strong>Gate definitions of done:</strong> G1 — <code>docker compose up</code> gives login, asset list and green CI. G2 — a planner and a technician complete a work order end to end with audit. '
        "G3 — injected fault visible on live chart within 15 s; duplicates not stored. G4 — both model cards committed; replay report shows baseline beaten. G5 — Terraform apply from clean subscription; "
        "authZ suite green; budget alert configured. G6 — public repository, hosted demo URL, five-minute video. "
        "<strong>Learning track:</strong> every week closes with a note in <code>docs/learning</code> (concepts met, decisions and alternatives, pitfalls, interview answers, check-yourself questions); "
        "a skills coverage matrix states the depth reached per area.</p>"
    )
    return page(n, "29", "12-week delivery roadmap", "Twenty hours a week, one deliverable a week, six gates with a definition of done", body)


def p31(n: int) -> str:
    body = table(
        ["Risk", "Likelihood / impact", "Practical mitigation"],
        [
            ["Scope growth / abandonment", "High / critical", "Vertical slice by week 4; pre-agreed cut list (p.4); weekly deliverable; gates with definition of done"],
            ["Synthetic data overclaim", "High / high", "State source type on UI, model cards and README; no field-accuracy language; synthetic-data header on every API response"],
            ["Weak model generalisation", "High / high", "Scenario-level splits, baseline gates, calibration, limitations section"],
            ["Python / ML learning curve", "Medium / high", "Two simple, well-documented algorithms; notebooks converted to scripts early; 44 budgeted hours"],
            ["Runtime leaves support mid-build", "Medium / medium", "LTS-only policy (ADR-010); vendor lifecycle checked before scaffolding (.NET 8/9 and Angular 20 all end support in November 2026); review before cloud deploy"],
            ["Local stack too heavy", "Medium / medium", "Compose profiles; Redpanda instead of Kafka+ZooKeeper; single Postgres; observability optional locally"],
            ["False-positive alert fatigue", "Medium / high", "Persistence, hysteresis, cooldown, load-aware baselines, human confirmation"],
            ["Cloud bill surprises", "Medium / medium", "Budgets, scale-to-zero, nightly stop workflow, cost dashboard in demo"],
            ["Event duplicate / ordering", "Medium / medium", "Outbox, idempotency, event-time windows, dead-letter topics"],
            ["OT access / write-back", "Low / critical", "Simulator first; read-only connector; segmented network; no PLC write path exists in code; architecture test"],
            ["Terraform / Azure friction", "Medium / medium", "Start from official Container Apps and Flexible Server examples; 32 budgeted hours; manual fallback documented"],
        ],
        widths=["22%", "16%", "62%"],
        cls="compact",
    )
    body += "<h3>Architecture decision records (ADRs), published under <code>docs/adr</code></h3>" + bullets(
        [
            "<strong>ADR-001:</strong> .NET for workflow services, Python for model training and inference.",
            "<strong>ADR-002:</strong> Modular monolith plus three single-purpose services on Container Apps; Kubernetes as documented migration path.",
            "<strong>ADR-003:</strong> Event outbox and idempotent consumers for maintenance alerts over a Kafka-API bus (Redpanda local, Event Hubs cloud).",
            "<strong>ADR-004:</strong> Hybrid OT boundary with read-only edge ingestion; simulator as the Release 1 plant.",
            "<strong>ADR-005:</strong> MLflow registry, quality gate and human approval before model promotion.",
            "<strong>ADR-006:</strong> Angular frontend; frontend is not a target skill; framework-neutral API.",
            "<strong>ADR-007:</strong> Single PostgreSQL with module schemas and TimescaleDB; Redis for ephemeral state; Blob for datasets.",
            "<strong>ADR-008:</strong> Keycloak OIDC with tenant claim; row-level security as defence in depth.",
            "<strong>ADR-009:</strong> Bounded GenAI assistant: read-only, cited, budgeted, optional.",
            "<strong>ADR-010 (new in v2.1):</strong> LTS-only runtime policy; .NET 10 replaces .NET 8 (both .NET 8 and 9 reach end of support on 10 November 2026; .NET 10 is supported to November 2028).",
            "<strong>ADR-011 (new in v2.1):</strong> Angular 22 replaces Angular 20 (Angular 20 LTS ends 28 November 2026; Angular 22 LTS runs to June 2028); Node 22 LTS.",
        ],
        tight=True,
    )
    return page(n, "30", "Risks, mitigations and architectural decisions", "Make the demo defensible under critical engineering review", body)


def p32(n: int) -> str:
    body = table(
        ["Portfolio deliverable", "Reviewable evidence"],
        [
            ["Working application", "Hosted URL with seeded demo identities per role, and local one-command Docker startup"],
            ["Architecture dossier", "C4 context and container views, event catalogue, threat model, deployment diagram, eleven ADRs"],
            ["Plant specification", "Sensor catalogue, fault library, simulator configuration and scenario presets (<code>docs/plant/plant.yaml</code>, test-validated)"],
            ["ML documentation", "Datasets and attribution, experiments, two model cards, confusion matrix, PR curve, replay evaluation report"],
            ["Business workflows", "User stories, screenshots, screen recording, seed data, demo script"],
            ["Engineering quality", "CI badge, test summary, trace screenshot, load results, security scan results, backup runbook"],
            ["Cloud operations", "Terraform modules, cost dashboard screenshot, budget alert, stop/start workflow"],
            ["Learning track", "Thirteen weekly notes (concepts, decisions, pitfalls, interview answers, self-check questions) and a skills coverage matrix with honest depth levels"],
            ["Measured vs illustrative", "Clear labels separating benchmark results, simulations and product targets"],
        ],
        widths=["24%", "76%"],
        cls="compact",
    )
    body += "<h3>Five-minute technical demonstration</h3>" + bullets(
        [
            "<strong>00:00–00:40</strong> Plant dashboard, machine tree, condition summaries, data freshness.",
            "<strong>00:40–01:35</strong> Inject <code>bearing-wear-mtr042</code> via the simulator control API; trace the MQTT event through the ingest worker into TimescaleDB.",
            "<strong>01:35–02:30</strong> Watch the anomaly alert appear; review model version, explanation and evidence window.",
            "<strong>02:30–03:30</strong> Planner creates and approves a work order with a reserved bearing kit; technician completes the checklist on a phone-width view.",
            "<strong>03:30–04:20</strong> Show closed-loop audit history, the label linked to the alert and the simulator health reset.",
            "<strong>04:20–05:00</strong> Show the reproducible model evaluation, the distributed trace in Azure Monitor and the cost dashboard; replay a duplicate event and show nothing duplicates.",
        ],
        tight=True,
    )
    body += "<h3>Interview talking points</h3>" + bullets(
        [
            "How a noisy sensor event becomes a trusted, human-reviewed maintenance action.",
            "Why four deployables and not six, and what would trigger extracting a module.",
            "How the system recovers from a broken broker or a duplicate message.",
            "Why PR-AUC and calibration were chosen over accuracy, and what the models cannot claim.",
            "Why the runtime versions changed between v2.0 and v2.1, and how support windows are treated as a security requirement.",
            "How the monthly Azure bill is kept under control and what the enterprise version would add.",
        ],
        tight=True,
    )
    body += callout(
        "Strong interview story",
        "Explain the full path from a simulated bearing fault to a closed work order, with every hop observable in one trace, and show the exact point where a human decides.",
    )
    return page(n, "31", "Portfolio proof package and interview walkthrough", "Evidence that the product is engineered, reproducible and usable", body)


def p33(n: int) -> str:
    refs = [
        ("R1", "UCI AI4I 2020 predictive-maintenance dataset", "https://archive.ics.uci.edu/dataset/601/ai4i+2020+predictive+maintenance+dataset"),
        ("R2", "NASA PCoE repository, C-MAPSS run-to-failure simulations", "https://www.nasa.gov/intelligent-systems-division/discovery-and-systems-health/pcoe/pcoe-data-set-repository/"),
        ("R3", "OPC Foundation, OPC UA specification", "https://reference.opcfoundation.org/"),
        ("R4", "Azure Architecture Center, IoT architecture guide", "https://learn.microsoft.com/en-us/azure/architecture/guide/iot/iot-get-started"),
        ("R5", "MQTT v5 specification (OASIS)", "https://docs.oasis-open.org/mqtt/mqtt/v5.0/mqtt-v5.0.html"),
        ("R6", "Microsoft MLOps v2 architecture", "https://learn.microsoft.com/en-us/azure/architecture/ai-ml/guide/machine-learning-operations-v2"),
        ("R7", "NIST SP 800-82 Rev. 3, Operational Technology Security", "https://csrc.nist.gov/pubs/sp/800/82/r3/final"),
        ("R8", "ISO 55000:2024 asset management principles", "https://www.iso.org/standard/83053.html"),
        ("R9", "ISA-95 enterprise-control system integration", "https://www.isa.org/standards-and-publications/isa-standards/isa-95-standard"),
        ("R10", "Azure Container Apps pricing and free allowances", "https://azure.microsoft.com/en-us/pricing/details/container-apps/"),
        ("R11", "Azure IoT Hub pricing and Free tier limits", "https://azure.microsoft.com/en-us/pricing/details/iot-hub/"),
        ("R12", "Azure Database for PostgreSQL Flexible Server pricing, stop/start and extensions", "https://learn.microsoft.com/en-us/azure/postgresql/flexible-server/"),
        ("R13", "Azure pricing calculator (region-specific estimates)", "https://azure.microsoft.com/en-us/pricing/calculator/"),
        ("R14", "ISO 20816 series, mechanical vibration evaluation of machines", "https://www.iso.org/standard/63180.html"),
        ("R15", "Azure Event Hubs for Apache Kafka", "https://learn.microsoft.com/en-us/azure/event-hubs/azure-event-hubs-kafka-overview"),
        ("R16", "TimescaleDB documentation (hypertables, compression, continuous aggregates)", "https://docs.timescale.com/"),
        ("R17", "Redpanda documentation (Kafka-compatible streaming)", "https://docs.redpanda.com/"),
        ("R18", "Eclipse Mosquitto MQTT broker", "https://mosquitto.org/documentation/"),
        ("R19", "Keycloak server administration (OIDC, realms, roles)", "https://www.keycloak.org/documentation"),
        ("R20", "MLflow model registry and tracking", "https://mlflow.org/docs/latest/"),
        ("R21", "OpenTelemetry specification and .NET / Python SDKs", "https://opentelemetry.io/docs/"),
        ("R22", "Terraform AzureRM provider", "https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs"),
        ("R23", ".NET support policy and release lifecycle (.NET 8/9 end of support, .NET 10 LTS)", "https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core"),
        ("R24", "Angular versioning and release schedule (support windows per major)", "https://angular.dev/reference/releases"),
        ("R25", "Microsoft Testing Platform with dotnet test (.NET 10 SDK)", "https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-integration-dotnet-test"),
    ]
    body = table(["Ref", "Source", "URL"], [[r, s, f'<span style="word-break:break-all">{u}</span>'] for r, s, u in refs], widths=["7%", "36%", "57%"], cls="xxs")
    body += ('<p class="small"><strong>Citation and scope note:</strong> technical statements are sourced to primary references where possible. Graphics, the simulated plant and '
             "workflows are conceptual; financial, risk and UI figures are illustrative. Pricing, free-tier limits and support dates change; verify before relying on them.</p>")
    return page(n, "32", "Authoritative references and verification", "Sources for dataset properties, industrial standards, open-source components, cloud capabilities and runtime lifecycles", body)


def p34(n: int) -> str:
    body = table(
        ["Term", "Working meaning"],
        [
            ["OT / IT", "Operational technology controlling physical processes / information technology"],
            ["OPC UA / MQTT", "Industrial interoperability protocol / lightweight publish-subscribe messaging protocol"],
            ["Hypertable / continuous aggregate", "TimescaleDB partitioned time-series table / automatically maintained roll-up view"],
            ["Outbox / DLQ", "Reliable publication pattern / dead-letter quarantine of failed events"],
            ["PR-AUC / Brier", "Precision-recall area under curve for imbalanced events / calibration error of probabilities"],
            ["Isolation Forest", "Tree-based anomaly detector that isolates unusual points with few splits"],
            ["MTBF / MTTR", "Mean time between failures / mean time to repair; define inclusion policy"],
            ["Model card / MLOps", "Documented model purpose, data, metrics, limitations / reproducible model lifecycle"],
            ["LTS / STS", "Long-term support release (36 months for .NET, 18 months for Angular after active support) / standard-term support release; this project uses LTS only"],
            ["Fitness function", "An automated test that asserts an architectural property, such as module isolation, on every build"],
        ],
        widths=["26%", "74%"],
        cls="xs",
    )
    body += "<h3>Definition of done (Release 1)</h3>" + bullets(
        [
            "One-command local stack startup and repeatable seeding of plant, users and sample history.",
            "All five personas can finish their core workflow without database manipulation.",
            "A fault injected in the simulator appears as a reviewed alert and a closed work order with an audit trail.",
            "Every ML experiment has dataset hash, code version, baseline and an independent test report; both model cards are published.",
            "Event replay and duplicate delivery do not duplicate orders or alerts; broker and ML outages degrade gracefully.",
            "Access to assets and maintenance records is constrained by role and tenant, proven by tests.",
            "Azure deployment is reproducible from Terraform, budget-alerted and safe to shut down overnight.",
            "Architecture, security, model limitations, dashboard shots, demo video and the learning track are published; no language asserts real factory savings or certified industrial integration.",
        ],
        tight=True,
    )
    body += "<h3>Release 2 backlog (not scheduled)</h3>" + table(
        ["Item", "Why later"],
        [
            ["Remaining-useful-life model (C-MAPSS)", "Cross-domain research experiment; needs its own evaluation story"],
            ["OPC UA edge adapter with store-and-forward", "Requires a real or emulated OPC UA server; contract already fixed"],
            ["Technician PWA offline sync and signatures", "Significant frontend effort outside target skills"],
            ["Production-window negotiation, scheduling optimisation", "Business logic beyond the vertical slice"],
            ["Arabic localisation, RTL", "Structure is i18n-ready; translation is content work"],
            ["Cosmos DB hot-path document store, SCIM, ABAC, Azure ML jobs", "Additional cloud breadth once the core is proven; no access pattern in Release 1 needs a document database"],
        ],
        widths=["34%", "66%"],
        cls="xs",
    )
    body += ('<p class="small"><strong>Implementation status:</strong> the granular backlog exists as <code>docs/ROADMAP.md</code> with checkboxes per task; week 1 is complete and week 2 (gate G1) is next.</p>')
    return page(n, "33", "Appendix — glossary, definition of done, Release 2 backlog", "A structured definition of done for a strong AI engineering portfolio", body)


def p35(n: int) -> str:
    body = callout(
        "Why a v2.1 so soon",
        "Version 2.0 was accepted on 01 November 2026 and implementation started the same day. Checking vendor lifecycle pages before scaffolding showed that two of the named "
        "runtimes leave support during the build. The corrections below keep the design intact and change only the versions, the licence and the engineering practices that "
        "week 1 made concrete. Page numbers of v2.0 are preserved; pages 35–36 are new.",
    )
    body += "<h3>What changed from v2.0</h3>" + table(
        ["Area", "v2.0", "v2.1", "Where"],
        [
            ["Backend runtime", ".NET 8 (ASP.NET Core 8)", ".NET 10 LTS (ASP.NET Core 10, EF Core 10, C# 14); SDK pinned in <code>global.json</code>; LTS-only runtime policy", "ADR-010; pp.2, 15"],
            ["Frontend", "Angular 20", "Angular 22, Material 22, zoneless, Vitest; Node 22 LTS", "ADR-011; pp.11, 15"],
            ["Licence", "Not stated", "MIT for the repository; CC BY 4.0 attribution for the AI4I dataset", "p.2, <code>LICENSE</code>"],
            ["Plant metrics", "\"31 metrics\"", "32 metrics (the sensor table always listed 32; PMP-211 has five); throttled profile defined as one reading per metric every 30 s", "p.8, <code>docs/plant/plant.yaml</code>"],
            ["Test stack", "xUnit, NetArchTest, pytest, Playwright", "xUnit v3 on Microsoft Testing Platform (required by the .NET 10 SDK), AwesomeAssertions (Apache-2.0 fork of FluentAssertions, whose v8 licence is not permissive), Vitest for Angular", "p.29"],
            ["DevSecOps baseline", "Planned for week 10", "From week 1: gitleaks, commitlint (Conventional Commits with scope list), grouped weekly Dependabot across six ecosystems, container builds gated on tests; CodeQL, Trivy, SBOM and cosign remain week 10", "pp.24, 29"],
            ["Identity bootstrap", "Realm export", "Realm export carries roles, clients (PKCE SPA, bearer-only API, dev-only password-grant test client), tenant claim mapper and user-profile schema but no users or credentials; a one-shot seed job creates personas from <code>.env</code>", "pp.6, 24"],
            ["Synthetic-data labelling", "UI, charts, model cards", "Additionally an <code>X-SmartFactory-Data: synthetic; advisory-only; no-plc-control</code> header on every HTTP response of every service", "pp.11, 31"],
            ["Repository layout", "src/platform-api, ingest-worker, ml-service, simulator, web, infra, contracts, docs", "Adds <code>src/building-blocks</code> (shared .NET library), <code>infra/compose</code> (stack configuration), <code>docs/learning</code>, one <code>.slnx</code> solution and central package management at the root", "p.15"],
            ["Learning track", "Not part of the plan", "Weekly learning note (concepts before the week, decisions and alternatives after, interview answers, self-check) and a skills coverage matrix with depth levels Core / Applied / Documented / Not covered", "pp.4, 30, 32"],
            ["ADR set", "ADR-001 … ADR-009", "ADR-001 … ADR-011", "p.31"],
            ["Week 1 status", "Planned", "Delivered: Compose stack with nine health-gated services, .NET 10 solution with 38 passing tests, Angular 22 shell with OIDC login, Python skeletons with 8 tests, CI run #1 green in 2.5 minutes", "p.30"],
        ],
        widths=["17%", "19%", "48%", "16%"],
        cls="xxs",
    )
    return page(n, "34", "Revision history — what changed in v2.1", "Corrections made when implementation started, with the reason for each", body)


def p36(n: int) -> str:
    body = ""
    body += "<h3 style='margin-top:0'>Unchanged on purpose</h3>" + bullets(
        [
            "Scope, personas, plant specification, fault library, event catalogue, data model, model choices, Azure topology, hour budget, gates and cost envelope.",
            "Python 3.12 and PostgreSQL 16: both well inside their support windows; reviewed again at gate G5 under ADR-010.",
            "The content rules: advisory only, synthetic data labelled everywhere, no performance claims, no secrets in the repository.",
        ],
        tight=True,
    )
    body += "<h3>Revision history</h3>" + table(
        ["Version", "Date", "Summary"],
        [
            ["1.0", "01 November 2026", "Initial blueprint: six microservices, three models, React PWA; reviewed as a team-sized plan"],
            ["2.0", "01 November 2026", "Right-sized to 240 hours: four deployables, two models, Angular, full simulated plant, weekly plan and gates"],
            ["2.1", "01 November 2026", "Runtime lifecycle corrections (.NET 10, Angular 22), MIT licence, week-1 engineering practices, learning track, metric count"],
        ],
        widths=["12%", "22%", "66%"],
        cls="compact",
    )
    body += "<h3>Week 1 delivered (01 November 2026)</h3>" + table(
        ["Deliverable", "Evidence in the repository", "Verified by"],
        [
            ["Local stack", "<code>docker-compose.yml</code>, <code>infra/compose/</code>: nine health-gated services, realm import, dev-user seed job, <code>.env.example</code>", "<code>docker compose config</code> in CI; first real start-up is the owner's laptop check"],
            [".NET 10 solution", "<code>SmartFactory.slnx</code>, BuildingBlocks (auth, tenancy, telemetry, synthetic header), four modules, host with /health, /api/me, OpenAPI + Scalar, ingest worker", "38 tests: architecture fitness functions, host smoke tests, claims parsing, options validation"],
            ["Angular 22 shell", "<code>src/web</code>: Material shell, synthetic banner, OIDC PKCE login, home page calling /api/me, Transloco, ESLint, Prettier", "4 Vitest tests, lint, production build within bundle budget"],
            ["Python skeletons", "<code>src/ml-service</code>, <code>src/simulator</code>: uv projects, FastAPI health, pydantic model of plant.yaml", "8 pytest tests, ruff, strict mypy"],
            ["CI / DevSecOps", "<code>.github/workflows/ci.yml</code>, <code>dependabot.yml</code>, <code>commitlint.config.mjs</code>, <code>.gitleaks.toml</code>", "Run #1 green: 12 jobs including five container builds, 2.5 minutes"],
            ["Documentation", "README, CLAUDE.md conventions, ADR-001…011, <code>docs/ROADMAP.md</code>, <code>docs/plant/plant.yaml</code>, learning notes 01–02 and skills matrix", "Link check; plant.yaml validated by simulator tests"],
        ],
        widths=["16%", "52%", "32%"],
        cls="xs",
    )
    return page(n, "35", "Revision history and implementation status", "What stays as designed, the version log, and the evidence week 1 left behind", body)


PAGES = [p25, p26, p27, p28, p29, p30, p31, p32, p33, p34, p35, p36]
