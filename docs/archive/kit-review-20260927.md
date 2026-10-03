# Adversarial code review — Hollow Saint M3 kit (ArcBolt / SpearDischarge / ArcStep)

**Reviewer:** independent adversarial reviewer (not the implementer)
**Date:** 2026-09-27
**Scope:** `FoundationKit/ArcBolt/*`, `FoundationKit/SpearDischarge/*`, `FoundationKit/ArcStep/*`,
`FoundationKit/KitShared.cs`, `KitRegistration.cs`.
`FoundationKit/OpenCircuit/*` was written concurrently by another agent and is **excluded** by instruction.
**No code was modified.** Build was run read-only to confirm compilation:
`dotnet build HollowSaint.csproj -c Release --no-restore` → **Build succeeded, 0 errors**, 2 warnings
(the expected `NU1701` MMHOOK warning, and `ArcStepState.cs(161,62): warning CS0162: Unreachable code detected`).

**Method / evidence standard.** Every finding below is anchored to a quoted line. Game-API claims were
checked against the installed GameLibs assembly
(`C:/Users/stuwj/.nuget/packages/riskofrain2.gamelibs/1.4.1-r.0/lib/netstandard2.0/RoR2.dll`) and against the
installed `R2API.ContentManagement` / `R2API.Language` plugin DLLs (read-only decompiles; nothing was written
into the game install or the r2modman profile).

> **Hard limit on this review's authority.** `RoR2.dll` ships with all method bodies stripped — ILSpy prints
> `throw null;` for every non-trivial method. So *signatures, fields and enum values* are verified; *method
> bodies* (`HealthComponent.TakeDamageProcess`, `EntityStateMachine`'s transition logic, the death transition,
> `BlastAttack.Fire`) are **not** readable. Where a conclusion depends on a body, it is marked
> "could not verify" rather than asserted. Nothing here is verified in game; per the contract everything is
> **unverified by construction**.

---

## Verdict

The core design is sound and the server-authority discipline is genuinely good: **I found no client damage,
client charge, client projectile-spawn or client cooldown-consume path.** The re-entrancy claim
(`Consume()` before `FireBurst()`) **holds** — see F1. What is actually broken is narrower and sharper than
"authority holes":

- **1 CRITICAL** — a `throw` on the content-load path that reproduces the known "stall at 99%" failure mode.
- **8 SERIOUS** — one entire approved feature is dead and fails *silently*; an approved cap is never applied;
  the meter is not replicated despite the class doc saying it is; two projectiles can be permanently
  inerted by their own self-hit test; the authority check fails **open**; the documented damage tag does
  not exist in the data model.
- **20 MINOR** — dead code, dead constants, duplicated constants, misleading comments, one compiler warning.
- **12 FALSE ALARMS** — things that look wrong and are fine, verified against the real assembly.

Build is green. The dangerous item is runtime, not compile time.

---

## CRITICAL — will break in game

### C1. `ArcStepRegistration.CreateSkillDef()` throws on the content-load path; `FoundationContent` rethrows → content load fails

`HollowSaintMod/FoundationKit/ArcStep/ArcStepRegistration.cs:68-74`

```csharp
public static SkillDef CreateSkillDef()
{
    if (!RegisterEntityState())
    {
        throw new System.InvalidOperationException(
            "Arc Step SkillDef cannot be created before its entity state is registered.");
    }
```

**The path.** `KitRegistration.Install` calls it unguarded, at `KitRegistration.cs:55`:

```csharp
var arcStep = ArcStepRegistration.CreateSkillDef();
```

and the only caller of `Install` is `FoundationContent.cs:47`, inside a try/catch that **rethrows**:

```csharp
catch (Exception error) { Plugin.Log.LogError("Hollow Saint content load failed: " + error); throw; }
```

**Why it matters.** This is precisely the previously-diagnosed failure mode for this project: an exception
out of `LoadStaticContentAsync` aborts the content-provider pass, so the game hangs at 99% / errors on load.
The other two modules deliberately do *not* do this — `ArcBoltRegistration.cs:88-92` and
`ConduitSpearSkillDef.cs:85-89` both **log and `return null`** when registration fails, and
`KitRegistration.Assign` then leaves the vanilla skill in place (`:83-88`). Arc Step is the only module that
converts a registration failure into a fatal throw, so a single rejected state registration takes down the
whole survivor (and with it Arc Bolt and Conduit Spear, which are already registered at that point).

