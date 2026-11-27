# ML Service — uv-based multi-stage build. Context: src/ml-service
#   docker build -t smartfactory/ml-service src/ml-service
FROM ghcr.io/astral-sh/uv:python3.12-bookworm-slim AS build
WORKDIR /app
ENV UV_COMPILE_BYTECODE=1 UV_LINK_MODE=copy UV_PYTHON_DOWNLOADS=never
COPY pyproject.toml uv.lock ./
RUN uv sync --frozen --no-dev --no-install-project
COPY src ./src
COPY README.md ./
RUN uv sync --frozen --no-dev

FROM python:3.12-slim-bookworm AS runtime
WORKDIR /app
ENV PATH="/app/.venv/bin:$PATH" PYTHONUNBUFFERED=1 SMARTFACTORY_DATA=synthetic
RUN useradd --create-home --uid 10001 app
COPY --from=build --chown=app:app /app /app
USER app
EXPOSE 8000
CMD ["uvicorn", "ml_service.main:app", "--host", "0.0.0.0", "--port", "8000"]
