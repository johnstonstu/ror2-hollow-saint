# RESULT — Open Circuit (SPECIAL)

Agent deliverable for the Hollow Saint M3 kit. **Nothing here is verified in game.**
No build was staged, no game was launched, no profile was touched. Everything below is
either decompile-verified against the real assemblies or reasoned from the repo docs, and
is labelled accordingly.

---

## 1. Files created

All six are new, all inside `HollowSaintMod/FoundationKit/OpenCircuit/`. **No file outside
that folder was created, modified or deleted** — verified with `git status --porcelain --
HollowSaintMod/`, which shows only the six untracked files below and zero modifications to
any tracked file.

| File | Role |
|---|---|
| `OpenCircuitRegistration.cs` | SkillDef factory + EntityState/BuffDef registration. **The coordinator's entry point lives here.** |
| `OpenCircuitState.cs` | The cast `EntityState` — plays the unfold gesture, opens the buff. |
| `OpenCircuitPulseDriver.cs` | `MonoBehaviour` that owns the 8 s pulse loop and all damage/charge. |
| `OpenCircuitBuff.cs` | The 8 s `BuffDef` — the single source of truth for "is the crown open". |
| `OpenCircuitTuning.cs` | Frame-derived constants + all PROPOSAL flags. No approved number is restated here. |
| `OpenCircuitVfxHooks.cs` | Pure event surface for the VFX owner. No prefab logic, matching `ArcStepVfxHooks`. |

### Build status (real output, not a claim)

Full rebuild (`--no-incremental`, so no file is elided from the warning list):

```
Build succeeded.
    2 Warning(s)
    0 Error(s)
```

The 2 warnings are exactly the two pre-approved ones: the transitive `MMHOOK` `NU1701` and
the `ArcStepState.cs(161,62)` `CS0162`. **I introduced no new warnings.** I did briefly
create a second `CS0162` (a `const bool` folded by the compiler made a branch dead code in
the pulse driver); the fix was to make those flags `static readonly` rather than `const`,
which keeps them single-line flips without the dead-code warning. See PROPOSAL 5.

---

## 2. EXACT public API the coordinator must call

### The one call

```csharp
using HollowSaint.FoundationKit.OpenCircuit;

var openCircuit = OpenCircuitRegistration.RegisterOpenCircuit();
// -> RoR2.Skills.SkillDef, or null on failure (logged, never thrown)
```

Signature: `public static SkillDef RegisterOpenCircuit()` in
`HollowSaint.FoundationKit.OpenCircuit.OpenCircuitRegistration`.

Idempotent. Registers all three content objects in order and returns the SkillDef:

1. `ContentAddition.AddEntityState(typeof(OpenCircuitState), out wasAdded)` → `activationState`
2. `ContentAddition.AddBuffDef(HollowSaintOpenCircuitBuff)` → the 8 s window
3. `ContentAddition.AddSkillDef(definition)` → returned to you

Returns `null` if the state or the buff fails to register. **Do not assign a null** —
`KitRegistration.Assign` already leaves the vanilla skill in place for a null SkillDef, so
a failed Open Circuit cannot take the other three installed skills down.

### Wiring into the special slot

```csharp
InstalledSlots += Assign(skillLocator.special, openCircuit, "special");
```

The existing `Assign` helper in `KitRegistration.cs` does exactly this — it calls
`slot.SetSkillInternal(definition)`, which re-points the slot while keeping the
`GenericSkill` component and therefore the native `InputBankTest` binding intact. **No
SkillLocator or GenericSkill component is swapped anywhere in this module.**

### The other two public members

```csharp
// Attach the pulse driver at install time so REMOTE clients animate. Idempotent.
// Call it right next to the existing body.gameObject.AddComponent<DischargeMeter>() line.
public static OpenCircuitPulseDriver EnsurePulseDriver(CharacterBody body);

// Read-only, for your audit. Same shape as ArcBoltRegistration.ArcBoltStateType.
public static SerializableEntityStateType OpenCircuitStateType { get; }
```

