"""Per-pair velocity / acceleration jump report for the item 10 transition matrix.
python jumps.py [--glob 'transitions/matrix/*'] --md <out.md>
Per handoff: root travel speed before/after (m/s, 3 frames out) and the peak root acceleration (m/s^2) across it;
the worst tracked bone's peak acceleration vs the clips' own peak and the eased-pose-change allowance; the worst
rotation pop; the travel-direction turn (deg/frame)."""
import json
import sys
from pathlib import Path

WIP = Path(__file__).resolve().parents[1]
pat = sys.argv[sys.argv.index('--glob')+1] if '--glob' in sys.argv else 'transitions/matrix/*'
out = Path(sys.argv[sys.argv.index('--md')+1])
lines = ['| Pair | Frame | Blend | From -> To | Speed before -> after m/s | Root acc m/s^2 | Travel turn deg/f | '
         'Worst bone | Acc mm/f^2 | Clip peak | Ease allowance | Rot pop deg/f^2 (bone) |',
         '|---|---|---|---|---|---|---|---|---|---|---|---|']
n = 0
for d in sorted(WIP.glob(pat)):
    f = d/'stitch.json'
    if not f.exists():
        continue
    s = json.loads(f.read_text(encoding='utf-8'))
    for h in s['handoffs']:
        n += 1
        sp = f"{h['speed_before']} -> {h['speed_after']}" if 'speed_before' in h else '-'
        lines.append(f"| {s['name']} | {h['frame']} | {h['blend']} | {h['from']} -> {h['to']} | {sp} | "
                     f"{h.get('root_acc_mps2', '-')} | {h.get('travel_turn_deg', '-')} | {h['worst_bone']} | "
                     f"{1000*h['acc']:.0f} | {1000*h['clip_max_acc']:.0f} | {1000*h.get('ease_acc', 0):.0f} | "
                     f"{h.get('rot_acc_deg', '-')} ({h.get('rot_bone', '-')}) |")
out.write_text('\n'.join(lines)+'\n', encoding='utf-8')
print(n, 'handoffs ->', out)
