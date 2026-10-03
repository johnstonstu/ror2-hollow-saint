# Handoff to Opus 5.5 — Hollow Saint, September 27, 2026

**Written by Hermes, at Stuart's request, so a different agent can take over
polishing. Read this file first, then `HANDOFF.md`, then the docs it names.**

## TL;DR — where this actually is

M1 is **verified in game**. M3's four skills are **written, integrated, build-green
and code-reviewed** — but **not one line of M3 has ever been launched**. You are
polishing a kit that has never run. That is the single most important sentence in
this document, and it should govern your first hour of work.

| Milestone | State |
|---|---|
| M1 foundation (mannequin + animation contract) | **Verified in game** by Stuart, 22:22 PT |
| M2 controller integration | Blocked on Unity — Stuart's editor holds the project |
| M3 kit — 4 skills + passive | **Code-complete, reviewed, build-green, never run** |
| VFX / animation import | Specification only, 50 of 65 clips unimported |
| Staged for playtest | **Nothing.** Profile still holds the verified M1 DLL |

Local commits, **nothing pushed, nothing on Thunderstore**:
`208b68bb` → `d7b768f9` → `21c638e8` → `8f99776f` → `8c07d50b` → `6f026b55` → `2700d5e0` → `3c13e048`

## The four skills, and how they are wired

| Slot | Skill | Module |
|---|---|---|
| Primary | **Arc Bolt** — auto-firing chained hitscan bolt, 4 targets | `FoundationKit/ArcBolt/` |
| Secondary | **Conduit Spear** — thrown spear, applies Conductor Mark | `FoundationKit/SpearDischarge/` |
| Utility | **Arc Step** — 2-stock air dash, holds altitude | `FoundationKit/ArcStep/` |
| Special | **Open Circuit** — 8s crown, 0.5s pulse ticks, AoE | `FoundationKit/OpenCircuit/` |
| Passive | **Discharge** — arc hits charge a meter; at full the next hit bursts | `FoundationKit/SpearDischarge/DischargeEmitter.cs` |

**The load-bearing design decision:** the temporary Commando kit was retired by
re-pointing the body's *existing* `GenericSkill` components with
`SetSkillInternal`, not by replacing them. `InputBankTest → GenericSkill` native
input is therefore completely untouched. **Do not "clean this up" by rebuilding
the skill components** — that is the one refactor guaranteed to break input.

## Do not re-learn these two API facts

Both were found by subagents, cost real time, and are invisible to the compiler:

1. **`EntityStates.EntityState` is a plain class, NOT a MonoBehaviour.** You cannot
   add it as a child GameObject. Register with
   `R2API.ContentAddition.AddEntityState(typeof(YourState), out bool wasAdded)` and
   assign the returned `SerializableEntityStateType` to `SkillDef.activationState`.
   Must happen before `EntityStateCatalog.Init()`.
2. **`SerializableEntityStateType` IS constructible**, in namespace `EntityStates`,
   **not** `RoR2`. A "type not found" result almost always means a wrong-namespace
   lookup, not a missing type.

Type names: `RoR2.Skills.SkillDef` (not `RoR2.SkillDef`), `EntityStates.EntityState`
(no `RoR2` prefix). The authoritative assembly is the **nuget GameLibs** package,
not the game's `RoR2.dll`:
`C:/Users/stuwj/.nuget/packages/riskofrain2.gamelibs/1.4.1-r.0/lib/netstandard2.0/RoR2.dll`

`ilspycmd` needs `C:/` paths — MSYS `/c/` paths fail with "file does not exist".
**Every decompiled method body prints `throw null;`** because the shipped assembly
is stripped. That is a decompiler artefact, not real behaviour; trust signatures
and fields only, and never infer control flow from a stripped body.

## Hard constraints — breaking any of these loses work

- **The mod runs ONLY from**
  `C:/Users/stuwj/AppData/Roaming/r2modmanPlus-local/RiskOfRain2/profiles/Hollow Saint Dev`
- **The vanilla Steam install is READ-ONLY.** Never write to
  `C:/Program Files (x86)/Steam/steamapps/common/Risk of Rain 2`. Reading `RoR2.dll`
  from it for decompilation is fine.
- **The profile has no `Managed/` directory of its own** — it shares the vanilla
  Data folder. A DLL copied into the vanilla folder is a **silent no-op**, not an error.
- **Stuart launches and playtests the game.** Never automate the game or r2modman.
- **Staging the DLL is the coordinator's job**, and only after a build and a review.
  The currently staged DLL is the M1 build Stuart verified — do not overwrite it
  casually; it is the known-good rollback point.
- **Unity and Blender belong to Stuart and are/were open.** Never launch them, never
  use blender-mcp, never write into `Library/` or `Temp/`. All VFX/anim work is
  spec-only until he closes the editor. `HollowSaintUnityProject/` is ~796 MB and
  untracked — there is **no rollback cover** for it.
- **Local commits only.** No push, no release, no profile export, no Thunderstore
  publish, without Stuart's explicit go.
- **`demo time` and `demo time new` are untouched reference profiles.** Never modify.
- **No input replacement.** Never introduce `KeyCode`, `Input.GetKey`, or joystick
  polling. Animation **reads** movement, it never replaces it. Animation never
  bypasses `CharacterMotor`.
