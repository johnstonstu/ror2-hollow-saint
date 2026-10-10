# Hollow Saint 1.3.1: audit handoff

Updated 2026-10-09 19:59 PDT after the audit fixes. Nothing committed, tagged or pushed. Commit, tag and push wait on Stu.

## State

| What | Where |
|---|---|
| Repo | `C:\Users\stuwj\Documents\Coding\ror2-lightning`, branch `main` |
| Last commit | `07ddb210` (1.3.0, tag `v1.3.0`, on GitHub, never on Thunderstore) |
| Working tree | 1.3.1, uncommitted; include the new files listed below; `art/` scratch stays out |
| Package | `artifacts/candidates/20261009-195855-9547336/JohnstonStu-Hollow_Saint-1.3.1.zip` |
| DLL | sha256 `7EA00D6AAF06BAA1C7F7D926B7B818E0E9167375EC98791641374AFB0302A813`; **not staged** (dev profile still has E551B839...4947) |
| verify.ps1 | 35/35 Native-scope checks pass; `artifacts/verification/20261010T025709-123696Z/results.json` |
| Access check | ACCESS_CHECK_PASS |

New, untracked files:
- `HollowSaintMod/Development/DevAutopilot.Accept131.cs`
- `HollowSaintMod/FoundationKit/Gaze/Rules/GazeFocusPolicy.cs`
- `docs/dev/balance-1.3.1.md`
- `docs/dev/audit-handoff-1.3.1.md`
- `HollowSaintMod/FoundationKit/Gaze/Runtime/GazeFocusTracker.cs`
- `tools/ChargedStormChecks/CloudAuditChecks.cs`
- `tools/GazeReleaseChecks/FocusAdapters.cs`
- `tools/GazeReleaseChecks/FocusLifecycleChecks.cs`

The user-facing change list is `HollowSaintMod/Package/CHANGELOG.md` (1.3.1 section). Balance reasoning is in `docs/dev/balance-1.3.1.md`.

## What 1.3.1 contains

1. **Game patch fix.** The October 2026 patch added `ProjectileController.ghostPrefabAddress`, and that address beat our cloned `ghostPrefab`, so Arc Bolt and Stormspear showed Artificer's bolt. `Vfx/Ghosts.Assign` sets the ghost and clears the address/reference fields by reflection. Callers: `ArcBoltProjectile`, `StormspearProjectile`.
2. **Balance pass.** Arc Bolt 1.9 coefficient with proc 1.0. Death discharge at 0.3 Static. Orb and Thundercloud damage and cooldowns. Circuit pulse 0.8. Spear full charge 12.5. Level damage 20% of base.
   - Migrations rewrite only exact old defaults.
   - Main counter "Defaults version" is 17.
   - Separate counter "Charged defaults version" is 3. It runs after BindChargedStorm, because Orb and Cloud bind after ApplyMigrations.
3. **Gaze.**
   - Surges are removed (legacy config toggle, off).
   - All charges go into one opening blast, radius 6 + 2 per charge, capped at 20.
   - Focus ramp (`GazeFocusPolicy`): up to +100% over 3 s, 0.5 s grace, 1 s decay, 5 tiers with audio steps.
   - Range 90.
   - Knockback immunity through `CharacterMotor.ApplyForce` / `ApplyForceImpulse` hooks (`GazeArmor`).
4. **Thundercloud.**
   - Higher spawn (18 to 28 m) and column targeting.
   - Attack-speed scaling, captured at cast, max 2x rate.
   - Early end: press Special again for a refund of cooldown x 0.5 x remaining fraction, added to `rechargeStopwatch` in OnExit.
   - Free cast with an empty bank, fixed via `followsCloudFreeCast`.
   - Strike SFX/VFX pass.
   - The body current no longer stretches to the cloud (`BodyCurrentFx`).
5. **Orb.**
   - Backup Magazine gives +1 hit per magazine, capped at 24, with `dontAllowPastMaxStocks`.
   - Repeat-hit proc 0.25.
   - Bigger: 0.9 m + 0.15 m per charge, cap 2.5.
   - Charge-up sound, plus a flash and electric crackle when full.
6. **Audio and feel.**
   - Thunderbolt crack + boom.
   - Charge-gain pop and chime on the halo.
   - Spear charge rise, step chimes, and an electric full cue (`Play_loader_R_shock` + `Play_captain_m2_tazer_impact`, no beep).
   - Spear sheath breathes at full.
7. **Logging.** Normal sessions print one Hollow Saint line. Everything else is gated behind Verbose / Event log.
8. **Docs.**
   - CHANGELOG.
   - READMEs: root + Package, en / zh-CN / ru / pt-BR.
   - Version 1.3.1 in Plugin.cs, the csproj and the manifest.

## Audit fixes completed

The three P2 findings from `artifacts/audit-131/audit-report.json` are addressed:

