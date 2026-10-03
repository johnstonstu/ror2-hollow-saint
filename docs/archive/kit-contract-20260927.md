# Hollow Saint M3 kit contract — shared rules for parallel work

**Every agent working on the Hollow Saint kit must read this first.** It exists so
parallel agents do not walk over each other or guess at APIs. Verified against the
actual installed assemblies on September 27, 2026 — not from tutorials.

## Game install topology (two locations — this is a known trap)

There are **two** RoR2 locations on this machine and they are **not interchangeable**.

**1. The modded profile — the only place the mod actually runs from:**
```
C:/Users/stuwj/AppData/Roaming/r2modmanPlus-local/RiskOfRain2/profiles/Hollow Saint Dev
  BepInEx/plugins/JohnstonStu-HollowSaint/HollowSaint.dll   <- our plugin
  BepInEx/plugins/JohnstonStu-HollowSaint/hollowsaintassets <- the AssetBundle
  BepInEx/LogOutput.log                                     <- the game's log
  BepInEx/plugins/                                          <- 34 support mods (R2API suite etc.)
```

**2. The vanilla Steam install — reference material only, never write here:**
```
C:/Program Files (x86)/Steam/steamapps/common/Risk of Rain 2
  Risk of Rain 2_Data/Managed/RoR2.dll
```

**The trap:** the profile has **no `Managed/` folder of its own** — it shares the
vanilla install's Data folder. So a DLL copied into the vanilla install is **not
loaded** by the modded profile; it is a silent no-op. Reading `RoR2.dll` from the
vanilla path for API decompilation is correct and expected. Writing there is not.

For decompiling, prefer the nuget GameLibs package — same API surface, no game
install involved:
```
C:/Users/stuwj/.nuget/packages/riskofrain2.gamelibs/1.4.1-r.0/lib/netstandard2.0/RoR2.dll
```

`ilspycmd` is a native Windows binary: use `C:/...` paths, never `/c/...` MSYS paths.

**Coordinator-owned, do not touch:** the staged `HollowSaint.dll` in the profile.
It is the last build Stuart launched and verified in game. Staging and backing up
is the coordinator's job, after code review. Never stage, overwrite, sync or
"helpfully install" it from an agent task.

**Never touch:** the `demo time new` and `demo time` profiles. They are Stuart's
untouched reference profiles and must stay byte-identical.

**Never launch the game or r2modman.** Stuart launches and playtests personally.
The game must be stopped while agents work.

## Hard constraints (do not violate)

- **Unity editor PID 50188 and Blender PID 4500 belong to Stuart. Never touch them.**
  Do not launch Unity, do not use blender-mcp, do not write into
  `HollowSaintUnityProject/Library/`, `Temp/`, `Logs/`, or `UserSettings/`.
  Any Unity asset work in this phase is **read-only reconnaissance** or a written
  spec — a human runs the editor.
- **No `KeyCode`, `Input.GetKey`, or joystick APIs.** Native `CharacterMotor`,
  `InputBankTest` and `SkillLocator` are retained. Input arrives via the skill system.
- **Server-authoritative.** Anything that deals damage, charges, or consumes a
  cooldown must check `hasEffectiveAuthority` / `NetworkManager.isServer` and no-op
  otherwise. The client must never apply damage.
- **No game launch, no UI automation.** Stuart launches through r2modman.
- **Do not push to git.** Local commits only, and only the coordinator commits.
- **Numbered outputs, never overwrite.** New bundle folder is `GameFoundation02/`.

## File ownership (one owner per file — this is how we avoid collisions)

| File | Owner |
|---|---|
| `HollowSaintMod/FoundationKit/**` | each agent owns only its own subfolder |
| `HollowSaintMod/Plugin.cs` | **coordinator only** |
| `HollowSaintMod/FoundationContent.cs` | **coordinator only** |
| `HollowSaintMod/FoundationAudit.cs` | **coordinator only** |
| `HollowSaintMod/Foundation{Body,Skin,Materials,MeshSplitter,Mounts,Presentation}.cs` | **frozen — do not edit** |
| `HollowSaintUnityProject/**` | **frozen in this phase** |
| `art/**` | **frozen — do not edit** |

If you need a change in a file you do not own, **write it in your result document
as a request to the coordinator.** Do not edit it. Do not create a partial duplicate
of someone else's file.

Shared constants, tokens and helpers live in
`HollowSaintMod/FoundationKit/KitShared.cs` and are **append-only**: add new members,
never rename or remove an existing one, because four agents compile against it
simultaneously.

## Verified RoR2 1.4.1 API (decompiled from the installed assemblies)

These are real. Use them. If you need something not listed here, decompile it
yourself rather than guessing:

```bash
ilspycmd -t RoR2.Skills.SkillDef "C:/Users/stuwj/.nuget/packages/riskofrain2.gamelibs/1.4.1-r.0/lib/netstandard2.0/RoR2.dll"
```

Note: `ilspycmd` is a native Windows binary — **MSYS paths like `/c/...` do not
work, use `C:/...`**. Type names are `RoR2.Skills.SkillDef`,
`EntityStates.EntityState`, etc., and live in the **GameLibs** package
(`riskofrain2.gamelibs/1.4.1-r.0`), not the game's `RoR2.dll`.

### SkillDef (`RoR2.Skills.SkillDef : ScriptableObject`)

