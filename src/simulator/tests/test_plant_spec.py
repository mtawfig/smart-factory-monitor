from pathlib import Path

from simulator.plant_spec import load_plant_spec

SPEC_PATH = Path(__file__).resolve().parents[3] / "docs" / "plant" / "plant.yaml"


def test_plant_spec_matches_blueprint_shape() -> None:
    spec = load_plant_spec(SPEC_PATH)

    assert spec.synthetic is True
    assert [line.id for line in spec.lines] == ["line-a", "line-b"]
    assert len(spec.assets) == 8
    # Blueprint v2.0 text says 31; its own sensor table lists 32 (PMP-211 has five). See plant.yaml.
    assert spec.metric_count == 32


def test_every_fault_applies_to_known_assets() -> None:
    spec = load_plant_spec(SPEC_PATH)
    asset_ids = {asset.id for asset in spec.assets}

    for fault in spec.faults:
        if fault.applies_to == "all":
            continue
        unknown = set(fault.applies_to) - asset_ids
        assert not unknown, f"{fault.id} references unknown assets {unknown}"


def test_every_asset_lists_only_faults_from_the_library() -> None:
    spec = load_plant_spec(SPEC_PATH)
    fault_ids = {fault.id for fault in spec.faults}

    for asset in spec.assets:
        assert set(asset.applicable_faults) <= fault_ids, asset.id


def test_sensor_faults_are_quality_flags_not_failures() -> None:
    spec = load_plant_spec(SPEC_PATH)
    sensor_fault = next(f for f in spec.faults if f.id == "SENSOR_FAULT")

    assert sensor_fault.label == "quality_flag"
    assert sensor_fault.applies_to == "all"
