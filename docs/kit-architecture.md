# Hollow Saint kit architecture

How the survivor's code is put together, and the rules any change must keep.
Replaces `archive/kit-contract-20260927.md` and `archive/kit-wiring-api-20260927.md`,
which described an approach that did not survive contact with the game code.

## Layout

| Path | Role |
|---|---|
| `HollowSaintMod/Plugin.cs` | BepInEx entry. Tokens, server hooks, post-load verification. |
| `HollowSaintMod/FoundationContent.cs` | The mod's one `IContentPackProvider`. Loads the bundle, registers kit content, builds body/display/survivor, fills the ContentPack. |
| `HollowSaintMod/Foundation*.cs` | Body construction, skin, materials, mesh split, mounts, locomotion presentation. |
| `HollowSaintMod/KitRegistration.cs` | Registers every skill and installs the kit on the body. |
| `HollowSaintMod/FoundationKit/KitShared.cs` | `KitTuning`, `KitContent`, `DischargeMeter`, `KitAnim`, `KitUtil`, `KitLog`, `KitTokens`. |
| `HollowSaintMod/FoundationKit/<Skill>/` | One folder per skill: registration, EntityState, projectile or driver, VFX hooks. |

## Content registration

Everything goes into the mod's own ContentPack through `KitContent`. Nothing uses
R2API `ContentAddition`, so there is exactly one pack and one load path.

Order inside `FoundationContent.LoadStaticContentAsync`:

1. `KitRegistration.RegisterContent()` adds states, SkillDefs, buffs and projectiles to `KitContent`.
2. `FoundationBody.Build()` clones CommandoBody and swaps in the model.
3. `KitRegistration.InstallOnBody()` creates one `SkillFamily` per slot, points each
   `GenericSkill._skillFamily` at it (reflection), sets the passive, and adds
   `DischargeMeter` and `OpenCircuitPulseDriver` to the body.
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

State that clients need to see is stored in **buffs**, because `CharacterBody`
already replicates them. The Discharge meter is the stack count of the hidden
`HollowSaintDischargeCharge` buff; Open Circuit's window is the
`HollowSaintOpenCircuit` buff. Custom `NetworkBehaviour`s are avoided because the
plain `dotnet build` does not run the UNet weaver.

## State machines and input overlap

| Machine | Skills | Why |
|---|---|---|
| `Weapon` (vanilla) | Arc Bolt, Conduit Spear | The spear (PrioritySkill) can cut a bolt at any time; bolts (Any) wait for the spear. |
| `Body` (vanilla) | Arc Step | Owns movement while dashing; weapon skills keep firing. Jump cancels into a momentum jump. |
| `Crown` (added) | Open Circuit | Its 1.2 s cast never blocks firing and cannot be cancelled after the cooldown is spent. Networked and reset on death and stun. |

## VFX and SFX

Every audiovisual moment is a `Beat` (`FoundationKit/Vfx/KitFx.cs`). Local beats are raised
by EntityStates or replicated buff edges and run on every machine; server beats
(impacts, chain hops, pulses, discharge) go through one networked effect prefab
(`VFXAttributes.DoNotPool`) so every client sees and hears them. Visuals are built in
code from vanilla FX materials re-ramped to the kit-v2 palette (`VfxAssets`). Sounds
are vanilla Wwise events in `KitSfx.For`, the single place to swap in a custom bank.

Anything that sits on the halo reads `HaloRing` (`FoundationKit/Vfx/HaloRing.cs`), never the
`Halo` socket alone. Open Circuit makes the crown by swinging the four arc bones
(`halo 1..4`) flat above the head while the socket and `halo root` barely move, so the ring
is refitted every LateUpdate (order 150) from the arc bone heads (`HaloRingShape`, checked
against the v34 rig by `tools/tests/Check-HaloRing.ps1`). Charge orbs (`StormChargeHalo`,
160), the crown and spine feed (`CircuitCrownFx`, 170) and the pulse tendrils (the
`CircuitArc` beat carries its owner so each client starts it on its own live ring) all use it.

## Damage hooks

- **Conductor Mark** multiplies damage in an `On.RoR2.HealthComponent.TakeDamageProcess`
  hook, before health is subtracted. `GlobalEventManager.onServerDamageDealt` fires
  after health is already reduced, so changing damage there does nothing.
- **Discharge** listens on `onServerDamageDealt` (it reacts to a hit, it does not
  change it). It consumes the meter first, then fires a capped burst through
  `KitUtil.CappedBlast` (distinct entities, nearest first).
- Our hits carry `DamageSource.Primary/Secondary/Special`. `KitUtil.SourceOf` maps
  those back to a skill; item procs arrive as `NoneSpecified` and are ignored.

## Animation

`KitAnim.Play` checks the layer and state exist before playing, and logs
`HOLLOW_SAINT_ANIM_PENDING` once per missing state. The current bundle has one layer
(`Body`), so skill gestures are skipped until the controller described in
`unity-vfx-anim-spec-20260927.md` is built. State names use spaces (`Arc Bolt right`).

## Verifying a build in game

Log lines in `...\Hollow Saint Dev\BepInEx\LogOutput.log`:

| Line | Meaning |
|---|---|
| `HOLLOW_SAINT_KIT_CONTENT states=4 skillDefs=4 families=4 buffs=3 projectiles=2 effects=1` | Content reached the pack |
| `HOLLOW_SAINT_ANIM_CONTRACT_DONE driven=15 missing=0` | Body-layer presentation found every state it drives |
| `HOLLOW_SAINT_SFX_BANK <bank> result=` | Placeholder sound banks loaded |
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
