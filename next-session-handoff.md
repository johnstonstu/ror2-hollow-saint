# Hollow Saint handoff: v0.7.14 (2026-09-30)

For Stu's independent audit before playtest, start with `audit-handoff-20260930.md`.
It lists the exact candidate, priority questions, evidence limits and reproduction commands.

## Current: user-requested correction of detached-hand/ring preview

- After the daytime cutoff Stu identified visibly detached hands and a displaced
  ring near the end of `current-flow01/skin3-handoffs.gif`. That image was wrong;
  prior bone/line endpoint checks had not established rendered attachment quality.
- Two preview defects reproduced: repeated Camera.Render calls inside a single
  editor frame reused earlier GPU-skinned body poses while bones/lines moved, and
  the fixture jumped from Open Circuit hold to Empty, skipping the actual buff
  driver's closing clip. With write-defaults off, ring translations stayed unfolded.
  The fixture also incorrectly hid the held spear on dash/recovery without a throw.
- Corrected CurrentFlowCapture uses reusable fresh CPU-baked posed mesh snapshots
  with the current materials/property blocks. CurrentFlow fixture now runs the
  existing Halo-layer Open Circuit end clip to completion and retains held spear
  state/grasp across dash/recovery. **These are preview/harness corrections; no
  production DLL, model or animation change was needed or made in this pass.**
- New current-flow02 has five regenerated 120-frame GIFs and end-frame contact
  sheet, visually reviewed for connected wrists, dash posture, ring close and skin
  colors. Native connected-current recheck: 90 cases / 544,466 assertions pass,
  including held-spear preservation. The sequence remains an explicit route/pose
  study with sustained currents, not gameplay or real skill cadence/full fan VFX.
- Independent Check-EndPose.ps1 renders ordinary SkinnedMeshRenderers across
  separate native editor updates, without snapshots: 120 frames reviewed, ring
  arcs return within 5 mm of authored rest. Skipping the closing clip deliberately
  fails this check (end-pose-baseline). Final passing evidence: end-pose02; diagnostic
  A/B and transform/curve audit: end-pose01. No hand detachment in corrected native
  frame 80/119; the old render-only artifact does not establish an in-game rig defect.
- Retire current-flow01 animated media as attachment evidence; keep it as the
  reported failure. Also regenerate older same-editor-frame animated media before
  using it to judge mesh/bone alignment. Existing numerical source/geometry tests
  remain separate from the faulty rendered media. Latest reviewed media: current-flow02
  and end-pose02. Do not claim actual gameplay/input/bloom/network testing.
- Staged Dev remains v0.7.14, DLL hash and rollback below unchanged. No filler
  version bump, stage write, commit, push or release. Stu's new request authorized
  this focused correction after the earlier 3 PM automation finished.

## Previous: tenth and final daytime refinement pass, cached lighting metadata

- Staged **v0.7.14 | skin flow | cached lighting** in Hollow Saint Dev; local
  manifest/listing 0.7.14 (35 entries). Unchanged bundle13. This finishes today's
  development; do not start another pass at/after 3 PM Pacific. Branch remains
  `codex/spear-channel`, all WIP uncommitted/unstaged, no push/public release.
- DLL SHA256: `6E4971A5DE9EF9A99328FACC1D84D78E8BD2ACB91689713273E1AE747D221A4F`.
  Bundle13: `1AD13A0DF9706A340AE20A0B656AF26F4FACD84048796034FEAC9D3ED57F35BC`.
  Rollback to v0.7.13: with game closed restore BOTH DLL and bundle13 from
  `artifacts/foundation/profile-backup-20260930-144839`.
- FoundationSkinAnimation now caches theme/atlas classifications per active material
  reference. Skin replacement refreshes the cache; emission values remain live each
  tick, and the existing fade envelope, palette, restore and property-block behavior
  are retained. No geometry, authored animation, controls, damage or timing changes.
- Native cache check: 47 lighting targets, 5,000 stable ticks, unchanged active
  material references, and an exact same-phase half-emission change. Directly reading
  Material.name 100 times produced 100 distinct managed string references; caching
  removes repeated classification/name lookups on stable materials. **No measured
  FPS or zero-GC claim**: native Mono's allocation counter reported zero even for a
  known 65,536-byte allocation, so allocation totals are unavailable. The retained
  baseline0713 measurement/verification files were uncalibrated and are invalid as
  allocation evidence. Calibration and final evidence: `artifacts/skin-loop01`.
- With final production source, native recovery/palette checks pass (2,319 assertions /
  21 cases), five-skin atlas/material checks pass (2,335,904 assertions), connected
  current integration passes (90 cases / 544,308 assertions). Native rear route
  captures reviewed; prior VFX, spear, movement, arm and baked-grip checks remain
  applicable because those sources/assets are unchanged. Build: 0 errors, known
  NU1701 warning only. Access, ArmLife/SpearModes/AnimRules/Locomotion and whitespace
  checks pass. Final v0.7.14 real-game version/catalog/five-skin startup smoke passes;
  evidence is in `artifacts/game-smoke0714`. The baseline ProBuilder shader error
  and OS cursor-confinement denial remain, plus the existing JobTempAlloc warning;
  no unexpected/mod errors. Staged DLL
  hash/manifest verified; own test process closed normally. Startup only, no input
  or live gameplay/bloom/network/FPS proof. Dev is ready to launch.
- Today's candidate includes calm movement/arm and finger follow-through, held grip
  through throw/catch markers, softer spear aim recovery and casting-side transfers,
  synchronized ring/core/arm/spear/fan lightning, five-skin body lighting and softer
  model-light recovery. Preserve all previous playtest limits: native adapters are
  not live gameplay; actual feel/input/bloom/physics, performance and multiplayer
  still need playtesting. Seven shared atlases add ~37 MiB GPU storage. The small
  authored forearm-strip skin-weight issue remains.

## Previous v0.7.13: ninth daytime pass, model-light recovery and skin transfers

- Completed the focused 2 PM recovery pass. Staged **v0.7.13 | current flow |
  softer recovery**, unchanged bundle13; Dev listing/manifest 0.7.13 (35 entries).
  Branch `codex/spear-channel`, all WIP uncommitted/unstaged, no push/public release.
  Do not start any new development pass at/after 3 PM Pacific; provide final build
  and playtest limits at the cutoff. The build below is the current tested candidate.
- DLL SHA256: `94C84E4557D2EBC5426873E14D8A7AB9A4155F33BCC0AC47AA3C1AC99C94AA34`.
  Bundle13: `1AD13A0DF9706A340AE20A0B656AF26F4FACD84048796034FEAC9D3ED57F35BC`.
  Rollback to v0.7.12: restore BOTH DLL and bundle13 with game closed from
  `artifacts/foundation/profile-backup-20260930-142413`.
- Native baseline reproduced an immediate +1.6 model-light brightness jump at
  ability onset. FoundationSkinAnimation now ramps its shared current strength at
  full-scale rates of 60 ms in / 90 ms out, retaining the existing travelling beat.
  Smaller current strengths settle proportionally sooner. No added bolt geometry
  or delay to gameplay, line effects, cast timing, damage or accepted controls.
  Model light recovers softly after abrupt channel/crown/dash boundaries; normal
  timed arm windows keep their own already-existing release/cancel tails.
- Flat themed lights blend between their existing quiet idle modulation and ability
  gain; atlas lights return to the authored steady emission. Death, invisibility
  and component disable immediately restore emission and clear transfer history.
  The envelope pauses when dt=0 and continues from its current value on rapid restart.
- Found and fixed stale color in emission property blocks at rest: after a pulse,
  changing the skin could retain the previous material's emission override. The
  component now reads the active defaultMaterial and refreshes emission on every
  target even at rest, preserving other renderer properties. Disable/death restore
  all targets, not just currently animated ones. No material/texture allocations in
  the frame loop; do not infer a measured zero-GC result from that statement.
