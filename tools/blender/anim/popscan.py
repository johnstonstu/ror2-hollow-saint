"""Pop scan (per-bone second differences) of clip modules on a fresh v18 (never saved).

Run:
  blender --background --factory-startup --python-exit-code 1 --python tools/blender/anim/popscan.py -- run walk
Prints the worst rotation (deg) and world-position (mm) pops per clip.
"""
import importlib
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
sys.path.insert(0, str(Path(__file__).parent/'clips'))
import hs_anim as H
from arcstep import motion_report

modules = sys.argv[sys.argv.index('--')+1:]
p = H.open_start()
for m in modules:
    built = importlib.import_module(m).build(p)
    for title, r in motion_report(p, built, top=3).items():
        print('POP', title, 'rot', r['rot'][:3], 'pos', r.get('pos', [])[:3], flush=True)