**Why the premise behind it is wrong.** Three comments assert that R2API itself throws:
`ConduitSpearSkillDef.cs:60` ("AddEntityState throws internally if the EntityStateCatalog has already
initialized"), `ArcStepRegistration.cs:22-24`, and `ArcStepRegistration.cs:51-53`. I decompiled the
**installed** `R2API.ContentManagement.dll`. It does not throw:

```csharp
RejectContent(entityStateType, nonAPICaller, "EntityStateType", "but the EntityStateCatalog has already initialized!");
wasAdded = false;
return default(SerializableEntityStateType);
```

`RejectContent` catches its own `InvalidOperationException` and only calls `Logger.LogError`. So the mod's
own `throw` is the *only* fatal path, and it exists because a comment told the author it was load-bearing.

**Fix (the file is agent-owned; `KitRegistration.cs` is coordinator-owned, so the second half is a request):**

```csharp
public static SkillDef CreateSkillDef()
{
    if (!RegisterEntityState())
    {
        Plugin.Log.LogError("Arc Step: entity state unavailable; leaving the vanilla utility skill in place.");
        return null;                       // never throw from the content window
    }
```

and in `KitRegistration.Install`, wrap the call the way every other failure in this file is handled (log +
degrade), so one module's failure can never abort the pack:

```csharp
SkillDef arcStep = null;
try { arcStep = ArcStepRegistration.CreateSkillDef(); }
catch (Exception e) { Plugin.Log.LogError("Arc Step SkillDef build failed: " + e); }
```

Also correct the three comments that claim R2API throws.

---

## SERIOUS — wrong behaviour under normal play

### S1. The conductor mark is never registered. The whole approved secondary-skill feature is dead, and it fails *silently*

`ConductorMark.cs:227` defines `EnsureRegistered()`; it is called **nowhere in any `.cs` file in the repo**
(verified by grep over `*.cs`: the only occurrences of `ConductorMarkServer` are the definition at `:217`
and the `ApplyMark` call at `:181`). `KitRegistration.Install` registers the meter and the emitter
(`KitRegistration.cs:60-61`) but never the mark. The only mention is a *request* in the agent's result doc.

Consequences, both silent:
- `ConductorMarkBuff` stays `null` → `ConductorMarkServer.cs:267`
  `if (victim == null || duration <= 0f || ConductorMarkBuff == null) return;` → **the 6 s mark is never
  applied by the spear** (an approved behaviour: "450% single target, 6 s conductor mark").
- The amplification hook is never subscribed (`:247`) → the ×1.5 amplification never runs.

**In game:** the spear deals its damage and nothing else, forever, with no error anywhere in the log.
Fix (coordinator, `KitRegistration.cs`, next to `:61`): `ConductorMarkServer.EnsureRegistered();`

### S2. `KitTuning.DischargeMaxChainTargets = 6` is never used — the Discharge burst is uncapped

`KitShared.cs:53` declares it; the only other occurrence in the entire mod is a **doc comment**
(`DischargeEmitter.cs:109`). `FireBurst` fires one `BlastAttack` with `radius = KitTuning.DischargeChainRange`
(`:119-138`) and no target limit.

**In game:** a 3.0× Discharge (`DischargeSnapDamageCoefficient`) hits *every* enemy within 10 m. In a
dense pack (Behemoth boss + adds, or a 12-target clown horde) that is a screen-clearing nuke where the design
says "up to 6 targets". Fix: reuse the pattern ArcBolt already has — collect candidates with `BullseyeSearch`
and apply damage to the first `DischargeMaxChainTargets` — or delete the constant and correct the comment so
nobody tunes a number that does nothing.

### S3. The Discharge meter is not replicated, and the Discharge overlay plays only on the server

`KitShared.cs:67-69` promises: *"Server-authoritative meter. Only the server mutates it; clients read the
replicated value for UI."* The class has no networking of any kind — a private `float meter`, `Awake`,
`AddCharge`, `Consume`, `IsServer`. No `NetworkVariable`, no synced field, no RPC. Grep for readers of
`.Meter` / `.Normalized` across the mod returns **zero** consumers; only `DischargeEmitter.cs:86` reads
`.IsFull`. `DischargeEmitter.PlayDischargeOverlay` (`:140-160`) is called from inside the server-only
`FireBurst`, and its own doc comment (`:146`) claims it is a "Local-only visual" — it is not local, it is
server-only.

**In game:** a client never sees the meter fill, never sees the meter-full flourish, and never sees the
Discharge snap animation — only the damage. Fix: make the meter a `FloatNetworkVariable` on the body (or sync
it explicitly) and drive the client VFX from `OnValueChanged`; raise the overlay on every peer (e.g. from a
replicated event or an `EffectManager` client spawn) instead of from inside the server-side burst.

### S4. Both impact components set `consumed = true` *before* the self-hit test, then return without destroying — the projectile is permanently inert

`ArcBoltProjectile.cs:135` and `ConductorMark.cs:135`:

```csharp
consumed = true; // single impact semantics, like vanilla 'alive = false'
```

`ArcBoltProjectile.cs:157-160` (identical at `ConductorMark.cs:159-162`):

```csharp
if (projectileController != null && healthComponent.gameObject == projectileController.owner)
{
    return; // never damage the shooter
}
```

**Why it matters.** Vanilla's `ProjectileSingleTargetImpact` tests the owner *first* and only then sets
`alive = false`, so a projectile that grazes the shooter passes through and can still hit a target. These
components mark themselves consumed and then bail out: no damage, no chain, no mark, no charge, **no
destroy either** (the `if (consumed) Kill(...)` at `:190` is never reached because of the earlier `return`).
The projectile survives as a flying ghost until its lifetime, and the skill's cooldown has already been
spent. For Conduit Spear that is a 5 s cooldown for a silent no-op.

**Fix** — hoist the owner test above the `consumed` write, or reset the flag on that path, which is exactly
what the existing pass-through branch already does (`ArcBoltProjectile.cs:184-188`: `consumed = false; return;`).

### S5. The server-authority check on the meter fails **open**

`KitShared.cs:101-104`:

```csharp
private bool IsServer()
{
    return owner == null || owner.hasEffectiveAuthority;
}
```

`IsServer()` gates both `AddCharge` and `Consume`. With `owner == null` it returns **true on every machine**,
so a client mutates the meter. The contract is explicit: *"must check `hasEffectiveAuthority` / `NetworkManager.isServer`
and no-op otherwise."* This is the permissive direction.

Honest scoping: I could **not** construct a live trigger. The meter is added to the `CharacterBody`'s own
GameObject (`KitRegistration.cs:60`) and `AddComponent` runs `Awake` synchronously on an active prefab, so
`GetComponentInParent<CharacterBody>()` at `KitShared.cs:82` resolves. It is a one-character fix in the
direction the contract already demands:

```csharp
return owner != null && owner.hasEffectiveAuthority;
```

### S6. The "explicit `HsDamageSource` tag" gate the design relies on does not exist in the data model

The wiring doc (`kit-wiring-api-20260927.md:80-84`) and the code comments both say the meter is protected by
"an explicit `HsDamageSource`" that "item procs never carry". In the real code, `HsDamageSource` never
travels with the damage — `RoR2.DamageInfo` is vanilla and immutable as far as this mod is concerned. What
actually exists is two different mechanisms on two different paths:

- **Fill path** — each skill passes a *call-site literal*:
  `ArcBoltChain.cs:78` `AwardChargeForConfirmedHit(attackerBody, HsDamageSource.ArcBolt)`,
  `ConductorMark.cs:178` `... HsDamageSource.ConduitSpear`, `OpenCircuitPulseDriver.cs:197` `... OpenCircuit`.
  The gate is then `KitUtil.IsHollowSaintSkillDamage` (`KitShared.cs:146-153`), which checks only
  `attacker.GetComponentInParent<CharacterBody>().baseNameToken == "HS_NAME"`. That path is sound — the
  caller is our own code.
- **Consume path** — `HsDamageFrom` re-derives the source from the **shared vanilla** field:
  `DischargeEmitter.cs:167-178` switches on `damageInfo.damageType.damageSource`, mapping
  `Primary→ArcBolt, Secondary→ConduitSpear, Special→OpenCircuit, Utility→None, default→None`.

Three consequences, all provable from the code:

1. **`HsDamageSource.Discharge` is unreachable dead code.** Nothing in the mod ever produces it —
   `HsDamageFrom` has no `DamageSource` that maps to it, and no caller passes it. Meanwhile the class doc at
   `DischargeEmitter.cs:26-29` claims the opposite: *"Discharge's own hits pass HsDamageSource.Discharge,
   which the award gate accepts for meter FILL — and even if the burst refilled the meter to full in one
   frame..."*. The burst actually uses `DamageSource.NoneSpecified` (`:133`), so **the Discharge snap awards
   zero charge**, and the `firing` guard is doing something different from what the comment says (it stops a
   *second discharge*, not a refill).
2. The consume gate is a **convention on a vanilla field**, not a private tag. I verified `RoR2 1.4.1`
   ships pre-baked `DamageTypeCombo.GenericPrimary / GenericSecondary / GenericUtility / GenericSpecial`
   statics and an `IsDamageSourceSkillBased` helper. Any code — vanilla, another mod, a future update —
   that deals damage with `attacker = <the HS body>` and one of those combos fires a Discharge. I could not
   prove a specific vanilla item does this (see "Could not verify"), so this is a structural fragility, not a
   proven leak.
3. The duplicated mapping is copy-pasted at `ConductorMark.cs:307-318` with the comment *"if one changes,
   change both"* — see M1.

**Fix:** either make the tag unforgeable and private (a server-side hit registry keyed on the inflictor, or
a custom `DamageTypeExtended` bit that only this mod sets), or — minimally — delete the "explicit tag"
language from the docs and state the real rule: *meter fires on any damage whose `DamageSource` is
Primary/Secondary/Special and whose attacker is a `HS_NAME` body.* Same for the Discharge snap's
charge behaviour, which needs a decision, not a comment.

### S7. `KitRegistration.Install` leaves the vanilla Commando special and passive in place

`KitRegistration.cs:66-68` assigns only `primary`, `secondary`, `utility`. `skillLocator.special` is never
re-pointed and `skillLocator.passiveSkill` is never touched (the wiring doc lists both as the intended
surface). **In game today:** Hollow Saint keeps the Commando's special slot skill, and the Discharge passive
has no `passiveSkill` entry (no name/description/icon in the character sheet). This is expected to be closed
by the concurrent OpenCircuit work, but the success log at `:70-73` reads `HOLLOW_SAINT_KIT_INSTALLED slots=3`
with no mention of the omissions — flagging so it is not mistaken for done.

### S8. `KitRegistration.Install` is not idempotent, and Arc Step's SkillDef has no cache

`Install` has no `installed` guard. A second call would: re-run `RegisterTokens` (harmless — see F11),
re-run `ArcStepRegistration.CreateSkillDef()` (`:68`, **no cache, unlike `ArcBoltRegistration.SkillDef` and
`ConduitSpearRegistration.SkillDef` which are both cached behind a `Registered` flag), and re-assign all
three slots. Today `Install` is called once per process from the content window, so this is latent — but the
parent's own brief asked whether a second call is safe, and the honest answer is "it allocates a second
ScriptableObject and is not designed for it". Fix: a static `installed` guard in `Install`, and a static
`SkillDef` cache in `ArcStepRegistration` mirroring the other two modules.

---

## MINOR

- **M1 — `HsDamageFrom` is duplicated verbatim** at `DischargeEmitter.cs:167-178` and `ConductorMark.cs:307-318`,
  with the comment *"if one changes, change both"*. It belongs in `KitShared.cs` (append-only, coordinator-owned)
  as a single `KitUtil.HsDamageFrom(DamageInfo)`. **Request to the coordinator.**
- **M2 — contradictory timing comment.** `ArcBoltState.cs:23` says `f3 Bolt release → 0.15`, but the constant
  it documents four lines below (`:32`) is `BoltReleaseNormalizedTime = 2f / ClipFrames; // f3 → 0.15` — which
  is **0.10**, not 0.15. `KitTuning` uses the 0-based `(f-1)/20` convention throughout
  (`ArcBoltInterruptNormalizedTime = 12f/20f` for f13 ✓). The code is right; the two comments are wrong.
- **M3 — dead constants:** `ArcBoltState.cs:33` and `ConduitSpearState.cs:33` both declare
  `RecoveredNormalizedTime = 1f` and never use it.
- **M4 — Arc Step's SkillDef never reaches the content pack.** `ArcStepRegistration.cs:68-106` builds the
  `SkillDef` and returns it, but never calls `ContentAddition.AddSkillDef` — unlike
  `ArcBoltSkillDef.cs:105` and `ConduitSpearSkillDef.cs:101`. It is also not cached (see S8), so each
  `Install` allocates a fresh one.
- **M5 — Arc Step's SkillDef never sets `icon`.** The other two set `icon = null; // REQUIRED ART`
  (`ArcBoltSkillDef.cs:39`, `ConduitSpearSkillDef.cs:35`). Arc Step has no line at all, so the art request
  is lost and the comment convention is broken.
- **M6 — the unreachable branch is confirmed by the compiler:** `ArcStepState.cs:161`
  `if (ArcStepStateTuning.PreserveVerticalVelocity) velocity.y = entryVerticalVelocity;` →
  `ArcStepState.cs(161,62): warning CS0162`. The code comments are honest about it, so this is a note, not a
  defect.
- **M7 — wrong comment about `fixedAge`.** `ArcStepState.cs:93-95` claims *"EntityState.fixedAge is only
  maintained by BaseState subclasses"*. I verified `public float fixedAge` is declared on
  `EntityStates.EntityState` itself. The private `dashAge` clock is harmless and arguably safer, but the
  stated reason is wrong and would stop a future editor from using `fixedAge` correctly.
- **M8 — `DashingBodies` leaks.** `ArcStepState.cs:71` is a process-wide
  `static readonly HashSet<GameObject>`, holding strong references, pruned only in `OnExit` (`:186`). A body
  destroyed mid-dash (despawn, telefrag, machine teardown) leaves a dead `GameObject` in the set for the life
  of the process, and `IsBodyDashing(deadBody)` then returns `true` (`:78`). Fix: prune
  `null`/destroyed entries, or hold `WeakReference<CharacterBody>`.
- **M9 — no client ever receives the dash-trail event.** `ArcStepState.cs:123-128` raises
  `ArcStepVfxHooks.SpawnGroundTrail` only under `isAuthority`, while `ArcStepVfxHooks.cs:22-25` says "spawn
  the trail wherever this fires (server) **or mirror it on clients** from the replicated state". No mirroring
  code exists. The `DashEnded` pairing (`:185`) is consistently authority-gated ✓.
- **M10 — `ArcStepTrailSampler.Latest` (`ArcStepVfxHooks.cs:117`) returns a `default` struct with
  `body == null`** before the first dash. A polling consumer must null-check; not currently a crash.
- **M11 — `destroyOnWorld` is dead.** Public, defaults `true`, never set → the `else if (!destroyOnWorld)`
  pass-through branch at `ArcProjectile.cs:184` / `ConductorMark.cs:185` is unreachable. (It is, however, the
  correct shape for the S4 fix.)
- **M12 — unguarded `projectileController` dereference.** `ArcBoltProjectile.cs:161` and `ConductorMark.cs:163`
  use `projectileController.teamFilter.teamIndex` with no null check, while every other access in the same
  methods null-checks it. The Mage template always has one, so this is defensive only.
- **M13 — `handCounter` is `static`** (`ArcBoltState.cs:45`). With two Hollow Saints in co-op, left/right hand
  alternation runs off one global counter, so both players tend to fire the same hand simultaneously.
  Cosmetic; documented as a deliberate trade-off in the comment.
- **M14 — allocation in the chain.** `ArcBoltChain.cs:87-98` builds a fresh `BullseyeSearch` and re-runs
  `RefreshCandidates()` for each of up to 4 hops, on every bolt, at a 0.5 s cadence. Correct, just not free.
- **M15 — dead helpers in a shared file.** `KitUtil.ResolveSocket` (`KitShared.cs:114`) and
  `KitUtil.EyePosition` (`:138`) have no callers in the three reviewed modules (the cast states use
  `aimRay.origin` instead). Dead code in coordinator-owned, append-only `KitShared.cs`.
- **M16 — the mark does not amplify the Discharge snap.** `ConductorMarkServer.OnServerDamageDealt:293` gates
  on `HsDamageFrom`, and the burst's `DamageSource` is `NoneSpecified` (`DischargeEmitter.cs:133`), so the
  spear's mark never amplifies the 3× Discharge. Given the spear's own description token says the mark
  "amplif[ies] arc damage", this needs a decision.
- **M17 — three tuning homes, one duplicated value.** The kit has `KitTuning` (coordinator),
  `SpearDischargeTuning` and `ArcStepStateTuning` (agent-owned). `ConduitSpearBaseDuration = 0.6f`
  (`SpearDischargeTuning.cs:38`) is duplicated as `ConduitSpearState.BaseDurationSeconds = 0.6f`
  (`ConduitSpearState.cs:34`) — same value, two constants, one of them dead. Use
  `SpearDischargeTuning.ConduitSpearBaseDuration` and delete the local. `ConductorMarkDamageMultiplier = 1.5f`
  and the other PROPOSALs should be promoted into `KitTuning` when Stuart rules on them (**request**).
- **M18 — the f13 interrupt gate does not implement the rule; it is correct by arithmetic coincidence.**
  `ArcBoltState.cs:103-108` raises the minimum interrupt priority to `Skill` at
  `duration * KitTuning.ArcBoltInterruptNormalizedTime`, but `ArcBoltSkillDef.cs:42` sets
  `interruptPriority = PrioritySkill`, and `CanInterruptState` is `min <= requested`. `PrioritySkill <= PrioritySkill`
  is true **before f13 as well**, so the auto-repeat can cut the cast at any age. It happens to land on f13
  because the 0.5 s interval / 0.833 s cast ratio is 0.6 (both scale with attack speed, so the ratio is
  invariant) — correct today, correct by luck, and silently wrong the moment either number is retuned.
- **M19 — over-broad doc claim.** `ArcBoltSkillDef.cs:16-18` claims *"all vanilla body SkillDefs use
  activationStateMachineName 'Weapon'"*. That is not true in general — vanilla dash/utility skills run on
  `"Body"`, which is exactly what `ArcStepRegistration.cs:38` correctly uses. Harmless here, misleading to the
  next reader.
- **M20 — three comments state a false API contract.** `ConduitSpearSkillDef.cs:60` and
  `ArcStepRegistration.cs:22-24` both say `AddEntityState` "throws" if the catalog has initialized. Verified
  false against the installed R2API (it logs and returns `default`). See C1 — this false premise is what
  justified the fatal throw.

---

## FALSE ALARMS — verified fine, no action needed

These are the things that *look* wrong on a read-through. I checked each against the real assembly.

- **F1 — the Discharge re-entrancy claim HOLDS.** I attacked it and could not break it.
  `DischargeEmitter.cs:95` `meter.Consume();` is a plain field write that lands **before**
  `firing = true` (`:98`) and before `FireBurst(...)` (`:101`).
  - *Two enemies hit in the same frame:* the first hit consumes synchronously; the second finds
    `IsFull == false` and returns at `:86`. Structurally impossible to fire twice.
  - *The burst's own damage re-entering the hook:* suppressed by `firing` at `:81`. (And in fact the burst
    damage uses `DamageSource.NoneSpecified`, so it is *also* rejected by the source gate at `:83` — two
    independent stops, not one.)
  - *`firing` reset:* yes — `try { ... } finally { firing = false; }` at `:99-106`. No `catch`, so an
    exception from `FireBurst` still resets the flag.
  - *Stale-flag leak across bodies or sessions:* no. `firing` is only ever true inside the synchronous
    `FireBurst` stack and is always cleared in `finally`; the process-global `hooked` flag is
    subscription bookkeeping, not state.
  - *One residual, not a double-fire:* `FireBurst` has no `try/catch` of its own, and
    `PlayDischargeOverlay` (`:146-160`) is the one call inside it that touches Unity animation APIs. An
    exception there would escape into the caller's hit processing and could abort the remainder of that
    triggering hit. Wrap `FireBurst` in its own `try/catch`. Cheap, and it makes the "degrade, never
    throw" promise real.
