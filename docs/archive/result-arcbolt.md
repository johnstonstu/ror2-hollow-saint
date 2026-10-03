# RESULT — Arc Bolt (primary skill)

**Status: compiles clean, nothing verified in game.** Every RoR2 API used was
decompile-verified against the installed assemblies (GameLibs nuget package for
signatures; the vanilla install's `RoR2.dll` under
`C:/Program Files (x86)/Steam/steamapps/common/Risk of Rain 2/` — read-only — for
real method bodies where behaviour mattered). No file outside
`HollowSaintMod/FoundationKit/ArcBolt/` was touched. Nothing was written into the
vanilla install, any r2modman profile, or the staged plugin DLL.

## Files created

| File | Contents |
|---|---|
| `ArcBolt/ArcBoltSkillDef.cs` | `ArcBoltSkillDef` (SkillDef factory) + `ArcBoltRegistration` (one-call registration) |
| `ArcBolt/ArcBoltState.cs` | `ArcBoltState` — the cast `EntityStates.BaseSkillState` (auto-repeat, fire timing, interrupt gate, server-only spawn) |
| `ArcBolt/ArcBoltProjectile.cs` | `ArcBoltProjectile` (prefab builder) + `ArcBoltProjectileImpact` (`IProjectileImpactBehavior`, server-authoritative hit → chain → charge) |
| `ArcBolt/ArcBoltChain.cs` | `ArcBoltChainServer` — hop chain (falloff, exclude previous target) + Discharge charge award |
| `ArcBolt/RESULT-ARCBOLT.md` | this document |

## Public API surface for the coordinator

```csharp
using HollowSaint.FoundationKit.ArcBolt;

// 1. In Plugin.Awake (before content collection), ONE call does everything:
Skills.SkillDef arcBolt = ArcBoltRegistration.RegisterArcBolt();
//    Returns the registered SkillDef (idempotent; null + logged error on failure).
//    Internally: R2API.ContentAddition.AddEntityState(typeof(ArcBoltState)) →
//    ArcBoltProjectile.EnsurePrefab() + AddProjectile → BuildSkillDef + AddSkillDef.

// 2. Re-point the primary slot (coordinator-owned wiring, per kit-wiring doc):
//    primarySlot.SetSkillInternal(ArcBoltRegistration.SkillDef);
```

Members other agents may reference:

- `ArcBoltRegistration.SkillDef` — the registered SkillDef.
- `ArcBoltRegistration.ArcBoltStateType` — `SerializableEntityStateType` of the cast state.
- `ArcBoltProjectile.Prefab` / `ArcBoltProjectile.EnsurePrefab()` — the projectile prefab.
- `ArcBoltChainServer.ResolveConfirmedHit(attackerBody, victim, position, damage, crit)` —
  available for other skills that want the same "confirmed hit → Discharge charge" contract,
  though the hop chain is Arc Bolt-specific.
- `ArcBoltState.BoltReleaseNormalizedTime` (0.15), `ArcBoltState.ClipFrames` (20).

## Wiring decisions the coordinator must know (verified, with receipts)

These **correct two findings** in `docs/kit-wiring-api-20260927.md`; they change the wiring
model, so please re-verify before staging:

1. **`EntityStates.EntityState` is a plain class, not a MonoBehaviour.** Its
   `gameObject`/`transform`/`GetComponent` members are thin wrappers over `outer`
   (the owning `EntityStateMachine`); `EntityStateCatalog.InstantiateState` creates
   instances with `Activator.CreateInstance(stateType)`. **Custom states therefore
   cannot be "attached as child GameObjects on the body prefab"** — that path cannot
   work for `EntityState` (it is not a `Component`). The state is delivered to the
   machine entirely through the `SkillDef`, as vanilla does.
2. **`SerializableEntityStateType` IS constructible — the "not found" result was a
   namespace artefact.** The type lives in `EntityStates`, not `RoR2`
   (`ilspycmd -t EntityStates.SerializableEntityStateType` resolves it fine). Real
   decompiled bodies (vanilla `RoR2.dll`): the struct stores
   `value.AssemblyQualifiedName` and resolves via `Type.GetType(_typeName)`, which
   works for loaded mod assemblies. It has public `(string)` and `(Type)`
   constructors, and `R2API.ContentAddition.AddEntityState(Type, out bool wasAdded)`
   returns a correctly-built one. This code uses exactly that path, so the "prefer
   activationStateMachineName only" workaround is unnecessary here.
3. **Actual activation chain (real decompiled bodies):**
   `SkillDef.OnExecute` → `skillSlot.stateMachine.SetInterruptState(InstantiateNextState(...), interruptPriority)`
   → `EntityStateCatalog.InstantiateState(ref activationState)` →
   `Activator.CreateInstance` (→ `ISkillState.activatorSkillSlot = skillSlot`).
   `GenericSkill.PickTargetStateMachine` (private, real body) resolves the machine via
   `EntityStateMachine.FindByCustomName(base.gameObject, skillDef.activationStateMachineName)`.
   **Consequence: `activationStateMachineName` must name the machine on the body.**