- New `Check-GlowRecovery.ps1`: **2,319 assertions / 21 cases**. Five skins at
  30/60/144 Hz plus six bodyless mannequin selections. Full-strength onset/recovery
  bounds, rapid restart, paused clock, 0.55 Open Circuit strength, immediate lifecycle
  resets, all 25 skin pairs at rest and active, and other property-block preservation.
  Native model13/materials and exact production skin/current classes with explicit
  replicated-current flags; fixed pulse phase isolates the rate tests, not gameplay.
  Actual measured first-frame boosts: 0.889/0.444/0.185 at 30/60/144 Hz, versus the
  former instantaneous 1.6. Transfer CSV and retained failing baseline/intermediate
  source/logs are in `artifacts/glow-recovery01`. Pre-palette-fix failure uses a
  seeded emission property block to reproduce the retained override, not game footage.
- Rechecked **2,335,904 five-skin atlas/geometry/pulse assertions**, **90 connected
  current cases / 544,308 assertions**, **700 VFX / 96 spear / 192 movement / 144 arm**
  cases. All pass. CurrentFlow fixture/captures now use Tick(now,dt); runtime passes
  Time.time/Time.deltaTime. Five current-flow01 GIFs regenerated; recovery frames
  reviewed. These are native route/lighting studies with sustained currents near
  release/catch markers, not real skill cadence, bloom, physics or networking.
- Bundle13, rig/controller/curves, compact surfaces, molded grip, fingers and all
  previous fluidity work are unchanged. The v0.7.12 grip-flow03 geometry/marker checks
  still apply. Build 0 errors (known NU1701), access, ArmLife/SpearModes/AnimRules/
  Locomotion and whitespace pass. Final v0.7.13 real-game content smoke passes
  version/catalog/five-skin checks; evidence is in game-smoke0713. The log retains
  the baseline ProBuilder shader error and OS cursor-confinement denial, with no
  unexpected/mod errors. Own test process closed normally; Dev is ready to launch.
  This is startup evidence only, not a live body/input/bloom/gameplay test.
- Playtest must still judge busy fight transitions, motion feel, actual bloom,
  live body spawn/input, performance, collision/physics and multiplayer. Native
  checks use explicit adapters. Preserve prior limits: seven shared atlases add
  ~37 MiB GPU storage; the small authored forearm-strip skin-weight issue was not
  changed by these passes. Do not claim an in-game visual/gameplay test was performed.

## Previous v0.7.12: eighth daytime pass, skin-colored body light

- Completed the 1 PM focused skin/light pass. Staged **v0.7.12 | skin current |
  unified glow**, bundle13, Dev manifest/listing 0.7.12 (35 entries). Branch remains
  `codex/spear-channel`, all source/art WIP uncommitted/unstaged and preserved.
  No push/public release. Continue hourly until 3 PM Pacific; stop new passes then.
- Final DLL SHA256: `60F62F7BCD383CA3AE42354BAC58C7BB7F559AFFC892F248AC662BA72CEB5245`.
  Bundle13: `1AD13A0DF9706A340AE20A0B656AF26F4FACD84048796034FEAC9D3ED57F35BC`.
  **Rollback to v0.7.11: restore BOTH DLL and bundle12 from
  `artifacts/foundation/profile-backup-20260930-133057` with game closed.** Later
  133543/133859 backups contain intermediate v0.7.12 audit fixes, not v0.7.11.
- Audit disproved the suspected V11-name issue: all newer flat conductors/core/gap
  lights already recolored correctly. Remaining cyan was baked into body_base and
  body_emission atlases. Original bundle12 material TSVs and actual game material
  application IL are retained in `artifacts/skin-light01`.
- FoundationLightMaps loads six offline skin diffuse atlases plus one neutral
  emission map from bundle13. Only authored cyan light pixels inside the emission
  mask get the arc palette. Other pixels retain the existing relic armor tint,
  baked in linear light; surface metallic/smoothness tuning remains. Neutral map
  preserves the original mask's peak brightness/coverage. Default and Obsidian
  retain their source cyan maps; posterior graphite's existing game tuning remains.
  No runtime readback/recoloring/texture allocations. Seven 1024px RGBA atlases with
  mipmaps add about 37 MiB of shared GPU texture storage; no CPU-readable copies.
- Bundle13 compacts the four body submeshes into single-material renderers, shared
  by body and selection display. **The playable body already split these at runtime
  in FoundationMeshSplitter; that was not an unresolved gameplay material-slot bug.**
  The old display did not split. New aggregate skinning vertices are 8,767 versus
  33,832 in the old four full-mesh runtime copies (~74% fewer); original source mesh
  had 8,458 vertices. Same 28,224 indices, draw surfaces, bone weights/bindposes,
  normals/tangents/UVs, rig paths, source renderer settings and animation controller.
  No measured FPS claim. Body still enables offscreen updates on these four parts.
- FoundationSkinAnimation now includes emissive atlas surfaces in ability current
  pulses, using the existing LightningRhythm; restores their authored emission on
  idle/disable, preserving other property-block writers. Existing flat-light idle
  modulation remains. Tick(now) is a native clock seam; runtime passes Time.time.
  Attack controls, timing, damage/balance, fitted grip and bundle12 finger clips stay.
- `Check-SkinLight.ps1`: **2,335,904 assertions / five skins** on actual bundle13:
  geometry/attributes, compact surface layout, all original transforms/controller,
  emission coverage/neutrality, armor isolation, diffuse/emit colors, exact ability
  pulse and idle reset, other property-block preservation, no readable texture copies.
  Ten front/rear native Standard renders in skin-light01 reviewed. Uses exact game
  material tuning/tints plus explicit CharacterModel/current adapters; not game bloom.
- Rechecked **700 VFX / 96 spear / 192 movement / 144 arm** cases on model13, plus
  **90 connected-current cases / 544,308 assertions** using actual game tuning and
  skin-light animation. Five current-flow01 GIFs/rear snapshots regenerated/reviewed.
  These sustain currents near release/catch markers; not actual skill cadence or
  full fan/crown/game rendering. Grip-flow03: **240 baked poses**, zero intersections,
  1.209 mm minimum sampled clearance; **3,630 checks / 242 finger marker poses** pass.
  The older controller/finger tests cover unchanged sources, not a new game playtest.
- Build 0 errors (known NU1701), access, ArmLife/SpearModes/AnimRules/Locomotion and
  whitespace pass. Final real-game startup **v0.7.12 catalog and five-skin checks
  pass**, body/display both have 140 complete single-material surfaces. Log has the
  baseline ProBuilder shader error (key 86212504c7e9f468db2300dc5932dc17) and an OS
  cursor-confinement permission error (`ClipCursor failed: Access is denied`).
  No unexpected/mod errors in the final startup. Do not describe the log as clean.
  Game test process closed normally. Two earlier startup failures from stale body
  part naming in the audit were corrected and preserved in game-smoke0712; final
  smoke passes. Avatar, 23 mounts, renderer coverage and skin order also pass.
- Still no actual gameplay input/body spawn/bloom/physics/network review. User's
  playtest must check all-skin selection changes, body glow during active/recovery
  transitions, real busy fights, sprint/jump/dash/grip and memory/performance feel.
  Next focused pass should favor final integration/recovery checks before the 3 PM
  cutoff; do not change accepted controls/balance or claim native previews are game.

## Previous v0.7.11: seventh daytime pass, connected-current transfers

- At the noon heartbeat, completed the focused body-current handoff pass.
  Staged **v0.7.11 | connected current | softer transfers**; Dev listing/manifest
  and DLL/bundle hashes match (35 entries). Bundle12 unchanged. Branch remains
  `codex/spear-channel`, source uncommitted/unstaged, all prior WIP retained.
  No push/public release. Continue hourly through 3 PM Pacific, then stop passes.
- DLL SHA256: `9669A1E2BAEE36407120D42B0F078F3CBD5A3E756FD92961B6FBFDDBA9114C38`.
  Bundle12: `97CC61AF4C042CDD66327E30E4E65A9F4D27022536B199A5395910A2A4DDD672`.
  Rollback to v0.7.10: BOTH DLL and bundle12 from
  `artifacts/foundation/profile-backup-20260930-123117`, with game closed.
- Baseline native test reproduced a 0.36 rig-unit lateral snap in the core feed
  when the stronger current switched from the left to right arm. Baseline failing
  report/source retained under `artifacts/current-flow01/baseline-*`. BodyCurrentFx
  now eases only that body-relative lateral offset at 18/sec (~56 ms time constant),
  while source, spine/ring and arm endpoints follow the final pose directly. It
  settles to the new casting side in ~0.2 s without lagging behind world movement.
  Routing history resets when hidden/idle/dead/disabled or the model is replaced.
  No added lines/material allocations, changes to skill timing, controls or balance.