- **F2 — `firing` is process-global, not per-body, and I could not turn that into a co-op bug.** It looks
  like a mutual-exclusion hazard across players, but the only damage events inside the synchronous
  `blastAttack.Fire()` stack are the burst's own, all attributed to the firing attacker — so player B's
  discharge cannot be swallowed by player A's. Style nit only; a per-body flag would read better.
- **F3 — there is no client damage, charge, spawn or cooldown path.** I checked every mutation call:
  - `ArcBoltProjectile.cs:161` — damage + charge + chain, gated `NetworkServer.active` + `FriendlyFireManager`.
  - `ArcBoltChainServer.cs:27` — gated before both the chain (`:59`) and the charge (`:31`).
  - `ConductorMark.cs:163` — damage + charge + mark, same gate; `ConductorMark.cs:266` gates the mark.
  - `DischargeEmitter.cs:115` gates the burst; `:95`'s `Consume()` is additionally gated inside the meter.
  - `ArcBoltState.cs:115-116` and `ConduitSpearState.cs:93-94` — projectile spawns require
    `isAuthority` (checked at `ArcBoltState.cs:78,84` / `ConduitSpearState.cs:71,77`) **and**
    `NetworkServer.active` **and** `hasEffectiveAuthority`. Belt and braces, correct.
  - `NetworkServer.active` (UNet) rather than `RoR2.NetworkManager.isServer` is used **consistently across
    the whole mod**, including the frozen foundation — house style, not an oversight.
  - Neither module touches `GenericSkill` stock, recharge or cooldown; that is the vanilla path, so
    `ArcStepState`'s claim at `:46-48` is accurate.
  - The one constructible client-mutation path is the fail-open `IsServer()` (S5), and I could not trigger it.
  - `ConductorMarkServer.OnServerDamageDealt:283` has no explicit `isServer` check, but it subscribes to
  `onServerDamageDealt`, which is server-only by design (vanilla's own subscribers —
  `RoR2.Achievements.Seeker.NukeSojourn`, `RoR2.Achievements.Chef.RolyPolyHitFiveAirEnemies` — use it
  unguarded). Not a hole.
