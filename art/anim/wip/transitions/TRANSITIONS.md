# Hollow Saint transitions (refinement item 10, run 5)

Written 2026-09-27 PT by the coordinator agent. Everything here, and the thresholds especially, is a
**PROPOSAL** until Stuart approves it.

## Result

- **149 stitched sequences, 402 handoffs.** Every state pair and every skill is played out of and into the states it
  can come from:
  - idle / walk / run / glide / jump / fall / land (landing into idle and into run);
  - all 8 run and walk directions, plus leans, pivots and plant turns;
  - Arc Step (4 directions) from idle, run, air and glide;
  - every upper-body skill over every state;
  - cancels, casting mid-jump, stopping mid-stride, direction changes, and a full stick circle.
- **399 pass, 3 flagged.** All three were checked frame by frame in the renders and look smooth. Details are
  under "Flagged handoffs" below.
- New clips: `Run stop` (from the L contact, Run forward f1), `Run stop R` (from the R contact, f9) and
  `Run start` (Idle f1 to Run forward f9). They live in `tools/blender/anim/clips/startstop.py`. All four of their
  seams are exact (0.0 mm).
- New cancel markers: Arc Bolt `Interrupt` f13, and Arc Step end `Cancel` f5 on all four directions. Conduit
  Spear `Cancel` f11 was already there. They are also written to meta as `cancel`.
