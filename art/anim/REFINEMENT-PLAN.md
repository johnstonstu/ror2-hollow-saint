# Hollow Saint anim v6: refinement critique

Source: `art/anim/hollow-saint-anim-v6.blend` (33 clips). I reviewed the review.html GIFs (EEVEE, half frame rate)
through contact sheets in `sheets/<clip>.png` and cross-checked them against `v6/catalog.json` and the seam report
(`qa/`). This is a snapshot from 2026-09-26 ~19:45 PT.

## QA numbers (from the PC)
- **Seams:** all 30 measured seams are 0.0 mm, except Land f15 -> Idle and Idle -> Jump f1, which are both **1.23 mm** (L shin).
- **Foot slide:** the directional runs measure 1-2e-5 m/frame (effectively zero). STATUS reports zero slide on all
  locomotion. The run contact pop dropped from 141 to ~90 mm and the walk from 47 to 36 mm, so both are still visible.
- **IK:** Arc Step start/loop/end have **ik_miss 1.2 mm** and **max_leg_extension 1.01** (legs fully straight or
  slightly over-extended; every other clip stays at 0.99 or below).
- **Arm raise (shoulder-crumple risk):** Discharge 68.2 deg, Open Circuit 64.9 deg, Select intro 54.7, Select idle 48.4,
  OC hold/end 44.6. Arc Bolt shoulder swing at release is -71 deg (R) and -77 deg (L). The known crumple is on high raises.
- **Pops:** Idle finger pop 21 deg/f^2 (R little.2, f78). Select intro R-hand 12.6 deg/f^2 (f10). Spawn halo 7.1 deg/f^2 (f41).
  Discharge R-forearm 67.8 deg/f is an intended snap.
- **Pauldron follow error:** Discharge 7.3 deg, Charge full 7.2 deg, Charge loop 4.2 deg.
- **Motion energy** (mean side-view pixel change per GIF frame; higher means more movement): run 6.9, walk 4.2, glide loop 0.66,
  ascend 1.2, arc-step loop 0.84, idle 0.53, select idle 0.45, OC hold 0.32, charge full 0.32, charge loop **0.19**.
  Every loop wraps cleanly (wrap diff is about the same as a normal frame step), so none of them pop at the loop point.

## Per-clip notes

### Locomotion
- **Run forward (16f, 6 m/s):** Good lean and flight phase, and it has the strongest silhouette in the set. The arms pump in a
  small arc tucked at the ribs, so give them more fore/aft swing and counter-rotate the chest against the pelvis. The tabard
  reads as a rigid board; add follow-through and a lag on the contact frames. The contact pop is still ~90 mm, so soften the
  contact-to-down frames.
- **Walk forward (26f, 1.5 m/s):** Calm and readable. The knees stay bent through the whole cycle, which reads as a creep,
  so straighten the passing pose. The arms hang with the claws splayed and swing very little. Add more pelvis tilt and a
  small vertical bob.
- **Run backward (4.25 m/s):** The torso is too upright and the arms are held forward like a boxer. Lean the torso back
  slightly into the backpedal and loosen the arms.
- **Run left / right (4.5 m/s):** The legs work (no crossing, no slide), but the upper body is frozen upright with the hands
  hovering at the waist. Lean the torso and head into the travel direction, add a shoulder dip on each push-off, and give
  the halo some lateral lag.

### Sprint / glide
- **Glide enter (12f):** A strong push-off-to-skim arc, the best transition in the set. The halo tilt snaps between f6 and f9, so ease it.
- **Glide loop (32f, 8.7 m/s):** Almost static (energy 0.66). The hover high/low amplitude doesn't read, the legs trail
  without any flutter, and the hero-camera silhouette collapses into a blob. Add a bigger bob, alternating leg drift, and
  finger or tabard streaming, and consider rolling the torso slightly so the chest light shows.
- **Glide exit (13f):** It goes from horizontal to upright in about 3 GIF frames (f7-f10), so it feels abrupt. Add an overshoot
  (knees up, torso past vertical) before the run hand-off.

