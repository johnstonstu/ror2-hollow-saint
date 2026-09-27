# Hollow Saint v12 pauldron pass

Continues the v11 fidelity pass toward LEFT A. The concept's broad, angular
ivory shoulder plates were the largest remaining silhouette gap.

Open [the review page](v12/review.html) or the
[concept / v11 / v12 sheet](v12/comparison.jpg).
Editable scene: [hollow-saint-hybrid-v12.blend](hollow-saint-hybrid-v12.blend).

Built from v11, so all v11 detail, the v8 rig and the six `HS_v10` actions are
retained. v1–v10 files are byte-for-byte unchanged (SHA-256 checked); v11 was only
opened, never saved.

## Changes

- **Pauldrons:** `L/R SHOULDER | V12 pauldron`, procedural ellipsoid shells sized
  from the measured shoulder caps, broadened (x 1.18, y 1.25, z 1.05), grown until
  they enclose the cap, tilted 16° outward and flattened on top. Solidify (14 mm)
  plus an angle-limited bevel with hardened normals gives clean plate edges.
- **Material:** `V12 ivory ceramic plate`, subtle noise-varied ivory matching the
  body paint.
- **Skinning:** weights interpolated from the body, restricted to
  chest/spine/neck/upper-arm/scapula groups so the shells stay rigid-ish.

Earlier attempts (open roof plates, copied/decimated body faces, smoothed body
copies) read as hoops, spiked or crumpled and were discarded.

## QA

`review_hybrid_v11.py` now takes `HS_VERSION` and checks objects tagged v11 up to
that version. [v12 QA](v12/qa.json): PASS. Decal drift is identical to v11
(max 8.3 mm, Arc Step abdomen line). Pauldron drift is reported separately
because a rigid shell is expected to lift off the skin; worst is 17 mm at
Arc Step frame 9. [Pose check renders](v12/review.html) at the worst frames
show no visible clipping.

## Known limits

- Shells are smooth ellipsoid segments, not the concept's multi-plate layered
  pauldrons; a layered or trim-edged plate is a possible v13 step.
- Extreme overhead arm poses have not been authored, so shoulder clipping there is
  untested.
- Blender studio evidence only; no export or in-game test.

## Reproduction (Blender 5.2, background only)

1. `tools/blender/build_hybrid_v12.py`: rebuilds from v11 and renders all views.
2. `HS_VERSION=v12` + `tools/blender/review_hybrid_v11.py`: read-only saved-file QA.
3. `tools/blender/render_v12_pose_checks.py`: worst-frame pose renders (no save).
4. `tools/blender/package_review_v12.py`: normal Python with Pillow.
