# RESULT — Conduit Spear (secondary) + Discharge passive (consume side)

**Status: compiles clean (`dotnet build` → 0 errors, only the expected pre-existing
NU1701 MMHOOK warning + one pre-existing CS0162 in ArcStep), decompile-verified, and
nothing is verified in game.** Every RoR2 API used was checked against the installed
GameLibs assembly (`C:/Users/stuwj/.nuget/packages/riskofrain2.gamelibs/1.4.1-r.0`),
and where behaviour mattered (impact dispatch, `SerializableEntityStateType`, firing
pattern) against real decompiled method bodies in the vanilla install's
`RoR2.dll` (**read-only** — nothing was written to the vanilla install, any r2modman
profile, or the staged plugin DLL, and the game was never launched). No file outside
`HollowSaintMod/FoundationKit/SpearDischarge/` was created or modified.

## Files created (all inside `HollowSaintMod/FoundationKit/SpearDischarge/`)

| File | Contents |
|---|---|
| `SpearDischargeTuning.cs` | `SpearDischargeTuning` — folder-local tuning (marked PROPOSAL where unapproved) + animator layer/state name constants |
| `ConduitSpearState.cs` | `ConduitSpearState : EntityStates.BaseSkillState` — the cast state: UpperBody gesture, release at `KitTuning.ConduitSpearReleaseNormalizedTime` (0.30), server-only spawn |
| `ConduitSpearSkillDef.cs` | `ConduitSpearSkillDef` (SkillDef factory, approved 5 s cooldown) + `ConduitSpearRegistration.RegisterSpear()` (one-call registration) |
| `ConductorMark.cs` | `ConduitSpearProjectile` (prefab builder, 150 m/s) + `ConduitSpearImpact` (`IProjectileImpactBehavior`: 450% damage → charge → mark) + `ConductorMarkServer` (registered BuffDef + server amplification) |
| `DischargeEmitter.cs` | `DischargeEmitter` — the CONSUME-AND-FIRE passive + the shared charge-award entry point |
| `RESULT-SPEARDISCHARGE.md` | this document |

## Public API surface — what the coordinator and the Arc Bolt agent call

```csharp
using HollowSaint.FoundationKit.SpearDischarge;

// ── COORDINATOR (Plugin.Awake, BEFORE content collection; each call is idempotent) ──

// 1. Conduit Spear: registers EntityState + projectile + SkillDef. Returns the SkillDef
//    (null + logged error on failure).
Skills.SkillDef spear = ConduitSpearRegistration.RegisterSpear();

// 2. Conductor mark buff (BuffDef registration + amplification hook).
ConductorMarkServer.EnsureRegistered();

// 3. Discharge consume hook.
DischargeEmitter.Install();

// 4. Wire the secondary slot (coordinator-owned wiring, per kit-wiring doc) — the SAME
//    existing "Weapon" EntityStateMachine the primary uses, so no new machine is needed:
//    secondarySlot.SetSkillInternal(spear);

// ── ARC BOLT / OPEN CIRCUIT AGENTS (on a CONFIRMED hit, server-side, after damage applied) ──

// The one-line award entry point. Item procs are rejected inside (source None → false).
bool meterMayNowBeFull = DischargeEmitter.AwardChargeForConfirmedHit(ownerBody, HsDamageSource.ArcBolt);
```

Other members available for diagnostics: `ConductorMarkServer.ApplyMark(victim, seconds)`,
`ConductorMarkServer.IsMarked(victim)`, `ConductorMarkServer.ConductorMarkBuff`,
`ConduitSpearProjectile.Prefab` / `.EnsurePrefab()`,
`ConduitSpearRegistration.SpearStateType`, `ConduitSpearSkillDef.SkillName`,
`SpearDischargeTuning.*`.

## The consume-before-fire re-entrancy guarantee (structural, stated explicitly)

`DischargeEmitter.OnServerDamageDealt` (hooked on `GlobalEventManager.onServerDamageDealt`,
server-only by construction):

1. Gate: attacker must pass `KitUtil.IsHollowSaintSkillDamage(attacker, source)`.
2. `meter.IsFull` must be true.
3. **`meter.Consume()` runs FIRST** — from this line on the meter is empty (0.0) for
   every subsequent call in the same frame. A second hit arriving in the same frame
   finds `IsFull == false` and is treated as a normal hit.
4. **`FireBurst(...)` runs SECOND** — a `BlastAttack` (verified server AoE primitive)
   with the approved 3.0x snap coefficient, 10 m radius, ≤6 targets, Linear falloff.
5. Belt-and-braces: a `firing` bool guards the whole hook, so even damage events raised
   synchronously *during* the burst can never re-enter and chain-fire.

