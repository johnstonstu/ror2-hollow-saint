# Translating Hollow Saint

In-game text lives in [`HollowSaintMod/Language/HollowSaint.language`](../HollowSaintMod/Language/HollowSaint.language). R2API loads that file from `BepInEx/plugins/HollowSaint/`, next to `HollowSaint.dll`. The `strings` section is English and the fallback for every language that does not have its own entry. `zh-CN`, `ru` and `pt-BR` are the sections the game selects when Risk of Rain 2 is set to those languages.

The Simplified Chinese, Russian and Brazilian Portuguese text shipped here is machine-translated. Corrections are welcome. Open a [translation issue](https://github.com/johnstonstu/ror2-hollow-saint/issues/new?template=translation.md) or edit the file and send a pull request.

The Mod Options menu (Risk of Options, about 100 settings) stays in English on purpose. Those labels are not in the language file.

## Tokens

Static tokens (name, subtitle, character-select description, outros, skill names, lore, skin names) are the finished text. `HS_LORE` and `HS_BODY_LORE` must stay identical: the logbook looks the lore up by swapping `_NAME` for `_LORE` on the body token.

These ten tokens are templates. The mod fills the numbers from the live config whenever a setting changes, in every language:

| Token | What it is |
|---|---|
| `HS_SKILL_ARCBOLT_DESC` | Arc Bolt |
| `HS_SKILL_SPEAR_DESC` | Stormspear |
| `HS_SKILL_ARCSTEP_DESC` | Arc Step |
| `HS_SKILL_CIRCUIT_DESC` | Open Circuit |
| `HS_SKILL_GAZE_DESC` | Gaze of the Hollow |
| `HS_PASSIVE_STORM_DESC` | Answered Prayer |
| `HS_KEYWORD_STORM` | The Storm keyword |
| `HS_KEYWORD_STATIC` | Static keyword |
| `HS_KEYWORD_ELECTROCUTE` | Electrocute keyword |
| `HS_KEYWORD_SHOCKED` | Shocked keyword |

Keep every `{name}` and every `<style=...>` / `<color=...>` tag that the English string has. The check fails if a translation drops one or adds a new one. Numbers use a dot (`0.5`, `100%`), the same in every language.

## Placeholders

`{name}` inserts that value.

`{name, plural, =0 {none} one {one} few {a few} many {many} other {rest}}` picks a branch from the number. An exact `=0` or `=1` wins over the category. If the category has no branch, `other` is used.

| Language | Categories |
|---|---|
| English (`strings`), Brazilian Portuguese | `one` when the number is 1, otherwise `other` (0 is `other`, so "0 charges" stays plural) |
| Russian | `one` (1, 21, 31, … but not 11), `few` (2–4, 22–24, …), `many` (0, 5–20, 11–14, …), `other` for fractions such as 1.5 |
| Chinese | always `other` |

A Russian plural block needs `few`, `many` and `other`. Example:

```text
{stepStock, plural, one {{stepStock} заряд} few {{stepStock} заряда} many {{stepStock} зарядов} other {{stepStock} заряда}}
```

`{name, select, off {} on {text} other {fallback}}` picks a branch from a word the mod passes in. The words in use are:

| Name | Values |
|---|---|
| `death` | `off`, `full`, `partial` |
| `jolt` | `off`, `on` |
| `armorOn` | `no`, `yes` |

Style tags the game colours: `<style=cIsDamage>`, `<style=cIsUtility>`, `<style=cKeywordName>`, `<style=cSub>`, `<style=cMono>`. The character-select description also uses `<color=#CCD3E0>`.

## Check

From the repo root:

```
powershell -ExecutionPolicy Bypass -File tools\tests\Check-Language.ps1
```

`tools\release\Make-Package.ps1` runs that check before it builds the zip, and the zip's allow-list includes `plugins/HollowSaint/HollowSaint.language`. A manual install has to keep that file in the same folder as the DLL.
