"""Author four original, short held-item foley clips; no sampled recordings."""
from pathlib import Path
import hashlib
import json
import math
import random
import struct
import wave

RATE = 44100
OUT = Path(__file__).resolve().parents[1] / "game/Assets/Resources/PropSounds"


def pulse(t, centre, width):
    return math.exp(-((t - centre) / width) ** 2)


def make(name, duration, seed, paper, opening):
    rng = random.Random(seed)
    fast = slow = 0.0
    samples = []
    for i in range(round(duration * RATE)):
        t = i / RATE
        white = rng.uniform(-1, 1)
        fast += .46 * (white - fast)
        slow += .055 * (white - slow)
        friction = fast - slow
        if paper:
            # Several overlapping passes of fibres and folds, rather than a tone.
            peaks = ((.05, .022, 1), (.17, .052, .68), (.32, .058, .42)) if opening else ((.055, .032, .8), (.19, .043, 1), (.29, .035, .38))
            envelope = sum(pulse(t, at, width) * strength for at, width, strength in peaks)
            value = (friction * .90 + white * .09) * envelope
        else:
            # A small metal latch/resonance over soft pocket fabric movement.
            strike = .11 if opening else .035
            u = max(0, t - strike)
            ring = 0 if t < strike else min(1, u / .0015) * math.exp(-u / (.022 if opening else .014))
            metal = math.sin(2 * math.pi * (1760 if opening else 1310) * u) * .35
            metal += math.sin(2 * math.pi * 3270 * u) * .16 + white * .16
            fabric = pulse(t, .067 if opening else .16, .055) * .45
            value = friction * fabric + metal * ring
        edge = min(1, t / .006, (duration - t) / .018)
        samples.append(value * max(0, edge))
    gain = .62 / max(abs(v) for v in samples)
    samples = [v * gain for v in samples]
    data = struct.pack("<" + "h" * len(samples), *(round(v * 32767) for v in samples))
    path = OUT / (name + ".wav")
    with wave.open(str(path), "wb") as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(RATE)
        wav.writeframes(data)
    return dict(name=name, seconds=duration, peak=max(abs(v) for v in samples), rms=math.sqrt(sum(v*v for v in samples)/len(samples)), sha256=hashlib.sha256(path.read_bytes()).hexdigest())


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    results = [make("MapUnfold", .46, 193101, True, True), make("MapFold", .38, 193102, True, False),
               make("WatchTakeOut", .30, 193103, False, True), make("WatchPutAway", .28, 193104, False, False)]
    print(json.dumps(results, indent=2))
