"""python inventory.py [tag]: clip inventory (module, frames, loop, kind, markers, seams) from art/anim/<tag>/catalog.json."""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
tag = sys.argv[1] if len(sys.argv) > 1 else 'v24'
for c in json.loads((ROOT/'art/anim'/tag/'catalog.json').read_text()):
    seams = {k: c[k] for k in ('seam_from', 'seam_to', 'run_frame_start', 'run_frame_end') if k in c}
    print(f"{c['module']:12s} {c['title']:24s} {c['frames']} loop={int(c['loop'])} {c.get('kind', ''):12s} "
          f"spd={c.get('speed_mps', '')} mk={c.get('markers')} {seams}")
