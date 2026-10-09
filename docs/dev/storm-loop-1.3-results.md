# Storm loop results (1.3 candidate)

October 7, 2026 (Pacific). Implements the [storm loop plan](storm-loop-1.3-plan.md) on
top of the [storm flow pass](storm-flow-1.3-results.md). Local only: nothing
committed, pushed or published. Staged to the Hollow Saint Dev profile.

## What landed

| Area | Behaviour | Owner |
|---|---|---|
| Primed mark | Spender priming sets a `primed` flag on the victim's Static; cleared on Electrocute, death discharge or full decay. | `StormServer` |
| Gaze | Opening blast, surges and lock-on under-strike prime 35%. Beam core unchanged (finisher). Timing/controls/healing untouched. | `GazeFuelPulse`, `GazeFuelController.Under` |
| Thunderbolt | Funded splash primes 35%; main target still Electrocuted. Bank is only claimed by a fully charged throw. | `ThunderboltDriver`, `StormspearProjectile` |
| Stormspear | Hits on primed enemies build 2x Static. Ready cue (flash, ring, meter-full chime, faster tip sparks) when full charge meets a full bank. | `StormServer`, `StormspearFx` |
| Closed Circuit | Fed charges refund one per awarded Electrocute within 12 m while the bank can take it; owed charges at close become telegraphed 270% strikes within 30 m, primed first, retargeting if a target dies; nothing left in range returns them. Recast appends behind queued strikes. | `ClosedCircuitLedger`, `ClosedCircuitDriver`, `OpenCircuitBuff.Opened` |
| Circuit pulses | Empowered pulses feed Static only to primed enemies, and a primed enemy they kill still death-discharges. | `OpenCircuitPulseDriver`, `StormServer.FeedPrimed/FinishPrimedDeath` |
| Income guard | Token bucket, 2 per second (burst of 2), on every new charge including Gaze reserve and refunds; returns bypass it. | `ChargeIncomeBucket`, `DischargeMeter` |
| Telemetry | `HOLLOW_SAINT_STORM_INCOME` every 30 s, always on: charges per minute, primes, refunds, closing strikes, income-limited, wasted at full bank. | `StormTelemetry` |
| Config | Closed Circuit, refund reach, closing strike damage/range/priming, empowered pulses finish primed, Gaze priming, Thunderbolt splash priming, charge income per second, spear primed multiplier, Thunderbolt needs full charge. | `KitConfig.ChargedStorm` |
| Text | Spear, Circuit, Gaze, passive, keywords, survivor text in 4 languages; Circuit interval now reads short-to-long. | language file, `KitDescriptions` |
| Docs | Loop diagram and build-out grid (`docs/media/storm-loop.*`, `storm-builds.*`), 8 READMEs (sonnet helper), changelog, acceptance rows. | |

## Review

An independent Opus review found no P1 defects: no self-refunding spender, no
recursion, balanced storm-damage scope, correct server/client split. All of its
P2/P3 findings were fixed before staging: empowered-pulse kills now
death-discharge; dead closing-strike targets retarget or return the charge; the
Gaze reserve path no longer burns an income token on a rejected gain; Circuit
interval order; always-on income log; recast strike timing; Unity-safe component
lookup; client-side spear cue during Gaze; config/plan wording.

Open risks it raised (watch in playtest, not defects):
- Stormspear + Open Circuit/Thundercloud has no free primer, so the Special waits
  for the first Electrocute on stage 1.
- Late game the bank may sit at the income guard; stuns/arcs are not capped.
- In dense fights refunds compete with Electrocute awards for income tokens, so
  more fed charges come back as closing strikes.
- Item procs off spender hits can still build Static (pre-existing).
- Not covered by linked tests: `PrimeStatic` lifecycle, spear 2x path, the
  Closed Circuit driver, the spear full-charge gate. These need gameplay.

## Verification

- Native verification `artifacts/verification/20261008T044912-916210Z`: every
  source suite, restore and runtime build pass; ChargedStormChecks 945 assertions
  (884 at handoff); Language checks pass. `AccessScannerFixtures` fails only
  because this PC lacks PowerShell 7; the real `Check-Access` scan passes on the
  staged DLL.
- No gameplay was run.

## Staged build

Backup of previous files: `artifacts/foundation/profile-backup-20261007-214952`.

| Asset | SHA-256 |
|---|---|
| DLL | `F8941B4E7D3AD60D698D8C58520505CC289A2C6AF9B4596E341C8A63AD1984AA` |
| Language | `B66AA2B423D90257EF5905E240E02CA0DE73F42052765ECEC580F002F828442F` |
| Bundle | unchanged |
