# Hollow Saint — Full-body rig & animation audit

Stuart request 1:03 AM PT Sun 2026-09-27. Auditor: executor subagent. Read-only.

- **Measured on:** `art/anim/wip/audit-full/audit-full-v16-copy.blend`. v16 was the newest numbered blend when the audit started (~01:05 PT).
- **Re-verified on v18:** run3 saved v17 (01:17 PT) and v18 (01:18 PT) during the audit, so every clip was re-measured on a copy of v18 in `audit-full/v18/`.
  - Rig, mesh, weights and rest overlaps are identical between v16 and v18. The only rig difference is the VFX sole jets becoming heel jets.
  - Every penetration, pop and foot number below agrees within 0.5 mm / 0.5 deg between v16 and v18.
  - Exceptions, all in the ±5 mm noisy hip-crease/front-tabard zone and consistent with run3's 9d glide/run leg re-pose:
    - Glide enter: thigh-in-pelvis went from 11.7 to 7.5 mm, and tabard front→pelvis (Shrinkwrap off) from 13.7 to 11.5 mm.
    - Run forward and Glide exit: tabard-front pairs of 4-8 mm dropped off (front→spine 8.3 and 6.2 mm, front→R shin 7.2→4.0), while Glide exit front→R thigh rose from 7.3 to 9.0 mm.
  - The tabard back-panel (M1) numbers are unchanged on v18.
  - The table's numbers therefore stand for v18.
- **Sampling:** all 33 clips, every frame, evaluated deformed meshes (depsgraph).
- **Scripts** (all in this folder):
  - `inspect_rig.py`: rig structure
  - `audit_measure.py rest|anim`: BVH triangle overlap, ray-parity penetration depth, bone transforms, feet and edges
  - `parts_lib.py`: part grouping
  - `probe_geo.py`: seams and weight extents
  - `edge_pass.py`: edge stretch vs Idle f1
  - `render_shots.py`: Workbench close-ups with the offending parts highlighted
  - Box-side `analyze.py`: per-clip worst, frames over 5 mm, joints, pops, feet, symmetry
- **Raw outputs:**
  - Rest: `rest_audit.json`, `rig_inspect.json`
  - Animated: `anim_A/B.json`, and `anim_noshrinkA/B.json` with the tabard Shrinkwrap disabled
  - Probes: `probe_geo.json`, `edges_vs_idle.json`
  - The same set for v18 is in `v18/`.
- **Depth** means the deepest vertex of part A inside the closed body/part B, found by ray parity and confirmed by BVH triangle overlap. It is reported in mm at the 1.0 rig scale. Depth against a thin plate saturates at about half its thickness.

## Severity summary
**MUST-FIX: 6. SHOULD-FIX: 10. MINOR: 7.**

