"""v36 ring-fed throw using the existing v35 IK authoring helpers.

Run in background Blender. Refuses to overwrite the immutable v36 checkpoint.
"""
import ast
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SOURCE = Path(__file__).with_name("armpass_v35.py")
OUT = ROOT / 'art/anim/hollow-saint-anim-v36.blend'
assert not OUT.exists(), f'Refusing to overwrite {OUT}'
sys.path.insert(0, str(SOURCE.parent))
sys.argv = [str(SOURCE), '--', str(OUT)]
tree = ast.parse(SOURCE.read_text(encoding='utf-8'))
nodes = []
for node in tree.body:
    if isinstance(node, ast.Expr) and isinstance(node.value, ast.Call):
        call = node.value.func
        if isinstance(call, ast.Name) and call.id == 'arm_ik_off':
            break
    nodes.append(node)
ns = {'__file__': str(SOURCE), '__name__': 'throw_study_helpers'}
exec(compile(ast.Module(body=nodes, type_ignores=[]), str(SOURCE), 'exec'), ns)

# A side-to-forward sweep, with the hand trailing the elbow before release.
# First/last poses and the frame-5 release remain on the current game contract.
ns.update(
    WRIST_KEYS={
        2: (-0.24, 0.08, -0.33),
        3: (-0.15, 0.25, -0.14),
        4: (-0.05, 0.38, -0.04),
        5: ('ext', 0.89, 0.015),
        6: ('ext', 0.97, 0.035),
        7: ('ext', 0.94, 0.035),
        8: ('ext', 0.87, 0.025),
        9: ('ext', 0.80, 0.01),
        10: (-0.17, 0.37, -0.015),
        12: (-0.28, 0.17, -0.13),
    },
    PUSH_DIR=(-0.08, 1.0),
    POLE_PUSH=(-0.50, -0.25, -1.0),
    HAND_W={1: 0.0, 2: 0.06, 3: 0.20, 4: 0.57, 5: 1.0},
    FINGER_DIR={
        3: (0.05, 0.75, 0.40), 4: (0.04, 0.92, 0.28),
        5: (0.02, 0.985, 0.15), 6: (0.0, 0.995, 0.10),
        7: (0.02, 0.97, 0.17), 8: (0.04, 0.91, 0.25),
        9: (0.08, 0.83, 0.35),
    },
    PALM_N={3: (0.20, 0.30, -0.90), 4: (0.12, 0.25, -0.95)},
    PALM_DEFAULT=(0.08, 0.20, -0.97),
    ROLL_SHARE=0.35,
    ROLL_W={1: 0.0, 2: 0.05, 3: 0.25, 4: 0.65, 5: 1.0,
            9: 0.95, 10: 0.80, 18: 0.0, 19: 0.0},
    FINGERS={
        2: (9.0, 9.0, 0.0, 0.25), 3: (12.0, 10.0, 0.0, 0.60),
        4: (7.0, 6.0, 0.06, 0.90), 5: (-3.0, -2.0, 0.20, 1.0),
        6: (-6.0, -3.0, 0.30, 1.0), 7: (-3.0, -1.0, 0.25, 1.0),
        8: (1.0, 1.0, 0.16, 1.0), 9: (5.0, 3.0, 0.08, 1.0),
    },
    YAW_PEAK=9.0, LEAN_PEAK=1.2,
    OFF_SWING=5.0, OFF_FLEX=4.0,
    MUZZLE_W={1: 0.0, 2: 0.0, 3: 0.0, 4: 0.0, 5: 0.0, 9: 0.0, 13: 0.0},
)
ns['arm_ik_off']()
for title, side in (('Arc Bolt right', 'R'), ('Arc Bolt left', 'L')):
    ns['author'](title, side)
    action = ns['bpy'].data.actions[ns['H'].PREFIX + title]
    info = json.loads(action['clip_json'])
    info.pop('v35', None)
    info['concept'] = 'ring-fed side sweep / forward throw, v36'
    action['clip_json'] = json.dumps(info)
ns['rig'].animation_data.action = None
ns['bpy'].ops.wm.save_as_mainfile(filepath=str(OUT))
report_dir = ROOT / 'art/anim/v36'
report_dir.mkdir(parents=True, exist_ok=True)
(report_dir / 'author-report.json').write_text(
    json.dumps(ns['REPORT'], indent=2), encoding='utf-8')
print('THROW_STUDY_SAVED', str(OUT), flush=True)
