# Item 12: natural hand curl pass (run 6, Sep 27 2026)

Checkpoints: `art/anim/hollow-saint-anim-v26.blend`, then **`hollow-saint-anim-v27.blend` (final)**. v27 is v26 plus
the Conduit Spear grip fix. v25 is untouched.

## What changed

The v18 finger bones are modelled as a claw:
- palm cocked 11-14 deg back off the forearm;
- index/middle tips pre-hooked 21-25 deg;
- little tip bent back 12 deg;
- thumb tip hooked 56 deg.

Clips author curl as the same local rotation on every joint, so every clip inherited the claw. The fix is a pose
library that runs inside the bake, so **all 65 clips changed**. Each clip keeps its authored curl level, per-finger
offsets, tremors and flicks; only the finger shape, thumb and low-arm wrist change.

| Clip group | Clips | Hand result |
|---|---|---|
| Idle, Idle combat, Select idle/intro, Spawn | 5 | relaxed cascade, palms to thighs |
| Locomotion: run/walk 8 dirs, turns, stop/start, strafes | 25 | relaxed / fist-light cascade |
| Jump, Ascend, Descend, Land, Glide enter/loop/exit | 7 | relaxed |
| Arc Step start/loop/end x 4 dirs | 12 | relaxed / light |
| Arc Bolt L/R, aims | 7 | index point, others curled |
| Charge loop/full, Meter full flourish, Discharge (+snap), Open Circuit (+hold/end) | 8 | casting curl, open splay on release |
| Conduit Spear | 1 | grip on the draw, open palm release |

### Library (`tools/blender/anim/handpose.py`)
- Poses by curl level: straight -12, open 0, relaxed 21, casting 36, fist-light 46, grip 64, fist 92.
- In the relaxed pose, base/middle/tip bends run from index 15/18/11 to little 32/32/19. Curl grows from index to
  little at every level, and no tip out-curls its middle joint.
- Curled fingers converge toward parallel bend planes (`CONVERGE`), so tips don't fold across neighbours. The thumb
  sits alongside the index, slightly opposed (`THUMB`), and wraps as the hand closes.
- Arms below the 9f gate flex the wrist 6 deg toward the palm. The 9f forearm/upper-arm roll is unchanged, so
  extended arms still have thumbs up and palms forward.
- `settle`: a spring gives 2-4 frames of lag per finger joint, plus a relax on the follow-through after big hand
  moves. Seam frames and finger accents stay exact.
- `leg_clear` (the last bake pass): the minimal wrist extension that keeps the fingers 15 mm off the thighs
  (at most 26 deg), held and blurred over 3 frames. Seams keep their per-frame value.
- Blending: levels between knots are monotone cubic, so clip-to-clip cross-fades (item 10) blend between natural
  poses. Seams are exact: 72 seams, worst 0.001 mm.
- `handpose.ENABLED = False` restores the v25 hands.

### Clip edit
`clips/spear.py`: the right hand closes on the draw and opens across the elbow lead into the open-palm release.
- Curl goes from 26 to 40 on the draw, 28 on the lead and 6 on the release.
- `LEAD` joins `finger_accents`, which is the elbow-snap frame.

## QA (full preview QA, v27 build)

| | v25 | v27 |
|---|---|---|
| Status PASS | 13/65 | **13/65 (same 13)** |
| 9f orientation | 65/65 | **65/65** |
| Natural-hand check clean | 14/65 | **63/65** |
| Min joint bend | -177 deg (backward folds) | **+0.5 deg** |
| Max sideways tip bend | 82.9 deg | **0 deg** |
| Curl order index to little | 7 clips out of order | **all in order (≤ 4.6 deg)** |
| Worst finger pop | — | 9.3 deg/f² (limit 12) |
| Finger-finger / finger-body penetration | — | 1.5 mm max (limit 2) except below |
| Seams | 0.001 mm | 0.001 mm |

The v25 column comes from `fullqa_audit.py` on a copy of v25 (`_work/audit-v25.json`); the `—` cells weren't
summarised from it. The check's gates (`handnat.py`):
- joints bend inward within their natural range (backward limit 1 deg);
- sideways ≤ 12 deg;
- curl order within 10 deg;
- penetration ≤ 2 mm;
- pops 12 deg/f² (24 on accents).

Every clip was diffed against the v25 QA: there are no new failures of any kind, apart from the one below.

**Remaining failure (pre-existing): Arc Step left/right start, f4 (launch frame).** In v25 the whole hand and forearm
already sat 24-46 mm inside the thigh/shin on that frame, and the thigh pops 84 deg/f² there. With the fingers curled
naturally, the finger parts read deeper: L 23.6 to 40.1 mm, R 13.5 to 28.5 mm. A 40 deg wrist cap only reached
35/27 mm, because the palm itself is inside the leg. The fix is to re-author the arm on that frame in
`clips/arcstep_dirs.py`.

Other notes:
- The settle hits its 8 deg clamp briefly in some clips (see `settle` in `qa.json`); that's accepted.
- The item 10 stitched-transition GIFs and the transition matrix were not re-rendered. The clips they stitch now
  carry the new hands, and the seams are exact.

## Previews (`art/anim/wip/hands2/`)
- `sheets-v27/<clip>/`: v25 vs v27 for REST and 14 key clips (Idle, Idle combat, Run forward, Walk forward, Glide
  loop, Jump, Arc Bolt right, Arc Step start, Charge full, Discharge, Conduit Spear, Open Circuit, Spawn,
  Select intro). Each folder has:
  - `compare.gif` (both hands, before above after);
  - `hands.gif` (v27 close-up, both hands, profile/back/palm);
  - `before-after.jpg`;
  - `body.gif` (v27 hero full body).
- `sheets/<clip>/`: the same for v25 vs v26.
- `v25/`, `v26/`, `v27/<clip>/`: raw close-up frames. The colours are thumb red, index green, middle white, ring
  grey, little purple and palm blue. Each folder also has `pair-*` front views of both hands against the thighs.
- `body/<clip>.gif`: one full-body hero GIF per clip (65), copied from `art/anim/wip/<module>/<clip>/hero.gif`,
  which also has side/front/chase GIFs. Idle and Run forward cover the idle/run pose from Stuart's screenshot.
- `art/anim/review.html`: the key clips' cards show `compare.gif`.
- QA summaries: `art/anim/v26/qa-summary.json` and `art/anim/v27/qa-summary.json`.

## Tooling added or changed
- New: `handpose.py`, `handnat.py`, `hand2_closeup.py`, `hand2_sheet.py`.
- Changed: `handpass.py`, `hs_anim.py` (bake order), `fullqa.py`, `fullqa_audit.py`, `preview.py`, `refresh.py`,
  `handqa.py`, `package_review.py`, `clips/presentation.py` (Select idle `curl_exempt` on the flick),
  `clips/spear.py`.
- README "Added in item 12" documents them.
- Scratch scripts are in this folder: `qa_compare.py`, `qa_lanes.ps1`, `preview_set.py`, `probe_*.py`, `lab.py`,
  `montage.py`.

## Housekeeping (not deleted, per the rules)
- `C:\art\anim\wip\hands2\_lab1\`: a stray output folder outside the repo, from an early lab run with a bad
  relative path.
- `art/anim/--help/` (empty) and `art/anim/wip/logs/--help-preview-*.log`: from a mistaken `refresh.py --help`
  call. It was stopped within a minute, and its preview outputs were overwritten by the real v26 refresh.
- `_work/`: scratch builds (`s*-1.blend`, `it1-1.blend`) and the v25/v26/v27 copies used for rendering.
