
## 9i — Full-body audit fixes (Stuart, 1:03 AM Sun, approved) — do with 9f/9g in Phase A
Full report: `art/anim/wip/audit-full/FULL-AUDIT.md`. It was measured on v16 and re-verified on v18 with identical results. It has the severity table, frames and measures, stills `full-audit-*.png`, and the rig-structure notes. Scripts and raw JSON are in the same folder. Do these alongside 9f (hands and forearm roll) and 9g (pauldrons), since they touch the same rig areas: shoulder, neck, arm chain and halo. Save as a new numbered .blend only.

MUST-FIX:
- **M1 Tabard clipping hidden by Shrinkwrap.** Every TABARD object has a Blender-only Shrinkwrap ("outside", 1.5 mm) that won't export.
  - With it off, the back panel sinks 15-23 mm into the pelvis/buttocks in every locomotion, air, glide and Arc Step clip. The front panel goes 10-15 mm into the thighs/shins.
  - Spawn has the front panel 18 mm into the R thigh (f18-32) even with Shrinkwrap on.
  - Fix: tabard side/hip bones driven by the thighs, a larger rest offset, and removing or applying the Shrinkwrap before export. QA must run with Shrinkwrap off.
- **M2 Head/neck skin swallows the back hardware** (scapula shells, back node, yoke).
  - Depth: Arc Step up to 80 mm, Aim up 50, Run fwd 36, Discharge 31, Idle combat 23. At rest the shells are already 12 mm inside the neck.
  - Fix: reweight neck/collar with a gradient, add a mid-neck bone, move the shells/back node out, and limit head pitch-back in Arc Step and Aim up.
- **M3 Hard-surface chest parts tear.** The chest core, housing rim, ring and upper plates are weighted across chest/neck/head, with edge changes of 150-350% in the Aims, Arc Step and Spawn. Make them rigid on the chest (or core socket).
- **M4 Halo lower arcs (2/3) sink 30-47 mm into the chest, shoulders and pads.** This happens in Arc Step loop, Idle combat, Arc Bolt, Land, Glide loop and Select intro. Arc 3 goes into the head in Arc Step loop (34 mm), and arcs 1/4 into neck/head in Spawn (33 mm). At rest the pad and arc 2/3 overlap by 9.7 mm.
  - Fix: raise/scale the halo for ≥15 mm clearance at rest, and clamp the halo keys.
- **M5 L elbow/forearm goes 45 mm into the L hip/flank** (Discharge f3-7, Open Circuit end f6-14, OC f5-6: 18 mm). Extend the avoid-solve ellipse over the hips/thighs.
- **M6 The R arm bones don't fit the R arm mesh.** The mesh sits about 3.75 cm off centre, but the arm bones are mirrored about x=0: R elbow 6.6 cm off the elbow seam, R wrist 4-6 cm off, R fingers 68.6 mm off the mirror.
  - Fix: refit the R arm/hand/finger bones (or re-centre the mesh) and rebind.
  - Do this BEFORE 9f's forearm-roll work, and re-check the Arc Bolt muzzle afterwards.

SHOULD-FIX:
- **S1** Arc Step loop: shins cross by 24 mm on all frames.
- **S2** Run L/R: feet clip each other by about 9 mm at the pass frames.
- **S3** Hip weighting and pivots. Thigh weights reach 12 cm above the hip pivot, the pivots are 4-5 cm forward (L also 4 cm lateral), and the R thigh goes into the pelvis in Run.
- **S4** Neck weighting (the neck bone owns only 32 verts). Part of the M2/M3 fix.
- **S5** Spawn kneel tears the abdomen plates, lower spine conductor and tabard sigil ring. Make those rigid single-bone.
- **S6** Add forearm and upper-arm twist bones. Forearm roll reaches ±90° in Open Circuit, and the conductor deforms. Move 9f's roll into the twist chain.
- **S7** Velocity pops:
  - Arc Bolt forearm snap (108°/f²)
  - Arc Step start/end feet and toes (up to 92)
  - Jump legs (41-46)
  - Hand-offs Arc Step loop→end (43) and Idle→Jump (30)
  - Discharge's burst can be declared as an accent.
- **S8** Knee hinge off the bone X axis (L 83° / R 67°). Re-roll the shins.
- **S9** Legs asymmetric: R thigh 19 mm longer, R foot/toe 47-60 mm off mirror, and toe roll differs by 62°. Symmetrize, then re-check Run L vs R.
- **S10** Discharge f5-7: the hands go 11 mm into each other.

New permanent anim QA checks (part of PASS, run on every clip and every frame):
1. **All-pairs contact:** BVH plus depth for every part pair (armor, halo arcs, yoke, scapula shells, tabard panels, chest core, head, limbs, hands, feet). Fail above 5 mm over the rest baseline. This replaces the hands-only contact.py.
2. **Tabard measured with Shrinkwrap disabled.**
3. **Rigid-part integrity:** hard-surface edge change under 5% vs Idle f1. No more than 3% of seam edges above 40%.
4. **Joint sanity:** no knee/elbow hyperextension, hinge off-axis ≤ 30°, no roll flip over 90°/f, hand-forearm twist ≤ 70°, forearm twist ≤ 60°.
5. **Pops:** angular acceleration ≤ 20°/f² outside declared accents. Velocity continuity at loop seams and hand-offs ≤ 12°/f².
6. **Foot slide/penetration:** sole no more than 1 cm below ground, contact slide ≤ 5 mm vs travel speed, knee-to-foot direction in ground contact, and foot-foot/shin-shin clearance.
7. **Symmetry:** L/R rest bones ≤ 5 mm / 3°. Mirrored clip pairs (Run L/R, Arc Bolt L/R, Aim L/R) ≤ 10 mm.
8. **Rest-overlap audit** of different-bone part pairs after every rig change.
