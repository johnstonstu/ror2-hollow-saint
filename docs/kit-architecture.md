# Hollow Saint kit architecture

Current source map for the local refactor of released 1.2.0. The runtime still
ships as one `HollowSaint.dll`; folder moves preserve namespaces and identities.
See [development workflow](dev/README.md) for commands and evidence limits.

## Layout

| Path | Role |
|---|---|
| `HollowSaintMod/Plugin.cs` | BepInEx entry. Tokens, server hooks, post-load verification. |
| `HollowSaintMod/Content/FoundationContent.cs` | The mod's one `IContentPackProvider`. Loads the bundle, registers kit content, builds body/display/survivor, fills the ContentPack. |
| `HollowSaintMod/Character/` | Animation, appearance and rig responsibilities. |
| `HollowSaintMod/Content/KitRegistration.cs` | Registers every skill and installs the kit on the body. |
| `HollowSaintMod/FoundationKit/Configuration/` | Feature binding partials, ordered migration history, tuning and options UI. |
| `HollowSaintMod/FoundationKit/Shared/` | Shared damage-source identity and gameplay utilities. |
| `HollowSaintMod/FoundationKit/<Skill>/` | One folder per skill: registration, EntityState, projectile or driver, VFX hooks. |
| `HollowSaintMod/FoundationKit/Gaze/` | `Rules` (offline policies), `Runtime` (game integration), `Networking`, `Presentation`. |
| `HollowSaintMod/Audio/`, `Localization/`, `Diagnostics/` | Soundbank and beat audio; tokens/descriptions; logs/audits. |
| `HollowSaintMod/Development/` | Existing opt-in autopilot, kept in the single assembly. |

The former `KitShared.cs` types now live with their owners: `KitContent` in
Content, `DischargeMeter` in Storm, `KitAnim` in Character/Animation, `KitLog`
in Diagnostics and `KitTokens` in Localization. [Move inventory](dev/refactor-map.json)
records old/new paths and the pre-move reference checklist.

## Content registration

Everything goes into the mod's own ContentPack through `KitContent`. Nothing uses
R2API `ContentAddition`, so there is exactly one pack and one load path.

Order inside `FoundationContent.LoadStaticContentAsync`:

1. `KitRegistration.RegisterContent()` adds states, SkillDefs, buffs and projectiles to `KitContent`.
2. `FoundationBody.Build()` clones CommandoBody and swaps in the model.
3. `KitRegistration.InstallOnBody()` creates one `SkillFamily` per slot, points each
   `GenericSkill._skillFamily` at it (reflection), sets the passive, and adds
   `DischargeMeter`, Gaze controllers/HUD and Open Circuit components to the body.
4. `GenerateContentPackAsync` calls `KitContent.PopulateInto(args.output)`.

**Why families and not `SetSkillInternal`:** `GenericSkill.Awake` runs
`baseSkill = skillFamily.defaultSkillDef` on every spawn (checked in the RoR2.dll IL),
and the loadout screen reads `skillFamily.variants`. Anything set directly on the
prefab's slot is overwritten when the body spawns. The clone also still points at
Commando's families, so never edit those, or Commando changes too.

## Networking model

EntityStates run on every machine. The owning player's copy has `isAuthority`; the
server runs its own copy (for a remote player, `isAuthority` is false there).

| Work | Where | Guard |
|---|---|---|
| Timing, aim, ending the state | owning player | `isAuthority` |
| Firing a projectile | owning player | `isAuthority`, then `ProjectileManager.FireProjectile` (relays to the server from a client) |
| Applying a buff from a state | server copy of the state | `NetworkServer.active` |
| Direct damage, AoE, chain hops, meter changes | server | `NetworkServer.active` |
| Animation and local VFX | every machine | none |

Two mistakes the earlier code made, both invisible in single player:

- Gating a projectile on `NetworkServer.active` means clients can never shoot.
- Gating server work on `hasEffectiveAuthority` means the host never runs it for a
  remote player's body.

Buffs replicate stored charge and the Open Circuit window. Gaze also has an
explicit request/state/pulse transport in `Gaze/Networking/GazeFuelTransport.cs`.
Preserve its message IDs, field order, ownership checks and cast sequencing.
The ledger separates frozen entry charges from newly earned reserves; presentation
must not authorize resource spending or healing. Custom `NetworkBehaviour`s are
avoided because plain `dotnet build` does not run the UNet weaver.

## State machines and input overlap

