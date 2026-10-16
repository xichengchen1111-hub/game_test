#!/usr/bin/env python3
"""Door, lock and knock sounds for the police station and the opening (user, 2026-10-15: the
wooden and iron door sounds were wrong: a buzzing creak, a synth-bass thud and a bell for the cell
door, the same clip for opening and closing it).

Physically modelled rather than tones: short contact excitations (clicks, a fist, a slamming leaf)
ring a bank of damped resonators tuned like wood panels, steel bars and small lock parts; hinge
creaks are stick-slip pulse trains whose rate wanders as the leaf moves; every clip then gets a
short room response (stone cell, plaster rooms) so it does not sound dry and synthetic.

Writes 16-bit mono 44.1 kHz WAVs:
  game/Assets/Art/Police/Sounds/PoliceDoorOpen.wav, PoliceDoorClose.wav,
                                PoliceIronDoor.wav (opening), PoliceIronDoorClose.wav
  game/Assets/Resources/OpeningSounds/CellKnock.wav (one blow of a fist on the cell door),
                                      CellUnlock.wav, CageUnlock.wav, CageDoor.wav
Deterministic (fixed seeds). Needs numpy and scipy.
"""
import os
import sys
import wave

import numpy as np
from scipy import signal

RATE = 44100
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))


def seconds(n):
    return np.arange(n) / RATE


def resonate(x, modes):
    """Each mode (frequency Hz, decay s to 1/e, gain): a two-pole resonator ringing at unit amplitude."""
    out = np.zeros_like(x)
    for f, tau, g in modes:
        if f >= RATE / 2 * .95:
            continue
        r = np.exp(-1 / (tau * RATE))
        w = 2 * np.pi * f / RATE
        out += signal.lfilter([np.sin(w)], [1, -2 * r * np.cos(w), r * r], x) * g
    return out


def band(x, lo, hi, order=2):
    sos = signal.butter(order, [lo, hi], btype='band', fs=RATE, output='sos')
    return signal.sosfilt(sos, x)


def lowpass(x, f, order=2):
    return signal.sosfilt(signal.butter(order, f, btype='low', fs=RATE, output='sos'), x)


def highpass(x, f, order=2):
    return signal.sosfilt(signal.butter(order, f, btype='high', fs=RATE, output='sos'), x)


