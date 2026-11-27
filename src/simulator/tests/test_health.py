from fastapi.testclient import TestClient

from simulator.main import SYNTHETIC_HEADER, app

client = TestClient(app)


def test_liveness_and_label() -> None:
    response = client.get("/health/live")
    assert response.status_code == 200
    assert "synthetic" in response.headers[SYNTHETIC_HEADER]
