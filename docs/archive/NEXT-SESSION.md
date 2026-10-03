# Current: v0.7.2 staged for playtest

Read `../next-session-handoff.md` for the complete current state, balance defaults,
verification, rollback, and pending movement-blending work. Use `../PLAYTEST.md`.

Build tag: `rear-arm | elbow-flow`. Current follows the back of the upper arm,
around the elbow and under the forearm/hand using skin-bind anatomical directions.
Bundle09 contains only the accepted
v36 throw replacement; other clips are frozen from bundle07. Release build,
member-access check, route geometry/timing checks pass; prior six regression checks
and Unity bundle09 verification apply to unchanged code/bundle. v0.7.1 DLL rollback:
`artifacts/foundation/profile-backup-20260929-185446` (keep bundle09).
No game launch, push, or public release. v0.7.0 rollback (DLL AND bundle):
`artifacts/foundation/profile-backup-20260929-182237`.

The following sections are historical and describe earlier builds.

# Next session handoff (v0.6.3 shootable spear and v34 arms)

## Current work: v0.6.3 / bundle07

Build tag: `shoot-spear | arms-v34`. Staged in Hollow Saint Dev with bundle07; r2modman
listing refreshed. Local commit `81ac2e9c` plus this handoff commit; nothing pushed.
Release build 0 errors, Check-Access passes. NOT launched or smoke tested (usage budget);
Stu playtests. Rollbacks: v0.6.2 (bundle06) `artifacts/foundation/profile-backup-20260928-231110`,
v0.6.1 (bundle05) `profile-backup-20260928-223417`, v0.6.0 `profile-backup-20260928-223154`.

### Agent transition

- No work is pending except possibly the v34 arm agent's final review renders in
  `artifacts/anim-audit/v34/renders/` (read-only; bundle07 was already built and staged).
  If a later v34 tweak exists on disk (check `git status`, `art/anim/STATUS.md` v34
  section), rebuild as Builder08/bundle08 rather than overwriting bundle07.
- Next action: collect Stu's v0.6.3 playtest feedback. Priorities: (1) do the v34 arms
  feel right (idle out at the sides, Arc Bolt hurl flow, no curled fists); (2) is the
  spear easy to shoot and is the strike reaction readable; (3) spear plant/pulse/recall.
- Stu's usage is limited: compile checks only, no routine game launches.

### Timeline this session (2026-09-28 evening)

- v0.6.0 Conduit Spear anchor (see section below and `docs/conduit-spear-redesign.md`).
  Stu's decisions: stays planted until recast; on enemy hit it sticks in the enemy and
  drops to the ground when it dies; pulse = small damage + Static; Arc Bolt hits in the
  radius spread to other enemies; recast recalls then throws.
- Animation audit (`artifacts/anim-audit/`: contact sheets, clip_metrics.json,
  transitions.json). Key findings: gestures began/ended in the arms-down neutral pose
  (120 deg snaps), Open Circuit arms never cast, fingers nearly static, run arms ~110 deg
  out of phase, Run stop never played.
- v0.6.1 code (all still in place): `FoundationArmPose.cs` runtime "Arm life" (breathing,
  finger noise, wrist sway, turn lag; config "0. Movement" toggle + intensity);
  `FoundationAnimRules.cs` + `tools/tests/Check-AnimRules.ps1`; KitAnim `PlayGesture`
  (arms-only "UpperArms" layer when moving above 1.2 m/s or airborne, UpperBody when
  standing), `PlayGestureWithRecover`; entry fade 0.09 s, exit at 85% with 0.2 s fade;
  FoundationPresentation fades (combat flip 0.35, moving to combat idle 0.3, glide to jump
  0.12, run to glide 0.2), Combat ready/relax on combat flips, idle fidgets every 8-15 s,
  Run stop restored (from 3.5 m/s, latched foot); spear recall interrupted -> refund stock.
- v0.6.2: v33 clips (10 new: Combat ready/relax, Idle fidget 1-3, Idle combat fidget,
  Conduit Spear recover, Discharge recover, Open Circuit arms/arms hold; 41 reworked) and
  bundle06 (Builder06, 68 clips, UpperArms mask 46 bones). Stu's verdict: disliked the
  hand changes (curled fists at the hips, tucked hands), Arc Bolt arm motion felt funny;
  he prefers arms held out at the sides then hurling, with more natural flow.