The burst's own hits carry `DamageSource.NoneSpecified`, so they are rejected by the
gate in step 1 in any case — Discharge cannot recursively self-trigger. The burst also
has `procCoefficient = 0` (PROPOSAL) so it cannot proc items that would deal further
Hollow-Saint damage.

## How item procs are provably rejected (the single most important rule)

Two independent gates, both decompile-verified:

1. **The source tag.** `HsDamageSource` (`KitShared`, append-only, untouched) is `None`
   for every path our skills do not explicitly call with a real value. Item procs never
   construct one. `KitUtil.IsHollowSaintSkillDamage` (existing, untouched) rejects
   `None` immediately and additionally checks the attacker is a Hollow Saint body.
2. **The `DamageTypeCombo.damageSource` field** (verified in the real game assembly:
   `DamageInfo.damageType.damageSource`, `DamageSource.Primary/Secondary/Utility/Special`
   set by vanilla-style `new DamageTypeCombo(...)` construction — the exact pattern the
   VoidSurvivor decompiled body uses). `DischargeEmitter.HsDamageFrom` maps
   Primary→ArcBolt, Secondary→ConduitSpear, Special→OpenCircuit, everything else
   (`NoneSpecified`, `Hazard`, `DOT`, `Equipment`)→`HsDamageSource.None` → rejected.

An item proc therefore fails both gates: it never carries our tag, and its
`damageSource` is `NoneSpecified`. The award path (`AwardChargeForConfirmedHit`) takes
the tag as a **parameter from the skill's own code**, so an item proc literally has no
way to pass the check.

## Wiring decisions the coordinator must know (verified, with receipts)

These **correct two findings** in `docs/kit-wiring-api-20260927.md`, matching what the
Arc Bolt agent independently found in their RESULT §"Wiring decisions":

1. **`EntityStates.EntityState` is a plain class, not a MonoBehaviour.** Its
   `gameObject`/`transform`/`GetComponent` members are wrappers over `outer`; states are
   created with `Activator.CreateInstance` by `EntityStateCatalog.InstantiateState`
   (real decompiled body). **Custom states cannot be attached as child GameObjects on
   the body prefab** — that path cannot compile for a non-Component. The state reaches
   the machine entirely through the SkillDef, as vanilla does. (The doc's
   "activationStateMachineName only" part survives: the machine IS looked up by name.)
2. **`SerializableEntityStateType` IS constructible — the "not found" result was a
   namespace artefact.** It lives in `EntityStates`, not `RoR2`
   (`ilspycmd -t EntityStates.SerializableEntityStateType` resolves fine; real body in
   the vanilla assembly stores `AssemblyQualifiedName` and resolves via
   `Type.GetType`, which works for loaded mod assemblies). Public `(string)` and
   `(Type)` constructors; `R2API.ContentAddition.AddEntityState(Type, out bool)` returns
   a correctly built one. This code uses exactly that path.
3. **Activation chain (real bodies):** `SkillDef.OnExecute` →
   `skillSlot.stateMachine.SetInterruptState(InstantiateNextState(...), priority)`.
   `GenericSkill.PickTargetStateMachine` (private, real body) resolves the machine via
   `EntityStateMachine.FindByCustomName(base.gameObject, skillDef.activationStateMachineName)`.
4. **Machine name = `"Weapon"`.** The foundation clones CommandoBody, whose machine
   carries `customName = "Weapon"`. The spear fires through the SAME machine as the
   primary (they are never active at once — `SetInterruptState` queues correctly, and
   `ConduitSpearState.GetMinimumInterruptPriority` returns `PrioritySkill` before
   release, `Skill` after). **The coordinator must leave the cloned body's `"Weapon"`
   machine untouched** or `PickTargetStateMachine` resolves null and the skill silently
   never fires. `KitRegistration.cs` currently points the secondary at
   `"HollowSaintConduitSpear"` — see coordinator requests below.
5. **R2API.ContentAddition is available without a csproj change** — transitive dep of
   R2API.Prefab (verified in `packages.lock.json`: `R2API.ContentManagement 1.0.10`),
   same finding as the Arc Bolt agent.
6. **Release-fraction nuance (documented, not changed):** the approved constant is
   `KitTuning.ConduitSpearReleaseNormalizedTime = 6/20 = 0.30` ("f7 of 20"); the repo
   anim spec's exact marker fraction is `(7-1)/19 = 0.3158`. I used the KitTuning
   constant (shared, coordinator-owned); the gap is under one animation frame at 24 fps.

## Required animation clips and layers (declared per contract — coordinator-owned import)

Source: `docs/unity-vfx-anim-spec-20260927.md` (repo-verified controller layout).