4. **Machine name = `"Weapon"`** (`ArcBoltSkillDef.ActivationStateMachineName`).
   The foundation clones CommandoBody, whose machine carries `customName = "Weapon"`
   (Commando's own SkillDefs use exactly this). **The coordinator must leave the
   cloned body's `"Weapon"` `EntityStateMachine` untouched when re-pointing the
   primary slot**, or `PickTargetStateMachine` resolves null and the skill silently
   never fires. No new machine needs to be created.
5. **Interrupt-gate mechanics (real body):**
   `EntityStateMachine.CanInterruptState(p) = (nextState ?? state).GetMinimumInterruptPriority() <= p`.
   `ArcBoltState.GetMinimumInterruptPriority` returns `PrioritySkill` before the
   interrupt marker and `Skill` after — which is precisely "the next shot interrupts
   the current one from frame 13" when ArcBolt's own SkillDef
   `interruptPriority = PrioritySkill`. No custom timer machinery needed.
6. **R2API.ContentAddition is available without a csproj change** — it is a transitive
   dependency of R2API.Prefab (verified in `packages.lock.json`:
   `R2API.ContentManagement 1.0.10`), and the project already uses PrefabAPI.
   `AddProjectile` validates that the prefab has a `ProjectileController` and warns
   when no ghost is assigned.
7. **Server authority:** spawn path `ArcBoltState.FireBoltServer` is gated on
   `isAuthority` → `NetworkServer.active` + `characterBody.hasEffectiveAuthority`.
   Damage path `ArcBoltProjectileImpact.OnProjectileImpact` applies damage only under
   `NetworkServer.active` (mirroring vanilla `ProjectileSingleTargetImpact`, whose real
   decompiled body does exactly this), then `ArcBoltChainServer` re-gates on
   `NetworkServer.active`, and `DischargeMeter.AddCharge` guards server on its own.
   Clients never spawn, damage, chain, or charge.

## Required animation clips and layers (declared, per contract — not assumed to exist)

From the Blender catalog (24 fps, 1-based frames):

| Clip | Frames | Markers used | Normalized time used |
|---|---|---|---|
| `ArcBoltRight` ("Arc Bolt right") | 1–20 | Anticipation, **Bolt release** (f3), **Interrupt** (f13), Recovered | fire = 0.15 (`BoltReleaseNormalizedTime`); interrupt = 12/20 (`KitTuning.ArcBoltInterruptNormalizedTime`); end = 1.0 |
| `ArcBoltLeft` ("Arc Bolt left") | 1–20 | same | same |

- **Layer:** full-body `"Body"` layer is what the code passes today (PROPOSAL — see
  below). If an additive upper-body layer is introduced later (Arc Step / Open
  Circuit both want one), Arc Bolt can move to it by changing the single constant
  `ArcBoltState.AnimationLayerName`.
- **Animator parameter:** `ArcBolt.playbackRate` (float) — PlayAnimation scales the
  clip by `duration`, standard RoR2 pattern (`FirePistol2` precedent).
- **Animator state names:** `ArcBoltRight` / `ArcBoltLeft` — the Unity controller must
  expose the imported clips under these names (or the constants must be edited to the
  actual clip names). **Not verifiable from here** — only 15/65 clips are in the
  current bundle and the controller is coordinator-owned.
- The state plays one clip per cast, alternating hands per activation (static
  counter — see Proposals).

## Required VFX

| Slot | Field/hook | Status |
|---|---|---|
| Projectile ghost | `ProjectileController.ghostPrefab` on the clone (Mage bolt ships one) | **inherited from template** — replace with Hollow Saint bolt ghost when art exists |
| Impact effect | `ArcBoltProjectileImpact.impactEffectPrefab` (public field, `EffectManager.SimpleImpactEffect` on server) | **null — drop-in hook, no code change needed when art lands** |
| Muzzle flash | not implemented | open slot; `EffectManager.SimpleMuzzleFlash(prefab, gameObject, muzzleName, false)` is the verified call |
| Trail / flight loop | inherited from Mage bolt template | replace later |
| Skill icon | `skillDef.icon` (Sprite) | **null — required art** |

## Tuning constants used (all from `KitTuning`, none changed)

- `ArcBoltInterval` 0.5 → `skillDef.baseRechargeInterval` (0.5 s auto-repeat cadence)
- `ArcBoltDamageCoefficient` 1.0 → projectile damage = coefficient × `damageStat`
- `ArcBoltMaxChainTargets` 4 → hop count cap
- `ArcBoltChainRange` 12 → `BullseyeSearch.maxDistanceFilter`
- `ArcBoltChainFalloff` 0.75 → each hop's damage multiplier (compounding)
- `ArcBoltProjectileSpeed` 80 → **NOT yet applied** — see Unverified/Issues
- `ArcBoltRadius` 0.6 → projectile `SphereCollider.radius`
- `ArcBoltInterruptNormalizedTime` 12/20 → interrupt gate
- Discharge: `DischargeMeter.AddCharge(1f)` per **confirmed hit** (per shot, not per hop —
  "~10 Arc Bolt hits fills the meter"); source tagged `HsDamageSource.ArcBolt` via
  `KitUtil.IsHollowSaintSkillDamage`.

## PROPOSAL decisions (parked; defaults chosen, flagged in code)

1. **Proc coefficient 1.0** (projectile + hops). No approved number exists; 1.0 is the
   vanilla default (`DamageInfo.procCoefficient = 1f`). Flagger: affects on-hit item
   stacking from chains.
2. **Chain hop Discharge charge = none.** Only the confirmed primary hit charges the
   meter; hops deal damage but add no charge. Alternative: charge per hop
   (meter would fill in ~2 full chains). Stuart's "~10 Arc Bolt hits" reads as
   primary hits; flagged for M5.
3. **Left/right hand alternation is a static counter** that increments per cast,
   because an interrupt reconstructs the state parameterlessly
   (`SkillDef.InstantiateNextState` → `Activator.CreateInstance`) and per-instance
   hand state cannot survive the cut. Cosmetic only.
4. **Auto-repeat via vanilla held-input loop, not self re-entry.** The state returns
   to main when the cast ends; the vanilla primary input loop re-executes while the
   button is held (`mustKeyPress = false`). This matches the verified vanilla
   dispatch (`SkillLocator`/`GenericSkill` input path) without the state spawning
   copies of itself. If playtesting shows the 0.5 s cadence drifts (e.g. input
   polling adds a frame), the fix is a self re-entry branch guarded on
   `activatorSkillSlot.IsReady()` inside the `fixedAge >= duration` block — left out
   of v1 to avoid double-charging edge cases.
5. **Chain team filter uses `TeamMask.GetEnemyTeams(attackerTeam)`** with no
   line-of-sight requirement (`filterByLoS = false`) so bolts chain through walls —
   matches "auto-chains between nearby enemies" reading; LoS chaining would be a
   one-line change.
6. **Projectile template** = Mage lightning bolt (`Assets/RoR2/Base/Characters/Mage/Skills/MageLightningboltBasic.prefab`,
   addressables-verified path; legacy mapping
   `"Prefabs/Projectiles/MageLightningboltBasic"` UNVERIFIED at runtime — see below).

## Unverified / known risks (honest list — nothing here is playtested)

1. **Nothing is verified in game.** No launch, no log, no playtest. Everything above
   is compile- + decompile-verified only. "Unverified by construction."
2. **Projectile template load path.** `LegacyResourcesAPI.Load("Prefabs/Projectiles/MageLightningboltBasic")`
   is *inferred* from the foundation's working `"Prefabs/CharacterBodies/CommandoBody"`
   mapping and the addressables-verified asset path
   `Assets/RoR2/Base/Characters/Mage/Skills/MageLightningboltBasic.prefab`. Whether the
   **legacy `Resources.Load` map** (not addressables) still carries projectile prefabs
   under that exact name could not be confirmed from the install's assets (the legacy
   map lives inside Unity serialized data I could not enumerate read-only). If the
   load fails, the error is logged loudly and the skill fires nothing rather than
   throwing. Fallback options for the coordinator: (a) another vanilla projectile,
   (b) R2API-addressables load of the verified addressables path, (c) building a
   minimal projectile prefab from scratch.