- v0.6.3: v34 clips (`art/anim/hollow-saint-anim-v34.blend`, `tools/blender/anim/armpass_v34.py`,
  `Source/v34_clips01`) rebuilt from v32 arm character with overlapping follow-through;
  Arc Bolt has no wind-up (it retriggers every shot); bundle07 via Builder07. Compare
  sheets `artifacts/anim-audit/v34/zoom_v32_v33_v34.png`. FullQA 13/75 pass (v33 12/75),
  no new failures. Shootable spear: owner's Arc Bolt swept test within "Spear hit radius"
  (1.6 m) of the planted shaft pops on the spear and spreads from it; SpearStruck beat
  (flash, shaft crackle, ring, zap max 6/s, ghost swell) always plays; crosshair aim
  assist toward own spear. Risk: generous radius may eat bolts aimed at an adjacent enemy.

### Known open items

- Discharge has no animation caller, so "Discharge recover" never plays.
- Overlay gestures (meter flourish) have no arms-only copy.
- `tools/tests/Check-Locomotion.ps1` needs PowerShell 7 (`readonly struct`); pwsh is not installed.
- Spear: no line-of-sight check on pulse/spread; skill description numbers are static
  text; remote clients see a spear riding a fast enemy lag slightly.
- Builder06 had a compile bug (duplicate `source` local) fixed by hand; Builder07 copies the fix.
- Untracked, preserve: `art/anim/wip/robe-polish/`, `art/audio/HollowSaintAudio/Originals/SFX/Wwise.dat`.

## Previous v0.6.0 handoff


## Current work: v0.6.0 / unchanged bundle05

