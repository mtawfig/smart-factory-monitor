"""Runtime configuration (environment variables prefixed ML_)."""

from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Settings for the ML service. Values come from the environment; no secrets live in code."""

    model_config = SettingsConfigDict(env_prefix="ML_", extra="ignore")

    service_name: str = "ml-service"
    mlflow_tracking_uri: str = "http://localhost:5000"
    kafka_bootstrap_servers: str = "localhost:19092"
    # Why: the label travels with every response; the content rules require it on every surface.
    data_label: str = "synthetic; advisory-only; no-plc-control"


settings = Settings()
