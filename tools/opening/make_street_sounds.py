"""The barrel organ on the square (StreetOrgan): an original waltz written for the game, played by
modelled pipes, no sampled recordings.

A 1930s street organ: a pinned barrel playing flue and reed pipes together, a little out of tune,
with the tremolo of hand-cranked bellows. The melody is new (sixteen bars in G major, 3/4, a second
time round with a different turn into the cadence); the accompaniment is the usual oom-pah-pah. The
loop is rendered three times and the middle copy kept, so the reverberant tails wrap across the seam.
Energy sits above about 150 Hz so laptop speakers carry it (AGENTS.md, 声音可听性).
"""
import os
import sys
import wave

import numpy as np

RATE = 22050
ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'game/Assets/Resources/StreetSounds/BarrelOrgan.wav')
BEAT = 60 / 138  # quarter note

# Scale degrees of G major relative to G4.
NOTE = {'F#4': -1, 'G4': 0, 'A4': 2, 'B4': 4, 'C5': 5, 'D5': 7, 'E5': 9, 'F#5': 11, 'G5': 12, 'A5': 14}
# (note, beats) per bar; bars 9–16 differ the second time.
FIRST = [
    [('D5', 2), ('B4', 1)], [('G4', 2), ('B4', 1)], [('D5', 1), ('E5', 1), ('D5', 1)], [('B4', 3)],
    [('C5', 2), ('A4', 1)], [('F#4', 2), ('A4', 1)], [('C5', 1), ('D5', 1), ('C5', 1)], [('A4', 3)],
    [('B4', 1), ('C5', 1), ('D5', 1)], [('G5', 2), ('F#5', 1)], [('E5', 1), ('D5', 1), ('C5', 1)], [('B4', 2), ('A4', 1)],
    [('G4', 1), ('B4', 1), ('D5', 1)], [('C5', 2), ('E5', 1)], [('D5', 1), ('A4', 1), ('F#4', 1)], [('G4', 3)],
]
SECOND = FIRST[:8] + [
    [('D5', 1), ('E5', 1), ('F#5', 1)], [('G5', 2), ('D5', 1)], [('E5', 1), ('G5', 1), ('E5', 1)], [('D5', 2), ('B4', 1)],
    [('C5', 1), ('E5', 1), ('A5', 1)], [('G5', 2), ('E5', 1)], [('D5', 1), ('C5', 1), ('A4', 1)], [('G4', 3)],
]
# Chord per bar (root, third, fifth as semitones from G4): G, D7, C.
G, D7, C = (-12, -8, -5), (-5, -1, 2), (-7, -3, 0)
HARMONY = [G, G, G, G, D7, D7, D7, D7, G, G, C, G, G, C, D7, G]


def hz(semitones, detune=0.0):
    return 392.0 * 2 ** ((semitones + detune) / 12)


def pipe(f, seconds, reed, rng):
    """One pipe: a flue (near sine, soft octave) or a reed (bright odd partials), with a chiff."""
    n = int(seconds * RATE)
    t = np.arange(n) / RATE
    partials = [(1, 1), (2, .5), (3, .34), (4, .2), (5, .16), (6, .08), (7, .07)] if reed else [(1, 1), (2, .22), (3, .07), (4, .03)]
    x = sum(a * np.sin(2 * np.pi * f * k * t + rng.uniform(0, 6.28)) for k, a in partials)
    attack = min(.025, seconds / 4)
    env = np.minimum(1, t / attack) * np.minimum(1, (seconds - t) / .05).clip(0)
    chiff = rng.standard_normal(n) * np.exp(-t / .012) * .12
    return (x / sum(a for _, a in partials) + chiff) * env


def render(bars, rng):
    total = sum(sum(b for _, b in bar) for bar in bars) * BEAT
    out = np.zeros(int(total * RATE) + RATE * 2)
    clock = 0.0
    for i, bar in enumerate(bars):
        chord = HARMONY[i % 16]
        # The tune: flue and reed pipes in unison, a few cents apart (an organ never quite in tune).
        at = clock
        for note, beats in bar:
            length = beats * BEAT * .92
            s = int(at * RATE)
            f = hz(NOTE[note], rng.uniform(-.06, .06))
            tone = .55 * pipe(f, length, False, rng) + .38 * pipe(f * 1.0015, length, True, rng) + .12 * pipe(f * 2, length, False, rng)
            out[s:s + len(tone)] += tone
            at += beats * BEAT
        # Oom: the bass on the first beat (an octave above the true bass, so small speakers carry it).
        s = int(clock * RATE)
        bass = .5 * pipe(hz(chord[0], rng.uniform(-.05, .05)), BEAT * .7, True, rng)
        out[s:s + len(bass)] += bass
        # Pah-pah: the chord, short, on beats two and three.
        for beat in (1, 2):
            s = int((clock + beat * BEAT) * RATE)
            for k, semis in enumerate(chord):
                c = .17 * pipe(hz(semis + 12, rng.uniform(-.05, .05)), BEAT * .38, k == 1, rng)
                out[s:s + len(c)] += c
        clock += sum(b for _, b in bar) * BEAT
    return out[:int(total * RATE)], total


def room(x, rng, rt60=.55, mix=.22):
    n = int(rt60 * RATE)
    t = np.arange(n) / RATE
    ir = rng.standard_normal(n) * np.exp(-6.9 * t / rt60)
    ir[:int(.012 * RATE)] = 0
    wet = np.fft.irfft(np.fft.rfft(x, len(x) + n) * np.fft.rfft(ir, len(x) + n))[:len(x)]
    return x + mix * wet / (np.abs(wet).max() + 1e-9) * np.abs(x).max()


def main():
    rng = np.random.default_rng(193110)
    tune, seconds = render(FIRST + SECOND, rng)
    # Three copies end to end, keep the middle: the tails of one pass ring into the next exactly as in the loop.
    triple = np.concatenate([tune, tune, tune])
    n = len(tune)
    t = np.arange(len(triple)) / RATE
    # The bellows: a tremolo at the crank's pace and a breath of wind.
    triple *= 1 + .07 * np.sin(2 * np.pi * 5.6 * t) + .04 * np.sin(2 * np.pi * 1.05 * t)
    wind = np.convolve(rng.standard_normal(len(triple)), np.ones(9) / 9, 'same') * .018
    triple = room(triple + wind, rng)
    loop = triple[n:2 * n]
    # Keep it out of the sub-bass entirely (laptop speakers cannot play it anyway).
    spectrum = np.fft.rfft(loop)
    freqs = np.fft.rfftfreq(len(loop), 1 / RATE)
    spectrum[freqs < 120] *= (freqs[freqs < 120] / 120) ** 2
    loop = np.fft.irfft(spectrum, len(loop))
    loop *= .89 / np.abs(loop).max()
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with wave.open(OUT, 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes((np.clip(loop, -1, 1) * 32767).astype('<i2').tobytes())
    power = np.abs(np.fft.rfft(loop)) ** 2
    low = power[freqs < 150].sum() / power.sum()
    seam = abs(loop[0] - loop[-1])
    print('sound', os.path.relpath(OUT, ROOT), '%.1fs' % (len(loop) / RATE), 'below150Hz=%.1f%%' % (100 * low), 'seam=%.4f' % seam, flush=True)
    if low > .1 or seam > .05:
        print('STREET_SOUNDS_FAIL', flush=True)
        return 1
    print('STREET_SOUNDS_PASS', flush=True)
    return 0


if __name__ == '__main__':
    sys.exit(main())
