# Changelog

## 1.1.0
- In-game text follows the language set in Risk of Rain 2. Simplified Chinese, Russian and Brazilian Portuguese are included. Any other language shows the English text.
- Those three translations are machine-translated. Corrections are welcome.
- Skill, passive and keyword numbers still update when you change them in Mod Options, in every language.
- The Mod Options menu itself stays in English.
- `HollowSaint.language` ships next to the DLL. Keep it there if you install by hand.

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
