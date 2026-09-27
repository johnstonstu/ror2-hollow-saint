# Technical research and setup

Inspected 2026-09-26. Web documentation and local observations are distinguished below.

## Continuation audit: current evidence

This section supersedes the earlier inspection where they differ. See
[implementation plan](implementation-plan.md) for milestone gates.

### Installed game and compiler

- Steam `appmanifest_632360.acf`: build ID **21587608**, installed depot manifest
  `5715419509320521739`.
- Local `globalgamemanagers` identifies **1.4.1**. Read-only Cecil inspection of
  `RoR2Application.AssignBuildId()` confirms it uses `UnityEngine.Application.version`.
  This is local metadata evidence, not a game-launch validation.
- RoR2.dll assembly and file version are `0.0.0.0`; do not use those as patch IDs.
- Game engine: **2021.3.33f1 (ee5a2aa03ab2)**. Only standard Hub editor found:
  **6000.5.4f1**. No Unity MCP tools are exposed in this session.
- .NET SDKs **8.0.423** and **10.0.302**, NETStandard.Library.Ref **2.1.0**.
  Installed RoR2.dll references `netstandard 2.1.0.0`.
- Game root has `.doorstop_version`, `.thunderstoremm`, `doorstop_config.ini`,
  and `winhttp.dll`, but no root BepInEx directory. Existing loader files were
  not changed. Existing Player.log could not be read in the default sandbox.

