# Plant Simulator — uv-based multi-stage build. Context: repository root (needs docs/plant/plant.yaml)
#   docker build -f src/simulator/Dockerfile -t smartfactory/simulator .
FROM ghcr.io/astral-sh/uv:python3.12-bookworm-slim AS build
WORKDIR /app
ENV UV_COMPILE_BYTECODE=1 UV_LINK_MODE=copy UV_PYTHON_DOWNLOADS=never
COPY src/simulator/pyproject.toml src/simulator/uv.lock ./
RUN uv sync --frozen --no-dev --no-install-project
COPY src/simulator/src ./src
COPY src/simulator/README.md ./
RUN uv sync --frozen --no-dev

FROM python:3.12-slim-bookworm AS runtime
WORKDIR /app
ENV PATH="/app/.venv/bin:$PATH" PYTHONUNBUFFERED=1 SMARTFACTORY_DATA=synthetic \
    SIM_PLANT_SPEC=/app/plant/plant.yaml
RUN useradd --create-home --uid 10001 app
COPY --from=build --chown=app:app /app /app
COPY --chown=app:app docs/plant/plant.yaml /app/plant/plant.yaml
USER app
EXPOSE 8000
CMD ["uvicorn", "simulator.main:app", "--host", "0.0.0.0", "--port", "8000"]
