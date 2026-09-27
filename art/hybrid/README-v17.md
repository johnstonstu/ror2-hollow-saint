# Hybrid v17: concept silhouette pass

Source: `hollow-saint-hybrid-v16.blend` (unchanged). Output: `hollow-saint-hybrid-v17.blend`.
Build: `tools/blender/build_hybrid_v17.py` (background, factory startup). Sheets:
`python tools/compare_concept_sheets.py v17 v15`. Review: `v17/review.html`.

## Changes

- **Head and neck:** head vertices lifted 9 cm (by head/neck weight, only within 16 cm of the
  centre line and never on the chest front); neck column thinned 14%. `neck` tail, `head`,
  `head socket`, `halo root`, `halo 1-4` and `halo socket` bones moved up the same amount, so
  the halo stays centred on the helmet. Four dark neck cables. Dark ear sockets with cyan cores
  (bone-parented to `head`).
- **Chest:** removed the V11 lower branches, abdomen centre line and side dashes (the "spider"
  look). Added a raised dark core housing rim, ivory upper-chest plates (both sides) and a left
  rib plate, three segmented dark abdomen plates with cyan slits, a core drop slit and angled
  side slits. The right rib plate raycast misses the narrower right torso and is skipped.
- **Pauldrons:** V13 shells replaced by faceted 20 mm caps wrapped around a tilted shoulder
  axis with a swept-up pointed outer corner; rigid to the v16 `L/R pauldron` bones.
- **Halo yoke:** dark bar joining the lower halo arcs behind the shoulders, strut to the back
  node, cyan node light; bone-parented to `chest` (arcs still unfold freely in Open Circuit).
- **Build:** waist slimmed up to 18% in x (z 1.10-1.38), excluding limbs and tabard.
- **Shading:** body uses Smooth by Angle 38 deg for crisper plate edges.

The body's rest shape changed, so v16's "identical deformation" check no longer applies.
Solidified plates run thickness/bevel before the Armature modifier; an angle-limited bevel
after the deform changed topology per pose (it also broke the per-vertex QA).

## QA (`HS_VERSION=v17 review_hybrid_v11.py`)

PASS, no weight or non-finite errors. Max surface drift per clip: Arc Bolt 5.0 mm, Arc Step
11.6 mm (abdomen segment 2, not visible in render), Charge 4.0, Discharge 4.3, Idle 1.9,
Open Circuit 7.0. Rigid pauldron drift is informational (up to 71 mm in Arc Bolt); pose
renders show no clipping.

## Remaining gaps vs concept

Concept pauldrons angle further down over the deltoid; concept chest plates are larger and
the neck still reads longer. Right rib plate missing.
