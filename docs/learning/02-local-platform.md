# 02 — Local platform engineering

**Week 1.** The week produced no business feature and that was the point: a platform you can start with one
command, four service skeletons that build and test, a login that works, and a CI pipeline that refuses bad
commits. This note explains the engineering behind each.

## Concepts you will meet

Docker Compose service graph · health checks and readiness · image pinning · OIDC Authorization Code + PKCE ·
JWT claims and role mapping · multi-stage container builds · central package management · fitness functions ·
CI as a contract · secret scanning · Dependabot · Conventional Commits

---

## 1. Compose is a dependency graph, not a list

`docker-compose.yml` declares nine services. The interesting part is not the services, it is the edges:

```
postgres ──healthy──▶ keycloak ──healthy──▶ keycloak-seed
   │                                      
   └──────healthy──▶ mlflow
redpanda ──healthy──▶ redpanda-console
```

`depends_on` with `condition: service_healthy` means "do not start me until that container's **health check**
passes", not merely "until it is running". Keycloak would crash-loop if PostgreSQL were still initialising.
Every service therefore has a health check that exercises the real protocol: `pg_isready`, `redis-cli ping`,
an MQTT subscribe, `rpk cluster health`, an HTTP GET on Keycloak's management port, a Python one-liner
against MLflow.

**Why this matters beyond your laptop.** Kubernetes readiness probes and Azure Container Apps health probes
are the same idea with different syntax. Writing them locally first means the cloud configuration in Week 9
is a translation, not an invention.

**The Keycloak probe trick.** The Keycloak image ships without `curl`, so the probe opens a TCP socket with
bash's `/dev/tcp` and speaks HTTP by hand. Remember it: minimal images often lack the tools you reach for.

**Interview answer.** "I model start-up order with health-gated dependencies, and every health check speaks
the service's own protocol. The same probes move to Container Apps unchanged."

---

## 2. Pin everything, then let a robot propose upgrades

Every image is an exact tag (`timescale/timescaledb:2.31.0-pg16`, `quay.io/keycloak/keycloak:26.8.1`).
Every NuGet version lives once in `Directory.Packages.props`. Every Python dependency is resolved into
`uv.lock`. Every npm dependency is in `package-lock.json`.

Two reasons. **Reproducibility:** a `docker compose up` in March must produce the same stack as in
November, or bugs become unreproducible. **Security:** you cannot audit what you cannot name.

The cost of pinning is drift, so `.github/dependabot.yml` opens one grouped pull request per ecosystem per
week. You review a diff and a changelog instead of discovering a breaking change in production. This is the
first DevSecOps control in the repository and the cheapest.

**Interview answer.** "I pin every dependency and image to an exact version and let Dependabot propose
grouped weekly upgrades. Reproducible builds and an auditable bill of materials for the price of one PR
review a week."

---

## 3. One login, three parties: how the OIDC flow actually works

The Angular app never sees a password and never validates a token. Keycloak and the Platform API do.

```
Browser (SPA)                 Keycloak                     Platform API
   │ 1. redirect with code_challenge ─▶│                          │
   │ ◀── 2. login form, then code ─────│                          │
   │ 3. POST code + code_verifier ────▶│                          │
   │ ◀── 4. access + id + refresh ─────│                          │
   │ 5. GET /api/me  Authorization: Bearer <access> ─────────────▶│
   │                                   │ ◀─ 6. fetch JWKS (once) ─│
   │ ◀────────────────── 7. CallerInfo {tenant, roles} ──────────│
```

- **PKCE** (steps 1 and 3) replaces the client secret a browser cannot keep. The SPA proves it is the same
  party that started the flow. That is why `smartfactory-web` is a *public* client in the realm.
- **The API validates offline** (step 6): it downloads Keycloak's public keys once and checks the signature,
  issuer and audience of every token itself. No per-request call to Keycloak. The `aud` claim contains
  `smartfactory-api` only because the realm has an **audience mapper**; without it the API would reject the
  token, which is the correct default.
- **Roles arrive nested** as `realm_access.roles`. `KeycloakRoles.LiftRealmRoles` copies them into standard
  role claims so ASP.NET Core policies work. The SPA does the equivalent in `parsePersonaRoles`, but only for
  display; **authorisation decisions happen in the API**, never in the browser.
- **`tenant_id` is a user attribute** mapped into a claim. The API reads it through `ICurrentTenant` and will
  use it in Week 2 to set EF Core filters and the PostgreSQL row-level-security session variable (ADR-008).

Where to look: `infra/compose/keycloak/realm-smartfactory.json` (clients, mappers),
`src/building-blocks/.../Auth/PlatformAuthenticationExtensions.cs`, `src/web/src/app/core/auth/`.

**Interview answer.** "The SPA uses Authorization Code with PKCE as a public client. The API validates tokens
offline against the IdP's keys, checks audience and issuer, and derives tenant and roles from claims. The
browser never makes a security decision."

---

## 4. Secrets: the realm export has no users, on purpose

The easy path is to export a Keycloak realm with test users and passwords and commit it. That is a credential
in git history forever, and gitleaks would flag it. Instead:

- the realm export carries structure only (roles, clients, mappers, the user-profile schema);
- a one-shot container runs `kcadm.sh` after Keycloak is healthy and creates the five persona users with a
  password from `.env`, which is git-ignored;
- `.env.example` holds placeholders, and `.gitleaks.toml` allow-lists exactly those placeholder strings.

