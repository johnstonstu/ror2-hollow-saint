# Foundation mount contract

First game-facing set: 23 aliases. These are stable named references, not fitted item
rules. Registered with ChildLocator.AddChild during content loading, before the game's
locator layout is locked for networking. The rig's bone names remain unchanged.

| Alias | Rig target |
|---|---|
| Head | head |
| Chest | chest |
| Stomach | spine |
| Pelvis | pelvis |
| UpperArmL / UpperArmR | L upperarm / R upperarm |
| LowerArmL / LowerArmR | L forearm / R forearm |
| HandL / HandR | L hand / R hand |
| ThighL / ThighR | L thigh / R thigh |
| CalfL / CalfR | L shin / R shin |
| FootL / FootR | L foot / R foot |
| MuzzleLeft / MuzzleRight | L muzzle / R muzzle |
| Core | core socket |
| Halo | halo socket |
| HeelL / HeelR | L heel socket / R heel socket |
| MainHurtbox | Runtime-authored capsule center |

Next set: dedicated face/crown/shoulder/back/equipment offsets and palm/orb/spear/halo
tip VFX mounts. Fit those against actual item/VFX prefabs before freezing their axes.
Never assume the bone's local axes are the right placement axes for every item.
ItemDisplayRuleSet remains empty in the first foundation, explicitly pending M2/M5.
