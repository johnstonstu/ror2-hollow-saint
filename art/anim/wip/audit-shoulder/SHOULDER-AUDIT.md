# Shoulder / pauldron clipping audit (v16 copy, background Blender, Sep 27 ~12:50 AM PT)

Read-only audit of `audit-copy-v16.blend` (copy of hollow-saint-anim-v16.blend). Scripts: `measure.py` (vertex depth), `overlap_all.py` (triangle overlap), `shot.py` (renders).

## How the pads are driven
- Meshes: `L/R SHOULDER | V17 pauldron upper` (49 verts, Solidify 20 mm + Bevel + Armature). One vertex group each (`L/R pauldron`) = rigid skin to a single bone.
- Bone `L/R pauldron`: parent `L/R scapula` (parent `chest`, no constraints). Constraint "Follow upper arm 40%" (COPY_ROTATION from upper arm, influence 0.4) is BAKED into keys and muted (STATUS pauldron fix). No clavicle bone in the rig; the scapula is fixed to the chest.
- So the pad = chest/scapula + 40% of the full upper-arm rotation, with no lift/translation and no collision correction. On big raises (Arc Bolt ~74-80 deg arm vs chest -> pad 53-58 deg) the pad swings about its own pivot and its inner edge drives into the collar/neck/chest; on low/forward arm poses its underside sinks into the upper arm/deltoid.
- Also STATIC: at rest and in every frame of every clip the pads intersect the lower halo arcs (`HALO | independent copper arc 2/3`, ~70-100 tri pairs) and the chest `HALO | V17 yoke bar` (~40-55 pairs). This is the "clipping into the chest" visible in the Idle hero render - a placement/modeling overlap, not only animation.

## Vertex penetration into the body mesh (pad vertex inside body, depth to nearest body surface; rest pose = 0 mm)
"head" = body faces dominated by the head group, i.e. the neck/collar region.

| clip | side | obstacle | worst mm | worst frame | frames > 5 mm |
|---|---|---|---|---|---|
| Arc Bolt right | R | torso | 46.3 | 12 | 3-15 (13f) |
| Arc Bolt right | R | head | 41.5 | 11 | 4-15 (9f) |
| Arc Bolt left | L | torso | 38.3 | 4 | 3-16 (14f) |
| Arc Step start | L | torso | 35.3 | 4 | 4-7 (3f) |
| Arc Step start | R | torso | 33.7 | 4 | 4-7 (4f) |
| Select intro | R | torso | 28.5 | 14 | 5-29 (25f) |
| Arc Step start | R | head | 24.0 | 4 | 4-6 (3f) |
| Arc Step end | R | torso | 23.0 | 2 | 1-2 (2f) |
| Arc Step end | R | head | 22.0 | 2 | 2-7 (2f) |
| Arc Step end | L | head | 20.2 | 2 | 1-7 (3f) |
| Arc Step loop | R | head | 18.8 | 7 | 2-10 (9f) |
| Arc Step loop | R | torso | 18.7 | 7 | 1-11 (11f) |
| Arc Step start | L | head | 17.2 | 4 | 4-7 (4f) |
| Select intro | R | head | 17.0 | 20 | 7-20 (12f) |
| Arc Step end | L | torso | 16.6 | 3 | 1-8 (6f) |
| Arc Bolt left | L | head | 12.1 | 16 | 16-16 (1f) |
| Arc Step loop | L | head | 11.5 | 2 | 1-11 (11f) |
| Discharge | R | torso | 11.4 | 10 | 9-11 (3f) |
| Discharge | R | head | 10.1 | 9 | 9-11 (3f) |
| Spawn | R | head | 9.6 | 24 | 1-33 (33f) |
| Run forward | L | head | 7.8 | 2 | 1-17 (3f) |
| Open Circuit | L | torso | 7.1 | 20 | 20-23 (4f) |
| Discharge | L | torso | 6.8 | 10 | 9-12 (3f) |
| Charge full | R | head | 6.6 | 16 | 15-23 (7f) |
| Select idle | L | head | 6.1 | 91 | 1-97 (28f) |
| Select idle | L | torso | 6.1 | 90 | 77-90 (14f) |
| Run forward | L | torso | 5.9 | 2 | 1-17 (3f) |
| Arc Step loop | L | torso | 5.9 | 2 | 1-11 (3f) |
| Glide exit | L | head | 5.8 | 14 | 14-14 (1f) |
| Select intro | L | head | 5.6 | 44 | 1-44 (10f) |
| Run forward | R | torso | 5.5 | 2 | 2-2 (1f) |
| Glide exit | L | torso | 5.4 | 14 | 14-14 (1f) |
| Glide exit | R | torso | 4.9 | 14 | - |
| Run backward | L | head | 4.8 | 10 | - |
| Run forward | R | head | 4.6 | 10 | - |
| Glide enter | R | head | 4.6 | 1 | - |
| Idle combat | R | torso | 4.4 | 9 | - |
| Run backward | L | torso | 4.3 | 10 | - |
| Glide loop | L | head | 3.6 | 11 | - |
| Glide enter | L | torso | 3.1 | 1 | - |
| Idle combat | R | head | 2.5 | 30 | - |
| Aim left | R | head | 2.1 | 2 | - |
| Glide exit | R | head | 2.1 | 7 | - |
| Select intro | L | torso | 2.0 | 44 | - |

Clips with no pad vertex inside the body (edge crossings still possible, see overlap json): Idle, Charge loop, Walk forward, Run left/right (<1 mm), Ascend, Descend, Land, Open Circuit hold/end, Aims (<2.1 mm).

Note: vertex-inside depth under-reports shallow edge/face crossings; `shoulder-overlap.json` counts triangle-pair intersections per clip/side/object (body, halo arcs, yoke bar, scapula shell).

## Renders
- idle-hero.png (Idle f1, Hero three quarter camera, matches Stuart's angle)
- idle-closeup-R-f1.png, idle-closeup-L-f1.png
- worst1-arcbolt-right-R-f12.png, worst2-arcbolt-left-L-f4.png, worst3-arcstep-start-L-f4.png, worst4-select-intro-R-f14.png, worst5-discharge-R-f10.png