- **F4 — `ConductorMarkServer`'s in-flight `damageInfo.damage *= 1.5f` is not a no-op.** This looked like a
  classic "you mutated a struct copy" bug. It is not: I verified `RoR2.DamageInfo` is a **`class`** and
  `RoR2.DamageReport` is also a **`class`**, so `var damageInfo = report.damageInfo;` aliases the same object.
  (The accompanying claim that the hook runs *between* dispatch and final damage application depends on a
  `TakeDamageProcess` body I cannot read — flagged as unverified, but the aliasing that makes it work is sound.)
- **F5 — the remote-machine state transitions are not missing.** `ArcBoltState.cs:84` and
  `ArcStepState.cs:138` both return early on `!isAuthority`, so a remote machine never calls
  `SetNextStateToMain()` itself and looks like it would hang. It does not: `RoR2.NetworkStateMachine`
  replicates the transition (`SendSetEntityState(int stateMachineIndex)` /
  `HandleSetEntityState(NetworkMessage)`, both verified present). The authority's transition drives the
  remote copy. Not a softlock.
- **F6 — a plain `EntityState` is driven by the machine.** `ArcStepState : EntityState` (not `BaseState`)
  still gets `Update()`/`FixedUpdate()` from `EntityStateMachine` — vanilla `Commando.DodgeState`, which the
  code cites, is the same shape. The private `dashAge` clock is belt-and-braces.
