# Hollow Saint: audit before playtest

Prepared September 30, 2026. Stu wants an independent audit of the current mod and
the corrected hand/ring preview before doing a playtest. Start with the working
tree and this file; use `next-session-handoff.md` for detailed history and evidence.

## Workspace and candidate

- Repository: `C:\Users\stuwj\Documents\Coding\ror2-lightning`
- Branch: `codex/spear-channel`.
- Extensive preexisting source, Unity and Blender WIP is uncommitted. Nothing is
  staged. Preserve it; do not reset, clean, discard, reimport over or bulk-rewrite it.
- Local **Hollow Saint Dev** profile contains **v0.7.14 | skin flow | cached lighting**.
  DLL, bundle and profile manifest were rechecked while preparing this handoff.
- DLL SHA256: `6E4971A5DE9EF9A99328FACC1D84D78E8BD2ACB91689713273E1AE747D221A4F`.
- Bundle13 SHA256: `1AD13A0DF9706A340AE20A0B656AF26F4FACD84048796034FEAC9D3ED57F35BC`.
- Profile plugin directory:
  `%APPDATA%\r2modmanPlus-local\RiskOfRain2\profiles\Hollow Saint Dev\BepInEx\plugins\JohnstonStu-HollowSaint`.
- Local build output: `HollowSaintMod/bin/Release/netstandard2.1/HollowSaint.dll`.
  Bundle: `artifacts/foundation/bundle13/hollowsaintassets`.
- Rollback to v0.7.13: with the game closed, restore BOTH DLL and bundle from
  `artifacts/foundation/profile-backup-20260930-144839`. Its `stage.txt` identifies
  both previous and staged hashes. See the main handoff for earlier rollback pairs.
- No push or public release is authorized. Do not spawn subagents. This request is
  for an audit; report findings before expanding into another development pass.
  The earlier one-day automation ended at 3 PM; subsequent user requests govern.

## Accepted design and controls to preserve

- Calm, floaty movement with soft trailing arms/fingers and subtle follow-through.
- Connected electricity from core/back ring to posterior upper arm, around elbow,
  under forearm and through hand/spear. Ability current is visible during attacks
  and abilities; normal authored idle material glow is separate.
- While the spear is held, hold primary for a continuous close lightning fan that
  builds Static and controls crowds. With the spear out, primary uses ranged Arc Bolt.
- Secondary recalls for free. Recall stores/catches the spear; a separate press
  with a ready secondary charge throws it and spends that charge.
- The hand has an authored fitted grasp and custom molded spear grip.
- Arc Bolt release has a brief connection that breaks; the bolt continues onward.
- Preserve accepted damage, cooldowns, charges, release markers and balance. In
  particular, the accepted Electrocute/spear spread reductions remain in place.

## What the current build contains

- Softer spear aim recovery, small acceleration/braking/turn posture accents,
  improved dash/airborne handoffs and gradual free-finger follow-through.
- Corrected imported finger interpolation: grasp remains exact through throw
  release and after catch arrival. Other curves/markers are retained.
- Shared lightning rhythm across ring/body/arm/spear/fan, with softer casting-side
  feed transfers and ability-light recovery. Death/hidden/disable paths clear effects.
- Five skin palettes: Cracked Icon, Obsidian Saint, Verdigris Relic, Solar Vespers,
  Umbral Choir. Previously cyan baked body light pixels now match the relic palettes.
- Bundle12 added finger-track corrections. Bundle13 retained those animations and
  the rig, compacted body surfaces and added shared skin light atlases. Seven shared
  1024px atlases with mipmaps add approximately 37 MiB GPU storage.
- v0.7.14 caches lighting material classifications while reading emission values
  live. It refreshes classification when the active material reference changes.

## The issue Stu just flagged — audit this first

Stu saw detached hands and a displaced/top ring near the end of the Solar GIF
`artifacts/current-flow01/skin3-handoffs.gif`. The agent had presented it as a
reviewed native preview. The visible failure was real in that clip and was missed.

Investigation identified three preview/harness faults:

1. Repeated `Camera.Render()` calls inside one editor frame reused an earlier
   GPU-skinned body pose while bones, hand pieces and lightning followed newer poses.
2. The fixture jumped from Open Circuit hold to the Halo layer's Empty state,
   skipping the real buff driver's closing animation. Write-defaults-off retained
   the unfolded ring's translations after the switch.
3. The fixture hid the held spear when entering dash/recovery, without a throw.

