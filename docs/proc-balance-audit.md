# Proc and item audit — 2026-10-01

Read-only audit of the current kit, including the locally built v0.9.5 pose
candidate. **No proc values have changed.** Values below are source defaults;
the profile's saved config can override the exposed sliders. Subsequent v0.9.6
installed the cocked hold and gentle homing for Arc Bolt/Stormspear. More shots
landing can increase practical item output without changing these coefficients;
reassess balance with the improved accuracy before broad reductions.

## Ability coefficients

| Damage source | Current proc per enemy hit | Frequency / overlap at base attack speed |
|---|---:|---|
| Arc Bolt direct | 1.0 | 2 bolts/s; attack speed increases this |
| Arc Bolt three bounces | 0.5 / 0.5 / 0.5 | Up to three additional enemies per bolt; damage falls by 0.75 per bounce, proc does not |
| Stormspear direct | 1.0 | 5 s stock recharge; 2 s full charge; Backup Magazines allow stock dumps |
| Stormspear impact burst | 0.5 | Every enemy in 3–9 m, no target cap; surviving direct victim can also receive the burst |
| Thunderbolt direct | 1.0 | Passive needs six Electrocutes and has a 4 s minimum cooldown |
| Thunderbolt splash | 0.5 | Up to eight other enemies within 3 m; direct victim excluded |
| Crown spear's extra Thunderbolt | 1.0 direct / 0.5 splash | Same hit coefficients; triggered by a full crown spear independently of passive charge/cooldown |
| Electrocute pop | 0.3 | Up to two neighbours; Electrocute capped at four/s per Saint |
| Open Circuit pulses | 0 | Two pulses/s within 8 m; kills can still trigger on-kill items |
| Gaze core | 0.5 | Five ticks/s per beam target; pierces enemies; attack speed increases ticks, capped at 20/s |
| Gaze impact splash | 0.3 | Every core tick; excludes enemies already hit by that tick's core |
| Gaze forks | 0.3 | Two volleys/s with up to two forks each |
| Gaze fork chains | 0.2 | One additional hop per fork; targets deduplicated within a volley |
| Arc Step | No damage hit | No item roll of its own |

Gaze core and splash deduplicate each tick, but forks do not exclude the core's
victims. One enemy can receive a core hit and a fork in the same volley. The
four-second beam duration and special cooldown limit sustained throughput;
the high proc rate applies during the beam.

At zero luck, before downstream item effects, a 10% on-hit item has a 10% roll
on a coefficient-1 hit and a 5% roll on a coefficient-0.5 hit. Coefficients are
weights, not absolute chances. Gaze core alone supplies 2.5 coefficient units/s
per continuously hit enemy versus Arc Bolt's 2.0 on its direct victim. Splash,
forks and additional pierced targets add separate rolls.

## Item behaviour checked against the installed game

These are different systems; a blanket coefficient reduction affects them
differently. This is a focused audit of representative items, not a claim that
every DLC or third-party item has been exercised in a live run.

| Item / category | Interaction with the incoming coefficient |
|---|---|
| ATG Missile Mk. 1 | Native 10% chance multiplied by incoming proc; missile proc mask prevents repeating that item in its own chain |
| Ukulele | Native 25% chance multiplied by incoming proc; native chain mask prevents repeat Ukulele in that item chain |
| Sticky Bomb | Native 5% per stack multiplied by incoming proc |
| Tri-Tip Dagger / ordinary bleed roll | Body bleed chance multiplied by incoming proc; special guaranteed-bleed damage is a separate path |
| Sentient Meat Hook | Native hyperbolic stack chance multiplied by incoming proc |
| Stun Grenade | Proc weight enters its hyperbolic chance calculation; do not assume every item simply multiplies its final displayed chance |
| Leeching Seed | Healing amount is stack count multiplied by incoming proc, rather than a random roll |
| Runald's / Kjaro's Bands | Incoming proc must be positive; native damage/base-damage threshold is at least 4.0 and bands must be ready. A smaller positive proc does not add a lower random chance |
| Brilliant Behemoth | Uses `OnHitAll`; requires nonzero proc, and explosion radius scales with incoming proc. It is not an ordinary chance roll |
| Will-o'-the-wisp / Gasoline / Topaz Brooch | Native death handler responds to credited kills. Lowering the killing hit's on-hit proc coefficient does not directly reduce these on-kill triggers |

