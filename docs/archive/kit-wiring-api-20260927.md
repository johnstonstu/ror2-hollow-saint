# Coordinator findings — skill wiring API (decompiled, verified September 27, 2026)

Kept separate from `kit-contract-20260927.md` because it was established *after*
dispatching the kit agents, and it answers the wiring question the coordinator
reserves. Agents should read it before assuming a wiring approach.

**All verified by decompiling the installed GameLibs assembly**, not from tutorials:

```bash
ilspycmd -t RoR2.GenericSkill "C:/Users/stuwj/.nuget/packages/riskofrain2.gamelibs/1.4.1-r.0/lib/netstandard2.0/RoR2.dll"
```

`ilspycmd` is a native Windows binary — `/c/...` MSYS paths fail with
"File does not exist". Always use `C:/...`.

Note: decompiled bodies show `throw null;` for method bodies. That is an artefact
of the decompiler on stripped game assemblies — the signatures and fields are
real and authoritative, the bodies are not. Trust the shape, not the body.

## Replacing the Commando kit without breaking native input

The foundation clones `Prefabs/CharacterBodies/CommandoBody`, so the body still
carries Commando's `GenericSkill` components. We do **not** replace the
`SkillLocator` or the `GenericSkill` components — that would cost us the native
input binding the whole controller-first design depends on.

Instead, swap the `SkillDef` *inside* each existing slot:

```csharp
// RoR2.GenericSkill — verified public
public void SetSkillInternal(SkillDef newSkillDef);
public void SetBaseSkill(SkillDef newSkillDef);
public void UnassignSkill();
```

`SetSkillInternal` is the right call: it re-points the slot at our SkillDef while
leaving the `GenericSkill` component — and therefore `InputBankTest` → skill
activation — untouched. This is how the placeholder Commando kit gets retired
without touching input.

`SkillLocator` slots to repoint (verified): `primary`, `secondary`, `utility`,
`special`, plus `passiveSkill` (`SkillLocator.PassiveSkill` with `enabled`,
`skillNameToken`, `skillDescriptionToken`, `keywordToken`, `icon`).

## How a SkillDef finds its EntityState

`RoR2.Skills.SkillDef` has **both** of these; the string one is the safe path here:

- `activationStateMachineName` (string)
- `activationState` (`SerializableEntityStateType`)

`SerializableEntityStateType` is **not resolvable by name in the GameLibs
assembly** — `ilspycmd -t RoR2.SerializableEntityStateType` reports the type
definition cannot be found, even though `SkillDef.activationState` is typed with
it. This is the same class of problem the handoff already recorded for
`ChildLocator.transformPairs` and `ContentPack.identifier`: **check real assembly
visibility before trusting a type name.** Constructing one from script is
therefore fragile; prefer `activationStateMachineName` plus an explicitly
registered `EntityState` child.

`EntityStateMachine` (verified) has:
`SetNextState(EntityState)`, `SetNextStateToMain()`, `SetState(EntityState)`,
`public EntityState nextState`, `public string customName`,
`initialStateType`, `mainStateType`, `nextStateModifier`, and a
`CommonComponentCache` giving direct access to `characterBody`,
`characterMotor`, `characterDirection`, `inputBank`, `skillLocator`,
`healthComponent`, `projectileController`, `sfxLocator` and more.

**Wiring consequence:** each custom EntityState should be added as a child
GameObject on the body prefab, and its state machine reached by
`activationStateMachineName`. Because `EntityState` is a plain MonoBehaviour,
adding these as components on cloned prefab children is the
`PrefabAPI.InstantiateClone` path the foundation already uses successfully.

## Damage application

`RoR2.DamageInfo` verified fields: `damage`, `crit`, `inflictor`, `attacker`,
`procCoefficient`, `damageType` (`DamageTypeCombo`), `canRejectForce`, `rejected`.

The `attacker` field is the hook for kit damage attribution: `KitUtil.IsHollowSaintSkillDamage`
walks `attacker.GetComponentInParent<CharacterBody>()` and checks
`baseNameToken == "HS_NAME"`. Combined with an explicit `HsDamageSource`, that is
what keeps item procs from filling the Discharge meter — an item proc is not
issued by one of our skills, so it never carries our `HsDamageSource`.

## Assembly-visibility traps (do not rediscover these)

| Symbol | Status | Use instead |
|---|---|---|
| `ChildLocator.transformPairs` | inaccessible | public `AddChild` |
| `ContentPack.identifier` setter | inaccessible | populate public output collections |
| `RoR2.SerializableEntityStateType` | not constructible by name | `activationStateMachineName` + registered state |
| `RoR2.Skills.SkillDef` | **not** `RoR2.SkillDef` | namespace is `RoR2.Skills` |
| `EntityStates.EntityState` | not `RoR2.EntityStates.EntityState` | namespace has no `RoR2` prefix |
| GameLibs vs game DLL | types live in GameLibs | decompile the nuget package, not the game folder |

That last pair cost real time this session: `ilspycmd -t RoR2.SkillDef` against
the GameLibs assembly returns "Could not find type definition", and the correct
name is `RoR2.Skills.SkillDef`. Verified type names in use so far:
`RoR2.Skills.SkillDef`, `EntityStates.EntityState`, `RoR2.GenericSkill`,
`RoR2.SkillLocator`, `RoR2.DamageInfo`, `RoR2.Projectile`, `RoR2.CharacterBody`,
`RoR2.ChildLocator`, `RoR2.CharacterModel`, `RoR2.ModelSkinController`.
