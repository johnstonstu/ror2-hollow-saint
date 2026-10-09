# Charge build toward 1.3: manual acceptance

Status: **local 1.3.0 with first-playtest Orb and Open Circuit refinements**. Stuart authorized
private staging and gameplay on October 6, 2026. Solo mapped-action trials now
cover damage, charge spending, fresh-target priority, two-target ping-pong,
empty-bank admission, Utility cancellation, overhead Open Circuit casts and
every installed skin. Pending rows below are the full human acceptance matrix;
automated coverage does not certify physical controls, ordinary combat feel or
two-client multiplayer.

The [balance plan](dev/charge-build-1.3-plan.md) records defaults and the
[refinement result](dev/orb-refinement-1.3-results.md) records current automated evidence.
Do not treat source adapters or installed-member scans as gameplay acceptance.

## Playtest controls

Select **Thundercloud** as the third Special and **Hollowed Orb** as the second
Secondary. Gaze and Stormspear remain defaults. Hollowed Orb works with an empty
Static bank; Thundercloud and Open Circuit require one charge. All need ready
native stock. Hold the mapped action, then release. Orb short casts preserve the
bank; its first empowering charge gathers at 0.50 s, then every 0.25 s. It has a
0.18 s minimum windup and 0.06 s release settle. The two Specials gather first at
0.12 s, then every 0.25 s, and settle for 0.26 s before commitment. Their release
before the first charge cancels. All freeze the gathered count and aim once.
Holding after full charge eventually auto-releases.

Open Circuit feeds gathered charges into the crown before its authored opening
sequence. One/three/five charges provide 1x/1.5x/2x area pulse density with more
lightning arcs. Duration, radius, per-pulse damage and spear multiplier are
unchanged. Extra pulses cannot accelerate direct Static generation. Its Orb is
held and launched overhead; Primary remains available throughout that gather.

Utility cancels gathering and attempts the equipped native dash once after the
gather state exits. Cancellation preserves charges. A committed Orb miss spends
its charges; releasing a cloud over an empty area cancels without spending.
Partial releases retain the rest of the bank. New gains during gathering cannot
expand that cast's entry allowance. Direct new-ability damage cannot directly
recycle its own Static cost.

Record DLL/bundle hashes, game build, configuration, level, items, skin, input
device, camera and host/client role. Begin with ordinary gameplay and no damage
items so the base values can be assessed. Repeat the controls with a physical
controller and remapped native actions. Record observations, not just crashes.
Damage percentages below are base coefficients before native armor, critical
hits, Shocked vulnerability and item modifiers.

## Acceptance matrix

