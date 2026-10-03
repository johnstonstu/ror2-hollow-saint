"""Original deterministic PCM for Hollow Saint; no sampled third-party audio.

Run with the bundled Python. Produces conservative mono levels, seamless periodic
loops, source measurements, and Wwise's reproducible tab-delimited import list.
"""
from pathlib import Path
import json
import math
import wave
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "art/audio/source"
RATE = 48000
ROLES = {
    "Footstep": (.17, .10, "step"), "FootstepRun": (.15, .12, "step"),
    "Land": (.30, .16, "step"), "AirJump": (.30, .19, "air"),
    "GlideEnter": (.40, .13, "air"), "GlideExit": (.35, .10, "air"),
    "GlideLoop": (2., .055, "loop"), "CircuitLoop": (2., .075, "loop"),
    "ArcBoltCast": (.18, .19, "electric"), "BoltImpact": (.28, .24, "electric"),
    "ChainHop": (.20, .16, "electric"), "SpearThrow": (.28, .22, "air"),
    "SpearImpact": (.42, .28, "electric"), "ArcStepStart": (.25, .20, "air"),
    "ArcStepEnd": (.20, .14, "air"), "CircuitUnfold": (.60, .22, "air"),
    "CircuitOpen": (.40, .22, "electric"), "CircuitPulse": (.26, .10, "electric"),
    "CircuitClose": (.55, .16, "air"), "MeterFull": (.65, .18, "electric"),
    "ChargeTick": (.22, .10, "electric"), "Electrocute": (.35, .23, "electric"),
    "StaticTier": (.14, .045, "electric"),
    "ThunderTelegraph": (.35, .14, "charge"),
    "ThunderRelease": (.24, .20, "release"),
    "ThunderStrike": (1.2, .28, "thunder"),
    # v08 additions; appended so existing per-role seeds (index) are unchanged.
    "SpearRecall": (.5, .20, "recall"), "SpearCatch": (.25, .24, "catch"),
    "FanLoop": (2., .09, "loop"), "FanStart": (.2, .15, "ignite"),
    "FanEnd": (.25, .12, "fizzle"), "SpearPulse": (.18, .05, "pulse"),
    "SpearStruck": (.35, .26, "struck"),
}


def band_noise(n, rng, low, high):
    """Circular band-limited noise: no loop seam or sharp Nyquist content."""
    bins = np.fft.rfftfreq(n, 1 / RATE)
    shape = np.exp(-.5 * (bins / high) ** 4)
    shape *= 1 - np.exp(-.5 * (bins / max(1, low)) ** 4)
    spec = (rng.normal(size=len(bins)) + 1j * rng.normal(size=len(bins))) * shape
    spec[0] = 0
    signal = np.fft.irfft(spec, n)
    return signal / max(np.std(signal), 1e-8)


