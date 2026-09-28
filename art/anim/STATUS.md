# Hollow Saint animation: live status

Updated 2026-09-27 10:40 PT (v25, run 5) by the coordinator agent (refinement pass, `REFINEMENT-PLAN.md`). For remote check-ins: this page and
`art/anim/review.html` (GIFs of every clip) are the current state. **Nothing needs the user.**

## Checkpoints

- `art/anim/hollow-saint-anim-v27.blend` (**latest**, 65 clips): run 6, item 12 (natural hand curl in every clip).
  v26 is the same pass before the Conduit Spear grip fix. See "Item 12" at the end and
  `art/anim/wip/hands2/HANDS2-RESULT.md`.
- `art/anim/hollow-saint-anim-v25.blend` (65 clips, superseded by v27): run 5, item 10 (every gameplay transition).
  - New clips: `Run stop`, `Run stop R` and `Run start`.
  - New cancel markers: Arc Bolt `Interrupt` f13 and Arc Step end `Cancel` f5.
  - Heel jets now dim through travel reversals (`VFX_VERSION` 2).
  - Full QA: **13/65 PASS**, the same 13 as v24. The three new clips fail only on faults they inherit from Run
    forward's legs.
  - The earlier gates are as in v24. **All 72 seams exact** (worst 0.001 mm). Summary in
    `art/anim/v25/qa-summary.json`.
  - Transitions: **402 handoffs in 149 stitched sequences, 399 pass, 3 flagged and reviewed smooth**. See
    `art/anim/wip/transitions/TRANSITIONS.md` and "Refinement item 10" below.
- `art/anim/hollow-saint-anim-v24.blend` (62 clips, superseded by v25): run 5, item 11 (Conduit Spear, Discharge
  snap, Meter full flourish).
  - Full QA: **13/62 PASS** (v23: 11/59). Conduit Spear and Discharge snap both PASS; Meter full flourish fails
    one 6.0 mm contact inherited from Charge full.
  - The earlier gates are as in v23 (only the Arc Step left/right start hand contact fails). **All 66 seams
    exact.** Summary in `art/anim/v24/qa-summary.json`. Details under "Refinement item 11" below.
- `art/anim/hollow-saint-anim-v23.blend` (59 clips, superseded by v24): run 5, item 9h (eight-direction locomotion).
  - 26 new clips: 4 run diagonals, 7 walk directions, 2 run leans, 2 pivot 180s, 2 plant turn 90s, and Arc Step
    back/left/right (start/loop/end each).
  - Full QA (`full_qa_ok`): **11/59 PASS** (v22: 10/33; the new one is Walk backward). No v22 clip lost PASS.
  - Earlier gates: every clip passes, except the hand-contact check on Arc Step left start and Arc Step right
    start (a hand brushes the crouched knee on f4; see 9h below).
  - **All 66 seams exact** (worst 0.001 mm). Summary in `art/anim/v23/qa-summary.json`. Details under
    "Refinement item 9h" below.
- `art/anim/hollow-saint-anim-v22.blend` (33 clips, superseded by v23): run 5, item 9i (full-body audit fixes).
  - PASS now includes the 9i full-body QA (`full_qa_ok`), which is much stricter than before.
  - Result: **10/33 clips PASS**. v21 would score 0/33 on the same gate (every clip then had 6–62 mm contacts or
    pops up to 125 deg/f²).
  - Every earlier gate still passes on 33/33: bake, IK, hand QA, hand contact, hand orientation, arm clearance
    and pads.
  - **All 30 seams exact** (worst 0.001 mm, Select intro). Summary in `art/anim/v22/qa-summary.json`.
  - Before/after in `art/anim/wip/audit-full/fixes/9i-before-after.md`. Details under "Refinement item 9i" below.
- `art/anim/hollow-saint-anim-v21.blend` (33 clips, superseded by v22): run 5, item 9g (shoulder pads / pauldrons).
  QA: 33/33 PASS including the new pad contact check (worst 2.3 mm, limit 5) and the 9c/9f hand checks;
  **all 30 seams 0.0 mm**. Summary in `art/anim/v21/qa-summary.json`; details under "Refinement item 9g" below.
