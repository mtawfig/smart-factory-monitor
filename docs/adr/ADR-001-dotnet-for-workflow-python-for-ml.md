# ADR-001: .NET for workflow services, Python for ML training and inference

**Status:** Accepted (runtime version amended by ADR-010: .NET 10 LTS replaces .NET 8)
**Date:** 2026-11-01 (decision fixed in blueprint v2.0)

## Context

The platform has two very different kinds of work. Business workflow (assets, work orders, inventory,
audit, authorisation, multi-tenancy) needs a strongly typed, long-lived service with a mature ORM and
good tooling. Machine learning (training on tabular data, feature engineering, explanation, model
registry) is best served by the Python ecosystem. The owner's target skills include .NET, containers,
microservices and AI/ML; a single language for everything would weaken one side or the other.

## Decision

- All workflow services (Platform API, Ingest Worker) are written in **C# on .NET 10**.
- All ML training, evaluation and inference (ML Service) is written in **Python 3.12** with
  scikit-learn, LightGBM, SHAP and the MLflow client.
- The Plant Simulator is also Python 3.12 so that the simulator and the ML code share one data model
  for telemetry and labels.
- Services talk to each other only through versioned contracts (REST/OpenAPI and JSON-Schema events,
  see ADR-003). No shared code between the .NET and Python sides.

## Consequences

- Two toolchains in CI (dotnet, uv/pip) and two sets of lint/test conventions.
- Contracts under `contracts/` become the single source of truth and must be validated in both stacks.
- Clear portfolio signal: polyglot system with explicit integration boundaries.
