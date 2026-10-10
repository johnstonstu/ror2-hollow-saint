# Changelog

## 1.3.2
**Fluid controls**
- **Keep shooting while you charge:** hold Primary through the Gaze of the Hollow charge-up and the Thundercloud gather, and Arc Bolt keeps firing. The cast still uses the charges you had when you started; charges your bolts earn meanwhile stay in the bank for the next one.
- Bolts fired over a raised crown pose throw from one arm while the other stays up, then the arms settle back into the pose.
- Charges earned during Gaze's wind-up stay visible in the reserve bank throughout the beam, rather than disappearing until it ends.
- **Stormspear and Arc Bolt held together:** pressing Secondary mid-bolt only cancels a bolt that was winding up in the spear hand; a bolt already leaving the other hand still fires. Bolts pause while the spear charges and pick up again right after the throw.

**Animation and movement**
- Jump-cancelling out of Arc Step now plays the jump push-off instead of snapping into the rising pose.
- The hit flinch no longer overshoots at low frame rates.
- Sustained Arc Bolt fire keeps the arm layer raised between shots. Crown recovery respects incoming gestures on either arm layer and resumes correctly after movement changes or a long frame.

**VFX and sound**
- Hollowed Orb has a dedicated electrical rise, sustained crackling hold and fuller throw discharge. The throw's tail survives close-range hits, and its held loop fades on release or cancellation.
- Arc Step has a small static crackle at dash start.
- The Static bank "full" cue is now the electric crackle and snap used by the full Stormspear and Hollowed Orb (was the old chime).
- Body currents (arms, core, ring, legs, dash heels) fade out instead of switching off; the chest core light, the Stormspear glow and the Thundercloud light fade out too.
- Thundercloud fades away if you die mid-storm instead of vanishing.
- A Stormspear lodged in an enemy burns out when the corpse disappears instead of hanging in the air.
- Cancelling Gaze during its wind-up no longer leaves a light on at the crown.
- Cancelled Stormspear, Orb and Gaze charge sounds have short fade-out tails, including overlapping intake cues.
- Cloud cleanup also handles owner despawn, and lodged spears handle a target disappearing before their first visual update.
- The halo's new-charge chime no longer stacks when several charges arrive at once.
- Crimson skin: Shocked enemies now show the crimson overlay (they used the default cyan).
- Small per-frame cleanups in the cloud and halo effects.

**Docs**
- Shorter Thunderstore README: patch highlights, one skill table, the charge loop and install notes. Detailed numbers and builds remain in the GitHub player guide; developer notes live in docs/DEVELOPING.md.
- All four README languages explain held-button overlap and gathered-charge spending. The developer guide separates offline verification, staging and gameplay acceptance.

## 1.3.1
**Game patch fix**
- Arc Bolt and Stormspear showed Artificer's blue lightning bolt after the October 2026 patch (a new projectile ghost address overrode our visuals). Fixed; both show their own projectiles again.

