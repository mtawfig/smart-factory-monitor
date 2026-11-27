"""Simulator control API. Week 1: health only.

Week 5 adds the MQTT publisher (QoS 1, topics plant/{tenant}/{plant}/{line}/{asset}/{metric});
Week 6 adds scenario selection, fault injection, time compression and replay. This process is the
ONLY thing that ever publishes under plant/# (ADR-004).
"""

from collections.abc import Awaitable, Callable

from fastapi import FastAPI, Request, Response

from simulator import __version__

SYNTHETIC_HEADER = "X-SmartFactory-Data"
DATA_LABEL = "synthetic; advisory-only; no-plc-control"

app = FastAPI(
    title="SmartFactory AI — Plant Simulator (control API)",
    version=__version__,
    description=(
        "Controls the fictional Al Safa Industrial plant simulation: scenarios, fault injection, "
        "time compression. Produces synthetic telemetry only. This is a simulation control plane, "
        "not an equipment interface."
    ),
)


@app.middleware("http")
async def synthetic_data_header(
    request: Request, call_next: Callable[[Request], Awaitable[Response]]
) -> Response:
    response = await call_next(request)
    response.headers[SYNTHETIC_HEADER] = DATA_LABEL
    return response


@app.get("/health/live", tags=["health"])
def live() -> dict[str, str]:
    return {"status": "Healthy"}


@app.get("/health/ready", tags=["health"])
def ready() -> dict[str, str]:
    return {"status": "Healthy", "service": "simulator"}
