"""List every HS_anim action in v31 with frames, loop flag and markers (read-only)."""
import json
from pathlib import Path
import bpy

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / 'art/anim/hollow-saint-anim-v31.blend'
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
out = []
for action in sorted(bpy.data.actions, key=lambda a: a.name):
    if not action.name.startswith('HS_anim | '):
        continue
    info = json.loads(action['clip_json']) if 'clip_json' in action else {}
    out.append({'title': action.name[len('HS_anim | '):], 'frames': info.get('frames'),
                'loop': info.get('loop'), 'markers': info.get('markers', {})})
(ROOT / 'artifacts/anim-clip-list-v31.json').write_text(json.dumps(out, indent=1))
print('CLIPS', len(out))
