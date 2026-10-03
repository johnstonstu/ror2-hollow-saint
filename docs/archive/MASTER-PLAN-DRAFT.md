# Hollow Saint: master plan (DRAFT, PROPOSED, awaiting Stuart's approval)

Drafted Sun Sep 27, 2026, ~12:45 AM PT. **Nothing in this plan is actioned until Stuart approves it.** The one exception
is run3 (9d+9e), which is already in progress. Details: `art/anim/REFINEMENT-PLAN.md` (items 9h, 11, order update),
`art/vfx/VFX-ABILITY-PLAN.md` (2.9, 2.10, 5b, 5c), and `art/anim/STATUS.md`.

## (a) Where we are
- **Checkpoint:** `art/anim/hollow-saint-anim-v16.blend` (12:24 AM PT), 33 clips on the v18 model, QA 33/33 PASS.
  All 30 seams are 0.0 mm, with no foot slide.
- **Clips:** Idle, Idle combat, Walk forward; Run forward/backward/left/right (16 f each); Glide enter/loop/exit (Iron Man
  upright); Jump/Ascend/Descend/Land; Arc Bolt L/R and 5 aim poses; Arc Step start/loop/end (forward only);
  Charge loop/full, Discharge, Open Circuit/hold/end; Spawn, Select idle/intro.
- **Refinement items done:** 1 shoulder helpers, 2 Charge, 3 secondary motion, 4 locomotion upper body, 5 tabard, 6 weight,
  7 Arc Bolt body, 8 aim/IK, 9 glide (revised), 9b arm clearance, 9c hands and the v16 contact follow-up.
