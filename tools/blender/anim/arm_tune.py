"""Try arm-path variants against the clearance check without baking or rendering.

Run: blender --background --factory-startup --python tools/blender/anim/arm_tune.py -- <variants.json>
variants.json: [{"target": "run", "label": "...", "arm": {...overrides}}, ...]
targets: run, walk, back, left, right (patch run.ARM, walk.ARM, run_dirs.ARM_B, run_dirs.ARM_S).
Prints per variant the min clearance per side, frames under clearance.ARM_CLEAR_MIN, and the widest
elbow/hand offset from the body midline (to catch chicken-winged fixes).
"""
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
sys.path.insert(0, str(Path(__file__).parent/'clips'))
import hs_anim as H
import clearance
import run
import walk
import run_dirs

variants = json.loads(Path(sys.argv[sys.argv.index('--')+1]).read_text())
p = H.open_start()
clr = clearance.Clearance()


def target(name):
    if name == 'run':
        return run.ARM, run.pose, run.N
    if name == 'walk':
        return walk.ARM, walk.pose, walk.N
    if name == 'back':
        return run_dirs.ARM_B, run_dirs.pose_back, run_dirs.N
    k = 1 if name == 'left' else -1
    return run_dirs.ARM_S, (lambda pp, f: run_dirs.make_strafe(k)[0](pp, f)), run_dirs.N


results = []
for v in variants:
    arm, pose, n = target(v['target'])
    saved = dict(arm)
    arm.update(v.get('arm', {}))
    per = []
    spread = {'elbow': 0.0, 'hand': 0.0}
    for f in range(1, n+1):
        p.reset()
        p.ik(1.0, 0.0)
        pose(p, f)
        p.update()
        per.append((f, clr.frame()))
        for s in H.SIDES:
            spread['elbow'] = max(spread['elbow'], abs(p.world(f'{s} forearm').translation.x-H.MID_X))
            spread['hand'] = max(spread['hand'], abs(p.world(f'{s} hand').translation.x-H.MID_X))
    arm.clear()
    arm.update(saved)
    rep = clearance.summarize(per, 'locomotion')
    row = {'target': v['target'], 'label': v.get('label', ''), 'min_m': rep['arm_clear_min_m'],
           'L': {k: rep['arm_clearance']['L'][k] for k in ('min_m', 'frame', 'part', 'against', 'frames_under_min')},
           'R': {k: rep['arm_clearance']['R'][k] for k in ('min_m', 'frame', 'part', 'against', 'frames_under_min')},
           'L_mm': rep['arm_clearance']['L']['per_frame_mm'], 'R_mm': rep['arm_clearance']['R']['per_frame_mm'],
           'L_hits': rep['arm_clearance']['L']['hits'], 'R_hits': rep['arm_clearance']['R']['hits'],
           'spread_m': {k: round(x, 3) for k, x in spread.items()}}
    results.append(row)
    print('TUNE', json.dumps(row), flush=True)