def synth(name, duration, peak, kind, index):
    rng = np.random.default_rng(5100 + index)
    n = round(duration * RATE)
    t = np.arange(n) / RATE
    low = band_noise(n, rng, 60, 900)
    airy = band_noise(n, rng, 500, 4500)
    if kind == "loop" and name == "FanLoop":
        # Circular crackle: bursts wrap around the loop point, hum periods are integers.
        crisp = band_noise(n, rng, 1100, 7500)
        signal = .30 * band_noise(n, rng, 300, 3000) * (.6 + .4 * np.cos(2 * np.pi * 7 * t))
        signal += .10 * np.sin(2 * np.pi * 120 * t) + .05 * np.sin(2 * np.pi * 240 * t)
        for center in rng.uniform(0, duration, 70):
            dist = (t - center + duration / 2) % duration - duration / 2
            signal += crisp * np.exp(-(dist / rng.uniform(.0008, .0028)) ** 2) * rng.uniform(.5, 1.6)
    elif kind == "loop":
        # Exact integer periods across two seconds; FFT noise is circular too.
        signal = .65 * low + .16 * airy
        signal += .25 * np.sin(2 * np.pi * 96 * t) + .1 * np.sin(2 * np.pi * 192 * t)
        signal *= .8 + .12 * np.cos(2 * np.pi * 1.5 * t)
        if name == "CircuitLoop":
            signal += .1 * np.sin(2 * np.pi * 384 * t) * (1 + .4 * np.sin(2 * np.pi * 3 * t))
    elif kind in ("charge", "release", "strike"):
        # High-passed irregular discharges: no falling oscillator or bomb-like bass.
        crisp = band_noise(n, rng, 1100, 7200)
        progress = t / duration
        if kind == "charge":
            envelope = .10 + .90 * progress ** 1.6
            signal = .10 * airy * envelope
            centers = duration * (1 - (1 - np.linspace(.03, .96, 16)) ** 1.8)
        elif kind == "release":
            envelope = np.exp(-t * 10)
            signal = .15 * crisp * envelope
            centers = np.array([.008, .022, .045, .082, .125, .18])
        else:
            envelope = np.exp(-t * 13)
            signal = .18 * airy * envelope + .1 * crisp * np.exp(-t * 7)
            centers = np.array([.007, .015, .027, .044, .071, .11, .17, .25, .32])
        for center in centers:
            width = rng.uniform(.0012, .0045)
            burst = np.exp(-((t - center) / width) ** 2)
            signal += crisp * burst * envelope * rng.uniform(.5, 1.)
    elif kind == "recall":
        # Rising electrical hum, doppler-style pitch rise, crackle density builds.
        p = t / duration
        phase = 2 * np.pi * np.cumsum(190 * (1 + 1.5 * p ** 2)) / RATE
        hum = np.sin(phase) + .5 * np.sin(2 * phase + .7) + .3 * np.sin(3 * phase + 1.9)
        crisp = band_noise(n, rng, 1500, 7000)
        signal = (.55 * hum + .25 * airy * (.3 + p)) * (.12 + p ** 1.4)
        for center in duration * .95 * (1 - rng.uniform(0, 1, 14) ** 1.6):
            signal += .5 * crisp * np.exp(-((t - center) / .0018) ** 2) * (.3 + p)
    elif kind == "catch":
        # Inharmonic metallic clank plus a spark snap.
        crisp = band_noise(n, rng, 1500, 8000)
        ts = np.maximum(t - .004, 0)
        signal = np.zeros(n)
        for freq, amp, decay in ((1180, 1., 26), (1870, .7, 34), (2930, .5, 48), (4210, .35, 62), (310, .6, 40)):
            signal += .9 * amp * np.sin(2 * np.pi * freq * ts + rng.uniform(0, 6.28)) * np.exp(-ts * decay) * (t >= .004)
        for center, amp in ((.006, 1.4), (.017, .8), (.034, .5), (.06, .3)):
            signal += amp * crisp * np.exp(-((t - center) / .0014) ** 2)
    elif kind == "ignite":
        crisp = band_noise(n, rng, 1300, 7000)
        p = t / duration
        signal = .25 * airy * np.sin(np.pi * np.minimum(1, p)) ** .7 * p
        signal += .12 * np.sin(2 * np.pi * (380 * t + 3500 * t * t)) * np.minimum(1, p * 3) * np.exp(-t * 8)
        for center in duration * .75 * rng.uniform(0, 1, 18) ** .6:
            signal += crisp * np.exp(-((t - center) / .0016) ** 2) * (.4 + p) * rng.uniform(.6, 1.4)
    elif kind == "fizzle":
        crisp = band_noise(n, rng, 1300, 7000)
        signal = .22 * airy * np.exp(-t * 11)
        signal += .08 * np.sin(2 * np.pi * (1600 * t - 3000 * t * t)) * np.exp(-t * 14)
        for center in duration * .85 * rng.uniform(0, 1, 14) ** 1.8:
            signal += crisp * np.exp(-((t - center) / .0017) ** 2) * np.exp(-center * 8) * rng.uniform(.5, 1.2)
    elif kind == "pulse":
        crisp = band_noise(n, rng, 1500, 6500)
        signal = .5 * np.sin(2 * np.pi * (780 * t + 1400 * t * t)) * np.exp(-t * 24) + .15 * airy * np.exp(-t * 30)
        for center, amp in ((.010, .5), (.032, .25)):
            signal += amp * crisp * np.exp(-((t - center) / .0015) ** 2)
    elif kind == "struck":
        # Punchy pylon zap: low thump, bright snap, metallic ring on the spear.
        crisp = band_noise(n, rng, 1400, 8000)
        signal = .9 * np.sin(2 * np.pi * (140 * t - 120 * t * t)) * np.exp(-t * 16)
        signal += .35 * np.sin(2 * np.pi * 1450 * t) * np.exp(-t * 22) + .25 * np.sin(2 * np.pi * 2310 * t) * np.exp(-t * 30)
        signal += .3 * airy * np.exp(-t * 14)
        for center, amp in ((.007, 1.8), (.016, 1.), (.03, .7), (.055, .5), (.09, .35)):
            signal += amp * crisp * np.exp(-((t - center) / .0018) ** 2)
    elif kind == "thunder":
        # Sharp crack transient, then a slow rolling low rumble with a late swell.
        crisp = band_noise(n, rng, 1200, 8000)
        mid = band_noise(n, rng, 120, 900)
        rumble = band_noise(n, rng, 28, 160)
        signal = 1.2 * crisp * np.exp(-t * 70) * (t > .004)
        for center, width in ((.006, .002), (.012, .003), (.022, .004), (.038, .005), (.065, .005)):
            signal += 1.5 * crisp * np.exp(-((t - center) / width) ** 2) * np.exp(-t * 25)
        signal += .5 * mid * np.exp(-t * 9) * (1 - np.exp(-t / .01))
        env = (1 - np.exp(-t / .05)) * np.exp(-t * 2.8) + .6 * np.exp(-((t - .55) / .18) ** 2) * .3
        roll = 1 + .45 * np.sin(2 * np.pi * 5.5 * t + 1.3)
        signal += 1.1 * rumble * env * roll
        signal += .5 * np.sin(2 * np.pi * 52 * t) * env
    elif kind == "step":
        # A soft cloth/sole brush with short low thump, not a metallic impact.
        signal = .62 * low * np.exp(-t * 22) + .08 * airy * np.exp(-t * 32)
        signal += .25 * np.sin(2 * np.pi * (78 * t - 35 * t * t)) * np.exp(-t * 25)
    elif kind == "air":
        sweep = np.sin(2 * np.pi * (260 * t + 450 * t * t))
        signal = .62 * airy + .3 * low + .10 * sweep
        signal *= np.sin(np.pi * np.minimum(1, t / duration)) ** .8
    else:
        # Sparse rounded crackle over a short airy electrical sweep, no square-wave beep.
        freq = 470 + (index % 5) * 45
        chirp = np.sin(2 * np.pi * (freq * t + 1000 * t * t))
        signal = (.45 * airy + .25 * low + .18 * chirp) * np.exp(-t * 7)
        for center in rng.uniform(.01, duration * .55, 4):
            signal += .18 * airy * np.exp(-((t - center) / .0035) ** 2)
        if name in ("MeterFull", "ChargeTick"):
            signal += .1 * np.sin(2 * np.pi * 660 * t) * np.exp(-t * 9)
        if name in ("ArcBoltCast", "ChainHop", "ChargeTick"):
            # More distinct texture, keeping each role's existing peak ceiling.
            crisp = band_noise(n, rng, 1300, 6500)
            signal *= .7
            for center in rng.uniform(.008, duration * .8, 9):
                signal += .6 * crisp * np.exp(-((t - center) / .0017) ** 2)
    signal -= np.mean(signal)
    if kind != "loop":
        fade = np.minimum(1, t / .006) * np.minimum(1, (duration - 1 / RATE - t) / .045)
        signal *= np.maximum(0, fade)
    signal *= peak / max(np.max(np.abs(signal)), 1e-8)
    return np.round(signal * 32767).astype("<i2")


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    manifest = []
    rows = ["Audio File\tObject Path\tEvent"]
    for index, (name, (duration, peak, kind)) in enumerate(ROLES.items()):
        pcm = synth(name, duration, peak, kind, index)
        path = OUT / (name + ".wav")
        with wave.open(str(path), "wb") as stream:
            stream.setnchannels(1)
            stream.setsampwidth(2)
            stream.setframerate(RATE)
            stream.writeframes(pcm.tobytes())
        f = pcm.astype(float) / 32768
        assert np.max(np.abs(f)) < .29, name + " exceeded conservative headroom"
        if kind != "loop":
            assert pcm[0] == pcm[-1] == 0, name + " endpoint click"
        else:
            assert abs(int(pcm[0]) - int(pcm[-1])) / 32768 < .012, name + " loop seam"
        manifest.append({"name": name, "event": "Play_HS_" + name, "seconds": duration,
                         "loop": kind == "loop", "peak_dbfs": round(20 * math.log10(max(abs(f))), 2),
                         "rms_dbfs": round(20 * math.log10(np.sqrt(np.mean(f * f))), 2)})
        rows.append(f"{path.relative_to(ROOT / 'art' / 'audio')}\t\\Actor-Mixer Hierarchy\\Default Work Unit\\<Sound SFX>HS_{name}\tPlay_HS_{name}")
    (OUT / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    (ROOT / "art/audio/import.tsv").write_text("\n".join(rows) + "\n", encoding="utf-8")
    print(f"Generated {len(manifest)} original WAV sources; peaks/headroom/endpoints/loops verified.")


if __name__ == "__main__":
    main()
