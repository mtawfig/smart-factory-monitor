from fastapi.testclient import TestClient

from ml_service.main import SYNTHETIC_HEADER, app

client = TestClient(app)


def test_liveness() -> None:
    response = client.get("/health/live")
    assert response.status_code == 200
    assert response.json() == {"status": "Healthy"}


def test_every_response_carries_synthetic_label() -> None:
    response = client.get("/health/ready")
    assert "synthetic" in response.headers[SYNTHETIC_HEADER]
    assert "advisory-only" in response.headers[SYNTHETIC_HEADER]


def test_openapi_states_the_boundaries() -> None:
    spec = client.get("/openapi.json").json()
    description = spec["info"]["description"]
    assert "synthetic" in description
    assert "advisory" in description
