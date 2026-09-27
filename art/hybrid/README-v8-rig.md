# Hollow Saint v8 rig and animation study

First deformation-rig pass built from the preserved hybrid v7 scene. The original
v7 model, renders, and source remain unchanged. Open
[hollow-saint-hybrid-v8.blend](hollow-saint-hybrid-v8.blend) in Blender 5.2.

## Included

- A 54-bone armature with torso, head, arms, hands/fingers, legs, and four halo
  segment controls.
- Three editable actions: `HS_v8 | Idle contained storm | 2s loop`,
  `HS_v8 | Arc Bolt point and snap`, and
  `HS_v8 | Overcharge brace and release`.
- A two-second loop range at 24 fps, plus key-pose preview renders in
  [v8-review](v8-review/).
- Rebuild script: [rig_hybrid_v8.py](../../tools/blender/rig_hybrid_v8.py).

The charge/release action follows the charge-state board and uses restrained
body bracing, a right-hand discharge, and a return pose. The Arc Bolt action uses
an anticipatory lift, point, and recovery.

## Review limits

This is a first-pass animation study, not a production or game-ready rig. The
original body is one fused triangular mesh, so it uses approximate nearest-bone
weights; several accessories are rigidly attached to bones. The weights and
posed silhouette need further review, and the hands, feet, halo, and tabard need
contact/intersection checks across the complete motion. The animations have not
been tested in Unity or exported as a game asset. Hybrid v7 remains the current
visual-model checkpoint.

## Rebuild

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python-exit-code 1 --python tools/blender/rig_hybrid_v8.py
```

Render the three low-cost review poses with `tools/blender/preview_hybrid_v8.py`.
