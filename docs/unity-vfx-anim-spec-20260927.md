# Hollow Saint M3 — Unity animation import & VFX prefab specification

Written 2026-09-27 (PT). Author: read-only agent pass. Every frame number, marker name, socket alias,
file name and pipeline behavior below was **read from the repo**, not remembered. Sources: `art/anim/v31/catalog.json`
(65 clips, parsed programmatically), `HollowSaintUnityProject/Assets/HollowSaint/Editor/FoundationBundleBuilder.cs`,
`Editor/ProbeAssets.cs`, `GameFoundation01/Foundation.controller` (YAML inspected directly),
`HollowSaintMod/Character/Rig/FoundationMounts.cs`, `FoundationBody.cs`, `FoundationMaterials.cs`, `FoundationPresentation.cs`,
`KitRegistration.cs`, `art/vfx/assets/fbx/fbx_manifest.json`, `art/vfx/assets/README.md`, `art/vfx/VFX-ABILITY-PLAN.md`,
`docs/kit-contract-20260927.md`, `docs/next-agent-handoff-20260927.md`, `art/MASTER-PLAN.md`,
`tools/blender/export_unity_probe.py`. Unity/Blender were NOT launched; nothing under
`GameFoundation01/`, `Generated03/`, `Library/`, `Temp/`, `Logs/`, `UserSettings/` was written. No Unity asset was modified.

Everything marked **[PROPOSAL]** is a judgment call for Stuart; everything else is repo-verified fact.

---

# PART A — Animation import plan

## 0. Ground truth: how the animation pipeline actually works (verified from code)

The chain is: Blender → per-clip FBX → Unity editor scripts → game bundle. Know it before touching anything:

1. `tools/blender/export_unity_probe.py` (Blender, background) opens `art/anim/hollow-saint-anim-v31.blend`,
   exports **one FBX per clip** plus `HollowSaint.fbx` and `export-manifest.json` into
   `Assets/HollowSaint/Source/v31_probe03/`. It has a hardcoded `TITLES` list (currently the 15 bundled clips)
   and **refuses to overwrite an existing output folder** (`assert not OUT.exists()`).
2. `Editor/ProbeAssets.cs` (`ProbeAssets.Import()`) reads `export-manifest.json`, configures each clip FBX
   (Generic avatar, `animationCompression = Off`, `loopTime` from the manifest), injects the `hs_*` curves as
   editor float curves bound to `HollowSaintPreviewSignals`, injects each catalog marker as an
   **AnimationEvent** (`OnClipMarker(markerName)`, time `(frame-start)/24`), and writes clips to
   `Assets/HollowSaint/Generated03/Clips/<file-basename>.anim` — **underscores**, e.g. `Arc_Bolt_right.anim`.
   It asserts clip length == `(end-start)/24 s` — so Unity clip length = `end − start` seconds, i.e. loops
   repeat frame 1 as their last frame and the Unity duration is `(end−start)/24`, not `end/24`.
3. `Editor/FoundationBundleBuilder.cs` copies every clip from `Generated03/Clips` into
   `GameFoundationNN/Clips`, **strips every non-Transform curve binding and every AnimationEvent**
   (`binding.type != typeof(Transform)` removed; `SetAnimationEvents(clip, Array.Empty)`), adds one Animator
   state per clip named `source.name.Replace('_',' ')` on layer 0, sets `Idle` as default, declares
   6 bool + 10 float parameters, saves `mdlHollowSaint.prefab`, and builds
   `artifacts/foundation/bundle01/hollowsaintassets` from that prefab. It **throws if the output folder exists**
   (`GameFoundation01` is frozen; a rebuild needs `GameFoundation02`). It reads clips from the
   `ProbeAssets.Generated` const (`Generated03`) — that const must be bumped for a new probe set.

**Consequence that changes the whole design:** the game-bundle clips are transform-only. The catalog
markers (`OnClipMarker` events) and the `hs_glow/hs_jet/hs_spark_L/hs_spark_R/hs_jet_dir/hs_move_x/hs_move_y/
hs_turn/hs_spear` signal curves exist **only in the Generated03 preview clips** and are **stripped from the game
bundle**. Therefore:
- All in-game effect timing must come from **EntityState code firing at normalized-time fractions** (which is
  exactly what `kit-contract-20260927.md` asks agents to declare). Do not plan on AnimationEvents.
- `HollowSaintVFXController` cannot read the `hs_*` curves in game; jets/glow must be computed at runtime from
  replicated state (sprint, glide, motor velocity) — see Part B §12. [PROPOSAL: a GameFoundation02 builder
  variant could keep the signal curves and ship a runtime component on the model; deferred — the strip rule is
  what currently ships and is proven, keep it for M3.]

## 1. Which of the 65 clips to import (M3 kit), and priority

Import **all 50 remaining clips** into the new probe set (`Source/v31_probe04/` — the exporter's TITLES list
extended; it must not reuse `v31_probe03`) and into `GameFoundation02/Clips`. Clips are cheap in the folder;
the ordering matters for what gets *wired into the controller* first.

Current bundle contents (verified from `GameFoundation01/Clips/`, 15 files — note this is the exact set,
including **Arc Step LEFT start/loop/end** and Run start/Run stop): `Idle`, `Run_forward`, `Run_start`,
`Run_stop`, `Jump`, `Ascend`, `Descend`, `Land`, `Glide_enter`, `Glide_loop`, `Glide_exit`,
`Arc_Bolt_right`, `Arc_Step_left_start`, `Arc_Step_left_loop`, `Arc_Step_left_end`.

### Tier 1 — required for the M3 kit (34 clips)
| Group | Clips (title = catalog `title`; file = underscores) | Frames | Loop |
|---|---|---|---|
| Locomotion 8-dir run (7) | `Run backward`, `Run left`, `Run right`, `Run forward right`, `Run forward left`, `Run backward right`, `Run backward left` | 1–17 each | yes |
| Locomotion walk (8) | `Walk forward`, `Walk backward`, `Walk left`, `Walk right`, `Walk forward right`, `Walk forward left`, `Walk backward right`, `Walk backward left` | 1–27 each | yes |
| Arc Step 4-dir (9 — completes the set; the LEFT three already exist) | `Arc Step start`, `Arc Step loop`, `Arc Step end`, `Arc Step back start`, `Arc Step back loop`, `Arc Step back end`, `Arc Step right start`, `Arc Step right loop`, `Arc Step right end` | start 1–7, loop 1–11, end 1–16 | start/end no, loop yes |
| Arc Bolt (1) | `Arc Bolt left` (completes the L/R alternation; right exists) | 1–20 | no |
| Conduit Spear (1) | `Conduit Spear` | 1–20 | no |
| Open Circuit (3) | `Open Circuit` (1–30), `Open Circuit hold` (1–25), `Open Circuit end` (1–22) | — | hold yes |
| Discharge (2) | `Discharge` (1–28), `Discharge snap` (1–14) | — | no |
| Meter-full flourish (1) | `Meter full flourish` | 1–24 | no |
| Charge (2, repurposed per MASTER-PLAN (e)) | `Charge loop` (1–41), `Charge full` (1–25) | — | yes |

### Tier 2 — polish, wire after the Tier-1 graph works (12 clips)
`Aim neutral`, `Aim up`, `Aim down`, `Aim left`, `Aim right` (1–2 each, no loop; required for the aim layer —
wire these as soon as the aim layer exists, but they block nothing else);
`Run lean left`, `Run lean right` (1–17 loop); `Run pivot 180 left`, `Run pivot 180 right` (1–20);
`Plant turn 90 left`, `Plant turn 90 right` (1–16); `Run stop R` (1–16).

