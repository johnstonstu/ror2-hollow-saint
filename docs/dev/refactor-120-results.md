# Local refactor of Hollow Saint 1.2.0

Completed 2026-10-06 on `codex/refactor-120`, based on released tag `v1.2.0`
(`e44d582964bf7e69fb6e410e9eeaf2748b443197`). The subsequent repository synchronization
incorporates this refactor into `main` while retaining the newer design notes already
upstream. The prior divergent local `main` is preserved on a local backup branch.
No version bump, game/profile staging, package creation or Thunderstore publication
is part of this synchronization; the released 1.2.0 ZIP remains unchanged.

## What changed

93 source files moved into responsibility-based folders. Namespaces, class names,
serialized identities and Unity assets were preserved. The pre-move inventory is
in [refactor-map.json](refactor-map.json).

```text
HollowSaintMod/
  Plugin.cs
  Content/                 registration, body construction, content and icons
  Character/
    Animation/             presentation, pose, layers and gesture utilities
    Appearance/            skins and materials
    Rig/                   mounts, mesh split and ragdoll
  FoundationKit/
    Configuration/         tuning, feature bindings, migrations, options UI
    Shared/                common gameplay utilities and damage-source identity
    ArcBolt/ ArcStep/ OpenCircuit/ Storm/ Stormspear/
    Gaze/
      Rules/               pure policies and resource ledger
      Runtime/             state/controller integration
      Networking/          request/state/pulse transport
      Presentation/        HUD, crown, effects and feature audio
    Vfx/                   shared effects and Beat routing
  Audio/                   soundbank and shared beat sounds
  Localization/            tokens, language parsing and live descriptions
  Diagnostics/             logging and audits
  Development/             existing opt-in autopilot
```

`KitShared.cs` was decomposed into eight existing types, each under its owner.
The 727-line `KitFx.cs` was split into six existing types; the largest resulting
unit is 270 lines. `KitConfig` now orchestrates the original binding order in
65 lines, with feature partials, a 141-line migration file and separate options UI.
Configuration keys, defaults, migration ordering and live callbacks are retained.

| Measure | Release baseline | Refactor |
|---|---:|---:|
| Runtime C# files | 176 | 198 |
| Runtime files over 300 lines | 19 | 16 |
| Standalone C# check suites | 20 | 21 |
| Registered Quick checks | No common runner | 30 |

More focused files reduce the amount of unrelated code needed for a feature edit;
these metrics do not claim faster gameplay or lower runtime memory use.

## Tooling and iteration

- `tools/verify.ps1` runs an explicit check inventory with bounded concurrency,
  per-check logs, exit status and JSON reports. Build adds locked restore and a
  runtime build in ignored artifacts; Native adds scanner fixtures and installed
  game member-access verification. No scope launches the game or Unity.
- `global.json` pins the SDK. A Windows CI workflow runs Quick and retains logs.
  The workflow configuration was added locally; it has not run on GitHub.
- `tools/doctor.ps1` inventories local prerequisites. The root agent guide and
  updated architecture/development map give future work a starting point and
  distinguish historical notes from current instructions.
- `tools/bump.py` validates all inputs before writing and synchronizes Plugin,
  csproj and manifest versions. Tests cover escaping, invalid inputs, late-input
  failures and read-only drift detection. The actual version stays 1.2.0.
- Native access checking now reports missing/unresolved references as unavailable.
  CLR rectangular-array intrinsics are recognized explicitly. Synthetic fixtures
  verify public-member pass, private-member failure and unresolved-member rejection.
- Package and private-profile staging scripts now invoke common verification.
  Package candidates use unique timestamped directories, preserving released ZIPs.
  These mutating operations were not executed during the refactor.
- Legacy preview preparation rejects the missing 1.2 compatibility prerequisite
  before writing anything. All nine callers stop before launching Unity on failure.
  `.worktrees/` and animation WIP output are now ignored to help avoid storage drift.

## Verification evidence

The final `tools/verify.ps1 -Scope Native` run passed **34/34 checks**:

- 21 C# suites, including **219 complete configuration binding/migration assertions**;
- seven source/adapter PowerShell suites;
- four Python version-tool tests grouped as ToolingChecks, plus VersionParity;
- locked package restore, runtime build, native scanner fixtures and installed-member scan.

The full compile after reorganization succeeded with the same 20 warnings as the
release baseline (MMHOOK compatibility and existing obsolete development APIs).
Subsequent builds can be incremental and therefore print fewer warnings.

The compiled release/refactor comparison passed **5,164 type, field and method
contracts**, including method IL, outside `KitConfig` and its generated nested
types. Configuration is excluded because its methods were deliberately extracted;
the new suite links the complete production binder, tuning and migrations with
explicit in-memory config/presentation adapters.

Configuration fixtures cover fresh settings, revisions 1–14, future revision
preservation, migration idempotency, historical tolerant matching, recent exact
matching, nearby custom floats, integer/bool migrations, hand conversion, event
logging and live callbacks. They do not validate BepInEx's on-disk serializer.

All 34 PowerShell files in the reviewed verification/test set parsed successfully.
Preview preflight returned unavailable and left all 54 existing cached files
byte-identical. Git whitespace checking passed; release metadata and tracked
Unity/art assets have no changes.

Local evidence (ignored, reproducible where indicated):

- `artifacts/verification/20261007T025321-288282Z/results.json` and adjacent logs;
- `artifacts/refactor-120/assembly-comparison.txt`;
- `artifacts/refactor-120/preview-preflight.json`;
- `artifacts/refactor-120/baseline-runtime/HollowSaint.dll` for comparison.

## Remaining acceptance gates

No Unity rendering, native gameplay or host/client playtest was run. Native scope
means installed-assembly compatibility checking, not gameplay acceptance.

The old preview fixtures require removed `SpearAimPose` APIs. Their port must replace
string-extracted method fragments with complete source units/adapters and produce
fresh native evidence. Until then, leave the deeper `FoundationPresentation`,
`SpearCarry`, lifecycle/teardown and hook-ownership changes for the next gated phase.

[Asset inputs](asset-inputs.json) records bundle15 and its direct builder dependencies.
It is not a complete Unity dependency closure; earlier numbered assets remain
preserved. A portable end-to-end bundle rebuild is still future work.

Before treating this branch as a release candidate, complete the existing manual
single-player and multiplayer matrix. This refactor establishes the organization
and verification foundation for that next phase.