| # | Sev | Clip(s) | Part / joint | Frames | Measure | Cause | Recommended fix |
|---|---|---|---|---|---|---|---|
| M1 | MUST | All locomotion, air, glide and Arc Step clips (16 clips); Spawn | Tabard back panel into pelvis/buttocks; tabard front into thighs/shins | Every frame of those clips. Front: Walk f8-9, f19-23; Glide loop f2-6, f27-31; Spawn f18-32 | With Shrinkwrap off, back panel into pelvis: Glide loop/exit 23.2, Run fwd 22.3, Jump/Land/Descend 21, Run R 20.6, Run L 17.2, Walk 15.2 mm. Front: L shin 15.1 (Glide loop), R thigh 11.4 (Walk), 10.9 (Run R), 10.4 (Arc Step). Spawn front into R thigh 18.2 mm, which **also happens with Shrinkwrap on** | **Rig.** The tabard's clean look comes only from a Blender-only Shrinkwrap ("outside", 1.5 mm offset) on every TABARD object, which does not export to Unity/FBX. The tabard chains (front 3, back 2) hang off the pelvis with no thigh or side influence | Add tabard side/hip bones driven by the thighs (about 30-50% of thigh swing, front bones driven by thigh pitch). Push the back panel out 15-20 mm at rest. Key or drive the tabard in the clips. Apply or remove the Shrinkwrap before export, and run QA with it OFF |
| M2 | MUST | Arc Step start/loop/end, Aim up, Run fwd, Glide exit, Discharge, Idle combat, Open Circuit | Scapula shells, back node and yoke swallowed by head/neck skin | AS start f2-7; AS loop all frames; AS end f1-11; Run fwd and Idle combat all frames; Discharge f4-22; OC f4-25 | Scapula shell into head/neck skin: AS start 79.6, AS end 76, AS loop 74, Aim up 50, Run fwd 36, Glide exit 36, Discharge 31, Idle combat 23, OC 21.6 mm. AS loop: back node into head 40.7, yoke into head 28.6 mm. At rest the shells are already 12 mm inside the neck skin | **Rig + keys.** The shells are rigid on the non-deforming, chest-fixed scapula. The neck bone owns only 32 vertices. Chest weights reach z 1.73, giving a hard chest→head seam (78-85 of 108 seam edges stretch >40% vs Idle in the Aims, Arc Step and Spawn). AS and Aim up pitch the head back into the back hardware | Reweight neck/collar with a smooth chest→neck→head gradient. Add a mid-neck bone. Move the scapula shells/back node 15-25 mm back and out. Clamp head pitch-back in AS and Aim up (or counter-rotate via the neck, not the head) |
| M3 | MUST | Arc Step loop, Aim L/R, Spawn, Aim up | Chest core housing rim, core ring, upper chest plates, neck cables | AS loop f3; Aim left/right f1 | Max edge change vs Idle: core housing rim 347%, core ring 181%, R upper chest plate 196%, L 158%, neck cables 184% | **Rig.** Hard-surface parts are vertex-weighted across chest+neck+head, so they bend and tear | Make the core, ring, rim and plates 100% chest (or bone-parent them to the chest/core socket). Keep neck cables on neck/chest only, with a smooth gradient |
| M4 | MUST | Arc Step loop, Idle combat, Arc Bolt L/R, Land, Glide loop, Select intro, Glide enter, Spawn | Halo lower arcs (arc 2/3) into chest/shoulders/pads; arc 3 into head; arcs 1/4 into neck/head | AS loop all 11; Idle combat all 49; Arc Bolt f5-6; Land f10-13; Glide loop f13-26; Select intro f14-18; AS loop f7-10 (head); Spawn f26-40 | Arc 2/3 into chest: 46.8 (AS loop), 45.2 (Idle combat), 38-42 (Arc Bolt), 40.7 (Land), 38.8 (Glide loop), 30.7 (Select intro), 22 (Glide enter). Arc 3 into head 33.7. Spawn arcs 1/4 into neck/head 33. At rest: pad vs arc 2/3 9.7 mm (102 tri pairs), arc 2/3 vs yoke 3.2 mm | **Rig + keys.** The arcs are rigid on the halo bones with no clearance at rest. Keyed halo poses tilt or drop them further into the torso | Raise/scale the halo ring so the lower arcs clear pads and yoke by ≥15 mm at rest. Add a clearance clamp to the halo keys. For combat/Arc Step, tilt the halo back rather than down |
| M5 | MUST | Discharge, Open Circuit end, Open Circuit | L elbow/forearm into L hip/flank (R side minor) | Discharge f3-7; OC end f6-14; OC f5-6 | 45 mm (Discharge), 45.5 (OC end), 18 (OC). Confirmed by 26-36 BVH triangle pairs, so this is not ray noise. R side: 1-2 tri pairs, Discharge f5-7 | **Keys** (gather/recall pose), made worse by thigh weights reaching up onto the flank (S3) | Extend the `special.solve(avoid=True)` avoidance ellipse down over the hips/thighs, and add these pairs to contact.py. Current contact.py passes these frames because it tests hands/forearms against the torso only |
| M6 | MUST | Rig (all clips); visible in Arc Bolt L vs mirrored R | R arm bone chain vs R arm mesh | All | R elbow pivot 6.6 cm medial and 3.6 cm off in y from the mesh elbow seam (L is 1 cm off). R wrist 4.1 cm off in x, 5.7 in y. All R finger bones are 68.6 mm from the mirror of the L fingers. R hand-part centroid (-0.549, -0.089, 0.787) vs L (0.502, -0.039, 0.787). Arc Bolt L vs mirrored Arc Bolt R: 72 mm at the forearm tail (f5) | **Rig.** The mesh centreline is at x ≈ -0.03 to -0.04, and the halo root and core socket follow it (-0.0375/-0.04). The spine sits at x=0 and the arm bones are mirrored about x=0, so the R arm skeleton doesn't sit in the R arm mesh. This is the root of STATUS's "3.75 cm" mirror note | Refit the R upper arm, forearm, hand and fingers to the mesh (or re-centre the mesh on x=0 and refit both sides). Rebind the arm weights. Re-run hand QA and Arc Bolt muzzle aim afterwards |
| S1 | SHOULD | Arc Step loop / start / end | R shin into L shin/foot | Loop: all 11 frames; start f7; end f1 | 24.2 mm (25-40 tri pairs); about 11.5 mm at start f7 and end f1 | Keys | Widen the dash leg stance, or offset the trailing leg laterally by ≥3 cm |
| S2 | SHOULD | Run left, Run right | Feet into each other | Run L f4-5; Run R f13-14 | 9.2 / 8.9 mm | Keys (crossover step) | Add ≥2 cm lateral clearance at the pass frames |
| S3 | SHOULD | All (worst in Run) | Hip/flank weighting; R thigh into pelvis | Every frame | Thigh weights reach z 1.118, 12 cm above the hip pivot (z 1.0). 35 of 41 thigh→spine seam edges >40% in most clips (R worse). Hip pivots are 4.3-5.3 cm forward of the hip-seam centre. L hip pivot is 4.2 cm lateral (seam x 0.088 vs bone 0.13). R thigh inside pelvis up to 63 mm in Run (hip-crease self-intersection) | Rig | Re-place the hip pivots at the seam centre. Pull thigh weights down to about 5 cm above the pivot with a smooth pelvis blend. Optional hip-corrective helper bone |
| S4 | SHOULD | Aims, Arc Step, Spawn | Neck weighting | — | Neck bone dominates only 32 verts; 78-85/108 seam edges >40% | Rig | Part of the M2/M3 fix (mid-neck bone, gradient weights) |
| S5 | SHOULD | Spawn (kneel) | Abdomen segments, plates, BACK lower spine conductor, tabard sigil ring and front top | Kneel frames | Edge change vs Idle: abdomen slit 829%, plate 2 405%, lower spine conductor 391%, tabard sigil ring 895%, tabard front top tearing | Rig (rigid plates weighted across pelvis/spine/chest) | Make each abdomen plate single-bone rigid. Put the sigil ring on one tabard bone |
| S6 | SHOULD | Open Circuit, Run fwd, Idle; all arm clips | No forearm/upper-arm twist bones | OC f7-12 | Forearm roll relative to the upper arm reaches ±90° (OC), a constant 55° in Run fwd and 40° in Idle. R forearm conductor deforms 102% (OC f9). The 9f thumb/roll fix will add more roll | Rig | Add forearm twist (and upper-arm twist) bones sharing 50% of the roll. Move 9f's roll correction into the twist chain instead of the forearm |
| S7 | SHOULD | Arc Bolt L/R, Arc Step start/end, Jump, Discharge; hand-offs | Angular-velocity pops | Arc Bolt f3, f5; AS start f3-5; AS end f3-7; Jump f5; Discharge f7/f9 | Arc Bolt forearm 108°/f² at f3 (55°/f then stop) and 57 at f5. AS start toes/feet/thighs 37-92. AS end feet/thighs 47-66. Jump shins/thighs 41-46. Discharge arms 50-59, halo 35. Hand-offs: AS loop→end 43 (L shin), Idle→Jump 30.5, AS start→loop 21. Loop seams mild (max 23, Run left L foot f17) | Keys | Add 1-2 frames of ease-out / follow-through after the Arc Bolt snap and AS launch. Match velocities across AS start→loop→end and Idle→Jump. Discharge's burst may be an intentional hit-stop; declare it as an accent |
| S8 | SHOULD | Rig (all leg clips) | Knee hinge axis vs bone roll | All | Knees bend about thigh-local Z: L 83°, R 67° off X, a 16° asymmetry. Elbows 30° off X but symmetric | Rig (shin rolls) | Re-roll the shins so the knee hinge is local X on both sides. Re-bake the leg keys (the pose is preserved if re-rolled with keyframe compensation) |
| S9 | SHOULD | Rig; Run L vs Run R, Aim L vs Aim R | Leg asymmetry | All | R thigh 19 mm longer. R foot head 60 mm off mirror, foot roll 22° different. R toe 47 mm longer, roll 62° different. R shin head 46 mm off. Run L vs mirrored Run R 66 mm (toe, phase offset 8). Aim L vs R 60 mm. Idle thigh twist +51 (L) vs -71 (R) | Rig (plus some key asymmetry) | Symmetrize the leg skeleton to the mesh (fit R to L-mirror where the mesh is symmetric). Then derive Run R from Run L by mirroring, or keep both but check the mirror delta |
| S10 | SHOULD | Discharge | L and R hand parts into each other | f5-7 | 11.4 mm | Keys (gather) | Separate the hands ≥1 cm in the gather, or use the avoid-solve for hand-hand |
| m1 | MINOR | Rest | Halo arc 2/3 vs yoke | Rest | 3.2 mm | Rig placement | Fixed by the M4 halo raise |
| m2 | MINOR | Rest | Chest core/plates vs neck cables | Rest | 4-4.6 mm | Placement | Nudge the cables out 5 mm |
| m3 | MINOR | Rest | Back node vs yoke | Static | 5.5 mm (same bone) | Modelling | Cosmetic; merge or offset |
| m4 | MINOR | Rest; Discharge f9; Arc Bolt L | Wrist cuff into forearm skin | — | 8-10 mm at rest; 34 mm in Discharge f9; 31 mm in Arc Bolt L | Rig (no twist, rigid cuff) | Resolved by the S6 twist bones plus 5 mm of cuff clearance |
| m5 | MINOR | Static | Back conductors / mask into skin | — | 10.7 / 8.5 mm | Modelling | Cosmetic (hidden) |
| m6 | MINOR | Rig | BACK upper spine conductor weighted 100% to head/neck | — | Wrong bone | Rig | Weight it to the chest/spine |
| m7 | MINOR | Arc Step start | R sole below ground and slide | f3-4 | Sole -6.3 mm; 21 mm slide at the launch push | Keys | Lock the R foot until lift-off, or accept it as the push |

