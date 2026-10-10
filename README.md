<p align="center"><b>English</b> | <a href="README.zh-CN.md">简体中文</a> | <a href="README.ru.md">Русский</a> | <a href="README.pt-BR.md">Português (BR)</a></p>

<p align="center">
  <img src="docs/media/banner.jpg" alt="Hollow Saint, a storm survivor for Risk of Rain 2" width="100%">
</p>

<p align="center">
  <a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/Hollow_Saint/"><img src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fv1%2Fpackage-metrics%2FJohnstonStu%2FHollow_Saint%2F&query=%24.latest_version&label=thunderstore&prefix=v&color=4fd2ff&style=for-the-badge" alt="Thunderstore version"></a>
  <a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/Hollow_Saint/"><img src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fv1%2Fpackage-metrics%2FJohnstonStu%2FHollow_Saint%2F&query=%24.downloads&label=downloads&color=7b5cff&style=for-the-badge" alt="Thunderstore downloads"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/licence-MIT-6ee1e1?style=for-the-badge" alt="MIT licence"></a>
</p>

<h3 align="center">A cracked devotional icon that answers only to the storm.</h3>

<p align="center">Chain lightning through a pack, pin the biggest threat with a spear of lightning, and bank every Electrocute as a Static Charge.<br>Then spend the bank on a Hollowed Orb, a Thundercloud, a Gaze beam or an Open Circuit crown.</p>

<p align="center"><a href="#skills"><b>Skills</b></a> · <a href="#the-charge-loop"><b>The charge loop</b></a> · <a href="#builds"><b>Builds</b></a> · <a href="#install-and-options"><b>Install</b></a> · <a href="https://github.com/johnstonstu/ror2-hollow-saint/issues"><b>Feedback</b></a> · <a href="HollowSaintMod/Package/CHANGELOG.md"><b>Changelog</b></a></p>

<p align="center"><img src="docs/media/thundercloud-return-13.webp" alt="Thundercloud 1.3: crown release and rolling lightning" width="100%"></p>

<p align="center"><img src="docs/media/hollowed-orb-13.webp" alt="Hollowed Orb 1.3" width="49%"> <img src="docs/media/circuit-orb-13.webp" alt="Open Circuit and Hollowed Orb 1.3" width="49%"></p>

> **Early access:** Hollow Saint is still being tuned, so expect balance changes and the occasional bug. In-game skill descriptions always show the numbers for your current settings.

## Skills

| | Skill | Slot | Input | What it does | Key numbers |
|:-:|---|---|---|---|---|
| <img src="docs/media/icon-discharge.png" width="40" alt=""> | **Answered Prayer** | Passive | Automatic | Hits build Static on enemies. Full Static Electrocutes and the arc jumps to neighbours. Each Electrocute banks a Static Charge. | Bank holds 5. Charges never expire. |
| <img src="docs/media/icon-arc_bolt.png" width="40" alt=""> | **Arc Bolt** | Primary | Primary | A fast bolt that chains through a pack. Your steady pressure and the quickest way to spread Static. | 171% per hit. Up to 4 enemies per bolt. A shot every 0.5 s. Proc 1.0. |
| <img src="docs/media/icon-conduit_spear.png" width="40" alt=""> | **Stormspear** | Secondary | Hold to charge, release to throw | Sticks in what it hits, then bursts in a lightning dome. Builds double Static on primed enemies. | Full charge in 2 s. Burst 3 m to 10 m. Cooldown 5 s. |
| <img src="docs/media/icon-arc_step.png" width="40" alt=""> | **Arc Step** | Utility | Utility | Blink in any direction, even mid-air. Look up to climb. Jump out of a step to keep its momentum. | 2 charges. 5 s recharge. |
| <img src="docs/media/icon-gaze.png" width="40" alt=""> | **Gaze of the Hollow** | Special | Hold to gather charges, release to start. Tap to skip the charge-up. | Rise and channel a piercing beam. It opens with one big blast of everything you gathered, then builds focus on one target. | 7 s beam, 90 m. Opening blast 400% per charge, 6 m wide plus 2 m per charge (max 20 m). Focus up to +100% over 3 s. Cooldown 12 s. |
| <img src="docs/media/icon-open_circuit.png" width="40" alt=""> | **Open Circuit** | Alt. special | Hold Special to feed at least 1 charge, release | A crown that strikes everything around you while you keep fighting. | 10 s, 8 m radius. A pulse every 0.5 s for 72%. 1, 3 or 5 charges give 1x, 1.5x or 2x pulse density. Cooldown 8 s after the crown closes. |
| | **Hollowed Orb** | Alt. secondary | Tap, or hold past 0.5 s to gather charges | A big lightning ball that ping-pongs through the pack. Free to cast. | Free: 378% per hit, 3 hits, 0.9 m. Five charges: 738%, 8 hits, 1.5 m. 70 m range, 18 m to 36 m bounce. Cooldown 6 s. |
| | **Thundercloud** | Third special | Tap, or hold Special to pour in charges, release over your target | A lingering storm that strikes every enemy beneath it. Free to cast. | Strikes every 0.75 s for 4 s (9 s at five charges). 149% to 261% per strike. Radius 12 m to 30 m. 80 m aim range. Cooldown 10 s. |