Build tag: `spear-anchor | recall-throw`. Conduit Spear redesign implemented per
`docs/conduit-spear-redesign.md` (Stu's decisions and a "Built" section). DLL staged in
Hollow Saint Dev (bundle05 unchanged), r2modman listing refreshed. Release build 0 errors,
Check-Access passed. NOT launched or smoke tested (usage budget); Stu playtests with the
spear checklist at the top of `PLAYTEST.md`. Rollback to v0.5.2 DLL:
`artifacts/foundation/profile-backup-20260928-212717`.

- The spear projectile now survives impact as the anchor (`ConduitSpearAnchor`), server
  owned; clients see it through the projectile's network transform and lance ghost.
- New config sliders under "2. Conduit Spear" (radius, pulse interval/damage/proc/Static
  weight, spread targets/damage, recall duration, leash distance).
- `ProjectileController.flightSoundLoop` is private (Check-Access caught it); it is cleared
  by reflection with a logged fallback.
- Watch in the first log: `HOLLOW_SAINT_EVENT SPEAR_PLANTED / SPEAR_STUCK / SPEAR_PULSE /
  SPEAR_SPREAD / SPEAR_RECALLED`, and any warning about a missing ProjectileNetworkTransform.
- Untracked items still to preserve: `art/anim/wip/robe-polish/`,
  `art/audio/HollowSaintAudio/Originals/SFX/Wwise.dat`.

## Previous v0.5.2 handoff


## Current work: v0.5.2 / unchanged bundle05

Build tag: `sprint-silhouette | skin-lightning`. Final DLL and unchanged bundle05
staged in Hollow Saint Dev; r2modman listing refreshed. Startup checks passed.
No public release or push. Rollback v0.5.1 DLL and bundle05:
`artifacts/foundation/profile-backup-20260928-200428`.

### Agent transition

- Implementation checkpoint: `80eca898` on local `main`; nothing pushed.
- All subagents have completed. No implementation work is still running.
- Next action: collect the user's v0.5.2 playtest feedback before another pass.
  Start with rear-view sprint and Umbral orb/lightning color. Do not repeat
  completed checks unless new edits or a specific failure justify them.
- Preserve these untracked items: `art/anim/wip/robe-polish/` (earlier rejected
  experiments), `art/audio/HollowSaintAudio/Originals/SFX/Wwise.dat` (authoring
  cache), and `docs/conduit-spear-redesign.md` (not reviewed or changed in this
  pass; inspect before any spear work). They are not part of the v0.5.2 commit.
- Only stage into Hollow Saint Dev. No public release is authorized. The
  rollback and validation artifacts above are local ignored files, so a fresh
  clone will not contain them.

- User says v0.5.1 movement is better and likes the skins. Full sprint from
  directly behind still looks unnatural. Screenshots 2026-09-28 195115,
  195144 and 195319 show narrow, nearly straight legs and long parallel jets.
  They requested skin-matched orbs/lightning, explicitly purple for Umbral.
- Sprint pose was revised without changing the controller or authored
  walking cadence. Heel jets now use compact local-space Flash particles,
  no stretched impact sparks; maximum emission travel is about 0.6 m.
  Smaller heel cores keep the feet visible. Sprint adds at most 18 degrees of
  knee flex toward a bounded target, 6 degrees of stance spread, and slightly
  less lean. Nine source-rig samples pass knee/stance/sole/tabard assertions;
  zero-weight walk is unchanged. Representative ankle span 0.240 to 0.353 m,
  torso lean 27.22 to 24.59 degrees. Rear/side comparisons and measurements:
  `artifacts/foundation/sprint-silhouette-preview/`. This is an approximation
  of runtime bone accents; gameplay appearance still needs the user playtest.
- Per-owner skin palettes: default/Obsidian cyan, Verdigris green, Solar gold,
  Umbral purple. Bright cores retained. Coverage: earned orbs/halo gather/release,
  casts/projectile ghosts/impacts, Ukulele chain variants, Circuit, Electrocute,
  warning/Capacitor strike variants, heel jets/jump/dash/afterimages. Shared target
  Static/Conductor markers remain cyan because there is no unambiguous owner.
  Packets snapshot palettes in EffectData.color; independent material/ramp caches
  leave source assets untouched. Native alpha/gradient modes are preserved.
- Release build and public-member scan pass. Locomotion, Storm sequence and
  Thunderbolt flight regression checks pass. Startup audit exercises real game
  serialization for all five palettes, material isolation/cache identity,
  particle alpha modes and registration of three chain/six strike variants.
- Damage, movement speed, cooldowns and audio are unchanged. No terrain foot IK.
- Preserved `artifacts/foundation/v051-user-playtest.log`: two runs, 111.0 s
  and 101.3 s; first run had 14 charges and two Thunderbolts. Live-body ground
  reference also measures model origin 0.010 m above capsule bottom.
  No HS exception; game scene/network/pool errors and native Animator warnings
  occur during gameplay and should not be confused with clean startup evidence.
- Startup confirms SKIN_FX_CHECK_PASS (five real packet round trips, material
  isolation and alpha modes), SKIN_CHAIN_READY, THUNDER_IMPACT_READY and
  CATALOG_CHECKS_PASS. Known shader-key error remains. Evidence:
  `artifacts/foundation/v052-startup-smoke.log` and `v052-catalog-checks.txt`.
  Test process closed. Actual appearance/multiplayer remain user playtests.
- USER PREFERENCE: usage is low; user handles gameplay testing. Prefer compile
  checks and do not launch routine startup/playtests in future; launch only when
  necessary to diagnose a specific issue. Keep future verification focused.
- Focused acceptance: `PLAYTEST.md`.

## Previous v0.5.1 handoff

## Current work: v0.5.1 / bundle05

Build tag: `grounded-motion | storm-crackle`. No public release or push.
Final v0.5.1 DLL and bundle05 are staged in **Hollow Saint Dev**; r2modman
listing refreshed. Final startup verification passes; test process closed.
Rollback to v0.5.0 + bundle04: `artifacts/foundation/profile-backup-20260928-193426`.
Later backups may contain intermediate v0.5.1 files; use this named pair.

- User feedback: keep restrained sound levels, add electrical crackle; replace
  Answered Prayer's bomb-like rise/impact; make walking, turning and full sprint
  feel connected to travel and heel thrust.
- Bundle05 separates eight-direction walk/run trees from Idle. Playback follows
  ground speed and authored directional stride, calibrated from actual Animator
  clip weights. Imported clip names contain underscores; the startup cache
  normalizes these names. Very slow movement no longer forces 1x playback.
- World-space velocity smoothing avoids mixing different facing coordinate
  frames. Ordinary walking turns bypass the unrelated whole-body pivot clip.
  Footsteps follow evaluated half-cycle contacts and reset on air/death/hitches.
- Sprint has blended acceleration/turn lean and ankle counter-rotation, applied
  after animation without accumulating bone changes. Heel particles sample the
  resulting foot positions, fade with speed and aim opposite travel/downward.
  Reversals smooth the world-space lean vector through upright. A read-only
  Blender approximation increased forward torso lean 7.75 to 27.22 degrees
  with clear soles; artifacts/foundation/v051-motion-preview stores evidence.
  Death, dash and disable clear thrust. Movement/input/balance are unchanged.
- No terrain foot IK or exact world-space foot locking. Authored walk/run soles
  are roughly 8 mm above source ground; glide's ~0.21 m hover is intentional.
  A new `HOLLOW_SAINT_GROUND_REFERENCE` log compares model origin and capsule
  bottom. Startup measured capsule height 1.820 m and model origin 0.010 m
  above its bottom; no vertical model adjustment was made.
- Wwise bank: 1,125,114 bytes, format150, 29 events, 26 PCM sounds, three local
  stop events, two infinite loops; existing SFX_BUS routing, no bus definitions.
  Answered Prayer has finite 0.35 s static gather, upward electric release and
  lightning crack after the existing 0.25 s flight. Gather cancellation/re-pick
  uses network revisions; rapid valid re-picks are not sound-throttled.
- Custom impact clones the Capacitor visual and registers its new sound before
  the effect catalog is built. Vanilla asset is untouched. Read
  `HOLLOW_SAINT_THUNDER_IMPACT_READY` in the next startup log. Server bank failure
  chooses vanilla; individual client bank failure can still leave custom impact
  audio silent. Custom gather durations over 0.35 s can outlast its finite sound.
- Arc Bolt, chain hop and charge ticks have more crackle. Twelve movement/Circuit
  sources remain byte-identical to v0.5.0. Higgsfield credits remain untouched.
- Release compile and public-member access scan pass. PowerShell7
  `tools/tests/Check-Locomotion.ps1`, StormChargeSequence and ThunderboltFlight
  tests pass; bank event/routing/loop validation and deterministic rebuild pass.
  Unity's actual controller probe passes slow walk, sideways 1x/2x playback,
  backward run and 22.5-degree walk/run blend cadence. Evidence:
  `artifacts/foundation/bundle05-locomotion-probe.txt`.
- Final v0.5.1 startup confirms CUSTOM_AUDIO_READY (AK_Success),
  THUNDER_IMPACT_READY, KIT_VERIFIED and CATALOG_CHECKS_PASS. No new distinct
  error lines versus v0.5.0; the known shader-key error remains. Saved
  `artifacts/foundation/v051-startup-smoke.log` and `v051-catalog-checks.txt`.
  This is startup-only QA; real movement/audio playtest remains pending.
  Saved previous user log: `artifacts/foundation/v050-user-playtest.log`.
  Read `PLAYTEST.md` for focused acceptance. Existing robe/skin limits remain.

## Previous v0.5.0 handoff

## Current build: v0.5.0

Staged in **Hollow Saint Dev** with **bundle04**; r2modman listing refreshed.
Build tag: `sound-pass | animated-skins`. No public release or push.
Rollback DLL and bundle03: `artifacts/foundation/profile-backup-20260928-190755`.
Read `PLAYTEST.md` for the next run. The v0.4.3 notes below are history.

- Wwise 2023.1.4.8496 is installed at `C:\Audiokinetic\Wwise_2023.1.4.8496`.
  The game uses the 2023.1 engine family and bank format 150; old 2021 authoring
  advice is obsolete. The mod embeds our 1,027,491-byte content bank, with 23
  original PCM effects and 25 events. It references the game's SFX bus and owns
  no buses. Never load or distribute the authoring project's Init bank.
- Softer footsteps/landing, airy movement and restrained electrical combat audio.
  Circuit pulse audio is throttled to at most once per 0.9 s. Damage still pulses
  every 0.5 s. Both custom loops have emitter-local stop events; death/disable
  stops Circuit. Bank failure logs explicitly and uses vanilla fallbacks.
- Higgsfield exposed speech generation only, so no credits were spent. Audio
  sources, audition reel and build instructions: `art/audio/README.md`.
- Chains use vanilla Ukulele presentation for 0.18 s via the existing network
  beat; damage, target choice and proc behavior are unchanged. Snapshot endpoints
  survive victim destruction. The previous line effect remains a logged fallback.
- Three animated material skins: Verdigris Relic, Solar Vespers, Umbral Choir.
  Existing skins remain indices 0/1. Same mesh/textures/rig, per-model emission
  pulses, cyan skill effects. Catalog audit checks all five body/mannequin skins.
- Runtime transitions use bounded pair-specific fades; running jumps enter at
  authored frame 4. Stopping during glide exit no longer waits for the full clip.
  Walk/run footstep cadence now uses the same speed threshold as its sound.
- v32 changes only Run forward cloth swing: measured left-thigh overlap falls
  12.4 to 10.2 mm, with no new FullQA failures. All 72 seam checks remain within
  0.005 mm, noncloth poses unchanged. Glide/dash clipping remains; broader trials
  were rejected. Builder04 uses the unchanged v31 model/other clips plus
  `Source/v32_clips01/Run_forward.fbx`. Four layers, 58 gameplay clips retained.
- Release build, public-member scan, embedded-bank byte comparison, binary audio
  event/routing/loop checks and Thunderbolt timing checks pass. In-game visual
  quality, sound mix, distance attenuation/SFX slider and multiplayer need playtest.
- Startup smoke confirms v0.5.0, `CUSTOM_AUDIO_READY` (`AK_Success`),
  `CHAIN_FX_READY`, `KIT_VERIFIED`, and `CATALOG_CHECKS_PASS`, including all five
  skins on body/mannequin. No new distinct error lines versus v0.4.3; the known
  shader-key error remains. Saved `artifacts/foundation/v050-startup-smoke.log`
  and `v050-catalog-checks.txt`. Test process closed. This was startup-only QA.
- Saved user log `artifacts/foundation/v043-user-playtest.log`: 179.1 seconds,
  195 qualifying hits, 23 charges added, 3 Thunderbolts. Balance unchanged.

## Previous v0.4.3 handoff

Read this first. Then HANDOFF.md (tooling), docs/storm-passive.md (passive design), docs/kit-architecture.md, TODO.md.

## Where things stand

- Staged build: **v0.4.3** (`earned-orbs | impact-pause`), ready for playtest. Nothing pushed. Build tag is drawn top-left in game.
- Bundle: **bundle03** staged (per-layer gesture rates `attackSpeed` / `overlaySpeed` / `haloSpeed`; Open Circuit cast, hold and end also live on the Halo layer). Builder: `HollowSaintUnityProject/Assets/HollowSaint/Editor/FoundationBundleBuilder03.cs`, Unity 2021.3.33f1 at `C:\Program Files\Unity 2021.3.33f1\Editor\Unity.exe`. Builders refuse to overwrite; make Builder04 for a new bundle.
- Concept art supplied by Stu: `C:\Users\stuwj\Downloads\Answered Prayer Concept.html` (3 boards: loop, charge states, Thunderbolt timing). Decoded reference copies are in ignored `artifacts/concept-reference/`. Design doc: docs/storm-passive.md.
- Stu's verdict on v0.4.0: "on track". Movement feels good.

Previous DLL backup (v0.4.2): `artifacts/foundation/profile-backup-20260928-182428`.
The r2modman Dev listing is refreshed to 0.4.3; bundle03 is unchanged.

## v0.4.3 feedback and changes

Stu calls v0.4.2 a good start. Requested: only show earned charge orbs (no empty
placeholders), remove the charge GUI (confirmed to mean the on-screen meter, not
the chest glow), and add a short delay from upward launch to ground impact.

- Empty orb renderers are disabled, including their initial frame.
- Plugin no longer installs the Storm charge HUD; the chest glow stays unchanged.
- A 0.25-second server flight separates launch from impact. Impact damage and
  Capacitor audiovisual feedback remain synchronized; damage coefficients and
  Static gain stay unchanged.
- Launch spends charge once and snapshots damage/crit; new charges may accumulate in flight. A living target is tracked at impact, otherwise splash lands at the saved position. Cooldown starts at impact. Owner death permits resolution while the body exists; destruction cancels and logs.
- Build, public-API scan and `tools/tests/Check-ThunderboltFlight.ps1` pass. Runtime target-death/multiplayer behavior still needs playtesting.
- Startup smoke confirms v0.4.3, Capacitor asset ready, kit verified and catalog checks pass; no new distinct error lines versus v0.4.2. Saved `artifacts/foundation/v043-startup-smoke.log`; smoke process closed.
- v0.4.2 user log preserved at `artifacts/foundation/v042-user-playtest.log`:
  62.8 seconds, 26 qualifying hits, six charges added, one Thunderbolt.
- Focused acceptance: `PLAYTEST.md`.

## v0.4.1 playtest feedback and v0.4.2 scope

Stu likes the charge orbs and overall direction. Screenshots from 18:08 show the
orbs crossing the physical back halo instead of orbiting in its plane. Requested:
align that orbit, add charge crackle against the halo, gather orbs and launch their
energy upward when Thunderbolt strikes. Also correct the glide/flip heel exhaust.
Screenshots: `C:/Users/stuwj/Pictures/Screenshots/Screenshot 2026-09-28 180830.png`,
`180835.png`, `180840.png` in that same directory.

The saved v0.4.1 playtest log (`artifacts/foundation/v041-user-playtest.log`) reports
132.2 run seconds, 55 qualifying hits, 9 charge Electrocutes, 9 charges added,
1 Thunderbolt, and 1 strike-only Electrocute. Animation contract passes (missing=0).
Keep current balance; this pass is visual alignment and charge-release choreography.

## v0.4.2 implementation

- Charge orbit uses socket-local XZ, rotating around local Y, centered on the halo root. This matches the authored ring plane (bundle03 socket is -90 degrees X relative to its parent).
- Charged orbs crackle to the ring, capped at two short arcs per tick. Explicit network Gather/Release/Cancel beats drive convergence and restore the orbit on cancellation/re-pick; revisions discard stale/duplicate events.
- Only a committed Thunderbolt launches the fused orb upward and draws the sky connection. The existing Capacitor impact and damage still happen immediately at strike time; the outgoing visual lasts about 0.35 seconds. No added damage or delay.
- Heel emitters follow exact sockets in LateUpdate and are detached from animated foot rotation. Exhaust points opposite planar velocity with a downward component that grows at low speed. Double-jump moving puffs and sparks use downward cones; the under-foot ring/flash/sound remain.
- Release build and public-API scan pass (existing NU1701 dependency warning only). Sequence tests cover convergence, release, duplicate/stale packets, cancellation, re-pick and timeout recovery. Gameplay and two-player visual checks are pending Stu's playtest.

- Startup smoke confirms v0.4.2, Capacitor asset/sound ready, kit verified and catalog checks pass. One known shader-key error also appears in v0.4.1; no new distinct error lines. Saved log: `artifacts/foundation/v042-startup-smoke.log`. Smoke process closed; in-game visuals await Stu's test.

## v0.4.1 foundation (retained in v0.4.2)

- Storm HUD under the main HUD container reads the observed body's replicated charge; numeric count, segments and READY state. Hides for non-Saint/dead bodies.
- Cyan halo charge orbs, dark empty markers and white full-charge pulse; six by default, follows the configured maximum (2-20). Shader readability needs Stu's in-game feedback.
- Thunderbolt reuses vanilla Royal Capacitor `LightningStrikeImpact` presentation/audio only. Our damage path, procs, telegraph, target choice, Static gain and cooldown are unchanged. Load failure logs and falls back to the old custom beat.
- `HOLLOW_SAINT_STORM_SUMMARY` every 30 seconds and run end provides cumulative qualifying hits, charge Electrocutes, charges actually added, Thunderbolts and strike-only Electrocutes. Controlled by existing event-log config.
- Correction to the old tuning plan: `KitLog.Event` stops at three per event per process. Last v0.4.0 log reached that cap for both events, proving they ran but providing no frequency estimate. Do not lower thresholds based on those counts.
- Verification: Release build and public-member access scan pass. Startup log confirms v0.4.1, `CAPACITOR_FX_READY` (sound `Play_item_use_lighningArm`), `KIT_VERIFIED` and `CATALOG_CHECKS_PASS`. The one startup error also exists in the prior log. No gameplay/animation/HUD visual checks were performed; Stu's playtest is next. Smoke log: `artifacts/foundation/v041-startup-smoke.log`.
- Focused acceptance checklist: `PLAYTEST.md`. Enemy shock overlays, circuit/footstep audio, jet adjustment and spear persistence remain subsequent work.
- Known multiplayer limit: charge counts replicate but the configurable maximum is local; use matching settings until configuration synchronization is addressed.

## Workflow that works (keep using it)

Any agent with a shell on Stu's Windows PC (PowerShell) and optional desktop control can run this. Repo: `C:\Users\stuwj\Documents\Coding\ror2-lightning`.

1. Edit the C# in `HollowSaintMod\` directly. .NET SDK builds it: `dotnet build HollowSaintMod -c Release`.
2. Before every staged build: `python tools\bump.py <version> "<kw1> | <kw2>"`. It keeps `Plugin.cs` and `Package\manifest.json` in sync (ASCII only). The version and two keywords are drawn top-left in game, so Stu can confirm which build he is testing. Stay below 1.0.0 (1.0.0 = first public release).
3. Stage: `powershell -ExecutionPolicy Bypass -File tools\dev-profile\Stage-Build.ps1 [-SkipBuild] [-Bundle artifacts\foundation\bundle0N\hollowsaintassets]`. It builds, runs `Check-Access.ps1` (an IL scan that fails the stage if any private game member is used through the publicized reference assemblies), backs up the previous DLL, and copies into the `Hollow Saint Dev` r2modman profile only. It refuses while the game is running.
4. With r2modman CLOSED, `tools\dev-profile\Register-DevMod.ps1` refreshes the mod's entry and version in r2modman's list for that profile.
5. Smoke test: `tools\dev-profile\Start-Foundation.ps1` launches the game with the same profile arguments r2modman uses. Wait about 70 s, then search `%APPDATA%\r2modmanPlus-local\RiskOfRain2\profiles\Hollow Saint Dev\BepInEx\LogOutput.log` for `HOLLOW_SAINT_` lines (`KIT_VERIFIED`, `CATALOG_CHECKS_PASS`, `ANIM_CONTRACT_DONE`). The vanilla baseline is about 9 to 11 error lines. Close the game afterwards. If desktop control is available, the character is in the survivor grid after Drifter; the in-game build tag confirms the DLL.
6. Animation bundle changes: copy the latest builder to a new number (Builder04, GameFoundation04, bundle04) and run Unity in batch mode: `"C:\Program Files\Unity 2021.3.33f1\Editor\Unity.exe" -batchmode -nographics -projectPath HollowSaintUnityProject -executeMethod HollowSaint.Preview.Editor.FoundationBundleBuilder04.RunBatch -quit -logFile artifacts\foundation\bundle04-unity.log`. The report lands in `artifacts\foundation\bundle04-report.txt`. Stage with `-Bundle`.
7. Sound events must exist in the vanilla bank indexes (`...Risk of Rain 2_Data\StreamingAssets\Audio\GeneratedSoundBanks\Windows\*.txt`). Loaded banks are listed in `FoundationKit\Vfx\KitFx.cs`.
8. Commit locally after each staged build. Do not push, release, export profiles or publish to Thunderstore without Stu's explicit go. Never read or stage `.env`. Never touch the r2modman profiles `demo time` / `demo time new` or the Steam install (read-only).

Hard-won rules: use only PUBLIC game members (`((Component)body).transform`, never `body.transform`; do not call `EntityState.PlayAnimationOnAnimator`). No `Shader.Find` (throws in RoR2). Addressables keys only via `RoR2BepInExPack.GameAssetPathsBetter.*` constants. Input stays native (no Input.GetKey or KeyCode). PowerShell `Set-Content` re-encodes files, so use Python or a proper editor for non-ASCII text. Gesture layers have write-defaults off; skill gestures must clear the Overlay layer (already done in `KitAnim.Play`).

## v0.4.0 playtest feedback (Stu, verbatim intent) and the plan

Priority order for the next build (v0.4.1 / v0.5.0):

### 1. Answered Prayer is hard to see working (highest)
- Use the new Storm summary counters and Stu's feedback to judge frequency. The original first-three event log cannot establish rarity. Threshold 12% and minimum gain 12% remain possible experiments, not changes in v0.4.1.
- **Royal Capacitor**: Stu wants the Thunderbolt to look and feel like the vanilla Royal Capacitor strike, with our own scaling. Look up what the equipment actually uses (EquipmentSlot lightning fire path, `RoR2.Orbs` lightning strike orb classes, the strike impact effect prefab and its sound). Reuse its effect prefab / orb visuals via Addressables for our strike, keep our damage, targeting and cooldown. Verify every member is public (Check-Access will tell you).
- On-screen **charge meter** (see TODO "Next"): hook `RoR2.UI.HUD.Awake`, add a gauge under `hud.mainContainer`, read the Storm charge buff count (`bdHsStormCharge`) from `hud.targetBodyObject` (works for spectators). Reference: R2Wiki UI page, Ravager's Blood Well gauge.
- Enemy feedback: shock overlay on enemies at Static tier 2+ and while Electrocuted (`TemporaryOverlay` with a vanilla shock/electric material); bigger Electrocute pop.

### 2. Core and halo charge states unreadable against the white body
- Idea A (recommended): six small cyan charge orbs orbiting the halo, one lights per charge, all white-hot and pulsing at full. High contrast against the bone and white.
- Idea B: drive the emissive gap-light renderers on the halo mesh (find them; the halo pieces are named `HALO | independent copper arc 1..4`, `HALO | V17 yoke...`) from dark to bright cyan by charge.
- Plus the HUD meter from item 1.

### 3. Open Circuit: too loud, and needs real tendrils
- Sound: loop is `Play_loader_R_active_loop`, pulses `Play_loader_R_shock` every 0.5 s. Drop or throttle the per-pulse sound (every 2nd or 3rd pulse) and audition a quieter loop.
- VFX: replace the expanding ground ring per pulse with lightning tendrils from the halo to each enemy in range (the `CircuitArc` beat exists; make it thicker and more prominent), and consider persistent crackling tethers to the 3 nearest enemies for the whole 8 s. Keep the radius ring subtle or remove it.

### 4. Footsteps too loud and too mechanical
- Current: `Play_loader_step` / `Play_loader_step_sprint` (Loader is a mech, hence mechanical). Audition lighter steps from the bank indexes (Huntress, Mage, Merc, Captain), or play steps only on sprint, or drop them and rely on the glide hum. No per-play volume control without a custom bank (R2API.Sound) or an RTPC.

### 5. Heel jets angle
- "Close but not quite": should respond to movement direction and come out of the heels. Now: emitters at heel socket + 7 cm down + 7 cm forward, aimed `down + back*0.45`. Next: aim opposite local velocity with a down component that grows as speed drops; keep the origin at the heel socket; consider per-foot alternation with the gait. Check against the reference sheet `art/concepts/kit-v2/hs-kit-v2-heel-jets.png` (and the HEEL JET tile in the VFX library sheet).

### 6. Conduit Spear should visibly stick and bounce lightning
- It should embed in the ground or target and stay visible (projectile stick on world impact, lance ghost persists for the mark duration).
- Obvious lightning between the planted spear and the marked Conductor / chain hops through it. Option: a planted spear acts as a lightning rod for N seconds (zaps nearby enemies, builds Static, Arc Bolt chains can hop through it).

## Also open (from TODO.md)
- Survivor slot 19 sometimes not drawn in the select grid (registered, not hidden, portrait fine).
- Dedicated buff icons for Shocked and Storm charge.
- Camera shake on Thunderbolt once a public ShakeEmitter API is confirmed.
- Two-player network test.
- Afterimage mesh pooling (perf).
