# ADR-005: MLflow registry, quality gate and human approval before model promotion

**Status:** Accepted
**Date:** 2026-11-01 (decision fixed in blueprint v2.0)

## Context

Models that silently change behaviour in production erode trust, especially in maintenance where a
false negative is a missed failure and a false positive is wasted technician time. The project must
show responsible MLOps without a large platform.

## Decision

- Every trained model is logged to an **MLflow** tracking server and registered in the MLflow
  **model registry** with its parameters, metrics, data lineage and artefacts.
- **Quality gate**: a candidate is eligible for promotion only if it beats the committed baseline on the
  **untouched test split** on the primary metric (Model A: PR-AUC and recall at fixed precision with
  Brier calibration; Model B: precision at the alert cap and detection delay on replayed scenarios).
- **Human approval**: promotion from *Staging* to *Production* is a manual action by a user with the
  data/ML engineer role, recorded as an `AuditEvent` and published as a `ModelPromoted` event.
- A **model card** is mandatory for every model and lives in the repository next to the training code.
  It states intended use, training data (synthetic or public, named), metrics, limitations and the
  advisory-only boundary.
- Models in scope: **Model A** failure classifier on UCI AI4I 2020 (baselines → LightGBM, SHAP, served at
  `/score`); **Model B** anomaly detection on simulator telemetry (5-minute window features, Isolation
  Forest per asset class trained on healthy windows, persistence/hysteresis/cooldown). RUL on NASA
  C-MAPSS is Release 2 only.

## Consequences

- Promotion is slower by design; the demo shows the approval step.
- Model cards and the replay evaluation report are gate G4 deliverables.
- The ML Service always serves the *Production* version and exposes `model_version` on every score.
