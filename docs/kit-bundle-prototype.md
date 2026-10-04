# Hollow Saint combined private candidate

Source base: accepted Gaze fuel prototype `fc21389fa8b0377305f2da516f3b9441f8988dbc`,
on published 1.1.1 `20a821abb9f0f254d966536da82052007e734c1e`.
Branch: `codex/kit-bundle-prototype`. Plugin/package version stays 1.1.1.
The requested “1.2.5 or something” remains a provisional release label; select a
new public version only after review and playtesting. This is a private solo
candidate, with no Thunderstore manifest, profile installation or publication.

## Changes and limits

| Area | Candidate behavior | Gameplay boundary |
| --- | --- | --- |
| Hollowed Gaze | Stronger contrast between entry fuel diamonds, round reserve, intake, traveling collar, ground branches and confirmed contacts. Skin accent hues differ from the normal beam. Ambient haze and roots dim during events. | Accepted fuel mechanics are unchanged. Decorative branches do not create extra hits. |
| Arc Bolt | Default speed 80→120 m/s and radius 0.60→0.75 m; solid centered collision sphere, gravity off, nonkinematic Rigidbody with ContinuousDynamic. White core plus two skin contour rings show the configured sphere. | Damage, interval, proc, chains and forgiveness are unchanged. Above 80 m/s, lifetime shortens to retain the old nominal maximum range; fixed-step expiry is still quantized. Only the exact old speed default migrates; custom values remain. |
| Stormspear recharge | Vanilla recharge pauses during charge and before release, then resumes after an actual projectile fire. Living cancellation/failed throw refunds preserve the existing recharge stopwatch. | Existing stock count, cooldown scaling and queued spare-stock progress remain. Holding a spear cannot bank recharge time. |
| Stormspear conductor | An enemy hit sustains one weak conductor per owner for three seconds; newest enemy hit replaces it. Four ticks at 0.75-second intervals seek at most two distinct nearby enemy victims, excluding the host and owner, with world line of sight. Radius 4–6 m and coefficient 20–35% scale with captured charge. | Maximum eight secondary damage events; proc zero, no hit callbacks, no Static/refuel/marks/recursive chains. Throw crit is reused. Existing direct damage, burst and crown Thunderbolt remain. No terrain conductor. |
| Open Circuit | A transparent edge dome follows the actual core-centered spherical radius, including live config. Its lower half is faint. It follows the replicated buff, including observers who arrive later, and hides during invisibility. | Existing 8 m default radius, 60% pulse every 0.5 seconds, damage and targeting remain. This is a visual boundary, without shielding or collision. Existing pulses can pass through walls. |

Conductor damage is snapshotted at server projectile initialization from the
initialized projectile damage and server damage curve. Later owner stat or live
tuning changes do not alter its window. Remote owners need matching configuration;
this is not a new per-client tuning synchronization protocol.

The Gaze presentation uses a fixed maximum of 199 renderers per initialized body,
with inactive geometry disabled and reused. The dome adds 22 renderers and 694
positions updated per visible frame, built lazily once per body. The conductor
uses one reused component per owner, a fixed 64-collider search and two selected
victims per tick. Dense packs can overflow that query, so nearest means nearest
among the returned colliders. Its model is replaced per owner and sustains at
most one local crackle per 0.18 seconds. Death, disable, destroyed anchors and
stage changes retire the active conductor; Gaze and dome have their own cleanup.
No new asset bundle was generated: the candidate reuses the pinned existing
`hollowsaintassets` and code-built geometry/shared materials. New dome asset
failure disables that cosmetic component with one warning.

## Why the old conductor was not restored

Commit `fc726b2063ffa6de9d080327c10d20c51b1f16f8` replaced Conduit Spear with
Stormspear as part of the v0.9 kit redesign. Its message explicitly removed
planting, recall, ConductorMark and held spread. It does not establish a specific
balance reason for removing sustain.

The earlier anchor lasted until recall/death/leash, pulsed 50% every 1.5 seconds
in a 10 m area, and added owner Arc Bolt-triggered 35% spread. Backup Mag could
expand its range/target budget; its mark amplified Hollow Saint skill damage
and Static. The candidate restores a short, bounded automatic lightning window
without those indefinite anchors, item procs, marks or primary-triggered spread.

