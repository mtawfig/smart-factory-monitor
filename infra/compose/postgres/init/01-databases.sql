-- Runs once, on first start of the postgres volume (docker-entrypoint-initdb.d).
-- Why: Keycloak and MLflow need their own databases; the platform uses
-- "smartfactory" with one schema per module (ADR-007), created by migrations.

CREATE DATABASE keycloak;
CREATE DATABASE mlflow;

\connect smartfactory

-- The Timescale image preloads the library; the extension still has to exist in
-- the database that owns the hypertables. Idempotent on purpose.
CREATE EXTENSION IF NOT EXISTS timescaledb;

-- One schema per module. Owned by the application role created in Week 2
-- migrations; for now the superuser owns them so Compose "just works".
CREATE SCHEMA IF NOT EXISTS assets;
CREATE SCHEMA IF NOT EXISTS maintenance;
CREATE SCHEMA IF NOT EXISTS inventory;
CREATE SCHEMA IF NOT EXISTS audit;
CREATE SCHEMA IF NOT EXISTS telemetry;

COMMENT ON DATABASE smartfactory IS 'SmartFactory AI — synthetic data only; advisory-only platform';