- BodyCurrentFx has an internal Tick(now,dt) native-validation seam. Runtime
  LateUpdate passes the same Time.time/Time.deltaTime; current-window sampling,
  body pulse phase and line ticks consume those arguments. Carry.CurrentWeight
  retains its existing game clock; the runner explicitly adapts channel/buff flags,
  rather than claiming to execute the real spear states or their return-flight timing.
- New `Check-CurrentFlow.ps1`: **544,308 assertions / 90 cases**, all five palettes
  at 30/60/144 Hz. Fifteen isolated side handoffs plus 75 sequences through left/
  right casts, held fan, catch-current, Open Circuit, dash and idle while standing,
  running, strafing, jumping and sprinting. Runs the native model12/controller and
  production MotionPose -> ArmPose -> BodyCurrent layers with explicit state/motor
  adapters. Checks final-pose/core/ring joins, translated/turning anchors, bounded
  body-space handoffs, total reusable 26-line budget, palette isolation, rendered
  endpoint pinning and idle/death/invisibility cleanup. The measured 144 Hz step
  fell from 0.36 to ~0.042 rig units (~88% smaller). CSV/log/report in current-flow01.
- Five native 120-frame studies `skin0-handoffs.gif` through `skin4-handoffs.gif`
  and rear snapshots in `artifacts/current-flow01` were reviewed. These isolate the
  body route and sustain cast currents near release/arrival markers for complete
  connection inspection; they do not reproduce gameplay cadence, projectile flight,
  full fan/crown components, physics/networking or game bloom. Proper held-spear
  masks and animated crown geometry are used. Pack with Pack-SpearFlowPreview.py.
- Re-ran the existing **700 VFX / 96 spear / 192 movement / 144 arm** native cases:
  all pass. Build 0 errors (known NU1701), access, ArmLife, SpearModes, AnimRules,
  Locomotion and whitespace checks pass. The prior 63-controller and 234-clip/
  24-finger-transfer checks still cover their unchanged production sources.
- v0.7.11 has not been launched in game. Last actual startup remains v0.7.8,
  catalog/five-skin pass with the baseline ProBuilder shader error. Gameplay feel,
  busy state timing, actual bloom/physics/networking still need the user's playtest.
- Next useful focused pass: audit new model hand/arm light materials across skins.
  Solar review shows some limb light strips still cyan while current lines are gold.
  FoundationSkin.RelicTint and FoundationSkinAnimation recognize conductor/core/gap
  names, which may miss the newer V11 emitter material names. Confirm actual bundle
  material names/emission first; do not assume the AssetDatabase preview alone proves
  the in-game issue. Preserve the base/Obsidian cyan and non-emissive plates/cloth.

## Previous v0.7.10: sixth daytime refinement pass, free-finger gesture handoffs

- At the 11 AM heartbeat, completed the focused finger-follow-through pass.
  Staged **v0.7.10 | finger flow | softer handoffs**; Dev listing/manifest match
  (35 entries). Bundle12 unchanged. Branch `codex/spear-channel`, source still
  uncommitted/unstaged, all earlier WIP preserved. No push/public release.
  Hourly refinement continues through 3 PM Pacific; stop starting passes then.
- DLL SHA256: `91C7ABF7731895DF0327F48A2869518803891C0D9E200C31CBAE913CCB4399AE`.
  Bundle12: `97CC61AF4C042CDD66327E30E4E65A9F4D27022536B199A5395910A2A4DDD672`.
  Rollback to v0.7.9: restore BOTH DLL and bundle12 with game closed from
  `artifacts/foundation/profile-backup-20260930-113413`.
- Native audit found no curl-sign reversals across 234 clip sequences / 18,222
  frames. The actual issue was a small change in the joint-local additive curl
  direction at an authored hand-pose transition. At 144 Hz, first-step offsets
  reached about 0.95 degrees at normal intensity (1.91 at maximum), independent
  of the already-existing 80 ms cast suppression fade. Do not describe that
  existing fade as an immediate cut, or claim a curl-sign reversal was fixed.
- FoundationArmPose now follows each finger joint's small local life offset
  with an exponential 32/sec response, roughly a 31 ms time constant. Authored
  finger curves and attack/release timing are untouched. Right-hand life offsets
  clear immediately while Gripping, preserving the molded grasp, and clear on
  disable/death/teleport/reset or after arm life fades off. The existing 0.2 sec
  finger activation after release and 80/200 ms casting fades remain.
- New `Check-FingerFlow.ps1`: **60,587 assertions / 234 clip sequences / 24
  handoff cases**, 30/60/144 Hz, normal/max intensity. Finite/restored finger
  transforms, no settled curl reversals, correct casting side/fade/recovery and
  bounded handoff steps pass. At 144 Hz the default maximum step is ~0.19 degrees,
  about 80% below baseline; maximum intensity ~0.38. The test's 50 deg/sec per
  intensity bound failed against the pre-change source, retained with CSV/source
  evidence in `artifacts/finger-flow01/baseline-*`. Precise atan2 angle measurement
  avoids Quaternion.Angle rounding small steps to zero. These are native fixture
  measurements with fixed seeds and explicit game adapters, not gameplay proof.
- Actual native hand closeups rendered in `artifacts/finger-flow01/free-fingers/`
  and assembled into `free-fingers.gif`; sample attack/hold/recovery frames reviewed.
  Neutral materials and a camera following the left hand expose geometry and tiny
  motion; this is an inspection study, not skin/bloom/game footage.
- All earlier **700 VFX / 96 spear / 192 movement / 144 arm / 63 controller**
  native cases pass after the change. Build 0 errors (known NU1701), access,
  ArmLife, SpearModes, AnimRules, Locomotion and whitespace checks pass. Held-grip
  and socket locals are protected in the whole-pipeline runner; bundle12's actual
  geometry/markers are unchanged and retain v0.7.8 verification.
- v0.7.10 has not been launched in game. Latest actual startup evidence remains
  v0.7.8 (catalog/five-skin pass, baseline ProBuilder shader error). Controls,
  damage/balance and VFX have not changed in this pass. Animation feel, busy real
  transitions, physics/networking and actual bloom still need the user's playtest.
- Next useful focused pass: connected lightning visibility/fades during controller-
  driven attack/recall/sprint/dash interruptions. Keep explicit skill-state adapters
  separate from claims about real state-machine execution, damage or networking.

## Previous v0.7.9: fifth daytime refinement pass, actual presentation state controller

- At the 10 AM heartbeat, completed the focused locomotion/state-controller pass.
  Staged **v0.7.9 | state flow | motion recovery**; Dev listing/manifest match.
  Bundle12 unchanged. Branch `codex/spear-channel`, all source uncommitted/unstaged,
  earlier WIP retained. No push/public release. Continue hourly through 3 PM Pacific.
- DLL SHA256: `5EC552D79D044E00C9C9D2091D00E9A171F06A36C69954D30941118FA6B5FE67`.
  Bundle12: `97CC61AF4C042CDD66327E30E4E65A9F4D27022536B199A5395910A2A4DDD672`.
  Rollback to v0.7.8: BOTH DLL and bundle12 from
  `artifacts/foundation/profile-backup-20260930-102516`, game closed.
- Ordinary transitions passed before changes. A native invalid-motor test then
  reproduced an actual controller defect: a NaN velocity poisoned the world-space
  smoother and `gaitBlend` Animator parameter. Baseline failure/report retained in
  `artifacts/presentation-flow01/invalid-motion-baseline*`. FoundationPresentation
  now rejects NaN/Infinity/overflowed velocity magnitude before touching the smoother
  or choosing movement phases. It keeps the last valid movement state, logs once per
  continuous bad span, and resumes on valid input. Normal fades, controls, timings,
  movement speed, damage/balance, model/animation assets and VFX are unchanged.
- New `Check-PresentationFlow.ps1` and `PresentationFlowValidation.cs` run production
  controller decisions against the native model12 Animator with an explicit frame
  clock at 30/60/144 Hz. **293,566 assertions / 63 cases** pass: eight travel/reversal
  headings, stop recovery, short ground-gap debounce, jump/double-jump restart,
  landing compression, sprint recovery/loop sound stop, four directional dash exits
  while ascending/falling, invalid samples (including multi-frame warning bounds),
  Arc Bolt/Open Circuit arm-layer migration without restarting/losing gesture time.