class Clip:
    def __init__(self, length, seed):
        self.n = int(length * RATE)
        self.x = np.zeros(self.n)
        self.rng = np.random.default_rng(seed)

    def at(self, t):
        return int(t * RATE)

    def add(self, part, db):
        """Mix a component in at a designed peak level (dB below full scale), so a long soft push
        into low resonances cannot bury a click or a creak that is meant to be heard."""
        peak = np.abs(part).max()
        if peak > 0:
            self.x += part * (10 ** (db / 20) / peak)

    def ring(self, excitation, modes):
        return resonate(excitation, modes)

    def creak(self, *args, **kwargs):
        """stick_slip rendered on its own (see add)."""
        keep = self.x
        self.x = np.zeros(self.n)
        self.stick_slip(*args, **kwargs)
        part, self.x = self.x, keep
        return part

    def ticks(self, t, modes, hardness=.0004):
        return resonate(self.contact(t, hardness, 1.0), modes)

    def scraped(self, *args, **kwargs):
        keep = self.x
        self.x = np.zeros(self.n)
        self.scrape(*args, **kwargs)
        part, self.x = self.x, keep
        return part

    def contact(self, t, hardness, gain=1.0, length=None):
        """Excitation of a strike: a noise burst whose length sets how hard the contact is."""
        i = self.at(t)
        m = int((length or hardness * 8) * RATE) + 2
        e = self.rng.standard_normal(m) * np.exp(-seconds(m) / hardness)
        e = lowpass(e, min(16000, .35 / hardness))
        x = np.zeros(self.n)
        x[i:i + m] = e[:max(0, min(m, self.n - i))] * gain
        return x

    def soft(self, t, width, gain=1.0):
        """A soft, rounded push (a fist, a padded leaf): a half-sine pulse."""
        i = self.at(t)
        m = max(2, int(width * RATE))
        x = np.zeros(self.n)
        p = np.sin(np.pi * np.arange(m) / m) * gain
        x[i:i + m] = p[:max(0, min(m, self.n - i))]
        return x

    def tick(self, t, modes, hardness=.0004, gain=1.0):
        self.x += resonate(self.contact(t, hardness, gain), modes)

    def scrape(self, t, length, lo, hi, gain, grain=0.0, grain_rate=40):
        """Metal sliding on metal: band-limited noise, optionally with the regular catch of teeth or pins."""
        i, m = self.at(t), int(length * RATE)
        noise = band(self.rng.standard_normal(m), lo, hi)
        env = np.sin(np.pi * np.linspace(0, 1, m)) ** .7
        if grain:
            env *= 1 + grain * np.maximum(0, np.sin(2 * np.pi * grain_rate * seconds(m))) ** 8
        self.x[i:i + m] += (noise * env * gain)[:max(0, min(m, self.n - i))]

    def stick_slip(self, t, length, rate, wobble, jitter, envelope, modes, gain, seed=None):
        """A hinge creak: the pin sticks and slips, each slip a tiny impulse ringing the leaf and
        the hinge. `rate(u)` is the slip rate (Hz) at u in 0..1 over the movement, `envelope(u)`
        its loudness; `wobble` and `jitter` keep it from sounding like an oscillator."""
        rng = np.random.default_rng(seed) if seed is not None else self.rng
        x = np.zeros(self.n)
        start, end = self.at(t), self.at(t + length)
        slow = signal.savgol_filter(rng.standard_normal(64), 21, 2)
        slow /= max(1e-6, np.abs(slow).max())
        s = 0.0
        while True:
            u = s / length
            if u >= 1:
                break
            f = rate(u) * (1 + wobble * np.interp(u, np.linspace(0, 1, 64), slow))
            i = start + int(s * RATE)
            if i >= min(end, self.n - 8):
                break
            a = envelope(u) * (.55 + .45 * rng.random())
            k = rng.standard_normal(6) * np.exp(-np.arange(6) / 1.5)
            x[i:i + 6] += k * a
            s += (1 / max(5.0, f)) * (1 + rng.normal(0, jitter))
        self.x += resonate(x, modes) * gain

    def room(self, rt60, mix, size=1.0, brightness=.5, seed=7):
        """A short room: early reflections, then a diffuse tail that loses its highs faster."""
        rng = np.random.default_rng(seed)
        m = int(min(2.2, rt60 * 1.3) * RATE)
        t = seconds(m)
        noise = rng.standard_normal(m)

        def decay(rt):
            return 10 ** (-3 * t / rt)
        tail = lowpass(noise, 600) * decay(rt60) + band(noise, 600, 3500) * decay(rt60 * .8) * .8 + \
            highpass(noise, 3500) * decay(rt60 * .45) * .5 * brightness * 2
        tail *= np.minimum(1, t / (.012 * size))
        ir = tail / np.sqrt(np.sum(tail ** 2))
        for d, g in ((.0061, .55), (.0097, .42), (.0143, .33), (.0188, .27), (.0242, .2), (.0311, .15)):
            j = int(d * size * RATE)
            if j < m:
                ir[j] += g * (1 if rng.random() > .5 else -1) * .4
        wet = signal.fftconvolve(self.x, ir)[:self.n]
        wet = lowpass(wet, 7000)
        dry_rms = np.sqrt(np.mean(self.x ** 2)) + 1e-9
        wet_rms = np.sqrt(np.mean(wet ** 2)) + 1e-9
        self.x = self.x + wet * (dry_rms / wet_rms) * mix

    def finish(self, peak=.89, fade=.05):
        x = highpass(self.x, 35)
        m = int(fade * RATE)
        x[-m:] *= np.linspace(1, 0, m) ** 2
        return x / max(1e-9, np.abs(x).max()) * peak


# ---------------------------------------------------------------------------
# Wood: a panelled interior door with a lever handle and a mortice latch.
# ---------------------------------------------------------------------------
WOOD_PANEL = [(96, .055, 1), (151, .05, .8), (233, .04, .62), (348, .032, .5), (497, .026, .38),
              (702, .02, .28), (965, .015, .2), (1340, .011, .14)]
