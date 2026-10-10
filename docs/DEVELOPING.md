# Developing Hollow Saint

Players: see [README.md](../README.md). Contributors: read [AGENTS.md](../AGENTS.md), the [development map](dev/README.md) and [kit architecture](kit-architecture.md) before changing runtime behavior. Paths below are relative to the repo root.

## Repo

| Path | Contents |
|---|---|
| `HollowSaintMod/` | BepInEx plugin (C#, netstandard2.1). `Language/HollowSaint.language` is the in-game text. `Package/` holds the Thunderstore manifest, README, changelog and icon |
| `HollowSaintUnityProject/` | Unity 2021.3.33f1 project that builds the `hollowsaintassets` bundle (current generation: `GameFoundation11`-`15`, with clips from `GameFoundation10r1`) |
| `art/audio/` | Wwise project and the generated `HollowSaint.bnk` (the Pixabay samples stay local, see `.gitignore`) |
| `tools/dev-profile/` | Build staging into the `Hollow Saint Dev` r2modman profile, the scripted autopilot playtest |
| `tools/release/` | Packaging, clean-profile install test, README footage |
| `tools/tests/` | Offline checks for the presentation, kit math and language file (`Check-Language.ps1`) |
| `docs/` | Design and architecture docs; `docs/media/` is the README media, `docs/dev/` the playtest log and to-do list |

Preserve numbered Unity generations and unique ignored work: older assets can still be dependencies. See [asset inputs](dev/asset-inputs.json) before changing them.

## Build and test

Run from the repository root with the .NET SDK pinned in `global.json`, Python 3.11+ and PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/doctor.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -Scope Native
```

Quick runs all registered source suites without Unity or game assets. Build adds locked restore and runtime compilation. Native also checks access against installed game assemblies; it does not launch or validate gameplay. Reports, logs and the optional runtime build go to a new `artifacts/verification/<timestamp>/` directory.

Build requires the LFS soundbank and a Risk Of Options reference. The project looks for `RiskOfOptions.dll` in ignored `HollowSaintMod/lib/`, then in the `Hollow Saint Dev` profile; a direct build accepts `-p:RiskOfOptionsDll=<path>`. Native accepts `-Managed` and `-ProfileBepInEx` overrides. Missing inputs are not a pass.

Define behavioral success criteria before tests and register new `*Checks` projects in `tools/checks.json`. Offline policies, source assertions, native access scans and gameplay prove different things. The legacy Unity preview fixtures are unavailable; old RuntimeCopies do not certify current behavior.

## Gameplay acceptance

Staging a profile and launching gameplay are separate, opt-in actions. Stuart launches and playtests. After staging authorization, `tools/dev-profile/Stage-Build.ps1` verifies the build and installed member access, backs up the private profile, and stages the DLL and language file. It refuses while the game is running.

The existing autopilot requires `HS_AUTOPILOT`; its controlled solo fixtures are not ordinary survival, physical-controller or multiplayer acceptance. See the [1.3.2 polish record](dev/polish-1.3.2.md) for the current candidate's evidence and remaining manual cases.

## Release

Public publishing, pushes and media hosting each require Stuart’s explicit approval.

1. Check version parity with `python tools/bump.py --check`. A version change updates `Plugin.Version`, the project `<Version>`, and `Package/manifest.json` together, plus the changelog.
2. `tools/release/Make-Package.ps1` verifies source, runtime and native access, then creates the local ZIP in a new `artifacts/candidates/<timestamp>/` folder using the pinned bundle. Existing release outputs are preserved; creating a candidate does not publish it.
3. Record candidate DLL/ZIP hashes and gameplay results. A clean-profile test uses the generated candidate path; staging and launching it are separate actions.
4. README footage: `tools\release\Record-Showcase.ps1 -Name showcaseNN` records the showcase script (game window only), then `tools\release\Make-ReadmeMedia.ps1 -Name showcaseNN` writes the animated WebP clips and the skin lineup to `docs/media` (the showcase hides the HUD, and films the skins and the hero shot from the front). The Thunderstore README loads them from `main` on GitHub, so push before uploading.

## Logging

A normal session writes one line to the BepInEx log ("Hollow Saint <version> loaded.") plus any real warnings or errors. Config `6. Misc` > `Verbose log` turns the load diagnostics back on for bug reports; `Event log` adds the first occurrences of each gameplay event.