The same pattern will carry to Azure: Key Vault holds the values, the deployment references them.

---

## 5. Multi-stage Dockerfiles and why the build context is the repo root

The Platform API Dockerfile copies only `.csproj` and props files first, runs `dotnet restore`, then copies
the source. Docker caches each layer: a code change re-runs the build but not the restore, which is the slow
part. The ML service does the same with `uv sync --no-install-project` before copying `src/`.

The .NET build context is the **repository root** because the host references `src/building-blocks`, which
is outside `src/platform-api`. A Dockerfile can only see files inside its context. This is a common
stumbling block in monorepos and the reason CI passes `context: .` for those images.

Runtime images are the smallest that work (`aspnet` for the API, `runtime` for the worker, `python:slim`
for Python, `nginx-unprivileged` for the SPA) and run as a non-root user.

---

## 6. .NET 10 specifics you met this week

- **Central Package Management** (`Directory.Packages.props`): versions in one file; projects reference
  packages by name only.
- **`Directory.Build.props`**: one place for `net10.0`, nullable, warnings-as-errors and the analyser level.
  Change the target framework once for twelve projects.
- **Microsoft Testing Platform**: the .NET 10 SDK runs test projects as executables. xunit.v3 supports it
  natively; the opt-in is the `test.runner` key in `global.json`. If you see "VSTest target is no longer
  supported", this is why.
- **`.slnx`**: the new XML solution format created by `dotnet new sln`. Human-readable, merge-friendly.
- **Licence check before adding a package:** FluentAssertions 8 moved to a commercial licence;
  AwesomeAssertions is its Apache-2.0 fork with the same API. CLAUDE.md requires this check.

---

## 7. Architecture tests are the cheapest insurance you will buy

`ModuleBoundaryTests` uses NetArchTest to assert, on every build, that no module references another module
or the host, that BuildingBlocks is a leaf, that each module's only public type is its registration class,
and that nothing in the platform references an MQTT client. Fourteen tests, a few milliseconds.

Without them the modular monolith erodes one convenient `using` at a time. With them, a boundary violation is
a red build with the offending type named. Neal Ford calls these **fitness functions**: automated checks that
the architecture still has the property you designed for.

---

## 8. CI as a contract

The workflow runs seven jobs in parallel: secret scan, commit-message lint, .NET, web, two Python projects,
config validation; then five container builds. Nothing merges red. Two design choices worth copying:

- **Each job uses the project's own commands** (`dotnet test`, `npm test`, `uv run pytest`), the same ones in
  `CLAUDE.md`. CI never has a private way of building that developers cannot reproduce.
- **Container builds depend on the test jobs**, so a broken test never wastes ten minutes of image building.

Angular 22 ships with Vitest and a zoneless runtime by default; the Angular job runs lint, Prettier, tests and
a production build with bundle budgets, so a dependency that doubles the bundle fails the build.

---

## 9. The support-window habit, twice in one week

The blueprint said .NET 8 and Angular 20. Both leave support in November 2026. Checking the vendor lifecycle
page took two minutes each time and produced ADR-010 and ADR-011. Make it a reflex: **before scaffolding on
any runtime, look up its end-of-support date and compare it with your delivery date plus a year.**

---

## 10. Pitfalls met after week 1 (and what they teach)

**A green `main` does not mean green pull requests.** The first pull requests failed two checks in seconds with
"Resource not accessible by integration". The workflow token had `contents: read` only; on a `pull_request`
event the secret scan and the commit linter call `GET /pulls/{n}/commits`, which also needs
`pull-requests: read`. Pushes to `main` never make that call, so the bug hid until the first pull request.
Lesson: least-privilege tokens are right, but list every API your actions call, per event type, and test the
workflow on the event you will rely on (here: `pull_request`, because `main` is protected).

**"Builds" is not "runs".** Dependabot proposed Python 3.15 for the runtime stage of the Python images while the
build stage still installed packages into Python 3.12. The image built, CI went green, and the container would
have crashed on start. The fix is a smoke test that starts every image and calls its health endpoint. On its
first run the smoke test caught a second, older bug: the ingest worker image used the plain .NET runtime, but the
worker references BuildingBlocks, which needs the ASP.NET Core shared framework, so the image had never started.
A transitive framework reference changes the base image you need. Lesson:
test the artefact you ship, not only the steps that produce it.

**Automation must respect decisions.** Dependabot also proposed Node 26, TypeScript 7 and Python 3.15, each of
which contradicts an ADR (Node 22 and Angular 22 in ADR-011; Python 3.12 until reviewed). The Dependabot
configuration now ignores major (and, for Python tags, minor) bumps of ADR-pinned runtimes. Lesson: a bot that
proposes upgrades is a supply-chain control only if its rules encode your architecture decisions.

## Check yourself

1. Why does `depends_on` without a `condition` not solve the Keycloak-before-PostgreSQL problem?
2. In the OIDC diagram, which party verifies the token signature, and why is it not the SPA?
3. What breaks if the audience mapper is removed from the realm, and why is that the right failure?
4. Why does the Platform API Dockerfile copy `.csproj` files before the rest of the source?
5. What would a fitness function for ADR-004 (read-only OT boundary) check in Week 5?
6. Why did CI pass on `main` but fail on every pull request, and what does that say about testing a pipeline?
7. How can a container image build successfully and still be broken?

## What surprised me

*(Your paragraph.)*
