"""Gaze of the Hollow release sounds (playtest: release lacks punch, 1/2/3 charges indistinguishable).

    python tools/audio/gaze_release_sfx.py

Writes art/audio/new-gaze/: GazeLoad1..5 (charge absorbed: rising buzzy electrical swell + arc crackle + lock-in snap),
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
# Electrical power-up swell: buzzy rising saw + rising band noise + dense arc zaps + lock-in snap.
LOAD_DUR = {1: 0.34, 2: 0.36, 3: 0.39, 4: 0.42, 5: 0.60}
LOAD_SNAP = {1: 0.255, 2: 0.275, 3: 0.305, 4: 0.335, 5: 0.30}   # time the charge "locks in"
LOAD_F0 = {1: 140, 2: 172, 3: 210, 4: 255, 5: 305}             # buzz pitch at start (Hz)
LOAD_F1 = {1: 360, 2: 450, 3: 560, 4: 700, 5: 880}             # buzz pitch at snap (Hz)
LOAD_PEAK_DB = {1: -8.0, 2: -7.25, 3: -6.5, 4: -5.75, 5: -5.0}
LOAD_ZAPS = {1: 16, 2: 24, 3: 34, 4: 46, 5: 70}                # arc zaps in the swell
LOAD_STEP_DB = 1.5                                              # short-term loudness step per cue


def buzz_saw(n, f, maxhz=9000, wob=0.0, rng=None):
    """Harmonic-rich saw following f(t) (band-limited additive), with slow random pitch wobble."""
    t = np.arange(n) / RATE
    if wob and rng is not None:
        f = f * (1 + wob * np.sin(2 * np.pi * 47 * t + rng.uniform(0, 6)) + 0.5 * wob * np.sin(2 * np.pi * 113 * t))
    ph = np.cumsum(f) / RATE
    y = np.zeros(n)
    top = int(maxhz / f.max())
    for h in range(1, top + 1):
        y += np.sin(2 * np.pi * h * ph + 0.9 * h * h) / h ** 0.8
    return y


def zap_train(n, rng, count, t0, t1, bright):
    """Random arc zaps: short decaying noise bursts, density skewed late, brightness `bright` Hz."""
    out = np.zeros(n)
    u = rng.random(count) ** 0.7                      # skew toward the end of the window
    times = t0 + (t1 - t0) * u
    kern_n = int(0.006 * RATE)
    for tt in times:
        i = int(tt * RATE)
        L = int(rng.uniform(0.0008, 0.005) * RATE)
        k = rng.standard_normal(L) * np.exp(-np.arange(L) / (L / 3.0))
        k = np.diff(k, prepend=0.0)                   # tilt bright
        if rng.random() < 0.35:                       # forked double-zap
            k = np.concatenate([k, np.zeros(int(0.0012 * RATE)), 0.6 * k])
        amp = rng.uniform(0.3, 1.0) * (0.45 + 0.9 * (tt - t0) / max(1e-6, t1 - t0))
        m = min(len(k), n - i)
        if m > 0:
            out[i:i + m] += amp * k[:m]
    return fft_filter(out, bright, 15000)


def load_raw(tier):
    rng = np.random.default_rng(1200 + tier)
    dur, ts = LOAD_DUR[tier], LOAD_SNAP[tier]
    n = int(dur * RATE)
    t = np.arange(n) / RATE
    u = np.clip(t / ts, 0, 1)                         # swell progress 0..1 up to the snap
    # buzz pitch: exponential rise, then holds at top
    f = LOAD_F0[tier] * (LOAD_F1[tier] / LOAD_F0[tier]) ** (u ** 1.2)
    saw = buzz_saw(n, f, 9000, 0.012, rng)
    saw = np.tanh(1.8 * norm(saw))                    # extra grit / odd harmonics
    pulse = 0.5 + 0.5 * np.sign(np.sin(2 * np.pi * (70 + 150 * u) * t))   # rising-rate buzz gate
    saw *= 0.55 + 0.45 * pulse
    swell = u ** 1.6 * 0.85 + 0.15 * u                # capacitor-charge amplitude curve
    saw_l = saw * swell
    # rising noise: low band always, high band opens up with progress, floor raised per tier
    n_lo = fft_filter(rng.standard_normal(n), 250 + 80 * tier, 2200 + 500 * tier)
    n_hi = fft_filter(rng.standard_normal(n), 2500 + 500 * tier, 14000)
    noise = n_lo * (1.4 * u ** 1.3) + n_hi * ((0.25 + 0.2 * tier) * u ** 2.2)
    noise *= 1.0 + 0.8 * (rng.standard_normal(n) > 1.0).astype(float) * 0   # (kept deterministic, no-op)
    # existing-kit texture: charge-start sizzle, high passed, riding the swell
    try:
        sc = read(find("SpearChargeStart.wav"))
        off = int(rng.integers(0, max(1, len(sc) - n)))
        tex = fft_filter(sc[off:off + n], 1500 + 500 * tier, None)
        tex = np.pad(tex, (0, n - len(tex)))
        noise += 0.3 * norm(tex) * u ** 1.5
    except FileNotFoundError:
        pass
    zaps = zap_train(n, rng, LOAD_ZAPS[tier], 0.01, ts, 2500 + 700 * tier)
    # snap: bright electric crack + short noise burst where the charge locks in
    snap = np.zeros(n)
    si = int(ts * RATE)
    try:
        cr = crack_from("SpearBurst.wav", 0.03 + 0.004 * tier, 2200 + 400 * tier)
    except FileNotFoundError:
        cr = fft_filter(rng.standard_normal(int(0.03 * RATE)), 3000, None) * np.exp(-np.arange(int(0.03 * RATE)) / 300.0)
    m = min(len(cr), n - si)
    snap[si:si + m] = cr[:m]
    sn = fft_filter(rng.standard_normal(n), 3500, 15000) * env(np.maximum(t - ts, 0), 0.012) * (t >= ts)
    # tail after the snap: decaying buzz+noise (tiers 1-4), sustained crackling sizzle (tier 5)
    post = np.clip((t - ts) / 0.004, 0, 1) * (t >= ts)
    if tier < 5:
        tail = env(np.maximum(t - ts, 0), 0.03) * post
        tail_sz = 0.0
    else:
        tail = env(np.maximum(t - ts, 0), 0.22) * post
        sz = zap_train(n, rng, 60, ts, dur - 0.04, 5000)
        sz += 0.5 * fft_filter(rng.standard_normal(n), 4500, 14000) * (0.6 + 0.4 * np.sin(2 * np.pi * 38 * t))
        tail_sz = sz * tail
    pre = (t < ts).astype(float)
    body = (1.3 * saw_l + noise * 0.85 + (0.25 + 0.1 * tier) * zaps) * (pre + 0.55 * (1 - pre) * tail)
    y = norm(body) * 0.75 + (0.28 + 0.04 * tier) * norm(snap) + 0.08 * norm(sn) * (1 if tier < 5 else 0.8)
    y += (0.30 * norm(tail_sz) * (1 - pre) if tier == 5 else 0.0)
    return fft_filter(y, 120, None)


def loud100(x):
    w = int(0.1 * RATE)
    if len(x) <= w:
        return 20 * np.log10(np.sqrt(np.mean(x ** 2)) + 1e-12)
    c = np.concatenate([[0], np.cumsum(x ** 2)])
    r = (c[w:] - c[:-w]) / w
    return 10 * np.log10(r.max() + 1e-24)


def shape(y, tier, drive):
    y = y - y.mean()
    y = np.tanh(drive * norm(y)) if drive > 0 else norm(y)
    a = int(0.004 * RATE)
    y[:a] *= np.linspace(0, 1, a)
    b = int({5: 0.09}.get(tier, 0.035) * RATE)
    y[-b:] *= np.linspace(1, 0, b) ** 1.5
    return norm(y) * 10 ** (LOAD_PEAK_DB[tier] / 20)


def load_cues():
    """All five cues; soft-clip drive per tier is searched so loudest-100ms RMS rises ~1.5 dB per step."""
    raws = {k: load_raw(k) for k in range(1, 6)}
    base = loud100(shape(raws[1], 1, 0.8))
    out = {}
    for k in range(1, 6):
        target = base + LOAD_STEP_DB * (k - 1)
        best = None
        for d in np.arange(0.8, 8.01, 0.1):
            y = shape(raws[k], k, d)
            e = abs(loud100(y) - target)
            if best is None or e < best[0]:
                best = (e, y)
        out[k] = best[1]
    return out


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


MIX_LOAD_GAIN = 10 ** (0 / 20)  # final levels are set inside load_cues() (-8..-5 dBFS peaks)
MIX_HIT_GAIN = {1: 10 ** (-4.5 / 20), 2: 10 ** (-5.0 / 20), 3: 10 ** (-6.0 / 20)}


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    clips = {}
    for k, y in load_cues().items():
        clips[f"GazeLoad{k}"] = write(OUT / f"GazeLoad{k}.wav", y * MIX_LOAD_GAIN)
    for k in (1, 2, 3):
        # Tier 3 lands around SpearBurst; tiers keep ~3-4 dB steps.
        clips[f"GazeSurgeHit{k}"] = write(OUT / f"GazeSurgeHit{k}.wav", surge_hit(k) * MIX_HIT_GAIN[k])
    # audition: Load1..5 at 0.3 s spacing, 0.25 s gap, Hit3
    L = [clips[f"GazeLoad{k}"] for k in range(1, 6)]
    H3 = clips["GazeSurgeHit3"]
    start5 = 0.3 * 4
    end_loads = start5 + len(L[4]) / RATE
    mix = np.zeros(int((end_loads + 0.25 + len(H3) / RATE + 0.1) * RATE))
    for k in range(5):
        i = int(0.3 * k * RATE); mix[i:i + len(L[k])] += L[k]
    i = int((end_loads + 0.25) * RATE); mix[i:i + len(H3)] += H3
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
        print(n, "loud100=%.2f dB" % loud100(x), "DC=%.6f" % x.mean(), "first-sample=%.4f" % x[0])


if __name__ == "__main__":
    main()