## 1. Rig structure

### Deform chain
- The upper arms are parented **directly to the chest**. There is **no clavicle**.
- **Scapula** is non-deforming and chest-fixed. The **pauldron** bone hangs off the scapula with a 40% Copy Rotation (baked and muted), as SHOULDER-AUDIT found.
- The L/R shoulder helpers follow at 50% (baked and muted).
- **Missing bones:**
  - Clavicle
  - Upper-arm and forearm twist
  - Mid-neck
  - Hip/tabard side
  - Hip corrective
- The thigh weights and pivots are misplaced (S3).

### Rigid, bone-parented parts
| Part | Parent bone | Note |
|---|---|---|
| Pauldrons | pauldron | See SHOULDER-AUDIT |
| Scapula shells, back node | scapula / chest | **Problem:** chest-fixed while the neck/head skin moves over them (M2) |
| Yoke bar | chest | — |
| Halo arcs 1-4, gap lights, shoulder inlay bands | 4 halo bones under `halo root` (on chest, at x -0.0375) | Arc clearance is the problem (M4), not the parenting |
| Hand segments | hand/finger bones | The R chain is misfit (M6) |
| Mask, ear sockets | head | — |
| VFX heel jets (v18; v16 had sole jets) | toe/foot bones | Their drivers won't survive the Unity export |