### Air
- **Jump (10f):** The crouch at f3 is shallow and the arms don't drive upward, so the jump has little power. Deepen the crouch,
  swing the arms up at takeoff, and give the halo a downward lag.
- **Ascend (20f loop):** Reads fine but is quite still. Add a slow drift in the knee tuck and arms.
- **Descend (20f loop):** The feet are pigeon-toed and crossed. In side view the tabard sticks out behind the body as a
  rigid triangle. The arms are static, so add an upward float to the arms and tabard to sell the fall.
- **Land (14f):** The compression at f6 is light for a claw-first landing, and it snaps upright by f11, which feels floaty or weightless.
  Deepen the knee compression and hold it 2 frames. Fix the 1.23 mm Land -> Idle / Idle -> Jump shin offset.

### Primary (Arc Bolt + aims)
- **Arc Bolt R / L (19f):** Only the arm moves; the chest, hips, and head don't rotate into the cast. Add chest twist, a small
  weight shift, and a head nod on release (f5). The arm reaches ~75 deg forward, which is shoulder-crumple territory, so check
  the deltoid area and pauldron at f5-f10. It barely reads from the gameplay camera, so exaggerate it.
- **Aim up / down (+-66 deg):** Nearly all of the pitch sits in the spine and neck. Aim up gives a backward lean with the hips
  pushed forward, and aim down gives a deep hunch where the head drops into the chest. Distribute some pitch to the pelvis
  and shoulders, and check neck deformation.
- **Aim left / right / neutral:** Fine. Note that the L/R arms don't mirror exactly (arm bones sit 3.75 cm off the centreline).

### Arc Step
- **Arc Step start (6f):** A good crouch-and-lean into the arrow pose.
- **Arc Step loop (10f):** Essentially a held pose (energy 0.84). Add a subtle forward roll or ripple and halo trail
  flicker. The legs are over-extended (1.01) and there's a 1.2 mm IK miss, so bring them back to 0.99 or less.
- **Arc Step end (16f):** The lunge brace at f4 is good. After that it goes to a dead-straight rest by f12, which feels stiff,
  so add a settle with a slight overshoot. Same IK over-extension as the loop.

### Special
- **Charge loop (40f) / Charge full (24f):** These are the weakest reads in the set (energy 0.19 and 0.32). The hands
  cup at the chest and hide the orb, and from the chase camera the pose reads as arms folded. Charge full is hard to tell
  apart from Charge loop. Push Charge full into a clearly escalated pose (wider elbows, a lean back or forward, stronger
  tremor) and add a tiny torso pulse. The pauldron follow error is 4-7 deg here.
- **Discharge (27f):** The gather (f5) and burst (f8-9) now read from the chase camera, and the halo arcs blast out nicely.
  The release pose (arms flung ~45 deg down and out) sits close to the 68 deg raise that crumples the shoulder, so check
  the deltoids at f8-12. The recovery from f14 to f27 is long and slow, so trim it.
- **Open Circuit (29f):** The crown lift-and-split is the showpiece. The body stays passive, though: the torso doesn't
  lift, the chin doesn't rise, and the arms just open low. Add a chest-up, head-back beat as the crown rises. The raise
  peaks at 64.9 deg, which is in the crumple zone.
- **Open Circuit hold (24f loop):** The crown sways +-9 deg but the body is static (energy 0.32). Add a breath or hover
  pulse in the arms. As STATUS notes, a continuous crown spin belongs in Unity.
- **Open Circuit end (21f):** The crown folds back cleanly. The arms pass through the chest area around f9-11 (hand-chest
  ~40 mm), so check for intersection.

### Presentation
- **Idle (96f):** Readable but soldier-stiff and symmetrical: both hands claw at the thighs and the knees are locked
  slightly bent. A contrapposto weight shift (one hip dropped) and asymmetric hands would add character. The finger
  twitch pop at f78 is 21 deg/f^2, so ease it.