- **F7 — `EntityState.Reset()` is real.** Verified `public virtual void Reset()` on
  `EntityStates.EntityState`; the `ArcStepState` override at `:206-217` compiles and is the right hook.
- **F8 — the state-registration model is right.** `EntityStates.SerializableEntityStateType` is a struct in
  namespace `EntityStates` with a public `(Type)` constructor, `EntityState` is a plain class (not a
  MonoBehaviour), and `EntityStateMachine` is in `RoR2` (not `EntityStates`) — the coordinator's corrected
  note in `KitRegistration.cs:19-28` is accurate, and no child-GameObject wiring is needed.
- **F9 — `R2API.ContentAddition.AddEntityState` is safe to call and does not dedupe.** Decompiled from the
  installed plugin: it checks abstract-ness, `IsAssignableFrom(EntityState)` and catalog availability, then
  `wasAdded = true; HandleEntityState(...); return new SerializableEntityStateType(type)`. There is **no
  duplicate check** — a second call re-adds and reports `wasAdded = true` again. That is exactly why the
  per-module `Registered` flags matter, and ArcBolt and ConduitSpear both have one. (ArcStep's
  `RegisterEntityState` has `registered`, but `CreateSkillDef` has no SkillDef cache — S8.)
- **F10 — re-registering language tokens is harmless.** Verified `R2API.LanguageAPI.Add` writes straight into
  the custom-language dictionary and overwrites silently: no error, no duplicate warning. So `Install` being
  called twice would not spam the log over tokens (S8 stands on the SkillDef allocation alone).