Fields that matter: `skillName`, `skillNameToken`, `skillDescriptionToken`,
`keywordTokens[]`, `icon` (Sprite), `activationStateMachineName` (string),
`activationState` (`SerializableEntityStateType`), `interruptPriority`,
`baseRechargeInterval` (float seconds), `baseMaxStock` (int), `rechargeStock`,
`requiredStock`, `stockToConsume`, `resetCooldownTimerOnUse`, `isCombatSkill`,
`mustKeyPress`, `hideStockCount`, `hideCooldown`, `cancelSprintingOnActivation`.
Nested `BaseSkillInstanceData` for per-instance state.

### SkillLocator (`RoR2.SkillLocator`)

`primary`, `secondary`, `utility`, `special` are all `GenericSkill`.
Also `passiveSkill` (`SkillLocator.PassiveSkill`: `enabled`, `skillNameToken`,
`skillDescriptionToken`, `keywordToken`, `icon`), and `allSkills` / `AllSkills`.

### GenericSkill (`RoR2.GenericSkill`)

`skillName`, `baseStock`, `baseRechargeStopwatch`, `skillDef`, `baseSkill`,
`skillFamily`, plus `GenericSkill.SkillOverride` and `SkillOverrideHandle` for
cooldown/stock manipulation. `MustKeyPress` is read from the SkillDef, not here.

### EntityState (`EntityStates.EntityState`)

Extend this for custom states. `EntityStateMachine` drives them; skill
`activationState` resolves to a state machine by name on the body prefab.

### DamageInfo (`RoR2.DamageInfo`)

`damage`, `crit`, `inflictor`, `attacker`, `procCoefficient`, `damageType`
(`DamageTypeCombo`), `canRejectForce`, `rejected`.

### Other confirmed types
`RoR2.Projectile`, `RoR2.CharacterBody`, `RoR2.ChildLocator` (public `AddChild` —
**`transformPairs` is inaccessible**, a previously diagnosed trap),
`RoR2.ModelSkinController` (`[RequireComponent(typeof(CharacterModel))]`),
`RoR2.CharacterModel` (one base material per `RendererInfo` — the rear-shell
regression cause), `RoR2.Teleporter`, `EntityStates.GenericProjectileBaseState`.

**Known API traps already hit and fixed — do not reintroduce:**
- `ChildLocator.transformPairs` → inaccessible. Use public `AddChild`.
- `ContentPack.identifier` setter → inaccessible. Populate the public output collections.
- Check real assembly visibility before trusting a tutorial or package facade.

## Approved kit (Stuart's numbers — do not silently change these)

Cooldowns and tuning stay configurable in `KitTuning.cs`.

| Skill | Slot | Behaviour |
|---|---|---|
| **Arc Bolt** | primary | aimed, auto-chains, 0.5 s interval, next shot interrupts at f13 |
| **Conduit Spear** | secondary | 5 s cooldown, right-hand javelin throw, left arm points at target, release at f7 of 20f, usable while moving (upper-body layer), ~150 m/s projectile, 450% single target, 6 s conductor mark |
| **Arc Step** | utility | 2 charges × 5 s, 4 directions (diagonals by blend), usable in the air |
| **Open Circuit** | special | 12 s cooldown with an 8 s buff |
| **Discharge** | passive | meter fills (~10 Arc Bolt hits) from **every** damaging skill including Open Circuit pulses; **item procs give 0**; at 100% fires on the next enemy hit as an upper-body overlay |

**Discharge is passive and authoritative.** Charge only accrues on the server, only
from a *successful* hit by one of Hollow Saint's own damaging skills, never from an
item proc. At 100% it fires on the next enemy hit. A full meter must not be able to
recursively trigger twice.

Open questions Stuart **parked for M5** — implement a defensible default, mark it
`PROPOSAL` in a comment, and list it in your result: Arc Step i-frames (default
none), heel jets on jump/land, whether Open Circuit can strike during glide or
Arc Step.

## Animation assets that already exist (do not re-author)

All clips exist in the Blender source with markers already placed. Frames are 1-based
inclusive, at 24 fps. Markers are named strings in the catalog.

| Clip | Frames | Markers |
|---|---|---|
| Arc Bolt right / left | 1–20 | Anticipation, **Bolt release**, Interrupt, Recovered |
| Conduit Spear | 1–20 | Materialize, Draw, **Spear release**, Cancel, Fade, Recovered |
| Arc Step start / loop / end | 1–7 / 1–11 / 1–16 | **Dash start** / — / Arrive, Cancel, Recovered |
| Arc Step back/left/right start/loop/end | same as above | same |
| Open Circuit / hold / end | 1–30 / 1–25 / 1–22 | Unfold, **Crown active** / Pulse / Recall, Recovered |
| Discharge | 1–28 | Gather, Release, Recovered |
| Discharge snap | 1–14 | Release, Recovered |
| Charge loop / Charge full | 1–41 / 1–25 | Pulse high, Pulse low / Pulse high |
| Meter full flourish | 1–24 | Full, Release, Recovered |

**Only 15 of 65 clips are in the current game bundle.** Importing the rest is
coordinator-owned Unity work in a later step — each agent should *declare* which
clips and layers its skill needs, and at what frame fractions the EntityState should
fire effects, expressed as marker names plus normalised time. Do not assume a state
name exists in the current controller.

## Definition of done for a skill agent

1. Code compiles: `dotnet build HollowSaintMod/HollowSaint.csproj -c Release --no-restore`
   shows **0 errors**. (One pre-existing transitive MMHOOK `NU1701` warning is expected.)
2. No edits outside your owned folder.
3. Server-authority guards on all damage/charge/cooldown paths.
4. A result document listing: files created, public API surface other agents may call,
   required animation clips/layers, required VFX, tuning constants used, proposals,
   and anything you could not verify.
5. **No claim of in-game verification.** Nothing is verified until Stuart launches and
   we read a fresh log. Say "unverified by construction" where that is the truth.
