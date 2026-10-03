# RESULT-ARCSTEP.md — Arc Step (utility) implementation result

**Status: code compiles clean; NOTHING is verified in game.** No claim of in-game
verification is made anywhere in this document. Unverified by construction until
Stuart launches and a fresh `LogOutput.log` is read.

---

## 1. Files created (all inside `HollowSaintMod/FoundationKit/ArcStep/`)

| File | Purpose |
|---|---|
| `ArcStepState.cs` | `EntityStates.ArcStepState` (the dash itself) + `ArcStepStateTuning` (dash-internal PROPOSAL tunables) |
| `ArcStepRegistration.cs` | `ArcStepRegistration`: entity-state catalog registration + SkillDef factory |
| `ArcStepVfxHooks.cs` | `ArcStepVfxHooks` (events + dash data contract) and `ArcStepTrailSampler` (per-frame trail samples) |
| `RESULT-ARCSTEP.md` | this document |

**Files touched outside the folder: none.** Verified by `git status` diff against the
pre-work snapshot. My only writes were these four files; nothing else in the repo was
read-modify-written by me. (Scratch files written under the Hermes cache dir only.)

Build: `dotnet build HollowSaintMod/HollowSaint.csproj -c Release --no-restore`
→ **Build succeeded, 0 errors, 2 warnings** (both the expected pre-existing transitive
`NU1701` MMHOOK warning).

---

## 2. Corrections to the coordinator's wiring briefing (verified against GameLibs IL)

These change what the briefing in `docs/kit-wiring-api-20260927.md` concluded. Every one
was verified by decompiling
`C:/Users/stuwj/.nuget/packages/riskofrain2.gamelibs/1.4.1-r.0/lib/netstandard2.0/RoR2.dll`
with `ilspycmd` (C:/ paths).

1. **`EntityStates.EntityState` is NOT a MonoBehaviour.** IL:
   `.class public auto ansi beforefieldinit EntityStates.EntityState` →
   `extends [netstandard]System.Object`. State instances are plain C# objects created by
   `SkillDef.InstantiateNextState` → `EntityStateCatalog.InstantiateState`. The
   briefing's "add the EntityState as a child GameObject on the body prefab" plan is
   therefore impossible as stated — there is nothing to attach, and nothing needs
   attaching. Vanilla's own `EntityStates.Commando.DodgeState` is the same shape.
   Consequence for my code: `ArcStepState : EntityState` directly (like `DodgeState`),
   and it keeps its **own dash clock** because `fixedAge` is only maintained by
   `BaseState` subclasses — a `FixedUpdate` with `fixedAge` on a bare `EntityState`
   would never end the dash.
2. **`SerializableEntityStateType` EXISTS and is constructible** — the coordinator's
   `ilspycmd -t RoR2.SerializableEntityStateType` failed only because of the namespace:
   the real type is **`EntityStates.SerializableEntityStateType`** (a struct, verified:
   public `(string)` and `(Type)` constructors, `_typeName`/`_stateType` fields,
   `typeName`/`stateType` properties). No child-GameObject workaround is needed.
3. **`RoR2.EntityStateMachine`** (not `EntityStates.EntityStateMachine`). Its
   `CommonComponentCache` struct is confirmed (`characterBody`, `characterMotor`,
   `characterDirection`, `inputBank`, `skillLocator`, `healthComponent`, …), but the
   `EntityState` convenience properties already wrap exactly those, so the state uses
   `characterMotor`/`inputBank`/`characterBody` directly.
4. **`EntityStates.EntityState` virtual surface verified**: `OnEnter`, `OnExit`,
   `FixedUpdate`, `Update`, `GetDeltaTime()`, `GetMinimumInterruptPriority()`,
   `OnSerialize/OnDeserialize(NetworkWriter/NetworkReader)`, `Reset()`,
   `GetModelAnimator()`, `GetModelTransform()`, `SetNextStateToMain` via `outer`.
5. **R2API state registration**: `R2API.ContentAddition` lives in the
   **`R2API.ContentManagement`** package (transitive dep of the project's
   `R2API.Prefab` — verified in `packages.lock.json`), signature verified from its
   compiled IL: `public static SerializableEntityStateType AddEntityState(Type, out bool)`,
   which internally does `new SerializableEntityStateType(entityStateType)` and feeds
   `R2APIContentManager.HandleEntityState`. It must be called before
   `EntityStateCatalog.Init()` — content-window timing (see §4).