3. **`ArcBoltProjectileSpeed` (80) is not applied.** Speed on the template clone would
   be set on `ProjectileSimple.desiredForwardSpeed` (API verified), but the correct
   combination with the Mage template's existing speed + whether the clone keeps
   vanilla speed could not be decided without running the game. **Coordinator request:
   decide whether to set `desiredForwardSpeed = KitTuning.ArcBoltProjectileSpeed` on
   the clone (one line in `ArcBoltProjectile.EnsurePrefab`) and confirm in playtest.**
4. **Clip names / animator layer name in the Unity controller.** Constants are
   guesses (`"Body"`, `"ArcBoltRight"`, `"ArcBoltLeft"`, `"ArcBolt.playbackRate"`);
   the real names depend on how the clips are imported. Must be reconciled when the
   clips land in the bundle (coordinator-owned import).
5. **`EntityStateCatalog` registration timing.** `R2API.ContentAddition.AddEntityState`
   throws internally (logged, not crashing) if the `EntityStateCatalog` has already
   initialized — i.e. `RegisterArcBolt()` must run in `Plugin.Awake` (BepInEx load
   phase), not later. The `Registered` flag makes a double call harmless.
6. **Discharge interplay.** `DischargeMeter` is coordinator/passive-agent-owned. This
   code degrades quietly (no crash, no charge) if the meter component is not yet
   attached to the body when Arc Bolt hits. The passive agent must attach
   `DischargeMeter` to the Hollow Saint body prefab.
7. **Coordinator request (no code written outside my folder):** FoundationContent /
   Plugin must (a) call `ArcBoltRegistration.RegisterArcBolt()` in `Awake`, (b) wire
   the returned SkillDef into `SkillLocator.primary` via
   `GenericSkill.SetSkillInternal`, (c) register the two language tokens
   (`KitTokens.ArcBoltName`, `KitTokens.ArcBoltDesc`) — this file never calls
   `LanguageAPI`, and (d) reconcile animation constants with the real imported clips.