- **Idle combat (48f):** Good side silhouette. From the front the casting hand hides behind the body, so pull it out
  to the side.
- **Spawn (71f):** Strong kneel-to-rise story, and the halo unfolds nicely. The halo arcs pop around f41-47 (7 deg/f^2).
  The rise between the awaken and standing beats happens fast, so add a hand-on-knee push.
- **Select idle (96f):** Close to a freeze-frame (energy 0.45, body pop 0.1 deg). Palms-up is a nice idea, but it needs a slow
  levitation bob, a head tilt, and finger crackle so the select screen doesn't look paused.
- **Select intro (43f):** The finger snap is small on screen and gets lost at the chase distance. Raise the snap hand
  higher and closer to the face with a sharper 2-frame accent. It already reaches 54.7 deg of raise, so watch the shoulder.

## Top refinement items (ranked)
1. **Shoulder crumple on high arm raises.** Add shoulder corrective shapes or a twist/helper bone so the set can use
   raises above ~50 deg. Discharge (68 deg), Open Circuit (65), Arc Bolt (~75 deg forward), and Select intro (55)
   are already in the risky range, and more dramatic poses need it.
2. **Charge loop / Charge full read.** Neither reads from the gameplay camera and they're nearly identical. Make Charge full
   an escalated, readable pose and uncover the orb.
3. **Static loops.** Glide loop, Arc Step loop, Select idle, Open Circuit hold, and Ascend need secondary motion
   (bob, drift, tabard or finger flutter) so they don't look paused.
4. **Upper body in locomotion.** Add arm swing and chest/pelvis counter-rotation to the run and walk, and lean the torso
   into strafes and the backpedal. Right now the legs do all the work.
5. **Tabard secondary.** It reads as a rigid board in every clip and sticks out as a triangle in Descend. Add follow-through
   and a lag chain, or a jiggle bake.
6. **Weight on impacts and takeoffs.** Deepen the Jump crouch and the Land compression and hold them. Soften the run contact
   pop (~90 mm). Add an overshoot to the Glide exit and a settle to the Arc Step end.
7. **Arc Bolt body mechanics.** Add chest twist, weight shift, and head follow into the release so the primary fire doesn't
   look arm-only, and exaggerate it for the gameplay camera.
8. **Aim poses and IK.** Spread the +-66 deg aim pitch into the pelvis and shoulders instead of just spine and neck, fix the
   Arc Step leg over-extension (1.01) and 1.2 mm IK miss, and close the 1.23 mm Land/Idle/Jump shin seam.

## Showcase picks (box paths)
- `showcase/01-run-forward-hero.gif`: run forward, hero cam
- `showcase/02-glide-enter-side.gif`: sprint-to-glide push-off, side
- `showcase/03-jump-side.gif`: jump crouch to takeoff, side
- `showcase/04-arc-bolt-right-hero.gif`: Arc Bolt cast, hero
- `showcase/05-arc-step-start-hero.gif`: Arc Step crouch into arrow dash, hero
- `showcase/06-open-circuit-hero.gif`: Open Circuit crown unfold, hero
- `showcase/07-discharge-hero.gif`: Discharge gather and burst, hero
- `showcase/08-spawn-hero.gif`: Spawn kneel, awaken, halo unfold, hero

## 9. Sprint-to-glide rework (Stuart's feedback, Sep 26 8:39 PM PT) - HIGH PRIORITY after current item
Applies to Glide enter, Glide loop, Glide exit. Do this in its own checkpoint after the current item finishes.
- Too much forward lean: reduce the forward body pitch in enter and loop so it reads as a hovering glide, not a dive. Keep the silhouette readable from the gameplay chase camera.
- Feet: give the feet a deliberate direction (e.g. pointed down/back like thrusters, toes together) instead of trailing loosely.
- Thrust under the feet: add a lightning / thrust effect at the soles during the glide (emissive jets or crackling arcs from the soles), readable in hero, side and chase GIFs.
- Transition glow: enhance the glow (halo, veins/emissive, sole jets) as the character goes from running into the glide, ramping up in Glide enter, sustained in loop, fading in exit.
- Keep seams to Run and air clips at 0.0 mm and loops clean. Regenerate GIFs, review.html, and note it in STATUS.md.

