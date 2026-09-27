# Hollow Saint clip toolkit

Procedural clip authoring on `art/hybrid/hollow-saint-hybrid-v18.blend`. Every clip is a Python
module in `clips/` that poses the rig per frame (leg IK on), then bakes to FK on the regular bones.
Background Blender only; the interactive Blender and other projects' processes are off limits.

```
B="C:/Program Files/Blender Foundation/Blender 5.2/blender.exe"
"$B" --background --factory-startup --python-exit-code 1 --python tools/blender/anim/preview.py -- run --step 2
python tools/blender/anim/sheet.py art/anim/wip/run/run-forward --cols 4
```

- `hs_anim.py`: `open_start`, `Poser` (`foot`, `arm`, `rot`, `offset`, `pelvis`, `curl`), `bake`,
  `halo_lag`, render helpers. Rotation conventions are in its docstring. Knee pole angles are
  re-solved on load (the v16 values were fitted on a straight leg and flared the knees).
  Those poles aim the knee forward, but the rest knee points slightly outward, so IK at the rest
  foot position misses the rest knee by ~7 cm. Pass `foot(..., rest_match=1)` on frames that must
  equal rest (idle start, blend-to-rest) and fade it toward 0 as the leg bends; residual < 0.1 mm.
- `gait.foot_cycle`: the swing lands already moving back at `contact_match` (default 0.5) of the
  stance slide speed, so contacts don't stop dead and snap. `lift_ease` > 1 softens touch-down
  but keeps a moving foot low (skates in slower gaits); only the forward run uses 1.5.
- `popscan.py -- <modules>`: worst per-bone rotation/position pops per clip (1-frame snaps).
- `bake` captures with the pauldron 40% follow on, keys the result and mutes the constraint, so
  baked keys equal what Unity plays (Unity drops constraints). The bake check covers rotations.
- `seams.py -- <modules>` writes `art/anim/wip/seams-<modules>.json` (no shared file to race on).
- `gait.py`: planted-foot trajectories; stance slides at exactly `speed` so feet don't skate.
- `preview.py -- <module>`: builds the module on a fresh v18 (never saved), writes QA to
  `art/anim/wip/<module>/qa.json` and frames to `art/anim/wip/<module>/<clip-slug>/`.
  `--frames 1,5` renders single frames, `--views hero,side,front,back,chase` (chase = RoR2-like
  camera behind and above), `--ortho 3.4` widens the orthographic cameras.
  `--frames` renders go to `<clip-slug>/inspect/` so the full frame set survives.
- `probe.py -- <module> "L hand,R toe" 1,5,9`: world positions for debugging.
- `seams.py -- run glide ...`: measures loop wraps and declared hand-offs (meta `seam_from` /
  `seam_to` = `[title, frame]`: this clip's first/last frame must equal that clip's frame).
- `halo_lag` keeps half the spring lag (`gain=0.5`) with a soft 3.5 cm limit; use `halo_offsets`
  when a clip must reproduce another clip's halo exactly at a seam.
- `sheet.py <clip folder>`: contact sheets and GIFs (system Python, Pillow).
- `build_anim.py -- run walk ...`: assembles modules into a new versioned file under `art/anim/`
  (refuses to overwrite).
- `rigfix.py`: shoulder corrective applied by `open_start` on every load (v18 stays untouched):
  `L/R shoulder` deform helpers following the upper arm at 50%, and a smooth chest -> helper ->
  upper-arm weight blend. `bake` keys and mutes the helper constraint like the pauldron.
- `armfit.py` (applied by `open_start` before `rigfix`): refits `R upperarm` / `R forearm` / `R hand` to the R
  arm mesh seams (FULL-AUDIT M6). The body mesh is centred on `MID_X` = -0.0375 but v18's arm bones were
  mirrored about x = 0. Bone-parented meshes keep their world placement; `R hand IK` / `R elbow pole` follow.
- `handfix.py` (applied by `open_start` after `rigfix`): 9f hand chirality fix. v18's hands were modelled
  mirror-handed (palm forward-medial, thumb on the back edge). Each hand's bones and meshes (except the wrist
  cuff and conductor) are reflected across the plane through the knuckles normal to the index-little axis.
  Reflected bones keep +X curl, but local-Z (splay/fan) reverses, so code that splays by sign multiplies
  by `handfix.ZSIGN`.
