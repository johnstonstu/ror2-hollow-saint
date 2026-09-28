# Hollow Saint handoff

Updated Sun 2026-09-27 ~5:45 PM PT. Character: **Hollow Saint A / Cracked Icon** (settled, don't restart design).
The model is final ([hybrid v18](art/hybrid/hollow-saint-hybrid-v18.blend)); all work since is animation, VFX source
assets and planning. Approved plan: [art/MASTER-PLAN.md](art/MASTER-PLAN.md). Live anim status:
[art/anim/STATUS.md](art/anim/STATUS.md). Every clip as GIFs: [art/anim/review.html](art/anim/review.html).

## 1. Current state

**Run 6 (item 12, natural hand curl) is still running** as of 5:45 PM. It saved
[hollow-saint-anim-v26.blend](art/anim/hollow-saint-anim-v26.blend) at 5:25 PM and is now running the full refresh
(previews, QA, seams, GIFs, review.html). `art/anim/v26/qa-summary.json`, `art/anim/wip/hands2/HANDS2-RESULT.md` and
the STATUS entry for v26 don't exist yet. Treat **v25 as the last fully QA'd checkpoint** until run 6 finishes.

Checkpoints in `art/anim/` (numbered, never overwritten; v1-v18 history is in STATUS.md):

| File | Clips | Item | What it added |
|---|---|---|---|
| v19 | 33 | 9f + audit M6 | Thumbs up / palms forward on extended arms. The v18 hands were mirror-handed; fixed at rig level (`handfix.py`), R arm bones refit to the mesh (`armfit.py`), new orientation QA |
| v20 | 33 | 9g (draft) | Superseded by v21 (7 inexact seams). Don't use |
| v21 | 33 | 9g | Shoulder pads ride on the shoulder (`padfix.py`, `padpass.py`), halo moved back/up to clear them, pad contact QA |
| v22 | 33 | 9i | Full-body audit fixes (`bodyfix.py`, `tabardpass.py`): Shrinkwrap removed, neck gradient, rigid chest parts, twist bones. New strict full QA: 10/33 |
| v23 | 59 | 9h | 8-direction walk/run, leans, pivot 180s, plant turn 90s, Arc Step back/left/right, `hs_move_x/y` travel channels. **Last pushed to GitHub** |
| v24 | 62 | 11 | Conduit Spear (20 f, release f7), Discharge snap (14 f, release f3), Meter full flourish |
| v25 | 65 | 10 | Transitions: 149 stitched sequences, 402 handoffs (399 pass, 3 reviewed smooth); Run stop / Run stop R / Run start; Interrupt/Cancel markers. **Last fully QA'd** |
| v26 | 65 | 12 | Natural graduated finger curl (`handpose.py` library, finger settle/lag), natural-hand QA (`handnat.py`) in full QA. **QA in progress** |

Gates on v25: all 72 seams exact (worst 0.001 mm); bake, IK, hand QA, hand orientation, pads pass on every clip; strict
full QA **13/65 PASS** (details below). Run 6's mid-run log reported 10/65 on an early v26 build before further fixes,
so check the final number when it lands.

**VFX (Phase F): done.** [art/vfx/assets/hs-vfx-v07.blend](art/vfx/assets/hs-vfx-v07.blend) is current: 29 meshes in
11 FBX, 25 alpha PNG textures, previews and `previews/_overview.png`. Import settings, palette, sockets and per-beat
numbers (all PROPOSAL) are in [art/vfx/assets/README.md](art/vfx/assets/README.md). Hookup plan:
[art/vfx/VFX-ABILITY-PLAN.md](art/vfx/VFX-ABILITY-PLAN.md).

**Not started:** export (E), Unity project (G), in-game testing (H). Unity 2021.3.33 is not installed (Hub has 6000.5 only).

## 2. Design decisions (Stuart, approved 12:43 AM Sun unless noted)

- Kit: **Arc Bolt** (primary, aimed, auto chains, 0.5 s interval, next shot interrupts at f13), **Conduit Spear**
  (secondary), **Arc Step** (utility), **Open Circuit** (special), **Discharge** (passive).
- **Discharge is passive:** the meter fills (~10 Arc Bolt hits) from every damaging skill, including Open Circuit
  pulses; item procs give 0. At 100% it fires on the next enemy hit as an upper-body overlay (Discharge snap).
  Charge loop / Charge full are repurposed as the "meter full" flourish, not retired.
- **Cooldowns:** Spear 5 s; Arc Step 2 charges x 5 s; Open Circuit 12 s with an 8 s buff. All tuning stays configurable.
- **Conduit Spear:** right-hand javelin throw, left arm points at the target, release f7 of 20 f, usable while moving
  (upper-body layer), ~150 m/s projectile, 450% single target, 6 s conductor mark.
- **Arc Step:** 2 charges, 4 directions (diagonals by blend), usable in the air.
- **Hand rule (standing):** extended arms have thumbs up, palms forward, fingers curling in, L/R mirrored, fixed by
  forearm/upper-arm roll, never wrist kinks. Item 12 adds: relaxed hands curl gradually index -> pinky, no hyperextension.
- **Heel jets:** from the heel/Achilles, exhaust trailing opposite the travel vector (`hs_move_x/y`).
- **Glide:** sprint becomes an upright Iron Man glide (~7 deg lean), heel jets lit.
- **Controller-smooth 8-direction movement** that blends into every movement ability; transitions are top priority.
- Other: 24 fps, clips in place, faces -Y in Blender, ragdoll death, halo lags and sways.

## 3. Open items and proposals awaiting Stuart

Quality (from STATUS.md "Still failing" lists and TRANSITIONS.md):
- **Strict full QA is 13/65 on v25.** The older gates all pass; full QA (all-pairs contact at 5 mm, pops at 20 deg/f²,
  joint sanity) is much stricter and most clips still miss it on the items below.
- **Leg pops:** Run forward L/R shins 23-28 deg/f² (inherited by stop/start clips and turns), turn drive-offs 34-38,
  Arc Step start/end thigh accents 72-85 (limit 60), Run lean right R forearm 31.
- **Shin / tabard contacts:** shins into the body flap panels (Spawn 17, Run back 15, Run right 14 mm), tabard front
  into thighs (6-23 mm), Arc Step shins 10-12 mm, Arc Step left/right start hand brushing the knee (24-31 mm).
- **PROPOSAL, tabard flap detach:** move the body's loincloth flap panels into the TABARD objects so the tabard pass
  can move them; would clear most shin/flap contacts.
- **PROPOSAL, Unity foot IK for blend slide:** 8-way 50/50 blends still slide 10-34 mm (run) / 6-17 mm (walk) per
  contact. Turn on humanoid Foot IK plus a small grounding script that locks planted feet on the contact markers.
- **PROPOSAL, Glide exit speed curve:** Glide exit -> Run stop hits 70.6 m/s² root acceleration (limit 80).
- **Not done:** standing full-body Conduit Spear variant (needs a stand layer that seams into Idle).
- Other proposals: replace symmetry check 7 with a bone-to-own-skin fit test (the mesh is 20-35 mm asymmetric); a
  symmetric leg re-sculpt; the item 10 blend table, mask and judging rules are PROPOSAL until approved.
- **Open questions:** Arc Step i-frames (default none)? Heel jets on jump/land? Should Open Circuit strike during
  glide/Arc Step?
- **Run 6:** read `art/anim/wip/hands2/HANDS2-RESULT.md` once written for its QA result and any new regressions (mid-run
  it was chasing fingertip-to-thigh contacts, finger-finger penetration and Glide enter clearance).

## 4. Next phases (MASTER-PLAN)

- **E, sockets and export:** add sockets as bones (`L/R palm`, `orb`, `L/R heel jet`, `back`, `spear`, halo arc tips,
  `ground`) and **keep the non-deforming socket bones in the FBX** (the old "deform bones only" rule would drop them).
  Check whether `hs_glow` / `hs_jet` / `hs_move` / `hs_spear` curves survive import (fallback: Unity clip curves).
  Test FBX first (Arc Bolt R + Glide enter), then the full set plus an export README.
- **G, Unity/RoR2 integration:** Unity 2021.3.33 project, dependencies in the "Hollow Saint Dev" r2modman profile only
  (`tools/dev-profile/New-HollowSaintDevProfile.ps1`), tiny bundle-load test, Animator (8-dir blend space, glide,
  upper-body mask = `spine` and descendants, additive aim, halo layer), ChildLocator, VFX prefabs from Phase F,
  EntityStates on marker fractions, SkillDefs, meter UI. Game 1.4.1, engine 2021.3.33f1.
- **H, in-game testing:** controller stick circles/reversals/skill spam, attack speed 1-3x, crowds, boss, item procs,
  death/stage reset, host + client.

## 5. How to work

Rules (MASTER-PLAN section g and [tools/blender/anim/README.md](tools/blender/anim/README.md)):
- **Background Blender only** (`blender.exe --background --factory-startup ...`, Blender 5.2 at
  `C:/Program Files/Blender Foundation/Blender 5.2/`). Never use blender-mcp or touch a Blender you didn't start
  (Stuart's own is PID 4500). Run 6 caps itself at 2 Blenders at once.
- **One agent per clip module** (`tools/blender/anim/clips/<module>.py`, output only under `art/anim/wip/<module>/`).
  Shared files (`hs_anim.py`, `preview.py`, etc.) and `.blend` saves belong to the coordinator.
- **Numbered saves, never overwrite** (`build_anim.py` refuses). Never delete; superseded files go to `_old/` or `stale/`.
- Re-read `art/anim/REFINEMENT-PLAN.md` and `art/anim/wip/NEXT-RUN-NOTES.md` before each item; append only, back up
  plan docs as `.bakN` first. Label unapproved numbers PROPOSAL. Times in PT.
- Agent runs: Cursor CLI, headless, detached, working dir = project, log next to the prompt in `art/anim/wip/`
  (e.g. `refine-prompt-run6-hands2.txt` + `agent-run6-hands2-*.log`). Don't kill or relaunch a running agent.

QA:
- Per module: `blender --background --factory-startup --python-exit-code 1 --python tools/blender/anim/preview.py -- <module>`
  writes `art/anim/wip/<module>/qa.json` (PASS = bake, IK, hand QA, hand contact, orientation, pads, full QA incl. hand_nat).
- Whole checkpoint: `python tools/blender/anim/refresh.py vN --jobs 1` reruns all previews, seams, GIFs, review.html and
  writes `art/anim/vN/qa-summary.json`. Don't edit clip modules while it runs.
- Audits on a saved file: `fullqa_audit.py -- <blend> <out.json>`, `orient_audit.py -- <blend> <out.json>`; seams:
  `seams.py -- <modules>`; blend slide: `blend_qa.py`; transitions: `stitch.py -- <spec.json>`.

Previews: `art/anim/review.html` (clip cards + stitches), per-clip frames/GIFs in `art/anim/wip/<module>/<clip>/`,
hands in `wip/hands/` and `wip/hands2/`, pads in `wip/shoulders/`, locomotion stitches in `wip/locomotion8/`,
transitions in `wip/transitions/` (matrix in [TRANSITIONS.md](art/anim/wip/transitions/TRANSITIONS.md)), audits in
`wip/audit*/` ([FULL-AUDIT.md](art/anim/wip/audit-full/FULL-AUDIT.md)).

## 6. Git

- Remote: [johnstonstu/ror2-hollow-saint](https://github.com/johnstonstu/ror2-hollow-saint), branch `main`.
- Git LFS covers `.blend`, `.fbx`, `.glb`, `.psd`, `.exr`, `.png`, `.jpg`, `.gif`, `.wav`, `.zip` and timestamped
  backups (see `.gitattributes`). The repo is ~7 GB with ~22k LFS objects.
- Remote `main` is at **`0df5c2e` (anim v23)**. **v24, v25 and v26 plus thousands of re-rendered frames are NOT
  pushed.** Wait for run 6 to finish before `git add`, or files mid-write will fail to index.
- `.env` is gitignored and must never be read, printed or staged. `*.log`, `*.err`, `*.blend1` and PID-suffix temp
  copies are ignored too. History: [art/GIT-SETUP-RESULT.md](art/GIT-SETUP-RESULT.md).
- `.vscode/settings.json` sets `git.enabled: false` (plus autorefresh/autofetch off) because Cursor's git panel spawned
  ~1500 hung `git-lfs` processes on this repo. Use command-line git; re-enable at your own risk.
- No releases or publishing without Stuart naming them.

## 7. Scratch and leftovers (safe to clean up later; don't delete now)

- `art/anim/--help/`: empty folder left by run 6 mis-invoking `refresh.py --help`.
- Repo root: `@/` and several garbled-name folders (empty Blender `.thumbnails` caches).
- `art/anim/wip/`: `_run3/`, `_run4/`, `_run5/` (probe scripts), `_stage-v21/`, loose `_*.py` / `_*.ps1` probes,
  ~20 `seams-*.json`, old prompts and `run-*.cmd` launchers, `motion-range.json`.
- `art/anim/wip/hands2/`: `_lab1`-`_lab5`, `_probe-*`, `_work` and `probe_*.py` from run 6.
- `art/anim/wip/audit-full/audit-full-v16-copy.blend` and `v18/` copies; `art/vfx/assets/_old/`; `*.bakN` plan backups;
  `art/MASTER-PLAN-DRAFT.md` (kept on purpose as the pre-approval copy).
- Superseded checkpoints `art/anim/hollow-saint-anim-v1..v24.blend` (v20 is known bad) and hybrid v1-v17.

## History (short)

- **Concepts to model:** LEFT A chosen from `art/concepts/hollow-saint-variations-v1.png`. Blockout v1 rejected;
  local refinement v2-v6; a Higgsfield SAM3D GLB became the body base for hybrid v7 (hands/halo/back rebuilt locally).
- **Hybrid v8-v14:** first rig and six HS_v10 studies, fidelity pass (copper halo, emissive conductors, tabard),
  pauldrons, palette to concept. No paid Higgsfield jobs beyond the supplied GLB.
- **Hybrid v15-v18:** v16 is the animation-ready rig (leg chain refit, IK, pauldron/tabard/halo bones, sockets);
  v17 raised head, chest housing, faceted pauldrons; **v18** longer neck and larger chest plates = animation start file.
- **Anim v1-v18 (Sep 26-27):** 33-clip set (locomotion, glide, air, Arc Bolt + aims, Arc Step, Charge/Discharge/Open
  Circuit, presentation), then refinement items 1-9e (shoulder helpers, secondary motion, tabard follow, weight,
  Iron Man glide, natural hands, aim/IK, heel jets). Details in STATUS.md and REFINEMENT-PLAN.md.
- Old full handoff: [art/anim/wip/HANDOFF-before-20260927.md](art/anim/wip/HANDOFF-before-20260927.md) (environment
  notes, Higgsfield/credential notes, r2modman profile details, v1-v7 deliverables).
- Other references: `docs/animation-plan.md`, `docs/animation-handoff.md`, `docs/ability-kit-workshop.md`,
  `docs/design-brief.md`, `docs/technical-notes.md`, `docs/implementation-plan.md`.