- **F11 — the ordering of `KitRegistration.Install` is correct.** It runs from
  `FoundationContent.LoadStaticContentAsync` (`FoundationContent.cs:47`), i.e. inside the content-provider
  window, which is before `EntityStateCatalog.Init()`. Registration timing is *not* the problem — the
  failure handling is (C1).
- **F12 — `BullseyeSearch` misuse suspected, dismissed.** `ArcBoltChain.cs:99` can pass `null` into
  `FilterOutGameObject` when `previousVictim` is null, which would put a null in the filter list. It is
  unreachable (`ResolveConfirmedHit` is only ever called with a non-null `HealthComponent`) and a null entry
  in that list is harmless for the candidate comparison. No action.

---

## Could not verify (stated, not papered over)

1. **All `RoR2.dll` method bodies are stripped** (`throw null`). I could not read
   `HealthComponent.TakeDamageProcess` (so the "hook fires before the final damage application" claim in
   `ConductorMarkServer.cs:295-300` is unproven), `EntityStateMachine`'s transition/interrupt logic, the
   death transition, or `BlastAttack.Fire`'s own authority handling. F5 and F6 rest on the *shape* of
   `NetworkStateMachine`, which is verified.
2. **I could not enumerate all ~200 vanilla items** to prove none sets `DamageSource.Primary/Secondary/Special`.
   The pre-baked `DamageTypeCombo.GenericPrimary/GenericSecondary/GenericSpecial` statics **do** exist in
   1.4.1 (verified), which is why S6(c) is framed as a structural fragility rather than a proven leak.
