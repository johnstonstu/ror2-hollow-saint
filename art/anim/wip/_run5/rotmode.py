import bpy, sys
bpy.ops.wm.open_mainfile(filepath=sys.argv[sys.argv.index('--')+1])
rig = bpy.data.objects['Hollow Saint | v8 rig']
from collections import Counter
print('MODES', Counter(pb.rotation_mode for pb in rig.pose.bones))
print('NONQ', [pb.name for pb in rig.pose.bones if pb.rotation_mode != 'QUATERNION'][:40])
for n in ('L thigh','R thigh','L shin','R shin','L foot','R foot','L toe','R toe','L upperarm','R upperarm','pelvis'):
    b = rig.data.bones[n]
    print('BONE', n, [round(x,4) for x in b.head_local], [round(x,4) for x in b.tail_local], round(b.length,4), [round(x,3) for x in b.matrix_local.to_3x3().col[0]])
