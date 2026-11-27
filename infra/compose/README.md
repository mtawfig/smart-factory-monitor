# Local stack (Docker Compose)

Configuration files for the services started by the root `docker-compose.yml`. Everything here is for a
developer laptop. Nothing is production configuration, and all data produced is synthetic.

## Start

```bash
cp .env.example .env        # then replace every "replace-me-local-only"
docker compose up -d
docker compose ps           # wait until every service is "healthy"; keycloak-seed exits 0 when done
scripts/g1-check.sh         # Gate G1: login and asset list, end to end (bash + curl + python3)
```

After changing .NET or Angular code, rebuild the two app images: `docker compose up -d --build platform-api web`.
For day-to-day coding you can instead stop them (`docker compose stop platform-api web`) and use `dotnet run` and
`npm start`, which use the same ports.

Stop with `docker compose down`. Add `-v` to also delete the volumes (databases, topics, artefacts).

## Services and ports

| Service | Image | Host port(s) | Purpose | Open in browser |
|---|---|---|---|---|
| postgres | timescale/timescaledb (PostgreSQL 16) | 5432 | Platform database (`smartfactory`), plus `keycloak` and `mlflow` databases | — |
| redis | redis 8 | 6379 | Last value per asset, dedup sets, rate limits (cache only) | — |
| mosquitto | eclipse-mosquitto 2 | 1883, 9001 (WebSockets) | MQTT broker: simulator → ingest worker | — |
| redpanda | redpandadata/redpanda | 19092 (Kafka), 18081 (schema registry), 18082 (HTTP proxy), 9644 (admin) | Kafka-API event bus | — |
| redpanda-console | redpandadata/console | 8080 | Browse topics, consumer groups, messages | http://localhost:8080 |
| keycloak | quay.io/keycloak/keycloak | 8180 (HTTP), 9000 (health/metrics) | OIDC identity provider, realm `smartfactory` | http://localhost:8180 |
| keycloak-seed | (same image, one-shot) | — | Creates the five persona users from `.env` password | — |
| mlflow | built from `mlflow/Dockerfile` | 5000 | Experiment tracking and model registry | http://localhost:5000 |
| platform-api | built from `src/platform-api/Dockerfile` | 5080 | Platform API; migrates and seeds `docs/plant/plant.yaml` on start; finding attachments on the `attachments` volume | http://localhost:5080/health/ready |
| web | built from `src/web/Dockerfile` (`compose` build) | 4200 | Angular app: sign in, asset explorer | http://localhost:4200 |

Inside the Compose network, services address each other by name: `postgres:5432`, `redis:6379`,
`mosquitto:1883`, `redpanda:9092`, `keycloak:8080`, `mlflow:5000`, `platform-api:8080`.

## Keycloak realm `smartfactory`

Imported from `keycloak/realm-smartfactory.json` on first start. It contains:

- **Realm roles** for the five personas: `reliability-manager`, `maintenance-planner`, `technician`,
  `ml-engineer`, `administrator`.
- **Client `smartfactory-web`**: public SPA client, Authorization Code + PKCE (S256), redirect
  `http://localhost:4200/*`.
- **Client `smartfactory-api`**: bearer-only resource server; tokens for the SPA carry it as audience.
- **Client `smartfactory-authz-tests`**: password grant, dev/CI only, used by the cross-tenant authorisation
  test suite to obtain tokens for users of different tenants.
- **Client scope `tenant`**: maps the user attribute `tenant_id` into the `tenant_id` token claim.
- **Standard scopes `basic`, `profile`, `email`, `roles`, `web-origins`**, defined in the export itself: a realm
  import that lists its own `clientScopes` gets *only* those, so without them tokens would have no `sub`, no
  `preferred_username` and no `realm_access.roles`. The SPA asks for `openid profile email` (no `offline_access`;
  ordinary refresh tokens are enough for a browser session).
- **User profile**: `tenant_id` is a required, admin-editable attribute matching `^tnt-[a-z0-9-]+$`.

The export contains **no users and no credentials**. `keycloak/seed-dev-users.sh` creates these users in
tenant `tnt-alsafa`, all with the password from `DEV_USER_PASSWORD`:

| Username | Role |
|---|---|
| `manager` | reliability-manager |
| `planner` | maintenance-planner |
| `technician` | technician |
| `mlengineer` | ml-engineer |
| `admin-sf` | administrator |

Keycloak admin console: http://localhost:8180 with the `KC_BOOTSTRAP_ADMIN_*` values from `.env`.

## Useful checks

```bash
# PostgreSQL: Timescale extension and module schemas present
docker compose exec postgres psql -U "$POSTGRES_USER" -d smartfactory -c "\dx" -c "\dn"

# Redpanda: cluster health and topics
docker compose exec redpanda rpk cluster health
docker compose exec redpanda rpk topic list

# MQTT: watch everything the simulator publishes (Week 5)
docker compose exec mosquitto mosquitto_sub -t 'plant/#' -v

# OIDC discovery document the SPA and API will use
curl -s http://localhost:8180/realms/smartfactory/.well-known/openid-configuration | head -c 400
```

## Why these choices (short version; long version in `docs/learning/02-local-platform.md`)

- **Exact image tags**, not `latest`: reproducible stacks; Dependabot proposes bumps.
- **Health checks everywhere** and `depends_on: condition: service_healthy`: Keycloak and MLflow only start
  once PostgreSQL accepts connections. Same idea as readiness probes in Kubernetes.
- **No credentials in files**: everything sensitive comes from `.env`, which is git-ignored. The realm
  import deliberately has no users.
- **Redis without persistence**: it is a cache (ADR-007); losing it must be harmless, so we make losing it
  normal.
- **One PostgreSQL for three products**: cheap locally and in Azure; separate databases keep Keycloak and
  MLflow out of the platform schemas.