## 10. Transition polish pass (Stuart's request, Sep 26 9:09 PM PT) - do LAST, after items 1-9
Polish how clips flow into each other, not just positional seams (already 0.0 mm).
- Check every real gameplay transition pair: idle/combat idle to run/walk and back; run to strafes/backpedal; run/sprint to glide enter, glide exit to run/fall; jump to ascend to descend to land to idle/run; locomotion into and out of Arc Bolt, aim poses, Arc Step start/end, Charge/Discharge/Open Circuit; spawn to idle; select intro to select idle.
- Match velocity and momentum across each seam (no sudden speed or direction change in hips, hands, halo, tabard), ease in/out, carry anticipation and follow-through across the cut, and make sure secondary motion (tabard, halo, glow) does not reset or pop at the handoff.
- Render short stitched transition previews (clip A into clip B, hero + chase camera) as GIFs under art/anim/wip/transitions/, add them to review.html, and report per-pair velocity/acceleration jumps in QA.
- Save as a new numbered checkpoint; update STATUS.md.

## 9 (REVISED, supersedes item 9 above) + 9b. Stuart's feedback on v9, Sep 26 9:25 PM PT - do these NEXT, before item 10
### 9b. Run arm clipping (v9 regression)
- In Run forward (and check walk, strafes, backpedal, sprint), the arms/hands clip through the hips and sides of the torso/tabard. Widen the arm swing path (shoulder abduction / elbow out a bit) so hands and forearms clear the hips at every frame, while keeping the stronger upper-body swing from item 4. Add a collision/clearance check to QA (min hand/forearm-to-hip/torso distance per frame) and fix any clip that fails.
### 9. Sprint-to-glide rework - Iron Man style (replaces earlier item 9 spec)
- Body nearly UPRIGHT during the glide: standing-straight silhouette, only a slight forward tilt (roughly 10 degrees at most). Do NOT lead with the chest at an angle.
- Glide enter: from the run stride, the feet lift off the ground and the character transitions into forward thrust, like taking off from a run.
- Foot thrusters: lightning thrust jets from the soles of the feet push the character forward, Iron Man style. Feet angled down and slightly back as thrusters, legs mostly straight and together, and arms relaxed at the sides or slightly back for stability (they may flare a little for balance).
- Glow: ramp up the sole thrusters, halo and emissive glow as the run turns into the glide; sustain it in the loop with a subtle flicker/pulse; fade and power down in glide exit as the feet come back to the ground into the run.
- Keep seams to Run and air clips at 0.0 mm and the loop clean. Render hero, side and chase GIFs.

## 9c. Hands and thumbs - STANDING REQUIREMENT for every clip (Stuart, Sep 26 9:28 PM PT)
Stuart wants close attention to hand and thumb position throughout ALL animations. Apply in every remaining item and do a dedicated hand pass alongside 9b/9:
- Thumbs: never bent backward, hyperextended, sticking straight out, or passing through the palm/fingers. Rest pose tucks the thumb along the index finger with a natural slight curl; opposition when gripping or casting.
- Fingers: relaxed natural curl that cascades (pinky most curled, index least) at rest and in locomotion; no flat paddle hands, no fingers interpenetrating each other or the body/tabard. Deliberate, readable poses for casting (Arc Bolt fingertip point, Charge orb cradle, Discharge/Open Circuit open splayed hands).
- Wrists: no broken or candy-wrapper twists; the wrist follows through the arm swing slightly behind the forearm.
- Smooth motion: no finger pops (fix the known 21 deg/f^2 finger pop at Idle frame 78) and no jitter; the hand pose blends smoothly across clip seams.
- QA: add a per-frame hand check (thumb and finger joint angles within natural limits, finger angular acceleration threshold, finger-to-finger and finger-to-body interpenetration) and render close-up hand preview GIFs for Run forward, Arc Bolt, Charge full, Discharge, Idle and Glide loop under art/anim/wip/hands/.