LATCH = [(1720, .012, 1), (2960, .009, .7), (4410, .006, .45), (980, .018, .4)]


def wood_open():
    c = Clip(1.3, 3)
    # Lever pressed down: the follower and spring, then the latch bolt drawn out of the strike.
    c.add(c.ticks(.0, LATCH, .0005), -7)
    c.add(c.scraped(.025, .05, 1800, 5200, 1), -22)
    c.add(c.ticks(.072, [(1480, .01, 1), (2650, .008, .6), (3900, .005, .3)]), -12)
    # The leaf comes free of the frame: a soft knock of the panel and its rebate.
    c.add(c.ring(c.soft(.085, .004, 1), WOOD_PANEL), -10)
    # A dry old hinge as the door swings: slow, uneven slips (a low creak, not a squeal).
    c.add(c.creak(.13, .78, lambda u: 32 + 46 * np.sin(np.pi * u) ** 1.3, .35, .12,
                  lambda u: np.sin(np.pi * min(1, u * 1.15)) ** 1.5,
                  [(286, .03, .9), (471, .026, .8), (733, .02, .6), (1190, .014, .45),
                   (1735, .05, .35), (2870, .035, .2)], 1), -8)
    # Air moved by the leaf.
    m = c.at(.75)
    air = np.zeros(c.n)
    air[c.at(.12):c.at(.12) + m] = lowpass(c.rng.standard_normal(m), 420) * np.sin(np.pi * np.linspace(0, 1, m)) ** 2
    c.add(air, -26)
    c.room(.62, .26, 1.0, .45)
    return c.finish()


def wood_close():
    c = Clip(1.0, 4)
    # The leaf meets the frame: a broad soft impact rings the panel and the frame, a dull thump.
    c.add(c.ring(c.soft(.0, .0045, 1.0) + c.contact(.0, .0006, .25), [(f * .93, tau * 1.15, g) for f, tau, g in WOOD_PANEL]), 0)
    c.add(lowpass(c.soft(.0, .012, 1), 160), -7)
    # The latch snaps into the strike just after, and the leaf settles against it.
    c.add(c.ticks(.018, LATCH), -8)
    c.add(c.ticks(.047, [(1650, .008, 1), (2800, .006, .5)]), -18)
    c.add(c.ring(c.soft(.05, .003, 1), WOOD_PANEL), -16)
    c.room(.62, .3, 1.0, .45)
    return c.finish()


# ---------------------------------------------------------------------------
# Iron: the barred cell door, heavy flat-bar frame on two pin hinges, a lever lock.
# ---------------------------------------------------------------------------
def iron_modes(seed, base=1.0, gain=1.0):
    """Many bars of slightly different length ring together: close partials beat against each other."""
    rng = np.random.default_rng(seed)
    core = [(147, .85, 1), (239, .7, .8), (402, .6, .75), (531, .55, .6), (694, .48, .55),
            (858, .42, .45), (1081, .36, .4), (1349, .3, .32), (1622, .24, .26), (1994, .19, .2),
            (2447, .15, .15), (2961, .12, .1), (3526, .09, .07)]
    modes = []
    for f, tau, g in core:
        for k in range(3):
            modes.append((f * base * (1 + rng.normal(0, .012)), tau * (.8 + .4 * rng.random()), g * gain * (1 if k == 0 else .45)))
    return modes


def iron_open():
    c = Clip(1.6, 5)
    # The lock's bolt drawn back and the leaf leaving the frame: a dull clank with a short ring.
    c.add(c.ring(c.contact(.0, .0007, 1), iron_modes(11)), -8)
    c.add(c.ticks(.0, [(820, .03, 1), (1530, .025, .6), (2650, .015, .4)], .0005), -10)
    # Heavy pin hinges grinding round: a slow, ragged groan rising into a squeal and falling away.
    c.add(c.creak(.1, 1.0, lambda u: 70 + 120 * np.sin(np.pi * u) ** 2 + 30 * u, .3, .09,
                  lambda u: np.sin(np.pi * min(1, u * 1.08)) ** 1.2,
                  [(520, .06, .9), (1040, .05, .75), (1585, .045, .6), (2110, .04, .45),
                   (3170, .03, .3), (390, .08, .5), (760, .07, .45)], 1), 0)
    # The loose bars rattle in their frame as it moves.
    rattle = np.zeros(c.n)
    for k in range(9):
        t = .15 + .09 * k + c.rng.random() * .05
        rattle += c.ticks(t, [(2200 + 900 * c.rng.random(), .02, 1), (3800 + 700 * c.rng.random(), .012, .5)], .0003) * (.5 + .5 * c.rng.random())
    c.add(rattle, -20)
    c.room(.95, .3, 1.15, .4)
    return c.finish()


