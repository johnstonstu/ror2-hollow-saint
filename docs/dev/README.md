# Development map after 1.2.0

For the current uncommitted 1.3 candidate, start with the
[independent audit handoff](audit-handoff-1.3.md). It supersedes older audit briefs
as the current design contract and indexes final versus historical evidence.

Start with [architecture](../kit-architecture.md), the root [agent guide](../../AGENTS.md), and the [refactor record](refactor-120-results.md). The baseline is tag `v1.2.0`, commit `e44d582964bf7e69fb6e410e9eeaf2748b443197`. The historical refactor preserved that release version; the new charge abilities advance the local candidate to 1.3.0.

The active local candidate is deliberately versioned **1.3.0**. Stuart authorized private staging and gameplay on October 6, 2026. The first-playtest [refinement plan](orb-refinement-1.3-plan.md) and [result](orb-refinement-1.3-results.md) cover optional Orb fuel, easier aiming, body energy feeds, free hands during overhead gathering, and charge-powered Open Circuit. The earlier [acceptance record](charge-build-1.3-acceptance.md) and [Astra high audit](charge-build-1.3-astra-audit.md) retain their candidate identities and evidence limits. The [balance and implementation plan](charge-build-1.3-plan.md) records the initial prototype, whose metadata was still 1.2.0. Public publishing remains a separate approval.

## Normal edit loop

The latest [Orb VFX result](orb-vfx-1.3-results.md) records surface crackle,
flight trails, hit bursts and audio, with its own candidate identity and focused
Astra high review. Its [plan](orb-vfx-1.3-plan.md) states visual acceptance criteria.

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
| Shared stored-charge casts, Thundercloud, Hollowed Orb | `FoundationKit/ChargedStorm`, `Thundercloud`, `HollowedOrb` | ChargedStorm; native pose, collision and host/client acceptance |
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

## 1.3 early-game balance and player relay

The subsequent [storm pacing and Orb reach plan](storm-reach-1.3-plan.md) and
[results](storm-reach-1.3-results.md) cover the slower cloud, repeated cosmetic
return strokes and charge-dependent 18–36 m bounces. `HS_SEGMENTS=storm-reach`
records once-only cloud damage, four visual strokes per victim, and a clear
26 m enemy gap with free versus fully empowered Orbs.

See [the scoped plan](early-balance-1.3-plan.md) and [final results](early-balance-1.3-results.md) for the itemless Titan comparison, harmless owner relay, exact-default migration15, native videos and Astra audit. `early-13` tests native range, finite hits, vulnerable-owner health and ABAB alternation. `review-13` records all ability presentations; `early-balance` measures the paired Primary coefficients. `tools/release/Record-Review.ps1` captures the game window and calibrated loopback audio into local review clips. These controlled solo fixtures do not certify ordinary boss survival or multiplayer.
