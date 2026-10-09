# Storm loop plan (1.3 candidate, October 7 2026)

Goal: one readable loop for the whole kit. Finishers earn charges, spenders use
them and prime what they hit, Open Circuit pays back. Every Secondary + Special
pair has a plan, nothing eats the bank by accident, and late game does not turn
into a permanent full bank with no decisions.

Builds on [storm flow results](storm-flow-1.3-results.md) (Orb/cloud priming,
cloud Shock, 0.25 s gather, cloud look, text). Status of in-progress work at
plan time: Closed Circuit, Gaze/Thunderbolt priming and empowered-pulse
finishing are coded, build and pass 920 linked assertions, but are **not staged**.
The README diagram (`docs/media/storm-loop.svg/.png`) exists; README text is
untouched.

## 1. The rules (design contract)

| Rule | Meaning |
|---|---|
| Finishers build Static | Arc Bolt, Stormspear, Gaze beam core, Open Circuit pulses. Ordinary Static gain. |
| Spenders prime | Any hit paid for with charges leaves its target at up to 95% Static. Never Electrocutes, never awards a charge by itself. |
| Free primer | Hollowed Orb tap: costs nothing, primes. The on-ramp to the loop. |
| Circuit pays back | Charges fed into the crown return one per Electrocute within 12 m; leftovers fall as closing strikes when it closes; nothing in range returns them. |
| Spending is a choice | No skill takes charges unless you held it for them. |

## 2. Secondary roles

**Stormspear: the finisher.**
- Spear hits (impact, burst, conductor) on an enemy that already has Static gain
  **2x Static** when it carries a spender's primed mark (config `Primed Static multiplier`, 2.0). A charged spear into a
  primed pack pops it. Implementation: `StormServer.OnServerDamageDealt`, source
  Stormspear and existing Static above zero before the hit.
- The full-bank Thunderbolt only spends the bank on a **fully charged throw**
  (`shot.FullyHeld`). Taps never touch the bank. Ordinary full-charge strike
  unchanged. Implementation: `StormspearProjectile.CaptureShot`.
- Ready cue: while the bank is full and the spear reaches full charge, the spear
  tip crackles brighter and plays the existing meter-full tick once, so the
  player knows this throw will spend the bank. Code VFX only.

**Hollowed Orb: the primer.** No mechanical change beyond tonight: tap free and
primes 35% (falloff on revisits), hold spends for size/hits/reach. Its own hits
never finish.

## 3. Specials

**Gaze of the Hollow** (feel untouched): opening blast, surges and the lock-on
strike prime 35%; beam core keeps building Static as a finisher. Text gains one
line. Healing, timing, controls unchanged.

**Open Circuit: Closed Circuit** (coded, needs presentation):
- Ledger per crown: fed count, refunds (one per Electrocute within
  max(radius, 12 m), skipped while the bank is full or Gaze owns it, never more
  than fed). Recasting during a crown discharges the old debt first.
- Close: owed charges become strikes at 270% each within 30 m, primed enemies
  first, 0.45 s then 0.16 s apart, each with a closing ground telegraph, each priming its target. No targets: charges
  return to the bank.
- Empowered (extra) pulses build Static only on enemies that already have Static,
  so Circuit finishes primed targets without raising baseline income.
- Presentation to add: refund arc victim-to-crown (done) plus a short crown flare
  and the charge-tick cue; on close with debt, the crown flares and the existing
  crown-release gesture plays before the strikes fall from the sky
  (`CrownGestureFlow`, code-driven, authored layers/states unchanged).

**Thundercloud**: as tonight (prime 35% + Shock).

**Full-bank Thunderbolt**: splash primes 35%; main target is Electrocuted as before.

## 4. Anti-flood safeguards (late game, attack speed, big packs)

Existing limits that already bound the loop: bank cap 5; at most 4 Electrocutes
per second per player; 4 s per-enemy Electrocute immunity (priming respects it);
95% priming cap; every spender has a cooldown; Circuit refunds never exceed what
was fed. Late game, large hits already fill Static in one hit, so priming matters
mostly early and on bosses; flood risk comes from baseline Static, not the loop.

New, deliberately light:
- **Bank income limit**: at most 2 charges per rolling second from all sources
  (Electrocutes, death discharges, refunds). Electrocutes past the limit still
  stun, Shock and pop; they just do not bank. Config `Charge income per second`
  (2, 0 = off). Implementation: one gate in `DischargeMeter.AddCharge`.
- **Telemetry**: `StormTelemetry` logs charges earned per minute, refunds,
  primes and wasted (bank full) charges, so a playtest log shows whether the bank
  is flooding.
- Tuning knobs already exist for every prime amount and the cap. Rule of thumb
  after playtest: if the bank sits full in packs, lower Orb/cloud priming first,
  then the income limit.

## 5. Build-outs (README section)

| | Gaze | Open Circuit | Thundercloud |
|---|---|---|---|
| **Stormspear** | Stormcaller: blast primes, spear and beam finish | Lancer: fast spears finish inside the crown, Electrocutes refund it | Siege: cloud primes far away, spear finishes from range |
| **Hollowed Orb** | Seer: orbs prime, beam core finishes | Conductor: orbs prime, pulses finish, crown refunds (most self-sustaining) | Tempest: everything primes, Arc Bolt chains finish (high risk, high reward) |

## 6. Text, docs and visuals

- Language (4): Stormspear (finisher bonus, full-charge Thunderbolt), passive and
  keywords (spend/prime/finish rules), Circuit (Closed Circuit), Gaze (prime line),
  survivor summary. Language checks updated.
- README x8: What's new note; new "How the storm works" with the loop diagram and
  role table; "Build-outs" grid replacing "Put it together"; Open Circuit and
  Stormspear kit paragraphs. Root READMEs use the relative PNG, package READMEs
  the raw GitHub URL (Thunderstore does not render SVG).
- Diagram: update SPEND/FINISH boxes for the spear finisher bonus; second small
  graphic for the build-out grid.
- Changelog, results doc, project doc, manual acceptance rows.

## 7. Tests

Linked checks: ledger (done), spear Thunderbolt only on full charge with full
bank (StoredPrayerChecks), income limit window, priming policy (done).
Runtime-only (playtest): refund distance, closing strike targeting and look,
spear double Static, crown flare, telemetry output.

## 8. Order of work

1. Spear: full-charge Thunderbolt gate, primed 2x Static, ready cue.
2. Income limit + telemetry.
3. Closed Circuit presentation (flare, gesture, cue).
4. Text in 4 languages + checks.
5. Diagram update + build-out graphic + README x8 + changelog.
6. Full verification, build, stage to Hollow Saint Dev with backup, results doc.

## 9. Playtest checklist for Stuart

1. Each of the six pairs for one stage; note whether the bank feels starved, right
   or always full.
2. Tap spear with a full bank: bank must stay. Full-charge throw: Thunderbolt.
3. Open Circuit with 3 fed in a pack: refunds visible, any leftovers strike at close.
4. Late game (attack speed items): read `HOLLOW_SAINT_STORM_INCOME` lines in the log.
5. Gaze still feels the same, plus crackling targets after blasts.