def iron_close():
    c = Clip(2.2, 6)
    # The leaf swung shut against its stop: every bar rings at once.
    c.add(c.ring(c.contact(.0, .0009, 1.0), iron_modes(21)), 0)
    c.add(lowpass(c.soft(.0, .01, 1), 140), -9)
    # The spring bolt snaps home and the door bounces once against it.
    c.add(c.ticks(.024, [(880, .04, 1), (1720, .03, .6), (3050, .02, .4)]), -6)
    c.add(c.ring(c.contact(.061, .0008, 1), iron_modes(22, 1.01)), -11)
    rattle = np.zeros(c.n)
    for k in range(6):
        t = .09 + .05 * k + c.rng.random() * .02
        rattle += c.ticks(t, [(2400 + 800 * c.rng.random(), .015, 1)], .0003) * (1 - k / 6)
    c.add(rattle, -22)
    c.room(1.0, .32, 1.15, .4)
    return c.finish()


def cell_knock():
    """One blow of a fist on the barred door: a dull boom of the leaf in its frame, a short ring
    of the bars, and the leaf knocking against its lock."""
    c = Clip(1.1, 8)
    fist = c.soft(.0, .0055, 1.0)
    c.add(c.ring(fist, [(82, .08, 1), (121, .07, .85), (168, .06, .65), (226, .05, .45)]), 0)
    c.add(c.ring(fist * .2 + c.contact(.0, .0012, 1), iron_modes(31)), -9)
    c.add(c.ticks(.016, [(910, .03, 1), (1760, .02, .6), (2900, .015, .3)], .0005), -13)
    c.add(c.ticks(.034, [(950, .025, 1), (1820, .018, .5)], .0005), -19)
    c.room(.95, .33, 1.15, .4)
    return c.finish()


def cell_unlock():
    """The guard's key: in along the wards, a turn that lifts the levers, and the bolt thrown back."""
    c = Clip(1.0, 9)
    c.add(c.scraped(.0, .16, 1800, 6500, 1, grain=1.5, grain_rate=34), -14)
    c.add(c.ticks(.16, [(2350, .012, 1), (3900, .008, .5)], .0003), -12)
    c.add(c.ticks(.36, [(1150, .03, 1), (2750, .02, .6), (4300, .012, .35)]), -4)
    c.add(c.scraped(.38, .09, 700, 2600, 1), -16)
    c.add(c.ring(c.contact(.47, .0006, 1), [(330, .05, 1), (620, .04, .8), (1450, .025, .5), (2620, .015, .3)]), 0)
    c.add(c.ticks(.49, [(1400, .02, 1), (2900, .015, .4)]), -9)
    c.room(.9, .25, 1.1, .4)
    return c.finish()


def cage_unlock():
    """A small padlock on the wire cage: two light clicks and the hasp swung free."""
    c = Clip(.8, 10)
    c.add(c.scraped(.0, .08, 2500, 7000, 1, grain=1.2, grain_rate=45), -16)
    c.add(c.ticks(.1, [(2600, .014, 1), (4300, .009, .5)], .0003), -6)
    c.add(c.ticks(.24, [(2150, .016, 1), (3700, .01, .5), (5600, .006, .25)], .0003), 0)
    c.add(c.ticks(.33, [(1650, .03, 1), (3100, .02, .5)]), -8)
    c.room(.6, .22, .9, .5)
    return c.finish()