- `padfix.py` (applied by `open_start` after `armfit`): 9g placement fix. `R scapula` / `R pauldron` become the
  mirror of the L bones about `MID_X`; the whole halo assembly (halo bones, yoke bar/light, sheared strut) moves
  `HALO_SHIFT` back and up so the lower arcs and yoke clear the pads at rest; the yoke bar's ends are trimmed
  `YOKE_TRIM` so they stop at the lower-arc docks; the pauldron Copy Rotation is muted for good. `halo_pose(p, off,
  m3)` rotates/offsets the halo root pivoting on the lower-arc dock, so tilts swing the top of the ring instead of
  driving the lower arcs into the pads. Clips that hard-code the ring centre add `HALO_SHIFT`.
- `padpass.py` (run by `bake` on every frame after the hand pass): drives the pauldrons. Each pad rotates by a
  share of the upper arm's swing relative to the chest (`SHARE_X/Y/Z`) about a hinge on its collar-side edge,
  lifts up/out as the arm rises, yields some of that follow if it would meet the halo's lower arc, then lifts off
  any collar/neck/upper-arm skin still inside it. `finish` smooths the follow and push over time (envelope + blur,
  loop-aware, exact first/last frames for seams); `halo_clear` nudges the whole halo up/back on the few frames
  where a lower arc would still come within 4 mm of a pad. `bake` reports `pad_follow_max`,
  `halo_clear_push_max_mm`, `pad_pop_mm_f2` and `halo_pop_mm_f2`.
- `contact.PadContact` (`pad_contact`, `pad_contact_max_mm`, `pad_contact_ok`, part of PASS): pads vs torso,
  neck/collar and upper arm (beyond rest) and vs the halo lower arcs and yoke bar (absolute), limit 5 mm.
- `pad_closeup.py -- (--blend b | --module m) --tag T --clips "Title:1,5;Title:all/2"`: colour-coded Workbench
  shoulder close-ups (pads orange, halo cyan, yoke yellow, scapula shells green) from front/back/top/outL/outR,
  framed on the chest, written to `art/anim/wip/shoulders/<tag>/`. `pad_sheet.py <folder> [--compare other]
  [--out dir]` builds `pads.gif` / `pads-sheet.jpg` (before on top).
- `handorient.py`: the 9f orientation check. `OrientQA` in `preview.py` (`hand_orient`, `hand_orient_ok`, part of
  PASS): on frames where the upper arm is raised > 45 deg (or the elbow is open > 140 deg with > 30 deg raise),
  relative to the chest and not swept back, thumb-up (world Z) >= 0 and palm-forward >= -0.25. It reports
  failing frame ranges and the worst values per hand. `handpass` runs the matching orient step (forearm roll,
  up to 40% upper-arm twist on straight arms, weighted by the same gate) after the finger/wrist limits.
- `orient_closeup.py -- (--blend b | --module m) --tag T --clips "Title:1,5;Title:all/2"`: colour-coded hand
  close-ups (thumb red, index green, little purple, palm blue) from front/back/palm/thumb views, written to
  `art/anim/wip/hands/orientation/<tag>/`. `orient_sheet.py <folder> [--compare other]` builds the GIF/sheet.
  `orient_audit.py -- <blend> <out.json>` runs `OrientQA` over every action in a checkpoint.
- `action_diff.py -- <ref.blend> <modules...>`: rebuilds modules and diffs every bone/prop against the reference.
- `shoulder_diag.py`: shoulder-region stretch/compression metrics and upper-body renders.
- `refresh.py vN` (system Python): all previews, seams, sheets/GIFs, review.html and
  `art/anim/vN/qa-summary.json` in one go (background Blender, `--jobs 3`). Don't edit clip
  modules while it runs; the seams job imports all of them last.
- `preview.py` never deletes frames; frames not re-rendered move to `<clip-slug>/stale/`.
- `handpass.py`: natural-hand pass that `bake` runs on every frame after the pose function
  (`bake(..., hands=True)`). It covers the thumb curl floor and tuck, finger joint limits (tip joints
  measured on their true bend), fanning apart of neighbouring fingers, and wrist/forearm roll and bend
  limits. It depends only on the current frame, so seams stay exact. It moves the index fingertip, so
  anything aiming the `muzzle` must apply it too (see `primary.solve_arm`).
