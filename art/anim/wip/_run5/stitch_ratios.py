"""python stitch_ratios.py <stitch folder...>: worst hand-off ratio per handoff."""
import json
import sys

for d in sys.argv[1:]:
    s = json.load(open(f'{d}/stitch.json'))
    for h in s.get('handoffs', []):
        items = {k: v for k, v in h.items() if not isinstance(v, (list, dict))}
        r = h.get('ratios') or h.get('bones') or {}
        worst = max(r.items(), key=lambda kv: kv[1] if isinstance(kv[1], (int, float)) else kv[1].get('ratio', 0),
                    default=None) if isinstance(r, dict) else None
        print(d.split('/')[-1], items, worst)
