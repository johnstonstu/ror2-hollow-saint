# Hollow Saint: next-agent handoff

Updated September 27, 2026, after the successful 21:53 PT r2modman launch. This is the current integration checkpoint; older status sections in `HANDOFF.md`, `docs/foundation-current-status.md`, and `HollowSaintUnityProject/README.md` describe earlier stages and may say the game foundation has not launched.

## User decisions and boundaries

- Keep **Hollow Saint A / Cracked Icon** and the approved kit. The user approved `docs/ror2-integration-plan.md`, including its section 11 controller-first amendment. Read it, `art/MASTER-PLAN.md`, `docs/controller-acceptance.md`, and `art/vfx/VFX-ABILITY-PLAN.md` before changing mechanics.
- Stuart launches and playtests through **r2modman**. The agent builds and stages local changes, then asks for a test; do not launch or drive the game for him. Keyboard/mouse must remain supported through native RoR2 input.
- Modify only the `Hollow Saint Dev` profile. Preserve `demo time new` and `demo time` as unchanged references. The Hollow Saint plugin is installed manually, so it does not appear in r2modman's Installed Mods list.
- Work stays local. No commits, pushes, profile export, Thunderstore release, or other publication was done in this integration pass. Public release requires fresh explicit approval. Preserve numbered Blender sources and the user's open Unity/Blender sessions.
- The user offered Unity MCP or computer use if editor work becomes blocked. Neither is required for the current build; an isolated Unity 2021.3.33f1 batch project successfully diagnosed the renderer bug. Prefer the open editor for asset authoring when useful, without disturbing the user's scene.

## Verified baseline

| Item | Current evidence |
|---|---|
| Game/profile | RoR2 1.4.1, r2modman `Hollow Saint Dev`, 34 support packages copied from `demo time new` excluding AH-64. Root Doorstop files are present. |
| Plugin | `HollowSaintMod/`, `netstandard2.1`, GameLibs `1.4.1-r.0`. Installed DLL SHA256 `ABF8D74525F714C4C3AB438A7887A987112236067B30E1C45E5A5F6B1FCB11E1`. |
| Bundle | `artifacts/foundation/bundle01/hollowsaintassets`, SHA256 `852B72C7207F9BD16E572AD297ACE9E8318AD90C394438D0BC9457E10AB30B21`; installed profile bundle matched. |
| Latest playtest | Log last written 21:53 PT has one `HOLLOW_SAINT_CATALOG_CHECKS_PASS` and one `HOLLOW_SAINT_BODY_STARTED nativeInput=True nativeMotor=True authority=True`. Stuart confirmed the upper back now looks fixed in game. |
| Catalog audit | `foundation-catalog-checks.txt` has 13 passes: native input/motor/skills/equipment, 140 renderers after splitting the four body material slots, valid avatar, 23 mounts and linked hurtbox. |
| Source art | Current Blender animation checkpoint `art/anim/hollow-saint-anim-v31.blend` has 65 clips; 15 representative clips are in the Unity foundation bundle. Strict source full QA remains 15/65, not final sign-off. VFX v07 source exists but is not game-wired. |

Unity project: `HollowSaintUnityProject`, editor `C:/Program Files/Unity 2021.3.33f1/Editor/Unity.exe`, Built-In pipeline. Game prefab is `Assets/HollowSaint/GameFoundation01/mdlHollowSaint.prefab`; bundle builder is `Assets/HollowSaint/Editor/FoundationBundleBuilder.cs`. It refuses to overwrite its generated folder, so use a new numbered output for a future rebuild. The foundation still uses temporary Commando skills. The final Arc Bolt / Conduit Spear / Arc Step / Open Circuit / Discharge kit, custom SFX, production VFX, charge UI and item fitting are not implemented.