**Held inputs:** keep Primary held while Gaze or Thundercloud gathers to continue firing Arc Bolt. Those casts only gather from the charges present at the start; newly earned charges stay banked for later. Stormspear pauses bolts while charging, then they resume after the throw if Primary is still held.

### Skill notes

- **Stormspear:** a full charge also calls a lightning strike. A fully charged throw with a full bank becomes a **Thunderbolt** that primes the pack around it. Quick throws never touch the bank.
- **Hollowed Orb:** fresh enemies come first, then revisits, and each revisit on the same enemy keeps 75% of its previous damage. Aim assist favors the crosshair and respects walls. Each Backup Magazine adds one hit. With nothing else in reach it latches onto its target, zaps its remaining hits there, then bursts (3 m, +0.8 m per charge, 60% of hit damage). Every hit primes Static. Utility cancels gathering and returns your charges. During Open Circuit it gathers and launches overhead, so Primary stays free.
- **Thundercloud:** radius is 12 m free, 16 m at one charge and 30 m at five. It can be placed on an empty area and also strikes flyers and enemies on ledges. Strikes Shock and prime, and come faster with attack speed (up to twice as often). Utility cancels gathering. Press Special again while it rains to end it early and get up to half the cooldown back, scaled by the time left. The cooldown starts when the storm ends.
- **Gaze of the Hollow:** you take less damage and ignore knockback while gathering and channeling. Focus survives half a second off target, then fades. Special or UI Cancel ends the beam early, and Utility exits straight into Arc Step. The opening blast primes what it hits, so the beam finishes them.
- **Open Circuit:** the first charge is required to open the crown, and extra pulses do not speed up Static generation. Enemies that stay inside for 3 s take an extra zap. **Closed Circuit:** each Electrocute within 12 m returns one fed charge. Charges still in the crown when it closes burst out as a 12 m nova for 150% damage per charge, priming what it hits, or return to your bank if no enemy is in the nova.

## The charge loop

<p align="center"><img src="docs/media/storm-loop.png" alt="The storm loop: finish, Electrocute, bank, spend, prime" width="100%"></p>

**Finishers** earn Static Charges, **spenders** use them, and everything a spender hits is left **primed** for the next finisher.

1. **Static.** Eligible hits charge the enemy. Bigger hits, critical strikes and high-proc items charge it faster. It fades if you stop hitting.
2. **Electrocute.** At full Static the enemy is jolted (not bosses) and **Shocked**, taking extra damage for a moment. The arc charges nearby enemies too. Each Electrocute banks one **Static Charge**, up to five.
3. **Spend and prime.** A skill that spends charges also primes what it hits, up to 95% Static and never full, so a spender can never pay for itself.
4. **Finish.** Arc Bolt, Stormspear, the Gaze beam and Open Circuit pulses tip primed enemies over. A primed pack Electrocutes in a hit or two and your bank refills.

- **Finishers:** Arc Bolt, Stormspear, Gaze beam, Open Circuit pulses.
- **Spenders:** held Hollowed Orb, Thundercloud, Gaze opening blast, full-bank Thunderbolt. Gaze, Orb, Thundercloud and Open Circuit spend the count you gather.
- **Free primers:** tapped Hollowed Orb and a free Thundercloud prime without spending anything. The easiest way to start the loop.
- **Payback:** Open Circuit refunds fed charges from Electrocutes within 12 m.

Nothing takes your charges unless you held a skill for them. A light income guard (2 charges per second, adjustable) keeps huge late-game packs from flooding the bank.

## Builds

Your Secondary decides how you start and finish the loop. Your Special decides how you spend it.

<p align="center"><img src="docs/media/storm-builds.png" alt="Six build-outs: Stormspear or Hollowed Orb with Gaze, Open Circuit or Thundercloud" width="100%"></p>

