<p align="center">
  <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/banner.jpg" alt="Hollow Saint, a storm survivor for Risk of Rain 2" width="100%">
</p>

<p align="center">
  <a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/Hollow_Saint/"><img src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fv1%2Fpackage-metrics%2FJohnstonStu%2FHollow_Saint%2F&query=%24.latest_version&label=thunderstore&prefix=v&color=4fd2ff&style=for-the-badge" alt="Thunderstore version"></a>
  <a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/Hollow_Saint/"><img src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fv1%2Fpackage-metrics%2FJohnstonStu%2FHollow_Saint%2F&query=%24.downloads&label=downloads&color=7b5cff&style=for-the-badge" alt="Thunderstore downloads"></a>
  <a href="https://github.com/johnstonstu/ror2-hollow-saint/blob/main/LICENSE"><img src="https://img.shields.io/badge/licence-MIT-6ee1e1?style=for-the-badge" alt="MIT licence"></a>
</p>

<p align="center"><b>A cracked devotional icon that answers only to the storm.</b></p>

<p align="center">Enjoying it? Please <a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/Hollow_Saint/">like Hollow Saint on Thunderstore</a> so other players can find it.</p>

<p align="center">
  <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/hero.webp" alt="Open Circuit: the Saint's crown opens and strikes every enemy around it" width="100%">
</p>

<h3 align="center">Chain lightning. A spear of lightning. A storm that answers back.</h3>

Hollow Saint is an original survivor built around chain lightning. Snap bolts that leap between enemies, form a spear of lightning in your hand and drive it into a pack, and keep the hits coming until the storm answers with a Thunderbolt.

**[Report a bug](https://github.com/johnstonstu/ror2-hollow-saint/issues)** · **[Changelog](https://github.com/johnstonstu/ror2-hollow-saint/blob/main/HollowSaintMod/Package/CHANGELOG.md)** · **[Source](https://github.com/johnstonstu/ror2-hollow-saint)**

## The kit

| Slot | Skill | What it does | In game |
| :---: | --- | --- | :---: |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/icon-discharge.png" width="64" alt="Answered Prayer"><br>**Passive** | **Answered Prayer** | Hits build **Static**. Full Static **Electrocutes** an enemy: a short jolt, it takes more damage for a few seconds, and the arc jumps to its neighbours. Every Electrocute lights an orb on your halo; with all five lit, a **Thunderbolt** strikes a strong enemy for 1000% damage. | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/storm.webp" width="320" alt="The last orbs light and a Thunderbolt strikes the pack"> |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/icon-arc_bolt.png" width="64" alt="Arc Bolt"><br>**Primary** | **Arc Bolt** | Snap a bolt for 100% damage that chains to up to 3 more enemies. | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/arc-bolt.webp" width="320" alt="Arc Bolt chaining through a pack"> |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/icon-conduit_spear.png" width="64" alt="Stormspear"><br>**Secondary** | **Stormspear** | Hold to form a spear of lightning in your hand, release to throw it for 400% to 1600% damage. It sticks in what it hits, then bursts on everything around it. Arc Bolt keeps firing from your other hand while you charge. 5 s cooldown. | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/stormspear.webp" width="320" alt="A charged Stormspear sticking and bursting"> |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/icon-arc_step.png" width="64" alt="Arc Step"><br>**Utility** | **Arc Step** | Blink a short distance in any direction, even in the air. Follows your aim a little, so look up to climb. Two charges, 5 s each; jump out of it to keep the momentum. | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/arc-step.webp" width="320" alt="Arc Step left, right and up"> |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/icon-gaze.png" width="64" alt="Gaze of the Hollow"><br>**Special** | **Gaze of the Hollow** | Rise into the air and send your halo out before you: a lightning beam for 4 seconds, 500% damage per second, that pierces, splashes and forks across the ground, reaching further the longer it fires. Bonus armor while it channels. Recast, Arc Step or press B on a controller to end it early. 12 s cooldown. | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/gaze.webp" width="320" alt="Gaze of the Hollow sweeping a pack"> |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/icon-open_circuit.png" width="64" alt="Open Circuit"><br>*Special variant* | **Open Circuit** | Open your halo into a crown for 10 seconds. It strikes every enemy within 8 m twice a second while you keep fighting, your spear forms above your head and charges 2.5x faster, and a fully charged crown spear calls a Thunderbolt. 8 s cooldown, counted from when the crown closes. | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/open-circuit.webp" width="320" alt="Open Circuit striking a pack, then a crown spear"> |

## How the storm works

1. **Static.** Every hit charges the enemy you hit. Bigger hits, critical strikes and items with high proc coefficients charge it faster. It fades if you stop hitting.
2. **Electrocute.** At full Static the enemy is jolted (not bosses) and **Shocked**, taking 15% more damage for 3 seconds. The arc jumps to two nearby enemies and charges them too. An enemy that dies holding half its Static or more Electrocutes as it falls.
3. **Thunderbolt.** Each Electrocute lights an orb on your halo. When all five are lit, they combine, rise off your crown, streak across the sky and strike a strong enemy in sight.

Keep hitting the same pack and the chain feeds itself.

### Tips

- Throw the Stormspear into the middle of a pack: the burst hits everything around the enemy it sticks in. A full charge bursts for its whole damage.
- Charging the spear does not stop Arc Bolt. Keep the bolts going from your other hand.
- Gaze of the Hollow lifts you out of reach. Sweep the beam across a line of enemies.
- Arc Step lets you jump out of the step and keep its speed. Look up as you step to get onto ledges.

## Skins

Five skins, each with its own halo and lightning colour: Cracked Icon, Obsidian Saint, Verdigris Relic, Solar Vespers and Umbral Choir.

<p align="center">
  <img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/skin-lineup.png" alt="The five Hollow Saint skins: Cracked Icon, Obsidian Saint, Verdigris Relic, Solar Vespers and Umbral Choir" width="100%">
</p>

## Install

**Mod manager (recommended):** install with [r2modman](https://thunderstore.io/c/riskofrain2/p/ebkr/r2modman/) or the Thunderstore Mod Manager. The dependencies are installed automatically.

**Manual:** install the dependencies listed on this page, then copy the package's `plugins/HollowSaint` folder into `BepInEx/plugins/`. Keep `HollowSaint.dll` and `hollowsaintassets` together.

## Options

Every balance number is in the in-game **Settings → Mod Options → Hollow Saint** menu ([Risk Of Options](https://thunderstore.io/c/riskofrain2/p/Rune580/Risk_Of_Options/), installed with the mod) and in `BepInEx/config/com.johnstonstu.hollowsaint.cfg`. There are presentation toggles too: spear hand, item displays, arm motion and impact feel.

## Feedback

Bugs and balance feedback are welcome on [GitHub issues](https://github.com/johnstonstu/ror2-hollow-saint/issues). For a bug report, turn on **Verbose log** (Mod Options → 6. Misc), reproduce it, and attach `BepInEx/LogOutput.log` with the stage and what you were doing.

## Known limitations

- **Multiplayer has not had a real playtest yet.** The skills are server-authoritative and their effects are networked, but expect rough edges. Every player should run the same version and the same config. Reports are very welcome.
- Item displays borrow Commando's placements on matching mounts, so a few items sit slightly off.

## Credits and license

- Created by JohnstonStu: design, code, model, animation and effects.
- Lightning strike sounds built from Pixabay samples (Pixabay Content License). Other sounds are original.
- Built with BepInEx, R2API and Risk Of Options.

[MIT](https://github.com/johnstonstu/ror2-hollow-saint/blob/main/LICENSE) © 2026 JohnstonStu.