- `handqa.py`: per-frame hand QA in `preview.py` (`hand_qa`, `hand_qa_summary`, part of PASS): joint
  limits, mesh interpenetration beyond rest (2 mm), finger pops (12 deg/f²; 24 on frames a clip lists in
  meta `finger_accents`, ±1 frame).
- `clearance.py`: forearm/hand clearance from the torso and tabard per frame (`arm_clearance`); locomotion
  clips need at least 10 mm. `arm_tune.py -- <abs variants.json>` scores arm variants without baking.
- `hand_closeup.py -- <module> <slug,...> [--step N]`: back-of-hand and palm views of both hands, framed in
  the hand's own space (a thin clip slab keeps the body out), written to `art/anim/wip/hands/<slug>/`.
  `--set handpass.TUCK=0 --out tag` renders an override for comparison. Then run `hand_sheet.py <folder>`
  (system Python) for `hands.gif` / `hands-sheet.jpg`; `package_review.py` adds `hands.gif` to the
  clip's card.
- `vfx.py` (applied by `open_start`): heel thrusters and glow, driven by root-bone properties that `bake`
  keys in every clip (clips pass `props={name: fn(frame)}`; missing ones key 0). `hs_glow` 0..1 emissive
  boost; `hs_jet` scales both heel jets (0 hides them); `hs_spark_L` / `hs_spark_R` add a per-foot flash (run
  push-off sparks); `hs_jet_dir` tilts the exhaust below horizontal in degrees. The jets ride the heel
  spur (`heel socket` tail) but keep the character's axes, so the exhaust always trails back along the
  travel line whatever the ankle does. Unity drops the objects; the keyed properties are there for its VFX.
- `gait.heel_pivot(ball, pitch)`: for a toes-up foot (pitch < 0), moves the ball so the foot pivots on the heel
  spur (walk heel strike) instead of driving the heel into the ground.
- `stitch.py -- <spec.json> [--views hero,side,chase] [--no-render]`: plays clips back to back (hard cut, or a
  Unity-style cross-fade with `blend`/`sync`, or an upper-body `mask` layer over locomotion) and renders
  `art/anim/wip/transitions/<name>/`. `stitch.json` has per-handoff world-space acceleration of key bones
  against the worst inside the clips, plus per-frame series. Specs live in `art/anim/wip/transitions/specs/`.
  Then run `sheet.py` on the folder for the GIFs.
- `hand_closeup.py ... --context` adds an unclipped front-outside view per hand (hand vs thigh/torso/tabard).
- `contact.py`: hand/forearm-into-body contact QA in `preview.py` (`hand_contact`, `hand_contact_max_mm`,
  `hand_contact_worst`, `hand_contact_ok`). Hand and forearm probe points are tested against the body
  (thighs, shins, feet, torso; the arm's own faces excluded) by majority ray parity, and against the tabard
  shell. The rest-pose depth is subtracted and anything deeper than 4 mm fails. Contacts a clip means to have
  (a hand resting on a knee) go in meta `hand_contacts` as `"L hand > L thigh"` keys.
- Status also requires `arm_clear_ok` (locomotion), `hand_qa_summary.ok` and `hand_contact_ok`.

## Ownership (parallel agents)

One owner per clip module. An owner edits only `clips/<module>.py` and writes only under
`art/anim/wip/<module>/`. Shared files (`hs_anim.py`, `gait.py`, `preview.py`, `sheet.py`,
`probe.py`, `build_anim.py`, other modules) are coordinator-owned: put helpers inside your own
module or ask for the change. Only the coordinator saves `.blend` files.

## Clip conventions

- Action names `HS_anim | <Title>`, 24 fps, in place (the game moves the body; `root` stays at
  the origin). Loops repeat frame 1 as the last frame and set `loop=True`.
- `meta` records `speed_mps` for locomotion so Unity can scale playback, and `direction`
  (forward/backward/left/right) or `travel: [x, y]` so the planted-slide QA checks the right axis.
- Status must be PASS in `qa.json` (bake error < 1 mm, IK miss < 1 cm, body above ground).
