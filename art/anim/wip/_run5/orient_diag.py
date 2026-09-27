"""Print handpass per-frame orientation roll (raw solve, before finish's blur) for every clip a module bakes.
blender --background --factory-startup --python art/anim/wip/_run5/orient_diag.py -- arcstep_dirs"""
import sys
from pathlib import Path

root = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(root/'tools/blender/anim'))
sys.path.insert(0, str(root/'tools/blender/anim/clips'))
import importlib
import hs_anim as H
import handpass

orig = handpass.HandPass.finish


def finish(self, caps, frames, loop, pins=()):
    for f, h in zip(frames, self.hist):
        print('ROLL', f, ' '.join(f'{k}={v:.1f}' for k, v in sorted(h.items()) if isinstance(v, float)), flush=True)
    return orig(self, caps, frames, loop, pins)


handpass.HandPass.finish = finish
_bake = H.bake


def bake(p, title, *a, **k):
    print('CLIP', title, flush=True)
    return _bake(p, title, *a, **k)


H.bake = bake
mod_name = sys.argv[sys.argv.index('--')+1]
p = H.open_start()
mod = importlib.import_module(mod_name)
mod.bake = bake
mod.build(p)
