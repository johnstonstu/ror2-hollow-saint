import bpy, json
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(root/'art/comparison/higgsfield-sam3d-review.blend'))
obj=bpy.data.objects['geometry_0']
verts=[obj.matrix_world@v.co for v in obj.data.vertices]
def stats(points):
    if not points:return None
    return {'n':len(points),'min':[min(p[i] for p in points) for i in range(3)],
            'max':[max(p[i] for p in points) for i in range(3)],
            'mean':[sum(p[i] for p in points)/len(points) for i in range(3)]}
report={}
for lo,hi in [(2.0,2.21),(1.85,2.0),(1.7,1.85),(1.6,1.7),(1.5,1.6),(.96,1.0),(.9,.96),(.7,.9)]:
    report[str((lo,hi))]={'all':stats([v for v in verts if lo<v.z<hi]),
        'outer':stats([v for v in verts if lo<v.z<hi and abs(v.x)>.25]),
        'center':stats([v for v in verts if lo<v.z<hi and abs(v.x)<.13]),
        'hands':stats([v for v in verts if lo<v.z<hi and abs(v.x)>.38])}
report['halo_above_head']=stats([v for v in verts if v.z>2.0])
out=root/'art/hybrid'
out.mkdir(parents=True,exist_ok=True)
(out/'source-spatial-inspection.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