Corrections were to the preview/test setup. **No production DLL, model or animation
asset was changed in this correction; the installed candidate remains v0.7.14.**
Evidence supports a preview fault, but does not prove the live game is free of an
analogous pose/state problem. Independently check that distinction rather than
accepting the explanation or the passing test counts without examining it.

## Corrected evidence and limitations

- `artifacts/current-flow01/RETIRED-PREVIEW.txt`: old media is failure evidence,
  not valid evidence of visible mesh attachment.
- `artifacts/end-pose01/`: transform/curve audit and diagnostic renderer/layer A/Bs.
- `artifacts/current-flow02/skin0-handoffs.gif` through `skin4-handoffs.gif`:
  five corrected 120-frame route/pose studies using fresh CPU-baked posed meshes,
  current materials/property blocks, the halo closing clip and retained held spear.
  `all-skin-end-review.png` and `skinN-handoffs/frameNNN.png` contain contact sheet
  and original frames. Inspect especially frames 74–119, including 80 and 119.
- `artifacts/end-pose02/native-frames.gif` and its PNG folder: independent capture
  using ordinary SkinnedMeshRenderers across separate editor updates, without the
  CPU snapshot renderer. End frames were reviewed for connected wrists/closed ring.
- `artifacts/end-pose-baseline/`: intentionally skips the closing clip and fails
  the same ring-return assertion. This is a preview regression baseline, not a
  recorded game failure.
- Corrected route studies still use explicit motor/skill/buff adapters and sustain
  current around markers for visibility. They do not execute actual gameplay skill
  state machines, projectile flight/return, damage/networking, full fan/crown VFX or
  real skill cadence. Native material/line rendering also does not prove game bloom.
- CPU snapshots and ordinary native rendering have lighting differences; compare
  geometry independently and audit renderer/probe/material behavior if assessing
  exact appearance. Neither should be presented as game footage.
- Regenerate older animated media captured repeatedly in a single editor frame
  before using it to judge rendered alignment. Numerical/baked-geometry checks
  are separate evidence and are not automatically invalidated by that media fault.

## Priority audit questions

1. Are the preview fault explanations independently supported? Verify final mesh
   surfaces, not just bone positions/line endpoints. Inspect both hands/wrists,
   ring arcs/yoke, head/shoulders and held spear throughout the problem interval.
2. Does the production Animator/driver path actually keep the halo closing clip
   alive through dash, sprint, jumping, attacks and interruptions? Review masks,
   write-default behavior, rate parameters and the delayed halo-rest safety net.
   The corrected fixture supplies a closing path; it cannot validate the driver
   merely by doing so. Check rapid reopen, death, disable and model replacement.
3. Is motion/arm/aim restore ordering valid in real Update/Animator/LateUpdate
   execution, including frozen/hitch frames? Check mesh attachment, fitted grip
   through release/catch and source/final-pose ordering for HaloRing/BodyCurrentFx.
4. Do real spear state callbacks preserve free recall, charged throw, held primary,
   charge/recharge, grip/visibility and cancellation? Check network/remote visual
   state assumptions rather than treating fixture reflection assignments as proof.
5. Do all skins recolor every relevant body/hand/ring/spear/fan light? Review skin
   replacement, cached material classification, property-block persistence, idle
   reset and overlapping renderer-property writers. Check body/display parity.
6. Are lifecycle cleanups, shader assumptions and atlas/memory costs acceptable?
   Identify any blocking code/asset defect separately from subjective polish and
   gameplay-only checks. The small authored right-forearm glow-strip skin-weight
   flaw remains; it was not repaired by the preview correction.

## Source entry points

- Production pose: `HollowSaintMod/FoundationPresentation.cs`,
  `FoundationMotionPose.cs`, `FoundationArmPose.cs`, `FoundationAnimRules.cs`.
- Spear: `HollowSaintMod/FoundationKit/SpearDischarge/` — `SpearCarry.cs`,
  `SpearAimPose.cs`, `SpearMode.cs`, `SpearSkillDefs.cs`, `SpearFanState.cs`,
  `ConduitSpearState.cs`, `ConduitSpearRecallState.cs` and `ConduitSpearAnchor.cs`.
- Halo/state: `HollowSaintMod/FoundationKit/OpenCircuit/OpenCircuitPulseDriver.cs`,
  `OpenCircuitState.cs`, `OpenCircuitTuning.cs`; gesture helpers in `KitShared.cs`.
- VFX: `HollowSaintMod/FoundationKit/Vfx/BodyCurrentFx.cs`, `ArmCurrentPath.cs`,
  `HaloRing.cs`, `LightningLine.cs`, `SpearVisual.cs`, `SpearFanFx.cs`,
  `CircuitCrownFx.cs`, `DischargeLink.cs`, `SkinFxPalette.cs`, `LightningRhythm.cs`.
