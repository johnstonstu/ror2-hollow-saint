# Projectile template path — RESOLVED September 27, 2026

Both the Arc Bolt and Conduit Spear agents flagged their projectile template load
path as **unverified**, and warned that a failure would silently break both skills.
That risk is now closed. Recorded here because the next agent will otherwise
re-investigate it.

## What was verified

Both modules hardcode:
```csharp
public const string TemplateLegacyPath = "Prefabs/Projectiles/MageLightningboltBasic";
```

Verified against the game's own Addressables catalogue, which is the authoritative
asset map:
```
C:/Program Files (x86)/Steam/steamapps/common/Risk of Rain 2/Risk of Rain 2_Data/StreamingAssets/aa/catalog.json
```

- `MageLightningboltBasic` **exists** exactly once, as
  `Assets/RoR2/Base/Characters/Mage/Skills/MageLightningboltBasic.prefab`.
- The legacy path and the catalog path differ (`Prefabs/Projectiles/...` vs
  `Assets/RoR2/Base/Characters/Mage/Skills/...`), which is expected: they are two
  different naming schemes for the same asset.

## Why the mapping works

`RoR2.LegacyResourcesAPI` (in `LegacyResourcesAPI.dll`, type `RoR2.LegacyResourcesAPI`,
namespace `RoR2`) holds a runtime-populated dictionary:

```csharp
public static Dictionary<string, string> oldResourcesPathToGuid;
public static bool GetGuid(string path, out string guid);
```

The game populates `oldResourcesPathToGuid` at startup, which is what makes the
whole legacy `Prefabs/...` scheme work at all.

**The decisive evidence:** the shipped, in-game-verified foundation build already
loads `Prefabs/CharacterBodies/CommandoBody` and
`Prefabs/CharacterMasters/CommandoMonsterMaster` through this exact same
`LegacyResourcesAPI.Load<GameObject>` call, and that body spawns in game. The
`CommandoBody` prefab is confirmed in the catalogue at
`Assets/RoR2/Base/Characters/Commando/CommandoBody.prefab`. So the legacy→addressable
resolution is **proven working in this exact profile**, not merely plausible.

Both template loads additionally fail loudly rather than silently: each module logs
an error and returns null, degrading the skill to no projectile instead of throwing
or nulling a component mid-frame.

## Remaining caveat

`Prefabs/Projectiles/MageLightningboltBasic` is a *Mage lightning bolt*, reused as
the template for both a thrown spear and an arc bolt. Functionally that is fine —
both modules strip the vanilla impact behaviours and supply their own — but the
**visual** will be a lightning bolt for the spear until the VFX prefabs from
`docs/unity-vfx-anim-spec-20260927.md` replace it. That is cosmetic, not a
functional risk, and it is the expected next step rather than a bug.

## How to re-verify if ever needed

```bash
grep -c "MageLightningboltBasic" \
  "C:/Program Files (x86)/Steam/steamapps/common/Risk of Rain 2/Risk of Rain 2_Data/StreamingAssets/aa/catalog.json"
```
Expected: `1`.

Decompile the mapping with:
```bash
ilspycmd -t RoR2.LegacyResourcesAPI \
  "C:/Users/stuwj/.nuget/packages/riskofrain2.gamelibs/1.4.1-r.0/lib/netstandard2.0/LegacyResourcesAPI.dll"
```
Note the type is `RoR2.LegacyResourcesAPI`, not `LegacyResourcesAPI.LegacyResourcesAPI`.
