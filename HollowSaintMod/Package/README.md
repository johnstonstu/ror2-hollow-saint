# Hollow Saint

*A cracked devotional icon that answers only to the storm.*

Hollow Saint is an original survivor built around chain lightning. Snap bolts that leap between enemies, form a spear of lightning in your hand and drive it into a pack, and build up the storm until it answers with a Thunderbolt.

![Open Circuit: the crown strikes everything nearby, and a fully charged crown spear calls a Thunderbolt](https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/hero.gif)

## Skills

| | Skill | |
|---|---|---|
| **Passive** | **Answered Prayer** | Hits build **Static**. Full Static **Electrocutes** an enemy: a short jolt, it takes more damage for a few seconds, and the arc jumps to its neighbours. Every Electrocute lights an orb on your halo; with all five lit, a **Thunderbolt** strikes a strong enemy for 1000% damage. |
| **Primary** | **Arc Bolt** | Snap a bolt for 100% damage that chains to up to 3 more enemies. |
| **Secondary** | **Stormspear** | Hold to form a spear of lightning in your hand, release to throw it for 400% to 1600% damage. It sticks in what it hits, then bursts on everything around it. Arc Bolt keeps firing from your other hand while you charge. 5 s cooldown. |
| **Utility** | **Arc Step** | Blink a short distance in any direction, even in the air. Follows your aim a little, so look up to climb. Two charges, 5 s each; jump out of it to keep the momentum. |
| **Special** | **Gaze of the Hollow** | Rise into the air and send your halo out before you: a lightning beam for 4 seconds, 500% damage per second, that pierces, splashes and forks across the ground, reaching further the longer it fires. Bonus armor while it channels. Recast, Arc Step or press B on a controller to end it early. 12 s cooldown. |
| **Special (alt)** | **Open Circuit** | Open your halo into a crown for 10 seconds. It strikes every enemy within 8 m twice a second while you keep fighting, your spear forms above your head and charges 2.5x faster, and a fully charged crown spear calls a Thunderbolt. 8 s cooldown, counted from when the crown closes. |

| Arc Bolt | Stormspear |
|---|---|
| ![Arc Bolt chaining through a pack](https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/arc-bolt.gif) | ![A fully charged Stormspear sticking and bursting](https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/stormspear.gif) |
| **Arc Step** | **Gaze of the Hollow** |
| ![Arc Step left, right and up](https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/arc-step.gif) | ![Gaze of the Hollow sweeping a pack](https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/gaze.gif) |

## How the storm works

1. **Static.** Every hit charges the enemy you hit. Bigger hits, critical strikes and items with high proc coefficients charge it faster. It fades if you stop hitting.
2. **Electrocute.** At full Static the enemy is jolted (not bosses) and **Shocked**, taking 15% more damage for 3 seconds. The arc jumps to two nearby enemies and charges them too. An enemy that dies holding half its Static or more Electrocutes as it falls.
3. **Thunderbolt.** Each Electrocute lights an orb on your halo. When all five are lit, they combine, rise off your crown, streak across the sky and strike a strong enemy in sight.

Keep hitting the same pack and the chain feeds itself.

![The last orbs light, and a Thunderbolt strikes the pack](https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/storm.gif)

## Tips

- Throw the Stormspear into the middle of a pack: the burst hits everything around the enemy it sticks in. A full charge bursts for its whole damage.
- Charging the spear does not stop Arc Bolt. Keep the bolts going from your other hand.
- Gaze of the Hollow lifts you out of reach. Sweep the beam across a line of enemies.
- Arc Step lets you jump out of the step and keep its speed. Look up as you step to get onto ledges.

## Skins

Default, Obsidian, Verdigris, Solar and Umbral. Each skin recolours the lightning to match.

![All five skins firing Arc Bolt](https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/skins.gif)

## Options

Every balance number is in the config file (`BepInEx/config/com.johnstonstu.hollowsaint.cfg`), and in the in-game Mod Options menu ([Risk Of Options](https://thunderstore.io/package/Rune580/Risk_Of_Options/), installed with the mod). There are also presentation toggles: spear hand, item displays, arm motion, impact feel.

## Compatibility

- Multiplayer: the skills are server-authoritative and their effects are networked, but it has not had a real multiplayer playtest yet. Every player should use the same config. Reports are very welcome.
- Item displays borrow Commando's placements on matching mounts.
- Works with the vanilla item pool, including the DLC items. Heretic replacements are supported.

## Feedback

Bugs and balance feedback are welcome on [GitHub](https://github.com/johnstonstu/ror2-hollow-saint/issues). For a bug report, turn on "Verbose log" in the config and attach `BepInEx/LogOutput.log`.

## Credits

- Created by JohnstonStu.
- Lightning strike sounds: Pixabay (Pixabay Content License). Other sounds are original.
