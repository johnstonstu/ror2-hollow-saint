"""Per-sound in-game levels from an HS_SEGMENTS=audio-probe capture (clean room: music RTPC 0,
no monsters). Usage: python analyze_probe.py artifacts/<run>
Reports the SYNC alignment check, then for each probe: peak and 100 ms loudness in the cue
window vs the bed just before it, and the authored level (source WAV + Wwise trim)."""
import json, re, sys, wave
from pathlib import Path
import numpy as np

run = Path(sys.argv[1]); root = Path(__file__).resolve().parents[2]
meta = json.loads((run / 'game-audio.json').read_text()); raw = (run / 'game-audio.wav').read_bytes()
a = np.frombuffer(raw[raw.index(b'data') + 8:], dtype='<f4').reshape(-1, meta['channels'])
sr, t0 = meta['rate'], meta['first_ms']; p = (a ** 2).mean(1)
def loud(x0, x1):
    seg = p[max(0, x0):x1]; w = int(sr * .1)
    if len(seg) <= w: return -120.0
    return 10 * np.log10(np.convolve(seg, np.ones(w) / w, 'valid').max() + 1e-12)
def peak(x0, x1): return 20 * np.log10(np.abs(a[max(0, x0):x1]).max() + 1e-9)
src_ab = (root / 'tools/audio/author_bank.py').read_text(); block = src_ab[src_ab.index('MIX_TRIM_DB = {'):]; block = block[:block.index('}')]
trim = {k: float(v) for k, v in re.findall(r'"HS_(\w+)":\s*(-?[\d.]+)', block)}
marks = [(int(q[1]), ' '.join(q[2:])) for q in (l.split() for l in (run / 'audio-events.txt').read_text().splitlines()) if q and q[0] == 'MARK']
for ms, label in marks:
    if label == 'SYNC':
        s = int((ms - t0) / 1000 * sr); hop = int(sr * .005)
        env = 20 * np.log10(np.abs(a[s - sr:s + sr]).max(1) + 1e-9)
        e = np.array([env[i:i + hop].max() for i in range(0, len(env) - hop, hop)])
        print(f"SYNC onset at {(int(np.argmax(np.diff(e))) * hop - sr) / sr * 1000:+.0f} ms from its log stamp (expect ~0)")
rows = []
for ms, label in marks:
    if not label.startswith('PROBE '): continue
    name = label.split()[1]; s = int((ms - t0) / 1000 * sr)
    bed = loud(s - int(.45 * sr), s - int(.05 * sr)); L = loud(s, s + int(.8 * sr)); pk = peak(s, s + int(.8 * sr))
    authored = None
    f = root / 'art/audio/source' / (name + '.wav')
    if f.exists():
        with wave.open(str(f)) as w: x = np.frombuffer(w.readframes(w.getnframes()), dtype=np.int16).astype(float) / 32768
        wl = int(48000 * .1)
        authored = (10 * np.log10(np.convolve(x ** 2, np.ones(wl) / wl, 'valid').max() + 1e-12) if len(x) > wl else 10 * np.log10((x ** 2).mean())) + trim.get(name, 0)
    rows.append((name, pk, L, bed, authored))
print(f"{'probe':20s} {'peak':>6} {'loud':>6} {'bed':>6} {'rise':>6} {'authored':>8} {'in-game - authored':>18}")
for n, pk, L, bed, au in rows:
    print(f"{n:20s} {pk:6.1f} {L:6.1f} {bed:6.1f} {L-bed:6.1f} {('%8.1f' % au) if au is not None else '       -'} {('%18.1f' % (L-au)) if au is not None else ''}")