6. Minor traps hit: `SkillDef.name` is a **read-only** property in this assembly
   (CS0200) — use `skillName`. `EntityStates.InterruptPriority` values: `Any, Skill,
   PrioritySkill, Pain, Taunt, Stun, Immobilize, Frozen, Vehicle, Death`.

---

## 3. Public API surface (what the coordinator / other agents call)

All in namespace `HollowSaint.FoundationKit.ArcStep`:

```csharp
// — Registration (coordinator, once, inside the content window) —
static bool ArcStepRegistration.RegisterEntityState()
// Registers ArcStepState via ContentAddition.AddEntityState. Idempotent. Returns false
// if rejected (catalog already initialized). MUST succeed before CreateSkillDef().

static SkillDef ArcStepRegistration.CreateSkillDef()
// Returns the configured SkillDef (numbers in §6). Throws if the state was not
// registered. The coordinator then: utilitySlot.SetSkillInternal(CreateSkillDef()).
// Constants: SkillDefName = "HollowSaintArcStep",
//            StateTypeName = "HollowSaint.FoundationKit.ArcStep.ArcStepState",
//            ActivationStateMachineName = "Body"

// — VFX owner —
static event System.Action<ArcStepVfxHooks.ArcStepDashData> ArcStepVfxHooks.DashStarted;
static event System.Action<ArcStepVfxHooks.ArcStepDashData> ArcStepVfxHooks.DashEnded;
static bool ArcStepVfxHooks.HasSubscribers { get; }   // subscription guard
// ArcStepDashData struct fields: body, startPosition, direction (planar, normalized,
// world), duration, speed, startedGrounded; computed property EndPosition =
// start + dir*speed*duration (16*0.55 = 8.8 m nominal path length). Trail placement
// recipe: raycast down from points along startPosition→EndPosition; if
// KitUtil.ResolveSocket(body, "ground") returns non-null, prefer that transform's
// position as the trail's vertical anchor (namespace: HollowSaint.FoundationKit).
// DashEnded fires on EVERY exit path (end/interrupt/cancel/death) for trail fade-out.

static ArcStepVfxHooks.ArcStepDashData ArcStepTrailSampler.Latest { get; }
// most recent per-FixedUpdate position/velocity sample while dashing
// (8-sample ring, authority side)

// — Other kit skills / coordinator diagnostics —
static bool ArcStepState.IsBodyDashing(CharacterBody body)
// True while the dash state is active on this machine. Answers the parked M5 question
// "can Open Circuit strike during Arc Step" — gate on this if the answer is no.
// Also the flag FoundationPresentation's proposed gate reads (§5).
```

Constants for the animator contract:
`ArcStepState.AnimatorParamIsDashing = "isDashing"`,
`ArcStepState.AnimatorParamDashBlendX = "dashBlendX"`,
`ArcStepState.AnimatorParamDashBlendZ = "dashBlendZ"`.

---

## 4. Coordinator wiring requests (I own no file outside my folder — these are requests)

1. **Register the state inside the content window**, before `KitRegistration` assigns
   SkillDefs (content-provider load time, before `EntityStateCatalog.Init()`). In
   `KitRegistration.Install(...)` (coordinator-owned), before the `Assign(...)` calls:
   ```csharp
   if (!ArcStepRegistration.RegisterEntityState()) return; // log says why
   ```
   (`R2API.ContentManagement` is already a transitive package reference — no csproj change needed.)
2. **Build the utility SkillDef from my factory** in `KitRegistration.BuildSkillDef`'s
   utility branch (or replace the generic builder for the utility slot):
   ```csharp
   var utilitySkillDef = ArcStepRegistration.CreateSkillDef();
   // it already carries: activationStateMachineName = "Body", activationState =
   // ArcStepState, baseMaxStock = 2, rechargeStock = 1, baseRechargeInterval = 5,
   // requiredStock = 1, stockToConsume = 1, tokens KitTokens.ArcStepName/Desc.
   utility.SetSkillInternal(utilitySkillDef);
   ```
   The current `KitRegistration.BuildSkillDef` leaves `activationState` empty for every
   slot; that gap is exactly what my factory fills for Arc Step. The other three slots
   remain the coordinator's design.