| Machine | Skills | Why |
|---|---|---|
| `Weapon` (vanilla) | Arc Bolt | Native primary input, with skill policies controlling spear/Gaze overlap. |
| `Spear` (added) | Stormspear | Charge/throw lifecycle and native stock/recharge behavior. |
| `Body` (vanilla) | Arc Step | Owns movement while dashing; weapon skills keep firing. Jump cancels into a momentum jump. |
| `Crown` (added) | Gaze or alternate Open Circuit | Special ownership; Gaze temporarily maps primary input to hold/release surges. |

## VFX and SFX

Shared audiovisual events use `Beat` (`FoundationKit/Vfx/Beat.cs`). Local beats are raised
by EntityStates or replicated buff edges and run on every machine; server beats
(impacts, chain hops, pulses, discharge) go through one networked effect prefab
(`VFXAttributes.DoNotPool`) so every client sees and hears them. Visuals are built in
code from FX materials (`VfxAssets`). `Audio/KitSfx.cs` maps beat sounds and
`Audio/CustomSoundBank.cs` owns the embedded custom bank. Gaze also has feature-local
presentation/audio. Keep the explicit Beat values and server/local routing stable.

Anything that sits on the halo reads `HaloRing` (`FoundationKit/Vfx/HaloRing.cs`), never the
`Halo` socket alone. Open Circuit makes the crown by swinging the four arc bones
(`halo 1..4`) flat above the head while the socket and `halo root` barely move, so the ring
is refitted every LateUpdate (order 150) from the arc bone heads (`HaloRingShape`, checked
against the v34 rig by `tools/tests/Check-HaloRing.ps1`). Charge orbs (`StormChargeHalo`,
160), the crown and spine feed (`CircuitCrownFx`, 170) and the pulse tendrils (the
`CircuitArc` beat carries its owner so each client starts it on its own live ring) all use it.

## Damage hooks

- `StormServer` owns Static, Electrocute, stored charge and its server hooks.
- `KitDamagePolicy`, spear snapshots and feature policies distinguish raw config
  coefficients from effective damage. Inherited splash/chain damage must not be
  scaled twice; `NonGazeDamageChecks` and `LandingRecoveryChecks` cover this boundary.
- Gaze admission, spend, pulse and recovery policies bound casts independently of
  presentation events. Keep server ownership, stale/duplicate rejection and owner/stage-loss behavior.

## Animation

`KitAnim.Play` checks the layer and state exist before playing, and logs
`HOLLOW_SAINT_ANIM_PENDING` once per missing state. The released controller uses
body and gesture layers; preserve layer names, pending-cast ownership, playback
parameters and state names such as `Arc Bolt right`. `FoundationPresentation`,
`FoundationArmPose` and `SpearCarry` remain substantial controllers; splitting their
state transitions is a later phase requiring native pose/input acceptance.

The active package pins bundle15, with earlier numbered assets as dependencies.
See [asset inputs](dev/asset-inputs.json). Historical animation design documents
do not replace the released controller contract.

## Verifying a build in game

Log lines in `...\Hollow Saint Dev\BepInEx\LogOutput.log`:

| Line | Meaning |
|---|---|
| `HOLLOW_SAINT_KIT_CONTENT ...` | Registered content counts; compare with the tested baseline |
| `HOLLOW_SAINT_ANIM_CONTRACT_DONE ... missing=0` | Presentation found the states checked by its contract |
| `HOLLOW_SAINT_SFX_BANK ... result=` | Soundbank load result |
| `HOLLOW_SAINT_KIT_INSTALLED slots=4` | Families installed on the prefab |
| `HOLLOW_SAINT_KIT_VERIFIED` / `_VERIFY_FAILED` | After catalogs load: each slot resolves to our SkillDef |
| `HOLLOW_SAINT_BODY_STARTED ... skills=...` | A live body spawned with these skills |
| `HOLLOW_SAINT_EVENT <NAME> #n server=` | First 3 occurrences of each gameplay event |

Build and stage: `powershell -ExecutionPolicy Bypass -File tools\dev-profile\Stage-Build.ps1`
(backs up the staged DLL to `artifacts/foundation/profile-backup-*`, refuses while the game runs).

## Standing rules

- Stuart launches and playtests. Never automate the game or r2modman.
- Only the `Hollow Saint Dev` profile is modified. `demo time` and `demo time new` are references.
- The vanilla Steam install is read-only; the profile shares its `Managed` folder, so a DLL copied there does nothing.
- No `KeyCode`, `Input.GetKey` or joystick polling. Input stays native.
- Local commits only; no push or release without Stuart saying so.
- The 140-renderer / 4-slot body split checked by `FoundationAudit` must survive any re-export.