### Weighted parts that should be rigid (flagged)
| Part | Currently weighted to | Issue |
|---|---|---|
| Chest core, housing rim, ring, upper plates | chest + neck + head | M3 |
| Abdomen plates / segments | pelvis + spine + chest | S5 |
| BACK upper spine conductor | 100% head/neck | Wrong bone (m6) |

### Correctly weighted
- **Neck cables:** head/neck/chest. This is fine, but needs a smoother gradient.
- **Forearm conductors:** upper arm, forearm, hand. Fine, but they need twist bones.
- **Tabard:** pelvis plus tabard chains (front 3, back 2), with **Shrinkwrap** "outside" at 1.5 mm on every TABARD object. This modifier is Blender-only and hides M1.

### Body mesh and constraints
- **Body mesh:** 26 vertex groups plus a "Smooth by Angle" geometry-nodes modifier.
- **Constraints:** the IK and IK-rotation constraints are at influence 0 (the keys are baked). The pauldron/shoulder Copy Rotations are muted. There are no live constraints the export depends on.
- **Drivers:** only on the VFX objects.

## 2. Rest-pose overlaps (different-bone or rigid pairs over 2 mm)
| Pair | Depth | Note |
|---|---|---|
| Pauldron L/R vs halo arc 2/3 | 9.7 mm, 102 tri pairs | Matches ORIENT/SHOULDER; see M4 |
| Pauldron vs chest yoke bar | Saturates ~10 mm (thin plate) | Already in SHOULDER-AUDIT |
| Scapula shells vs neck skin | 12 mm | M2 |
| Scapula shell vs neck cables | 11 mm | M2 |
| Back conductors into skin | 10.7 mm | m5 |
| Wrist cuff into forearm skin | 8-10 mm | m4 |
| Mask into head skin | 8.5 mm | m5 |
| Back node vs yoke | 5.5 mm | m3 |
| Chest core/plates vs neck cables | 4-4.6 mm | m2 |
| Halo arc 2/3 vs yoke | 3.2 mm | m1 |