| Use | Layer | State name (SPACE form — spec §5 naming trap) | Playback param | Duration | Fire fraction |
|---|---|---|---|---|---|
| Spear cast | `UpperBody` (layer 1) | `Conduit Spear` | `attackSpeed` | 0.6 s / attackSpeed | release `Spear release` at `KitTuning.ConduitSpearReleaseNormalizedTime` (0.30; marker-exact 0.3158) |
| Discharge proc | `Overlay` (layer 2) | `Discharge snap` | `attackSpeed` | 13/24 ≈ 0.5417 s | `Release` 0.1538 (cosmetic — damage is not timed to the clip) |

Constants live in `SpearDischargeTuning` (`SpearAnimLayer`, `SpearAnimStateName`,
`SpearAnimPlaybackRateParam`, `DischargeOverlayLayer`, `DischargeOverlayStateName`) —
reconciling with the real imported clips is a constants-only change. The spear gesture
on `UpperBody` is exactly the approved "usable while moving (upper-body animation
layer)": locomotion keeps playing on the `Body` layer underneath.

## Required VFX (hooks exist; nothing blocks on art)

| Slot | Hook | Status |
|---|---|---|
| Projectile ghost | inherited from the Mage bolt template (`ProjectileController.ghostPrefab`) | inherited — replace with `FX_HS_Spear_Projectile` per the VFX plan §11 when art lands |
| Spear impact | `ConduitSpearImpact.impactEffectPrefab` (public field → `EffectManager.SimpleImpactEffect`, server) | **null — drop-in hook, no code change needed when `FX_HS_Spear_Impact` exists** |
| Conductor mark aura | the registered BuffDef (`HSConductorMark`) — attach `FX_HS_ConductorMark` to the marked enemy via a buff-vfx pass later | buff registered with `iconSprite = null` |
| Discharge burst | none wired — `BlastAttack` has `impactEffect` field (verified `EffectIndex`) that can be set without code change | open slot for `FX_HS_Discharge_Proc` |
| Skill icon / buff icon | `skillDef.icon` / `buff.iconSprite` | **null — required art** |

## Tuning constants used

From `KitTuning` (approved, untouched): `ConduitSpearCooldown` 5 → `baseRechargeInterval`;
`ConduitSpearDamageCoefficient` 4.5 → projectile damage; `ConduitSpearProjectileSpeed` 150
→ `ProjectileSimple.desiredForwardSpeed`; `ConduitSpearConductorMarkSeconds` 6 → mark
duration; `ConduitSpearReleaseNormalizedTime` 6/20 → release gate;
`DischargeMeterMax` 10 (meter, fill side); `DischargeSnapDamageCoefficient` 3.0 → burst
damage; `DischargeChainRange` 10 → burst radius; `DischargeMaxChainTargets` 6 (target cap
— enforced by `BlastAttack`'s own hit enumeration; the cap constant remains available if
we switch to a hand-rolled target list).