- Harness details/limits: Prepare-FxValidation copies 21 exact production sources,
  actual MoveGesture method, tint methods and a mechanically derived presentation
  class. Presentation differs ONLY in class name (`LivePresentation`, to coexist with
  prior pose fixtures) and Time.deltaTime -> PreviewFrame.DeltaTime. Actual native
  Animator state graph runs; motor/skill flags/sounds are explicit game adapters.
  The controller's alive/grounded/glide outputs bridge into the older three-field
  pose adapter before actual MotionPose -> ArmPose -> Carry/Aim ticks. This checks
  state decisions and handoffs, not game input, physics/network or real audio.
- Native `artifacts/presentation-flow01/sprint-jump-dash.gif` is a 120-frame rooted
  pose study driven by the controller and procedural layers, reviewed at jump/dash/
  landing. It is not gameplay footage. Pack via Pack-SpearFlowPreview.py with root
  artifacts/presentation-flow01 and name sprint-jump-dash. Reports/logs same folder.
- Existing **700 VFX / 96 spear / 192 movement / 144 arm** cases all pass after the
  harness expansion. Build 0 errors (known NU1701), access, Locomotion, AnimRules,
  SpearModes and diff whitespace checks pass. Grip geometry/markers remain verified
  by the unchanged bundle12's v0.7.8 checks; no unnecessary geometry rerun.
- v0.7.9 has not been launched in game. Latest actual startup evidence remains
  v0.7.8 (catalog/5-skin pass, baseline ProBuilder shader error); no gameplay or real
  network verification. Current PLAYTEST.md top includes the broader movement/skill
  handoffs. Source invalid-velocity recovery is now native-tested, not observed from
  a real game motor failure. Avoid presenting it as a change to normal animation feel.
- Next useful focused pass: inspect free-finger curl-axis continuity across gestures,
  or connected lightning visibility/fade during actual controller-driven interruptions.
  Current new runner doesn't execute real gameplay skill state machines, damage or
  network physics; do not treat its adapters as those proofs. No curl-flip/network
  defect established. Stop starting development passes at 3 PM Pacific.

## Previous v0.7.8: fourth daytime refinement pass, throw/catch grip timing

- At the 9 AM heartbeat, completed a focused hand/grip geometry pass. Staged
  **v0.7.8 | grip timing | spear flow**, new **bundle12**; Dev listing/manifest match.
  Branch `codex/spear-channel`, source uncommitted/unstaged, preexisting WIP retained.
  No push/public release. Hourly continuation stays active through 3 PM Pacific today.
- DLL SHA256: `29CF773024B75039EF68B075C73C68E443EE6ABD82A06069D97DE981666ECA5B`.
  Bundle12: `97CC61AF4C042CDD66327E30E4E65A9F4D27022536B199A5395910A2A4DDD672`.
  Rollback to v0.7.7: restore BOTH DLL and bundle11 from
  `artifacts/foundation/profile-backup-20260930-093051` with game closed.
- Actual baked bundle11 surfaces showed **32 intersecting poses / 240 samples**:
  fingers opening before throw release, and index-tip interpolation overshoot just
  after catch arrival. Held/fan/crown samples cleared. This was imported animation
  interpolation, not procedural wrist deformation; fixed finger/socket locals alone
  had missed it. Baseline evidence retained under `artifacts/grip-flow01`.
- `FoundationBundleBuilder12` keeps the bundle11 model, fitted spear, masks and state
  graph through an AnimatorOverrideController. Only 15 right-finger quaternion tracks
  in Conduit Spear and Spear catch are revised: exact grasp through release (6/19),
  exact grasp after arrival (13/19), zero boundary tangents. Later release/open-hand
  motion and earlier reach/closing remain. Other curves are checked key-for-key;
  duration, release/arrival timing, gameplay controls, damage and balance are retained.
  Blender v37 and bundle11 stay immutable; future reimports must retain this native
  curve correction. New assets: `GameFoundation12`, bundle12 build log/report.
- `Check-GripFlow.ps1 -Bundle 12`: actual built model and spear instantiated in native
  Unity, production MotionPose -> ArmPose -> Carry/Aim layers, 8 movement clips x
  held/fan/crown/throw/catch x 3 phases x default/max arm-life settings. All **240
  baked poses**, each 27 hand parts, pass independent Blender BVH triangle-surface
  intersection checks; minimum sampled grip-vertex clearance **1.209 mm**. Additional
  **3,630 finger checks / 242 dense poses** protect pre-release/post-arrival grasp.
  This is sampled surface evidence, not all possible pose blends or gameplay.
- `artifacts/grip-flow02`: final exports, surface-check.json, verification.txt,
  marker-grasp.txt, native logs and six `inspection-{held,throw,catch}-view*.png`
  closeups. Inspection images use neutral materials to expose geometry; they are
  not gameplay color/bloom references. Early dark preview images are diagnostic.
- Existing native **700 VFX / 96 spear / 192 movement / 144 arm** cases all pass
  against model12. `NativeRig` selects model12 plus the unchanged fitted spear11;
  ArmFlow fixture accepts built prefabs for independent geometry checks. A short
  free-hand amplitude assertion originally varied with Unity instance-ID Perlin
  seeds; preview wrist seeds are now fixed for reproducibility. Production variation
  and arm-life amplitudes are unchanged. Build 0 errors (existing NU1701), access,
  ArmLife, SpearModes, AnimRules and diff whitespace checks pass.
- **Actual v0.7.8 startup smoke** passed: loaded version, catalog and 5-skin FX checks;
  game launched and closed normally. Logs under `artifacts/game-smoke078`. One error
  severity line is the same preexisting ProBuilder shader Addressables key as baseline
  (`86212504c7e9f468db2300dc5932dc17`); do not describe the whole game log as clean.
  No gameplay/input/visual/network verification. Current playtest: PLAYTEST.md top.
- Next useful focused pass: integrate/test actual FoundationPresentation locomotion
  and skill transitions rather than supplying presentation fields in fixtures; or
  inspect free-finger curl-axis continuity across gestures. No curl flip/network
  defect is established. Do not spend passes on filler bumps or repeat identical
  tests without a change/failure. Stop starting passes at 3 PM Pacific.

## Previous v0.7.7: third daytime refinement pass, free-finger release flow

- At the 8 AM heartbeat, continued authorized work toward 3 PM Pacific. Staged
  **v0.7.7 | finger flow | arm recovery** in Hollow Saint Dev; listing/manifest match.
  Branch `codex/spear-channel`, source uncommitted/unstaged, earlier WIP retained.
  No push/public release. Hourly continuation remains active through 3 PM today.
- DLL SHA256: `4E904247C22D4979DB76B4DE9D258BD8315AE2626FDE4A8287235412A34C4E5B`.
  Bundle11 unchanged: `A11AC094CC1AA4EFADFEBAA61057C4556EEFDE379EDFD049B96899F41D2CC904`.
  Rollback to v0.7.6: BOTH DLL/bundle from `profile-backup-20260930-081920`.
- `FoundationArmPose` keeps right-finger life fully off during the fitted grasp.
  Previously it enabled finger noise/follow-through instantly when Gripping became
  false; now it eases that layer in over 0.2 s after opening, using a smoothstep.
  Regrip immediately protects the authored fitted fingers. Wrist/arm/finger spring
  constants, animations, controls, damage and balance are unchanged.
- Teleport/nonfinite velocity now clears both reactive spring chains, not just
  cached motion samples, preserving cast suppression. Bad frame time is ignored.
  This avoids retaining pre-snap energy and poisoning filtered velocity. Arm updates
  expose explicit dt/time for meaningful native tests; runtime still passes Unity's
  ordinary time. SpearCarry.ApplyAim accepts the same dt/time through an overload.
- Build 0 errors (existing NU1701), access check, ArmLife, SpearModes and AnimRules pass.
  Native **39,514 assertions / 144 full-pipeline cases** pass: actual
  FoundationMotionPose -> FoundationArmPose -> SpearCarry aiming, eight movement
  clips x held/free/left/right/fan/crown x 30/60/144 Hz. Includes held finger/socket
  protection, free-hand movement, correct cast mask, complete skeleton restoration,
  gradual finger activation, option-off/disable, remote-position sampling and
  teleport/nonfinite-input cleanup. Existing **700 VFX / 96 spear / 192 motion**
  cases also pass. These are state-adapter checks, not actual networking.
