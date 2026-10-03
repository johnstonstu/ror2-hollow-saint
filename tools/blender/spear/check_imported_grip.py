"""Check Unity-imported hand and grip surfaces, independent of Blender authoring data."""
import bpy
import json
import sys
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

assert bpy.app.background
ROOT = Path(__file__).resolve().parents[3]
args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
version = args[0] if args else '10'
assert version in ('10', '11', '11-animated')
data = json.loads((ROOT / f'artifacts/foundation/spear{version}-imported-geometry.json').read_text())

def tree(parts):
    vertices, triangles = [], []
    for part in parts:
        start = len(vertices)
        vertices.extend(Vector((v['x'], v['y'], v['z'])) for v in part['vertices'])
        indices = part['triangles']
        triangles.extend(tuple(start + k for k in indices[i:i+3]) for i in range(0, len(indices), 3))
    return BVHTree.FromPolygons(vertices, triangles, all_triangles=True), vertices

hand, _ = tree(data['hand'])
grip, vertices = tree(data['grip'])
overlap = len(hand.overlap(grip))
by_part = {p['name']: len(tree([p])[0].overlap(grip)) for p in data['hand']}
print('COLLISIONS_BY_PART', {k: v for k, v in by_part.items() if v}, flush=True)
clearance = min(hand.find_nearest(v)[3] for v in vertices)
result = {'overlap_face_pairs': overlap, 'nearest_sampled_vertex_mm': clearance * 1000,
          'hand_parts': len(data['hand']), 'fixed_finger_samples': data['fixedFingerSamples']}
(ROOT / f'artifacts/foundation/spear{version}-imported-grip-check.json').write_text(json.dumps(result, indent=2))
print('IMPORTED_GRIP_CHECK', result, flush=True)
assert overlap == 0, 'Grip intersects actual game-imported hand'
assert 0.0002 < clearance < 0.003, 'Grip no longer sits closely outside the hand'
