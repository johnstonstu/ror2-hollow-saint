# Hollow Saint v10 custom animation studies

Open [the motion player](v10-animation/review.html) to select, play, slow down,
and scrub all six clips. [Editable Blender scene](hollow-saint-hybrid-v10.blend).
All previous v7–v9 scenes are preserved byte-for-byte.

| Action | Duration at 24 fps | Intent |
|---|---:|---|
| Idle — contained storm | 2 s loop | Quiet breath, head counter-motion, subtle halo movement |
| Arc Bolt — two finger snap | 1 s | Anticipation, forward two-finger release, recoil, recovery |
| Charge — gathering current | 2 s loop | Cupped hands with a restrained halo pulse |
| Discharge — break the seal | 2 s | Gather, bilateral release, settle |
| Arc Step — in place dash | 1 s | Forward brace, arms trailing, recovery; no travel or leg cycle |
| Open Circuit — unfolding crown | 3 s | Four existing arcs unfold above the head and return |

These timings and supporting ability motions are proposals. No gameplay balance
or implementation is implied. The three old v8 actions remain for reference;
use the six `HS_v10` actions with the revised rig.

## Rig repairs

- Detached studio cameras/lights from the chest; they no longer move with it.
- Fitted digit bone endpoints to the actual transformed hand segments.
- Recentered each halo pivot; lips and cyan bands follow the corresponding arc.
- Replaced unrestricted nearest-bone skinning with topology-isolated forearms
  and regional influence sets. This removes the observed hand-driven thigh/skirt
  spikes. Mesh geometry and texture are preserved.
- Keyed location, quaternion rotation, and scale on every bone at every authored
  pose, preventing state carryover when switching actions.
- Added action event markers and clamped curve handles.

## Review and limits

The saved file was reopened and 270 animation frames checked for finite body
geometry, stationary studio objects, visible motion, matching endpoint poses,
normalized skin weights, and stationary lower legs. See
[numerical results](v10-animation/qa.json). Hero and side key-pose renders and
12 fps motion previews accompany the 24 fps editable actions.

This remains a study rig. Shoulder and elbow deformation is limited by the fused,
coarse source mesh; there are no corrective shapes, IK controls, cloth simulation,
run cycle, lightning VFX, or export/game tests. The halo crown is a first visual
proposal. Broad animation direction still needs user review.

## Editing and reproduction

In Blender, select `Hollow Saint | v8 rig` (name retained deliberately), open the
Dope Sheet's Action Editor, and select an `HS_v10` action. Set scene playback range
to 1–48 for either loop; one-shots end at frame 25, 49, or 73 as shown by their
action range. Loop actions include a duplicate endpoint at frame 49.

Run with Blender 5.2 background mode, in order:

1. `tools/blender/animate_hybrid_v10.py` — rebuild from preserved v9.
2. `tools/blender/review_animation_v10.py -- --motion` — saved-file QA and renders.
3. `tools/blender/package_animation_review_v10.py` — normal Python with Pillow.

The build may report a thumbnail-cache write warning; the actual scene save and
independent reopen succeed. Review evidence is local, not an in-game capture.
