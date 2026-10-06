"""Gaze of the Hollow release sounds (playtest: release lacks punch, 1/2/3 charges indistinguishable).

    python tools/audio/gaze_release_sfx.py

Writes art/audio/new-gaze/: GazeLoad1..3 (charge locked in, ascending C5 / F5 / C6 + crackle),
GazeSurgeHit1..3 (surge lands: crack + thump, escalating to a thunderclap with sub boom), and
audition.wav. Deterministic (fixed seeds). numpy only (no scipy): filters are FFT-based.
Layers reuse ThunderStrike / SpearBurst from art/audio/source (falls back to the main checkout
when run from a worktree where those generated files are absent).
"""
from pathlib import Path
import wave
import numpy as np

RATE = 48000
ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "art/audio/source"
OUT = ROOT / "art/audio/new-gaze"


def find(name):
    for base in [ROOT] + list(ROOT.parents):
        p = base / "art/audio/source" / name
        if p.exists():
            return p
    raise FileNotFoundError(name)


def read(path):
    with wave.open(str(path), "rb") as w:
        x = np.frombuffer(w.readframes(w.getnframes()), "<i2").astype(np.float64) / 32768
        assert w.getnchannels() == 1 and w.getframerate() == RATE
    return x


def fft_filter(x, lo=None, hi=None):
    """Zero-phase band filter with smooth (raised-cosine) edges."""
    n = len(x)
    f = np.fft.rfftfreq(n, 1 / RATE)
    g = np.ones_like(f)
    if lo:
        g *= np.clip((f - lo * 0.7) / (lo * 0.6), 0, 1) ** 2
    if hi:
        g *= np.clip((hi * 1.3 - f) / (hi * 0.6), 0, 1) ** 2
    return np.fft.irfft(np.fft.rfft(x) * g, n)


def env(t, tau):
    return np.exp(-t / tau)


def norm(x):
    return x / max(1e-9, np.abs(x).max())


def place(n, y, at=0.0):
    out = np.zeros(n)
    i = int(at * RATE)
    m = min(len(y), n - i)
    out[i:i + m] = y[:m]
    return out


def thump(n, f0, f1, sweep, decay, drive=1.6):
    t = np.arange(n) / RATE
    f = f1 + (f0 - f1) * env(t, sweep)
    return np.tanh(drive * np.sin(2 * np.pi * np.cumsum(f) / RATE)) * env(t, decay)


def crackle(n, rng, tau, density=0.004, lo=2500, hi=11000):
    t = np.arange(n) / RATE
    noise = fft_filter(rng.standard_normal(n), lo, hi)
    spikes = (rng.random(n) < density).astype(float)
    k = np.exp(-np.arange(400) / 25.0)
    spikes = np.convolve(spikes, k)[:n]
    return noise * (0.3 + 2.0 * spikes) * env(t, tau)


def zap(n, rng, start, length, tau):
    t = np.arange(n) / RATE
    f = 130 + 28 * np.sin(2 * np.pi * 33 * t) + 20 * rng.standard_normal(n).cumsum() / np.sqrt(n)
    ph = np.cumsum(f) / RATE
    saw = 2 * (ph % 1) - 1 + 0.7 * (2 * ((ph * 2.013) % 1) - 1)
    gate = np.zeros(n)
    i, end = int(start * RATE), int((start + length) * RATE)
    while i < end:
        w = int(rng.uniform(0.003, 0.014) * RATE)
        gate[i:i + w] = rng.uniform(0.5, 1.0)
        i += w + int(rng.uniform(0.002, 0.010) * RATE)
    s = int(start * RATE)
    shaped = np.zeros(n)
    shaped[s:] = env(t[:n - s], tau)
    return fft_filter(saw * gate * shaped, 350, 6500)


def finish(y, peak_db, fade_in=0.001, fade_out=0.05, drive=None):
    y = y - y.mean()
    y = fft_filter(y, 25, None)
    if drive:  # soft glue so RMS rises without clipping
        y = np.tanh(drive * norm(y)) 
    a = int(fade_in * RATE)
    y[:a] *= np.linspace(0, 1, a)
    b = int(fade_out * RATE)
    y[-b:] *= np.linspace(1, 0, b) ** 1.5
    y = norm(y) * 10 ** (peak_db / 20)
    return y


