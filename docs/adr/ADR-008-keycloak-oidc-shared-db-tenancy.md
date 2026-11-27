# ADR-008: Keycloak OIDC and shared-database multi-tenancy with row-level security

**Status:** Accepted
**Date:** 2026-11-01 (decision fixed in blueprint v2.0)

## Context

The platform must demonstrate multi-tenant SaaS posture and role-based access for five personas
(reliability manager, maintenance planner, technician, data/ML engineer, administrator) without the
cost of a database per tenant. Cross-tenant data leakage is the most serious bug such a system can have.

## Decision

- **Keycloak** is the identity provider. The SPA uses **OIDC Authorization Code with PKCE**; services
  validate JWTs. Tokens carry **roles** and a **`tenant_id` claim**.
- **Shared-database tenancy**: every tenant-scoped table has a `tenant_id` column.
  Defence in depth has three layers:
  1. **EF Core global query filters** on `tenant_id` derived from the current principal;
  2. **PostgreSQL row-level security** policies keyed on a session variable set per request;
  3. **Policy-based authorisation** in ASP.NET Core mapping persona roles to permissions (RBAC matrix
     in the blueprint).
- **Automated cross-tenant authorisation tests** run in CI: for every endpoint, a user of tenant A must
  receive 404/403 for tenant B's resources. Gate G5 requires this suite to be green.

## Consequences

- Every migration must add RLS policies for new tenant-scoped tables (checked by a test).
- Keycloak adds a container locally and a small hosted footprint in Azure.
- The authZ test suite is a reusable portfolio artefact and the main security evidence.