| Build | Loadout | How it plays | Tip |
|---|---|---|---|
| **Stormcaller** | Stormspear + Gaze | Arc Bolt fills the bank. The Gaze opening blast primes the pack, the focused beam and spears finish. | Spear and Gaze share one bank. Only fully charged throws spend it. |
| **Lancer** | Stormspear + Open Circuit | Spears charge much faster under the crown. Throw into primed enemies while pulses work the rest. | Stay within 12 m of the fight for refunds. |
| **Siege** | Stormspear + Thundercloud | Prime and Shock a distant group with a cloud, then finish from range with spears and Arc Bolt chains. | A free storm still primes. Save charges for a big storm or a full-charge spear. |
| **Seer** | Hollowed Orb + Gaze | Free Orbs prime the pack, then the opening blast and the beam cash it in. | Orbs never finish on their own. The beam and Arc Bolt do. |
| **Conductor** | Hollowed Orb + Open Circuit | Free Orbs prime, you feed the crown and fight inside it. Pulses finish and refunds refill the bank. The most self-sustaining build. | Overhead Orbs keep Primary free during the crown. |
| **Tempest** | Hollowed Orb + Thundercloud | Cloud and Orbs prime everything and Arc Bolt chains do all the finishing. High risk, high reward. | Free storms keep the loop going. Charged storms empty the bank fast. |

## Skins

Six skins, each with its own halo and lightning colour. **Crimson Vow** is the mastery skin: win or obliterate on Monsoon as Hollow Saint.

<p align="center"><img src="docs/media/skin-lineup.png" alt="The six Hollow Saint skins: Cracked Icon, Obsidian Saint, Verdigris Relic, Solar Vespers, Umbral Choir and Crimson Vow" width="100%"></p>

## Install and options

- **Mod manager (recommended):** install with [r2modman](https://thunderstore.io/c/riskofrain2/p/ebkr/r2modman/) or the Thunderstore Mod Manager. The dependencies come with it. After a game patch, update BepInExPack, R2API and HookGenPatcher along with Hollow Saint.
- **Manual:** install the dependencies listed on this page, then copy the package's `plugins/HollowSaint` folder into `BepInEx/plugins/`. Keep `HollowSaint.dll`, `hollowsaintassets` and `HollowSaint.language` together in that folder.
- **Options:** damage, range, cooldown and presentation settings are in **Settings → Mod Options → Hollow Saint** ([Risk Of Options](https://thunderstore.io/c/riskofrain2/p/Rune580/Risk_Of_Options/), installed with the mod) and in `BepInEx/config/com.johnstonstu.hollowsaint.cfg`. Presentation toggles cover spear hand, item displays, arm motion and impact feel.
- **Languages:** the mod follows the language you set in Risk of Rain 2. Simplified Chinese, Russian and Brazilian Portuguese are included (machine-translated; corrections are welcome in a [translation issue](https://github.com/johnstonstu/ror2-hollow-saint/issues/new?template=translation.md)). Any other language shows English. The Mod Options menu stays in English.

## Feedback and known limitations

Bugs and balance notes are welcome on [GitHub issues](https://github.com/johnstonstu/ror2-hollow-saint/issues). For a bug report, turn on **Verbose log** (Mod Options → 6. Misc), reproduce it and attach `BepInEx/LogOutput.log` with the stage and what you were doing.

- **Multiplayer has not had a real playtest yet.** Skills are server-authoritative and networked, but expect rough edges. Every player should run the same version and config.
- **Physical controller acceptance is unverified.** Mention your controller and input setup with input reports.
- Item displays borrow Commando's placements, so a few items sit slightly off.

## Also by JohnstonStu

<table>
  <tr>
    <td><a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/AH64/"><img src="docs/media/ah64-icon.png" alt="AH64" width="96"></a></td>
    <td><b><a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/AH64/">AH64</a></b>: an Apache attack helicopter survivor. It hovers and never lands, with a chain gun, Hydra rockets, Hellfire and Longbow missiles, and evasive rolls.</td>
  </tr>
</table>

## Credits, license and changelog

- Created by JohnstonStu: design, code, model, animation and effects.
- Lightning strike sounds built from Pixabay samples (Pixabay Content License). Other sounds are original.
- Built with BepInEx, R2API and Risk Of Options.
- [Full changelog](HollowSaintMod/Package/CHANGELOG.md).

[MIT](LICENSE) © 2026 JohnstonStu.

## Building from source

See the [developer guide](docs/DEVELOPING.md) for prerequisites, verification, local builds and the release workflow. Translation notes are in [docs/TRANSLATING.md](docs/TRANSLATING.md).
