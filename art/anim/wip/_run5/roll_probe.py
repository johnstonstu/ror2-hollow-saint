"""Handpass orientation roll per frame (raw frame_roll hist) and the blurred delta, for the last clip a module bakes
with the given title. blender -b --factory-startup --python roll_probe.py -- <module> "<title>" "<bone;bone>" """
import bpy
import importlib
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[4]/'tools/blender/anim'))
sys.path.insert(0, str(Path(__file__).resolve().parents[4]/'tools/blender/anim/clips'))
import hs_anim as H
import handpass

args = sys.argv[sys.argv.index('--')+1:]
title, bones = args[1], args[2].split(';')
seen = {}
orig = handpass.HandPass.finish


def spy(self, caps, frames, loop, pins=()):
    seen['hist'] = [dict(h) for h in self.hist]
    seen['frames'] = list(frames)
    return orig(self, caps, frames, loop, pins)


handpass.HandPass.finish = spy
p = H.open_start()
mod = importlib.import_module(args[0])
real_bake = H.bake


def bake_spy(p_, t, *a, **k):
    out = real_bake(p_, t, *a, **k)
    if t == title:
        seen['keep'] = (seen['hist'], seen['frames'])
    return out


mod.bake = bake_spy
mod.build(p)
hist, frames = seen['keep']
for b in bones:
    print('ROLL', b, [round(h.get(b, 0.0), 1) for h in hist])
for s in ('L', 'R'):
    tot = [h.get(f'{s} forearm', 0.0)+h.get(f'{s} upperarm', 0.0) for h in hist]
    for i, (h, t) in enumerate(zip(hist, tot)):
        bd = h.get(f'{s} band')
        if bd is not None or abs(t) > 0.05:
            print(f'BAND {s} f{frames[i]:2d} raw {t:7.1f} band', None if bd is None else (round(bd[0], 1), round(bd[1], 1)))
