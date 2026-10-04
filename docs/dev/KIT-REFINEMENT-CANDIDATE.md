# Hollow Saint kit refinement candidate

> **Historical report:** This records the pre-acceptance candidate, including its former 400-1600% spear values and then-pending runtime checks. The user later accepted the combined kit and 350-1400% Stormspear tuning in a single-player playtest. Release 1.1.1 is prepared from accepted source a6841d51; see [release notes](RELEASE-1.1.1.md). Multiplayer and physical controller acceptance remain unverified.

Local candidate only, based on published v1.1.0 (`95757859`), accepted Gaze VFX
(`6f5831ae`), and source base `38e7b3c1` prepared from that accepted candidate. No release or game profile
has been changed by this pass. Runtime acceptance remains pending. Recommended
eventual patch release: **1.1.1**, after acceptance; no version bump is made here.

## Delivery corrections

Stormspear previously reconstructed charge at impact by dividing projectile
damage by the owner's current damage stat. A fully charged 240-damage spear
launched at base damage 15 and landing after a level-up to 17.4 recovered only
81.61% charge: radius fell from 10 m to 8.71 m, burst fraction from 100% to
90.80%, and crown Thunderbolt eligibility was lost. Its direct damage was
unchanged. Charge now belongs to the individual projectile.

Crown bonus eligibility previously read the owner's current Open Circuit buff
after travel and the 0.25-second lodged delay. Crown throws could lose the bonus
when the buff expired; hand throws could gain it if the buff opened before
detonation. The original throw form now travels with the shot. Restoring the
promised bonus can recover 150 direct plus 75 splash damage at level 1; it is
not an increased coefficient.

Implementation: `StormspearThrowState` sets the custom projectile's reserved
`comboNumber` crown marker. Its unchanged knockback curve, `8 + 12 * charge`,
already carries charge as a float. `StormspearImpact` captures these fields in
`ProjectileController.onInitialized`, after the engine assigns them, and passes
an immutable `StormspearShot` through lodging to `SpearDetonation`. It does not
read owner charge, damage, or crown state at impact. Normal host and remote
authority `ProjectileManager.FireProjectile` routing is retained; no global
initialization hook, owner cache, or quantized charge payload is introduced.

Read-only decompilation of the installed RoR2 assembly verified that force and
combo are serialized, deserialized, forwarded into remote `FireProjectileInfo`,
assigned to `ProjectileDamage.force` and `ProjectileController.Networkcombo`,
then followed by `DispatchOnInitialized`. The helper owns both force encoding
and decoding so future knockback edits must keep this contract together.

## Conservative primary adjustment

Arc Bolt damage coefficient increases from 1.0 to 1.2. The interval remains
0.5 seconds and direct/chain proc coefficients remain 0.8 / 0.4 / 0.2 / 0.1.
At base damage 15 this changes direct fallback DPS from **30 to 36** (+20%).
Across four eligible targets, cumulative 0.75 chain falloff changes aggregate
DPS from **82.031 to 98.438**. During hand-spear charging, direct primary DPS
changes from **15 to 18**. The buff improves itemless output without increasing
hit frequency, target count, or reach. Damage-derived Static and damage-based
item effects can increase; this is not a claim that every downstream effect is
unchanged. Base damage remains **15 + 2.4 per level**.

Config migration must update only saved Arc Bolt Damage 1.0 values when moving
to defaults version 11. Custom values remain untouched. Historical proc-default
migration gaps are not silently repaired as part of this buff.

## Current mechanics inventory

Numbers assume base damage 15, base attack speed, and no crit, armor, items, or
Shocked amplification. Other ability coefficients are unchanged.

| Ability | Cadence and output | Item proc |
| --- | --- | --- |
| Arc Bolt, candidate | 120% every 0.5 s; 36 direct DPS; up to three hops at cumulative 0.75 falloff; hand-spear charge halves fire rate | 0.8 direct; 0.4/0.2/0.1 hops |
| Stormspear | 400-1600% (60-240 direct); 2 s full charge, 0.8 s in crown; one stock; 5 s recharge starts at charge activation | 1 direct |
| Spear burst | After 0.25 s; excludes direct victim; 200% tap to 1600% full (30-240); ground burst halves damage and radius | 0.5 |
| Crown spear Thunderbolt | 1000% direct (150), 500% splash (75); another 0.2 s delay; independent of passive charge/cooldown | 1 / 0.5 |
| Arc Step | No damage; two stocks, 5 s recharge each; 0.55 s dash, initial speed 20; no default invulnerability | None |
| Open Circuit | 60% pulse every 0.5 s (18 DPS per target in range); 10 s buff; 8 s cooldown after crown closes; first pulse at buff opening, approximately 0.875 s after cast | 0; Static-equivalent weight 0.3 |
| Gaze core | 1 s windup, 4 s beam, 0.4 s recovery; 12 s cooldown after skill ends; 100% per 0.2 s (75 DPS, nominal 300 total); attack speed adds ticks, interval floor 0.05 s | 0.5 |
| Gaze extras | Splash 50% per tick to enemies core missed; two 100% forks every 0.5 s, each able to chain once for 60% | Splash/fork 0.3; chain 0.2 |
| Electrocute | 0.5 s stun and 3 s of 15% increased damage taken; 4 s Static immunity; pop hits two neighbours for 150% (22.5) and adds 0.15 Static | Pop 0.3 |
| Passive Thunderbolt | Five electrocutes; 1000% direct/500% splash (150/75); 0.6 s telegraph + 1 s flight; 4 s minimum launch spacing | 1 / 0.5 |