- **All damage, Discharge charge and cooldown consumption are server-authoritative.**
  Guard with `NetworkServer.active` / `hasEffectiveAuthority`.

## What the review found, and what is STILL open

An independent adversarial reviewer (`docs/kit-review-20260927.md`, 1 CRITICAL /
8 SERIOUS / 20 MINOR / 12 false alarms) audited the three original modules. I fixed
C1, S1, S2 and S8. **Still open — these are the real multiplayer risks and none can
be settled without a 2-client test:**

- **S3 — the Discharge meter is not replicated.** A client never sees the meter fill,
  never sees the full-charge flourish. Its own class doc claims it is networked.
- **S5 — the meter's authority check fails OPEN**, so a non-authoritative caller can
  award charge. This is the inverse of the project's rule and should fail closed.
- **S6 — the `HsDamageSource` tag is a call-site literal**, not part of the damage
  data model, so a client or an item can forge it. The design depends on it to reject
  item procs.
- **S7 — the vanilla Commando special and passive are still installed** alongside the
  kit. Decide whether to remove them.
- **S9 (S8) — the 256-entry overlap buffer silently truncates** if a future radius
  exceeds it. Noted in a comment at the declaration.

**Fixed since the review was written:** C1 (Arc Step's fatal throw — the 99% stall
reproducer), S1 (Conductor Mark was never registered; the whole secondary-skill
feature was dead and silent), S2 (`DischargeMaxChainTargets = 6` never enforced, so a
3.0x Discharge hit everything in 10m), S8 (`Install` not idempotent).

### Two Open Circuit decisions now made (were the agent's open questions)

- **The special slot may not have existed.** Vanilla Commando has no special skill,
  so `skillLocator.special` on the clone is very likely null — which would have left
  Open Circuit registered, logged as installed, and permanently unplayable. It could
  not be settled statically (the Addressables bundles are compressed; `PlayerControls`
  is not in the reference assembly), so `KitRegistration.AssignSpecial` now handles
  both cases: reuse the slot, or add a `GenericSkill` and point the locator at it.
  Input is untouched either way. **Check the launch log** for
  `HOLLOW_SAINT_KIT_INSTALLED slots=4` to confirm Open Circuit actually installed.
- **Charge rate is a placeholder awaiting Stuart.** ~17 pulse-ticks per activation
  against a 10-charge meter overfills it 1.7x, which would make Open Circuit a
  strictly better charge source than Arc Bolt. The divisor is a single tunable,
  `KitTuning.OpenCircuitChargeEveryNPulses = 3`, so one activation is worth ~5.7 of
  10 charge. **This number is Stuart's call — change it, don't just accept it.**

**A caution about the review itself:** its S4 finding was a **false alarm** — it
claimed the projectile `consumed` flag was set before the owner self-hit test, which
would permanently inert both projectiles. The actual code has the owner test at
`ArcBoltProjectile.cs:140` *before* `consumed = true` at `:142`. I checked before
acting and rejected it. **Verify review claims against the source before changing
code** — that one would have been a pointless "fix" to correct code.

## Known cosmetic gaps (safe to leave, or your first polish targets)

- Both projectiles use a **Mage lightning bolt** as template, so the *spear looks
  like lightning*. The template path is verified to resolve
  (`docs/projectile-template-resolution-20260927.md`).
- Animator state names in the kit are **placeholders** until the 50 clips import.
- No VFX prefabs are wired. `docs/unity-vfx-anim-spec-20260927.md` specifies 25.
- `FoundationMaterials.cs` is still a diagnostic pass; `ignoreOverlays=true`
  everywhere.
- The `140-renderer / 4-slot` body-split invariant must survive any re-export.
- `docs/kit-wiring-api-20260927.md` was written **before** the two API corrections
  above and still contains the disproven child-GameObject advice — **rewrite it.**

## Suggested order of work

1. **Launch and smoke-test M3** — the single highest-value action, and the only one
   that can invalidate everything above. All four skills, one client. Watch for
   `HOLLOW_SAINT_KIT_INSTALLED slots=4` in the profile's `BepInEx/LogOutput.log`.
2. **Fix S3 / S5 / S6** (networking) *after* you know the kit runs at all.
3. **Then** Unity/VFX/animation, once Stuart has closed the editor.
4. Rewrite the stale wiring doc whenever you touch that area.

## Where the evidence lives

| Path | What |
|---|---|
| `…/Hollow Saint Dev/BepInEx/LogOutput.log` | The only real proof. Read it after every launch. |
| `HANDOFF.md` | Living handoff; updated for the M3 state |
| `docs/kit-contract-20260927.md` | Ownership table, constraints, **the two-install topology** |
| `docs/kit-review-20260927.md` | The adversarial review, in full, with false alarms marked |
| `docs/unity-vfx-anim-spec-20260927.md` | 50-clip import plan, 5-layer graph, 25 VFX prefabs |
| `docs/projectile-template-resolution-20260927.md` | Proof the projectile path resolves |
| `docs/next-phase-plan-20260927.md` | The phased plan this work follows |

**Good luck. The risk is not the code — it is that none of it has met the game yet.**
