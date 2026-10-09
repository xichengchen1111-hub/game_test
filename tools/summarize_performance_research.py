"""Summarize the temporary Unity research player's raw frames without retaining them in Git."""
import argparse
import collections
import hashlib
import json
import math
import statistics
from pathlib import Path


def percentile(values, fraction):
    if not values:
        return None
    ordered = sorted(values)
    return round(ordered[max(0, math.ceil(len(ordered) * fraction) - 1)], 4)


def summarize(frames):
    elapsed = [f["frameMs"] for f in frames if f["frameMs"] > 0]
    result = {
        "samples": len(frames),
        "meanFps": round(1000 / statistics.mean(elapsed), 2),
        "over33_33ms": sum(t > 1000 / 30 for t in elapsed),
        "over50ms": sum(t > 50 for t in elapsed),
        "over100ms": sum(t > 100 for t in elapsed),
    }
    for key in frames[0]:
        values = [f[key] for f in frames if f[key] >= 0]
        result[key] = {
            "availableSamples": len(values),
            "p50": percentile(values, 0.5),
            "p95": percentile(values, 0.95),
            "p99": percentile(values, 0.99),
            "max": max(values) if values else None,
        }
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("frames", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    raw = args.frames.read_bytes()
    data = json.loads(raw)
    result = {k: v for k, v in data.items() if k not in ("sections", "counters")}
    result["rawSha256"] = hashlib.sha256(raw).hexdigest()
    result["aggregation"] = "Nearest-rank percentiles; pooled repeats. FPS=1000/mean elapsed ms. Missing counters are null, not zero. Main Thread and CPU frame timings can include waits. Instrumentation allocates sample objects; GC is not attributed to game code."
    result["textureMemoryMiB"] = {
        "start": round(data["startTextureBytes"] / 1048576, 3),
        "end": round(data["endTextureBytes"] / 1048576, 3),
        "scope": "Texture.currentTextureMemory; excludes render textures and some internally-created resources. Not total VRAM or process RAM.",
    }
    groups = collections.defaultdict(list)
    result["repeats"] = []
    for section in data["sections"]:
        groups[section["name"]].extend(section["frames"])
        result["repeats"].append({"name": section["name"], "repeat": section["repeat"], **summarize(section["frames"])})
    result["scenarios"] = [{"name": name, **summarize(frames)} for name, frames in groups.items()]
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    for scenario in result["scenarios"]:
        print(scenario["name"], "n=", scenario["samples"], "p95=", scenario["frameMs"]["p95"], "GPU p50=", scenario["gpuMs"]["p50"], "draws=", scenario["drawCalls"]["p50"])


if __name__ == "__main__":
    main()