Static gain is `clamp(damageDealt / (0.25 * fullHealth), .06, 1) * proc`, with
a 1.5 multiplier on critical hits. Decay starts after 2 s, at 0.5 per second.
An enemy dying with pre-existing Static at least 0.5 discharges; fresh one-shots
do not grant charge. This is a documented rule, not missed hit delivery.

## Verification and remaining acceptance

`tools/tests/Check-StormspearSnapshot.ps1` compiles
the actual shot helper and tuning source and passes **65 behavioral assertions**
covering float transport precision, host/remote field parity, overlapping throws,
unchanged knockback, damage-stat changes, radius/burst preservation, and crown
eligibility boundaries. Additional checks verify the installed engine's field
transport and initialization ordering, and impact-to-detonation source wiring.
These are isolated and structural checks, not a live Unity or multiplayer test.
Run it in a fresh PowerShell process; it requires `ilspycmd` and accepts a
`-GameAssembly` path, defaulting to the installed Steam RoR2 assembly. Decompiled
evidence stays in memory during the check and is not added to the repository.

The root integrator records build and other candidate verification separately.
Required later runtime checks: crown throw just before expiry; hand throw before
crown opening; full charge across level-up/damage buffs; multiple spears in
flight; remote-client parity. Mods that deliberately replace projectile force
or combo before its initialization callback can alter this custom contract.

Do not use `tools/balance/dps_model.py` as current-kit evidence: it models the
retired Conduit Spear/Conductor kit and old passive/crown values. The existing
`tools/tests/Check-Stormspear.ps1` likewise expects the obsolete 1400% full damage;
the published baseline is 1600%. Neither stale artifact was treated as a valid
acceptance gate for these changes.

## Flow and showcase pass

Movement now waits for pending gesture requests before transferring a pose between
upper-body layers. This prevents a stale hold from overwriting a newly queued cast.
Crown recovery only replaces crown-owned gestures: buff expiry cannot lower Gaze's
arms or overwrite a newer primary/spear cast. Interrupted crown startup and Gaze
exit clear their own hold poses. Gameplay startup, active time, recovery duration,
movement, cooldowns and interrupt priorities are unchanged.

Both English README pages now lead with the survivor fantasy, skill roles and
three practical combos. They reuse the approved native player-view Gaze screenshot,
without changing its pixels, and retain language, installation, credit and support
links. The new `docs/media/gaze-player-view.png` must be published to GitHub main
before the packaged README's public image URL works. No publication occurred.
Future footage should show simultaneous bolts/spear, corrected crown-spear payoff,
the passive halo cycle, Gaze cancellation and Arc Step positioning.

## Combined integration checks

Release build: **0 errors, 19 existing warnings**. The repository's read-only
real-game assembly access check passes. Existing animation-rule checks and all
33 language tokens across four sections pass. The primary-default check passes
12 cases, including nearby custom values and repeat/version guards. Accepted
Gaze beam, server, tuning and all FX source match the accepted candidate after
line-ending normalization; only crown arm cleanup changes in GazeState.

These checks establish compilation, source contracts and isolated behavior. They
do not establish native visual quality, real multiplayer timing, frame cost or
in-game DPS. No game, Unity batch playtest or screenshot capture ran during this
refinement task. Installed profiles and the AH64 project were not modified.

## Stuart's playtest checklist

1. Start with the default config; confirm Arc Bolt reads 120%, feels better against
   early single targets, and still fires at the same rate while charging a spear.
2. Throw a fully charged crown spear just before crown expiry; confirm its bonus
   still lands. A hand throw made before crown opening must not gain that bonus.
   Repeat across a level-up or damage buff and with several spears in flight.
3. Move/strafe/jump while alternating bolts, spear and crown; interrupt crown
   startup. No stale hold, skipped gesture or crown close should replace a new cast.
4. End Gaze by timeout, recast, Arc Step, interruption and death; test crown expiry
   during Gaze. Confirm arms recover and accepted ambient/hit tendrils clean up.
5. Repeat on host and remote client, all skins, and high attack speed. Compare
   readability, actual hit delivery and frame cost. Controller acceptance remains
   a separate hands-on check.

Focused crown-flow verification: `tools/tests/Check-CrownGestureFlow.ps1` passes
27 checks using the production helper and explicit lightweight adapters. Cases
include current, incoming and queued primary/spear casts on either arm layer,
cancelled holds, death/dash, Gaze overlap, normal recovery and movement transfers.
This is not an Animator-controller or live Unity acceptance test.