- Harness now copies **19 exact production sources**, adding FoundationArmPose and
  FoundationArmLifeMath. New `FxValidation/Editor/ArmFlowValidation.cs`; report and
  full-pipeline motion study under `artifacts/arm-flow01`. Latest Unity log remains
  `artifacts/foundation/vfx-flow-check-unity.log`. Check-VfxFlow reads all four reports.
  Pack study via `Pack-SpearFlowPreview.py --root artifacts/arm-flow01 --names free-hand`.
  Native study includes the actual procedural layers and fitted spear, with explicit
  presentation/motor/state inputs. It does not prove skinned hand/grip collision
  clearance or actual network/animation-state-machine behavior.
- v0.7.7 was not launched in game. Latest actual-game evidence is the v0.7.5 startup
  smoke (below), not gameplay. Current playtest checklist is PLAYTEST.md top.
- Next useful work: integrate/test the actual FoundationPresentation state controller
  rather than driving native pose fixtures directly, or investigate pose-to-pose
  free-finger curl-axis continuity. Existing curl-sign estimation uses animated bend
  hinges and may warrant inspection across gestures; no flip defect is established.
  Also assess actual skinned grip geometry under procedural wrist movement before
  assuming unchanged finger/socket locals alone prove clearance. Keep each pass
  focused and stage only a concrete verified improvement; no filler version bumps.

## Previous v0.7.6: second daytime pass, movement follow-through

- Continued authorized daytime work at the 7 AM heartbeat. **v0.7.6 | movement flow |
  dash handoff** is staged in Hollow Saint Dev; listing/manifest updated. Same branch
  `codex/spear-channel`, uncommitted/unstaged source; preserve other WIP. No public release/push.
  Hourly continuation `hollow-saint-refinement-today` remains active through 3 PM Pacific today.
- DLL SHA256: `99024D21FE52541ADB74EB35E9B82F2C511457D0D8463D1F0599530DB47AE75E`.
  Bundle11 unchanged: `A11AC094CC1AA4EFADFEBAA61057C4556EEFDE379EDFD049B96899F41D2CC904`.
  Rollback to v0.7.5: BOTH DLL/bundle from `profile-backup-20260930-072556`.
- `FoundationMotionPose` previously accented sprint glide only. It now adds small
  world-direction torso acceleration/braking lean and facing-turn roll during normal
  movement, reduced in air and to 45% during gestures/fan/throw/catch. Spine moves,
  head counterbalances; ordinary ground pelvis/feet and all finger locals are untouched.
  Cached animator/layers/carry avoid new per-frame allocations.
- Additive glide weight now hands back over a bounded 80 ms tail when presentation
  drops it immediately on dash entry. Existing authored clips and game movement remain.
  Finite/hitch/teleport guards reset posture. Save captures each bone only once per
  frame, so torso-plus-glide cannot restore an already modified rotation and drift.
- Fixed a real state-boundary issue: airborne dash exit unconditionally selected
  Descend, even while rising, then corrected the following frame. Shared
  `FoundationAnimRules.IsAscending` now drives dash exit and normal airborne loops.
  Grounded dash end, jump logic, controls/damage/balance are otherwise retained.
- Build 0 errors (existing NU1701), access check, AnimRules (extended dash-loop checks),
  Locomotion and ArmLife pass. Native validation now copies **17 exact production
  sources**, including FoundationMotionPose; game motor/presentation/KitAnim are
  explicit adapters. Native **195,431 assertions / 192 motion cases** pass (eight
  movement directions x ground/air/glide x casting/free x 15/30/60/144 Hz), plus the
  retained **700 VFX / 96 spear cases**. Includes feet/root/finger preservation,
  all-bone restore/settle, reduced cast reaction, glide/dash handoff and cleanup.
- `FxValidation/Editor/MotionFlowValidation.cs` is the new runner. Reports/previews
  under `artifacts/motion-flow01`; latest full Unity log remains
  `artifacts/foundation/vfx-flow-check-unity.log`. `Check-VfxFlow.ps1` now checks all
  three reports. Camera studies advance the actual Animator through run/stop/idle
  and glide/dash/fall crossfades with the fitted spear, but use explicit state inputs.
  They are not gameplay footage or networking/full-presentation-state integration.
  GIF assembly: `Pack-SpearFlowPreview.py --root artifacts/motion-flow01 --names turn-stop glide-dash`.
- v0.7.6 was not launched in game; the prior v0.7.5 startup smoke below remains the
  latest actual-game evidence. Whole-body spear collisions, look/feel, bloom and
  multiplayer interpolation need playtesting. Current checklist is PLAYTEST.md top.
- Next useful pass: native integration of the actual arm-life layer with movement,
  held/free hands and skill transitions; inspect free-hand/finger follow-through,
  cast suppression/recovery and restore ordering. Start by reading current code;
  do not change spring constants just to fill time. Remote motor-vs-position sampling
  remains a known investigation candidate, not an established multiplayer defect.

## Previous v0.7.5: first daytime pass, spear aim/recovery flow

- Stu authorized continued development while away until **September 30, 2026,
  3 PM America/Los_Angeles**, including game testing. Keep the calm/floaty movement,
  soft trailing arms/fingers and connected body-powered lightning direction. No
  public release/push. Local builds/staging/game testing are authorized.
- Hourly thread heartbeat **`hollow-saint-refinement-today`** is active through
  `20260930T220000Z` (3 PM Pacific). Continue focused meaningful passes; finish/check
  each before the next. Do not pad time with repeated checks or speculative rewrites.
  At 3 PM stop starting passes and give the final staged build and evidence. No subagents.
- Branch `codex/spear-channel`, uncommitted/unstaged source and earlier WIP retained.
  Staged **v0.7.5 | spear flow | aim recovery**, profile manifest/listing updated.
  DLL SHA256: `49F9FCEF7DC0AB0A7DCB2E0F0C3C648C210EF3E975C9E26B2450780EBDB9D005`.
  Bundle11 unchanged: `A11AC094CC1AA4EFADFEBAA61057C4556EEFDE379EDFD049B96899F41D2CC904`.
  Rollback to v0.7.4: BOTH DLL/bundle from `profile-backup-20260930-065938`.
- Fixed a concrete pose snap: `SpearCarry.ApplyAim` previously dropped its aiming
  rotation to zero on fan exit, even while the authored end clip played. New
  `SpearAimPose` keeps the whole-arm offset in the parent's frame, filters channel
  tracking, blends warmup from the prior offset and eases back over 0.25 s.
  Rapid rechannel/throw inherits the preceding offset. Throw aiming stays exact at
  the existing release marker, then fades so the authored follow-through regains
  the arm. Socket/finger locals are untouched. Reset on model replacement/clear;
  repeated recovery/invalid inputs cannot restart an endless tail. No balance,
  controls, release timing or baked animation changes.
- Release build 0 errors (existing NU1701). Access check, SpearModes, ArmLife and
  AnimRules rerun/pass. Existing native VFX checks: 15,629 assertions / 700 cases.
  New actual production carry/aim/mode test: **21,344 assertions / 96 posed cases**
  (8 movement clips x 3 aim elevations x 4 facings). Tests exact release, smooth
  fan-off/restart, completed carry recovery, no local socket/finger changes and
  invalid-input cleanup, plus 15/30/60/144 Hz helper contracts.
- Native harness now copies **16 exact production sources**, including SpearCarry,
  SpearAimPose and SpearMode; removed its old fake carry. Explicit game/KitAnim
  adapters remain, so network/actual game skill-state integration is not simulated.
  Repro `tools/tests/Check-VfxFlow.ps1`; it reads both validation reports. New native
  runner `FxValidation/Editor/SpearFlowValidation.cs`. Report and 120-frame standing/
  running motion captures: `artifacts/spear-flow01`. GIF assembly helper:
  `tools/tests/Pack-SpearFlowPreview.py` using bundled Python/Pillow. The spear hides
  after the throw release; these are pose studies, with no flight/return simulated.
