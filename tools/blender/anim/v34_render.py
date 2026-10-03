"""Low-res compare renders of a saved checkpoint (v34 arm pass review).
Run: blender --background --factory-startup --python tools/blender/anim/v34_render.py -- BLEND TAG OUTDIR [set]
Writes OUTDIR/TAG/<clip-slug>/<view>-<frame>.png.  set: 'main' (default) or 'extra'."""
import bpy, sys, re
from pathlib import Path
sys.path.insert(0, str(Path(__file__).parent))
import hs_anim as H
args = sys.argv[sys.argv.index('--')+1:]
bpy.ops.wm.open_mainfile(filepath=str(H.ROOT/args[0]))
tag, out = args[1], H.ROOT/args[2]
SETS = {
    'main': {'Idle': [1, 25, 49, 73], 'Idle combat': [1, 13, 25, 37], 'Arc Bolt left': [1, 3, 5, 9, 15],
             'Arc Bolt right': [1, 3, 5, 9, 15], 'Run forward': [1, 5, 9, 13], 'Glide loop': [1, 9, 17, 25]},
    'extra': {'Combat ready': [1, 5, 8, 11], 'Idle combat fidget': [1, 16, 24, 36], 'Conduit Spear': [1, 5, 7, 11, 20],
              'Discharge': [1, 7, 9, 15, 28], 'Open Circuit arms': [1, 4, 10, 15], 'Walk forward': [1, 7, 14, 20],
              'Land': [1, 4, 6, 15], 'Idle fidget 1': [1, 20, 38, 61]},
}
which = args[3] if len(args) > 3 else 'main'
rig = bpy.data.objects[H.RIG]
for t in rig.animation_data.nla_tracks:
    t.mute = True
scene = bpy.context.scene
H.eevee(scene, 4)
VIEWS = {'front': 'Front orthographic', 'hero': 'Hero three quarter', 'side': 'Side orthographic'}
for title, frames in SETS[which].items():
    a = bpy.data.actions.get(H.PREFIX+title)
    if a is None:
        print('MISSING', title)
        continue
    rig.animation_data.action = a
    rig.animation_data.action_slot = a.slots[0]
    d = out/tag/re.sub(r'[^a-zA-Z0-9]+', '_', title)
    d.mkdir(parents=True, exist_ok=True)
    for v, cam in VIEWS.items():
        if cam not in bpy.data.objects:
            print('NOCAM', cam)
            continue
        for f in frames:
            scene.frame_set(f)
            H.render_still(scene, cam, d/f'{v}-{f:03}.png', (200, 250))
    print('RENDERED', title, flush=True)
print('RENDER DONE', flush=True)
