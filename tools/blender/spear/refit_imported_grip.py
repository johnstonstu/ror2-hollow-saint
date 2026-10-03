"""Relieve the two actual Unity hand contacts; export an explicit triangle mesh.

Uses Unity world coordinates and its exact inverse renderer matrix, avoiding a
second FBX axis conversion. Success: zero hand intersections, close clearance,
one closed grip component. Background only; preserves the original export.
"""
import json
from pathlib import Path
import bpy
import bmesh
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

assert bpy.app.background
ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'art/concepts/spear-runtime-fit01'
assert not OUT.exists(), 'Preserve completed mold; choose a new output directory'
OUT.mkdir(parents=True)
data = json.loads((ROOT / 'artifacts/foundation/spear10-imported-geometry.json').read_text())
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

def coords(part):
    return [Vector((v['x'], v['y'], v['z'])) for v in part['vertices']]

def triangles(part):
    t = part['triangles']
    return [tuple(t[i:i+3]) for i in range(0, len(t), 3)]

def tree(part):
    return BVHTree.FromPolygons(coords(part), triangles(part), all_triangles=True)

part = data['grip'][0]
mesh = bpy.data.meshes.new('Imported fitted grip')
mesh.from_pydata(coords(part), [], triangles(part))
grip = bpy.data.objects.new('Imported fitted grip', mesh)
bpy.context.scene.collection.objects.link(grip)
bm = bmesh.new()
bm.from_mesh(mesh)
# Unity splits vertices at normal seams; restore solid connectivity for Boolean.
bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=0.000001)
bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
bm.to_mesh(mesh)
bm.free()
before_tree = tree(part)
affected = [p for p in data['hand'] if tree(p).overlap(before_tree)]
assert {p['name'] for p in affected} == {'R HAND | ring segment 1', 'R HAND | wrist cuff'}
for hand in affected:
    bm = bmesh.new()
    for point in coords(hand):
        for x in (-1, 1):
            for y in (-1, 1):
                for z in (-1, 1):
                    bm.verts.new(point + Vector((x, y, z)) * 0.0012)
    hull = bmesh.ops.convex_hull(bm, input=list(bm.verts))
    discarded = set(hull['geom_interior'] + hull['geom_unused'])
    bmesh.ops.delete(bm, geom=list(discarded), context='VERTS')
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    cutter_mesh = bpy.data.meshes.new(hand['name'])
    bm.to_mesh(cutter_mesh)
    bm.free()
    cutter = bpy.data.objects.new(hand['name'], cutter_mesh)
    bpy.context.scene.collection.objects.link(cutter)
    boolean = grip.modifiers.new('Imported hand relief', 'BOOLEAN')
    boolean.operation, boolean.solver, boolean.object = 'DIFFERENCE', 'EXACT', cutter
    bpy.context.view_layer.objects.active = grip
    grip.select_set(True)
    bpy.ops.object.modifier_apply(modifier=boolean.name)
    cutter.hide_render = True
    cutter.hide_set(True)

bm = bmesh.new()
bm.from_mesh(grip.data)
bmesh.ops.triangulate(bm, faces=list(bm.faces))
bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
assert all(e.is_manifold for e in bm.edges), 'Grip must remain a closed solid'
bm.to_mesh(grip.data)
bm.free()
world = [v.co.copy() for v in grip.data.vertices]
indices = [tuple(p.vertices) for p in grip.data.polygons]
grip_tree = BVHTree.FromPolygons(world, indices, all_triangles=True)
overlaps = {p['name']: len(tree(p).overlap(grip_tree)) for p in data['hand']}
assert sum(overlaps.values()) == 0, overlaps
clearance = min(tree(p).find_nearest(v)[3] for p in data['hand'] for v in world)
assert 0.0002 < clearance < 0.003, clearance
values = part['worldToLocal']
inverse = Matrix([[values[f'e{i}{j}'] for j in range(4)] for i in range(4)])
local = [inverse @ v for v in world]
result = {'vertices': [dict(zip(('x', 'y', 'z'), v)) for v in local],
          'triangles': [i for face in indices for i in face]}
(OUT / 'grip-local.json').write_text(json.dumps(result), encoding='utf-8')
report = {'relieved_contacts': [p['name'] for p in affected],
          'overlap_face_pairs': sum(overlaps.values()),
          'nearest_sampled_vertex_mm': clearance * 1000,
          'vertices': len(local), 'triangles': len(indices), 'closed_solid': True,
          'fixed_finger_samples': data['fixedFingerSamples']}
(OUT / 'fit-report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'imported-grip-refit.blend'))
print('IMPORTED_GRIP_REFIT', report, flush=True)