Build: `dotnet build HollowSaintMod/HollowSaint.csproj -c Release --no-restore`. The latest build passed with the existing transitive MMHOOK `NU1701` warning. Installed plugin directory: `C:/Users/stuwj/AppData/Roaming/r2modmanPlus-local/RiskOfRain2/profiles/Hollow Saint Dev/BepInEx/plugins/JohnstonStu-HollowSaint/`. Fresh log: sibling `BepInEx/LogOutput.log`. Back up the installed DLL, verify the game is stopped, stage only to this profile, and compare hashes before asking Stuart to relaunch. Do not use `tools/dev-profile/Start-Foundation.ps1` for direct game launch.

## Open issues in the latest successful log

1. Selection display: `HollowSaintDisplay` lacks `ModelSkinController` and `CharacterModel`; two `SurvivorMannequinSlotController.ApplyLoadoutToMannequinInstance` null references occurred. The game still selected/spawned, but this should be diagnosed and fixed as part of the foundation. `FoundationContent.cs` currently clones the display from the raw bundle model, while `FoundationBody.cs` adds those components only to the playable body model.
2. Animation: five `Animator.GotoState: State could not be found` and five `Invalid Layer Index '-1'` warnings in that run. Earlier longer sessions produced many more. The foundation controller contains only a `Body` layer and 15 authored states while temporary Commando skills request vanilla states/layers. Capture the exact requests, define a controller/state contract, then fix or replace their callers. Avoid adding aliases blindly.
3. Gameplay acceptance remains incomplete: physical controller feel, damage/death/respawn, equipment, keyboard parity, multiplayer, and full skill kit. `docs/controller-acceptance.md` tracks the physical tests. A connected 8BitDo controller in the first log was not proof that those checks passed.
4. Other log messages include a missing `Hidden/ProBuilder/EdgePicker` shader key, unavailable item/buff definitions, a network-message warning and JobTempAlloc warnings. Their relationship to Hollow Saint is unproven; correlate before changing unrelated systems.
5. Visuals are still foundation quality: game-only `FoundationMaterials.cs` forces rear graphite opaque/dark and caps bright emissions. It was a diagnostic pass, not a final shader solution. Compare final materials in game before removing or retaining it. The bundle uses Unity Standard materials; body renderer info currently ignores RoR2 overlays for those shaders.

## Recommended work plan

### First assignment: finish the M1 foundation and selection display

1. Snapshot the current working baseline and inspect the latest log/checklist. Preserve the working DLL/bundle and the user-confirmed back result. Read `FoundationContent.cs`, `FoundationBody.cs`, `FoundationAudit.cs`, `FoundationMeshSplitter.cs`, and `FoundationPresentation.cs` before editing.
2. Reproduce the mannequin failure from the known log. Build the display prefab with the appropriate `CharacterModel`, `ModelSkinController`, skin/render metadata and animator configuration for current RoR2. Verify selection/loadout changes no longer throw; keep the model visibly intact from front and rear.
3. Map the remaining animation warnings to actual skill/state requests. Either supply compatible controller layers/states or narrow the temporary skill animation calls while preserving native input and movement. Success is zero Hollow Saint-caused animator state/layer warnings in a basic select, move, attack, jump and sprint run.
4. Ask Stuart for one focused r2modman playtest after staging the DLL and any new bundle. Acceptance: catalog and body-start markers pass; selection/loadout work; no mannequin null refs or missing animation state/layer warnings; back stays solid; movement, camera, sprint, jump, interact, equipment, damage and death are checked with his controller plus a keyboard/mouse parity pass. Record which checks actually happened, not only that the game loaded.

### After M1: M2 model and animation contract

- Import the remaining clips in a new numbered Unity output; specify locomotion blend, glide, air/land, masked attack, additive aim, stun/freeze/death and interruption behavior. Validate source contact/pop issues rather than treating FBX import as animation sign-off.
- Keep four body material slots visible. `CharacterModel.UpdateRendererMaterials` assigns one base material per renderer; `FoundationMeshSplitter` creates one skinned renderer per body submesh at runtime. Any derived export or material consolidation must retain that invariant or replace it with another verified solution.
- Review selection portrait/display, skins, cloak/flash overlays and RoR2 shader conversion. Fit a small representative set of item displays on the 23 existing locators. Mounts alone are not display rules. Measure the current 140-renderer cost before optimizing.