# ---------------------------------------------------------------- load cues
def load_cue(tier):
    rng = np.random.default_rng(1200 + tier)
    dur = {1: 0.18, 2: 0.22, 3: 0.30}[tier]
    n = int(dur * RATE)
    t = np.arange(n) / RATE
    f0 = {1: 523.25, 2: 698.46, 3: 1046.5}[tier]  # C5, F5 (fourth), C6 (octave)
    f = f0 * (1 + 0.06 * (1 - env(t, 0.03)))  # small upward chirp = "snap into place"
    ph = 2 * np.pi * np.cumsum(f) / RATE
    tone = np.sin(ph) + 0.35 * np.sin(2 * ph) + 0.18 * np.sin(3 * ph)
    tone += 0.25 * np.sin(ph * 1.5 + 0.8)  # fifth above: electric, slightly hollow
    amp = np.minimum(1, t / 0.003) * env(t, {1: 0.045, 2: 0.06, 3: 0.09}[tier])
    y = tone * amp
    zp = zap(n, rng, 0.0, dur * 0.7, 0.05)
    y = norm(y) + 0.22 * norm(zp)
    y += (0.35 + 0.1 * tier) * norm(crackle(n, rng, 0.05 + 0.02 * tier, 0.003 + 0.002 * tier,
                                            3000 if tier < 3 else 4500, 12000))
    if tier == 3:
        # full/ready: octave-up shimmer + a short sizzling tail
        sh = np.sin(2 * ph * 1.0) * env(t, 0.1) * np.minimum(1, t / 0.004)
        y += 0.35 * sh
        y += 0.4 * norm(crackle(n, rng, 0.14, 0.006, 5000, 14000)) * np.minimum(1, t / 0.05)
    y = fft_filter(y, 300, None)
    return finish(y, -6.0, 0.001, 0.04)


# ---------------------------------------------------------------- surge hits
def crack_from(name, length, hp=1500):
    x = read(find(name))
    s = max(0, int(np.argmax(np.abs(x) > 0.1 * np.abs(x).max())) - int(0.003 * RATE))
    y = fft_filter(x[s:s + int(length * RATE)], hp, None)
    return norm(y) * np.hanning(len(y) * 2)[len(y):] ** 0.5


def body_from(name, n, hp=50):
    x = read(find(name))
    s = max(0, int(np.argmax(np.abs(x) > 0.1 * np.abs(x).max())) - int(0.003 * RATE))
    y = fft_filter(x[s:s + n], hp, None)
    out = np.zeros(n)
    out[:len(y)] = norm(y)
    return out


def surge_hit(tier):
    rng = np.random.default_rng(1300 + tier)
    dur = {1: 0.35, 2: 0.5, 3: 0.9}[tier]
    n = int(dur * RATE)
    t = np.arange(n) / RATE
    # sharp electric crack (real strike transient), longer/louder per tier
    y = {1: 0.9, 2: 1.1, 3: 1.3}[tier] * place(n, crack_from("SpearBurst.wav", 0.05 + 0.01 * tier))
    if tier == 3:
        y += 0.45 * place(n, crack_from("ThunderStrike.wav", 0.09, 900))
    # punchy thump: fast pitch drop, deeper per tier
    f0, f1, dec = {1: (190, 70, 0.07), 2: (150, 52, 0.12), 3: (120, 46, 0.30)}[tier]
    y += {1: 0.95, 2: 1.15, 3: 1.3}[tier] * thump(n, f0, f1, 0.025 + 0.01 * tier, dec, 1.8 + 0.3 * tier)
    # zap: buzzing arc (absent on tier 1, bigger after)
    if tier >= 2:
        y += {2: 0.55, 3: 0.6}[tier] * norm(zap(n, rng, 0.005, 0.22 if tier == 2 else 0.5, 0.13 if tier == 2 else 0.25))
    if tier == 3:
        # sub boom 45-60 Hz and the rolling thunder body
        sub = np.sin(2 * np.pi * np.cumsum(46 + 14 * env(t, 0.12)) / RATE) * env(t, 0.32)
        sub *= np.minimum(1, t / 0.006)
        y += 1.5 * sub
        y += 1.0 * body_from("ThunderStrike.wav", n, 60) * env(t, 0.5)
        y += 0.5 * norm(crackle(n, rng, 0.38, 0.005, 3000, 12000)) * np.minimum(1, t / 0.03)
    else:
        y += (0.22 if tier == 1 else 0.3) * norm(crackle(n, rng, 0.07 * tier, 0.004, 3000, 11000))
    return finish(y, {1: -3.0, 2: -2.5, 3: -2.0}[tier], 0.0008, {1: 0.06, 2: 0.08, 3: 0.2}[tier],
                  drive={1: 1.0, 2: 1.1, 3: 4.0}[tier])


