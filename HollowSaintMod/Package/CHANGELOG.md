# Changelog

## 1.2.0 (private candidate)
- Special starts Gaze; fresh Primary presses send one entry orb per pulse, with a 0.25-second minimum interval. Holding does not repeat; new charges stay in reserve. Primary shows the pulse icon and available entry count, and other combat skills are unavailable until the channel ends. Original skills and cooldown progress return on exit. Unlaunched orbs return on living exits, and late presses are rejected before they can spend fuel without completing their travel.
- Gaze channel duration is fixed at cast entry and grows by 0.1 seconds per level above 1, up to two extra seconds and a six-second total cap. The default grows from four to six seconds; longer channels also increase ordinary beam exposure and delay cooldown.
- Gaze uses a thinner continuous beam, broad traveling pulses and physical radial crown expansion. Open Circuit now has moving tapered dome arcs. Effects reuse bounded geometry and clear on cancellation or teardown.
- Added Crimson Vow as the sixth skin, with red conductors and gold-white Gaze pulse accents. It unlocks through the native survivor mastery condition: a qualifying winning ending on Monsoon-equivalent hard difficulty. Previous clears are not replayed for the new achievement.
- Includes the accepted private prototype's faster, wider Arc Bolt; Stormspear recharge pause until throw; and a short, bounded conductor effect on a struck enemy. Existing direct damage and proc behavior are retained from that prototype.
- Private source/build candidate only. Native visual, unlock, controller and multiplayer acceptance remain pending.

## 1.1.1
- Gaze of the Hollow now has broad, branching ground tendrils throughout its channel, plus bright connectors at confirmed hit positions. The effects follow the skin palette and clear when the channel ends or is interrupted; damage and hit cadence are unchanged.
- Stormspear keeps its original charge and crown form through flight and lodging. Damage-stat changes no longer shrink a charged spear's burst, and opening or closing Open Circuit after the throw no longer changes its crown bonus.
- Arc Bolt damage increased from 100% to 120%. Fire rate, chain reach, target count and proc coefficients are unchanged. Saved default damage values migrate; custom values are retained.
- Crown recovery and movement transitions preserve queued casts and Gaze poses. Interrupted crown startup and Gaze exit clear their own hold poses.
- Stormspear direct damage reduced from 400-1600% to 350-1400% (12.5% less at every charge level). Its damage-derived burst follows the reduction; charge time, radius, proc coefficients and crown Thunderbolt damage are unchanged. Saved default values migrate; custom values are retained.
- English README updated with skill roles, practical combos and an approved normal-camera Gaze screenshot. Multiplayer and physical controller acceptance remain unverified.

## 1.1.0
- Fixed the game failing to launch with EnemiesReturns (its extra Anointed skin tripped a Hollow Saint startup check). Startup checks now only log a warning and never stop the game from loading.
- In-game text follows the language set in Risk of Rain 2. Simplified Chinese, Russian and Brazilian Portuguese are included. Any other language shows the English text.
- Those three translations are machine-translated. Corrections are welcome.
- Skill, passive and keyword numbers still update when you change them in Mod Options, in every language.
- The Mod Options menu itself stays in English, except the new Spear hand option.
- The spear hand now follows your input device: left hand on a controller (left trigger), right hand on mouse and keyboard (right-click). It switches between throws, never mid-charge, and other players see your choice. Option "Spear hand" (Stormspear section): Auto (default), Left or Right. If you had turned off "Spear in left hand", it becomes Right.
- `HollowSaint.language` ships next to the DLL. Keep it there if you install by hand.
- README in Simplified Chinese, Russian and Brazilian Portuguese, with the early access note.
- New README hero clip: Gaze of the Hollow on the Umbral skin, now sharper (1280x720 at 30 fps).

## 1.0.1
- Early-game damage buff: base damage 15 (was 12), +2.4 per level unchanged. Stage 1 and the first boss should go down faster; late game is nearly untouched.
- Base armor 15 (was 0) so the early game is less squishy.
- Early access note added to the README.
- New vanilla-style character select icon.

## 1.0.0
- First public release.
- No version tag in the top-left corner any more.
- New icon and character-select portrait.
- README with a banner, a clip for every skill and the skin lineup.