### Then M3/M4: vertical combat slices

- Implement Arc Bolt first with native skill input, an aimed initial hit, bounded chains, damage provenance, server-owned successful-hit charge, and Discharge consume-on-next-hit. Test misses and item procs cannot fill charge, targets are not hit twice, and a full meter cannot recursively trigger twice. Check host and client early.
- Give that single combat slice coherent hand flash, main bolt, one chain, impact, sound and meter feedback before expanding to Spear, four-way/air Step, Circuit and the rest of VFX/SFX. The approved kit values are in `art/MASTER-PLAN.md` and `docs/ror2-integration-plan.md`; label unresolved proc/iframe/overlay choices as proposals for Stuart, rather than silently choosing them.

### M5/M6: coverage and polish

- Fit vanilla/DLC item and equipment displays; inventory the actual loaded catalogs in the reference support profile. Add explicit adapters or unsupported-display reports for modded items. Preserve gameplay even when a cosmetic follower has no safe fit.
- Test host/client, clones, item-heavy stages, cooldown/attack-speed extremes, interrupts, death/revive, stage changes and sustained VFX/SFX load. Use the acceptance matrix in `docs/ror2-integration-plan.md`. Public packaging remains a separate approval decision.

## Hiccups and diagnostic lessons

- Initial dev profile omitted `.doorstop_version`, `doorstop_config.ini` and `winhttp.dll`, causing a vanilla launch. Restoring the reference profile's Doorstop files fixed it; r2modman itself did not need an upgrade. A fuller set of support mods was also synced from `demo time new` into the dev profile only.
- Two early plugin API assumptions failed against the installed assemblies: `ChildLocator.transformPairs` and `ContentPack.identifier` were inaccessible. Use the public `ChildLocator.AddChild` and populate the content pack's public output collections, as the working code does. Check real assembly visibility before relying on a tutorial or package facade.
- The upper-back hole was real. Blender FBX, Unity prefab and the exact installed bundle all rendered the back. Material brightness, `ignoreOverlays` and skinned bounds tweaks did not fix it. Decompiling RoR2's `CharacterModel.UpdateRendererMaterials` exposed its single-material-per-renderer behavior; forcing the bundle to one material reproduced the hole, and splitting the four body submeshes restored it in Unity and the user's game. Images and full trail: `artifacts/foundation/playtest-20260927/RESULT.md` and `unity-bundle-rear-*.png` there. Do not remodel the back to address this regression.
- The first split DLL stalled startup at 99% because our own `FoundationAudit` still asserted 137 renderers. The corrected build checks 140 and each split body slot. The successful 21:53 log passed that audit. When changing model structure, update startup assertions in the same build before asking for a playtest.
- A separate Unity batch diagnostic copy in the temp directory avoided disturbing the open editor. Initial camera captures were blank due to preview-scene setup, then corrected to a normal scene. The batch executable needed sandbox escalation because Unity's user cache could not initialize inside the restricted process. Unity MCP is optional, not a prerequisite.
- `git status` hit a Git LFS temp access-denied error in this large repository. Do not reset or clean the worktree to make status succeed. Use targeted diffs/read-only inspection until the environment issue is understood. No commit was made.

## Files to start with

- `HANDOFF.md` (new top pointer, then historical context)
- `docs/ror2-integration-plan.md` and `docs/controller-acceptance.md`
- `HollowSaintMod/{FoundationContent,FoundationBody,FoundationMeshSplitter,FoundationAudit,FoundationPresentation,FoundationMaterials}.cs`
- `HollowSaintUnityProject/Assets/HollowSaint/GameFoundation01/` and `HollowSaintUnityProject/Assets/HollowSaint/Editor/FoundationBundleBuilder.cs`
- `artifacts/foundation/playtest-20260927/RESULT.md` and the rear comparison PNGs
- `art/anim/hollow-saint-anim-v31.blend`, `art/vfx/assets/hs-vfx-v07.blend`, `art/MASTER-PLAN.md`
