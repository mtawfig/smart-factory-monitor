"""FastAPI application. Week 1: health endpoints and the synthetic-data header only.

Week 7 adds /score (Model A) and the MLflow client; Week 8 adds anomaly scoring (Model B).
"""

from collections.abc import Awaitable, Callable

from fastapi import FastAPI, Request, Response

from ml_service import __version__
from ml_service.settings import settings

SYNTHETIC_HEADER = "X-SmartFactory-Data"

app = FastAPI(
    title="SmartFactory AI — ML Service",
    version=__version__,
    description=(
        "Trains and serves the failure classifier (Model A) and the anomaly detector (Model B). "
        "All data is synthetic or public. Outputs are advisory; no equipment is ever controlled."
    ),
)


@app.middleware("http")
async def synthetic_data_header(
    request: Request, call_next: Callable[[Request], Awaitable[Response]]
) -> Response:
    """Stamp every response with the synthetic-data / advisory-only label."""
    response = await call_next(request)
    response.headers[SYNTHETIC_HEADER] = settings.data_label
    return response


@app.get("/health/live", tags=["health"])
def live() -> dict[str, str]:
    """Process is up."""
    return {"status": "Healthy"}


@app.get("/health/ready", tags=["health"])
def ready() -> dict[str, str]:
    """Dependencies reachable. Week 7 adds the MLflow registry check."""
    return {"status": "Healthy", "service": settings.service_name}