- Authorized actual-game **startup smoke** completed using `Start-Foundation.ps1`.
  PID 27096 was closed normally with CloseMainWindow and exited; no kill/input/control
  automation. v0.7.5 loaded, kit/catalog checks and skin/material checks passed.
  `artifacts/game-smoke075/{prelaunch.log,startup.log,verification.txt}` preserve evidence.
  One Hidden/ProBuilder/EdgePicker InvalidKey shader error is also in prelaunch.log;
  debug lines mentioning Exception hook names are not failures. Do not claim a clean
  whole-game log or gameplay testing. Native computer-control surfaces are unavailable
  in this session; the game was only launched and its log observed.
- Next useful pass: inspect directional sprint/air/dash posture transitions and
  turn/stop follow-through on the actual rig. `FoundationMotionPose` accents glide
  only; compare it with `FoundationPresentation` state boundaries and arm sampling
  before deciding what to change. Remote posture currently reads motor velocity while
  arm life differentiates interpolated position; assess with evidence before fixing.
  Keep gameplay numbers/controls intact. Save native motion evidence and stage only
  verified changes. Current playtest instructions begin at `PLAYTEST.md` top.

## Previous v0.7.4: lightning flow and skin pass

- Branch: `codex/spear-channel`. Source remains uncommitted and unstaged; preserve
  preexisting Unity/animation WIP. No push, public release or game launch.
- Staged in **Hollow Saint Dev**: `v0.7.4 | VFX flow | skin colors`. Profile listing
  and manifest updated successfully after r2modman closed. The animation/model bundle
  remains **bundle11** with the v37 clips and fitted grip; no new animation bake.
- DLL SHA256: `514A569F9F59BD77A3357F3173B42E5293410FD782FCB5DB5F51D7B1367EDF4D`.
  Bundle SHA256: `A11AC094CC1AA4EFADFEBAA61057C4556EEFDE379EDFD049B96899F41D2CC904`.
- Rollback to final v0.7.3: restore BOTH DLL and bundle11 from
  `artifacts/foundation/profile-backup-20260929-225855`, with the game closed.
  Original v0.7.2 DLL/bundle09 backup `profile-backup-20260929-214127` remains.
- User requested richer connected crackle, a shared body/ability rhythm, all five
  skin palettes, and movement/state transition polish. Existing spear mechanics,
  damage, proc coefficients and balance settings are retained.
- `LightningRhythm` provides a shared 0.48 s pulse propagating at 8 m/s through
  core/ring, anatomical arm route, held spear and fan. Crown, core light/particles
  and existing conductor surfaces respond to the same activity rhythm. Idle body
  current stays off. Existing movement/arm-life animation blending is retained.
- `LightningLine` smoothly follows crackle targets, caches palette changes, bounds
  segments and hides collapsed/invalid/zero-width geometry. Cached lines retint on
  skin changes. Fan roots are narrower to avoid additive white wash, easing in over
  60 ms and fading out over 90 ms after damage stops; dead/invisible/disabled clears
  immediately. Dash afterimages stop when the dash is interrupted.
- `DischargeLink` briefly connects the moving final hand pose to Arc Bolt/spear
  release for 0.1 s, with forks and a bounded/world-clipped path. Projectile direction
  and cosmetic release now share the original `ReleaseRay` calculation. Recall
  anchors to the moving fitted grip; Circuit Arc still picks up the live ring.
- `VictimFxTheme` registers independent hidden replicated cosmetic color snapshots
  for Static and marks. Only the server writes; last contributor wins independently,
  and expired statuses clear the snapshots. Static/mark VFX use those palettes.
  Display effects resolve skin materials when no CharacterBody exists. All five
  themes use the existing canonical palettes (Default/Obsidian cyan, green/amber/violet).
- Release build passes with 0 errors and the existing NU1701 warning. Access check
  and all nine offline tests pass: AbilityCurrent, AnimRules, ArmCurrentPath,
  Locomotion, HaloRing, ArmLife, ThunderboltFlight, StormChargeSequence, SpearModes.
- New isolated native Unity validation in `Assets/HollowSaint/FxValidation` uses
  13 exact production source copies, explicit game-state adapters and an approximate
  additive preview shader. Generated copies are gitignored; prepare via
  `tools/tests/Prepare-FxValidation.ps1`. Repro entry point:
  `tools/tests/Check-VfxFlow.ps1` (Unity caches require elevated access here).
  The underlying native batch method passed: **15,629 assertions / 700 posed cases**.
  Report: `artifacts/vfx-flow01/verification.txt`; latest log:
  `artifacts/foundation/vfx-flow07-unity.log`. Fifty rendered previews are under
  `artifacts/vfx-flow01`, covering all skins and Arc/fan/throw/crown/dash on dark/bright.
  These are native Unity previews, not game footage; actual bloom, networking,
  performance and game feel are not verified.
- Native checks cover palette caching/isolation, replicated color semantics,
  rhythm phase/propagation, moving/degenerate line geometry, actual bundle11 poses,
  body/fan/crown/held-spear interactions, fan fade and dead/invisible/disabled cleanup.
  Explicit pose assertions prevent passing on a frozen rig.
- Batch preview exposed Animator culling on hidden models. Game runtime already
  sets AlwaysAnimate in `FoundationPresentation.Start`; validation now does too.
  Reran `VerifyBundle10.RunFinalBatch` and `VerifySpearMesh10.RunFinalBatch` with
  animation active. Clip/mask/layer/seam preservation passes; **375 fixed-finger
  checks pass**, all 27 hand pieces have **zero grip intersections**, minimum sampled
  vertex clearance **1.209481 mm**. Latest geometry/report:
  `spear11-animated-imported-geometry.json` / `spear11-animated-imported-grip-check.json`
  under `artifacts/foundation`. Repro Blender check: `check_imported_grip.py -- 11-animated`.
  Older 1.248 mm report below was before forcing hidden-model animation and is historical.
- `PLAYTEST.md` begins with the current checklist. Stu handles the game playtest;
  next work should follow that feedback rather than assuming the previews prove game feel.

## Previous v0.7.3 checkpoint: fitted held spear and fan

- Branch: `codex/spear-channel`. Local source changes remain uncommitted; preserve
  older Unity/animation WIP. No push, public release or game launch.
- Staged in **Hollow Saint Dev**: `v0.7.3 | held-spear | lightning-fan`, DLL paired
  with **bundle11**. DLL SHA256:
  `F4F66EC61E689CA3B97A560DFE561DBC66017356E9D761D25BB8A0B3671EE0FF`.
  Bundle SHA256: `A11AC094CC1AA4EFADFEBAA61057C4556EEFDE379EDFD049B96899F41D2CC904`.
- Rollback: restore BOTH DLL and bundle09 from
  `artifacts/foundation/profile-backup-20260929-214127`, with the game closed.
- Starts with a held spear. Hold primary for the continuous Static/control fan.
  Throwing changes primary to the retained ranged Arc Bolt. Secondary while out
  recalls for free, even with zero stock, preserving recharge progress; it stores
  in the hand. A separate next press requires and spends a ready throw charge.
- Provisional fan: 8 m, full 60 degrees, 5 distinct visible victims, 120% damage
  and 35% Static/second at 1x attack speed, 0.1 proc/tick. All configurable under
  `2. Conduit Spear`. Server-only ticks, health deduplication, world LOS and cone
  checks. Fan Static scopes the exact DamageInfo; nested item procs do not inherit
  its separate flat Static gain. Crit/Conductor bonuses and existing immunity apply.
- Replicated hidden flags separate spear out (including flight), planted (existing
  aim-assist semantics), and returning. Recall/throw are separate EntityStates;
  SkillDef overrides preserve per-player stock without mutating shared definitions.
- Eight v37 clips: held, crown held, fan start/loop/end, catch, Conduit Spear and
  recover. Six new titles, two replacements. 73 Blender actions unchanged; Unity
  retains 66 unrelated clips exactly, including Arc Bolt. Total shipped clips: 74.
- `SpearCarry` is a right-arm/finger mask above the retained five layers, with its
  own speed parameter. Left arm, locomotion torso/legs and halo remain available.
  Existing arm life keeps whole-arm/wrist sway and follow-through; held fingers
  keep the fitted grasp. Fan/throw aim rotates the whole upper arm with the socket,
  inside the existing restore/save pass, so the shaft does not turn inside the grip.