## 0.9.17 (release candidate)
- Clean log: Hollow Saint writes one line when it loads, plus real warnings and errors. Option "Verbose log" (6. Misc) brings the load diagnostics back for bug reports.
- Two options renamed: "Item displays (restart)" and "RoR2 body shader (restart)". If you had changed either, set it again.
- The mod shows as "Hollow Saint" in the mod list. The version tag in the corner goes away at 1.0.
- README with gameplay footage, cooldowns and an honest multiplayer note.
- Declares every library it loads (RoR2BepInExPack and R2API ContentManagement added). Risk Of Options is now a dependency, so the Mod Options menu is always there.

## 0.9.16
- Stormspear impact: new heavier hit sound with an electric zap on every spear as it sticks, and a louder sub boom under the charged burst. The burst fills the air too: an expanding shell, arcs leaping over a dome and a column of lightning straight up. The struck enemy crackles around the lodged spear until it bursts.
- Arc Step follows your aim a little: look up to lift about 2 m, or down in the air to drop (option "Look lift").
- Thunderbolt every 5 Electrocutes (was 6).
- An enemy that dies holding half its Static or more Electrocutes as it falls: it lights an orb and arcs to its neighbours, so packs that die fast still feed the storm.

## 0.9.15
- Thunderbolt is slower and bigger: the halo orbs take longer to combine, then the charge climbs off the crown on a lightning tether, flares at the top, streaks across the sky to the target and strikes about a second later.
- Gaze of the Hollow: B (Circle) on a controller ends the beam early, and you have 30 extra armor while it channels.
- Open Circuit: the crown lasts 10 s (was 8). The 8 s cooldown starts when the crown closes (was 12 s from the cast), so cooldown items can't keep it up forever.
- The passive and the storm keyword box are reworded: one colour for Static, Electrocute, Shocked and Thunderbolt, plain sentences, full units.

## 0.9.14
- The Stormspear is held and thrown with the left hand, matching the left trigger on a controller. Arc Bolt fires from the right hand while the spear charges, and the left fingers close on the shaft.
- Option "Spear in left hand" (Stormspear section) puts it back in the right hand.

## 0.9.13
- Stormspear: the splash scales with charge, 50% of the spear's damage on a tap up to 100% at full charge, and the full-charge radius is 10 m. The impact plays as one sound: a punchy hit on a tap, a crackle as a charged spear sticks and the strike on the burst.
- Less crowd control: Electrocute is a 0.5 s jolt (was a 1.5 s stun) and Shocks every enemy. Arc Bolt proc coefficient 0.8 (was 1.0), chain bounces 0.4 / 0.2 / 0.1 (were 0.5).
- Gaze of the Hollow: splash and fork reach grows over the channel, from 40% to 160%.
- The passive reads as three steps, and one "The Storm" keyword box explains Static, Electrocute, Shocked and the Thunderbolt together. New overview tip on how the storm works.

## 0.9.12
- The crown spear (Open Circuit) is the real spear model above your head, with less lightning clutter around it.
- Elite body tint re-applies when the skin changes.

## 0.9.11
- The Saint keeps his own shading again, and cloak, shields, crit, immunity and elite effects still show on him. Elites tint his body in their colour.
- RoR2's body shader is still available in the options (it darkened the Umbral and Obsidian skins and every skin on night stages).
- Umbral is a step lighter so it reads in character select and dark stages.
- Character select: shorter skill descriptions that fit the panel, bolder skill icons, new tips for Stormspear and Gaze.
- The gameplay event log is off by default; Thunderstore dependencies updated.

## 0.9.10
- Stormspear sticks in what it hits, crackles, then bursts on every other enemy around it. Ground hits burst at half strength. Full charge 1600% (was 1400%, the burst no longer also hits the struck enemy).
- Gaze of the Hollow cooldown 12 s (was 16 s).
- Cloak, shields, crit, immunity and elite effects show on the Saint.
- Arc Bolt arm gestures no longer snap at very high attack speed.

## 0.9.9
- Brilliant Behemoth and other on-hit-all items now trigger from every Hollow Saint hit (chain bounces, Gaze, Electrocute, Thunderbolt).
- Gaze recast with a spare special charge (Lysate Cell) ends the beam instead of restarting it.
- Gaze of the Hollow has its own icon.

## 0.9.8 and earlier
- Development builds: kit rework (Stormspear, off-hand Arc Bolt, Gaze of the Hollow), javelin animation, skins, sounds.
