# Development map after 1.2.0

Start with [architecture](../kit-architecture.md), the root [agent guide](../../AGENTS.md), and the [refactor record](refactor-120-results.md). The baseline is tag `v1.2.0`, commit `e44d582964bf7e69fb6e410e9eeaf2748b443197`. This local refactor does not change the release version or publish anything.

The next feature idea is preserved in [charge-up strike-call design notes](next-special-strike-call.md); implementation has not started.

## Normal edit loop

1. Read the feature owner and its check suite in [tools/checks.json](../../tools/checks.json).
2. Make a focused change with explicit behavioral acceptance criteria.
3. Run `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1` from the repository root.
4. Use `-Scope Build` for runtime compilation, or `-Scope Native` to add member-access checks against installed game assemblies. Read each run's `artifacts/verification/<timestamp>/results.json` and logs.

Quick requires Python 3.11+, PowerShell and the SDK pinned in `global.json`; it does not require LFS media, Unity or a game installation. The first run can download .NET reference packs. CI runs this scope. `tools/doctor.ps1` inventories local prerequisites without modifying them.

Build additionally needs restored packages, the LFS soundbank and a RiskOfOptions reference DLL (`HollowSaintMod/lib/RiskOfOptions.dll` or the project's `RiskOfOptionsDll` property). Native also needs the game's real Managed assemblies and BepInEx profile dependencies. Override their locations with `-Managed` and `-ProfileBepInEx`. A missing or unresolved reference cannot count as a pass.

## Where to change things

| Work | Source owner | Representative checks |
|---|---|---|
| Gaze timing, admission, recovery | `FoundationKit/Gaze/Rules` | GazeFuel, GazeRelease, GazePrimary, LandingRecovery |
| Gaze execution and packets | `FoundationKit/Gaze/Runtime`, `Networking` | GazeFuel; native host/client acceptance for packet changes |
| Gaze HUD, crown, effects | `FoundationKit/Gaze/Presentation` | GazeHud, GazePresentation, GazeAssetFailure, CrownMount |
| Stormspear and Stored Prayer | `FoundationKit/Stormspear`, `Storm` | StoredPrayer, TerrainSpear, SpearCooldown, SpearConductor, LandingRecovery |
| Configuration/default migrations | `FoundationKit/Configuration` | ConfigMigration plus ArcBolt/Stormspear default scripts |
| Pose, skin, rig | `Character` | CrimsonVisual, CrownGesture; native pose acceptance |
| Tokens and descriptions | `Localization`, `Language` | Check-Language, NonGazeDamage |
| Audio | `Audio`, feature audio policies | SpearAudio |

All paths in this table are under `HollowSaintMod`; `*Checks` projects are under `tools`. Source tests use explicit adapters and do not certify Unity rendering, BepInEx persistence or multiplayer gameplay.

## Native and release boundaries

`tools/dev-profile/Stage-Build.ps1` runs common checks and native access verification before modifying the private profile, with backups. It is a separate opt-in operation; verification itself never stages or launches. Stuart runs the [manual acceptance matrix](../manual-1.2.0-playtest.md).

`tools/release/Make-Package.ps1` runs source checks, build and native access verification. Local candidates go to a new timestamped `artifacts/candidates/` folder, preserving released ZIPs. Creating a candidate does not grant publication approval. `python tools/bump.py --check` checks all three version declarations; an explicit bump updates Plugin, csproj and manifest together.

The legacy Unity preview fixtures reference `SpearAimPose`, absent from the release source. `Prepare-FxValidation.ps1` exits with code 2 before writing any RuntimeCopies. Port the fixtures and replace method-string extraction with complete production source units before using previews as acceptance evidence. Cached copies from older branches are invalid for this refactor.

## Assets and historical material

[Asset inputs](asset-inputs.json) records the active bundle pin and direct builder dependencies; it is not a complete Unity dependency closure. Preserve earlier numbered folders and `.meta` files until an end-to-end rebuild proves independence.

[Refactor plan](refactor-plan-1.2.md) records the original review. Lifecycle teardown, deeper pose decomposition and a portable Unity build remain follow-up phases with native gates. The 1.2 research/trial notes, `TODO`, `PLAYTEST` and older candidate documents are historical evidence, not current task instructions or fresh acceptance results.
