"""Typed view of docs/plant/plant.yaml.

The YAML is the single source of truth for the fictional plant (assets, sensors, faults, scenarios).
This module validates it with pydantic so a typo in the spec fails a test instead of a demo.
"""

from pathlib import Path
from typing import Literal

import yaml
from pydantic import BaseModel, ConfigDict, Field

SensorKind = Literal["counter", "counter_reset_on_tool_change", "setpoint_driven"]


class Sensor(BaseModel):
    model_config = ConfigDict(extra="forbid")

    metric: str
    unit: str
    interval_s: float = Field(gt=0)
    nominal: tuple[float, float] | None
    kind: SensorKind | None = None


class Asset(BaseModel):
    model_config = ConfigDict(extra="allow")

    id: str = Field(pattern=r"^[A-Z]{3}-\d{3}$")
    name: str
    asset_class: str
    sensors: list[Sensor] = Field(min_length=1)
    applicable_faults: list[str]


class Line(BaseModel):
    model_config = ConfigDict(extra="allow")

    id: str
    name: str
    assets: list[Asset] = Field(min_length=1)


class Fault(BaseModel):
    model_config = ConfigDict(extra="allow")

    id: str
    name: str
    label: Literal["failure", "quality_flag"]
    applies_to: list[str] | Literal["all"]


class Scenario(BaseModel):
    model_config = ConfigDict(extra="allow")

    id: str
    description: str
    time_compression: int | None = None


class PlantSpec(BaseModel):
    model_config = ConfigDict(extra="allow")

    spec_version: int
    synthetic: Literal[True]  # Why: the spec must say so; a non-synthetic plant is a different product.
    seed: int
    lines: list[Line] = Field(min_length=1)
    faults: list[Fault]
    scenarios: list[Scenario]

    @property
    def assets(self) -> list[Asset]:
        return [asset for line in self.lines for asset in line.assets]

    @property
    def metric_count(self) -> int:
        return sum(len(asset.sensors) for asset in self.assets)


def load_plant_spec(path: Path) -> PlantSpec:
    """Load and validate the plant specification from YAML."""
    with path.open(encoding="utf-8") as handle:
        raw = yaml.safe_load(handle)
    return PlantSpec.model_validate(raw)