- **In progress:** 9d+9e (feet/thrust progression and heel jets) by run3 (Cursor CLI, started 11:42 PM PT).
- **Known open issues:**
  - **9f hand orientation** (`art/anim/wip/audit/ORIENT-AUDIT.md`): the thumb points down on extended arms in
    Discharge, Open Circuit, Arc Bolt, Arc Step end, Charge full, Select intro and Jump. The audit also finds the hand
    geometry **appears mirror-handed** (each side carries the other side's finger layout), so forearm roll alone can't
    fix it. It probably needs a rig-level fix, and a human should check the audit renders first.
  - **Shoulder pads / crumple:** much reduced since v7, but a small back shard is left on high raises. A pad audit is
    running in `art/anim/wip/audit-shoulder/` (9g).
  - **L/R arm mirroring:** the arm bones sit 3.75 cm off the mesh centreline, so L/R poses don't mirror exactly.
  - **Socket export risk:** the plan says "FBX, deform bones only", but every effect socket is non-deforming, and the
    heel-jet mounts are objects, not bones. Without a fix, the ChildLocator has nothing to point at.
  - **`hs_glow`/`hs_jet` curves:** Unity drops the Blender drivers, and nobody has checked yet whether the custom
    property curves survive import.
  - **No export, Unity project or in-game test yet.** Unity 2021.3.33 isn't installed; Hub only has 6000.5.

## (b) Goal
A playable RoR2 survivor (game 1.4.1, Unity 2021.3.33) with controller-smooth movement. A stick circle, a figure-8,
reversals and any skill pressed mid-move should all blend without pops or foot sliding. The kit is Arc Bolt / Conduit
Spear / Arc Step / Open Circuit plus the passive Discharge, with custom lightning VFX, and it has to work host + client.

## (c) Phases (in order)
Effort is rough agent wall-clock time, excluding Stuart's review time.

### Phase A: animation fixes (~4-6 h)
- **Scope:** finish 9d+9e (walk heel-toe, run sparks, glide ignition, heel/Achilles jets trailing along travel);
  9f hand orientation (thumb up, palm forward, fingers curl in, mirrored), including a **rig-level mirror check**
  that confirms or fixes the mirror-handed hand layout before any roll fixes; 9g shoulder pads.
- **Deliverables:** new numbered .blend (v17+), updated STATUS, hand orientation GIFs in
  `art/anim/wip/hands/orientation/`, stitched walk → run → glide GIF.
- **QA:** automatic thumb-up/palm-forward check in hand QA (part of PASS), pad clearance and crumple metrics, seams 0.0 mm.

### Phase B: eight-direction locomotion and directional jets, 9h (~8-12 h)
- **Scope:**
  - 11 new clips: Run NE/NW/SE/SW; Walk backward/left/right/NE/NW/SE/SW.
  - Turn clips: pivot 180 L/R, plant-turn 90 L/R, cardinal starts/stops, a sprint lean-turn.
  - Strafe counter-rotation.
  - Heel jets and sparks oriented to the travel vector (`hs_move_x/y`).
  - Arc Step in 4 directions (forward, back, left, right; diagonals by blend).
- **Deliverables:** new .blend; `art/anim/wip/locomotion8/` GIFs (stick circle, figure-8, zig-zag, 180 reversal,
  diagonal run → glide, glide → diagonal landing).
- **QA:** blend foot slide < 1 cm per contact on neighbour 50/50 blends, contact frames identical across the set
  (run 16 f, walk 26 f, L contact f1), no leg crossing, hand QA, seams 0.0 mm.

### Phase C: new ability clips (~5-7 h)
- **Scope:**
  - **Conduit Spear** (item 11): a 20 f right-palm thrown lance with release f7 and a forearm materialize beat.
    Upper-body layer, plus a standing variant.
  - **Discharge as a passive overlay:** a short `Discharge snap` (~14 f, release f3), so the visual meets the
    instant auto-trigger.
  - **Re-examine Charge:** with an auto passive there is no held charge input. Options are to reuse Charge full as
    a brief "meter full" flourish overlay, use it as the Select-screen or idle-combat accent, or retire both clips.
- **Deliverables:** new .blend with event markers (Release, Interrupt, Cancel).
- **QA:** 9c/9f hand checks, contact/clearance, release-frame socket aim.

### Phase D: transitions, 10 EXPANDED, LAST of the anim work (~8-12 h)
- **Scope:**
  - Every real gameplay pair, including the new clips.
  - Stick-circle smoothness.
  - Cancel windows as markers (Arc Bolt interrupt at ~f13, Spear from f11, Arc Step from end f5).
  - Discharge overlay over every state: idle, walk/run in 8 directions, glide, air, Arc Step, and mid-cast.
  - Secondary motion (tabard, halo, jets, glow) never resets.
- **Deliverables:** `art/anim/wip/transitions/` GIFs, `TRANSITIONS.md` matrix (pass/fail per pair), new .blend.
- **QA:** per-pair velocity/acceleration jump report, frame-by-frame self-review.

### Phase E: sockets and export (~4-6 h)
- **Scope:**
  - Add sockets as bones: `L/R palm`, `orb`, `L/R heel jet`, `back`, `spear`, halo arc tips, `ground`.
  - Keep them in the FBX, since the export must include non-deforming socket bones.
  - Verify whether the `hs_glow`/`hs_jet`/`hs_move` curves import (fallback: Unity clip curves or runtime values).
  - Fix the L/R arm mirroring (3.75 cm offset).
- **Deliverables:** test FBX (Arc Bolt R + Glide enter), then the full set, plus an export README.
- **QA:** socket positions at event frames match Blender, and curves are present or the fallback is documented.

### Phase F: VFX source assets (~4-6 h, can run in parallel)
- **Scope:** the asset list in VFX-ABILITY-PLAN 5c:
  - bolts: short, long, branching, halo ring arc
  - heel-jet cone/ribbon
  - sprites and textures: spark sprite, glow cards, bolt flipbook, noise texture, meter glow ramp
  - Conduit Spear lance + trail
  - conductor mark, afterimage/trail, scorch decal
- **Deliverables:** `art/vfx/assets/` (numbered `hs-vfx-vNN.blend`, `textures/`, `fbx/`, `previews/`, README).
- **QA:** alpha PNGs, palette matches (cyan-white core, copper accents), preview per asset, no anim files touched.

### Phase G: Unity/RoR2 integration (~20-30 h)
- **Scope:**
  - Setup: Unity 2021.3.33 project, dependencies in the "Hollow Saint Dev" profile only, and a tiny asset
    bundle load test.
  - Animator: locomotion 2D blend space (8 directions, walk/run by speed), sprint/glide, upper-body gesture layer,
    additive aim layer, halo layer.
  - Hookup: ChildLocator, prefabs from Phase F, a `HollowSaintVFXController` for the jets and glow.
  - Gameplay: EntityStates firing at normalized marker fractions, SkillDefs, cooldowns/tuning from the
    VFX-ABILITY-PLAN 5b proposal (all configurable), the Discharge meter UI, skill icons.
- **Deliverables:** local build in the dev profile.
- **QA:** the design-brief acceptance checks (chains, charge math, empowered cap, attack-speed scaling).

### Phase H: in-game testing and polish (~10-15 h + Stuart's playtests)
- **Scope:**
  - Controller playtests: stick circle, reversals, skill spam and cancels.
  - Attack speed 1×/2×/3×.
  - Crowds and an isolated boss.
  - Item procs (Ukulele).
  - Death/stage reset.
  - Host + client.
- **Deliverables:** a test log with repro steps, tuning passes, recorded clips.

### Phase I: repo/GitHub setup (~1 h, ONLY with Stuart's explicit approval)
- **Scope:**
  - Init the repo.
  - `.gitignore` already excludes env files; verify `.env` is never staged (and add a pre-commit check).
  - Decide on LFS for .blend/FBX/PNG.
  - Confirm the GitHub identity before adding a remote.
  - First push, then releases only when Stuart names them.

## (d) Parallel work
- **Phase F (VFX assets)** can run alongside A-D: it uses its own folder and its own .blend files, and never touches
  `art/anim/`, the rig or `tools/blender/anim`.
- **Phase G setup** (Unity 2021.3.33 install, dependencies, bundle test, C# skeleton with placeholder visuals) can
  run alongside A-F.
- The anim phases stay serial: one agent per clip module, and only the coordinator saves .blend files.

## (e) Decisions made so far
- Hollow Saint A / Cracked Icon look; v18 model; 24 fps; clips in place; the character faces -Y in Blender.
- Aimed primary with automatic chains; hits build charge. **Discharge is a PASSIVE:** the meter builds from most
  abilities and fires automatically on the next enemy hit at 100%, as an upper-body overlay.
- Walk/run on the claw feet; sprint becomes an **upright Iron Man glide**; ragdoll death; the halo lags and sways.
- **Heel jets** come from the heel/Achilles, with exhaust streaming back along the travel line.
- **Extended arms:** thumbs up, palms forward, fingers curling in, L/R mirrored (standing rule).
- Hands (9c) and orientation (9f) QA are standing requirements. Transitions/smoothness are the top priority after the fixes.
- Controller play: smooth 8-direction movement and blending into every movement ability (12:37 AM).
- Conduit Spear: agent's judgment requested. Proposed design: right-palm thrown lance, 20 f, release f7, conductor mark.

## (f) Open questions for Stuart
1. Approve this plan and the phase order (A → B → C → D, with E-G after; F and G setup in parallel)?
2. Conduit Spear design OK (right-palm thrown lance, projectile, 5 s cooldown, 450%, 6 s mark)?
3. Cooldown proposal OK? Arc Bolt 0.5 s (repo), Spear 5 s, Arc Step 2 stocks × 5 s, Open Circuit 12 s with an
   8 s buff, and Discharge at 100 meter (~10 bolt hits).
4. Arc Bolt timing: keep 0.5 s with the next shot interrupting at ~f13 (recommended), or speed up the whole clip?
5. Charge loop/full clips: repurpose as a "meter full" flourish, keep them for select/idle, or retire them?
6. Which meter sources count: all damaging skills except crown pulses and item procs (proposed)?
7. Arc Step: 4 directions OK? Usable in the air? Any i-frames (the concept doesn't imply any)?
8. Should the jets fire on jump/land too, and should Open Circuit strike during glide/Arc Step?
9. Mirror-handed hand geometry (ORIENT-AUDIT finding 2): OK to fix at rig level after a human look at the renders?
10. Launch the VFX asset run (Phase F) in parallel now, or after Phase A?

## (g) Process rules
- Never read, print or commit `.env`. No git, pushes or remotes until Phase I is approved. Never delete anything.
- Background Blender only. Never touch Stuart's open Blender (PID 4500) or any Blender an agent didn't start. Never
  use blender-mcp or connect to a running Blender (every agent prompt must forbid it).
- One agent per clip file (tools/blender/anim/README). Save new numbered files and never overwrite a checkpoint. Only
  the coordinator saves anim .blend files.
- Before writing shared docs (REFINEMENT-PLAN, NEXT-RUN-NOTES), re-read them and append only, with a trailing
  newline before a new heading. Back up before editing plan docs (`.bakN`, never overwrite backups).
- Every anim run re-reads REFINEMENT-PLAN.md and NEXT-RUN-NOTES.md before each item.
- Cursor CLI runs: headless, detached with a hidden window, working dir = the project, logs next to the prompt file.
  Don't kill or relaunch a running agent.
- Label unapproved numbers and designs as PROPOSAL. Report times in PT. No publishing or releases without Stuart
  naming them.