def write(path, y):
    pcm = np.clip(np.round(y * 32767), -32768, 32767).astype("<i2")
    with wave.open(str(path), "wb") as w:
        w.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
        w.writeframes(pcm.tobytes())
    return pcm.astype(np.float64) / 32768


def metrics(x):
    S = np.abs(np.fft.rfft(x))
    f = np.fft.rfftfreq(len(x), 1 / RATE)
    return (len(x) / RATE, 20 * np.log10(np.abs(x).max() + 1e-12),
            20 * np.log10(np.sqrt(np.mean(x ** 2)) + 1e-12), (S * f).sum() / S.sum())


MIX_LOAD_GAIN = 10 ** (-10 / 20)
MIX_HIT_GAIN = {1: 10 ** (-4.5 / 20), 2: 10 ** (-5.0 / 20), 3: 10 ** (-6.0 / 20)}


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    clips = {}
    for k in (1, 2, 3):
        # Mix trim (1.2 loudness pass): loads repeat every 0.28 s; keep them near ChargeTick/ArcStep.
        clips[f"GazeLoad{k}"] = write(OUT / f"GazeLoad{k}.wav", load_cue(k) * MIX_LOAD_GAIN)
    for k in (1, 2, 3):
        # Tier 3 lands around SpearBurst; tiers keep ~3-4 dB steps.
        clips[f"GazeSurgeHit{k}"] = write(OUT / f"GazeSurgeHit{k}.wav", surge_hit(k) * MIX_HIT_GAIN[k])
    # audition: Load1..3 at 0.28 s, 0.3 s gap, Hit3; 1 s gap; Hit1, 0.5, Hit2, 0.5, Hit3
    L = [clips[f"GazeLoad{k}"] for k in (1, 2, 3)]
    H = [clips[f"GazeSurgeHit{k}"] for k in (1, 2, 3)]
    total = 0.28 * 2 + len(L[2]) / RATE + 0.3 + len(H[2]) / RATE + 1.0 + sum(len(h) for h in H) / RATE + 1.0 + 0.5
    mix = np.zeros(int(total * RATE))
    pos = 0.0
    def add(x, at):
        i = int(at * RATE); mix[i:i + len(x)] += x
    for k in range(3):
        add(L[k], pos + 0.28 * k)
    pos += 0.28 * 2 + len(L[2]) / RATE + 0.3
    add(H[2], pos); pos += len(H[2]) / RATE + 1.0
    add(H[0], pos); pos += len(H[0]) / RATE + 0.5
    add(H[1], pos); pos += len(H[1]) / RATE + 0.5
    add(H[2], pos); pos += len(H[2]) / RATE
    mix = mix[:int((pos + 0.1) * RATE)]
    write(OUT / "audition.wav", mix * min(1.0, 0.95 / np.abs(mix).max()))
    print(f"{'file':<22}{'sec':>7}{'peak':>8}{'rms':>8}{'centroid':>10}")
    rows = list(clips.items())
    for n in ["GazeSurge1", "GazeSurge5", "ChargeTick", "MeterFull", "ThunderStrike", "SpearImpact", "SpearBurst", "BoltImpact", "Electrocute"]:
        try:
            rows.append(("ref:" + n, read(find(n + ".wav"))))
        except FileNotFoundError:
            pass
    for n, x in rows:
        d, p, r, c = metrics(x)
        print(f"{n:<22}{d:7.3f}{p:8.1f}{r:8.1f}{c:10.0f}")
    for n in clips:
        x = clips[n]
        print(n, "DC=%.6f" % x.mean(), "first-sample=%.4f" % x[0])


if __name__ == "__main__":
    main()
