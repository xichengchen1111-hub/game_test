"""Compare native-frame matte surface samples from the reproducible movement run."""
import argparse
import json
import statistics
from pathlib import Path


def analyse(path):
    report = json.loads(path.read_text(encoding="utf-8-sig"))
    groups = {}
    for sample in report["samples"]:
        groups.setdefault(sample["surface"], []).append(sample)
    surfaces = {}
    for name, samples in groups.items():
        values = [0.2126 * row["r"] + 0.7152 * row["g"] + 0.0722 * row["b"] for row in samples]
        surfaces[name] = {
            "samples": len(values),
            "luminance_range": max(values) - min(values),
            "luminance_stddev": statistics.pstdev(values),
            "largest_adjacent_change": max((abs(a - b) for a, b in zip(values, values[1:])), default=0),
        }
    return {
        "frames": report["renderedFrames"],
        "duration": report["duration"],
        "diagnostic_switches_off": not any(report[key] for key in (
            "mainShadowsDisabled", "localShadowsDisabled", "ambientOcclusionDisabled")),
        "surfaces": surfaces,
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("before", type=Path)
    parser.add_argument("after", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    before, after = analyse(args.before), analyse(args.after)
    old = before["surfaces"]["window-return"]["luminance_range"]
    new = after["surfaces"]["window-return"]["luminance_range"]
    assert after["diagnostic_switches_off"], "Acceptance must retain actual production lighting"
    assert after["frames"] >= 20, "Too few rendered movement samples"
    assert after["surfaces"]["window-return"]["samples"] >= 20, "Too few visible window surface samples"
    assert new < 0.06 and new < old * 0.25, f"Window surface still unstable: {old:.4f} -> {new:.4f}"
    result = {
        "passed": True,
        "before": before,
        "after": after,
        "window_luminance_range_reduction": 1 - new / old,
        "scope": "Fixed world-space patches during native player movement and look rotation; visual frame review is also required",
    }
    args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(f"LIGHTING_STABILITY_PASS: window luminance range {old:.4f} -> {new:.4f}")


if __name__ == "__main__":
    main()