## 9d. Stuart's feedback on v11, Sep 26 11:14 PM PT - "close", but:
### Hands (again)
- Re-check hands and thumbs in every clip against section 9c after any other change; Stuart is still seeing problems. Render the close-up hand GIFs (art/anim/wip/hands/) and self-review them frame by frame before calling it done.
### Feet and thrust through the walk to run to glide progression
- The foot work in how the thrust builds from walk to run (and on into the glide takeoff) needs more work. Make the progression read clearly: walk (heel-toe, grounded, no thrust), run (stronger toe push-off, small sole sparks on each push), sprint into glide enter (last push-off becomes the lift-off; feet rotate into thruster position and the sole jets ignite with a visible burst), glide loop (steady jets), glide exit (jets cut, feet swing back to plant into the run stride).
- Foot orientation must be deliberate and consistent at each stage: no floppy ankles, no toes pointing in odd directions, no feet crossing. Contacts planted with zero slide.
- Render a stitched walk into run into glide enter into glide loop preview (hero, side, chase) under art/anim/wip/transitions/.


## 9e - Heel thrusters (Stuart, 11:41 PM)
Stuart, Sep 26 11:41 PM PT: "I don't know if I like how the lightning bolts are coming out of the toes of the feet. I'd rather have them coming out of the bases of the feet, kinda like the Achilles or the heel, pointing kinda in the direction of movement."
- Move the lightning jet emitters from the toes/soles to the heel / Achilles base of each foot.
- Jets stream along the line of travel: the exhaust trails backward behind the heels while the thrust pushes the character forward. The jets angle with the movement direction (not straight down).
- Applies everywhere jets appear: run push-off sparks, the lift-off ignition burst in glide enter, the glide-loop steady jets, and the jet cut in glide exit.
- Foot orientation must support heel emission: the feet should NOT point the toes at the ground like nozzles. Re-pose the thruster-stage feet so the heels face back along the travel line.
- Merge 9e with 9d: do them together in the same pass (one checkpoint), and re-render the stitched walk -> run -> glide preview with the heel jets.

## 10 EXPANDED - Gameplay transitions (Stuart, 11:41 PM)
Stuart, Sep 26 11:41 PM PT: "Imagine the gameplay is gonna be a lot of different buttons being pressed and things happening and movement changes, so more refinement on transition between animations. We have a good thing going here. Keep going. Take your time."
Supersedes/expands item 10. Still do this LAST, after 9d+9e and 8.
- Rapid, varied gameplay input is expected, so transitions must hold up under interrupts, not just clean A-into-B handoffs.
- First inventory the actual clip list and the likely gameplay state changes. Examples: idle, walk, run, sprint, jump, fall, land (into idle and into run), run -> glide lift-off, glide exit into fall or land, each skill (Arc Bolt, Charge, Discharge, Open Circuit, Arc Step) started from idle, run, air and glide, skill cancels back into run, jumping mid-cast, taking a hit or stopping mid-stride, and direction changes.
- For each important pair:
  - Match pose, velocity and momentum at the handoff points.
  - Secondary motion (tabard, hands, jets, halo) must not reset or pop.
  - Add blend-friendly start and end poses and, where needed, short dedicated transition clips.
  - Make the upper body layer cleanly over locomotion for casting while moving, if the rig/clip setup supports it (e.g. an upper-body avatar-mask layer in Unity; document the mask bones).
- Render stitched preview GIFs for every pair into art/anim/wip/transitions/.
- Write a transition matrix (markdown, e.g. art/anim/wip/transitions/TRANSITIONS.md) listing each pair, pass/fail, and notes.
## Priority note (Stuart, 12:30 AM Sun)
Stuart, Sep 27 12:30 AM PT: everything else is coming together, but the heel jets (9e) aren't done yet. Finish 9d+9e first.
- After 9d+9e, transitions are the top priority. Smoothness and polish matter more than anything else from here.
- Spend extra iterations on 10 EXPANDED. Self-review every stitched transition GIF frame by frame for pops, momentum breaks, secondary-motion resets (tabard, halo, jets, glow), foot sliding, and hand/thumb glitches. Keep iterating until they're clean.
- Prefer more transition pairs and short dedicated transition clips over speed. Take the time it needs.