**Balance pass** (benchmarked against vanilla survivors at level 1, no items)
- **Early game:** Arc Bolt 144% to 171% per hit (about 42 DPS on a Titan, between Huntress and Commando), proc coefficient 0.8 to 1.0 like other primaries.
- **Storm income:** an enemy that dies holding 30% Static (was 50%) discharges and banks a charge, so enemies primed by Orb, Thundercloud and Gaze pay out when they die.
- **Hollowed Orb:** free tap 297% to 378% per hit (738% at five charges), cooldown 7 s to 6 s, first-hit proc 0.5 to 0.8 and repeat hits 0.1 to 0.25 so items trigger.
- **Thundercloud:** strikes 81% to 149% free (261% at five charges), lasts 4 s free (was 3 s, 9 s at five charges), cooldown 12 s to 10 s, proc 0.4 to 0.5, and strikes faster with attack speed (up to twice as often).
- **Open Circuit:** pulse damage 54% to 72%.
- **Stormspear:** full charge 1400% to 1250% (it outpaced Artificer's Nano-Spear while also bursting for full damage in an area).
- **Level scaling:** damage per level is now 20% of base damage (3.0, was 2.4), the rule every vanilla survivor uses.
- Settings you changed are kept; only untouched defaults update.

**Playtest round (Oct 9)**
- **Gaze of the Hollow:** charges now all go into one big opening boom (6 m + 2 m per charge, up to 20 m) with a shockwave, arcs and sky bolts at every charge level. Primary no longer fires surges or lock-on bolts during the beam. Instead the core beam builds **focus** on whatever it holds: up to +100% damage over 3 seconds, kept through a half-second slip, fading over a second after that. Range 60 m to 90 m. No knockback while charging or channeling.
- **Thundercloud:** sits higher (18 to 28 m) and strikes a column under the cloud, so flyers and ledges are hit. Press Special again to end the storm early and get part of the cooldown back (half, scaled by the unused storm time). Bolts now leave the cloud's underside, each strike cracks with thunder and lights the cloud from inside.
- **Fix:** Thundercloud's free cast now works with an empty bank (it needed a charge to start before).
- **Hollowed Orb:** Backup Magazine adds one hit per magazine (also on a lone, latched target) instead of extra casts.
- **Thunderbolt:** the strike sound is now a thunder crack with a deep boom (was a fizzle).
- **Static Charges:** a newly banked charge pops on the halo with a rising chime.
- **Thundercloud:** the current no longer stretches from your body up to the cloud while it rains (it read as the storm hitting you). Bolts only go to the ground and enemies.
- **Gaze focus** is now heard: the beam's hum steps up in five notes as the damage climbs, with a crack at full focus.
- **Stormspear charge:** a soft hum rises in the hand while charging, with a chime at each step and an electric crackle at full; the spear's glow breathes and the tip sparks once fully charged.
- **Hollowed Orb:** a charge-up sound as it gathers, a flash and an electric crackle when it holds every charge it can, and a bigger ball overall (0.9 m free, +0.15 m per charge; was 0.6 / 0.1).
- Console output: normal sessions print one line from Hollow Saint; diagnostics only with the Event log option.

**Audit fixes**
- Ending Thundercloud early preserves spare Special stocks, including Lysate Cell and stock resets.
- Gaze focus history is owned by each body and clears on death, disable and stage changes; idle target history expires independently for each player.
- Thundercloud's attack-speed bonus stays capped at 2x with custom strike intervals too.
- Corrected Chinese Backup Magazine naming and Portuguese Thundercloud naming in both README copies; in-game Orb and Thundercloud text now explains magazine hits, faster strikes and early dismissal in all four languages.

## 1.3.0
**Updated for the October 2026 Risk of Rain 2 patch.** Update your mod manager's core dependencies (BepInExPack, R2API, HookGenPatcher) along with this release.

**New skills**
- **Hollowed Orb** (alternate Secondary): a two-handed lightning ball that bounces through the pack. A free tap deals 297% per hit for 3 hits; hold past 0.5 s to gather charges, up to 657% per hit and 8 hits at five charges. Bounce reach grows from 18 m to 36 m. With nothing else in reach it latches onto its target, zaps its remaining hits, then bursts. Every hit primes Static.
- **Thundercloud** (third Special): a lingering storm, free to cast. It lasts 3 s free, +1 s per charge, and every 0.75 s strikes everything beneath it (12 m free up to 30 m at five charges) for 81% to 162%, Shocking and priming.

**The storm loop**
- Spenders prime, finishers cash in. Hollowed Orb, Thundercloud, Gaze blasts, surges and lock-on strikes and the Thunderbolt's splash prime Static up to 95% (never a full Electrocute on their own). Arc Bolt, Stormspear, the Gaze beam and Open Circuit tip primed enemies into Electrocutes and bank the charges back.
- **Stormspear** builds 2x Static on primed enemies. A full bank only goes into a fully charged throw, with a flash and chime when it will spend.
- **Closed Circuit:** Open Circuit takes at least one charge, and Electrocutes within 12 m return what you fed it. Leftovers burst as a 12 m crown nova (150% per charge) when it closes. More charges mean denser pulses.
- Income guard: at most 2 Static Charges bank per second, so late-game packs can't flood the bank.
- See the loop diagram and build-outs in the README.

**Balance**
- Arc Bolt 108% to 144% per direct hit (only the exact old default migrates).
- Gaze opening blast 600% to 400% per charge; lock-on bolt 200% to 100% per charge, reach 14 m to 8 m.
- Charge gathering ticks every 0.25 s (was 0.30 s).

**Other**
- New icons, skin-colored effects and charge sounds for the new skills; descriptions rewritten in all four languages.
- Every new value is adjustable in Mod Options.
- Solo-tested. Multiplayer is still untested.

## 1.2.0
**Gaze of the Hollow, reworked**
- **Charge-up.** Hold Special to pull your stored Static Charges into the crown, one at a time, each with its own rising power-up sound. Aim while you gather. Tap Special to skip it.
- **Opening blast.** The beam opens by firing every charge you drew in at once: the crown flares wide and kicks back, a wave rides down the beam and lands with a blast for 600% damage per charge that grows wider with every charge.
- **Surges.** During the 7-second beam, hold Primary to load up to 3 stored charges and release to fire them as one surge for 400% damage per charge. Surges proc items, knock enemies back and heal 1% of max health per charge. Unspent charges return when the beam ends.
- **Lock-on strike.** Each surge also drops a bolt onto the closest enemy within 14 m (200% per charge, small splash), so anything closing in under the hover gets hit while you aim elsewhere.
- Gaze armor now covers the charge-up too. Special or UI Cancel ends the channel; Utility exits and activates if ready.
- Gaze balance: beam proc coefficient 0.5 to 0.3, fork damage 100% to 70%, splash proc 0.3 to 0.2 (the beam lasts longer than before). Saved default values migrate; custom values are kept.
- New Mod Options for Gaze: charge speed, surge damage and surge proc.

**Kit**
- **Stormspear impact** now bursts in 3D: a dome of lightning, a lance of light out of the impact, a shockwave ring at the real damage radius and a spark fountain, all scaling with charge. Uncharged throws still splash.
- **Answered Prayer** stores Static Charges instead of discharging automatically. A full bank turns your next successful Stormspear throw into a Thunderbolt on impact; partial banks are kept.
- **Open Circuit**: the crown opens to the edge of its damage area and its lightning crawls across the ground almost to that edge, climbing rocks and stopping at walls and drops. Enemies that stay inside for 3 seconds take one 270% zap.
- Non-Gaze skill damage reduced 10%. Stormspear recharge 5 to 6 seconds.

**New skin**
- **Crimson Vow**, a mastery skin: win or obliterate on Monsoon as Hollow Saint.

**Presentation**
- Smaller chain-lightning hit flashes that no longer hide enemies, ground rings that follow slopes, tinted sparks, a softer Arc Step afterimage.
- Audio mix pass: Arc Bolt, spear throw, Arc Step and the Thunderbolt-ready cue are easier to hear; the spear charge loop is quieter.
- Shorter, clearer character select and skill descriptions in all four languages.

**Known limitations**
- Multiplayer has not had a real playtest yet. Physical controller acceptance remains unverified.

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