## 3-7. What's clean (no issue found)
- **Foot/ground penetration > 1 cm:** none. The worst is -7.2 mm (Arc Step end R) and -6.3 mm (Arc Step start R). Standing clips sit +4 to +6.6 mm above ground (the claw-tip lift).
- **Foot sliding in contact:**
  - Walk, Run forward/backward/left/right, Land, Idle and Spawn: 0-0.4 mm, measured against the travel speeds 1.5 / 6 / 4.25 / 4.5 m/s.
  - The only slide is Arc Step start R, 21 mm at the launch push (m7).
- **Hyperextension:** no knee/elbow hyperextension or reverse bending in any clip.
- **Roll flips:** none over 90° between adjacent frames on any bone.
- **Hand-vs-forearm (wrist candy-wrapper):** peaks at 25.7°, under the 70° limit. The forearm-vs-upper-arm roll is the problem; see S6.
- **Knee direction:** consistent L/R in the ground clips.
- **Loop seams:** positions match (run3's 0.0 mm seam QA). Velocity seams are mild (≤23°/f²).
- **Pauldron cross-check:** my pauldron numbers reproduce SHOULDER-AUDIT (Arc Bolt R 45.3 vs 46.3 mm), which validates the method.

## Noisy / low-trust checks (honest caveats)
- **Knee-over-foot direction** is invalid in pointed-foot clips (Glide, Ascend, Descend, Arc Step, Jump). It was only used for ground clips.
- **Anything near the hip crease** (thigh→pelvis/spine, tabard front→pelvis) is unreliable. The body self-intersects at the hip in every pose, including Idle, so the ray-parity depth there partly measures the body's own fold. Examples:
  - Tabard front→pelvis reads about 9.5 mm even in Idle and with Shrinkwrap on.
  - Glide enter thigh→pelvis changed 11.7→7.5 mm between v16 and v18.
  - **Trust the back panel** (M1, 15-23 mm): it is at the buttocks, away from the crease, and the stills confirm it. Treat the front-panel numbers as ±5 mm.
- **Edge stretch vs rest pose** was unusable: Idle is already 10× off the A-stance rest. I re-measured against Idle f1. The **counts** of >40% seam edges are reliable, but the p99 magnitudes are dominated by sliver edges. Treat the % figures in M3/S5 as "tearing, yes", not exact values.
- **Thin-plate depth** saturates at about half the plate thickness (the pauldron/yoke read about 10 mm).
- **Thigh twist values** are offset by the A-stance rest; only the L/R asymmetry is meaningful.
- **The mirror-clip check** (Run L vs R, Arc Bolt L vs R, Aim L vs R) can't separate rig asymmetry (M6/S9) from key asymmetry.

## Stills (Workbench, offending parts highlighted)
In this folder and copied to the box at `/workspace/hollow-saint-previews/refine/`:
1. `full-audit-01-head-into-back-hardware-arcstep-start-f5.png` (ref: `full-audit-01ref-back-hardware-idle-f1.png`): M2
2. `full-audit-02-chest-plate-crumple-aim-right-f1.png` (ref: `full-audit-02ref-chest-plates-idle-f1.png`): M3
3. `full-audit-03-chest-core-deform-arcstep-loop-f3.png`: M3
4. `full-audit-04-halo-arcs-in-shoulders-idle-combat-f33.png`: M4
5. `full-audit-05-L-elbow-into-hip-open-circuit-end-f7.png`: M5
6. `full-audit-06-shins-cross-arcstep-loop-f5.png`: S1
7. `full-audit-07-tabard-into-pelvis-NOSHRINK-run-forward-f6.png` (ref: `full-audit-07ref-tabard-WITH-shrinkwrap-run-forward-f6.png`): M1
8. `full-audit-08-abdomen-tabard-top-deform-spawn-f1.png`: S5

Other `full-audit-*` PNGs in this folder are superseded first-batch framings and are not part of the report.

## Permanent QA checks to add to anim QA (preview.py / contact.py)
1. **All-pairs contact.** Every part pair (armor, halo arcs, yoke, scapula shells, tabard panels, chest core, head, limbs, hands, feet) gets BVH triangle overlap plus ray-parity depth on every frame. Compare against the rest baseline. **FAIL above 5 mm over baseline.** This replaces the hands-only contact.py.
2. **Tabard with Shrinkwrap disabled.** Measure the tabard with its Shrinkwrap modifiers off, since that is what exports. Alternatively, fail the export if a Shrinkwrap is still present.
3. **Rigid-part integrity.** For hard-surface parts (plates, core, rim, cuffs, shells), max edge change vs Idle f1 < 5%. For body seam regions (neck, hip, shoulder), no more than 3% of edges over 40%.
4. **Joint sanity.**
   - Knee/elbow: no hyperextension, and hinge off-axis ≤ 30°.
   - Roll: no roll change > 90°/frame.
   - Twist: hand-forearm twist ≤ 70°; forearm-vs-upper-arm twist ≤ 60° without twist bones.
5. **Pops.** Local angular acceleration > 20°/f² fails unless the frame is a declared accent (as handqa does). Velocity continuity at loop seams and at clip hand-offs (AS start→loop→end, Idle→Jump, Land→Idle) must be ≤ 12°/f².
6. **Feet.**
   - No sole more than 1 cm below ground.
   - Contact slide ≤ 5 mm relative to the clip's travel speed.
   - Knee within 45° of foot-forward in ground-contact frames only.
   - Foot-foot and shin-shin clearance ≥ 0.
7. **Symmetry.** L/R rest bone mismatch ≤ 5 mm / 3° (head, tail, length, roll, including fingers). Mirrored clip pairs (Run L/R, Arc Bolt L/R, Aim L/R) ≤ 10 mm after phase alignment.
8. **Rest-overlap audit after every rig change.** List all different-bone part pairs intersecting at rest (the §2 table) and fail on any new pair above 2 mm.
