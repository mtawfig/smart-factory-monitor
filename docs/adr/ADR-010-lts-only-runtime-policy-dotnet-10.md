# ADR-010: LTS-only runtime policy; target .NET 10

**Status:** Accepted (amends the runtime version named in ADR-001 and ADR-002)
**Date:** 2026-11-01

## Context

Blueprint v2.0 names .NET 8 for the Platform API and Ingest Worker. The owner asked whether .NET 9 would be
better. Microsoft's published lifecycle on the decision date:

| Release | Type | End of support |
|---|---|---|
| .NET 8 | LTS (36 months) | 10 November 2026 |
| .NET 9 | STS (extended to 24 months) | 10 November 2026 |
| .NET 10 | LTS (36 months) | November 2028 |

Both .NET 8 and .NET 9 leave support nine days after this decision. Starting a 12-week build on either would
mean shipping the portfolio release on an unsupported runtime. .NET 9 brings no support advantage over .NET 8
and is itself out of support before gate G2.

The same reasoning applies to every runtime and framework in the stack: a portfolio project is judged partly on
whether its choices would survive a production review, and "unsupported runtime" fails that review immediately.

## Decision

1. **Target .NET 10** (C# 14, ASP.NET Core 10, EF Core 10) for the Platform API, the Ingest Worker and all
   shared .NET libraries. `global.json` pins the SDK major version; `Directory.Build.props` sets
   `<TargetFramework>net10.0</TargetFramework>` once for the whole solution.
2. **LTS-only policy.** For every runtime with an LTS/STS cadence (.NET, Node.js for the Angular toolchain,
   PostgreSQL major versions, Python), choose the newest **LTS** release that the hosting platform supports.
   Adopt an STS release only by a new ADR with a stated reason and an exit plan.
3. **Hosting check before adopting.** Before any upgrade, confirm the base container images and Azure
   Container Apps support the version; record the check in the roadmap.
4. The remaining stack versions named in the blueprint (Angular 20, Python 3.12, PostgreSQL 16) stay as they
   are for Release 1. They are reviewed against this policy at gate G5, before the cloud deployment, and any
   change is a separate ADR.

## Consequences

- `.NET 8` in the blueprint PDF is superseded; `docs/blueprint/README.md` records the deviation.
- Library choices must have .NET 10 support: Npgsql 10 / EF Core 10 provider, MQTTnet 5.x,
  StackExchange.Redis 2.8+, Confluent.Kafka 2.x. Verified during the Week 1 skeleton.
- Developers need the .NET 10 SDK locally; CI uses `actions/setup-dotnet` with the `10.0.x` channel.
- Interview-ready statement: "I target LTS runtimes and record the support window in an ADR, because an
  unsupported runtime is a security finding, not a version number."

## Sources

- Microsoft .NET Blog, ".NET 8 and .NET 9 will reach End of Support on November 10, 2026"
- Microsoft .NET Blog, "Announcing .NET 10"
- Microsoft Learn, ".NET releases, patches, and support"
