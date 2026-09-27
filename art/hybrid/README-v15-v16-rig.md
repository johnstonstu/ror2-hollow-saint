# Hollow Saint v15 touch-ups and v16 animation-ready rig

- [v16 review page](v16/review.html) · [v15 comparison](v15/comparison.jpg)
- Editable scenes: [v15](hollow-saint-hybrid-v15.blend), [v16](hollow-saint-hybrid-v16.blend)
- Plan and decisions: [docs/animation-plan.md](../../docs/animation-plan.md)

v1–v15 files are unchanged by the v16 build.

## v15 touch-ups

- Outer forearm cyan conductors, like the long light on the concept arms, stopping
  short of the elbow blend zone (drift at most 7 mm). A wrist-back dash was
  tried and dropped: the left wrist had no clean surface and the body already has one.
- Pauldron ceramic toned down about 7% to sit with the warm body ivory.
- QA PASS (decal drift max 8.3 mm).

## v16 rig upgrade (79 bones, 56 deforming)

| Area | What changed |
|---|---|
| Legs | Chain refit to the digitigrade mesh. The old knee was 12 cm low and about 10 cm too far in; the old ankle was about 20 cm off. Joints are now measured from leg cross-sections and dark joint bands. Leg weights are redistributed with smooth knee/ankle blends; the hip blend with the pelvis is kept |
| Feet | New `L/R toe` bones for the claws (previously skinned to the shin); foot bone now runs ankle → ball; `heel socket` marks the spur |
| IK | `foot IK` + `knee pole`, `hand IK` + `elbow pole` in the `IK controls` collection. IK and IK rotation copies ship at **influence 0**; set to 1 to pose with IK. Pole angles are solved so enabling IK at rest moves the body < 0.4 mm |
| Pauldrons | `L/R pauldron` bones (children of scapula) follow 40% of upper-arm rotation; plates are rigidly bound, no longer weight-blended |
| Tabard | `tabard front.1-3` and `tabard back.1-2` chains. The welded tabard slab, including its rear face that was skinned to the legs, is now on the chain. Cloth/trim overlays follow the new weights, plus a Shrinkwrap "outside" pass so they never sink into the body when swung |
| Halo | `halo root` inserted above `halo 1-4` for lag/sway follow-through |
| Sockets | `L/R muzzle` (index fingertips), `core socket`, `halo socket`, `head socket`, `heel socket` (non-deforming) |

## v16 QA

- **Existing clips unchanged:** 60 frames across all six `HS_v10` actions. The body
  vertex difference from v15 is exactly 0; the largest overlay difference is 2 mm (Shrinkwrap).
- Weight sums normalised; surface-decal drift PASS (max 8.3 mm).
- **Exercise pose** (14 cm crouch with feet planted by IK, toe curl, tabard swing,
  left arm IK reach): knees bend at the joint bands, feet stay planted, claws curl,
  front tabard swings as one slab. Renders: `v16/rig-exercise-*.png`.
- **Pauldrons:** drift from the shoulder skin rises to 70 mm at Arc Bolt frame 9. The
  renders (`v16/pauldron-pose-compare.jpg`) show solid plates following the arm with no clipping.

## Known limits

- The fused shoulder still crumples when the arm is raised high by IK (152 over-stretched
  edges, all around the shoulder at z ≈ 1.5 m). This needs corrective shapes or local retopology before
  overhead poses.
- Tabard swings beyond about 15° show the slab edge; the sigil hinges visibly at the waistband.
- Arms were not refit because they are animated by the existing clips.
- Constraints must be baked to deform bones before FBX export; Shrinkwrap is Blender-only.

## Reproduction (Blender 5.2, background only)

1. `tools/blender/build_hybrid_v15.py`, then `tools/blender/build_rig_v16.py`.
2. `review_rig_v16.py -- dump` (reads v15), then `review_rig_v16.py -- check`.
3. `HS_VERSION=v16` + `review_hybrid_v11.py`; `render_v16_pose_checks.py -- v15|v16`;
   `audit_rig_animation_readiness.py -- v16`.