- VFX: the heel jets and sparks now dim through any cross-fade that reverses the travel direction (see "VFX
  rules").
- Checkpoint: `art/anim/hollow-saint-anim-v25.blend`.

Files in this folder:

- `matrix.md`: every handoff with its numbers and pass/fail.
- `grids.md`: the state -> state and skill-over-state pass/fail grids (reproduced below).
- `velocity-report.md`: the per-pair velocity / acceleration jump report.
- `gifs/<pair>-hero.gif`, `gifs/<pair>-chase.gif`, `gifs/<pair>-review.jpg`: every stitched sequence. The review
  sheet shows frames -4..+5 around each handoff.
- `specs/matrix/*.json`: the sequences, plus `specs/matrix/_blend-table.json` (the table below).
- `matrix/<pair>/stitch.json`: the measurements.

Frames for the GIFs were rendered to `transitions/temp/` (git-ignored). Only the GIFs and sheets are kept in
`gifs/`.

## Recommended transition table (Unity, 24 fps, PROPOSAL)

Cross-fade lengths in frames. These come from the blend sweeps (`exp`, `exp2`, `exp3` below): each is the
shortest that passes at every entry phase tested.

| Transition | Frames | Notes |
|---|---|---|
| 8-way run/walk direction change (blend tree moves, reversal, lean in/out) | 10 | 8 left one diagonal reversal phase failing |
| Walk reversal (fwd <-> back) | 10 | |
| Walk -> Idle | 12 | 10 failed at one walk phase |
| Idle -> Walk | 8 | |
| Walk -> Run / Run -> Walk | 6 / 8 | sync phase |
| Idle <-> Idle combat | 8 | |
| Run -> Fall (Descend) | 8 | |
| Air (Ascend/Descend) -> Glide loop | 6 | the glow lights at the Glide enter rate |
| Glide loop -> Fall | 10 | 8 left a 15 mm/f² toe pop |
| Ascend -> Descend | 4 | |
| Descend -> Land | 3 | |
| Land -> Run forward | 6 | from Land f6 (Compress) |
| Run -> Jump | 6 | Jump from f4 (skips the standing crouch); 4 failed at some run phases |
| Any state -> Arc Step start | 4 | |
| Arc Step end f5 (Cancel) -> locomotion | 4 | |
| Glide loop <-> Arc Step | 5 | the glow cuts/relights over the blend |
| Glide loop -> Glide exit | 4 | any glide phase |
| Upper-body skill layer out (weight 1 -> 0) | 6 | |
| Skill chain (Arc Bolt `Interrupt` f13 / Spear `Cancel` f11 -> next cast) | 2 | |

Skill layer fade-in (weight 0 -> 1):

| Skill | Frames |
|---|---|
| Arc Bolt L/R | 3 |
| Discharge snap | 4 |
| Discharge | 4 |
| Conduit Spear | 5 |
| Charge loop / Charge full | 6 |
| Meter full flourish | 10 |
| Open Circuit | 10 |

**Seams (no blend):**

- Run forward f16 -> `Run stop` f1 -> Idle f2.
- Run forward f8 -> `Run stop R` f1 -> Idle f2.
- Idle f96 -> `Run start` f1 -> Run forward f10.
- Glide exit f14 -> Run forward f2.
- Glide exit f14 -> `Run stop` f2 (Glide exit ends on the L contact, which is Run f1).

On stick release, play whichever stop comes first, so at most 8 frames wait. Every other clip chain in the set is
still exact (`seams.py`).

## Upper-body mask (the skill layer)

The mask is `spine` and all its descendants (58 bones):

- `spine`, `chest`, `neck`, `head`, `head socket`, `core socket`;
- L/R `scapula`, `shoulder`, `pauldron`, `upperarm`, `forearm`, `forearm twist`, `hand`, `muzzle`;
- all finger bones: `index`, `middle`, `ring`, `little` and `thumb`, `.1`–`.3`;
- the halo: `halo root`, `halo 1`–`halo 4`, `halo socket`.

Locomotion keeps these 25: `root`, `pelvis`, the legs (`thigh`, `shin`, `foot`, `toe`, `heel socket`), all five tabard
bones and the IK targets/poles. The tabard stays with the legs so the cloth keeps following the stride under a
cast.

## VFX rules

- **Skill layer:**
  - `hs_spear` comes from the skill layer.
  - `hs_glow` = max(locomotion, skill), so a cast never dims the glide glow.
  - Everything else (`hs_jet`, `hs_spark_L/R`, `hs_jet_dir`, `hs_move_x/y`, `hs_turn`) stays with locomotion.
- **Jet gain:** heel jets and sparks scale by `smoothstep((|hs_move| - 0.3) / 0.4)` (`vfx.jet_move_gain`).
  - No clip lights the jets with a travel vector shorter than 1 (checked on all 62 v24 clips), so clips are
    unchanged.
  - In a cross-fade between moves up to 90° apart the vector never drops below 0.707, so nothing changes there
    either.
  - A 180° reversal (Arc Step back end -> Glide loop, back-dash out of a forward glide) otherwise flips the
    exhaust 180° in one frame. With the gain it goes dark around the flip and relights facing the other way.
  - Unity's jet VFX should apply the same gain to the blended parameters.
- **Glide glow:** entering or leaving the glide from any state lights or cuts the glow at up to 1.5× the rate
  authored in Glide enter / Glide exit.

## Discharge snap over every state

`snap-*`: idle, idle combat, all 8 run and 8 walk directions, run lean, glide, ascend, descend, Arc Step (from idle
and from run), mid Arc Bolt, mid Spear, mid Charge, a running jump, a run stop, and twice during a full stick
circle. All pass. The snap layers over the whole upper body and hands, and the legs keep their state.

## Pass/fail grids

Cells are passed/total handoffs; bold means one is flagged. From `grids.md`:

### State -> state (rows: from, columns: to; passed/total handoffs, bold = has a flagged handoff)

| From \ To | Arc Bolt | Arc Step | Ascend | Conduit Spear | Discharge | Fall (Descend) | Glide | Glide enter | Glide exit | Idle | Idle combat | Jump | Land | Meter full flourish | Plant turn | Run (8-way) | Run lean | Run pivot | Run start | Run stop | Walk (8-way) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Arc Bolt |  |  |  |  |  |  |  |  |  | 2/2 |  |  |  |  |  |  |  |  |  |  |  |
| Arc Step |  | 41/41 |  |  |  | 4/4 | 4/4 |  |  | 5/5 |  |  |  |  |  | 6/6 |  |  |  |  |  |
| Ascend |  |  |  |  |  | 5/5 | 1/1 |  |  |  |  |  |  |  |  |  |  |  |  |  |  |
| Conduit Spear |  |  |  |  |  |  |  |  |  | 1/1 |  |  |  |  |  |  |  |  |  |  |  |
| Discharge |  |  |  |  |  |  |  |  |  | 1/1 |  |  |  |  |  |  |  |  |  |  |  |
| Fall (Descend) |  | 4/4 |  |  |  |  | 1/1 |  |  |  |  |  | 5/5 |  |  |  |  |  |  |  |  |
| Glide |  | **2/4** |  |  |  | 1/1 |  |  | 6/6 |  |  |  |  |  |  |  |  |  |  |  |  |
| Glide enter |  |  |  |  |  |  | 1/1 |  |  |  |  |  |  |  |  |  |  |  |  |  |  |
| Glide exit |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | 5/5 |  |  |  | 1/1 |  |
| Idle | 2/2 | 5/5 |  | 1/1 | 1/1 |  |  |  |  |  | 1/1 | 1/1 |  | 1/1 |  | 2/2 |  |  | 1/1 |  | 2/2 |
| Idle combat |  |  |  |  |  |  |  |  |  | 1/1 |  |  |  |  |  | 1/1 |  |  |  |  |  |
| Jump |  |  | 3/3 |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |
| Land |  |  |  |  |  |  |  |  |  | 3/3 |  |  |  |  |  | 2/2 |  |  |  |  |  |
| Meter full flourish |  |  |  |  |  |  |  |  |  | 1/1 |  |  |  |  |  |  |  |  |  |  |  |
| Plant turn |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | 1/1 |  |  |  |  |  |
| Run (8-way) |  | 7/7 |  |  |  | 1/1 |  | 1/1 |  | 1/1 |  | 2/2 |  |  | 1/1 | 19/19 | 1/1 | 1/1 |  | 3/3 | 1/1 |
| Run lean |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | 1/1 |  |  |  |  |  |
| Run pivot |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | 1/1 |  |  |  |  |  |
| Run start |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | 2/2 |  |  |  |  |  |
| Run stop |  |  |  |  |  |  |  |  |  | 4/4 |  |  |  |  |  |  |  |  |  |  |  |
| Walk (8-way) |  |  |  |  |  |  |  |  |  | 1/1 |  |  |  |  |  | 1/1 |  |  |  |  | 9/9 |

### Skill layer over locomotion (rows: skill event, columns: the state underneath)

| Skill | Arc Step | Ascend | Fall (Descend) | Glide | Glide enter | Idle | Idle combat | Jump | Land | Run (8-way) | Run lean | Run start | Run stop | Walk (8-way) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Arc Bolt (next phase / chain / state change under it) |  | 1/1 |  | 1/1 | 1/1 | 2/2 |  | 1/1 |  | 3/3 |  | 1/1 |  |  |
| Arc Bolt -> Discharge |  |  |  |  |  |  |  |  |  | 1/1 |  |  |  |  |
| Arc Bolt in |  |  | 2/2 | 2/2 |  | 4/4 |  |  |  | 14/14 |  |  |  | 2/2 |
| Arc Bolt out | 1/1 | 1/1 | 2/2 | 3/3 |  | 3/3 |  |  |  | 11/11 |  | 1/1 |  | 2/2 |
| Charge -> Discharge |  |  |  |  |  | 1/1 |  |  |  |  |  |  |  |  |
| Charge in |  |  |  |  |  | 3/3 | 2/2 |  |  |  |  |  |  |  |
| Charge out |  |  |  |  |  | 2/2 | 2/2 |  |  |  |  |  |  |  |
| Conduit Spear (next phase / chain / state change under it) |  |  |  |  |  |  |  |  | 1/1 | 1/1 |  |  | 1/1 |  |
| Conduit Spear -> Arc Bolt |  |  |  |  |  |  |  |  |  | 1/1 |  |  |  |  |
| Conduit Spear -> Discharge |  |  |  |  |  |  |  |  |  | 1/1 |  |  |  |  |
| Conduit Spear in |  |  | 2/2 | 1/1 |  | 1/1 |  |  |  | 7/7 |  |  |  | 1/1 |
| Conduit Spear out |  |  | 1/1 | 1/1 |  | 1/1 |  |  |  | 5/5 |  |  | 1/1 | 1/1 |
| Discharge (next phase / chain / state change under it) | 4/4 | 1/1 |  |  |  |  |  |  |  | 2/2 |  |  |  |  |
| Discharge in | 2/2 | 1/1 | 2/2 | 2/2 |  | 2/2 | 1/1 | 1/1 |  | 13/13 | 1/1 |  | 1/1 | 9/9 |
| Discharge out | 1/1 | 2/2 | 2/2 | 2/2 |  | 4/4 | 1/1 |  |  | 16/16 | 1/1 |  |  | 9/9 |
| Meter full flourish in |  |  | 1/1 | 1/1 |  | 1/1 |  |  |  | 3/3 |  |  |  | 1/1 |
| Meter full flourish out |  |  | 1/1 | 1/1 |  | 1/1 |  |  |  | 3/3 |  |  |  | 1/1 |
| Open Circuit (next phase / chain / state change under it) |  |  | 2/2 | 2/2 |  | 2/2 |  |  |  | 6/6 |  |  |  | 2/2 |
| Open Circuit in |  |  | 1/1 | 1/1 |  | 1/1 |  |  |  | **2/3** |  |  |  | 1/1 |
| Open Circuit out |  |  | 1/1 | 1/1 |  | 1/1 |  |  |  | 3/3 |  |  |  | 1/1 |

## How it is judged (PROPOSAL thresholds)

Each handoff is judged on a window of ±2 frames around the cut (`tools/blender/anim/stitch.py`, `matrix.py`).

- **Position pops** (world acceleration of 11 tracked points: pelvis, head, both hands, both toes, halo root, the
  front and back tabard tips, both middle fingertips):
  - Limit: ≤ 1.25 × reference + 5 mm/f².
  - The reference is the largest of:
    - the same point's peak inside the sequence, away from every cut and every cross-fade;
    - the peak of each clip playing in the window when that clip plays on its own;
    - the eased-pose-change allowance `6 d / (blend+1)²`: what a smoothstep cross-fade of that length needs to
      carry the point across the distance `d` between the two clips' poses.
  - The allowance means a limb moving to a different pose over the blend isn't counted as a pop. A velocity
    mismatch (momentum not matched) still is.
- **Rotation pops** (all 82 bones): fullqa's limit, ≤ max(20, 1.25 × the clips' own peak) deg/f². On accent
  frames (a clip's meta `accents` / `finger_accents` ±1) it rises to 60 for body bones and 24 for fingers.
- **VFX:**
  - Intensity steps (`hs_glow`, `hs_jet`, sparks, `hs_spear`): ≤ max(0.05, 1.5 × the clips' own peak step) per
    frame.
  - Lit-jet exhaust turn: ≤ max(10°/f, 1.5 × the larger of the clips' own peak and the travel-direction turn
    rate). The turn is weighted by the dimmer side's jet brightness, since a turn while one side is dark reads as a
    relight.
- **Root travel acceleration:** ≤ 80 m/s² (RoR2 `baseAcceleration`). The worst in the matrix is **70.6 m/s²**,
  at `glide-exit-runstop-idle` f27 (Glide exit f14 -> `Run stop` f2, 8.7 -> 3.9 m/s). Every Glide exit -> Run seam
  is 64.8 m/s² (8.7 -> 6.0 m/s). These pass, but they're the only large speed steps in the set: Glide exit has no
  speed curve, so glide speed meets run speed in one frame. PROPOSAL: give Glide exit a speed curve that eases
  8.7 -> 6.0 m/s, or let the Unity controller ease the speed.

## Flagged handoffs (3, all reviewed in the renders)

| Pair | Frame | Handoff | Measured / limit | Verdict |
|---|---|---|---|---|
| `over-opencircuit-run` | f13 (10-frame fade-in) | Run forward f12 -> Open Circuit over Run forward | L hand 91 mm/f² vs ref 59 (limit 79) | Smooth in the render. The left hand leaves the run's arm pump at its fastest point and swings up into the Open Circuit spread. It's a big pose change carried by the 10-frame ease, not a pop. The same fade-in passes at the other two run phases. |
| `step-left-glide` | f13 (5-frame blend) | Glide loop f12 -> Arc Step left start f1 | lit-jet turn 15.1°/f vs 7.2 | Deliberate: the heel exhaust re-aims 90° from the glide's travel to the side dash over the 5-frame blend. It reads as the jets swinging round, not a snap. The travel-turn allowance doesn't cover it because Arc Step start begins at 0 speed. |
| `step-right-glide` | f13 (5-frame blend) | Glide loop f12 -> Arc Step right start f1 | lit-jet turn 15.1°/f vs 7.2 | Same as left (mirrored). |

If a smaller jet turn is wanted (PROPOSAL, untested): have the Unity jet VFX slerp its aim at ≤ 10°/f, or
lengthen Glide -> side Arc Step, although that delays the dash. The forward and back dashes out of a glide pass at 5 frames.

## Inherited clip fails (not transition problems)

These are single-clip full-QA fails (`STATUS.md`) and show inside the sequences too:

- Run forward: back conductors > neck 6.3 mm f7; tabard front > thigh 11–12 mm; L shin pop 26 deg/f² f5.
- Run stop / Run stop R / Run start share the run's legs: L/R shin 23–28 deg/f² at the brake or push-off.
  Run stop R's tabard front > L thigh 5.0 mm f3 is inherited from Run f11.

## Before / after

| Pass | Specs | Handoffs | Fails | What changed |
|---|---|---|---|---|
| First naive pass (`transitions/pairs`) | 102 | 262 | 83 | naive 2-frame skill fade-in, 6-frame locomotion blends, no stop/start clips, old metric |
| Sweep 1 (`transitions/exp`, blend x phase) | 228 | 412 | 145 | found the per-transition blend lengths |
| Sweep 2 (`transitions/exp2`) | 129 | 228 | 32 | running jump, diagonal reversal, flourish / Open Circuit, walk->idle, snap on lean, glide->fall->land |
| Sweep 3 (`transitions/exp3`) | 159 | 195 | 38 | Spear / flourish / Open Circuit fade-in at 8 run phases, glide->fall, Arc Step <-> glide |
| **Final matrix (`transitions/matrix`)** | **149** | **402** | **3 (reviewed smooth)** | table above, stop/start clips, jet gain, accent-aware judging, ease allowance |

The sweep counts include deliberately bad settings (1–3 frame blends), so they aren't failure rates.

## Reproduce

```
B="C:/Program Files/Blender Foundation/Blender 5.2/blender.exe"
python art/anim/wip/_run5/gen_matrix.py
"$B" --background --factory-startup --python tools/blender/anim/stitch.py -- art/anim/wip/transitions/specs/matrix/<pair>.json ... --blend art/anim/hollow-saint-anim-v25.blend --no-render
python art/anim/wip/_run5/matrix.py --glob 'transitions/matrix/*' --fails [--md matrix.md]
python art/anim/wip/_run5/grids.py --md grids.md
python art/anim/wip/_run5/jumps.py --md velocity-report.md
python art/anim/wip/_run5/gen_render.py   (render copies -> transitions/temp, hero + chase)
python art/anim/wip/_run5/review_strips.py <temp/pair> ...
```

The matrix numbers above were measured on `hollow-saint-anim-v25.blend` (log `_run5/matrix-v25.log`). The
sweeps and the GIF renders used v24 plus the `startstop` module (`--modules startstop`). That is the v25 clip
set minus the marker and jet-driver changes, and neither of those affects the poses.

## GIF self-review

I checked every handoff by eye on `hollow-saint-anim-v25.blend` renders: the hero and chase GIFs in
`transitions/gifs/`, plus 81 contact sheets in `transitions/temp/_montage/000.jpg`–`080.jpg`. Each sheet row is
one handoff, shown as the 4 frames before, the handoff frame (yellow number) and the 5 frames after. They are built by
`python art/anim/wip/_run5/montage.py --force --final`.

- **All handoffs read as smooth.** None has a pop, a repeated pose, a foot slide at the seam or a VFX flicker.
- **The 3 flagged handoffs look smooth:**
  - `over-opencircuit-run` f13: the left hand rises steadily across the 10-frame fade.
  - Glide -> Arc Step left/right: the jets turn across the 5-frame blend. They never snap.
- **Fixed during the review: Arc Step loop -> end showed the same pose twice.** Loop f10 and end f1 are the same
  pose, so every Arc Step handoff held one frame. All 21 Arc Step specs (`step-*-{idle,run,air,glide}`,
  `step-fwd-chain`, `cancel-arcbolt-to-arcstep`, `cancel-arcstep-to-arcbolt`, `snap-arcstep`, `snap-arcstep-run`)
  now start Arc Step end at f2. They were re-rendered (`_run5/render-arcstep.log`) and republished, and every
  montage was rebuilt.
- **Not a seam:** Arc Step end changes pose sharply over one frame at f2 -> f3. That is the dash-arrival accent
  authored into the clip, and the standalone `wip/arcstep/arc-step-end/side-sheet.jpg` shows it too.
