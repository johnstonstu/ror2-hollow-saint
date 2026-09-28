"""Print handoff metrics of stitch outputs. python handoffs.py [dir-glob ...] (under art/anim/wip)."""
import json
import sys
from pathlib import Path

WIP = Path(__file__).resolve().parents[1]
pats = sys.argv[1:] or ['transitions/*', 'locomotion8/*']
for pat in pats:
    for d in sorted(WIP.glob(pat)):
        f = d/'stitch.json'
        if not f.exists():
            continue
        s = json.loads(f.read_text(encoding='utf-8'))
        for h in s['handoffs']:
            print(f"{s['name']:<34} f{h['frame']:<4} {h['from']:<28} -> {h['to']:<28} b{h['blend']:<2} "
                  f"{h['worst_bone']:<15} acc {1000*h['acc']:6.1f}mm clip {1000*h['clip_max_acc']:6.1f} "
                  f"r {h['ratio']}  rot {h.get('rot_bone', '-')} {h.get('rot_acc_deg', '-')}/{h.get('rot_clip_max_deg', '-')}")
