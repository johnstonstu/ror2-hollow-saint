"""Measure every logged sound cue inside a loopback recording of an autopilot run.
Usage: python analyze_capture.py artifacts/<run>   (expects game-audio.wav/.json + audio-events.txt)
Per event name: count, failed posts, median/max peak and 100 ms loudness in the cue window,
and 'rise' = cue loudness minus the 150 ms bed before it (how much it stands out in the mix).
Also: overall peak, overs (>= 0 dBFS), per-segment loudness, and cue density (stacking)."""
import collections, json, struct, sys, wave
from pathlib import Path
import numpy as np

run = Path(sys.argv[1])
meta = json.loads((run / 'game-audio.json').read_text())
raw = (run / 'game-audio.wav').read_bytes()
data_at = raw.index(b'data') + 8
audio = np.frombuffer(raw[data_at:], dtype='<f4').reshape(-1, meta['channels'])
mono = np.abs(audio).max(axis=1)
power = (audio ** 2).mean(axis=1)
rate, t0 = meta['rate'], meta['first_ms']
LAT = float(sys.argv[2]) if len(sys.argv) > 2 else None  # manual offset, s; default auto-calibrated

src = Path(__file__).resolve().parents[2] / 'art/audio/source'
durations = {}
for f in src.glob('*.wav'):
    with wave.open(str(f)) as w: durations['Play_HS_' + f.stem] = w.getnframes() / w.getframerate()

def db(x): return 20 * np.log10(max(float(x), 1e-9))
def loud(a, b):
    a, b = max(0, a), min(len(power), b)
    if b - a < 10: return -120.0
    win = int(rate * .1); seg = power[a:b]
    if len(seg) <= win: return 10 * np.log10(max(seg.mean(), 1e-12))
    c = np.convolve(seg, np.ones(win) / win, mode='valid')
    return 10 * np.log10(max(c.max(), 1e-12))

events, marks = [], []
for line in (run / 'audio-events.txt').read_text(encoding='utf-8', errors='replace').splitlines():
    p = line.split(' ')
    if p[0] == 'SND' and len(p) >= 6: events.append((int(p[1]), p[2], p[3], float(p[4]), p[5]))
    elif p[0] == 'MARK': marks.append((int(p[1]), ' '.join(p[2:])))

# The recorder's first-callback timestamp can be off by hundreds of ms (WASAPI start-up
# buffering). Calibrate: the offset that maximises summed onset energy at logged cue times.
sync = [ms for ms, label in marks if label == 'SYNC']
if LAT is None and sync:
    # Deterministic: the sharpest onset within +-2 s of the SYNC cue (SpearImpact after 1 s of quiet).
    s = (sync[0] - t0) / 1000.0
    hop = int(rate * .005)
    env = 10 * np.log10(np.convolve(power, np.ones(int(rate * .01)) / (rate * .01), mode='same')[::hop] + 1e-12)
    lo, hi = max(1, int((s - 2.0) * rate / hop)), min(len(env) - 1, int((s + 2.0) * rate / hop))
    rise = env[lo:hi] - env[lo - 1:hi - 1]
    LAT = (lo + int(np.argmax(rise))) * hop / rate - s
if LAT is None:
    env = 10 * np.log10(np.convolve(power, np.ones(int(rate * .02)) / (rate * .02), mode='same') + 1e-12)
    hop = int(rate * .01); e = env[::hop]; onset = np.maximum(0, np.diff(e, prepend=e[0]))
    times = np.array([(ms - t0) / 1000.0 for ms, name, *_ in events if name.startswith('Play_')])
    best, LAT = -1, 0.0
    for off in np.arange(-2.0, 2.0001, 0.01):
        idx = ((times + off) / .01).astype(int); idx = idx[(idx >= 0) & (idx < len(onset) - 3)]
        score = onset[idx].sum() + onset[idx + 1].sum() + onset[idx + 2].sum()
        if score > best: best, LAT = score, float(off)
print(f"alignment offset {LAT*1000:+.0f} ms")
stats = collections.defaultdict(list); failed = collections.Counter()
for ms, name, emitter, dist, status in events:
    if status == 'FAILED': failed[name] += 1
    s = (ms - t0) / 1000.0 + LAT
    if s < 0 or s * rate >= len(mono): continue
    dur = durations.get(name, .4)
    a, b = int((s - .03) * rate), int((s + dur + .12) * rate)
    bed = loud(int((s - .18) * rate), int((s - .03) * rate))
    l = loud(a, b)
    stats[name].append((db(mono[max(0, a):b].max()) if b > a else -120, l, l - bed, dist))

print(f"recording {meta['seconds']:.1f}s  device={meta['device']}")
print(f"overall peak {db(mono.max()):.1f} dBFS  overs(>=0dBFS) {int((mono >= 1.0).sum())} samples  "
      f"near-clip(>-1dBFS) {int((mono > 0.891).sum())}  mean loudness {10*np.log10(power.mean()+1e-12):.1f} dB")
print(f"{'event':34s} {'n':>4} {'fail':>4} {'peak':>6} {'loud':>6} {'rise':>6} {'maxpk':>6} {'dist':>5}")
for name, rows in sorted(stats.items(), key=lambda kv: -np.median([r[1] for r in kv[1]])):
    r = np.array(rows)
    print(f"{name[:34]:34s} {len(r):4d} {failed[name]:4d} {np.median(r[:,0]):6.1f} {np.median(r[:,1]):6.1f} "
          f"{np.median(r[:,2]):6.1f} {r[:,0].max():6.1f} {np.median(r[:,3]):5.1f}")
for name in failed:
    if name not in stats: print('FAILED only:', name, failed[name])
print('\nsegments:')
bounds = marks + [(int(t0 + meta['seconds'] * 1000), 'END')]
for (ms, label), (nxt, _) in zip(bounds, bounds[1:]):
    a, b = int((ms - t0) / 1000 * rate), int((nxt - t0) / 1000 * rate)
    if b <= a: continue
    n = sum(1 for e in events if ms <= e[0] < nxt)
    dens = n / max(.1, (nxt - ms) / 1000)
    print(f"  {label[:40]:40s} {(nxt-ms)/1000:5.1f}s peak {db(mono[a:b].max()):6.1f} loud {loud(a,b):6.1f} cues/s {dens:5.1f}")
# Worst stacking: max cues starting inside any 250 ms window.
ts = np.array(sorted(e[0] for e in events)); worst = 0; at = 0
for i in range(len(ts)):
    j = np.searchsorted(ts, ts[i] + 250)
    if j - i > worst: worst, at = j - i, ts[i]
if len(ts): print(f"\nworst stacking: {worst} cues within 250 ms at +{(at - t0)/1000:.1f}s: " +
                  ', '.join(e[1] for e in events if at <= e[0] < at + 250))