`RegisterEntityState()` and `CreateSkillDef()` are also public (matching
`ArcStepRegistration`'s shape) but `RegisterOpenCircuit()` is the normal path.

### Where to call it

Inside `KitRegistration.Install`, after the other three registrations and before the
`Assign` block. It must run in the content-provider window
(`FoundationContent.LoadStaticContentAsync`), because both `AddEntityState` and
`AddBuffDef` are gated on their catalogs still being open — verified in the R2API
decompile: each is wrapped in `CatalogBlockers.GetAvailability<T>()` and calls
`RejectContent(...)` after catalog init.

`OpenCircuitBuff.Register()` and `OpenCircuitRegistration.RegisterEntityState()` both need
`RoR2Content` to already be up. That is already true where `Install` is called.

---

## 3. The SkillDef it returns

| Field | Value | Source |
|---|---|---|
| `skillName` | `HollowSaintOpenCircuit` | matches the anim spec's machine row |
| `skillNameToken` | `KitTokens.OpenCircuitName` (`HS_SKILL_CIRCUIT_NAME`) | already registered by `KitRegistration.RegisterTokens` |
| `skillDescriptionToken` | `KitTokens.OpenCircuitDesc` (`HS_SKILL_CIRCUIT_DESC`) | same |
| `keywordTokens` | `Array.Empty<string>()` | — |
| `icon` | **`null` — REQUIRED ART** | see §5 |
| `activationStateMachineName` | `"Weapon"` | see REQUEST 2, this is unverified |
| `activationState` | the registered `SerializableEntityStateType` | from `AddEntityState` |
| `interruptPriority` | `PrioritySkill` | pre-crown cast must not be cheap-interrupted |
| `baseRechargeInterval` | `KitTuning.OpenCircuitCooldown` = **12 s** | approved |
| `baseMaxStock` / `rechargeStock` / `requiredStock` / `stockToConsume` | `1 / 1 / 1 / 1` | approved (12 s cooldown with an 8 s buff) |
| `isCombatSkill` | `true` | it deals damage |
| `mustKeyPress` | `true` | one activation per press |
| `hideStockCount` | `true` | 1 charge — no counter to show |
| `hideCooldown` | `false` | the 12 s is real feedback |
| `cancelSprintingOnActivation` | `false` | opening the crown must not drop a sprint |
| `attackSpeedBuffsRestockSpeed` | `false` | the 12 s is a flat approved number |
| `resetCooldownTimerOnUse` | `false` | uniform with the other three kit skills |

---

## 4. Required animation clips, layers, and normalized-time fractions

### Arithmetic shown

All fractions use **(frame − 1) / (end − start)**, per `docs/unity-vfx-anim-spec-20260927.md` §4.
Every one matches the coordinator's Unity spec to 4 decimals — independently recomputed, not
copied.

**"Open Circuit", frames 1–30** (span = 29)

| Marker | Frame | Arithmetic | Fraction | Action |
|---|---|---|---|---|
| `Unfold` | f6 | (6 − 1) / 29 = 5 / 29 | **0.17241** | gap-arc VFX starts (spec 0.1724) |
| `Crown active` | f22 | (22 − 1) / 29 = 21 / 29 | **0.72414** | **8 s buff applied; pulse schedule + crown VFX begin** (spec 0.7241) |
| `Recovered` | f30 | (30 − 1) / 29 = 29 / 29 | **1.00000** | return to main (spec 1.0) |

Cast duration: 29 / 24 = **1.20833 s** (spec: "cast 1.2083 s (29 f)").

**"Open Circuit hold", frames 1–25, loops** (span = 24)

| Marker | Frame | Arithmetic | Fraction |
|---|---|---|---|
| `Pulse` | f1 | (1 − 1) / 24 = 0 / 24 | **0.00000** (spec 0.0000) |

Loop length: 24 / 24 = **1.0 s** (spec: "hold clip loops at 1.0 s").

**"Open Circuit end", frames 1–22** (span = 21)

| Marker | Frame | Arithmetic | Fraction |
|---|---|---|---|
| `Recall` | f3 | (3 − 1) / 21 = 2 / 21 | **0.09524** (spec 0.0952) |
| `Recovered` | f22 | (22 − 1) / 21 = 21 / 21 | **1.00000** (spec 1.0) |

End clip length: 21 / 24 = **0.875 s** (spec: "end 0.875 s (21 f)").

### Clips and layers

| Clip | Layer | Played by | When |
|---|---|---|---|
| `Open Circuit` (1–30) | `UpperBody` (layer 1) | `OpenCircuitState.OnEnter` | once per cast, 1.20833 s |
| `Open Circuit hold` (1–25, loop) | `Halo` (layer 4) | `OpenCircuitPulseDriver` | the whole buff, looping at 1.0 s |
| `Open Circuit end` (1–22) | `UpperBody` (layer 1) | `OpenCircuitPulseDriver` | on the falling edge of the buff |

The `Halo` layer is halo-bone-masked, so the crown keeps looping **underneath** an Arc Bolt
that owns `UpperBody`. That is the reason the hold goes on layer 4 and not layer 1 — firing
mid-crown must not cancel the crown.

State names are **space form** per spec §5 (these clips are already authored that way).
The playback-rate parameter is `"attackSpeed"`, the same string the Spear and Discharge
modules use.

**None of these three clips are in the current game bundle** (spec: "Only 15 of 65 clips are
in the current game bundle"). Until the coordinator imports them, `PlayAnimation` calls name
states the runtime controller does not have. The code is null-tolerant — `GetModelAnimator()`
returns null and the gesture is simply skipped — but **this is unverified against a real
controller and the animation will not play until the clips are imported.**

---

## 5. Required VFX

Pure event surface in `OpenCircuitVfxHooks`; this module spawns nothing. All three are
REQUIRED ART and none exists yet.

| Prefab | Content | Spawn | Timing | Authority |
|---|---|---|---|---|
| `FX_HS_OpenCircuit_Crown` | crown segment shimmer + thin tethers | local, under `Halo` | from `Crown active` 0.7241 until `Recall` | server raises, VFX owner replicates |
| `FX_HS_OpenCircuit_Pulse` | long arc bolt, HDR ×8, width ×1.4 | crown → each target, **networked** | one per confirmed target per pulse | server |
| `FX_HS_OpenCircuit_GapSpark` | gap arcs flicker as segments separate | local, under `Halo` | f6 → f22 of the cast (0.1724 → 0.7241) | server |

Events: `UnfoldStarted`, `CrownActivated`, `PulseFired` (carries `Vector3[] hitPoints` so
the VFX owner can aim one bolt per target without resolving sockets),
`CrownRecalled`. Also required: a **SkillDef icon** (currently `null`) and a **buff icon**
(`BuffDef.iconSprite`, currently `null` — see REQUEST 5).

---

## 6. Tuning constants used

Every approved number is read from `KitTuning` at its single point of use. **No approved
value is restated or overridden anywhere in this module.**

| Constant | Value | Where |
|---|---|---|
| `KitTuning.OpenCircuitCooldown` | 12 s | `baseRechargeInterval` |
| `KitTuning.OpenCircuitBuffSeconds` | 8 s | `AddTimedBuff` duration in `OpenCircuitState` |
| `KitTuning.OpenCircuitPulseInterval` | 0.5 s | driver cadence |
| `KitTuning.OpenCircuitPulseDamageCoefficient` | 0.6 | `baseDamage = 0.6f * body.damage` |
| `KitTuning.OpenCircuitRadius` | 8 m | `BlastAttack.radius` |
| `KitTuning.DischargeMeterMax` | 10 | not read — the meter owns it |
| `HollowSaint.FoundationKit.KitTokens.OpenCircuitName/Desc` | — | SkillDef tokens |

Derived from the real frame numbers (in `OpenCircuitTuning`, arithmetic in §4):
`CastClipSeconds` 1.20833, `UnfoldNormalizedTime` 0.17241, `CrownActiveNormalizedTime`
0.72414, `HoldLoopSeconds` 1.0, `EndClipSeconds` 0.875, `RecallNormalizedTime` 0.09524.

---

## 7. Server authority

Every damage and charge path is gated, and re-gated at the method boundary so it is safe
even if a future caller misuses it.

| Path | Guards |
|---|---|
| Buff applied (opens the 8 s window) | `isAuthority` at the call site **+** `NetworkServer.active` **+** `body.hasEffectiveAuthority` inside `OpenCircuitServer()` |
| Pulse damage | `NetworkServer.active` **+** `hasEffectiveAuthority` at the driver tick, re-checked inside `PulseServer()`, which also re-checks `healthComponent.alive` and `HasBuff` (the buff can expire between the tick and the call) |
| Discharge charge | only reachable from `PulseServer()` after those guards |
| VFX event raises | `isAuthority` / authority-gated; animation itself is local and non-authoritative |

A client never deals damage, never awards charge, never spawns a networked effect. The
component is attached on **every** machine (the buff replicates, so clients can animate from
`HasBuff` alone) but its damage path is dead off-authority.

---

## 8. Design decision worth the coordinator's attention

**The pulses do not live in the EntityState.** The approved skill is a 12 s cooldown
granting an 8 s buff, but the cast gesture is only 1.20833 s. If the pulses lived in
`OpenCircuitState` they would either stop at 1.2 s (the buff would be a lie) or hold the
state machine for 8 s (the character could not fire, dash or aim for 8 s, and death would
truncate the buff).

So: the `EntityState` casts and hands control straight back, and
`OpenCircuitPulseDriver : MonoBehaviour` ticks off the **buff**. The buff is the game's own
replicated object (`CharacterBody.WriteBuffs` / `ReadBuffs` / `AddTimedBuff` /
`HasBuff(BuffDef)` — all decompile-verified), so the 8 s window is independent of what the
character is doing, and nothing has to be cleaned up on exit.

This is also what makes the PROPOSAL 1 default free — see below.

---

## 9. PROPOSAL decisions parked for M5

**1. Can Open Circuit strike during glide or Arc Step? → DEFAULT: ALLOWED.**
Flag: `OpenCircuitTuning.AllowPulsesDuringGlideAndArcStep = true`. The architecture grants
it for free — the driver is not on the state machine, so a dash cannot suspend it. Flipping
it to `false` makes the driver consult `ArcStepState.IsBodyDashing(body)` (that method was
added by the Arc Step agent for exactly this question). **The glide half cannot be
enforced**: glide is presentation-only in this build (layer-0 animator states), with no
code-level predicate to query. See REQUEST 4.

**2. `FirstPulseIsImmediate = true`.** The first pulse fires on the tick the crown lights,
so damage lands on the same frame as the ignition flash. `false` costs one pulse per buff
(17 → 16).

**3. Charge rate — needs your ruling.** One charge per pulse that hit at least one enemy.
That is the same "one charge per damage event" unit `ArcBoltChainServer` uses for a whole
chain, rather than one per victim. Worth knowing: **8 s / 0.5 s + the immediate first pulse
= 17 pulses = 17 charge opportunities against a 10-charge meter**, so one activation can
fill the meter 1.7× over. A per-victim award would be far worse (a pulse hitting 5 enemies
would be 5 charges). If you want one activation to be worth roughly one meter, dividing
the award by 2 is a one-constant change in `PulseServer()`. **I did not pick a divisor
because that is a balance decision, not a mechanical one.**

**4. The buff is never re-applied while running**, because cooldown 12 s > buff 8 s. If you
ever shorten the cooldown below the buff, `AddTimedBuff` (refresh) vs
`AddTimedBuffDontRefreshDuration` becomes a real choice.

**5. PROPOSAL flags are `static readonly`, not `const`.** A `const bool` is folded by the
compiler, so branching on it emits `CS0162`. Only the ArcStep file carries that warning, so
these stay `readonly` — still single-line, one-place flips.

**6. Pulses never crit** (`const bool crit = false` in `PulseServer`), and
**`procCoefficient = 0`** — pulses fill the meter (that is the point) but must not become
the kit's best item-proc engine. `DischargeEmitter` made the same no-crit call for the
same reason. Both are one-line flips.

**7. The buff is hidden from the HUD** until its icon exists
(`HideBuffFromHudUntilArtExists = true`). A `BuffDef` with a null `iconSprite` still
reserves a HUD buff-bar slot, and Vanilla's `BuffBar` is **not verified** to tolerate a null
sprite. `isHidden` affects display only — `HasBuff`, the pulse schedule and replication are
all identical either way.

---

## 10. REQUESTS for files I do not own

**REQUEST 1 — add the pulse driver at install time (one line, `KitRegistration.cs`).**
Next to the existing `body.gameObject.AddComponent<DischargeMeter>()` line:

```csharp
OpenCircuitRegistration.EnsurePulseDriver(body);
```

`OpenCircuitState` self-attaches the driver on first cast, so the **server always pulses**
and the skill works even if this line is missed. Without it, however, a **remote client
never gets the component** and therefore never plays the hold loop or the recall beat — the
server's crown would open silently on their screen.

**REQUEST 2 — verify the special slot exists on the cloned Commando body.** This is the
one genuine unknown in the wiring. Vanilla Commando has **no special skill**, so
`skillLocator.special` may be `null` on the clone. If it is, my `SkillDef` is correct but
unassignable and the skill will not fire at all. The coordinator owns the body prefab, so
please confirm — and if the slot is missing, a `GenericSkill` needs adding to the prefab
(whose component, not mine, to add). Relatedly, `"Weapon"` as
`activationStateMachineName` is inherited from ArcBolt/Spear, which verified it on this
clone; if the special slot ends up on a different machine, that constant is the single line
to change.

**REQUEST 3 — import the three Open Circuit clips and the animator states** listed in §4.
Not in the current bundle. Until then the gesture silently does not play.

**REQUEST 4 — a glide predicate, if PROPOSAL 1 is ever set to false.** Nothing in code can
currently answer "is this body gliding" — glide is layer-0 presentation only. A small
`IsGliding(CharacterBody)` hook on a frozen file (or a new shared helper in `KitShared.cs`,
which is append-only) would let the driver enforce the glide half.

**REQUEST 5 — supply a buff icon, or confirm the HUD-hidden default.** Then
`OpenCircuitTuning.HideBuffFromHudUntilArtExists` flips to `false` so the player can see the
8 s window running.

**REQUEST 6 — consider a Discharge charge divisor.** See PROPOSAL 3. This one needs a
number from you; I deliberately did not invent one.

---

## 11. What is NOT verified

**Nothing in this module is verified in game. There is no in-game evidence for any of it.**

- The code **compiles clean** (0 errors, only the 2 pre-approved warnings) — that is
  verified, and it is the only thing that is.
- Every API used was checked by decompiling the real assemblies
  (`RoR2.dll` from the GameLibs 1.4.1-r.0 package, and `R2API.ContentManagement.dll` from
  the dev profile). Decompiled **signatures and fields** are authoritative; the method
  **bodies** in that output are `throw null;` artefacts of the stripped assembly and were
  treated as such, never as behaviour.
- **Unverified by construction, and unverifiable until Stuart launches and we read a fresh
  log:** that the skill activates at all; that the 8 s buff replicates and the timing feels
  right; that the pulse cadence and radius feel correct; that the Discharge meter fills at
  a satisfying rate; that any animation plays (the clips are not in the bundle); that the
  special slot on the cloned body even exists (REQUEST 2); that a remote client sees the
  crown (REQUEST 1); that `isHidden` on a null-icon `BuffDef` is safe on Vanilla's
  `BuffBar`; and that `PlayAnimation` on a missing state degrades quietly rather than
  warning every cast.

The staged DLL in the profile was **not** touched — staging is the coordinator's job after
review. The game and r2modman were not launched. No git commit, no push. Nothing was written
to any game install or profile directory.