- **Thundercloud spare stock:** the skill admission gate reserves Special for dismissal while a released Cloud owns its state machine. Source checks exercise both native-input-first and dismissal-first ordering, spare stock and a full stock reset; no extra cast is consumed, and refund only advances a missing stock.
- **Gaze focus lifetime:** history now lives on the attacker's `GazeFocusTracker` component. Each body prunes idle/dead targets independently, clears on death/disable/stage change, and retains history through configurable grace/fade windows. No static attacker dictionary remains.
- **Cloud attack speed:** the bounded configured interval is divided by a speed multiplier clamped to 1–2. Tests cover base intervals 0.25–3 seconds, partial scaling, high speed and invalid input.
- **Documentation:** Chinese Backup Magazine is now `备用弹夹`; Russian `Запасной магазин` and Portuguese `Pente Reserva` match installed-game localization. Both Portuguese README summaries use `Nuvem Trovejante`. All four in-game language versions now describe Cloud attack speed/dismissal and Orb magazine hits, with refund and hit values taken from settings.

`ChargedStormChecks`: 2424 assertions passed. `GazeReleaseChecks`: 417 assertions passed, including the linked production tracker with explicit Unity lifetime adapters. Full Native verification passed all 35 checks. The package command also passed its source, language and native access gates. NuGet vulnerability metadata was unavailable (NU1900); compilation succeeded with existing compatibility/obsolete-API warnings.

Final ZIP DLL, standard Release output and isolated Native-verification output are byte-identical. The ZIP's manifest, README, changelog and language file match source. The intermediate `20261009-195815-8122241` candidate is marked `REJECTED.txt` because it retained the old DLL; use only the final candidate in the table above.

No game/profile staging, gameplay, commit, tag, push or upload was performed for these fixes. Remote multiplayer, Lysate Cell/stock-reset dismissal in-game, and listening to the electric full-charge cue remain Stuart's native acceptance steps. The earlier playtest evidence below does not certify this new DLL.

## Earlier gameplay evidence

- **accept-131 autopilot** (`artifacts/accept131-r3`), all RELEASE_PASS:
  - Orb with two magazines lands 5 hits on a lone target.
  - Focus goes 1.0 to 2.0 and resumes at 1.13 after a 2.6 s look-away.
  - Knockback 0 during Gaze, 199 outside it.
  - Cloud: 6 strikes, no strikes after dismiss, cooldown refunded.
- **Default smoke autopilot** (`artifacts/smoke131-r3`): complete, errors=0. The only errors in the log are vanilla or other mods:
  - ProBuilder shader InvalidKeyException
  - ClipCursor
  - RoR2BepInExPack FixNonLethalOneHP
- **Not run in-game:** the earlier E551B839 build introduced the electric full cue, which remains unheard in-game. The final audit-fixed 7EA00D6A build also has not been playtested.

## Audit scope and remaining native acceptance

1. **Networking and authority.** These are server-side; check client and host paths:
   - Cloud dismiss: `StoredChargeState.RequestDismiss` / `ServerRequest` / `StoredChargeDriver.DismissClouds`, plus the resend every 0.1 s.
   - The refund calculation.
   - Per-body Gaze focus cleanup in `GazeFocusTracker` (covered by source lifetime checks).
   - Multiplayer has never been playtested.
2. **Ghost fix robustness.** `Ghosts.Assign` uses reflection over `ghostPrefabAddress` / `ghostPrefabReference`. It must stay safe on both the pre-patch compile libs (GameLibs 1.4.1) and the live game.
3. **Hooks.** The `GazeArmor` ApplyForce hooks must always call `orig` when not immune. Check for unhook on disable and any per-frame allocation.
4. **Migrations.** Two counters (17 and 3). Confirm a config stamped by 1.3.0 migrates exactly once, and that user-edited values are untouched.
5. **Cooldown edge cases.** Cloud dismissal/refund with Lysate Cell or stock resets, the free cast, and death during the storm. Backup Magazine affects Orb hits, not Special stocks.
6. **Translations.** Machine-translated README and language tokens.
   - Item names verified against installed-game tokens; Chinese spelling corrected in both READMEs.
   - Portuguese Thundercloud naming is consistent in both READMEs and the skill token.
7. **Balance numbers.** Verify README/CHANGELOG numbers against defaults: KitTuning, ChargedStormTuning, StormspearTuning, GazeTuning.

## Before release (after the audit)

1. Optional: bump the manifest minimum RoR2BepInExPack to 1.44.0.
2. Rebuild the package if anything changes: `tools\release\Make-Package.ps1`.
3. Commit (leave `art/` out), tag `v1.3.1`, push. Copy the zip to `artifacts/release`. Stu uploads to Thunderstore.

## Known open items (not blocking)

- Stale charge-13 autopilot segment.
- Earlier AccessScannerFixtures failure on PS5 was not reproduced: the current Native-scope fixture check passes.
- Open Circuit ignores attack speed.
- Multiplayer untested.
- EnemiesReturns is in the dev profile (Stu doesn't want it).
- The Static halo "bank full" cue still uses the old chime. Stu only asked for the Spear and Orb cues to change.
