# ADR-004: Read-only OT boundary over MQTT

**Status:** Accepted
**Date:** 2026-11-01 (decision fixed in blueprint v2.0)

## Context

Predictive-maintenance systems sit next to operational technology (OT). A system that can write to a
PLC is a safety and security liability and would require controls far beyond a portfolio project.
The plant here is simulated, but the architecture must still show the correct real-world posture.

## Decision

- The simulator publishes telemetry over **MQTT** (Eclipse **Mosquitto**, **QoS 1**) on topics
  `plant/{tenant}/{plant}/{line}/{asset}/{metric}`.
- The Ingest Worker **subscribes only**. **No PLC write path exists anywhere in the code**, and none
  will be added. Architecture tests assert that no component publishes to `plant/#` except the simulator.
- The **only reverse message** is a "maintenance completed" notification from the platform to the
  **simulator** (not to any machine), so that the simulated asset health can be reset.
- Control of the simulator itself (scenario selection, fault injection, time compression) goes through
  its own FastAPI control API, which is clearly labelled as a *simulation* control plane.

## Consequences

- The system is advisory only, by construction, and the README, UI and model cards say so.
- Realistic edge posture: one-way data flow, broker in the middle, no inbound connections to the plant.
- Simulated asset health resets need the reverse notification; it is deliberately narrow and documented.
