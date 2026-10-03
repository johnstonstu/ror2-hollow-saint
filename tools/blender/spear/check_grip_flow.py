"""Independent BVH check of native Unity baked hand/grip surfaces after pose layers."""
import bpy
import json
import sys
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

assert bpy.app.background, 'Run in an isolated background Blender'
args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
folder = args[0] if args else 'grip-flow02'
assert folder in ('grip-flow01', 'grip-flow02', 'grip-flow03')
ROOT = Path(__file__).resolve().parents[3] / 'artifacts' / folder


def tree(parts):
    vertices, triangles = [], []
    for part in parts:
        start = len(vertices)
        vertices.extend(Vector((v['x'], v['y'], v['z'])) for v in part['vertices'])
        indices = part['triangles']
        triangles.extend(tuple(start + k for k in indices[i:i+3]) for i in range(0, len(indices), 3))
    return BVHTree.FromPolygons(vertices, triangles, all_triangles=True), vertices


results = []
for path in sorted(ROOT.glob('life*.json')):
    data = json.loads(path.read_text())
    hand, _ = tree(data['hand'])
    grip, vertices = tree(data['grip'])
    overlap = len(hand.overlap(grip))
    clearance = min(hand.find_nearest(v)[3] for v in vertices) * 1000
    collisions = {p['name']: count for p in data['hand'] if (count := len(tree([p])[0].overlap(grip)))} if overlap else {}
    results.append({'label': data['label'], 'overlap_face_pairs': overlap, 'nearest_sampled_vertex_mm': clearance, 'collisions': collisions})
    if overlap:
        print('GRIP_INTERSECTION', results[-1], flush=True)

summary = {'samples': len(results), 'intersecting_samples': sum(r['overlap_face_pairs'] > 0 for r in results),
           'minimum_sampled_clearance_mm': min(r['nearest_sampled_vertex_mm'] for r in results), 'results': results}
(ROOT / 'surface-check.json').write_text(json.dumps(summary, indent=2))
print('GRIP_FLOW_CHECK', {k: v for k, v in summary.items() if k != 'results'}, flush=True)
assert len(results) == 240, 'Incomplete geometry export'
assert summary['intersecting_samples'] == 0, 'Grip intersects baked hand after procedural pose'
assert all(0.2 < r['nearest_sampled_vertex_mm'] < 3 for r in results), 'Grip clearance outside fitted range'
(ROOT / 'verification.txt').write_text('ALL PASS\n240 baked poses, 27 hand parts each, no triangle-surface intersections.\nMinimum sampled grip-vertex clearance: ' + str(summary['minimum_sampled_clearance_mm']) + ' mm.\nNative motion/arm/aim layers; gameplay not tested.\n')