def cage_door():
    """The cage's wire door swung open: a thin squeak of the small hinges and the mesh shivering."""
    c = Clip(1.0, 12)
    c.add(c.creak(.02, .5, lambda u: 210 + 160 * np.sin(np.pi * u), .2, .07,
                  lambda u: np.sin(np.pi * u) ** 1.5,
                  [(980, .03, 1), (1960, .025, .6), (2890, .02, .4), (4100, .015, .25)], 1), -2)
    mesh = np.zeros(c.n)
    for k in range(14):
        t = .03 + .04 * k + c.rng.random() * .02
        mesh += c.ticks(t, [(3000 + 2500 * c.rng.random(), .01, 1)], .0002) * np.sin(np.pi * (k + 1) / 15)
    c.add(mesh, -14)
    c.add(c.ring(c.contact(.56, .0006, 1), [(1240, .08, 1), (2310, .06, .6), (3620, .04, .4)]), -4)
    c.room(.6, .22, .9, .5)
    return c.finish()


def briefcase_open():
    """The briefcase on a table (user, 2026-10-15: B opens it): two spring catches flick up one after
    the other, a small brass ring after each, then the leather lid lifts on its hinge."""
    c = Clip(.75, 14)
    for k, t in enumerate((.0, .09)):
        c.add(c.ticks(t, [(2900 + 180 * k, .012, 1), (4700, .008, .5), (6300, .005, .25)], .0003), -2 - 3 * k)
        c.add(c.ring(c.contact(t + .003, .0005, 1), [(1850 + 90 * k, .05, 1), (3420, .035, .5)]), -12)
    c.add(c.scraped(.2, .22, 900, 3800, 1, grain=.8, grain_rate=30), -20)
    c.add(c.creak(.22, .32, lambda u: 120 + 60 * np.sin(np.pi * u), .25, .08,
                  lambda u: np.sin(np.pi * u) ** 1.4,
                  [(640, .03, 1), (1280, .025, .6), (2240, .02, .35)], 1), -16)
    c.room(.45, .18, .7, .5)
    return c.finish()


def briefcase_close():
    """The lid set down on the leather frame and both catches pressed shut."""
    c = Clip(.7, 15)
    c.add(c.ring(c.soft(.0, .004, 1) + c.contact(.0, .0008, .3), [(210, .05, 1), (330, .04, .8), (520, .03, .5), (900, .02, .3)]), 0)
    c.add(c.scraped(.0, .05, 700, 2600, 1), -22)
    for k, t in enumerate((.17, .25)):
        c.add(c.ticks(t, [(2500 - 120 * k, .014, 1), (4200, .009, .5)], .0003), -4 - 2 * k)
        c.add(c.ring(c.contact(t + .002, .0005, 1), [(1700, .04, 1), (3100, .03, .45)]), -14)
    c.room(.45, .18, .7, .5)
    return c.finish()


def write(path, x):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    data = (np.clip(x, -1, 1) * 32767).astype('<i2').tobytes()
    with wave.open(path, 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(data)
    print('sound', os.path.relpath(path, ROOT), '%.2fs' % (len(x) / RATE), flush=True)


def main():
    police = os.path.join(ROOT, 'game/Assets/Art/Police/Sounds')
    opening = os.path.join(ROOT, 'game/Assets/Resources/OpeningSounds')
    write(os.path.join(police, 'PoliceDoorOpen.wav'), wood_open())
    write(os.path.join(police, 'PoliceDoorClose.wav'), wood_close())
    write(os.path.join(police, 'PoliceIronDoor.wav'), iron_open())
    write(os.path.join(police, 'PoliceIronDoorClose.wav'), iron_close())
    write(os.path.join(opening, 'CellKnock.wav'), cell_knock())
    write(os.path.join(opening, 'CellUnlock.wav'), cell_unlock())
    write(os.path.join(opening, 'CageUnlock.wav'), cage_unlock())
    write(os.path.join(opening, 'CageDoor.wav'), cage_door())
    write(os.path.join(opening, 'BriefcaseOpen.wav'), briefcase_open())
    write(os.path.join(opening, 'BriefcaseClose.wav'), briefcase_close())
    print('DOOR_SOUNDS_PASS', flush=True)


if __name__ == '__main__':
    sys.exit(main())
