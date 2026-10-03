"""v0.9.1: build bank sources from licensed recorded samples (Pixabay Content License).

Run AFTER synthesize_sfx.py (it replaces the synthesized ThunderStrike) and BEFORE author_bank.py:

    python tools/audio/synthesize_sfx.py
    python tools/audio/import_samples.py
    python tools/audio/author_bank.py

Sources live in art/audio/samples/ (credits in art/audio/samples/CREDITS.md). Each output is mono
PCM16 48 kHz like the synthesized set: lead silence trimmed to the onset, cut to length, faded,
optionally filtered/pitched, and peak-normalized. Levels are recorded in source/manifest.json.
Needs ffmpeg on PATH, numpy and scipy.
"""
from pathlib import Path
import json
import subprocess
import wave
import numpy as np
from scipy import signal

ROOT = Path(__file__).resolve().parents[2]
SAMPLES = ROOT / "art/audio/samples"
OUT = ROOT / "art/audio/source"
RATE = 48000

THUNDER = "patricksilvey-weather-lightning-2-464187.mp3"
STRIKE = "dragon-studio-lightning-strike-386161.mp3"

# name: (sample, offset after onset s, length s, fade-out s, peak dBFS, highpass Hz, pitch ratio)
CUES = {
    # Thunderbolt impact (crown spear Thunderbolt and Answered Prayer): crack plus rolling tail.
    "ThunderStrike": (THUNDER, 0.0, 2.4, 0.9, -9.0, 35.0, 1.0),
    # Charged Stormspear impact (burst radius 5 m and up). Taps keep the synthesized SpearImpact.
    "SpearBurst": (STRIKE, 0.0, 0.95, 0.35, -11.0, 60.0, 1.0),
    # Heavy layer on charged/crown throws: the first crackle, thinned and pitched up so it reads as a launch.
    "SpearThrowHeavy": (STRIKE, 0.0, 0.42, 0.16, -15.0, 1200.0, 1.12),
}


def decode(path):
    raw = subprocess.run(["ffmpeg", "-v", "error", "-i", str(path), "-ac", "1", "-ar", str(RATE), "-f", "f32le", "-"],
                         check=True, capture_output=True).stdout
    return np.frombuffer(raw, dtype="<f4").astype(np.float64)


def onset(x):
    level = np.abs(x)
    threshold = 0.1 * level.max()  # the real crack, not the faint pre-roll ticks
    index = int(np.argmax(level > threshold))
    return max(0, index - int(0.004 * RATE))  # keep 4 ms before the first transient


def build(name, sample, offset, length, fade, peak_db, highpass, pitch):
    x = decode(SAMPLES / sample)
    start = onset(x) + int(offset * RATE)
    # Read enough source for the pitched length.
    take = int(length * RATE * pitch) + 1
    y = x[start:start + take].copy()
    if highpass > 0:
        sos = signal.butter(4, highpass, "highpass", fs=RATE, output="sos")
        y = signal.sosfiltfilt(sos, y)
    if abs(pitch - 1.0) > 1e-3:
        y = signal.resample(y, int(round(len(y) / pitch)))
    y = y[:int(length * RATE)]
    n = len(y)
    fade_in = int(0.003 * RATE)
    y[:fade_in] *= np.linspace(0, 1, fade_in)
    nf = int(fade * RATE)
    y[n - nf:] *= 0.5 * (1 + np.cos(np.linspace(0, np.pi, nf)))
    y *= (10 ** (peak_db / 20)) / np.abs(y).max()
    pcm = np.clip(np.round(y * 32767), -32768, 32767).astype("<i2")
    with wave.open(str(OUT / (name + ".wav")), "wb") as stream:
        stream.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
        stream.writeframes(pcm.tobytes())
    rms = 20 * np.log10(np.sqrt(np.mean((pcm / 32768.0) ** 2)) + 1e-12)
    return {"name": name, "event": "Play_HS_" + name, "seconds": round(n / RATE, 3), "loop": False,
            "peak_dbfs": round(peak_db, 2), "rms_dbfs": round(float(rms), 2), "source": "sample:" + sample}


def main():
    manifest_path = OUT / "manifest.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    entries = {item["name"]: item for item in manifest}
    for name, cue in CUES.items():
        entries[name] = build(name, *cue)
        print("IMPORTED", entries[name])
    order = [item["name"] for item in manifest] + [n for n in CUES if n not in {i["name"] for i in manifest}]
    manifest_path.write_text(json.dumps([entries[n] for n in order], indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
