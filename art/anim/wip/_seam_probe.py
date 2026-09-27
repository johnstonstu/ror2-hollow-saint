"""Compare air.stand_pose with presentation Idle f1 (Land end / Jump start vs Idle seam). Background Blender."""
import sys
sys.path[:0] = ['tools/blender/anim', 'tools/blender/anim/clips']
import hs_anim as H
import air
import presentation as P

p = H.open_start()
P.arc_geometry(p)
P.STAND = P.stand_state()
out = {}
for name, fn in (('air', lambda: air.stand_pose(p)), ('idle', lambda: P.apply(p, P.idle_state(0.0)))):
    p.rig.animation_data.action = None
    p.reset()
    p.ik(1.0, 0.0)
    fn()
    p.update()
    out[name] = {b: p.world(b).translation.copy() for b in ('L foot IK', 'L knee pole', 'L shin', 'L thigh', 'L foot', 'pelvis',
                                                           'R shin', 'R knee pole')}
for b in out['air']:
    d = (out['air'][b]-out['idle'][b]).length*1000
    print('SEAMPROBE', b, round(d, 3), 'mm', tuple(round(v, 4) for v in out['air'][b]), tuple(round(v, 4) for v in out['idle'][b]))
