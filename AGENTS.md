# Hollow Saint development

## Baseline and scope

- Runtime refactors start from released `v1.2.0` (`e44d582964bf7e69fb6e410e9eeaf2748b443197`). The historical `v09` branch is divergent; do not merge it as cleanup.
- Read `docs/dev/README.md` and `docs/kit-architecture.md` before changing runtime behavior.
- Keep work local. Publishing, pushing, staging a game profile, and launching gameplay are separate actions. Stuart launches and playtests the game.
- Read files before overwriting/deleting them. Inventory identifiers and paths before renames, including strings, tests, docs and build outputs.

## Find the right owner

- `HollowSaintMod/Content`: body construction and registration.
- `Character/Animation`, `Character/Appearance`, `Character/Rig`: presentation and model support.
- `FoundationKit/<feature>`: gameplay; Gaze separates `Rules`, `Runtime`, `Networking`, `Presentation`.
- `FoundationKit/Configuration`: bindings by feature, ordered migrations, optional options UI.
- `Audio`, `Localization`, `Diagnostics`, `Development`: bank/audio mapping, language, logs, opt-in autopilot.
- Folder moves intentionally retain existing namespaces. Do not rename serialized/network identities to match folders.

## Verification

Run from the repository root with .NET SDK from `global.json`, Python 3.11+ and PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/doctor.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -Scope Native
```

Quick runs all registered source suites without Unity/game assets. Build adds locked restore and runtime compilation. Native adds installed-assembly access verification, **not gameplay execution**. Reports and logs live in `artifacts/verification/<timestamp>/`. Register new `*Checks` projects in `tools/checks.json`; missing registrations fail verification.

Define behavioral success criteria before tests. Prefer linked production code with explicit adapters. A source assertion, analytical model, native access scan, preview render and multiplayer playtest prove different things; label evidence accurately.

## Compatibility boundaries

- Preserve plugin GUID, assembly identity, content names, language tokens, config section/key/default/migration order, Beat enum values, message IDs/field order and Unity `.meta` GUIDs.
- Damage, resource spending and healing are server-owned. Authority decides input/aim; do not gate client projectile requests on server-only ownership.
- Use native skill/input paths, never `Input.GetKey`, `KeyCode` or raw joystick polling.
- Preserve animation callback order, layer/state/socket names and authored timing. Pose-controller changes require native acceptance.
- Log failures with operation context. Preserve deliberate content-load/VFX fallbacks; do not make an optional effect failure abort the game's startup.

## Assets and storage

- Unity numbered folders are dependencies, not automatically obsolete backups. See `docs/dev/asset-inputs.json` before touching assets.
- Avoid full Unity worktree copies. Keep generated output in ignored `artifacts/`; `.worktrees/` and `art/anim/wip/` are ignored.
- Never manually delete `.git/lfs`. Never broadly remove ignored files: they can contain unique authored work.
- `Prepare-FxValidation.ps1` currently reports unavailable before writing: legacy fixtures require a component removed before 1.2. Existing RuntimeCopies are not evidence of compatibility.