Candidate compile baseline, not yet restored or build-tested: `netstandard2.1`,
`UnityEngine.Modules 2021.3.33`, `RiskOfRain2.GameLibs 1.4.1-r.0`,
`BepInEx.Core 5.4.21`, `RoR2BepInExPack 1.41.0`. These versions are documented in
the [current R2API build configuration](https://raw.githubusercontent.com/risk-of-thunder/R2API/master/R2API.props).
Use exact versions when the prototype is created; do not use floating package
references or redistribute local game assemblies.

The [RoR2 editor guide](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Unity-Version/)
and [Unity 2021.3.33f1 release](https://unity.com/releases/editor/whats-new/2021.3.33f1)
match the engine family. Unity's [2025 security advisory](https://unity.com/security/sept-2025-01)
lists patched 2021 LTS editor **2021.3.45f2**; treat it as a candidate requiring
a minimal asset-bundle load test against this game. Do not migrate to Unity 6
or call either bundle workflow validated before a real test. The Blender
blockout and runtime placeholder prototype do not depend on asset bundles.

### Installed API signatures

Read-only metadata inspection found:

- `LightningOrb`: `bouncesRemaining`, `bouncedObjects: List<HealthComponent>`,
  `targetsToFindPerBounce`, `range`, `damageCoefficientPerBounce`,
  `procCoefficient`, `procChainMask`, `attacker`, `isCrit`.
- `BulletAttack`: `hitCallback`, `filterCallback`, `modifyOutgoingDamageCallback`,
  `Fire()` and `Fire(FireArgs)`.
- `BullseyeSearch`: `filterByDistinctEntity`, `filterByLoS`, `teamMaskFilter`,
  and distance filters.
- Damage APIs use `DamageTypeCombo`; old `DamageType` sample assumptions may fail.

These are implementation candidates, not proof that vanilla orb behavior matches
the desired finite-chain rules. Inspect implementation and validate in game.

### Blender and Higgsfield

- Blender **5.2.0 LTS** is running at
  `C:\Program Files\Blender Foundation\Blender 5.2\blender.exe`.
- Blender MCP responds. Initial live scene was unsaved, clean, Object mode, with
  only Cube, Camera and Light. Blockout work uses an isolated background process.
- Higgsfield connector balance still reports **4.85 credits / free plan** and
  a pending trial. No trial was activated. This connector balance is not the
  direct API balance.
- The local `.env` contains a populated combined ID/secret credential. A
  documented direct API **estimate-only** request returned HTTP **200**,
  **0.05 credits / USD 0.004** for the diagnostic SOUL v2 standard prompt.
  No generation was submitted and no media was uploaded. This verifies access
  to that estimate endpoint only, not mesh/animation access or remaining balance.
- Reproducible non-generating probe: `tools/higgsfield/Test-ApiAccess.ps1`.
  Credentials are read only in memory and never printed; redirects are disabled.
  Default sandbox network failed; approved network access succeeded.
- Connector catalog includes SAM 3 3D Objects, Meshy image-to-3D and multi-image
  to 3D, and rigging/animation options. Catalog availability does not establish
  direct API endpoints, pricing, rig quality or game readiness.
- User spending preference: reserve Higgsfield for useful work unavailable
  through ChatGPT/local tools. Local modeling costs no Higgsfield credits.
  Resolve model-specific docs and preflight cost before any paid operation;
  explain estimated costs before substantial batches.

Sources: [API authentication](https://docs.higgsfield.ai/docs/authentication),
[billing and estimates](https://docs.higgsfield.ai/docs/concepts/billing-and-retention),
[API FAQ](https://docs.higgsfield.ai/docs/help/faq). The API uses pay-as-you-go
account credits, separate from assumptions about the connected MCP plan.

### Dedicated development profile

Prepared `C:\Users\stuwj\AppData\Roaming\r2modmanPlus-local\RiskOfRain2\profiles\Hollow Saint Dev`
with only `mods.yml` containing `[]`. The two existing profiles, `demo time`
and `demo time new`, were preserved; their manifest hashes were checked before
and after creation. No selected-profile setting, game configuration, dependency
installation or game launch occurred. The empty profile is not playable yet.

The helper `tools/dev-profile/New-HollowSaintDevProfile.ps1` refuses an existing
destination and copies no profile data. Folder enumeration and minimal manifest
format were checked against r2modman source:
[ProfilesModule](https://github.com/ebkr/r2modmanPlus/blob/develop/src/store/modules/ProfilesModule.ts),
[ProfileImpl](https://github.com/ebkr/r2modmanPlus/blob/develop/src/r2mm/model_implementation/ProfileImpl.ts),
[ProfileModList](https://github.com/ebkr/r2modmanPlus/blob/develop/src/r2mm/mods/ProfileModList.ts).

Observed cache versions (not installed into the new profile): BepInExPack
5.4.2121, RoR2BepInExPack 1.43.0, HookGenPatcher 1.2.9, R2API_Core 5.3.0,
ContentManagement 1.0.11, Language 1.1.0, Loadout 1.0.2, Prefab 1.1.1.
The cache is not a compatibility lockfile. Resolve complete dependencies before
installation; the current runtime package page reports BepInExPack 5.4.2122.

### Deferred generated 3D starter

The user requested checking a single Higgsfield starter mesh, then explicitly
chose **Continue with local Blender for now** when website login was required.
No 3D job was submitted, no media uploaded and no Higgsfield credits spent.

The connector catalog lists Meshy `image_to_3d`, but its estimate-image tool
rejects that model with "use generate_3d instead". Neither `generate_3d` nor an
equivalent 3D estimate tool is exposed in this session. Public API docs inspected
did not establish a usable Meshy endpoint. This is an integration capability
gap, not evidence that the user's API balance is empty or plan is ineligible.
3D Jutsu website was opened as fallback and required login; user deferred it.

`art/concepts/hollow-saint-3d-input-v1.png` is a draft single-character input
created using ChatGPT's built-in image tool, with the exact prompt in
`art/concepts/prompts/hollow-saint-3d-input-v1.txt`. It is not a generated mesh.
It retains the selected identity but depicts a longer tabard and ambiguous
lower halo/hand detail; do not use it to override the reviewed blockout.

## Local observations

- The project directory was empty and contained no Git repository or project instructions.
- .NET SDKs 8.0.423 and 10.0.302 are installed; Git is available.
- The standard Unity Hub editor directory contains Unity 6000.5.4f1.
- The standard Blender Foundation directory contains Blender 5.2; editor connection not tested.
- RoR2 is installed at `C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2`.
- Its executable reports product version `2021.3.33f1 (ee5a2aa03ab2)` and file version `2021.3.33.15620650`. These identify the engine, not the RoR2 content patch.
- `Risk of Rain 2_Data\Managed\RoR2.dll` exists. Its game API/content version still needs inspection.
- Existing mod-loader-related files are present in the game folder. Their configuration was not changed.
- Listing the usual r2modman and Thunderstore profile directories returned access denied. A development profile has not been identified or configured.

## Useful foundations

### HenryTutorial: survivor example and asset workflow

[Repository](https://github.com/ArcPh1r3/HenryTutorial),
[tutorial](https://github.com/ArcPh1r3/HenryTutorial/wiki/Tutorial), and
[build instructions](https://github.com/ArcPh1r3/HenryTutorial/wiki/Building-the-Mod).

This is a public survivor template with a C# project and Unity project. The
inspected project targets netstandard2.1 and references Unity 2021.3.33 modules,
RiskOfRain2.GameLibs 1.4.1-r.0, and modular R2API dependencies. It is a useful
reference for survivor registration, skills, and asset assembly. Compatibility
with this local installation is not established yet.

The root listing did not show a license file. Check applicable reuse terms and
asset permissions before copying code or redistributing assets. No source has
been imported. The build file also contains author-specific copying steps that
must be reviewed before running a derivative build. A broad template rename
requires the complete identifier/path checklist from the user's global instructions.

### R2API: modular mod API

[Repository and MIT license](https://github.com/risk-of-thunder/R2API).
Use only the modules required by the implementation. The Orb module is relevant
if we register a custom orb type; its existence alone does not make it necessary
for reusing a built-in game orb. Inspect the current game API before choosing
the lightning implementation or proc-chain behavior.

### Community-maintained setup guidance

[First Mod](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Getting-Started/First-Mod/)
covers BepInEx, HookGenPatcher, R2API, building, and local testing.

[Unity Version](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Unity-Version/)
identifies the game's 2021.3.33f1 engine family. The local executable agrees on
the product version. Verify a compatible, appropriately patched editor before
creating the asset project; do not migrate a RoR2 template to Unity 6 by default.

## Recommended implementation order

1. Inspect the installed game's API/content version and select explicit dependency versions.
2. Write the minimal original C# foundation, using reviewed survivor examples as references.
3. Prototype primary combat with runtime-loaded placeholder visuals.
4. Identify or create a dedicated local development mod profile; preserve existing profiles.
5. Verify damage and charge in game, then add the final asset project and model workflow.

Keep local game assemblies and extracted game assets out of version control.
Local build packaging and public publication are separate operations. Public
release requires explicit approval naming the release, per the user's instructions.

## Higgsfield result

One concept sheet was estimated at 0.25 credits using `gpt_image_2_5`.
The generation request was rejected with: `Requires basic plan or higher.`
No job ID or image was returned. The prompt is preserved under `art/concepts`.

## Next-session setup detail

The user confirmed r2modman as their mod manager on 2026-09-26.
Identify a dedicated development profile path when implementation begins.
Current profile contents and dependencies remain unverified due to access denial.
