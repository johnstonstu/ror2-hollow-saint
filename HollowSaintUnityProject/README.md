# Hollow Saint local Unity preview

**Current integration status:** the separate game foundation has now loaded and
spawned in RoR2. This README describes the older Unity art preview. For the active
game build, open issues and next steps, read
[`docs/next-agent-handoff-20260927.md`](../docs/next-agent-handoff-20260927.md).

Use **Unity 2021.3.33f1**, Built-In rendering. The installed editor is at
`C:/Program Files/Unity 2021.3.33f1/Editor/Unity.exe`.

Open **Hollow Saint > Open movement preview**, or open
`Assets/HollowSaint/Scenes/ImportProof04.unity`, then press Play and click the Game view.

| Input | Action |
|---|---|
| WASD | Camera-relative movement |
| Left Shift + movement | Glide |
| Space | Jump |
| Q | Left dash |
| E | Arc Bolt cast animation |
| R | Reset position |
| Tab | Toggle the 15-clip inspection menu |

## Verified local milestone

Source: `../art/anim/hollow-saint-anim-v31.blend`, preserved without modification.
Current imports: `Assets/HollowSaint/Source/v31_probe03`; generated assets:
`Assets/HollowSaint/Generated03`. Earlier numbered probes are retained for diagnosis.

- 137 mesh renderers and all 83 bones retained, including sash and socket bones.
- 15 representative clips preserve source frame durations and marker counts.
- Six socket/hem positions checked at every exported frame against Blender;
  peak differences are approximately 0.001 mm.
- Body base color and emission baked; all imported material slots remapped.
- Play Mode acceptance checks pass: grounding, 7.800 m run over 1.3 s,
  actual Animator state/time, 0.562 m body vertex deformation versus idle,
  braking, 0.907 m jump height, landing, 11.310 m glide over 1.3 s,
  sampled jet signal, left dash, cast and return to idle.
- Animator uses AlwaysAnimate in the preview so offscreen testing continues
  evaluating transforms and animation signals. Runtime captures were visually reviewed.

Evidence: `../artifacts/unity-setup/proof04/import-verification.txt`,
`runtime-verification06.txt`, and `Runtime_Run/Glide/Cast.png` in that folder.
Artifacts are local/ignored. Prior failed verification reports remain for diagnosis.
Runtime captures are the reliable posed renders; early direct-sampling editor
snapshots may contain stale skinned render buffers.

## Scope and next work

This is a controllable Unity preview, not an installed RoR2 survivor. Sash motion
is baked, restrained follow-through from v31, not runtime cloth simulation.
Only 15 of 65 clips are imported. Body maps are baked, while procedural armor
materials are approximated; final RoR2 shaders are not hooked up.
Jet/glow channels and markers are preserved, but no rendered ability effects,
damage, networking, survivor selection, asset bundle or mod build is provided yet.
Source full animation QA is still 15/65; remaining contact/pop/blend issues
continue to block final art sign-off.

Next: validate the export/socket contract for the complete kit, import the remaining
clips and VFX, then build the RoR2 prefab/bundle and survivor integration. Check
the local dev profile and dependencies before estimating the full in-game milestone.
All work stays local until Stuart requests a push. Nothing was committed or pushed.

## Reproducing checks

Close the interactive editor first. Run Unity with this project and
`-batchmode -executeMethod HollowSaint.Preview.Editor.ProbePlayMode.Run`.
Do not add `-quit`: the validator exits with success/failure after Play Mode checks.
The validator writes its latest report and runtime captures in the evidence folder.
Build/export scripts deliberately refuse to overwrite numbered source/generated
assets; choose a new numbered output for a fresh export, rather than rerunning Build
against existing assets. The original Blender checkpoint is never saved by export.