From `SpearDischargeTuning` (folder-local, all PROPOSAL unless noted):
`ConductorMarkDamageMultiplier` 1.5 (parked for M5 — no approved number exists anywhere),
`ConduitSpearProjectileRadius` 0.35, `ConduitSpearKnockbackForce` 8, 
`ConduitSpearMaxLifetimeSeconds` 4, `ConduitSpearProcCoefficient` 1.0 (matches the Arc
Bolt agent's decision), and — not proposals — `ConduitSpearBaseDuration` 0.6 s and the
anim layer/state names (repo anim spec).

## PROPOSAL decisions (parked, flagged in code, default chosen)

1. **Mark strength 1.5x** — the design says only "bonus damage from subsequent hits".
   1.5x keeps the spear kit-relevant without making it mandatory. Stuart decides at M5.
2. **Mark amplifies ALL Hollow Saint skill damage on the marked target** (not only the
   literally-next hit). Reading "next Arc Bolt / Discharge hits" as a category, not a
   count — a strict one-shot mark would waste the mark on a 1-damage hop.
3. **Mark applies AFTER the spear's own damage** (ordering in `ConduitSpearImpact`), so
   the spear's 450% hit is never amplified by its own mark. Refresh semantics: a second
   spear hit extends the 6 s timer rather than stacking (BuffDef `canStack = false`,
   matching the VFX plan's "one mark at a time").
4. **Mark amplification is generic to kit skills** (any of our sources) — item procs
   hitting a marked enemy are explicitly NOT amplified.
5. **Discharge burst: never crits, `procCoefficient = 0`, `LoSType.None`** (chains
   through walls, matching the Arc Bolt agent's no-LoS chain decision). `procCoefficient
   = 0` also makes the burst provably unable to proc items, which strengthens the
   re-entrancy story.
6. **Burst target cap**: `BlastAttack` enumerates all hits in radius; the approved
   `DischargeMaxChainTargets = 6` is not enforced by a hand-rolled list in v1 (the
   radius bounds it in practice). If Stuart wants a hard cap, the swap is
   `BlastAttack` → `BullseyeSearch` + N × `TakeDamage` (the Arc Bolt agent's pattern).
7. **The spear hit also charges the meter** (1 charge, same as an Arc Bolt hit) — the
   contract says the meter fills "from EVERY damaging skill of Hollow Saint's own", and
   the spear is one.
8. **`mustKeyPress = true` + `resetCooldownTimerOnUse = true` +
   `beginSkillCooldownOnSkillEnd = true`** for the secondary (deliberate throw per press;
   5 s clock starts when the throw resolves). The Arc Bolt agent set the opposite
   pattern for the auto-repeat primary — both are per-slot design intent.

## Unverified / known risks (honest list — nothing is playtested)

1. **Nothing is verified in game.** No launch, no log, no playtest. Compile- and
   decompile-verified only — "unverified by construction".
2. **Projectile template load path.** `LegacyResourcesAPI.Load("Prefabs/Projectiles/MageLightningboltBasic")`
   is inferred from the working `Prefabs/CharacterBodies/CommandoBody` mapping plus the
   addressables-verified path `Assets/RoR2/Base/Characters/Mage/Skills/MageLightningboltBasic.prefab`
   (I confirmed the asset name exists in the install's Addressables catalog, read-only).
   Whether the legacy `Resources` map carries it could NOT be confirmed. On failure the
   error is logged loudly and the spear fires nothing rather than throwing. This is the
   identical risk the Arc Bolt agent carries (their RESULT risk 2) — **one fix covers
   both skills** if the coordinator re-templates.
3. **`ProjectileSimple.desiredForwardSpeed` vs the template's own speed fields** —
   setting it is API-verified, but whether the clone honours it over the template's
   serialized velocity curve needs a playtest. If the spear flies at the Mage's speed,
   the one-line fix is `useSpeedOverride = true` + `speedOverride` via `FireProjectileInfo`
   (verified fields) in `ConduitSpearState.FireSpearServer`.
4. **Animator state/layer names.** `UpperBody` / `Conduit Spear` / `Overlay` /
   `Discharge snap` / `attackSpeed` come from the repo anim spec, which describes the
   *planned* `GameFoundation02` controller. The currently-shipped bundle has a single
   `Body` layer and only 15 clips — until the coordinator builds v2, the spear gesture
   will log `Animator.GotoState: State could not be found` and the discharge overlay
   will not play (gameplay unaffected: damage/meter/mark are code, not animation).
5. **Mark amplification timing** relies on `onServerDamageDealt` firing before the final
   health deduction inside `HealthComponent.TakeDamageProcess`. The vanilla pipeline
   order was verified by decompiling the call site (`TakeDamage` → event → application),
   but the exact hook position inside `TakeDamageProcess` is stripped in GameLibs. If
   playtests show the mark amplifying one hit late, the fallback is to amplify in a
   `HealthComponent.TakeDamageProcess` IL hook or switch the mark to a pre-damage
   multiplier read at damage creation time in each skill (kit-internal change).
6. **`DischargeMeter` replication for HUD** — out of scope here (fill side +
   coordinator UI); the meter's `Meter`/`Normalized` getters exist for it.
7. **Coordinator requests (no code written outside my folder):**
   a. `KitRegistration.Assign(...)` currently builds a generic SkillDef for the
      secondary pointing at `"HollowSaintConduitSpear"`. After both registration calls
      exist in `Plugin.Awake`, point the secondary at
      `ConduitSpearRegistration.SkillDef` (machine `"Weapon"`) instead — or change the
      const `KitRegistration.ConduitSpearStateMachine` to `"Weapon"`. Until then the
      spear state cannot be reached (PickTargetStateMachine resolves null).
   b. Call `ConductorMarkServer.EnsureRegistered()` and `DischargeEmitter.Install()` in
      `Plugin.Awake`.
   c. The Arc Bolt agent's `ArcBoltChainServer.AwardDischargeCharge` duplicates my
      award gate (their code, their call — harmless duplication, both funnel into the
      same meter). If they adopt `DischargeEmitter.AwardChargeForConfirmedHit`, delete
      their private helper — NOT my scope.
   d. Reconcile animation constants with the real imported clips (constants-only).
   e. Art: skill icon, buff icon, `FX_HS_Spear_Impact`, `FX_HS_Discharge_Proc`,
      `FX_HS_ConductorMark`, spear projectile ghost.
