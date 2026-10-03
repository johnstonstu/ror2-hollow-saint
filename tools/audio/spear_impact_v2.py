"""v0.9.16 (Stu: the spear impact sounds light, no zap or impact): new SpearImpact and SpearBurst.

Run after import_samples.py and before author_bank.py (it overwrites those two sources):

    python tools/audio/spear_impact_v2.py
    python tools/audio/author_bank.py

SpearImpact (every spear as it sticks): a real electric crack (the first 60 ms of the licensed
strike sample, high-passed), a pitched-down body thump, a gated buzzing zap and a crackling sizzle.
SpearBurst (charged spear burst): the strike sample over a sub boom and the same zap, louder.
Both peak at about -4 dBFS (the old ones were -11), so they sit with the Thunderbolt.
"""
from pathlib import Path
import json
import wave
import numpy as np
from scipy import signal

import import_samples as samples

RATE = 48000
OUT = Path(__file__).resolve().parents[2] / "art/audio/source"


def env(t, tau):
    return np.exp(-t / tau)


def bandpass(x, low, high):
    sos = signal.butter(4, [low, high], "bandpass", fs=RATE, output="sos")
    return signal.sosfiltfilt(sos, x)


def highpass(x, f):
    sos = signal.butter(4, f, "highpass", fs=RATE, output="sos")
    return signal.sosfiltfilt(sos, x)


def norm(x):
    return x / max(1e-9, np.abs(x).max())


def thump(n, start_hz, end_hz, sweep_tau, decay_tau, drive=1.6):
    t = np.arange(n) / RATE
    freq = end_hz + (start_hz - end_hz) * env(t, sweep_tau)
    phase = 2 * np.pi * np.cumsum(freq) / RATE
    return np.tanh(drive * np.sin(phase)) * env(t, decay_tau)


def zap(n, rng, start, length, decay_tau):
    """Buzzing arc: a detuned saw pair, FM wobble, chopped by random crackle gates."""
    t = np.arange(n) / RATE
    f = 120 + 25 * np.sin(2 * np.pi * 31 * t) + 18 * rng.standard_normal(n).cumsum() / np.sqrt(n)
    phase = np.cumsum(f) / RATE
    saw = 2 * (phase % 1) - 1 + 0.7 * (2 * ((phase * 2.013) % 1) - 1)
    gate = np.zeros(n)
    i = int(start * RATE)
    end = int((start + length) * RATE)
    while i < end:
        width = int(rng.uniform(0.004, 0.018) * RATE)
        gate[i:i + width] = rng.uniform(0.5, 1.0)
        i += width + int(rng.uniform(0.002, 0.012) * RATE)
    gate = signal.lfilter([0.15], [1, -0.85], gate)  # soften the gate edges
    shaped = np.zeros(n)
    shaped[int(start * RATE):] = env(t[:n - int(start * RATE)], decay_tau)
    return bandpass(saw * gate * shaped, 350, 6500)


def sizzle(n, rng, decay_tau):
    t = np.arange(n) / RATE
    noise = bandpass(rng.standard_normal(n), 3000, 10000)
    spikes = (rng.random(n) < 0.004).astype(float)
    spikes = signal.lfilter([1], [1, -0.97], spikes)
    return noise * (0.35 + spikes) * env(t, decay_tau)


def crack(n, length=0.06):
    x = samples.decode(samples.SAMPLES / samples.STRIKE)
    s = samples.onset(x)
    y = highpass(x[s:s + int(length * RATE)], 1500)
    y = norm(y) * np.hanning(len(y) * 2)[len(y):] ** 0.5
    out = np.zeros(n)
    out[:len(y)] = y
    return out


def strike(n):
    x = samples.decode(samples.SAMPLES / samples.STRIKE)
    s = samples.onset(x)
    y = highpass(x[s:s + n], 40)
    out = np.zeros(n)
    out[:len(y)] = norm(y)
    fade = int(0.3 * RATE)
    out[n - fade:] *= 0.5 * (1 + np.cos(np.linspace(0, np.pi, fade)))
    return out


def write(name, y, peak_db, note):
    fade_in = int(0.002 * RATE)
    y[:fade_in] *= np.linspace(0, 1, fade_in)
    fade = int(0.08 * RATE)
    y[-fade:] *= np.linspace(1, 0, fade)
    y = norm(y) * 10 ** (peak_db / 20)
    pcm = np.clip(np.round(y * 32767), -32768, 32767).astype("<i2")
    with wave.open(str(OUT / (name + ".wav")), "wb") as stream:
        stream.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
        stream.writeframes(pcm.tobytes())
    rms = 20 * np.log10(np.sqrt(np.mean((pcm / 32768.0) ** 2)) + 1e-12)
    return {"name": name, "event": "Play_HS_" + name, "seconds": round(len(y) / RATE, 3), "loop": False,
            "peak_dbfs": peak_db, "rms_dbfs": round(float(rms), 2), "source": note}


def main():
    rng = np.random.default_rng(916)
    n = int(0.6 * RATE)
    impact = (0.65 * crack(n) + 1.0 * thump(n, 170, 48, 0.03, 0.11)
              + 0.75 * norm(zap(n, rng, 0.01, 0.30, 0.13)) + 0.22 * norm(sizzle(n, rng, 0.18)))
    entries = [write("SpearImpact", impact, -4.0, "v0.9.16 synth + crack from sample:" + samples.STRIKE)]

    n = int(1.0 * RATE)
    burst = (1.0 * strike(n) + 0.9 * thump(n, 110, 32, 0.06, 0.26, drive=2.0)
             + 0.4 * norm(zap(n, rng, 0.0, 0.35, 0.18)) + 0.15 * norm(sizzle(n, rng, 0.35)))
    entries.append(write("SpearBurst", burst, -4.5, "v0.9.16 sample:" + samples.STRIKE + " + sub boom + zap"))

    manifest_path = OUT / "manifest.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    byname = {e["name"]: e for e in entries}
    manifest = [byname.get(item["name"], item) for item in manifest]
    manifest_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    for e in entries:
        print("WROTE", e)


if __name__ == "__main__":
    main()