- Final model has 14 mesh parts. Held/flight/planted copies share `mdlConduitSpear`.
  Flight/return ghost hides once the local fitted hand appears, preventing duplicate
  weapons while awaiting the server destroy packet. A ghost must first observe out
  or returning mode before latching the catch; a delayed remote out buff cannot hide
  a newly thrown ghost permanently. Body current uses the accepted
  rear/under arm path; the hand contact continues up the shaft with a thicker pulse.
  Fan rays have crackling forks and stop at world surfaces; idle body current is off.
  The study's static pulse mesh is removed; only the timed travelling pulse remains.
  Energy tint uses stable mesh names because Unity renames saved material assets.
- Immutable source: `art/anim/hollow-saint-anim-v37.blend` and author report in
  `art/anim/v37/`. `author_spear_v37.py` uses v35 IK helpers on the fitted study and
  refuses source overwrite. Animation exports: `Source/v37_clips01`.
  Weapon base export: `Source/spear02` (includes the original rig/rest hand socket
  to retain exact FBX bone axes). The final grip is replaced by a native Unity mesh
  molded against the actual imported hand (see below). `spear01` is not bundled.
- `FoundationBundleBuilder10` writes `GameFoundation10r1` / bundle10r1 and clones
  bundle09's working controller/clips. The first `GameFoundation10` build failed
  because existing clip internal names use underscores; its output is preserved.
  The corrected build maps those titles, verifies the original hand rest axes,
  extracts the calibrated socket and packs both character/weapon models.
- An independent check found 18 triangle contact pairs in bundle10r1's imported
  grip, at ring segment 1 and the wrist cuff, despite the Blender study passing.
  `VerifySpearMesh10.RunBatch` exports exact Unity surfaces and inverse matrices;
  `tools/blender/spear/refit_imported_grip.py` relieves those contacts and explicitly
  triangulates the closed solid without another FBX conversion. Immutable repair:
  `art/concepts/spear-runtime-fit01/{imported-grip-refit.blend,grip-local.json,fit-report.json}`.
  `FoundationBundleBuilder11` retains the verified v37 controller unchanged, copies
  the character/socket, swaps only the native grip mesh, removes the static pulse,
  and writes `GameFoundation11` / bundle11. Grip: 1602 vertices, 3204 triangles.
- Release build: 0 errors, existing NU1701 warning. All nine offline checks pass,
  including `Check-SpearModes.ps1` (free recall, recharge, stock gate, busy/return
  gates, prediction, separate players, primary modes and cone boundaries).
  `Check-Access.ps1` passes against real installed assemblies.
- `VerifyBundle10.RunFinalBatch` passes imported clip preservation, right-only mask, fixed grasp,
  running/jump/glide/Arc Step layering, held/throw and catch/held seams, loop seams,
  and loading both models from the built bundle.
- `VerifySpearMesh10.RunFinalBatch` checks geometry loaded from the final built
  bundle, with all 375 fixed-finger samples passing. `check_imported_grip.py -- 11`
  reports zero intersections against all 27 hand parts and 1.248 mm minimum sampled
  vertex clearance. Reports: `artifacts/foundation/bundle11-verify.txt` and
  `spear11-imported-grip-check.json`. Whole-body clearances still need playtesting.
- Visual reviews: `art/anim/v37/held-fan-standing-running.gif` and
  `spear-flow-standing-running.gif`. These are Blender motion/VFX studies, not
  in-game captures. Return/throw travel is illustrative. Game feel, network timing,
  actual whole-body weapon clearances and VFX readability/performance remain untested.
- Full test instructions are the new top section of `PLAYTEST.md`.
- After Stu closed r2modman, the local profile listing and manifest were updated
  successfully to 0.7.3. Staged file hashes match the built outputs.
- Final staging backup `profile-backup-20260929-220718` holds the superseded interim
  v0.7.3 files; use the original `profile-backup-20260929-214127` for v0.7.2 rollback.

## Previous v0.7.2 checkpoint

## v0.7.2 playtest checkpoint (superseded above)

- v0.7.2 is staged in **Hollow Saint Dev**, with the accepted ring-fed throw and
  connected body current. All v0.7.0 balance/spear/Open Circuit/arm-life work remains.
- In-game tag: `Hollow Saint v0.7.2 | rear-arm | elbow-flow`.
- Bundle09: only Arc Bolt left/right replaced with v36; 66 other shipped clips
  retained exactly from bundle07. Five animation layers retained.
- Release build: 0 errors, existing NU1701 dependency warning only.
- `Check-Access.ps1` passes against installed game/dependency assemblies.
- All eight `tools/tests/Check-*.ps1` checks pass under PowerShell 7: animation
  rules, arm-life stability/reactions, halo geometry, locomotion, Storm charge
  sequencing, Thunderbolt flight timing, ability-current timing, and arm-current
  geometry. The route/timing checks were rerun for v0.7.2; the other six are unchanged.
- Unity `VerifyBundle09` passes: imported throw boundaries/timing, preservation
  of other clips, standing/running casts, legs/torso and Open Circuit halo layering.
- Staged DLL/bundle hashes match the built outputs; profile listing is 0.7.2.
- No game launch or gameplay verification this session. Stu handles playtesting.
- No push, public mod release, or deployment. Only the local Dev profile changed.
- Rollback to v0.7.1: restore the DLL from
  `artifacts/foundation/profile-backup-20260929-185446`; keep bundle09.
- Rollback to v0.7.0: restore BOTH DLL and bundle from
  `artifacts/foundation/profile-backup-20260929-182237` with the game closed.
  That backup contains the working bundle07. The earlier v0.6.3 DLL remains in
  `artifacts/foundation/profile-backup-20260929-172936`.

## v0.7.2 rear-arm route refinement

- Stu wants the path to read directly from behind: down the back of the upper arm,
  around the elbow, underneath the forearm, through the hand and out.
- `ArmCurrentPath` adds a mid-upper-arm waypoint, an outside-elbow wrap, underside
  forearm/wrist and hand waypoints. Both arms use it, including spear and crown current.
- `BodyCurrentFx` derives anatomical directions from skin bind poses, then follows
  the posed bone rotations. The earlier world-back offsets could cut across an extended
  arm. Missing bind poses log a warning and fall back to the current pose direction.
- Same v36 clips/bundle09; no new animation bake, gameplay or release-timing changes.
- `Check-ArmCurrentPath.ps1` verifies posterior/underside routing, elbow clearance,
  outlet position, rotations, mirroring, scaling and finite straight/degenerate cases.
- Rear and side Blender frames reviewed standing/running; animated previews are in
  `art/anim/v36/current-route02/`. Repro: `tools/blender/anim/review_arm_current.py`
  under isolated background Blender. They illustrate the route and are not game captures.
- Actual mesh clearance/readability and skin-bind calibration still need in-game
  visual testing; the broader movement-blending work below remains pending.

## v0.7.1 connected-current implementation (retained)

- Common body circuit: core/chest around the shoulder to the spine and live ring;
  ring pickup arcs; current down the back of the casting arm into fingers/muzzle.
  A thicker pulse and short branched crackles strengthen the release.
- Cast-window generation tokens protect rapid/replaced casts; interruption fades
  without a release flash. Current fades 0.12 s after release. Arc Bolt still travels
  freely; no persistent target tether or gameplay change was added.
- Arc Bolt and spear use the same arm path; spear recall powers the core connection.
  Open Circuit powers both arms while active; Arc Step connects to the legs/heels;
  Thunderbolt gather/release shares the core/ring connection. Existing crown VFX retained.
- Uses live bones/ring shape, per-skin palettes, cached lines and death/model/skin
  cleanup. Idle electrical crackles are gated off. Stored-charge orbs remain visible.
- Blender flair renders illustrate the direction; actual C# VFX still need Stu's
  in-game visual/performance/multiplayer checks. Do not describe the GIF as game footage.
- Production authoring: `tools/blender/anim/armpass_v36.py`, shared v35 IK helpers,
  immutable `art/anim/hollow-saint-anim-v36.blend`, `Source/v36_clips01` (two FBX).
  Review/proofs/baseline archive: `art/anim/v36/`.
- Builder08/bundle08 are retained but NOT staged: reimporting present FBX sources
  changed twelve unrelated older clips. Builder09 clones the working bundle07's
  clips instead. Its baseline is preserved in `art/anim/v36/retained07-clips.zip`;
  see that folder's README for fresh-checkout restoration. Never overwrite numbered outputs.