3. **Nothing is verified in game.** Per the contract: unverified by construction. Everything above is
   static analysis plus decompiled-API cross-checks. The two things I would watch in a fresh log after
   staging: `HOLLOW_SAINT_KIT_INSTALLED` with `slots=3` and no `ERROR` lines, and `Conductor Mark registered
   (buffIndex=…)` — which will be **absent** until S1 is fixed.
4. `FoundationKit/OpenCircuit/*` was excluded by instruction; note that it calls
   `DischargeEmitter.AwardChargeForConfirmedHit` (`OpenCircuitPulseDriver.cs:197`) and depends on the same
   public API reviewed here, so S6's conclusion extends to it.

---

## Priority order for the implementer

1. **C1** — remove the `throw` from `CreateSkillDef`; make `Install` failure-tolerant. (Only change that can
   reproduce the 99% stall.)
2. **S1** — one line in `KitRegistration.cs`; restores an entire approved feature that is currently silent-dead.
3. **S2, S3, S4** — real in-game behaviour: uncapped burst, invisible meter/snap, dead projectiles.
4. **S5, S6** — one-character authority fix; and decide what the damage tag actually is, then make the docs
   match the code.
5. **S7, S8** + MINORs — consistency, dead code, and the comment corrections (M2, M20) that will otherwise
   mislead the next reader.