### Tier 3 — presentation, wire in the presentation pass (4 clips)
`Idle combat` (1–49 loop), `Spawn` (1–72), `Select idle` (1–97 loop), `Select intro` (1–44).

Total: 34 + 12 + 4 = **50 imported** → 65 in `GameFoundation02`.

**Lower priority / not recommended for M3:** none are excluded from import; the pivot/plant-turn/lean clips are
the only ones whose *controller wiring* is deferred (they need turnAngle-driven transitions that don't exist yet).

## 2. AnimatorController layer graph for `GameFoundation02/Foundation.controller`

Current state (verified by reading the controller YAML): **one** layer named `Body`, **15 states**,
**zero transitions, zero blend trees**, default state `Idle` — so today only `Idle` is reachable via the graph
itself; `FoundationPresentation.CrossFadeInFixedTime` drives the other states by direct cross-fade. That is the
known defect. The new graph below replaces it. **Keep layer 0 named exactly `Body`** — `FoundationPresentation`
and the `FoundationAnimationContract` hard-code layer 0 and state names (spaces), and it logs
`HOLLOW_SAINT_ANIM_STATE_MISSING` for any name drift.

Proposed layers (5 total):

| # | Layer | Mask | Weight / IK | States |
|---|---|---|---|---|
| 0 | `Body` | full body | weight 1 | `Idle` (default), `Idle combat`, `Locomotion` (2D blend tree), `Run start`, `Run stop`, `Run stop R`, `Jump`, `Ascend`, `Descend`, `Land`, `Glide enter`, `Glide loop`, `Glide exit`, `Arc Step [dir] start/loop/end` ×4 dir (12 states), `Stun` (placeholder, see §3) |
| 1 | `UpperBody` | AvatarMask `HS_UpperBody`: spine, chest, neck, head, both scapulae, both pauldrons, upper arms, forearms (incl. forearm twist), hands, all finger bones, `L/R muzzle` — **excludes** pelvis, thighs, shins, feet, tabard, halo (halo has layer 4) | weight 1, sync off | `Empty` (default), `Arc Bolt right`, `Arc Bolt left`, `Conduit Spear`, `Open Circuit`, `Open Circuit hold`, `Open Circuit end` |
| 2 | `Overlay` | same mask as `UpperBody` | weight 1 | `Empty` (default), `Discharge snap`, `Discharge`, `Meter full flourish`, `Charge full`, `Charge loop` |
| 3 | `Aim` | AvatarMask `HS_Aim`: chest, neck, head, upper arms, forearms, hands, fingers, muzzles | weight 1 (faded by state machine, see below) | `Aim Off` (default, no motion), `Aim Tree` (2D tree of the 5 aim poses) |
| 4 | `Halo` | AvatarMask `HS_Halo`: `halo root`, `halo 1`, `halo 2`, `halo 3`, `halo 4`, `halo socket` | weight 1 | `Empty` (default), `Open Circuit hold` (same clip, halo-only mask — this is what lets the crown keep animating under an Arc Bolt that overrides `UpperBody`, per the STATUS suggestion) |

Glide / air / land are **states on layer 0**, not separate layers — they are exclusive full-body poses.
Stun/freeze/death: **no authored clips exist for these in the v31 catalog** (verified — the 65 titles contain
no stun/freeze/death clip). Proposal: `Stun` state on layer 0 holding `Idle combat` with the animator slowed
by code; freeze = set `animator.speed = 0`; death = RoR2 ragdoll (needs its own later work item — flag to
coordinator, see §14). Do not invent clips.

### Layer 0 — `Body` details

**`Locomotion` state = one 2D Freeform Cartesian blend tree**, parameters `forwardSpeed` (horizontal, +forward)
and `rightSpeed` (vertical, +right), positions taken from the catalog's own `speed_mps` and direction data
(`hs_move_x`/`hs_move_y` conventions: x = right+, y = forward+; diagonal components = speed × 0.7071):

| Motion | (forwardSpeed, rightSpeed) | source speed_mps |
|---|---|---|
| `Idle` | (0, 0) | — |
| `Run forward` | (6.0, 0) | 6.0 |
| `Run backward` | (−4.25, 0) | 4.25 |
| `Run right` | (0, 3.4) | 3.4 |
| `Run left` | (0, −3.4) | 3.4 |
| `Run forward right` | (3.32, 3.32) | 4.7 |
| `Run forward left` | (3.32, −3.32) | 4.7 |
| `Run backward right` | (−2.70, 2.70) | 3.825 |
| `Run backward left` | (−2.70, −2.70) | 3.825 |
| `Walk forward` | (1.5, 0) | 1.5 |
| `Walk backward` | (−1.0, 0) | 1.0 |
| `Walk right` | (0, 0.6) | 0.6 |
| `Walk left` | (0, −0.6) | 0.6 |
| `Walk forward right` | (0.74, 0.74) | 1.05 |
| `Walk forward left` | (0.74, −0.74) | 1.05 |
| `Walk backward right` | (−0.57, 0.57) | 0.8 |
| `Walk backward left` | (−0.57, −0.57) | 0.8 |

17 motions, Automate Thresholds **off**. Known tradeoff (flag for playtest): a Freeform tree blends walk and run
simultaneously at intermediate speeds and Unity does **not** phase-sync the two gaits' foot contacts; if mid-speed
foot ghosting shows up, fall back to two states — `Walk Tree` and `Run Tree` with the same 9-position inner tree —
split at `planarSpeed ≈ 2.5 m/s` with hysteresis (enter Run at >2.8, return to Walk at <2.2). Per-direction cycle-rate
mismatch is handled by the existing global `animator.speed = planarSpeed/7` clamp in `FoundationPresentation`
(since root motion is off); replacing that with per-state playbackRate params is a later polish item and touches
frozen `FoundationPresentation.cs` — **coordinator change request**.

**Layer 0 transitions** (all fixed-duration cross-fades, seconds; conditions on the declared parameters):

| From → To | Condition | Duration |
|---|---|---|
| `Idle` → `Locomotion` | `isMoving == true` | 0.15 |
| `Locomotion` → `Idle` | `isMoving == false` | 0.2 |
| `Idle` → `Run start` | `isMoving == true` && `forwardSpeed > 0.5` (acceleration from standstill) | 0.05 |
| `Run start` → `Locomotion` | exit time 1.0 (R contact = clip end) | 0.1 |
| `Locomotion` → `Run stop` | `isMoving == false` && `rightSpeed <= 0.1` | 0.08 |
| `Locomotion` → `Run stop R` | `isMoving == false` && `rightSpeed > 0.1` (plant the leading/strafing foot) | 0.08 |
| `Run stop` / `Run stop R` → `Idle` | exit time 1.0 (marker `Stopped`, normalized 1.0) | 0.1 |
| `Idle` / `Locomotion` → `Jump` | `isAirborne == true` && `upSpeed > 1` | 0.05 |
| `Jump` → `Ascend` | exit time 0.5 (marker `Takeoff` = (6−1)/10) **or** `upSpeed > 0.5` | 0.08 |
| `Ascend` → `Descend` | `upSpeed < 0` | 0.12 |
| `Descend` → `Land` | `isGrounded == true` | 0.02 |
| `Land` → `Locomotion` | exit time 0.95 && `isMoving == true` | 0.1 |
| `Land` → `Idle` | exit time 0.95 && `isMoving == false` | 0.1 |
| `Idle` / `Locomotion` → `Glide enter` | `isSprinting == true` && `isGrounded == true` && `forwardSpeed > 4.5` | 0.1 |
| `Glide enter` → `Glide loop` | exit time 0.9 (seam QA'd in Blender; keep the settle) | 0.12 |
| `Glide loop` → `Glide exit` | `isSprinting == false` | 0.1 |
| `Glide exit` → `Locomotion` | exit time 0.93 (just before marker `L contact` = 1.0) | 0.1 |
| Any → `Stun` | `isStunned == true` (proposed new param) | 0.03 |
| `Stun` → `Locomotion` / `Idle` | `isStunned == false` | 0.15 |

Arc Step (12 states) are **not transition-reachable** — they are played by the `HollowSaintArcStep`
EntityStateMachine via `PlayAnimation`/`PlayCrossfade` (sequence: start → loop → end, direction chosen in code).
Same for any skill gesture: skill states live on their layers and are entered by EntityStates, not by conditions.

### Layer 1 — `UpperBody` transitions
`Empty` is default. Skill EntityStates play states here with `PlayAnimation("UpperBody", "<state>", "attackSpeed", duration)`.
No graph transitions needed beyond `Empty ↔ gesture states` — RoR2's `PlayCrossfade` handles entry; add one
`Any → Empty` transition on `inCombat == false` with exit-time none, duration 0.25, so gestures fade back when
combat ends. [PROPOSAL]

### Layer 2 — `Overlay` (Discharge / meter-full)
`Empty` default. `Discharge snap` and `Discharge` are played at the Discharge proc (passive, no EntityState —
`DischargeMeter` plays the clip via the model animator). `Meter full flourish` plays once when the meter hits
100% (at marker `Full` it is already past its own flash). `Charge loop`/`Charge full` are **not** auto-played in
M3 (kept for the flourish variants). [PROPOSAL per VFX-ABILITY-PLAN open Q9]

### Layer 3 — `Aim`
`Aim Off` (default) ↔ `Aim Tree`: transition `Aim Off → Aim Tree` on `aimWeight > 0.05` (duration 0.1),
back on `aimWeight <= 0.01` (duration 0.15). `Aim Tree` = 2D Freeform Cartesian, horizontal `aimYawCycle`,
vertical `aimPitchCycle`, **both redefined as signed −1…+1 with 0 = neutral** (these params exist but are unused;
we own their semantics — record that contract in `KitShared.cs`): positions `Neutral (0,0)`, `Up (0,+1)`,
`Down (0,−1)`, `Left (−1,0)`, `Right (+1,0)`. The 5 pose clips are 2-frame absolute poses, so an override-weight
layer crossfade (not a true Unity additive transform layer — unreliable on Generic rigs) gives the additive look
safely. [PROPOSAL on the signed contract]

### Layer 4 — `Halo`
`Empty` default; `Open Circuit hold` played by the OpenCircuit EntityState while the buff lasts (halo-only mask,
so arms can be owned by `UpperBody`/Arc Bolt simultaneously).

### EntityState ↔ animation contract (who plays what, where, and fires when)

| Skill machine (KitRegistration const) | EntityState behavior | Anim states (layer) | Duration | Fire fractions (see §4) |
|---|---|---|---|---|
| `HollowSaintArcBolt` | primary; L/R alternate; duration = 0.5 s / attackSpeed | `Arc Bolt right`/`left` on `UpperBody`, playbackRate param `attackSpeed` | 0.5 s / attackSpeed | fire `Bolt release` 0.2105 (state-time 0.105 s at 1×); next shot interrupts at `Interrupt` 0.6316; recovery tail only when firing stops |
| `HollowSaintConduitSpear` | secondary; duration 0.6 s / attackSpeed per 5b | `Conduit Spear` on `UpperBody` | 0.6 s / attackSpeed | release `Spear release` 0.3158 (state-time 0.189 s at 1×); cancellable from `Cancel` 0.5263 |
| `HollowSaintArcStep` | utility; dash is gameplay motion; anim sequence start→loop→end | `Arc Step [dir] start/loop/end` on `Body` | start clip 0.25 s (Unity length = 6 f; the 5b doc's "0.29 s" counts all 7 frames), dash 0.25 s (~9 m), end 0.625 s (15 f, until `Recovered`) | `Dash start` 1.0 (clip end — dash begins immediately after start clip), `Arrive`/`Cancel` 0.2667, `Recovered` 0.8667 |
| `HollowSaintOpenCircuit` | special; cast 30 f, crown at `Crown active`, hold for 8 s buff, then end | `Open Circuit` → `Open Circuit hold` (loop) → `Open Circuit end` on `UpperBody`; halo part on `Halo` layer | cast 1.2083 s (29 f); hold = buff 8 s (hold clip loops at 1.0 s); end 0.875 s (21 f) | `Unfold` 0.1724, `Crown active` 0.7241 (buff/pulses start), `Recall` 0.0952, `Recovered` 1.0 |
| — (passive, `DischargeMeter`) | fires on next enemy hit at 100% meter | `Discharge snap` on `Overlay` (preferred; `Discharge` is the long variant) | snap 0.583 s; full 1.125 s | `Release` 0.1538 (snap) / 0.2963 (full) |

Timing rule (make this a `KitShared` comment): because `PlayCrossfade`/`PlayAnimation` scale the clip to the
state duration, **the normalized fraction of a marker is invariant under duration/playback changes** — the
EntityState fires effects when `normalizedTime` crosses the marker fraction. All fractions = `(frame − 1) / (end − start)`.

## 3. Animator parameters — the full contract

Declared today (verified in `Foundation.controller`, all unused by any transition): bools `isMoving`,
`isGrounded`, `isSprinting`, `isDeath`, `inCombat`, `isAirborne`; floats `forwardSpeed`, `rightSpeed`, `upSpeed`,
`walkSpeed`, `aimPitchCycle`, `aimYawCycle`, `aimWeight`, `turnAngle`, `jumpPlaybackRate`, `attackSpeed`.

| Parameter | Type | Driven by (code owner) | Used by |
|---|---|---|---|
| `isMoving` | Bool | `FoundationPresentation`-successor: `planarSpeed > 0.1` (existing logic) | Idle ↔ Locomotion, Run start/stop |
| `isGrounded` | Bool | `characterMotor.isGrounded` | Descend → Land, Glide entry |
| `isSprinting` | Bool | `body.isSprinting` | Glide enter/exit |
| `isAirborne` | Bool | `!characterMotor.isGrounded` | Jump entry |
| `isDeath` | Bool | death handling (ragdoll transition; M-later) | reserved |
| `inCombat` | Bool | `body.outOfCombat` inverted | UpperBody → Empty fade |
| `forwardSpeed` | Float | `characterMotor.velocity` projected on model forward, smoothed ~4 frames | Locomotion tree x-axis, Run start gate, Glide entry |
| `rightSpeed` | Float | same on model right, smoothed ~4 frames | Locomotion tree y-axis, Run stop vs Run stop R |
| `upSpeed` | Float | `characterMotor.velocity.y` | Jump/Ascend/Descend conditions |
| `walkSpeed` | Float | reserved for the walk/run split fallback tree (threshold 2.5–2.8) | not wired in v1 |
| `aimPitchCycle` | Float | signed pitch of aim direction vs model forward, `asin(dot)` −1…+1 | Aim tree (vertical) |
| `aimYawCycle` | Float | signed yaw, `atan2(side component)` −1…+1 | Aim tree (horizontal) |
| `aimWeight` | Float | 1 while Arc Bolt/Spear/cone states active (RoR2 aim convention), 0 otherwise | Aim layer fade |
| `turnAngle` | Float | signed turn rate (rad/s or deg — pick one, record in KitShared) | reserved for pivot/plant-turn/lean transitions (Tier 2) |
| `jumpPlaybackRate` | Float | Jump-state playback so Takeoff matches actual jump impulse | reserved |
| `attackSpeed` | Float | RoR2 already drives this on model animators; EntityStates pass it as the playbackRate param name | all gesture durations |
| `isStunned` | Bool (**add in builder v2**) | `SetStateOnHurt`/stun component | Any → Stun |
| `meterCharge` | Float 0–1 (**add in builder v2**) | `DischargeMeter` (server, replicated read for visuals) | halo gap arcs, core glow, ready ping — driven by the VFX controller, not transitions |

`walkSpeed`, `turnAngle`, `jumpPlaybackRate`, `isDeath` stay declared-but-reserved so no builder change removes
them (kit contract: append-only shared constants).

## 4. Marker → normalized time table (read from catalog.json — do not invent)

Formula (repo convention, verified against `ProbeAssets.ImportClip` event times and the VFX-ABILITY-PLAN's own
numbers): `fraction = (frame − 1) / (end − start)`, `seconds = (frame − 1) / 24` at 1× speed. The task brief's
example "Spear release at f7 of 20 → 0.30" is slightly off: the exact fraction is 6/19 = **0.3158**. Because
`PlayCrossfade` scales the clip into the state, `normalizedTime` equals this fraction at any attack speed.

### Kit clips (what EntityStates and VFX beats key off)
| Clip (frames) | Marker | Frame | Fraction | Seconds @1× | Use |
|---|---|---|---|---|---|
| `Arc Bolt right` / `Arc Bolt left` (1–20) | `Anticipation` | 3 | 0.1053 | 0.083 | fingertip glint/crackle at MuzzleRight/Left |
| | `Bolt release` | 5 | **0.2105** | 0.167 | fire hit + tracer + muzzle flash |
| | `Interrupt` | 13 | **0.6316** | 0.500 | next-shot interrupt / cancel point |
| | `Recovered` | 20 | 1.0000 | 0.792 | safe exit |
| `Conduit Spear` (1–20) | `Materialize` | 2 | 0.0526 | 0.042 | filaments begin on LowerArmR |
| | `Draw` | 5 | 0.2105 | 0.167 | filaments converge; lance threshold ramps |
| | `Spear release` | 7 | **0.3158** | 0.250 | spawn projectile ~150 m/s, palm flash, copper sparks |
| | `Cancel` | 11 | 0.5263 | 0.417 | cancel window opens |
| | `Fade` | 14 | 0.6842 | 0.542 | any-state fade window |
| | `Recovered` | 20 | 1.0000 | 0.792 | safe exit |
| `Arc Step [fwd/bk/left/right] start` (1–7) | `Dash start` | 7 | **1.0000** | 0.250 | launch: burst FX, afterimage, dash motion begins (marker is at the last frame) |
| `Arc Step [dir] loop` (1–11) | — | — | — | — | travel, looped |
| `Arc Step [dir] end` (1–16) | `Arrive` | 5 | **0.2667** | 0.167 | arrival puff + static ring; trail stops emitting |
| | `Cancel` | 5 | 0.2667 | 0.167 | same frame |
| | `Recovered` | 14 | 0.8667 | 0.542 | safe exit |
| `Open Circuit` (1–30) | `Unfold` | 6 | 0.1724 | 0.208 | gap arcs spark as segments separate |
| | `Crown active` | 22 | **0.7241** | 0.875 | crown ignition flash; buff + pulse schedule starts |
| `Open Circuit hold` (1–25 loop) | `Pulse` | 1 | 0.0000 | 0.000 | pulse strike origin beat each loop |
| `Open Circuit end` (1–22) | `Recall` | 3 | 0.0952 | 0.083 | tethers retract, gaps dim |
| | `Recovered` | 22 | 1.0000 | 0.875 | safe exit |
| `Discharge` (1–28) | `Gather` | 5 | 0.1481 | 0.167 | arcs draw inward |
| | `Release` | 9 | **0.2963** | 0.333 | core flash + radial ring + hand lances (1-frame hit-stop f9–10) |
| | `Recovered` | 28 | 1.0000 | 1.125 | — |
| `Discharge snap` (1–14) | `Release` | 3 | **0.1538** | 0.083 | same burst, reactive timing (preferred for the proc) |
| | `Recovered` | 14 | 1.0000 | 0.542 | — |
| `Charge loop` (1–41 loop) | `Pulse high` | 11 | 0.2500 | 0.417 | orb surge |
| | `Pulse low` | 31 | 0.7500 | 1.250 | orb dim |
| `Charge full` (1–25 loop) | `Pulse high` | 7 | 0.2500 | 0.250 | heartbeat surge |
| `Meter full flourish` (1–24) | `Full` | 6 | 0.2174 | 0.208 | flourish begins |
| | `Release` | 15 | 0.6087 | 0.583 | vent flash |
| | `Recovered` | 24 | 1.0000 | 0.958 | — |

### Locomotion / presentation clips (cosmetic beats)
| Clip (frames) | Marker | Frame | Fraction | Use |
|---|---|---|---|---|
| `Run forward` + all 7 other run dirs (1–17) | `L contact` / `R contact` | 1 / 9 | 0.0000 / **0.5000** | run push-off spark at each toe-off (dominant-clip rule; never double-fire in blends) |
| `Walk forward` + 7 walk dirs (1–27) | `L contact` / `R contact` | 1 / 14 | 0.0000 / 0.5000 | no sparks (9d) |
| `Glide enter` (1–13) | `Push off` / `Airborne` | 3 / 5 | 0.1667 / **0.3333** | last push-off sparks / jet ignition burst plays |
| `Glide loop` (1–33) | `Hover high` / `Hover low` | 1 / 17 | 0.0000 / 0.5000 | steady jet flicker reference |
| `Glide exit` (1–14) | `Legs down` / `L contact` | 5 / 14 | 0.3077 / 1.0000 | jet cut (~3 f) + trail afterglow |
| `Jump` (1–11) | `Crouch` / `Takeoff` | 3 / 6 | 0.2000 / **0.5000** | optional heel spark at Takeoff [PROPOSAL, open Q8] |
| `Land` (1–15) | `L touch` / `R touch` / `Compress` / `Recovered` | 2 / 4 / 6 / 15 | 0.0714 / 0.2143 / 0.3571 / 1.0000 | optional dust/static ring in the L touch–Compress window [PROPOSAL] |
| `Run start` (1–17) | `L step` / `R contact` | 8 / 17 | 0.4375 / 1.0000 | — |
| `Run stop` (1–16) | `L contact`/`R plant`/`L plant`/`Stopped` | 1/6/11/16 | 0.0000/0.3333/0.6667/1.0000 | — |
| `Run stop R` (1–16) | `R contact`/`L plant`/`R plant`/`Stopped` | 1/6/11/16 | same fractions | — |
| `Run pivot 180 left/right` (1–20) | `L contact` / `R contact` | 1 / 20 | 0.0000 / 1.0000 | — |
| `Plant turn 90 left/right` (1–16) | `L contact` / `R contact` | 1 / 16 | 0.0000 / 1.0000 | — |
| `Run lean left/right` (1–17) | `L contact` / `R contact` | 1 / 9 | 0.0000 / 0.5000 | — |
| `Idle` (1–97) | `Inhale`/`Twitch L`/`Glance`/`Twitch R` | 1/30/58/78 | 0.0000/0.3021/0.5938/0.8021 | optional micro crackles [PROPOSAL] |
| `Spawn` (1–72) | `Awaken`/`Halo lit`/`Ready` | 13/38/60 | 0.1690/**0.5211**/0.8310 | core flicker; halo gaps light arc-by-arc at `Halo lit` |
| `Select intro` (1–44) | `Snap`/`Halo flare`/`Settled` | 13/15/44 | **0.2791**/0.3256/1.0000 | fingertip snap spark; halo flash |
| `Idle combat` (1–49) | `Inhale` | 1 | 0.0000 | — |

## 5. The naming trap (spaces vs underscores) — read before writing any Play call

- Catalog `title` → FBX file name replaces spaces with underscores → Generated/bundle `.anim` asset keeps the
  **underscore** name (`Arc_Bolt_right.anim`) → the **controller state name** is `source.name.Replace('_',' ')`
  → `"Arc Bolt right"` **with spaces**. Verified in both the builder code and the controller YAML
  (states `Arc Step left start`, `Arc Bolt right`, `Run forward`, …).
- Therefore **every `PlayAnimation`/`PlayCrossFade`/`CrossFadeInFixedTime` call uses the SPACE form**. Using the
  underscore form silently no-ops (RoR2 logs `Animator.GotoState: State could not be found`, then
  `Invalid Layer Index '-1'` — the exact warning pair in the 21:53 log).
- `FoundationAnimationContract.StateName()` (in frozen `FoundationPresentation.cs`) is the single mapping point;
  keep its enum/state strings byte-identical in GameFoundation02. Any new driven state goes through the same
  contract, not raw strings at call sites.
- Second trap: alias casing. The ChildLocator aliases are `MuzzleLeft`/`MuzzleRight`/`HeelL`/`HeelR` — **not**
  `MuzzleL`/`MuzzleR`/`HeelJetL`/`HeelJetR` as some VFX docs write. See Part B §8.

## 6. Concrete build procedure for GameFoundation02 (coordinator, in Stuart's open editor)

Prerequisite (Blender side, coordinator-owned, background Blender only — never PID 4500): extend the `TITLES`
list in `tools/blender/export_unity_probe.py` with the 50 remaining titles, point `OUT` at
`HollowSaintUnityProject/Assets/HollowSaint/Source/v31_probe04/` (the script refuses existing folders), run it.
Verify `export-manifest.json` lists 65 clips. Do not re-edit `v31_probe03`.

Unity steps (editor UI / coordinator-edited scripts; **no new numbered folder is auto-created by the tools**):

1. In `Editor/ProbeAssets.cs` bump `Generated` to `"Assets/HollowSaint/Generated04/"` (the const is read by the
   bundle builder via `ProbeAssets.Generated`). Run the import (it creates `Generated04/Clips/` with 65 clips,
   `Generated04/Materials/`).
2. Copy `Editor/FoundationBundleBuilder.cs` to a v2 (do not edit in place unless coordinator chooses) and change
   `Folder` to `Assets/HollowSaint/GameFoundation02`; keep the throw-if-exists guard; add (a) the layer/mask
   setup of §2, (b) the blend trees of §2 (create via `BlendTree` + `stateMachine.CreateBlendTree...` editor API
   or author the controller by hand in the UI — the UI route is less error-prone for one build), (c) the two new
   parameters `isStunned`, `meterCharge`, (d) the layer-assignment table below.
   Layer assignment when adding states (the current builder dumps everything on layer 0):
   - Layer 0 `Body`: all `kind` locomotion/air/idle/presentation + 12 Arc Step states.
   - Layer 1 `UpperBody`: `Arc Bolt right/left`, `Conduit Spear`, `Open Circuit`, `Open Circuit hold`,
     `Open Circuit end`.
   - Layer 2 `Overlay`: `Discharge`, `Discharge snap`, `Meter full flourish`, `Charge loop`, `Charge full`.
   - Layer 3 `Aim`: the five `Aim *` clips (inside the `Aim Tree` blend tree; keep individual states too for
     direct play if wanted).
   - Layer 4 `Halo`: `Open Circuit hold` again (halo mask) — same clip asset, two states, fine.
3. Create the three AvatarMask assets (`HS_UpperBody`, `HS_Aim`, `HS_Halo`) and tick the bones listed in §2.
   Exact transform paths come from the model hierarchy (83 bones are in `Source/v31_probe03/export-manifest.json`
   `bones` — socket bones present today: `head socket`, `L/R muzzle`, `halo root`, `halo 1–4`, `halo socket`,
   `core socket`, `L/R heel socket`; **no** palm/orb/spear/back/ground bones exist — Phase E pending).
4. Run `Hollow Saint/Build game foundation bundle` (menu) — with the v2 script — to produce
   `GameFoundation02/mdlHollowSaint.prefab` and the bundle.
5. **Coordinator-owned mod-side changes to request** (files are frozen for skill agents):
   - `FoundationPresentation.cs`: stop driving `Run forward`/`Glide loop` directly once the tree exists; feed
     `forwardSpeed/rightSpeed/upSpeed/isMoving/...` instead; keep driving the air/glide state selection.
   - `FoundationAudit.cs`: renderer/mount assertions must be re-checked after any persistent VFX children are
     added (see Part B §12 — the 140-renderer lesson).
   - `FoundationContent.cs`: point the bundle loader at the new bundle name/folder when switching over.
6. Bundle-size check: `GameFoundation01` clips alone are ~120 MB of .anim assets (`Idle.anim` is 34.7 MB because
   the importer sets `animationCompression = Off`). With 65 clips the bundle will be large. **[PROPOSAL]** enable
   keyframe reduction for the game build (keep `Off` for the preview path) and QA contact frames L1/R9 (run) and
   L1/R14 (walk) against the source. Measure and report the bundle size before/after.

---

# PART B — VFX prefab plan

## 7. VFX mesh → skill/beat mapping (meshes verified in `fbx_manifest.json` — 11 FBX, 29 meshes)

| Mesh (FBX file) | Tris | Serves | Notes from manifest |
|---|---|---|---|
| `HS_arc_short_A/B/C` (`HS_arc_short.fbx`) | 128/128/192 | Arc Bolt **chain hop**, fingertip crackle, halo gap arcs | 0.4 m along +Z from start pivot; crossed ribbons; pick A/B/C randomly, re-roll every ~2 f |
| `HS_bolt_long_A/B/C` (`HS_bolt_long.fbx`) | 1024 ea | Arc Bolt **main tracer** muzzle→hit, Open Circuit **pulse strike** | 10 m along +Z; scale local Z = distance/10, keep X/Y = 1 |
| `HS_bolt_branch_A/B` (`HS_bolt_branch.fbx`) | 1216/1280 | **Discharge** side lances (2 per hand), empowered Arc Bolt at target | 3 m along +Z from start pivot |
| `HS_halo_ring_segments` (`HS_halo_ring.fbx`) | 320 | persistent halo rim shimmer (meter, Open Circuit crown) | ring in Unity XY plane, r 0.32 (placeholder — fit to the v18 halo in editor) |
| `HS_halo_gap_arc_1..4` (`HS_halo_ring.fbx`) | 64 ea | meter gap bridges (25/50/75/100%), Discharge petal vent | gap centres 45/135/225/315°; parent under `Halo`, rotate locally |
| `HS_jet_cone_outer`, `HS_jet_cone_core` (`HS_heel_jet.fbx`) | 384 ea | persistent **heel jets** (glide), run push-off plume | pivot = nozzle; plume along local +Z; orient with `LookRotation(−travelDir)` |
| `HS_jet_arcs`, `HS_jet_ribbon` (`HS_heel_jet.fbx`) | 96/64 | crackle arcs inside jet plume; exhaust streak | arcs spin 130–160°/s about local Z (L+, R−) |
| `HS_jet_burst_spikes`, `HS_jet_burst_ring` (`HS_jet_burst.fbx`) | 448/96 | **Arc Step launch** burst, glide ignition | spikes scale 0.25→1 over 0.12 s; ring 1→2.5 over 0.15 s, both fading |
| `HS_spear_lance_core`, `HS_spear_lance_shell` (`HS_spear.fbx`) | 720/1200 | Conduit Spear **projectile** | 1.2 m, pivot centre, tip +Z; shell = fresnel glow |
| `HS_spear_trail` (`HS_spear.fbx`) | 96 | spear trail ribbon (or TrailRenderer 0.12 s, width 0.05→0) | 1.6 m behind tail |
| `HS_spear_filaments` (`HS_spear_materialize.fbx`) | 1152 | Conduit Spear **materialize** f2–6 on the R forearm | draw-on by UVMap.x; needs the threshold shader (§10) |
| `HS_mark_ring_segments`, `HS_mark_diamond` (`HS_conductor_mark.fbx`) | 240/2 | **Conductor mark** on the spear-impacted enemy (6 s, rotating 30–45°/s) | r 0.25 m; scale ≈ 0.6 × body radius clamped 0.25–1.2 m |
| `HS_mark_quad` (`HS_conductor_mark.fbx`) | 2 | cheapest PNG version of the mark | 0.694 m quad for `hs_conductor_mark_512.png` |
| `HS_step_ground_trail` (`HS_step_ground_trail.fbx`) | 136 | **Arc Step** ground trail (grounded only; in air use a TrailRenderer instead) | 9 m along +Z, 3 cm above floor; scale Z = dash distance/9 |
| `HS_card_quad_1m` (`HS_quads.fbx`) | 2 | glow cards (star/soft/ring flashes) | 1×1 m, faces −Z |
| `HS_decal_quad_1m` (`HS_quads.fbx`) | 2 | scorch/static-ring ground decals | 1×1 m, faces up |

Textures: 25 RGBA PNGs verified present in `art/vfx/assets/textures/` (list matches README).

## 8. Sockets: what actually exists (verified from `FoundationMounts.cs`) vs. what the VFX docs assume

The plugin wires exactly **23 aliases**: `Head, Chest, Stomach, Pelvis, UpperArmL, UpperArmR, LowerArmL,
LowerArmR, HandL, HandR, ThighL, ThighR, CalfL, CalfR, FootL, FootR, MuzzleLeft, MuzzleRight, Core, Halo,
HeelL, HeelR, MainHurtbox`. Bones: `head, chest, spine, pelvis, L/R upperarm, L/R forearm, L/R hand, L/R thigh,
L/R shin, L/R foot, L/R muzzle, core socket, halo socket, L/R heel socket` (+ the created `MainHurtbox` transform).

**Contradiction to flag:** the VFX-ABILITY-PLAN and `art/vfx/assets/README.md` cite sockets/aliases that do **not**
exist in the current model or mounts: `MuzzleL/MuzzleR` (actual: `MuzzleLeft/MuzzleRight`), `PalmL/PalmR`,
`Orb`, `Spear`, `Back`, `HaloArc1–4`, `Ground`. The export manifest itself says:
*"Existing sockets retained; complete Phase E socket additions remain pending."* Do not write code against the
phantom aliases. Substitutions for M3 (verified aliases only):

| VFX need | Phantom socket in docs | M3 substitute (verified alias) |
|---|---|---|
| Discharge hand bursts / palm flash | `PalmL/R` | `HandL` / `HandR` |
| Charge orb between palms | `Orb` | `Core` with a local offset on the prefab, or defer orb VFX [PROPOSAL] |
| Spear materialize parent | `Spear` | `LowerArmR` (the filaments' forearm-end UV origin matches the forearm conductor) |
| Open Circuit tethers to upper back | `Back` | `Chest` with a rear local offset, or defer tethers to M4 [PROPOSAL] |
| Per-arc gap-arc parenting | `HaloArc1–4` | one prefab under `Halo` with 4 children rotated 45/135/225/315° locally |
| Ground trail / decal anchor | `Ground` | world-space placement from a raycast at spawn (README's option A) — no socket |
| Arc Bolt muzzle origin | `MuzzleR/L` | **`MuzzleRight` / `MuzzleLeft`** (exact strings) |
| Heel jets / bursts | `HeelJetL/R` | **`HeelL` / `HeelR`** (exact strings) |

Phase E (adding `L/R palm`, `orb socket`, `spear socket`, `back socket`, `L/R heel jet` bones, `ground`) remains a
real future upgrade path — when it lands, the coordinator adds mounts in `FoundationMounts.cs` (it owns that
file's freeze; the alias additions belong in a coordinator change request).

## 9. Shader strategy without reintroducing the rear-shell regression

**Root cause recap (verified from the handoff's diagnostic section):** RoR2's
`CharacterModel.UpdateRendererMaterials` assigns **one base material per renderer**; the body is one skinned
renderer with 4 material slots (submeshes), so any "one material for the model" pass silently rendered only
submesh 0 → the visible hole in the back. The fix that ships: `FoundationMeshSplitter` splits the 4 submeshes
into 4 skinned renderers (140 renderers total, asserted by `FoundationAudit`), each with its own Standard
material, and **`ignoreOverlays = true` on every body rendererInfo** because RoR2's overlay shaders corrupted
the Standard render path.

Rules for VFX (conservative, concrete):

1. **One-shot effects never touch the body.** Tracers, impacts, flashes, marks, decals, afterimages are
   standalone prefabs spawned in world space (or on the target), registered as networked effects. They are never
   children of the model and never appear in `CharacterModel.rendererInfos` → `UpdateRendererMaterials` and the
   overlay system cannot see them. Zero regression risk. This is the pattern for ~20 of the prefabs in §11.
2. **Persistent body-attached VFX (heel jets, halo gaps/ring, core glow, crown shimmer) are added as child
   GameObjects under the verified mount transforms at runtime, and are NOT added to `rendererInfos`.**
   `UpdateRendererMaterials` only iterates its rendererInfos list, so unknown child renderers keep their own
   materials. The body already sets `ignoreOverlays = true` on its own infos, so the damage-flash overlay path is
   off for this model anyway. **Do not** add these renderers to rendererInfos (that would put them back through
   the one-material-per-renderer conversion) and **do not** merge any body material.
3. **Ordering:** `FoundationMaterials.Apply(model)` iterates ALL child renderers and caps bright emissions
   (MaxEmission 0.9) — if it ran after the jet/crown children were attached it would dim the VFX. Attach
   persistent VFX **after** the material pass (or exclude non-body renderers in a coordinator change request).
4. **Audit interplay:** `FoundationAudit` asserts 140 renderers / 23 mounts at startup. Persistent VFX children
   change the renderer count if the audit counts `GetComponentsInChildren(true)` (it does — that's how the 140
   assertion works). Either attach VFX after the audit runs, or update the assertion in the same build (the
   handoff's exact lesson: "When changing model structure, update startup assertions in the same build before
   asking for a playtest").
5. **Shader choice:** use the game's own `Hopoo Games/FX/Cloud Remap` (HGCloudRemap), found at runtime with
   `Shader.Find("Hopoo Games/FX/Cloud Remap")` — vanilla ships it, it is built for additive FX, plays nice with
   RoR2 bloom, and avoids bundling any shader. Assign: `_MainTex` = our sprite/ribbon texture, `_RemapTex` =
   `hs_meter_ramp_256x16` (or a cyan ramp), `_Cloud1Tex`/`_Cloud2Tex` = `hs_noise_scroll/hs_noise_streak/
   hs_noise_crackle` with the scroll speeds from the README. **Verify the exact property names on a vanilla
   material in the editor once** (one right-click inspect) — do not trust the property list from memory.
   Set Cull Off (ribbons are crossed pairs), ZWrite Off, additive blend, renderQueue 3000+.
   **Fallback** if HGCloudRemap proves fiddly: a minimal custom unlit additive shader (`HS/FX/AdditiveUnlit`:
   Cull Off, ZWrite Off, `Blend One One`, `rgb × tex.a × vertexColor.a × tint × HDR`, optional second layer on
   TEXCOORD1 with its own tiling/scroll) **included in our own bundle** — a small custom shader in our bundle is
   safe (it is our asset; only game-owned shaders are at risk of stripping).
6. **Two places need a bespoke shader either way** (the dissolve/draw-on effects): `HS_spear_lance_core/shell`
   and `HS_spear_filaments` use threshold/draw masks (`visible = mask.b < _Threshold`, filaments
   `UVMap.x < _Draw`, hot band ×2.5). Write one small `HS/FX/ThresholdUnlit` custom shader, include it in the
   bundle, and confirm it survives bundle load with a tiny load-test (the phase-G "asset bundle load test" item).
7. **Never** apply HG overlay-family shaders or RoR2 "fake" materials to the body renderers; never remove the
   `ignoreOverlays = true` flags on body rendererInfos; never re-consolidate body materials (keep the 4-slot
   invariant; `FoundationMeshSplitter` owns it).

## 10. Texture import settings (from the README — verified doc; apply in editor)

- All: Default type, sRGB **on**, Alpha Is Transparency on, mipmaps on.
- sRGB **off** (data masks): `hs_spear_dissolve_mask_128x512`, `hs_noise_scroll_256`, `hs_noise_streak_256`,
  `hs_noise_crackle_256`, `hs_ghost_scanline_256`.
- Wrap = **Repeat**: `*_tile`, `noise_*`, `trail_*`, `ghost_*`, `band_*`, `jet_exhaust` (i.e.
  `hs_bolt_ribbon_tile_512x64`, `hs_trail_angular_512x128`, `hs_band_profile_64`, `hs_jet_exhaust_256`, the
  three `hs_noise_*`, `hs_ghost_scanline_256`). Wrap = **Clamp**: everything else (flipsheets, glows, sparks,
  decals, mark, ramps).
- Vertex colour `Col` (alpha = end fade/taper) must multiply into alpha in every shader — the FBX carry it
  (verified in the manifest reimport check: `colors: ["Col"]` on every mesh).
- UV2 (`UVTile`) is TEXCOORD1 with U in texture tiles — scrolling layers bind to `uv2`.

## 11. Prefabs to create (names, spawn context, socket, timing)

Naming: `FX_HS_<Skill>_<Thing>`. One-shots that must appear on all clients get registered as networked effects
(R2API `ContentAddition.AddEffect` / `EffectCatalog`); items marked **local** are cosmetic children driven by
replicated state, not registered effects.

**Arc Bolt (machine `HollowSaintArcBolt`; fire fraction 0.2105, interrupt 0.6316)**
| Prefab | Content | Spawn | Timing |
|---|---|---|---|
| `FX_HS_ArcBolt_MuzzleFlash` | star card ×8 + 2–3 short arcs + crackle flipbook burst | `MuzzleRight`/`MuzzleLeft` (alternating hands), world one-shot | at `Bolt release` |
| `FX_HS_ArcBolt_Tracer` | `HS_bolt_long_A/B/C` mesh, scale Z = hitDist/10, aimed +Z at hit; HDR ×6 core | from muzzle to first hit; networked | at `Bolt release`; life 0.10–0.15 s, 2–3 f fade; UVTile.x scroll −10/s |
| `FX_HS_ArcBolt_ChainHop` | `HS_arc_short_A/B/C` scaled to hop distance + impact sparks (6–12) + crackle | between hop targets, at each hop; networked | at each chain hit |
| `FX_HS_ArcBolt_Impact` | spark burst + star card + reflected-light flash | at first hit; networked | at `Bolt release` |
| `FX_HS_ArcBolt_Empowered` | branch bolt at target + brighter tracer variant (width ×1.6, HDR ×10) | at target; networked | Discharge-empowered shot |

**Conduit Spear (`HollowSaintConduitSpear`; release 0.3158)**
| Prefab | Content | Spawn | Timing |
|---|---|---|---|
| `FX_HS_Spear_Materialize` | `HS_spear_filaments` (draw-on `_Draw` 0→1.05 over f2–5) + lance core/shell threshold ramp (f2–6, shell 1 f later) + core flash card at `Core` f6 | **local**, parented under `LowerArmR`, oriented along the forearm; life 0–0.25 s of the state | `Materialize` 0.0526 → `Spear release` 0.3158 |
| `FX_HS_Spear_Projectile` | lance core (HDR ×8) + shell (outer ×2, fresnel ~2, noise streak scroll V 1.5/s) + trail ribbon; TrailRenderer alternative (0.12 s, width 0.05→0, min vertex distance ≤ 0.3 m at 150 m/s) | spawned at release from `HandR` position along aim; parented to the projectile; networked | at `Spear release` 0.3158; ~150 m/s |
| `FX_HS_Spear_PalmFlash` | star + soft cards ×8 at the launch palm | `HandR`, one-shot | at `Spear release` |
| `FX_HS_Spear_Impact` | tight spark burst (8–12) + star card ×6 | at hit; networked | on impact |
| `FX_HS_ConductorMark` | ring segments + diamond (or the PNG quad), additive; scale 1.4→1 pop-in; segments rotate 30–45°/s; brighten ×2 on chain arrival; fade last 0.3 s of 6 s | parented to the marked enemy (billboard toward camera, body centre), networked | on spear impact; lifetime = mark 6 s; **one mark at a time** |

**Arc Step (`HollowSaintArcStep`; dash start 1.0 of start clip, arrive 0.2667 of end clip; 4 directions)**
| Prefab | Content | Spawn | Timing |
|---|---|---|---|
| `FX_HS_ArcStep_Launch` | `HS_jet_burst_spikes` + `HS_jet_burst_ring` at both heels, oriented along the dash vector | `HeelL`/`HeelR`; networked | at `Dash start` (normalized 1.0 of start clip) |
| `FX_HS_ArcStep_Afterimage` | baked `SkinnedMeshRenderer.BakeMesh` ghost of the full model incl. halo, additive fresnel rim (outer ×1.5) + `hs_ghost_scanline` 25–35% fill; α 1→0 over 0.35 s, scale 1.0→1.03 | at dash start position, world; networked | at `Dash start`; lives even if the step is cancelled |
| `FX_HS_ArcStep_Trail` | `HS_step_ground_trail` mesh (grounded; scale Z = dist/9) or TrailRenderer (0.35 s, width 0.05, `hs_trail_angular` tile) | from dash start point along dash vector; networked | emitted from `Dash start` until `Arrive` 0.2667, fade 0.3 s |
| `FX_HS_ArcStep_Arrive` | spark puff + static ring 1 m (`HS_decal_quad_1m` + `hs_static_ring_512`) | at arrival point (raycast to ground); networked | at `Arrive` 0.2667 |

**Open Circuit (`HollowSaintOpenCircuit`; crown 0.7241, hold 8 s, recall 0.0952 of end)**
| Prefab | Content | Spawn | Timing |
|---|---|---|---|
| `FX_HS_OpenCircuit_Crown` | `HS_halo_ring_segments` shimmer (noise crackle scroll 0.3/s on UVTile) + thin tethers (`HS_arc_short` chains) from crown toward `Chest`-substitute back anchor | **local**, under `Halo` (fit to the v18 halo radius in editor first) | from `Crown active` 0.7241 until `Recall`; halo layer keeps it alive under other gestures |
| `FX_HS_OpenCircuit_Pulse` | long bolt, brighter/thicker than hand bolt (HDR ×8, width ×1.4) | from crown to pulse target; networked | every pulse (≈1.0 s interval, tuning-owned) during hold |
| `FX_HS_OpenCircuit_GapSpark` | gap arcs flicker while segments separate | under `Halo`; local | f6–22 of the cast (`Unfold` → `Crown active`) |

**Discharge (passive — `DischargeMeter` spawns; no EntityState)**
| Prefab | Content | Spawn | Timing |
|---|---|---|---|
| `FX_HS_Discharge_Proc` | 2× `HS_bolt_branch` per hand along the fling direction + star/soft core flash ×10 (0.6–0.8 m, 3 f) + radial ring pulse (0.3→2.0 m over 0.2 s) + optional target impact | `HandL`/`HandR` + `Core`; target impact at the triggering enemy; networked | at the proc (the meter's next-hit rule); overlay clip `Discharge snap` starts on the same frame |
| `FX_HS_Discharge_Scorch` | `HS_decal_quad_1m` + `hs_scorch_decal_512` (alpha blend, 1.5–2.5 m, 4 s + 1 s fade) + additive glow variant fading 0.6 s + static ring | under the target, raycast to ground | at the proc [PROPOSAL, optional] |
| `FX_HS_MeterFull_Ping` | ring card 0.4→0.9 m, 0.15 s, ×6 | under `Halo`; local | the frame the meter hits 100% |

**Meter/persistent glow (local, driven by `meterCharge` + state, via `HollowSaintVFXController`)**
| Prefab | Content | Socket | Behavior |
|---|---|---|---|
| `FX_HS_CoreGlow` | soft card, emission = `ramp(meter) × (1 + 2.5·meter)`, breathing 0.5→1.5 Hz | `Core` | persistent, meter-scaled; dumps on proc over 6–8 f |
| `FX_HS_HaloGaps` | `HS_halo_gap_arc_1..4` children: 0–24% none, gap 1 at 25%, 2 at 50%, 3 at 75%, 4 at 100% | `Halo` | persistent; Discharge petal vent = all 4 at ×12 for 2 f then fade 6–8 f |
| `FX_HS_HeelJet` (instantiate ×2, L/R) | outer cone + core cone + arcs + ribbon, scale = jet strength, yaw = smoothed −travel vector, pitch per `hs_jet_dir` equivalent; spawn offset 4–6 cm outward when pointing into the leg | `HeelL` / `HeelR` | persistent; glide loop steady, glide enter ignition burst, exit cut ~3 f + trail afterglow; run push-off = short flashes (scale ~0.35, dying ~2.5 f) + sparks, never a full jet |
| `FX_HS_RunSparks` | 2–4 spark streaks, sprayed opposite travel | `HeelL`/`HeelR`, world-ish | at run contact fractions 0.0 / 0.5 (dominant-clip rule in blends) |

**Presentation (local)**
`FX_HS_Spawn_HaloIgnite` (gap arcs light arc-by-arc at `Spawn` 0.5211), `FX_HS_Select_Snap` (fingertip spark at
`MuzzleRight` at `Select intro` 0.2791 + halo flash at 0.3256), `FX_HS_Land_Dust` (static ring, small, in the
`Land` 0.0714–0.3571 window [PROPOSAL, open Q8]), `FX_HS_Jump_Takeoff` (heel spark at `Jump` 0.5 [PROPOSAL]).

Counts to respect (roadmap requirement): bounded pooled instances; one conductor mark at a time; afterimage pool
~4; scorch decals capped (~8).

## 12. Persistent VFX ↔ controller/Audit interplay (the two things most likely to bite)

1. `FoundationAudit`'s 140-renderer assertion and the 23-mount check run at startup; persistent VFX children
   (heel jets ×2, halo gaps, core glow, crown) added before the audit will break the count. Attach after the
   audit or update the assertion in the same build (handoff lesson, verbatim).
2. `FoundationMaterials.Apply` caps emissions on every child renderer — apply the body material pass before
   attaching persistent VFX, or the VFX gets dimmed.
3. `animator.speed` is global: `FoundationPresentation` currently scales it (`planarSpeed/7`). Once gesture
   layers exist, that global scale also speeds up Arc Bolt/Spear states while running. Replace global speed
   scaling with per-state playbackRate (`attackSpeed`, `jumpPlaybackRate` params) — coordinator change request
   to frozen `FoundationPresentation.cs`.
4. `HollowSaintVFXController` must compute jet/glow values at runtime (replicated state: sprint, glide,
   `characterMotor.velocity` projected to model space, smoothed 4–6 frames) because the `hs_*` curves are
   stripped from the game bundle (§0).

## 13. Contradictions vs the handoff docs (all verified against source files)

1. **Bundle clip set:** docs say "15 representative clips". The actual 15 include the **Arc Step LEFT**
   start/loop/end (not forward) plus `Run start`/`Run stop`. The M3 Arc Step is 4-direction, so the current
   bundle's step set is the wrong default direction — GameFoundation02 supersedes it.
2. **Phantom sockets:** VFX-ABILITY-PLAN §3.1 and the assets README cite `MuzzleL/R`, `PalmL/R`, `Orb`, `Spear`,
   `Back`, `HaloArc1–4`, `Ground`; `FoundationMounts.cs` wires `MuzzleLeft/MuzzleRight`, `HeelL/HeelR` and none of
   the others. Phase E sockets are still pending (manifest limitation says so verbatim). §8 gives substitutes.
3. **AnimationEvents/curves are stripped from the game bundle** (builder code), so the plan's marker/`clip.json`
   event approach and the `hs_*` signal curves do not reach the game; timing must be normalized fractions in
   EntityStates, and jet drivers must be computed at runtime.
4. **"Spear release f7 of 20 → 0.30"** (task brief) — exact repo convention is (7−1)/19 = **0.3158**; use 0.3158.
5. **No stun/freeze/death clips exist** in the v31 catalog, although the M2 plan lists a "stun/freeze/death"
   layer. Placeholder strategy in §2; commission clips in a later anim pass if wanted.
6. Minor: the controller does declare `isMoving` (Bool) — the task brief said "bools and floats", confirmed.
   The layer is named `Body` (not `Base Layer` — that string in the YAML is the state machine's default name).

## 14. What I could not verify (honest list)

- **In-game behavior of anything above** — nothing is game-verified by construction (kit-contract rule). The
  HGCloudRemap property names must be confirmed against a vanilla material in the editor (one-time check).
- **The Blender export of the remaining 50 clips** has not been run; `Source/v31_probe04/` doesn't exist yet.
  Whether the `hs_*` curves survive for all clips is moot for the game bundle (stripped) but relevant for previews.
- **Halo ring fit** (r 0.32 m, gap angles) is a placeholder — must be fitted to the v18 halo in the editor.
- **Exact transform paths for the AvatarMasks** were not enumerable from the flat bone list; build the masks in
  the editor from the model hierarchy using the bone names listed in §2.
- **Bundle size** after 65 clips (compression proposal in §6 step 6) is unmeasured until built.
- `KitTuning.cs` values were not read for this spec (out of scope; cooldowns are quoted from approved docs).

## 15. Do-not-touch recap (unchanged)

Unity PID 50188 and Blender PID 4500 belong to Stuart. No Unity launch, no blender-mcp, no writes to
`Library/`, `Temp/`, `Logs/`, `Obj/`, `UserSettings/`, no edits to `.cs/.unity/.prefab/.controller/.asset/.meta`,
no commits, no pushes. This document is the deliverable.