3. **State machine name = "Body".** The SkillDef resolves the state machine on the body
   prefab by `activationStateMachineName`. The foundation body (cloned CommandoBody)
   already carries the vanilla `"Body"` machine running `GenericCharacterMain` — that is
   the correct machine for a movement utility: while Arc Step runs, the machine's state
   is the dash; when it ends (`SetNextStateToMain()`), `GenericCharacterMain` resumes
   locomotion. **Do NOT create a new `EntityStateMachine` child** for Arc Step (the old
   briefing's plan; no longer applicable — see §2.1), and do not repoint the machine's
   `mainStateType`.
4. **Interrupt plumbing is automatic**: `SkillDef.interruptPriority = Skill` lets other
   skills replace the dash through the normal
   `EntityStateMachine.SetInterruptState` path; `GetMinimumInterruptPriority()` returns
   `Any` so anything (including pain and death) may interrupt. No hook needed.
5. **"ground" socket** (for the VFX owner): `FoundationMounts` currently wires heel
   sockets but no `"ground"` socket. `KitUtil.ResolveSocket(body, "ground")` will return
   null until one is added (`locator.AddChild("ground", …)` in coordinator-owned
   FoundationMounts — my hooks degrade gracefully to the body position meanwhile).

---

## 5. Animator-parameter proposal (REQUEST — FoundationPresentation.cs is frozen; coordinator edits later)

**Current conflict risk (analysis, no edit made):** `FoundationPresentation.LateUpdate`
crossfades a single driven state on Body layer 0 **every frame** from motor velocity and
sprint state, with `CrossFadeSeconds = 0.12`. During a dash, `characterMotor.velocity`
is a fast planar vector (16 m/s → speed clamp ~2.3 on the existing
`planarSpeed / 7f` scale, grounded only), so with today's controller the dash renders as
a fast `Run forward` / `Glide loop`. Nothing breaks; it is just not the dash animation.
Any crossfade-based approach owned by the dash state would fight the LateUpdate driver
(it would win every frame), which is why the proposal below is **parameter-driven** and
lives entirely in the controller + a small presentation gate.

**Proposed additive parameter set (controller side only; no state renames, no new
layers, no changes to the existing driven-state list):**

| Parameter | Type | Written by | Meaning |
|---|---|---|---|
| `isDashing` | bool | `ArcStepState` (already, guarded — see below) | dash active on this machine |
| `dashBlendX` | float [-1,1] | `ArcStepState` | model-space dash component: +right / −left |
| `dashBlendZ` | float [-1,1] | `ArcStepState` | model-space dash component: +forward / −back |

- **Diagonals by blend**: `dashBlend` is the dash direction projected on the model's
  planar forward/right axes (computed in `ArcStepState`), so a forward-left dash sets
  both components (e.g. 0.7/−0.7). The blend tree picks the matched directional clip
  ("Arc Step start/loop/end" sets exist for all four cardinals); diagonals are the
  blend of two, per the approved "diagonals by blend" design.
- **Already implemented on my side, safely:** `ArcStepState` writes these three
  parameters **only if the runtime controller actually declares all three** (it scans
  `animator.parameters` once on entry). Until the controller is updated, the writes are
  skipped entirely — no missing-parameter warnings, and the frozen
  `FoundationPresentation` keeps full animator ownership. The moment the coordinator
  adds the parameters, the state starts driving them with no code change.
- **Guard on the Blender clips**: `isDashing` must be consumed in the controller by a
  sub-state machine on the **Body layer** that switches between the dash sub-states and
  falls back to the normal locomotion states when false. The dash clips to wire (already
  authored, not yet in the game bundle — coordinator Unity work):
  `Arc Step start` (f1–7, marker **Dash start**), `Arc Step loop` (f1–11, loop),
  `Arc Step end` (f1–16, markers **Arrive**, **Cancel**, **Recovered**), plus the
  back/left/right mirrored sets. Suggested normalized-time triggers for the EntityState
  side (matches the marker catalog): Dash start at 0.0, Arrive ≈ 0.55 into `end` from
  dash end, Cancel/Recovered for the interrupt path.
- **The one edit FoundationPresentation will eventually need** (coordinator's future
  change, quoted from its current source so the edit is unambiguous):
  in `LateUpdate()`, right where `desired` is computed, gate the dash:
  ```csharp
  if (ArcStepState.IsBodyDashing(body)) return; // or blend weight handling
  ```
  i.e. the locomotion crossfade must not run while the dash state owns the body —
  otherwise the 0.12 s crossfade continuously fights the controller's dash sub-state.
  With the parameters in place, the dash animation is driven by the controller's
  `isDashing` transitions and the state machine driver simply stands down for the
  0.55 s. Until that edit is made, the parameter writes are harmless and the dash looks
  like a fast run.

---

## 6. Tuning constants used (all from `KitTuning` — none changed)

| Constant | Value | Where used |
|---|---|---|
| `KitTuning.ArcStepMaxStock` | 2 | SkillDef.baseMaxStock |
| `KitTuning.ArcStepRecharge` | 5.0 s | SkillDef.baseRechargeInterval |
| `KitTuning.ArcStepDuration` | 0.55 s | dash clock end condition + speed taper |
| `KitTuning.ArcStepSpeed` | 16 m/s | initial dash speed, tapering linearly to 8 m/s at exit |
| `KitTuning.ArcStepGrantsIFrames` | **false** | PROPOSAL branch, kept constant-only, never hardcoded true |

Local PROPOSAL tunables (in `ArcStepStateTuning`, my folder, marked for M5):
`InputDeadzone = 0.1`, `ExitSpeedFraction = 0.5`, `PreserveVerticalVelocity = false`.

SkillDef fields set explicitly (no reliance on ScriptableObject defaults):
`baseRechargeInterval=5`, `baseMaxStock=2`, `rechargeStock=1`, `requiredStock=1`,
`stockToConsume=1`, `resetCooldownTimerOnUse=false`, `beginSkillCooldownOnSkillEnd=false`,
`cancelSprintingOnActivation=false`, `canceledFromSprinting=false`,
`forceSprintDuringState=false`, `isCombatSkill=false`, `mustKeyPress=true`,
`triggeredByPressRelease=false`, `hideStockCount=false`, `hideCooldown=false`,
`attackSpeedBuffsRestockSpeed=false`, `dontAllowPastMaxStocks=false`,
`suppressSkillActivation=false`, `fullRestockOnAssign=false`,
`interruptPriority=EntityStates.InterruptPriority.Skill`,
`activationStateMachineName="Body"`, `activationState=EntityStates.SerializableEntityStateType(ArcStepState)`,
`skillNameToken=KitTokens.ArcStepName`, `skillDescriptionToken=KitTokens.ArcStepDesc`.

---

## 7. PROPOSAL decisions (defensible defaults, flagged for Stuart's M5)

1. **I-frames** — `KitTuning.ArcStepGrantsIFrames` stays `false`; the dash checks the
   constant and applies vanilla `RoR2Content.Buffs.HiddenInvincibility` (verified field)
   for the dash duration when it flips to true. Buff add/remove is authority-gated and
   always symmetric via `OnExit`.
2. **Interruptibility** — `GetMinimumInterruptPriority() = InterruptPriority.Any`
   (anything cancels the dash). PROPOSAL: Stuart may prefer `.Skill` so primaries
   cannot cancel a step mid-flight.
3. **Zero-input dash** — with no movement input held, the dash steps along the model's
   facing rather than refusing to fire (a charged stock is never spent on a
   zero-length step). Deadzone 0.1.
4. **Exit speed** — dash speed tapers linearly 16 → 8 m/s across the 0.55 s so control
   handback flows into a run instead of a dead stop.
5. **Air dash** — planar: vertical velocity is zeroed during the step (holds altitude),
   gravity resumes on exit. `PreserveVerticalVelocity` flag exists for the glide
   alternative. The brief only says "usable in the air"; the planar reading is the
   defensible default.
6. **`mustKeyPress = true`, `isCombatSkill = false`** on the SkillDef — utility, no
   auto-fire, no combat proc implications.

---

## 8. What is NOT verified (honest list)

- **Nothing is verified in game.** No launch, no log, no playtest — per instructions.
- Controller parameter/clip wiring is **unverified by construction** — the dash clips
  are not in the current game bundle (15 of 65 clips), and the controller has no
  `isDashing`/`dashBlendX`/`dashBlendZ` parameters yet, so `ArcStepState`'s animator
  writes are currently no-ops by design.
- The `"Body"` state machine resolution (`SkillDef` → machine on the cloned body
  prefab) is reasoned from the vanilla prefab shape, not observed in a live log.
- `ContentAddition.AddEntityState` ordering (content window vs `EntityStateCatalog.Init()`)
  is reasoned from R2API's compiled guard (`CatalogBlockers.GetAvailability<EntityState>()`),
  not observed.
- Remote-client behaviour of `OnSerialize/OnDeserialize` (dash direction replication) is
  implemented per vanilla state conventions but unobserved.
- The VFX hooks are contracts only; no VFX exists yet (VFX owner's domain).