| Case | Expected observation | Status |
|---|---|---|
| Loadout | Three Specials in order Gaze, Open Circuit, Thundercloud; two Secondaries Stormspear, Hollowed Orb; original defaults and icons intact | Pending |
| Empty bank | Orb tap launches without spending Static; the two charged Specials do not consume stock; Primary and ordinary Utility work | Pending |
| One charge | Large cloud and substantial Orb; no tiny minimum-charge attack | Pending |
| 1 / 3 / 5 charges | Cloud radius 16 / 23 / 30 m; Orb diameter 0.6 / 0.8 / 1.0 m; both visible growth and actual eligibility grow | Pending |
| Broad camera coverage | Aim near, midrange and distant groups spread across the screen; preview covers the intended group, reaching up to 80 m | Pending |
| Aimed cloud | Crown rises into the cloud; bolts descend from that cloud; eligible enemies around the aimed point, including behind it, receive one strike each | Pending |
| Cloud order and tail | Near-to-far rolling sequence, thunder, fade and crown return; no lingering damage ticks or final extra clap | Pending |
| Cloud empty / obstruction | Empty area preserves bank/stock; terrain blocks cloud-to-enemy strikes; preview communicates the intended coverage | Pending |
| Hop / hover | Short upward/backward gather hop, free camera aiming, ceiling/wall restraint; ordinary falling resumes after release/cancel | Pending |
| Orb hands and release | Both hands frame the growing ball; a forward two-handed throw and continuous visible flight sell its size | Pending |
| Orb fresh targets | With A, B and C reachable, A-B-C precedes revisits | Pending |
| Orb two enemies | A-B-A-B allowed; total hit budget is 3 / 5 / 7 at 1 / 3 / 5 charges, including first hit | Pending |
| Orb lone enemy | Harmless player return within 8 m feet proximity and clear core travel; two free or seven fully charged enemy hits maximum. Otherwise one hit; no instant A-A teleport or player damage | Pending |
| Orb wall / range | Nearby launch walls and intervening terrain stop the Orb; 70 m initial reach; bounce reach is 18 m free/one charge, 27 m at three, 36 m at five, with the same finite hit budget | Pending |
| Slow storm / moving victims | About 4–6 seconds from release through fade; four branching visual strokes per surviving visible target, one damage hit; later flashes stop if the target dies, leaves the radius or moves behind cover | Pending |
| Moving / dying target | Flight redirects only to an eligible living target; missed/dead victims receive no phantom damage | Pending |
| Damage without items | Free Orb: 157.5%, two hits. Cloud at 1/3/5: 270/495/720%; Orb: 225/360/495%; repeats retain 0.65 per previous victim hit | Pending |
| Static priming | Orb/cloud hits raise visible Static tiers but never Electrocute alone; a follow-up Arc Bolt finishes them and banks a charge; cloud survivors show Shocked | Pending |
| Closed Circuit | Feed 3 into Circuit in a pack: arcs return charges on nearby Electrocutes; leftovers telegraph then strike at close; empty area returns them | Pending |
| Spear finisher | Quick throw with full bank keeps the bank; full-charge throw with full bank flashes, chimes and calls the Thunderbolt; spear on primed enemies Electrocutes fast | Pending |
| Gaze priming | Blasts/surges leave crackling targets; Gaze timing and controls unchanged | Pending |
| Income guard | Late-game attack speed in big packs: log `HOLLOW_SAINT_STORM_INCOME` shows incomeLimited rising, bank not permanently full | Pending |
| Partial allocation | From five charges, release with two gathered, retain three, then use them for the other new option | Pending |
| Gather input contention | Normal two-hand Orb and Special gathers own inputs; overhead Orb leaves Primary available; simultaneous consumers never spend the same bank twice | Pending |
| Charged Open Circuit | At least one charge required; held charges enter crown, then native opening markers activate one paid window; 1/3/5 tiers visibly increase lightning density | Pending |
| Utility / interruption | Utility cancels and dashes once; stun, death, disable and stage/body loss clean up gravity, fall guard, poses, preview and bank ownership | Pending |
| Cooldown / extra stock | Cloud cooldown starts after sequence; Orb after release/recovery; cancelled casts return stock without resetting prior recharge progress; bonus stocks do not duplicate spending | Pending |
| Gaze + Orb | Gaze retains its released controls; equipped Orb cooldown/stock survives Gaze's contextual overrides, with no free refill | Pending |
| Stormspear + cloud | Ordinary hold-charge and full-bank Prayer remain available outside the new gather; no shared-bank double claim | Pending |
| Skins / camera / audio | Crown/hand poses restore; palette fits every skin; cloud does not hide the fight; loudness and thunder tail are readable | Pending |
| Language / options | Four languages have complete names/descriptions; options update numbers; cooldown changes require restart as labelled | Pending |
| Host / remote client | Same build/config: charge admission, release, partial bank, targets and Utility behavior agree; no client-authorized damage | Pending |
| Latency / reconnect | Retries never duplicate costs/hits; reconnect restores handlers; interrupted pending casts clean up; an unacknowledged throw cannot optimistically refund stock | Pending |

## Workshop after the first session

Judge cloud coverage and Orb size before increasing damage. Compare a one-charge
cast, a full-bank cast and splitting the bank across both skills. Record how long
ordinary combat takes to refill the bank; test packs and bosses separately. Tune
range, radius, coefficients, cooldown and hit budget in sections **8. Thundercloud**
and **9. Hollowed Orb**. Keep generation and the released spear economy unchanged
until these comparisons show a reason to change them.

The cloud and hand poses are additive runtime animation. Cloud smoke, lightning
and cues use code presentation and existing audio, with the released bundle
unchanged. Thundercloud is the selected Special name. Growth after Orb bounces,
a new thunder sound layer and further balance tuning remain workshop options.

## October 7 early-game and relay refinement

Arc Bolt defaults to 144% effective direct damage, with its existing cadence, proc coefficients and chain rules. Hollowed Orb can use the nearby owner as a harmless relay once only one or two reachable enemies remain. Keep within 8 m measured between the bodies' feet, including vertical separation, and keep the actual return flight within its charge-dependent bounce reach (18-36 m by default) and clear of terrain. Target loss and the 12-second lifetime can still end a flight. This permits a close tall boss without treating an enemy far above you as close. New enemies win before revisits; two enemies retain A/B alternation across player contacts, while a lone enemy can be revisited. Player contacts add no damage, healing, item procs, Static or enemy-budget consumption. Direct Orb/cloud/Gaze opening/surge damage does not build Static.

See [current native evidence](dev/early-balance-1.3-results.md). In human testing, compare an ordinary first-boss fight with Primary plus free Orb, keep charges for Special, then assess optional empowered Orb use. Repeat close/far, one/two/three enemies, movement, walls and physical mapped controls.