- `art/anim/hollow-saint-anim-v20.blend` (33 clips, **superseded by v21, don't use**): the first 9g save. Its pad and
  halo smoothing left 7 seams inexact (Arc Step start/end <-> loop 23.9 mm on the halo, four pad seams 1-3 mm).
  v21 fixes that; the clips are otherwise the same.
- `art/anim/hollow-saint-anim-v19.blend` (33 clips): run 4, item 9f (hand orientation) plus FULL-AUDIT
  M6 (R arm skeleton refit), which the plan requires before the 9f roll work. QA: 33/33 PASS, now including the
  new orientation check; **all 30 seams 0.0 mm**. Summary in `art/anim/v19/qa-summary.json`; details under
  "Refinement item 9f" below.

- `art/anim/hollow-saint-anim-v1.blend`: Run forward, Walk forward, Glide enter / loop / exit
  on v18 (plus the six older HS_v10 studies). v18 unchanged.
- `art/anim/hollow-saint-anim-v2.blend` (12 clips): v1 + Jump, Ascend, Descend, Land,
  Run backward, Run left, Run right. All loop wraps and hand-offs measured 0.0 mm.
- `art/anim/hollow-saint-anim-v3.blend` (22 clips): v2 + Arc Bolt L/R, 5 aim poses,
  Arc Step start/loop/end.
- `art/anim/hollow-saint-anim-v4.blend` (latest, 28 clips): v3 + Charge loop, Charge full,
  Discharge, Open Circuit, Open Circuit hold, Open Circuit end. Pauldron fix applied to all clips.
- `art/anim/hollow-saint-anim-v18.blend` (33 clips): run 3 item c, the item 8 re-check after 9d/9e.
  Nothing regressed, so no clip code changed and this is a rebuild of v17 (QA as `art/anim/v17/qa-summary.json`).
  - Aim up/down/left/right: ±66 deg, PASS.
  - Arc Step: IK miss 0.07/0.08 mm and dash legs 0.985. The 0.997 reading is the straight-legged rest pose on
    its first and last frames.
  - Land -> Idle -> Jump: 0.0 mm.
  GIFs copied to `art/anim/wip/item8-check/`.
- `art/anim/hollow-saint-anim-v17.blend` (33 clips): v16 + items 9d and 9e together (run 3, item b):
  heel thrusters, the walk -> run -> glide foot/thrust progression, and a hand re-check of every clip. QA: 33/33
  PASS; **all 30 seams 0.0 mm**. Summary in `art/anim/v17/qa-summary.json`; details under "Refinement items
  9d + 9e" below.
- `art/anim/hollow-saint-anim-v16.blend` (33 clips): v15 + the remaining 9c work (run 3, item a).
  The new `contact.py` QA checks for hands and forearms sinking into the body, and every contact it found is
  fixed. QA: 33/33 PASS including hand QA and contact; **all 30 seams 0.0 mm**. Summary in
  `art/anim/v16/qa-summary.json`; details under "Refinement item 9c follow-up" below.
- `art/anim/hollow-saint-anim-v15.blend` (33 clips): v14 + item 8 (aim pitch through pelvis and
  shoulders, Arc Step leg reach, Land/Idle/Jump seam). QA: 33/33 PASS including hand QA; **all 30 seams 0.0 mm**.
  Summary in `art/anim/v15/qa-summary.json`.
- `art/anim/hollow-saint-anim-v14.blend` (33 clips): v13 + item 9c (natural hands: hand pass
  on every clip plus per-frame hand QA). QA: 33/33 PASS including hand QA; seams as v13 (all 0.0 mm except
  Land/Idle/Jump 1.233 mm). Summary in `art/anim/v14/qa-summary.json`.
- `art/anim/hollow-saint-anim-v13.blend` (33 clips): v12 + item 9 revised (upright Iron Man
  glide). QA: 33/33 PASS, seams as v12 (Run <-> Glide 0.0 mm). Summary in `art/anim/v13/qa-summary.json`.
- `art/anim/hollow-saint-anim-v12.blend` (33 clips): v11 + item 9b (locomotion arms clear the
  hips/torso/tabard; new arm clearance QA). QA: 33/33 PASS, 30 seams, all 0.0 mm except Land/Idle/Jump 1.233 mm.
  Summary in `art/anim/v12/qa-summary.json`.
- `art/anim/hollow-saint-anim-v11.blend` (33 clips): v10 + item 9 first draft (glide rework: hover
  pose, thruster feet, sole jets, glow ramp). QA: 33/33 PASS, 30 seams, all 0.0 mm except
  Land/Idle/Jump 1.233 mm. Summary in `art/anim/v11/qa-summary.json`.
- `art/anim/hollow-saint-anim-v10.blend` (33 clips): v9 + items 6-7 (weight, Arc Bolt body)
  and a locomotion arm fix. QA: 33/33 PASS, 30 seams, all 0.0 mm except Land/Idle/Jump 1.233 mm.
  Summary in `art/anim/v10/qa-summary.json`.
- `art/anim/hollow-saint-anim-v9.blend` (33 clips): v8 + items 4-5 (upper body in locomotion, tabard
  follow-through). QA: 33/33 PASS, seams as v8. Summary in `art/anim/v9/qa-summary.json`.
- `art/anim/hollow-saint-anim-v8.blend` (33 clips): v7 + refinement item 3 (secondary
  motion on the near-static loops). QA: 33/33 PASS, 30 seams, all 0.0 mm except the known
  Land/Idle/Jump 1.233 mm pair. Summary in `art/anim/v8/qa-summary.json`.
- `art/anim/hollow-saint-anim-v7.blend` (33 clips): v6 + refinement items 1-2
  (shoulder helper bones, Charge rework). QA: 33/33 PASS, 30 seams, all 0.0 mm except the known
  Land/Idle/Jump 1.233 mm pair. Summary in `art/anim/v7/qa-summary.json`.
- `art/anim/hollow-saint-anim-v6.blend` (33 clips): v5 after the full polish pass
  (special, presentation, Arc Step, locomotion). Every seam 0.0 mm except Land/Jump <-> Idle 1.2 mm.
- `art/anim/hollow-saint-anim-v5.blend` (33 clips, full planned set): v4 + Idle,
  Idle combat, Spawn, Select idle, Select intro. Land -> Idle and Idle -> Jump fixed (136 mm -> 1 mm).

## Done (QA PASS, seams measured 0.0 mm)

- Toolkit `tools/blender/anim/` (README there). Knee pole angles re-solved (v16 values flared knees).
- **Run forward**: 16 frames, 6 m/s, flight phase, ~14 deg lean, fists, reactive tabard, halo lag.
- **Walk forward**: 26 frames, 1.5 m/s, upright and calm (agent-built, reviewed).
- **Glide set** (sprint): enter from run frame 10 (push-off, lift-off), 32-frame glide loop, exit landing
  exactly on run frame 1. Since v13 it's an upright Iron Man thruster hover; see refinement item 9.

- **Air set**: Jump (11 f) hands off to Ascend (20 f loop); Descend (20 f loop) hands off to
  Land (15 f, soft claw-first touch, ends standing). Agent-built, reviewed.
- **Run backward / left / right**: same 16-frame phase as the forward run (L contact f1, R f9)
  for the blend tree; backpedal 4.25 m/s, side-skips 4.5 m/s, no leg crossing, no foot slide.
  I lowered the backpedal arms (they jutted forward).

- **Arc Bolt right / left** (20 f gesture, release f5, fingertip muzzle within 0.06 deg of
  straight ahead, starts/ends exactly at rest) + **Aim up/down/left/right/neutral** (66 deg,
  additive; Unity reference pose = 'Aim neutral'). Agent-built, reviewed; in v3.
- **Arc Step** start (7 f crouch-and-lean) / loop (10 f arrow-like dash, arms swept back, halo
  trailing) / end (14 f brake, claw-first brace, back to rest). Agent-built, reviewed; in v3.
- Preview now has a RoR2-like `chase` camera (behind/above).
- Toolkit: `foot(..., rest_match=w)` makes leg IK reproduce the rest knee (was ~7 cm off).
- **Special set** (upper-body gestures, legs free for locomotion): Charge loop (40 f, cupped orb,
  tremor, halo pulse), Charge full (24 f, clawed, braced), Discharge (28 f, gather then 2-frame
  burst, halo arcs blast out), Open Circuit (30 f, halo lifts/tilts into a flared crown above the
  head) -> hold (24 f loop, crown sways) -> end (22 f, crown folds back). Agent-built, reviewed.
- **Pauldron fix (all clips):** the 40% follow constraint was applied twice on playback (~64%, pads
  winged out) and Unity would have dropped it entirely. `bake` now keys the full effect and mutes
  the constraint; the bake check also verifies rotations now (all clips 0.0 deg).

- **Presentation set**: Idle (96 f loop: two breaths, weight shift, glance, finger crackles),
  Idle combat (48 f, lowered, casting hand ready, halo opened), Spawn (72 f: half-kneel with halo
  folded -> awaken jolt -> rises -> halo unfolds arc by arc -> ends on Idle f1), Select idle
  (96 f, palms-up hover, halo sway), Select intro (44 f, finger snap, halo flare). Agent-built.

All previews re-rendered after the pauldron fix (all 33 PASS); `review.html` is current.

## Polish pass (complete, in v6)
- Special: DONE. Discharge reads from the chase camera (deeper gather, wider fling, 2-frame
  hit-stop, bigger halo burst), release palms face forward (miss 68 -> 3-10 deg), Open Circuit
  framing hands on target (52 -> 5 mm), finger splay stays in the palm plane, Open Circuit
  forearm pop eased (accel 24 -> 13.5 deg/frame^2).
- Presentation: DONE. Spawn halo now folds into a closed collar (hides the chest yoke bar, no arc
  overlap, lag well under its cap) and the top arcs sweep up into the ring; tabard drapes over the
  thigh in the kneel. Idle is ~1.4x livelier (still calm), Select intro snap spread over 3 frames.
  All flicks/springs ease in; seams 0.0 mm.
- Arc Step: DONE. End clip is now 16 f with the leg swap spread over 4 frames; worst pops cut
  25-75%; seams 0.0 mm.
- Locomotion (coordinator, from the Arc Step agent's review): DONE. Feet land already moving
  back instead of stopping dead (run contact pop 141 -> ~90 mm, walk 47 -> 36 mm), softer run
  push-off, run halo no longer pinned at its lag limit (peak 2.7 cm vs 3.5 cm cap), jump takeoff
  arms/toes eased, glide-enter arm and leg ramps widened. All PASS, zero foot slide, seams 0.0 mm.

## Refinement pass (`REFINEMENT-PLAN.md`)

- **1. Shoulder crumple: DONE (v7).** v18 had a hard chest/upper-arm weight ring (0.9 -> 0 over one
  4-15 cm edge loop). `tools/blender/anim/rigfix.py` (applied on load by `hs_anim.open_start`, v18
  file untouched) adds `L/R shoulder` deform helpers at the upper-arm head that follow the upper
  arm at 50% (baked and muted like the pauldron), and re-spreads the weights chest -> helper ->
  upper arm with a smooth falloff over 24 cm (surface meshes copy the body split). Worst edge
  stretch in the shoulder region dropped 2-4x: Arc Bolt L f5 30.7 -> 7.6, Discharge f5 26 -> 5.5,
  Open Circuit f10 25.5 -> 5.7, Select intro 14.9 -> 3.6. Some extra compression on downward flings
  (Discharge f9/f11, Open Circuit f22) sits under the pauldron. A small back shard remains at extreme
  raises. Diagnostic: `shoulder_diag.py` (metrics and upper-body renders in `art/anim/wip/shoulder/`).
- **2. Charge rework: DONE (v7).** The orb is now held higher and further out (loop 1.13 m, full
  1.21 m), with hands wide enough to show past the torso from the chase camera. Charge full is clearly
  bigger: chest up and back, elbows flared, clawed fingers, halo flared, 3-beat heartbeat surge with
  stronger tremor. Loop has a 2-beat pulse. Crop: `art/anim/wip/special/charge-v7-chase-crop.png`.
- **3. Secondary motion: DONE (v8).** All terms are integer harmonics of each loop, and the
  enter/exit/intro clips derive from the same pose functions, so seams stay 0.0 mm. Measured with
  `motion_range.py` (v7 -> v8 bounding-box travel / peak rotation):
  - Glide loop: bigger bob with a second lift, torso roll with head counter-roll, alternating arm
    and finger drift, leg scissor and knee flex, stronger tabard flutter. Head 81 -> 146 mm,
    tabard tip 102 -> 166 mm, halo 90 -> 166 mm.
  - Arc Step loop: roll rippling up spine -> chest -> head, asymmetric arm pump, halo arc flicker and
    pulse, bigger tabard ripple. Head 30 -> 70 mm, halo arcs 31 -> 100 mm.
  - Select idle: buoyant knee-soft rise on the breath (never above stand height), slow head tilt,
    chest counter-roll, two finger-crackle flicks with an arc twitch. Head tilt 2.1 -> 4.8 deg.
  - Open Circuit hold: arms float out on the inhale plus a 2-per-loop surge through elbows, wrists
    and fingers in step with the crown pulse; light spine/head drift. Hands 11 -> 90 mm.
  - Ascend: slow drift in both legs' tuck (phase-offset), arms, chest and head; hover base raised
    12 mm so the feet stay clear of the floor. Hands +40%, tabard +50%.
  - Arc Step IK miss is 1.7 mm (was 1.2), still far under the 1 cm limit; fixed in item 8.
- **4. Upper body in locomotion: DONE (v9, arm fix in v10).** Run: hips and chest counter-rotate
  ~15 deg apart at contact (neck/head cancel it, gaze steady), scapulae ride forward with the arm,
  forearm trails the upper arm, chest side-bends on contact, bigger fore/aft swing. Walk: more
  counter-rotation, bigger swing, scapula motion. Backpedal: torso leans back ~6 deg into the travel,
  looser arm pump. Strafes: ~16 deg lean into the travel (head mostly upright), more counter-rotation,
  lead arm lifts out, trail arm tucks. v10: the forward fist no longer crosses the midline.
- **5. Tabard follow-through: DONE (v9).** New `hs_anim.tabard_follow`: each tabard link's world angle
  trails the pelvis yaw/roll by a growing phase delay (a whip down the chain). It's a pure function
  of loop phase, so Glide enter/exit, Jump/Land and Arc Step start/end keep exact seams. Used in
  Run, Walk, Backpedal, strafes, Glide, Ascend, Descend and Arc Step (faded in/out via a channel).
- **6. Weight: DONE (v10).** Jump crouch 165 -> 210 mm with more fold, arms load back then drive
  forward-up, halo dips at takeoff. Land squash 155 -> 195 mm, held ~2 frames, slower rise that still
  ends exactly on the stand. Run: `gait` gained `release_match` (foot keeps sliding back briefly at
  toe-off) and the run lands at 70% of the slide speed: worst foot pop 91 -> 71 mm, contact 87 -> 68 mm,
  max leg extension 0.975. Glide exit: torso overshoots upright and the body sinks into the stride
  (zero on the last frame). Arc Step end: sink, then torso and arms settle just past rest.
- **7. Arc Bolt body: DONE (v10).** Bigger chest wind-up/follow-through (spine+chest -14.5 -> +22.5
  deg), torso loads onto the casting side then shifts across (new roll channel), forward lean into
  the shot, head nod on release, and the head's counter-turn trails the chest by 1.5 frames (exact
  rest at both ends). The release still re-solves the arm: muzzle within 0.08 deg of straight ahead.
- **9. Sprint-to-glide rework: DONE (v11).** Body pitch cut to 36 deg with the head and spine lifted
  so it reads as a hovering glide, not a dive (loop hover body z 0.097 m, all Glide clips PASS). Feet
  point down/back like thrusters, toes together, with a light scissor. New `tools/blender/anim/vfx.py`
  (applied on load like rigfix, v18 untouched): per foot a hot core cone, an outer cone and three
  spinning zigzag arcs fire from the claw tips, parented to the toe bones, plus emission drivers on
  the cyan veins, core and halo gap lights. Both run off two root-bone properties keyed into every
  clip by `bake(props=...)`: `hs_glow` (emission x(1+1.6g)) and `hs_jet` (jet scale, 0 = hidden;
  0 in every non-glide clip). Enter ramps glow then jets in, the loop holds them with a flicker
  (integer harmonics, seamless), exit fades jets fast and glow slower, both 0 at the Run hand-off.
  Seams to Run 0.0 mm. Glide previews now include the chase camera. Unity drops the drivers; its VFX
  should read the keyed `hs_glow`/`hs_jet` curves. Crops: `art/anim/wip/glide/glide-v11-*.png`.
- **9b. Run arm clipping: DONE (v12).** New `tools/blender/anim/clearance.py`, run by `preview.py` on every
  frame of every clip: closest distance from each forearm (body verts) and hand mesh to the torso (pelvis/
  spine/chest/thigh faces + abdomen/rib plates) and the tabard. QA fields `arm_clearance` (per side: min,
  worst frame, part, what it hit, per-frame mm) and `arm_clear_min_m`; locomotion clips must stay >= 10 mm
  or they go CHECK. v11 failed it everywhere: Run forward touched the tabard/thighs on 14 of 17 frames, and walk,
  backpedal and strafes had forearms at 0 mm. Fix: less adduction plus upper-arm twist (elbow in, forearm
  out), swing amplitude unchanged. Run 27 -> 15 deg adduct / twist -25, walk 22 -> 10, backpedal 25 -> 15 /
  -25, strafes 27 -> 14 / lead 5 / -25 with forearms carried higher (elbow 58 -> 85) because the lead thigh
  swings out under the arm. Min clearance now: run 15.4 mm, walk 16.4, backpedal 15.7, strafes 28-30, glide
  enter/exit 50+. `arm_tune.py` scores arm variants against the check without baking. Arm params now sit in
  `ARM` dicts at the top of `run.py`, `walk.py` and `run_dirs.py`.
- **9 revised. Iron Man glide: DONE (v13).** Supersedes the v11 draft (36 deg, chest-leading). The body now
  hovers upright: pelvis 5 + spine 2 + chest 1 deg, so the pelvis-to-neck line leans 6.7 deg on average and 7.8 max
  in the loop (new QA field `body_pitch_max_deg` / `_mean_deg` on the glide clips). Pelvis hovers +33 cm with
  the legs nearly straight (9 deg behind the hips, soft knee), ankles 24 cm apart, feet pointed down and back
  (toes 36 deg past vertical) so the sole jets angle back as forward thrust and mostly clear the floor. Arms
  hang relaxed, slightly back and flared, palms turned in. The back tabard streams in a curve instead of a rigid
  board. Loop keeps its bob/roll plus a small 5th/9th-harmonic thrust buzz, so the wrap stays exact. Enter
  keeps the push-off from run frame 10: the feet leave the ground and fold together under the body while the
  glow, then the jets, ramp up. Exit: the feet flatten before they come down (separate location weights in
  `apply_blend`, which fixed a 6 cm toe dip through the floor), knees tuck, then the torso dips into the run
  lean while jets cut out fast and glow fades slower. Both hs_* props are 0 at the Run hand-offs. Jet cones are
  longer (`vfx.JET_LEN` 0.36 -> 0.50 m). Seams: Glide enter from Run f10, Glide exit to Run f1 and the loop
  wrap are all 0.0 mm. Hero/side/front/chase GIFs are in review.html.
- **9c. Natural hands: DONE (v14).** `handpass.py` runs inside `hs_anim.bake` after every pose function
  (`bake(..., hands=True)`). It depends only on the current frame's rotations, so seams stay exact, and its
  soft limits leave natural poses alone:
  - thumb joints always keep a slight curl (never bent back or straight out), and the thumb base tucks
    along the index when the hand is open; the tuck fades out as the fingers close so the thumb wraps the fist
  - finger base knuckles are limited relative to rest; middle and tip joints are limited on their true bend
    relative to the parent segment (the rig's tip bones are modelled pre-bent)
  - adjacent fingers curled by very different amounts, or both curled tightly, fan slightly apart so they
    don't intersect
  - hand roll, hand bend and forearm roll are soft-limited (no candy-wrapper wrists)

  New per-frame hand QA (`handqa.py`, now part of each clip's PASS status) checks joint limits,
  finger-to-finger and thumb-to-palm interpenetration against the mesh (over 2 mm beyond rest fails), and
  finger pops (angular acceleration, max 12 deg/f²; 24 on frames a clip declares in `finger_accents`, such
  as the Arc Bolt anticipation/release, Discharge release, Arc Step launch and Select intro snap).
  Per-clip fixes:
  - Idle and Select idle: finger twitches are slower (the Idle f78 pop dropped from 21 to 9.9 deg/f²)
  - Spawn: the awaken kick is softer
  - Arc Bolt: the anticipation no longer curls then snaps open the index, and the fist and thumb close
    evenly. The arm solve now aims with the hand pass applied, so the fingertip muzzle stays within
    0.06/0.07 deg of straight ahead
  - Discharge: the fingers clench evenly to 34 deg and burst open under constant acceleration into the
    hit-stop; the frame-to-frame finger jitter is gone
  - Charge full: finger tremor 7 -> 2.5
  - Arc Step start: the pre-curl before the launch is smaller

  Worst results after the fixes: pops 11.5 deg/f² (accent 20.5), interpenetration 1.3 mm. Hand close-up GIFs
  (back-of-hand and palm views of both hands, framed in the hand's own space) are in
  `art/anim/wip/hands/<clip>/hands.gif` (+ `hands-sheet.jpg`) for Run forward, Arc Bolt right, Charge full,
  Discharge, Idle and Glide loop, and on the matching review.html cards. The tools are `hand_closeup.py`
  (background Blender) and `hand_sheet.py`.
- **8. Aim poses and IK: DONE (v15).**
  - Aim up/down: the ±66 deg pitch is now pelvis 6 + spine 8 + chest 10 + neck 18 + head 24. The thighs
    counter-rotate so the legs don't move, and the shoulders rise 6 deg into an up-aim and settle into a
    down-aim. Aim up leans back from the hips instead of pushing them forward; aim down folds from the
    pelvis instead of dropping the head into the chest.
  - Arc Step: airborne ankles are clamped to 98.5% of each leg's real length. The IK miss drops from 1.7 to
    0.07 mm and the dash legs sit at 0.985.
  - Leg extension is now measured per side, because the v18 right leg is 2 cm longer than the old single
    `LEG_LENGTH`. The old 1.01 figure was just the rig's straight-legged rest pose on Arc Step's first and
    last frames, which now reads 0.997.
  - `bake` also reports `max_leg_extension_at` / `ik_miss_at`.
  - Land/Idle/Jump seam: Jump and Land plant the feet with the claw tip on the ground, 1 mm higher than
    the stand used by `air.stand_pose` and Idle. Both stands now use that claw-safe height (Spawn's step is
    now an offset from it), so the seam is 0.0 mm.
- **Refinement item 9c follow-up: DONE (v16, run 3 item a).** Checking against 9c turned up one gap: nothing
  tested whether the hands or forearms sink into the body. The hand QA only measured the fingers against
  each other, and `clearance.py` only measured the forearm clearance to the torso and tabard. The new
  `tools/blender/anim/contact.py` (README) runs in `preview.py`, and `hand_contact_ok` is now part of PASS.
  On v15 it found real clipping, confirmed in unclipped renders:
  - Idle, Jump f1, Land end and Spawn: the claws hung 4-6 cm inside the thighs.
  - Discharge gather: the left elbow sat 56-64 mm inside the ribs (the arm solve was pinned at its adduct
    limit).
  - Open Circuit end recall: 53 mm into the torso.
  - Arc Bolt right anticipation: the little finger sat 33 mm inside the chest.
  - Spawn crouch: 25-30 mm into the knees.
  - Run backward: 12 mm into the right shin.

  Fixes:
  - Stand arms (`air.STAND_ARMS`): adduct 22 -> 9. Jump's first key now reads `STAND_ARMS`, so Idle -> Jump
    stays 0.0 mm, and its crouch arms stay wide.
  - Combat-idle left arm: adduct 24 -> 11.
  - `special.solve(avoid=True)` adds a torso-ellipse penalty on the elbow, mid-forearm and wrist, with a
    wider gather target and recall pose. The Open Circuit end relax opens by 12 deg adduct.
  - Arc Bolt anticipation: swing -42, adduct 14, elbow 112.
  - Spawn crouch arms: adduct 30 -> 12 (right arm 4).
  - Run backward arm tuck on the back swing: -10.

  Worst results now: Spawn 3.9 mm (a left knee brush during the rise), Open Circuit end 2.9 mm, Land 0.9 mm;
  everything else 0.0. Hands QA stays OK (worst pop 11.5 deg/f², Jump 7.6). Stills of the fixed poses
  (front, outside and back views around each hand, body unclipped) are in `art/anim/wip/contact/v16/`.
- **Refinement items 9d + 9e (heel thrusters, feet/thrust progression, hands): DONE (v17, run 3 item b).**
  - Heel thrusters (`vfx.py`): the jets now come from the heel spur / Achilles base (the `heel socket` bone
    tail) instead of the toe claws. They keep the character's axes, so the exhaust always trails back along
    the travel line. Its downward tilt is keyed per clip in the new root property `hs_jet_dir`: run 12 deg,
    lift-off 58, glide 28. The new per-foot `hs_spark_L` / `hs_spark_R` properties drive the run sparks.
  - Walk: heel-toe and grounded, with no thrust. The heel spur strikes first with the toes 10 deg up, pivoting
    on the heel via the new `gait.heel_pivot`. The foot rolls flat by a quarter of stance, then peels off onto
    the toes. Lowest body point 7.6 mm (the heel resting on the ground), zero slide.
  - Run: stronger toe push-off (pushoff 38 -> 46 deg). A small heel spark (0.45 scale, about 4 frames) flashes
    on each toe-off, per foot, and it wraps exactly with the loop.
  - Glide enter: the right foot's last push-off becomes the lift-off. Its spark hands over to both heel jets
    igniting in a burst (hs_jet peaks at 1.66), with the exhaust swinging down to 58 deg for lift and then
    easing back to 28. The push-off sink/pop is softer.
  - Glide loop: steady flickering jets at 28 deg. The feet are no longer toe-down nozzles (foot pitch 106 -> 28,
    toe 126 -> 14): a slight point that turns the heels back along the travel line.
  - Glide exit: the jets cut within 3 frames as the legs swing down, and the feet plant into run frame 1 with
    no spark.
  - Handoff check (new `stitch.py`, `art/anim/wip/transitions/walk-run-glide/`): the halo cross-fade in Glide
    enter/exit now tracks the run's halo offsets frame by frame instead of freezing one; the frozen offset had
    put a 7 cm/f² halo kick into the lift-off. Lift-off peaks are now pelvis 35, tabard 35 and halo 55 mm/f²,
    against the run's own ~17-30. That is the intended take-off accent, not a seam pop. At the hard cut itself
    the worst bone is 1.1x its in-clip maximum.
  - Stitched preview: walk -> run (8-frame phase-aligned cross-fade) -> glide enter -> glide loop -> glide
    exit -> run, as hero, side and chase GIFs in `art/anim/wip/transitions/walk-run-glide/`, also on
    review.html.
  - Hands (9c re-check): I rendered close-ups of all 33 clips (`art/anim/wip/hands/<clip>/hands.gif`) with a
    new unclipped context view (`hand_closeup.py --context`) and reviewed them frame by frame (every third
    frame on the long idle loops, which QA also checks every frame).
    Found and fixed: Arc Bolt L/R, Discharge, Open Circuit, Open Circuit end and Arc Step started and ended on
    the bind rest with flat paddle fingers, and the Arc Bolt off-hand and the aim poses stayed flat throughout.
    They now use the relaxed Idle hand (curl 21, thumb 12; `special.rest_state`, `primary.REST_HAND`, Arc Step
    `REST`).
    Looked right: run and walk fists and relaxed hands, the glide hover hands, the Jump/Land swing, the Charge
    claw cradle, the Discharge clench and burst, the Open Circuit splay and recall, the Select intro point, and
    the Idle/Spawn rest hands. No thumb hyperextension, no thumb through the palm, no pops above the limit
    (worst 11.7 deg/f², Arc Step end). The v15 close-ups moved to `art/anim/wip/hands/_v15/`.
- **Refinement item 9f (hand orientation) + FULL-AUDIT M6: DONE (v19, run 4).**
  - **Finding: the v18 hands were mirrored.** Confirmed by eye in colour-coded close-ups (thumb red, index green,
    little purple; `art/anim/wip/hands/orientation/v18-check/`) and by geometry: each hand had the other hand's
    handedness (L +0.75, R -0.75). At rest the palm faced forward-medial with the thumb and index on the back edge,
    so any forearm roll that turned the thumb up also turned the palm backward. Forearm roll alone could not
    satisfy the rule.
  - **Rig fix (`handfix.py`, applied on load):** each hand's finger bones and hand meshes (except the wrist cuff and
    conductor) are reflected across the plane through the knuckles normal to the index-little axis. The fingers
    still curl toward the palm, and handedness is now L -0.75 / R +0.75. Splay/fan code (`handpass`,
    `presentation`) takes `handfix.ZSIGN`. `special.py` and Arc Step calibrate their signs from the geometry.
  - **M6 (`armfit.py`, applied on load before `rigfix`):** the body mesh is centred on x = -0.0375, but v18's arm
    bones were mirrored about x = 0. That put the R shoulder, elbow and wrist pivots 75, 66 and 72 mm off the R arm
    mesh. They now sit on the R mesh seams (using the L side's bone-to-seam offset). The R finger bones and hand
    meshes already fitted each other and are unchanged. The R arm mesh itself is not a mirror of the L (its
    forearm is modelled about 4 cm further forward), so the R upper arm is now 243 mm against 278 on the L. That
    comes from the mesh, not the fit. Arc Bolt muzzle aim after the refit: 0.07 deg (R) / 0.03 deg (L).
  - **Roll (supination, not wrist kinks):** a new orient step in `handpass` runs after the finger/wrist limits.
    On raised or extended arms, it finds the smallest forearm roll (plus up to 40% upper-arm twist on straight arms)
    that puts the thumb up and the palm off backward, weighted by the same gate as the QA so it fades in
    smoothly. The wrist is not bent for this. Charge full's orb cradle couldn't reach both targets, so its palm
    target now tips about 20 deg forward and no roll is needed there.
  - **New QA (`handorient.py`, part of PASS):** on frames where the upper arm is raised more than 45 deg, or the
    elbow is open more than 140 deg with more than 30 deg of raise (both relative to the chest, swept-back arms
    excluded), thumb-up must be >= 0 and palm-forward >= -0.25. `qa.json` has per-hand failing frame ranges and
    the worst values. v18 failed it on all 33 clips (`hands/orientation/audit-v18.json`). The check also flags
    wrong-handed hands, and on top of that the thumbs pointed fully down (-0.8 to -1.0) in Jump, Arc Bolt, Arc
    Step, Open Circuit, Charge full and Select intro. v19 passes all 33. Its worst gated values are thumb +0.07
    and palm -0.09. On forward-reaching arms the palm faces inward, which is the natural thumb-up grip.
  - Side effects fixed: the reflected fingers brought the L hand closer to the thighs, so the arms moved out a
    little. Walk adduct 10 -> 7 and backpedal 15 -> 7 now clear 29 and 25 mm. Strafes 14 -> 11, the Arc Step
    wind-up 6 deg wider, and the Spawn crouch left arm 6 deg wider now read 0 mm contact.
  - Before/after close-ups (v18 vs v19; front, back, palm and thumb views per hand) for Rest, Run forward, Arc
    Bolt right, Jump, Open Circuit, Arc Step end, Charge full, Glide loop and Select intro are in
    `art/anim/wip/hands/orientation/after-v19/<clip>/orient.gif` and `orient-sheet.jpg`; the v18 frames are in
    `before-v18/`.
  - Still open for 9i: forearm roll relative to the upper arm now reaches 80-90 deg in Arc Step and Open Circuit.
    That passes the current hand QA (95 deg) but not the audit's new joint-sanity check (60 deg without twist
    bones). The S6 twist bones will share it.
- **Refinement item 9g (shoulder pads / pauldrons): DONE (v21, runs 4-5).**
  - **Cause (SHOULDER-AUDIT):** each pad copied 40% of the whole upper-arm rotation about the shoulder joint, so on
    big raises its inner edge dug into the collar/neck and its underside into the deltoid. Separately, the halo's
    lower arcs and the yoke bar sat inside both pads at rest (10-13 mm), and the halo's own keyed lag/tilt drove the
    arcs further in (Arc Bolt, Select intro, Idle combat, Arc Step).
  - **Placement (`padfix.py`, applied on load):** the R scapula/pauldron bones mirror the L ones about the mesh
    centre (they had the same x = 0 mirror error as the old R arm). The whole halo assembly moves 6.5 cm back and
    3 cm up, so the lower arcs and yoke clear the pads at rest. The yoke bar's ends are trimmed 5.5 cm so they stop at
    the lower-arc docks instead of poking out as bare rods behind the pads. The 40% Copy Rotation is muted for good.
  - **Drive (`padpass.py`, runs in `bake` on every frame):** a pad takes a share of the upper arm's swing relative to
    the chest (sideways raise 50%, forward/back 25%, horizontal 20%, twist ignored). It hinges on its collar-side
    edge, so the outer edge lifts with the arm while the inner edge stays on the collar, and it lifts 1.5 cm up and
    8 mm out at full raise. If a pad would meet the halo's lower arc, it gives up some of that follow. If any collar,
    neck or upper-arm skin still ends up inside it, it lifts off just far enough. Both corrections are smoothed over
    time and never go below what each frame needs. Loops keep the exact per-frame solve at their seam frames (new
    meta `seam_anchors`), so seams stay 0.0 mm. A residual halo nudge (up/back) covers the few frames where an arc
    would still come within 4 mm of a pad.
  - **Halo at the source:** the halo root now tilts about the lower-arc dock instead of the ring centre
    (`padfix.halo_pose`), so recoil and lean swing the top of the ring. Arc Bolt's yaw trail is damped, the
    Arc Step lower arcs flare sideways only, and `hs_anim.halo_tether` soft-limits the halo lag's drop and forward
    swing to 8 mm. Arc Bolt now needs 0-1 mm of halo correction instead of about 107 mm.
  - **Arc Step neck (also FULL-AUDIT M2):** the dash counter-pitched the neck and head back 54 deg to keep the face
    level, which bent the collar skin up into the pads (the pads had to lift ~6 cm off it). The neck now carries 40%
    and the head 80% of that, so the head tucks into the dash (face about 25 deg down). The pad lift in Arc Step
    dropped from 62-67 mm to 0-4.5 mm.
  - **New QA (`contact.PadContact`, part of PASS):** pads vs torso, neck/collar and upper arm (beyond rest), and vs
    the halo lower arcs and yoke bar (absolute), every frame, limit 5 mm. v19 failed it on most clips; v21 passes all 33
    (worst 2.3 mm, Run forward R pad vs halo arc 3). `bake` also reports `pad_follow_max`, `halo_clear_push_max_mm`,
    `pad_pop_mm_f2` and `halo_pop_mm_f2`.
  - **Pops:** pad acceleration relative to the chest stays under 15 mm/f² except where the arm itself snaps:
    Discharge's burst (71/91 mm/f², the declared accent), Arc Step start/end (25-45), Arc Bolt left (26). The
    close-ups read smoothly through those frames.
  - **Still visible, not clipping:** in Arc Step, Glide loop and Select intro the halo sits 3-9 cm further back than
    its keyed pose (the residual nudge), which reads as a trailing halo. The small back shard at extreme raises
    (item 1) doesn't show in the close-ups.
  - **Before/after (v19 top, v21 bottom), colour-coded close-ups (pads orange, halo cyan, yoke yellow) from front,
    back, top and both outsides:** `art/anim/wip/shoulders/<clip>/pads.gif` + `pads-sheet.jpg` for Rest, Idle,
    Idle combat, Run forward, Discharge, Open Circuit, Glide enter, Glide loop, Arc Bolt right, Aim up, Select intro
    and Arc Step start/loop/end. Tools: `pad_closeup.py`, `pad_sheet.py`.
- **Refinement item 9i (full-body audit fixes, FULL-AUDIT M1-M6 / S1-S10): DONE (v22, run 5).**
  - **Rig fixes on load (`bodyfix.py`).**
    - The tabard Shrinkwrap is removed for good (Blender-only, it hid M1). The TABARD now rides the body flap
      under it.
    - The collar skin gets a chest -> neck -> head weight gradient (M2/S4), and the scapula shells and back node
      move out of the skin.
    - Chest core, chest/rib/abdomen plates and sigil rings move as rigid blocks (M3/S5); rigid edge change is
      now under 2.2% everywhere.
    - Conductors, cuffs, shells and neck cables take their weights from the skin under them, so they follow it
      instead of tearing.
    - Thigh weights fade into the pelvis (S3), the shins are re-rolled so X is the knee hinge (S8), and forearm
      twist bones are added (S6).
  - **Tabard motion (`tabardpass.py`).**
    - The body's loincloth flaps swing just far enough to keep the legs 4 mm off their inner surface.
    - Per-bone bend limits (10-22 deg) stop the flap's large faces from bending through the TABARD cloth.
  - **Hands and forearm roll.**
    - `handpass.finish` blurs the forearm roll over time, but only within the range that still passes the
      orientation check. Seam and end frames stay exact.
    - The worst roll step dropped from 81 to under 20 deg/f² outside accents.
    - Run authors its own forearm roll (`ROLL_UP`) instead of letting the hand pass snap it.
  - **Feet.** `gait` `angle_blur` smooths foot pitch/toe in time, and the backward run matches touch-down speed to
    the stance slide.
  - **Poses.** Jump, Land and Discharge declare their deliberate snaps as `accents`: Jump f3/f5, Land f2,
    Discharge f7/f9. Special-move solves keep a per-pose waist margin, so elbows no longer sink into the hips
    (M5: Discharge 45 -> 3.7 mm).
  - **Result vs the v21 audit baseline** (`fixes/compare-v21-v22.md`):
    - Full QA passes on 0 -> 10 clips.
    - Worst contact 62 -> 17 mm, excluding the exempt hip/armpit creases.
    - Worst plain pop 125 -> 28 deg/f².
  - **Still failing (honest list).**
    - Shin into the body flap panels: Spawn 17, Run back 15, Run right 14, Select idle 8, Idle combat 7 mm.
    - Tabard front into thighs: Spawn 13.5, Glide enter 12.7, Run fwd 12.5 mm.
    - Arc Step feet/shins: 10-12 mm (S1 partial).
    - Neck cables into head: Spawn 10.7, Charge full 9, Aim down 7.9 mm.
    - Back conductors into neck: 6-9.7 mm in 9 clips.
    - Spawn halo: arc 2 into arc 1 11 mm; arc 4 into R pauldron 8 mm.
    - Plain pops of 21-28 deg/f²: Glide enter, Run fwd/back/left, Arc Step start/end, Jump/Walk tabard,
      Discharge f2 (the gather, to be reworked in item 11).
  - **S9 (leg symmetry): report only.**
    - The body mesh itself is 20-35 mm asymmetric (median) at the limbs, so mirroring the skeleton would pull
      the bones out of their own skin.
    - Permanent check 7 (L/R rest bones within 5 mm) therefore cannot pass on this mesh. It is reported, not
      gated.
  - **PROPOSALS (not approved):**
    - Detach the body flap panels into the TABARD objects so the tabard pass can move them. That would clear
      most of the shin/flap contacts.
    - Replace check 7 with a bone-to-own-skin fit test.
    - A symmetric re-sculpt of the legs.
- **Refinement item 9h (eight-direction locomotion): DONE (v23, run 5).**
  - **Travel and turn channels.** Every clip keys `hs_move_x` / `hs_move_y` on the root: the travel direction
    relative to facing (+x = character right, +y = forward), unit length while moving (0/0 when still). The heel jets and sparks
    (`vfx.py`) trail back along that vector instead of the facing, so a strafe's exhaust points sideways. Turn
    clips also key `hs_turn`, which runs 0..1 over the clip's `meta.turn_deg` (+ = left). Unity reads it to
    rotate the capsule in sync with the feet.
  - **Run and walk, 8 directions (`loco8`, `run_dirs`).**
    - All run directions share Run forward's 16-frame cycle and foot timing (L contact f1, R f9). All walk
      directions share Walk forward's 26-frame cycle. Contact frames are therefore identical across each blend
      space.
    - Run diagonals (forward/backward right/left) blend the two neighbouring cardinal upper bodies 50/50, as a
      blend tree would, with the feet solved along the diagonal.
    - New walks: backward, left, right and the four diagonals. The side walk is authored slow (0.6 m/s) so the
      feet stay about 19 cm apart as they close.
    - The strafe runs were re-timed to the shared cycle at 3.4 m/s. A wider stance plus a small L foot offset
      stops the legs crossing; the ankle gap stays positive through every pass.
    - v23 fix: on the left diagonals the L upper arm opens 5 deg, so the L hand clears the lead thigh (4-8 mm
      before, now 43-46 mm).
  - **Blend QA (`blend_qa.py`, `art/anim/wip/locomotion8/blend-*.json`).** It plays neighbouring pairs mixed
    50/50 and measures planted-foot slide.
    - The v22 four-direction space slid 199-309 mm per contact at 50/50.
    - The eight-direction space slides 10-34 mm (run) and 6-17 mm (walk).
    - The 1 cm target is met by 6 of the 8 walk pairs and none of the 8 run pairs. The worst pairs are Run
      forward with the forward diagonals (33-34 mm) and Walk forward with the forward diagonals (17 mm).
  - **Turns (`turns`).** Run lean left/right (loops, blend with Run forward), Run pivot 180 left/right (20 f)
    and Plant turn 90 left/right (16 f).
    - Each turn starts on Run forward f1 and ends on Run forward f9, with exact seams both ways.
    - v23 fix: both seams were 5-9 mm out (halo root and tabard front.3), because Run forward smoothed f9 and
      picked a different halo nudge direction. Run forward now pins f9 as a seam anchor, and the turns take Run
      forward's halo nudge direction (new meta `halo_clear_dir`).
  - **Arc Step in 4 directions (`arcstep_dirs`).** Back, left and right, each with start/loop/end, built on the
    forward Arc Step with exact seams to its rest pose. The loops are marked `air_ok` for use in the air.
    - Back: the body leans back and the knees tuck up ahead of the hips.
    - Left/right: the body rolls 36 deg into the dash with the feet folded behind. The arms tuck in low and bent
      ahead of the roll; with the arms high, the hand-orientation solve flipped the forearms (up to 359 deg/f²).
  - **Stitched GIFs (hero + chase, root motion on a checker floor), each at `art/anim/wip/locomotion8/<name>/`:**
    - `stick-circle`: run around all 8 directions.
    - `figure-8`: walk, two opposite circles.
    - `zig-zag`: forward-diagonal alternation.
    - `reversal-180`: run, then a pivot left, then run again, then a pivot right.
    - `diag-run-to-glide` and `glide-to-diag-land`.
    - `stitch.py` gained `out` and `root_motion` spec keys for these. Hand-off ratios are 1.0-1.4, except
      tabard front.3 at the Run forward -> pivot cut (3.29) and at BL -> B (2.03). Item 10 will look at those.
  - **Still failing (honest list).**
    - Hand contact at Arc Step left/right start f4: the travel-side hand brushes its own crouched knee, 24-31 mm
      beyond rest. Five variants were tried; none cleared it without bringing back the forearm flips. It is not
      exempted.
    - Pops: Arc Step start/end accent thigh pops of 72-85 deg/f² (accent limit 60). The turns have R shin pops of
      34-38 deg/f² at the drive-off (Run forward: 26), and Run lean right has an R forearm pop of 31.
    - Full-QA contacts: shin into the pelvis creases (6-17 mm) and the tabard into the shins/thighs (6-23 mm),
      the same classes as the cardinal runs. Arc Step right has L upperarm > spine 12 mm.
    - Blend slide is over 1 cm in every run pair (see above).
    - Cardinal run starts/stops (optional in the plan) are not done.
  - **PROPOSAL (not approved): foot IK for the blend space.** In Unity, turn on humanoid Foot IK for the
    locomotion layer, and have a small grounding script lock a foot on its planted interval (the contact
    markers). Authoring can't remove the last 1-3 cm of slide in a 50/50 mix of two different strides.
- **Refinement item 11 (Conduit Spear, Discharge snap, meter-full flourish): DONE (v24, run 5).**
  - **Conduit Spear** (`clips/spear.py`, 20 f, upper-body layer; legs untouched, first/last frame at rest like the
    other gestures).
    - f1-6: the chest winds back to the right while the R hand draws up beside the ear in a hammer grip (thumb
      up, palm in). `hs_spear` keys the lance materializing along the R forearm (0 -> 1, core flash f6), and the
      L arm rises to point at the target.
    - f6-7: the elbow leads, then the R arm drives forward and the lance leaves the open palm on **f7** as the
      chest turns through.
    - f8-11: the arm carries on down in front while the L arm folds in. f12-20: recovery.
    - Markers: Materialize 2, Draw 5, Spear release 7, Cancel 11 (Arc Bolt / Arc Step / jump / sprint), Fade 14
      (any state), Recovered 20. Meta `cancel` lists the windows (locked f1-7); `projectile_mps` is 150.
    - Release aim: the fingers point 12 deg off the target line at f7, 1.41 m up and 0.69 m in front. The game
      aims the projectile at the crosshair from the palm.
    - QA: **PASS**, including the 9f orientation check on both extended arms, hand contact, pads and full QA.
      Worst plain pop 13 deg/f²; the whip accent is 52 (limit 60).
    - Tuning notes: the first solved follow-through (down and across) put the hand on the other roll branch of
      the 9f check, and the hand pass flipped the forearm (116 deg/f²). The follow-through is now the release
      grip carried down in joint space. The draw hand sat behind the shoulder (27 mm into the chest) until it
      moved up and out.
  - **Discharge snap** (`special`, 14 f): the passive Discharge overlay. There is no gather: the arms fling from
    rest to the release on **f3** (a small tense on f2), hold the hit-stop on f4, then recoil and settle by f14,
    with the same halo burst as Discharge. Accents are f2-4. QA: **PASS** (finger accent 22.5, limit 24). The
    full Discharge clip is unchanged.
  - **Charge as the meter-full flourish.**
    - New **Meter full flourish** (`special`, 24 f one-shot): the arms rise into the Charge full cradle, ride two
      heartbeats and drop back to rest. It has a lighter head bow than the loop, because the neck cables met the
      head.
    - Charge loop and Charge full keep their motion; meta `role` now marks them as meter-full overlays: the
      loop is an idle-only accent while the meter waits, and the full clip is a held flourish.
    - QA: the flourish fails full QA only on the R forearm conductor into the forearm (6.0 mm, limit 5), the same
      contact Charge full has (5.9).
  - **Overlay stitches** (upper-body mask over locomotion, hero/side/chase GIFs, root motion) in
    `art/anim/wip/transitions/`: `overlay-spear-run`, `overlay-spear-strafe` (Spear over Run left) and
    `overlay-discharge-glide`. Each overlay fades out from its Fade marker. Hand-off ratios are 1.0-1.14.
    - Fix: `stitch.py` now fades out of an upper-body overlay from overlay-over-locomotion; it used to crash
      building that source.
  - **Not done:** the standing full-body Spear variant with a step-in ("if cheap"). Its legs have to seam into the
    Idle stand, which the gesture layer doesn't support yet (the item 10 stand-layer work).
  - **For Unity:** all four new clips go on the upper-body avatar-mask layer (spine and everything under it:
    neck/head, halo, scapulae, pauldrons, arms, hands). The lance VFX reads `hs_spear` and spawns from the R palm
    on the Spear release marker.
- **Refinement item 10 (run 5): every transition, smoothness first.** Done, in v25. The full write-up (blend
  table, mask, VFX rules, grids, flagged handoffs, GIF self-review) is in `art/anim/wip/transitions/TRANSITIONS.md`.
  Everything below is a **PROPOSAL** until Stuart approves it.
  - **Coverage:** 149 stitched sequences with 402 handoffs.
    - Every state pair: idle / idle combat / walk / run / glide / jump / fall / land, all 8 run and walk
      directions, leans, pivots and plant turns.
    - Arc Step (4 directions) from idle, run, air and glide.
    - Every upper-body skill faded in and out over every state.
    - Cancels (Arc Bolt -> Arc Step, Arc Step -> Arc Bolt, Spear -> Arc Bolt/Discharge, a glide or jump during
      a cast), jumping mid-cast, stopping mid-stride, and direction changes (zigzag, reversals, a full stick
      circle).
    - The Discharge snap over every state.
  - **Result:** 399 pass and 3 are flagged. All three were reviewed frame by frame and look smooth:
    - Open Circuit fading in at one run phase (L hand 91 vs 59 mm/f²; a big pose change carried by the 10-frame
      ease).
    - Glide -> Arc Step left/right: the lit jets re-aim 90° over the 5-frame blend.
  - **Root travel:** worst 70.6 m/s² (limit 80), at Glide exit -> Run stop. PROPOSAL: give Glide exit a speed
    curve.
  - **Judging (PROPOSAL):**
    - Position pops: ≤ 1.25 × reference + 5 mm/f² on 11 tracked points. The reference is the largest of the
      in-sequence peak, the clips' own peaks, and the ease allowance `6d/(blend+1)²`.
    - Rotation: fullqa's pop limit, with accents allowed.
    - VFX intensity and lit-jet turn rate.
  - **Blend table:** Unity cross-fade lengths per transition are in TRANSITIONS.md. Examples: 8-way direction 10,
    walk -> idle 12, run -> jump 6 (Jump from f4), Arc Step in/out 4, skill fade-in 3-10, skill fade-out 6,
    skill chain 2.
    - Run stop / Run stop R / Run start seam exactly, so they need no blend.
  - **Upper-body mask:** `spine` and all its descendants (58 bones). Locomotion keeps root, pelvis, legs, tabard
    and IK.
  - **VFX:**
    - The skill layer supplies `hs_spear`, and `hs_glow` is the max of both layers. Everything else follows
      locomotion.
    - Jet/spark gain is `smoothstep((|hs_move|-0.3)/0.4)`, so a reversal dims the exhaust instead of flipping it.
  - **Outputs** (in `art/anim/wip/transitions/`):
    - `matrix.md`: every handoff.
    - `grids.md`: state -> state and skill-over-state grids.
    - `velocity-report.md`: speed, acceleration and pop per handoff.
    - `gifs/`: hero and chase GIFs plus a review sheet per sequence.
    - `specs/matrix/`: the sequence specs.
  - **Inherited, not transition faults:** Run forward's legs (L/R shin pops 23-28 deg/f², tabard > thigh) show in
    the stop/start clips too.
- Tooling: `refresh.py vN` reruns every preview (QA and renders), seams, sheets/GIFs, review.html
  and writes `art/anim/vN/qa-summary.json`. `preview.py` no longer deletes frames; stale frames
  move to `<clip>/stale/`.
  `motion_range.py -- a.blend b.blend --clips ...` compares how far key bones travel per clip;
  `qa_diff.py` diffs QA fields; `crop_frames.py` tiles full-size frame crops. `probe.py` prints bone
  directions (`^bone`) and object axes (`@object`); `jet_probe.py` prints world jet vs foot positions.

## Plan for the night

1. Merge finished modules into anim v2, v3...
2. Abilities: Charge loop / full-charge hold / Discharge, Arc Step dash (start/loop/end),
   Open Circuit crown unfold (rebuilt from the v10 studies as layered gestures).
3. Presentation: idle polish, spawn/intro, character-select idle. Death is ragdoll (no clip).
4. Smoothness pass across all clips; shoulder correctives where poses show crumpling.
5. Later (user): VFX / SFX.

## Open issues

- Shoulder crumple in high arm raises: much reduced in v7 (helper bones), small back shard left. Pads no longer clip
  (9g, v21).
- No export or in-game test yet.
- Crown hold sways +-9 deg; a continuous spin would be done in Unity on `halo root`.
- Arm bones sat 3.75 cm off the mesh centreline (fixed in v19 by `armfit.py`). The R arm mesh is still not a mirror
  of the L arm mesh, so L/R arm poses can't mirror exactly.
- For Unity: halo bones on their own avatar-mask layer would let the crown hold play under Arc Bolt.

## Item 12: natural hand curl (Sep 27 2026, run 6) -> v26, v27

- **Problem:** the v18 finger bones are modelled as a claw (palm cocked 11-14 deg back, index/middle tips
  pre-hooked, little tip bent back, thumb tip hooked 56 deg). Clips author curl as one local rotation per joint, so
  every clip kept the claw.
- **Fix:** `tools/blender/anim/handpose.py`, run inside the bake for every clip. Each finger is rebuilt from
  anatomical joint bends, taken from a library keyed by the clip's authored curl level (straight, open, relaxed 21,
  casting 36, fist-light 46, grip 64, fist 92):
  - Curl cascades from index (least) to little (most).
  - Every joint bends toward the palm, and no tip out-curls its middle joint.
  - Curled fingers converge so their tips don't cross.
  - The thumb sits alongside the index, slightly opposed.
  - Low arms flex the wrist 6 deg toward neutral.
  - A spring settle adds 2-4 frames of finger lag and a follow-through relax.
  - A last pass extends the wrist only as far as needed to keep the fingers 15 mm off the thighs.
  - The 9f orientation roll is unchanged.
  - Conduit Spear (v27) now closes the lance hand into a grip on the draw and opens it into the open-palm release.
- **New QA:** `handnat.py` inside full QA (`hand_nat_ok`, part of `full_qa_ok`) checks:
  - joint range, with no backward bend;
  - sideways bend;
  - curl order index to little;
  - finger-finger and finger-body penetration (2 mm);
  - finger pops.
- **v27 results:**
  - 65 clips, **13/65 PASS**, the same 13 as v25. Seams: 72, worst 0.001 mm.
  - 9f orientation: 65/65.
  - Natural hands: 63/65 clean. All bends are 0.5-93.6 deg, sideways 0, curl order within 4.6 deg, worst finger
    pop 9.3 deg/f², worst finger penetration 1.5 mm.
  - v25 on the same check: 51/65 clips fail.
- **Remaining:** Arc Step left/right start f4 (the launch frame) fail finger contact. The hand and forearm already
  sat 24-46 mm inside the thigh/shin there in v25, and the curled fingers now read deeper (L 40 mm, R 29 mm). This needs
  the arm pose on that frame re-authored in `arcstep_dirs`.
- **Previews:** `art/anim/wip/hands2/` (details in HANDS2-RESULT.md). Before/after hand GIFs for 14 key clips also
  appear on the clip cards in `art/anim/review.html`.
