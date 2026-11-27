# MLflow tracking server with a PostgreSQL driver.
# Why: the upstream image ships without psycopg2, so --backend-store-uri
# postgresql+psycopg2://... fails. This adds the one dependency and nothing else.
FROM ghcr.io/mlflow/mlflow:v3.17.2

RUN pip install --no-cache-dir psycopg2-binary==2.9.10

# Non-root for parity with the Azure Container App.
RUN useradd --create-home --uid 10001 mlflow \
 && mkdir -p /mlflow/artifacts \
 && chown -R mlflow:mlflow /mlflow
USER mlflow
WORKDIR /mlflow

EXPOSE 5000
