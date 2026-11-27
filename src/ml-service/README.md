# ML Service

Python 3.12 / FastAPI. Trains and serves Model A (failure classifier, UCI AI4I 2020) and Model B
(anomaly detection on simulator telemetry). MLflow registry, quality gate and human approval before
promotion (ADR-005). **All data is synthetic or public; outputs are advisory only.**

```bash
uv sync                      # create .venv with dev tools
uv run ruff check . && uv run ruff format --check . && uv run mypy
uv run pytest
uv run uvicorn ml_service.main:app --reload --port 8000
```

Week 1 contains health endpoints only. See `docs/ROADMAP.md` Weeks 7–8.
