"""Torso cross-section extents at rest by height band (for the special.solve avoidance capsule)."""
import bpy
import sys
from pathlib import Path

TOOLS = Path(__file__).resolve().parents[4]/'tools/blender/anim'
sys.path.insert(0, str(TOOLS))
import hs_anim as H
import clearance

p = H.open_start()
body = bpy.data.objects[H.BODY]
dom = clearance.dominant_groups(body)
vs = [(body.matrix_world @ v.co, dom[i]) for i, v in enumerate(body.data.vertices)]
for z in (0.9, 1.0, 1.1, 1.2, 1.3, 1.4, 1.5):
    band = [v for v, g in vs if abs(v.z-z) < 0.03 and g in ('pelvis', 'spine', 'chest')]
    if band:
        xs = [v.x for v in band]
        ys = [v.y for v in band]
        print(f'BAND z={z} n={len(band)} x[{min(xs):.3f},{max(xs):.3f}] y[{min(ys):.3f},{max(ys):.3f}]', flush=True)
for o in bpy.data.objects:
    if o.type == 'MESH' and o.name.startswith(('ABDOMEN |', 'CHEST |')):
        ps = [o.matrix_world @ v.co for v in o.data.vertices]
        print('PLATE', o.name[:40], f'y[{min(v.y for v in ps):.3f},{max(v.y for v in ps):.3f}] z[{min(v.z for v in ps):.3f},{max(v.z for v in ps):.3f}]', flush=True)