## 9f — Hand orientation on extended arms (Stuart, 12:32 AM Sun) — MUST FIX, standing rule
Stuart, Sep 27 12:32 AM PT: "I don't know if the thumbs are pointing in the right direction. When both arms are extended out, the thumbs need to be pointing up in the air. The left hand: thumb pointing up, palm facing forward with the fingers curling in. The opposite with the right hand: thumb up in the air with the hand curling in. It kinda looks like the thumbs are facing down with the palms backwards in a few of the renderings."
- Rule: whenever an arm extends outward or forward (Discharge, Open Circuit, Arc Bolt, arms-out glide poses, Spawn, any T-pose-like reach), the forearm is rotated so the THUMB POINTS UP (toward the sky), the PALM FACES FORWARD (the direction the character faces / casts), and the fingers curl inward. Left and right hands mirror each other. Thumb-down / palm-back means the forearm is over-pronated or rolled wrong: that is a bug.
- Standing QA requirement for EVERY clip, now and after every future change.
- Hand QA must add an automatic orientation check: for frames where the upper arm is raised/abducted more than ~45 deg or the elbow is extended past ~140 deg, compute in world space the thumb direction (thumb metacarpal / proximal bone vector) and the palm normal. FLAG frames where the thumb's world-up (+Z) component is negative or the palm normal points backward (against the character's facing). Report per clip/hand: failing frame ranges, worst thumb-up value, palm-forward value. Counts toward PASS/FAIL.
- Fix at the forearm / upper-arm roll (supination), NOT by kinking or twisting the wrist. Wrist twist limits from 9c still apply.
- Close-up before/after GIFs of each fixed clip go in art/anim/wip/hands/orientation/.
- Priority: do 9f together with the rest of 9d+9e (hands), BEFORE 10 EXPANDED. Audit of v16: art/anim/wip/audit/ORIENT-AUDIT.md.
- **Mirrored-hand check FIRST (ORIENT-AUDIT.md, 12:45 AM):** the v16 audit found thumbs pointing down on extended arms in Charge full, Open Circuit, Jump, Arc Bolt, Arc Step end, Discharge and Select intro, plus evidence the hand rigs may be mirrored left-to-right (index finger at the back and little finger at the front at rest; fingers curl against the palm side). Before touching forearm roll, verify the hand/finger layout by eye in close-up renders. If mirrored, fix the finger/thumb layout or the auto-curl direction at rig level, THEN correct forearm roll, THEN re-check every clip. Forearm roll alone will not satisfy this rule.