## Damage budget for comparison

These are coefficient budgets, at default settings, one stock, 1× attack speed,
no items, no critical hits, and uninterrupted repeated use. Travel/target loss
and fixed-step timing change practical results.

- Arc Bolt remains 120% every 0.5 seconds: 2.4 body-damage coefficient per second
  (36 DPS at body damage 15). More reliable hits can improve practical output.
- A full hand spear still deals 1400% directly. Recharge formerly overlapped its
  two-second charge; actual-throw recharge gives roughly 5 + 2 + 0.083 = 7.083
  seconds between full hand throws, about 1.98 coefficient/second direct output
  (29.65 DPS at damage 15), versus 2.8 (42 DPS) with activation-start recharge.
- Full crown charging takes 0.8 seconds at 1× attack speed: roughly 5.8-second
  throw period and 2.41 coefficient/second direct output (36.21 DPS). Its existing
  burst and independent bonus Thunderbolt are additional, unchanged budgets.
- Conductor totals 80–140% per repeatedly eligible victim over the four ticks,
  at most 160–280% aggregate across two targets on every tick, before the captured
  throw crit. It never adds sustain damage to the struck host.
- Open Circuit remains 1.2 coefficient/second while active (18 DPS at damage 15).

## Later playtest checklist

Installation needs a separate authorized handoff while the game is closed.

1. Compare Arc Bolt hits against moving small enemies, overlapping hurtboxes,
   thin terrain, steep angles, the owner, allies and range-edge targets. Confirm
   one hit dispatch and a visible collision envelope on every skin. Test custom
   speeds above and below 80 m/s and custom radius after restart.
2. Hold a hand/crown spear beyond its cooldown: no stock should recharge until
   thrown. Interrupt charge and pre-release, then test spare stocks and cooldown
   reduction items. Refunds must not erase existing recharge progress or grant
   extra stocks. Confirm hand apex release and crown immediate release.
3. Spear one enemy near a pack: existing burst remains, then four weaker ticks
   reach at most two other enemies through clear sight. Move behind a wall,
   replace the host with a second hit, kill either body, disable and change stage.
   No ongoing terrain anchor, repeated Static or strike-generated Storm charge.
4. Check Open Circuit dome against the true sphere on flat terrain, slopes,
   ledges and while airborne, and at configured radii 3, 8 and 25 m. Cloak and
   buff expiration must hide it. Look for excessive bloom or a misleading shield.
5. Compare Gaze with zero, partial and full entry fuel on all skins. Fuel intake,
   traveling energy, ground splash and target contacts should remain readable
   against a bright beam, many enemies and simultaneous Saints. Cancel during
   intake/travel/spread, then die or change stage: no pending fueled damage or
   lingering geometry. See `gaze-fuel-prototype-playtest.md` for resource cases.

Live appearance, collider contacts, performance and multiplayer timing have not
been validated in Unity. Gaze still lacks a mid-cast observer state snapshot,
so this package is for solo evaluation and does not claim multiplayer acceptance.
No game launch, screenshot, installed-profile write, push, tag or publication
is part of this handoff.

## Verification

Release build passes with zero errors and 19 existing warnings: one MMHOOK
framework compatibility warning and 18 existing obsolete-API warnings.
The actual installed-game member accessibility check passes; a stricter Gaze
scan resolves all 1,026 game/Unity member operands with no inaccessible members.

Offline production checks pass: 24 primary reliability, 2,853 dome geometry,
166 cooldown, 93 conductor, 1,591 Gaze fuel, and 501 Gaze presentation assertions
plus 1,000-cast reuse. Existing regressions pass: 12 Arc Bolt defaults, 12 flight
defaults, 20 spear defaults, 27 crown gesture cases, 67 spear snapshot cases,
actual engine transport ordering, animation rules and 33 language tokens across
four sections. Physics/rendering adapters do not establish native Unity behavior.

Independent read-only review found and closed impact-time conductor damage
normalization: its immutable schedule is now captured at projectile initialization.
No remaining default-path source blocker was found. Review retained the gameplay
checks above and the inherited mid-cast observer limitation.
