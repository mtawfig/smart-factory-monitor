# Plant Simulator

Python 3.12. Generates **synthetic** telemetry for the fictional Al Safa Industrial plant defined in
`docs/plant/plant.yaml`, publishes it over MQTT (QoS 1) and exposes a FastAPI control API for scenarios,
fault injection and time compression. It is the only component that publishes under `plant/#` (ADR-004).

```bash
uv sync
uv run ruff check . && uv run ruff format --check . && uv run mypy
uv run pytest                 # includes validation of docs/plant/plant.yaml
uv run uvicorn simulator.main:app --reload --port 8001
```

Week 1 contains the control API health endpoints and the typed plant-spec loader. Weeks 5–6 add the
generator, fault library and replay.