## 9h — Eight-direction locomotion and directional heel jets (Stuart, 12:37 AM, controller) — PROPOSED, awaiting Stuart's approval
Stuart, Sep 27 12:37 AM PT: "The heel animations need refinement in every direction. I play on controller, so imagine somebody wiggling the left stick around in circles and having smooth animations between that and all the different movement abilities."
Context: in RoR2 the body faces the aim/camera direction while not sprinting, so a stick circle plays the directional set relative to facing. Sprint turns the body toward travel, so sprint and glide need lean-into-turn instead.
Inventory (v16): Run forward/backward/left/right (all 16 f, L contact f1, R f9; 6.0 / 4.25 / 4.5 / 4.5 m/s) and Walk forward only (26 f, L f1, R f14). No diagonals, no walk back or strafes, no turn/pivot/start/stop clips. Arc Step is forward-only (start 7 f, loop 10 f, end 16 f).
- **8 directions, walk and run** (N, NE, E, SE, S, SW, W, NW, relative to facing), built as a blend-space-friendly set: every run shares the 16 f cycle and every walk the 26 f cycle, with L contact on f1, matched stride phase and foot-contact timing, so the 2D blend (and walk-run speed blend) has no foot sliding. Add only the missing clips: Run NE/NW/SE/SW, Walk backward/left/right, Walk NE/NW/SE/SW (11 new clips). Diagonals must not cross the legs (reuse `run_dirs.track` with a diagonal `travel`).
- **Turning:** lean into turns (a sprint lean-turn additive or left/right lean poses), pivot/plant-turn clips for sharp reversals (Run pivot 180 L/R, Plant turn 90 L/R), start/stop clips per direction if cheap (at least 4 cardinal starts and stops), and hip/torso counter-rotation for strafing (hips toward travel, chest toward aim).
- **Directional heel jets and sparks:** push-off sparks and jets orient to the actual movement vector in every direction, with the exhaust always trailing opposite to travel (backpedal exhaust streams forward, strafes sideways). Replace the single pitch `hs_jet_dir` with a character-space travel vector (e.g. `hs_move_x` / `hs_move_y`) keyed per clip, so Unity can use the real velocity. The emitter stays at the heel/Achilles mount (9e); the spawn point can offset so exhaust never passes through the leg.
- **Arc Step in multiple directions:** the kit calls it a "short directional dash", so do forward, back, left and right start/loop/end (diagonals by blending). The backward step is the escape read from the concept; the jets fire along the dash vector.
- **Stitched test GIFs** (hero + chase) in `art/anim/wip/locomotion8/`: stick circle (N -> NE -> E -> ... -> N), figure-8, zig-zag strafes, 180° reversal, run into glide from a diagonal, glide into a diagonal landing.
- **QA:** foot-slide measurement on the blends (sample 50/50 blends of each neighbour pair, planted-foot slide < 1 cm per contact), cycle-phase match (contact frames equal across the set), no leg crossing, standing 9c/9f hand checks, seams 0.0 mm.
- New numbered .blend only; one agent per clip module (README).

## 11 — Conduit Spear (Stuart: use best judgment) — PROPOSED, awaiting Stuart's approval
Grounded in `docs/ability-kit-workshop.md` + the concept prompt: a short-lived energy lance fired from an extended palm, not a carried weapon; focused single-target damage plus a broken four-segment cyan ring mark (one marked enemy at a time; chains prefer it); own cooldown, no charge spend.
- **Design:** a thrown lance. It materializes along the RIGHT forearm, is drawn back beside the shoulder, and is launched javelin-style off the open palm (thumb up, palm forward per 9f) while the left arm points at the target as an aim guide. It is a fast projectile (proposal ~150 m/s), not hitscan, so the lance reads in flight.
- **Frames (20 f, 0.79 s at 24 fps, same length as Arc Bolt):** anticipation f1-6 (weight back, R hand draws back, spear materializes f2-6), **release f7** (0.25 s), follow-through f7-11 (chest turns through, R arm extends, L arm pulls in), recovery f12-20 (exact rest at f20).
- **Cancel windows:** locked f1-7; Arc Bolt / Arc Step / jump / sprint from f11; any-state fade from f14.
- **Spear-materialize VFX beat:** f2-6 cyan filaments converge along the R forearm conductor into the lance, then the core flashes white at f6; release flash at the palm on f7 plus a trail ribbon.
- **Moving version:** upper-body layer (mask excludes pelvis/legs), so it plays over any locomotion or glide. Also a standing full-body variant with a small step-in if it's cheap.
- Sockets: new `R palm` (launch), `L palm` (aim guide), optional `spear socket` along the R forearm for the materialize.
- New clips go in a new numbered .blend; QA includes the 9f orientation check on both extended arms.

## Order update (Stuart, 12:37 AM) — PROPOSED, awaiting Stuart's approval
1. 9d+9e (in progress, run3)
2. 9f+9g
3. 9h
4. 11
5. 10 EXPANDED LAST: covering all new clips, stick-circle smoothness, the Discharge overlay over every state, and skill cancels.
Full consolidated plan: `art/MASTER-PLAN-DRAFT.md`.