The installed native `BlastAttack.PerformDamageServer` sends both `OnHitEnemy`
and `OnHitAll`. Arc Bolt and Stormspear direct hits also send both. Manual
Arc Bolt bounces, Gaze hits, Electrocute pops and Thunderbolt hits send only
`OnHitEnemy`. **Their nonzero proc values therefore do not give complete
Brilliant Behemoth compatibility.** This should be corrected and checked with
the item before treating current item-output comparisons as final. Increasing
the coefficient alone cannot fix a missing event. No event change is included
in the current pose candidate.

## Static and downstream effects

Ordinary skill damage and eligible item damage attributed to Hollow Saint can
build Static; gain is weighted by incoming proc. Lowering Arc Bolt, spear or
Gaze coefficients therefore also slows their passive generation, despite
unchanged raw skill damage. Killing hits do not build Static on the dead victim.

There are deliberate exceptions:

- Open Circuit has proc 0 but uses a separate Static weight of 0.3.
- Electrocute pop gives neighbours a fixed 0.15 Static cascade. Its item proc
  coefficient does not control that explicit cascade.
- Electrocute and Thunderbolt damage, including synchronous item callbacks,
  run under the storm-damage guard and cannot immediately refill Static through
  the ordinary damage handler. Later item hits can enter the normal handler.

The chain hop damage falls by 0.75 independently of its proc weight. Item
effects can launch further native damage with their own coefficients and masks;
lowering a skill coefficient changes the chance to start those effects, not
necessarily their eventual damage or on-kill explosion propagation.

## Suggested first balance trial (not applied)

Keep meaningful direct attacks at 1.0 and Open Circuit at 0. Start with the two
clearest sources of repeated rolls:

1. Arc Bolt bounces: **0.5 / 0.25 / 0.125**. Full four-target coefficient sum
   becomes 1.875 instead of 2.5: 25% fewer expected ordinary chance-based
   on-hit triggers across the whole bolt, before luck or downstream chains.
2. Gaze core: **0.25** instead of 0.5. This becomes 1.25 coefficient units/s
   per core target at base attack speed. Retest the passive cadence because
   core Static contribution is also halved.

Keep pop 0.3 for the first trial: it already has a two-target limit and an
Electrocute rate cap. If packs still generate too many items, the next trials
are spear burst **0.25** (currently 0.5), Thunderbolt splash **0.25** (currently
0.5), and Gaze splash/fork/chain **0.15 / 0.2 / 0.1** (currently 0.3 / 0.3 / 0.2).
These are proposed starting values, not measured final balance. Change them
in a separate trial so their effects can be attributed.

Do not globally modify vanilla items to balance this survivor. If lower item
output should preserve the current passive cadence, introduce explicit Static
weights for the affected skill hits instead of compensating with a global
threshold change that would also buff other hits.

## Verification and next playtest

Evidence: source review plus Mono.Cecil inspection of the user's installed
`RoR2.dll`, SHA256
`0497A902A7AAF3C97FA2F1251A5364723B0C1B422839F7002A4B5F9C02563E4F`.
Native methods checked: `GlobalEventManager.ProcessHitEnemy`,
`OnHitAllProcess`, `OnCharacterDeath`, `BlastAttack.PerformDamageServer`, and
`SetStateOnHurt.OnTakeDamageServer`. No proprietary game implementation was
copied into this repository. No live item-output validation was performed.

For a balance build, success criteria are: diminishing coefficients on each
additional Arc Bolt hop; direct attacks retain coefficient 1; each nonzero
manual damage source reaches the intended item events exactly once; no
recursive re-entry into the skill's own chain; and deliberate preservation or
retuning of Static cadence. Check direct hits and additional targets separately
with ATG/Ukulele, bleed, Seed, bands, Behemoth and on-kill items. Start with
isolated item loads, then test a mixed late-game inventory and attack speed.
Chance rolls need repeated samples; one successful activation does not prove
the intended probability.

Current pose build/staging status remains in `../PLAYTEST.md`.
