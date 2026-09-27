"""Per-frame tabard solve: clip bend (limited), raw swing, limit (top) and final smoothed swing, per bone.
blender -b --factory-startup --python tabard_hist.py -- <module> "Title A;Title B" "bone a;bone b" """
import importlib
import math
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[4]/'tools/blender/anim'))
sys.path.insert(0, str(Path(__file__).resolve().parents[4]/'tools/blender/anim/clips'))
import hs_anim as H
import tabardpass

args = sys.argv[sys.argv.index('--')+1:]
titles = set(args[1].split(';'))
bones = args[2].split(';')
orig = tabardpass.TabardPass.finish


def finish(self, caps, frames, loop, pins=()):
    out = orig(self, caps, frames, loop, pins)
    rows = []
    for i, f in enumerate(frames):
        row = []
        for b in bones:
            loc, q, sw, top = self.hist[i][b]
            _, bend = tabardpass.split_x(q)
            row.append(f'{b}: bend {bend:6.1f} raw {sw:6.1f} top {top:5.1f}')
        rows.append(f'  f{f:3d} ' + ' | '.join(row))
    self.dump = (list(pins), rows)
    return out


tabardpass.TabardPass.finish = finish
orig_apply = tabardpass.TabardPass.apply
WHO = [s for s in (args[3].split(';') if len(args) > 3 else [])]


def apply(self):
    if WHO and len(self.hist)+1 in {int(w) for w in WHO}:
        import numpy as np
        import fullqa
        deform = {b.name for b in self.p.rig.data.bones if b.use_deform}
        dom = fullqa.dominant(self.body, deform)
        self.p.update()
        P = self.skin()
        for n in bones:
            m = np.linalg.inv(np.array(self.p.pb[n].matrix))
            v = self.cells[n].violation(P@m[:3, :3].T+m[:3, 3])-np.maximum(np.nan_to_num(self.rest_v[n], nan=0.0), 0)
            order = [i for i in np.argsort(-np.nan_to_num(v, nan=-9)) if v[i] > 0][:6]
            print('WHO f', len(self.hist)+1, n, [(dom[self.ids[i]], round(float(v[i])*1000, 1)) for i in order])
    return orig_apply(self)


tabardpass.TabardPass.apply = apply
p = H.open_start()
mod = importlib.import_module(args[0])
orig_bake = H.bake


def bake(poser, title, *a, **k):
    r = orig_bake(poser, title, *a, **k)
    if title in titles and hasattr(poser.tabardpass, 'dump'):
        pins, rows = poser.tabardpass.dump
        print('CLIP', title, 'pins', pins)
        print('\n'.join(rows))
    return r


H.bake = bake
mod.bake = bake
mod.build(p)