## 9g — Shoulder pad / pauldron clipping (Stuart, 12:36 AM Sun) — MUST FIX
Stuart, Sep 27 12:36 AM PT, on an Idle/hero render: "the shoulder looks like it's clipping a little bit in ways that it shouldn't. Can you take a look at that animation." (Both pads tilt and clip into the chest and upper arm, worst on the character's right / screen-left.)
- Rule: the pauldrons ride ON TOP of the shoulder and never penetrate the chest, collar, neck or upper arm, in any clip. Also clear the halo's lower arcs and the chest yoke bar.
- Orientation follows the clavicle/shoulder with a smooth PARTIAL rotation: not rigidly locked to the upper arm and not locked to the chest. When the arm raises, the pad lifts and tilts outward without swallowing the arm or digging into the collar.
- Check how the pads are driven (weights, child-of, separate bone, or rigid parenting). v16 audit (art/anim/wip/audit-shoulder/SHOULDER-AUDIT.md): `L/R SHOULDER | V17 pauldron upper` is rigidly skinned (one vertex group) to bone `L/R pauldron`, parented to `L/R scapula` (fixed to `chest`), with "Follow upper arm 40%" COPY_ROTATION baked and muted. There is no clavicle and no lift, so the pad just rotates about its pivot. Big raises push its inner edge into the collar/neck/chest (Arc Bolt R 46 mm, Arc Bolt L 38 mm, Arc Step start 35 mm, Select intro R 28 mm, Arc Step loop/end 19-23 mm, Discharge R 11 mm). At rest and in every clip, the pads also intersect halo arcs 2/3 and the yoke bar (static placement overlap, visible in the Idle hero shot).
- Fix: adjust the drive/weights, or give the pad bone a copy-rotation influence of about 0.4-0.6 from the upper arm PLUS the clavicle/shoulder (add a clavicle or shoulder-lift driver if needed) and a small upward/outward translation as the arm abducts. Fix the static overlap with the halo arcs and yoke bar (pad rest offset/scale or halo rest placement). Add key-level corrections where needed. Bake as before (no live constraints in export). New numbered .blend only.
- QA: extend `contact.py` to include the pauldron meshes against the torso/collar/neck/head/upper arm (plus the halo arcs and yoke bar). Threshold about <= 5 mm penetration, part of PASS for every clip.
- Before/after GIFs of Idle, run, Discharge, Open Circuit and the glide go in art/anim/wip/shoulders/.
- Also covers the known "small shoulder crumple left" in STATUS.md (item 1: "a small back shard remains at extreme raises"). Re-check it with the pad fix.
- Priority: do 9g together with 9f (hand orientation), BEFORE 10 EXPANDED.

## APPROVED (12:43 AM Sun)
Stuart, Sep 27 12:43 AM PT: approved `art/MASTER-PLAN.md` "as-is, start phases A and F".
- The PROPOSED sections **9h** (eight-direction locomotion and directional heel jets), **11** (Conduit Spear) and **"Order update"** are now **APPROVED**. Order: 9d+9e -> 9f + 9g -> 9h -> 11 -> 10 EXPANDED LAST.
- Conduit Spear as designed: right-hand javelin-style throw with the left arm pointing, release on f7, usable while moving (upper-body layer).
- Cooldowns: Spear 5 s; Arc Step 2 charges at 5 s; Open Circuit 12 s with an 8 s buff; the Discharge meter fills in about 10 bolt hits, fed by every damaging skill.
- Arc Bolt keeps its 0.5 s interval; the next shot interrupts at about frame 13 (add an `Interrupt` marker at f13).
- Charge loop / Charge full are repurposed as the "meter full" flourish (overlay), not retired.
- Arc Step in 4 directions (forward, back, left, right; diagonals by blend), usable in the air.
- Hand mirroring is fixed at rig level after a visual check of the close-ups (9f), then forearm roll, then re-check every clip.

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
