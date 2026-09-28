"""List clip frames where the heel jets/sparks are lit but the hs_move travel vector is shorter than unit length.
blender --background --factory-startup <blend> --python jet_move_check.py"""
import math
import bpy


def curve(act, name):
    path = f'pose.bones["root"]["{name}"]'
    fcs = [fc for layer in act.layers for strip in layer.strips for bag in strip.channelbags for fc in bag.fcurves]
    for fc in fcs:
        if fc.data_path == path:
            return fc
    return None


checked = 0
for act in bpy.data.actions:
    cj, cx, cy = curve(act, 'hs_jet'), curve(act, 'hs_move_x'), curve(act, 'hs_move_y')
    sl, sr = curve(act, 'hs_spark_L'), curve(act, 'hs_spark_R')
    if not (cj and cx and cy):
        continue
    checked += 1
    a, b = act.frame_range
    bad = []
    for f in range(int(a), int(b)+1):
        lit = cj.evaluate(f)+max(sl.evaluate(f) if sl else 0.0, sr.evaluate(f) if sr else 0.0)
        m = math.hypot(cx.evaluate(f), cy.evaluate(f))
        if lit > 0.05 and m < 0.99:
            bad.append((f, round(lit, 2), round(m, 2)))
    if bad:
        print('JETMOVE', act.name, len(bad), bad[:6], flush=True)
print('JETMOVE DONE', len(bpy.data.actions), 'actions,', checked, 'with jet+move curves', flush=True)