- Known limitation: running torso lean lowers the release hand. Next pass should
  address aim/movement/gesture blending, jump/landing casts, Open Circuit arm hold
  interruption/resumption, and smoother recoveries. These broader motion fixes are pending.

## Changes to test

- Extra secondary charges enlarge the single planted spear's radius, spread
  target count, and visible size within configurable caps. Pulse and spread
  now require line of sight. Recall/rethrow still leaves only one spear.
- Open Circuit follows the animated copper halo: charge orbs follow its plane,
  crown arcs bridge its gaps, and pulse tendrils start on the ring near the target.
- Arm life reacts to acceleration, turns, jumps and landings, with spring
  follow-through through elbow, wrist and fingers. Casting-arm share is 25%.
  Controls are in `0. Movement`; the existing toggle disables all arm life.
- Skill/passive descriptions now follow config values. Remaining text edits
  clarify arm life and extra spear charges. Arc Step preserves sprint.

## Balance defaults (complete list)

| Setting | Before | v0.7.0 |
|---|---|---|
| Electrocute pop damage | 250% | 150% |
| Pop targets | 3 | 2 |
| Pop proc coefficient | 0.5 | 0.3 |
| Pop Static per target | 40% | 15% |
| Static Conductor multiplier | 1.5 | 1.25 |
| Minimum Static per full-proc hit | 8% | 6% |
| Shocked damage taken multiplier | 1.2 | 1.15 |
| Spear spread targets | 3 | 2 |
| Spear spread damage | 60% | 35% |
| Spear spread proc coefficient | 0.5 (fixed) | 0.3 (new setting) |

Arc Bolt's direct hit is unchanged. Defaults migration version 3 changes existing
entries only when their saved value matches the old default; custom values are
kept. It cannot distinguish a deliberate choice equal to the old default.
The earlier agent's roughly 30% early-pack reduction is a model estimate, not
an in-game measurement. See `tools/balance/dps_model.py`.

## Current visual direction and prototype (Stu, 2026-09-29)

- Stu likes the current balance and improved Open Circuit. Wants every ability
  to read as powered by the back ring, especially Arc Bolt: current travels
  from the ring down the back of the casting arm, through the hand and outward.
- Lightning should be visible only during attacks/abilities. No idle current.
- Movement character: calm and floaty, with soft trailing arms and fingers.
- Arc Bolt should combine a throw with that current path; the earlier v35 palm
  push is not the chosen final direction. A brief connection breaks after
  release and the bolt continues onward; do not change it into a sustained beam.
- First isolated Blender study: `artifacts/arcbolt-concept01/ring-throw02.blend`.
  Uses the existing v35 IK authoring helpers against v34, with a forward throw,
  relaxed hand orientation, delayed fingers and illustrative bone-following VFX.
- Review: `artifacts/arcbolt-concept01/standing-1x.gif`, `standing-slow.gif`,
  `running-1x.gif`, `running-slow.gif`. These are Blender concept renders,
  not in-game VFX captures. Three views per GIF: rear, front, side.
- Original study scripts are in that artifact folder. Production `armpass_v36.py`
  now uses the helpers copied into root, so it no longer needs the anim35 worktree.
- QA of the revised two clips: only the known back-conductor/neck contacts
  (5.7-5.8 mm); initial upper-arm accent pops were corrected. The earlier
  palm-push forearm-strip contact does not occur in this throw study.
- Contract check: only Arc Bolt left/right changed; 73 clips unchanged,
  markers and 20-frame ranges retained, boundary values differ only by
  float32 normalization (maximum channel error 1.14e-6).
- Known prototype limitation: when composited on Run forward with the current
  arms-only mask, torso lean lowers the hand's release position. Address this
  in the next movement/aim/gesture blending pass; do not call it final.
- Stu accepted the throw and requested more shoulder/core crackle and a thicker
  pulse. The direction is now implemented/staged in v0.7.1; the broader movement
  blending audit remains next, after playtest feedback.

## Earlier Arc Bolt v35 candidate (not selected as final)

- Branch `v07-anim35`, commit `dde58ac0`, worktree `.worktrees/anim35`.
- Not merged, bundled, or staged. Only Arc Bolt left/right change; 73 other
  clips remain v34. Chest-height palm push replaces the underhand hurl.
- Review files in `.worktrees/anim35/art/anim/v35/`:
  `compare_Arc_Bolt_right.png`, `compare_Arc_Bolt_left.png`,
  `stand_right_1x.gif`, and `run_right_1x.gif`.
- Known new defect: the right forearm glow strip sinks into the forearm during
  frames 4-9 (QA worst contact 8.6 mm). The conductor's hand skin weight differs
  from the left side. Fixing that belongs to model/rig work.
- Preserve as authoring/reference work. Do not merge or stage it automatically;
  Stu now wants the ring-fed throw described above. The next accepted animation
  uses Builder09 / bundle09. Use the next unused number for future work.
- More details: the v35 section of the animation branch's `art/anim/STATUS.md`.

## Earlier held-spear design and hand-fit study (implemented in v0.7.3 above)

- Stu approved the workshop direction: held spear changes primary to a continuous
  crackling fan while primary is held. Its role is Static buildup and nearby crowd
  control. Spear flying/planted restores existing ranged Arc Bolt. Recall is free,
  including while secondary stock recharges; it stores the spear in hand. A separate
  next press throws only with a ready charge. No automatic recall/rethrow.
- Proposed starting fan values: 8 m, 60 degree cone, 5 targets, lower direct damage
  than Arc Bolt. These values remain provisional. Line of sight and server-owned
  damage are required. No new mechanics or animations have been implemented/staged.
- Stu wants to settle the spear appearance before starting that implementation.
  Two imagegen concepts were shown: Halo Lance and Storm Fork. This hand-fit study
  develops the recommended Halo Lance direction; a final A/B choice was not explicit.
- Fit to the actual v36 character hand, not the concept's generic ivory hand:
  tapered charcoal grip relieved around individual rigid finger segments and palm,
  extended thumb seat, and copper collars outside the hand envelope. A hand-bone
  grip socket keeps the weapon aligned while the arm/wrist moves. The grasp is a
  dedicated repeatable pose; suppress procedural finger motion while gripping,
  while retaining soft whole-arm/wrist follow-through and release/catch finger motion.
- Stu also explicitly wants the electrical connection from the back ring, down the
  back of the arm, around the elbow, under the forearm, through the hand/contact
  seat and into the spear. A thick travelling pulse continues up the shaft to the
  head. Body current is visible only during attacks/abilities; quiet held silhouette
  remains visible. Use one shared body circuit with other abilities.
- Actual Blender fit/flow previews and source: `art/concepts/spear-hand-fit01/`.
  This is a prototype, not a game capture or runtime asset. Geometry/topology,
  shaders, all-animation clearances and multiplayer behavior still need integration.
- Repro: isolated background Blender, `tools/blender/spear/hand_fit_study.py --
  artifacts/<new-numbered-study>/study`. It loads immutable v36; never saves it.
  Default latest working run: `artifacts/spear-hand-fit05/study`. The current review
  script `review_hand_fit_current.py` targets that run. Each output refuses overwrite.
- Study QA: 0 grip/hand intersecting face pairs; nearest sampled grip vertex is
  about 1.21 mm from the actual hand. All four collars clear the hand. Socket follows
  four arm/wrist perturbations with maximum matrix error 1.2e-7. This checks one
  grasp pose/attachment, not running/jumping/casting animation transitions.
- Earlier study01 ignored separate bone-parented hand parts; study02/03 are older
  fitting iterations. Use study05 and the retained concept folder.
- Current playable checkpoint remains local v0.7.2 and bundle09. No game/profile
  changes, version bump, public release or push occurred during this concept pass.

## Preserve

Keep existing untracked Unity generated assets/metas, animation WIP, Blender
scripts/backups, and Wwise.dat. They were not included in the v0.7.0 checkpoint.
`.env` is ignored and must never be staged. No subagents are active in this chat.

Next action: Stu playtests v0.7.3 and supplies movement, catch/throw, fan/VFX and
balance feedback. Keep the v0.7.2 backup available. No game launch by the agent.