- Skin/body: `HollowSaintMod/FoundationSkinAnimation.cs`, `FoundationSkin.cs`,
  `FoundationLightMaps.cs`, `FoundationMaterials.cs`, `FoundationBody.cs`.
- Built model/controller: `HollowSaintUnityProject/Assets/HollowSaint/GameFoundation13/`;
  fitted spear in GameFoundation11. Controller/retained clips originate in
  GameFoundation10r1, with the GameFoundation12 finger overrides retained by 13.
- Corrected preview code:
  `HollowSaintUnityProject/Assets/HollowSaint/FxValidation/Editor/CurrentFlowValidation.cs`,
  `CurrentFlowCapture.cs`, `PosedMeshSnapshot.cs`, `EndPoseFrameCapture.cs`,
  `EndPoseAudit.cs`, `NativeRig.cs`; explicit adapters in `FxValidation/PreviewBindings.cs`.

## Existing checks and reproduction

Run native Unity jobs sequentially against this project. Do not run two editors on
the same checkout. Existing PowerShell wrappers prepare exact runtime source copies.
Unity: `C:\Program Files\Unity 2021.3.33f1\Editor\Unity.exe`.
Use `.WaitForExit()` on the Unity process; `Start-Process -Wait` previously hung on
licensing child processes. Background helpers should use `-WindowStyle Hidden`.

```powershell
pwsh -NoProfile -File tools/tests/Check-CurrentFlow.ps1
pwsh -NoProfile -File tools/tests/Check-EndPose.ps1 -Baseline
pwsh -NoProfile -File tools/tests/Check-EndPose.ps1
```

The baseline wrapper must report the expected missing-close failure, then the
corrected wrapper must pass. The async end-pose runner deliberately omits `-quit`
and exits after its EditorApplication.update captures finish.

Relevant existing results (review their actual scope before relying on them):

- CurrentFlow: 90 cases / 544,466 assertions, after preview corrections.
- EndPose: 120 native frames; four halo arcs return within 5 mm of authored rest.
- GlowRecovery: 21 cases / 2,319 assertions; all skin pairs and mannequin transfers.
- SkinLight: 2,335,904 assertions / five skins on bundle13, including geometry/maps.
- GripFlow bundle13: 240 baked poses, no sampled hand/grip intersections; 3,630
  finger checks / 242 marker poses. Sampled evidence, not every possible blend.
- Prior exact-source checks cover 700 VFX, 96 spear, 192 motion, 144 arm,
  63 presentation-controller and 234 finger clip sequences. See per-pass history.
- Wrappers: Check-GlowRecovery, Check-SkinLight, Check-GripFlow, Check-VfxFlow,
  Check-PresentationFlow, Check-FingerFlow, Check-SkinLoop and the offline
  Check-ArmLife/Check-SpearModes/Check-AnimRules/Check-Locomotion scripts.
- Build command: `dotnet build HollowSaintMod/HollowSaint.csproj -c Release --no-restore -nologo -v q`.
  Last build: zero errors, existing NU1701 warning. Access check:
  `pwsh -NoProfile -File tools/dev-profile/Check-Access.ps1`.
- Native Mono allocation accounting is unsupported: a known 65,536-byte allocation
  reported zero in skin-loop01/counter-control.txt. Old uncalibrated baseline totals
  are invalid. Do not claim zero GC, improved FPS or measured gameplay performance.

## Live-game evidence and audit deliverable

`artifacts/game-smoke0714/` verifies real version/catalog/five-skin content startup
only. Two known error lines remain: the ProBuilder shader Addressables key
`86212504c7e9f468db2300dc5932dc17` and OS ClipCursor access denial. The existing
JobTempAlloc warning also remains. No unexpected/mod errors were found in that
smoke, and the agent-owned game process closed normally. Do not call the log clean.

No hands-on gameplay input, live body spawn, combat feel, game bloom, physics or
multiplayer test was performed. Native computer controls were unavailable; do not
invent a gameplay test or work around disabled UI tools with input injection.

Deliver a concise audit with severity, concrete trigger, source/asset locations and
supporting evidence. Separate reproduced defects, plausible risks and untested
gameplay questions. Give a clear recommendation: ready for a limited playtest, or
blocked pending specific fixes. Include the minimum playtest checklist and retain
rollback instructions. Read `PLAYTEST.md` for the current player-facing checklist.
