<p align="center"><b>English</b> | <a href="https://github.com/johnstonstu/ror2-hollow-saint/blob/main/HollowSaintMod/Package/README.zh-CN.md">简体中文</a> | <a href="https://github.com/johnstonstu/ror2-hollow-saint/blob/main/HollowSaintMod/Package/README.ru.md">Русский</a> | <a href="https://github.com/johnstonstu/ror2-hollow-saint/blob/main/HollowSaintMod/Package/README.pt-BR.md">Português (BR)</a></p>

<p align="center"><img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/banner.jpg" alt="Hollow Saint, a storm survivor for Risk of Rain 2" width="100%"></p>

<h3 align="center">A cracked devotional icon that answers only to the storm.</h3>

Chain lightning through a pack, pin a threat with Stormspear, and bank Static Charges. Spend them on a bouncing Orb, a Thundercloud, a piercing beam or a lightning crown.

<p align="center"><img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/gaze-hero.webp" alt="Gaze of the Hollow gathers charges into the crown and opens with a blast" width="100%"></p>

## New in 1.3.2

- Keep firing Arc Bolt while Gaze or Thundercloud gathers. Newly earned charges stay banked for your next cast.
- Smoother held-button transitions, arm poses and dash-jumps, with cleaner effect fades and cancellation.
- A new electrical charge, crackling hold and fuller throw sound for Hollowed Orb, plus a subtle dash crackle.

[Full changelog](https://github.com/johnstonstu/ror2-hollow-saint/blob/main/HollowSaintMod/Package/CHANGELOG.md)

## Skills

| Slot | Skill | How it plays |
|---|---|---|
| Passive | **Answered Prayer** | Hits build Static. Electrocute spreads it and banks a charge, up to five. |
| Primary | **Arc Bolt** | Fast chain lightning that spreads Static through a pack. |
| Secondary | **Stormspear** | Hold to charge, release to throw. Sticks, then bursts; a full-charge throw with a full bank becomes Thunderbolt. |
| Alt. secondary | **Hollowed Orb** | Tap for a free bouncing Orb, or hold to feed charges for more damage, hits and reach. |
| Utility | **Arc Step** | Dash in any direction, including upward. Jump out to carry the momentum. Two stocks. |
| Special | **Gaze of the Hollow** | Hold to gather, release for an opening blast and piercing beam. Tap to skip gathering. |
| Alt. special | **Open Circuit** | Feed at least one charge to open a lightning crown. Keep fighting inside it; nearby Electrocutes return fed charges. |
| Third special | **Thundercloud** | Place a free storm, or hold to feed charges for a wider, longer storm. Press Special again to end it early. |

In-game descriptions show the numbers for your current settings. Hold Primary through a Stormspear throw and bolts resume afterward; Utility cancels Orb or Thundercloud gathering and returns the gathered charges.

## The charge loop

**Build Static → Electrocute → bank charges → spend → finish.**

Orb and Thundercloud prime enemies so Arc Bolt, Stormspear, the Gaze beam or crown pulses can finish the next Electrocute. Both have free casts, so you can start the loop with an empty bank. Choose how many charges to gather; charges earned during Gaze or Thundercloud gathering stay in reserve.

Want the numbers, skill tips and six suggested loadouts? See the [full player guide](https://github.com/johnstonstu/ror2-hollow-saint#skills).

## Skins

Six skins with matching lightning colours. Unlock **Crimson Vow** by winning or obliterating on Monsoon as Hollow Saint.

<p align="center"><img src="https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/skin-lineup.png" alt="The six Hollow Saint skins" width="100%"></p>

## Install and options

Install through **r2modman** or **Thunderstore Mod Manager** with the listed dependencies. For manual installation, copy `plugins/HollowSaint` into `BepInEx/plugins/`, keeping the DLL, asset bundle and language file together.

Tune the kit in **Settings → Mod Options → Hollow Saint**. English, Simplified Chinese, Russian and Brazilian Portuguese are included; translations follow the game's language setting. Mod Options stays in English.

## Feedback

Still in early access. Multiplayer and physical controllers need more playtesting; all players should use the same version and config. Some item displays may sit slightly off.

Report bugs or balance feedback on [GitHub](https://github.com/johnstonstu/ror2-hollow-saint/issues). For bugs, enable **Verbose log** under Mod Options → 6. Misc, reproduce the issue, and attach `BepInEx/LogOutput.log`.

Created by **JohnstonStu**. Built with BepInEx, R2API and Risk Of Options. Lightning strike samples: Pixabay Content License; other sounds are original. [MIT license](https://github.com/johnstonstu/ror2-hollow-saint/blob/main/LICENSE).

Also try [AH64](https://thunderstore.io/c/riskofrain2/p/JohnstonStu/AH64/), an Apache helicopter survivor.
