# Hollow Saint 1.3.1: game patch fix and balance pass

Historical first-pass record. The [current handoff](audit-handoff-1.3.1.md) supersedes the candidate identity, migration counters, later Gaze changes and open-item status below.

Prepared 2026-10-08 night. Staged in the Hollow Saint Dev profile; not committed, packaged for release or pushed.

## Game patch fix (October 2026 RoR2 patch)

- The patch added `ProjectileController.ghostPrefabAddress` (replacing `ghostPrefabReference`), and `Awake` now prefers a valid address over `ghostPrefab`.
- Arc Bolt and Stormspear are clones of Artificer's `MageLightningboltBasic`, so they inherited its address and showed Artificer's blue bolt.
- `Ghosts.Assign` clears the inherited address by reflection (works on either field name and against the older compile references), then sets our ghost. Logs `HOLLOW_SAINT_GHOST_ASSIGNED` and, once per session, `HOLLOW_SAINT_GHOST_LIVE`.
- Native access scan against the patched `RoR2.dll`: pass. No member Hollow Saint uses was removed. Other new address fields (`EffectDef`, `ProjectileGhostController.ghostReference`) do not affect our content.
- The `jumpWasClaimed` error spam seen right after the patch came from other mods and is gone after the dependency updates.

## Benchmarks used

Level 1, no items, values as % of the survivor's own base damage per second. Hollow Saint has 15 base damage against vanilla's 12, so its numbers are shown x1.25 as "vanilla-equivalent".

| Reference | Output |
|---|---|
| Commando Double Tap | 100% per 0.15 s, proc 1.0 (~667%/s; ~67 DPS on a level-1 Titan) |
| Huntress Strafe | 150% per 0.5 s, proc 1.0 (300%/s) |
| Artificer Nano-Spear | 400-1200%, 5 s cooldown, 2 s charge |
| Artificer Ion Surge / Flamethrower | 800% per 8 s / ~2095% per 5 s |
| Huntress Arrow Rain / Ballista | 2090% (proc 0.2 x 19) / 2700%, 12 s |
| Modded norms (Enforcer, Arsonist, Deputy, Cyborg, Paladin) | multi-hit and AoE skills use proc 0.5-0.75; single hits 1.0; damage +20% of base per level |

Measured in game (autopilot `early-balance`, itemless level-1 Titan, armor 20): Arc Bolt at 144% = 35.3 DPS. At 171% it scales to about 42 DPS.

## What was off

- Arc Bolt was at Huntress level and about half of Commando: early fights felt slow.
- Hollowed Orb (free ~145%/s) and Thundercloud (free ~40%/s AoE) were far below vanilla secondaries and specials.
- Orb, Thundercloud and Open Circuit ignore attack speed, and their procs were low (Cloud 0.4, Orb repeats 0.1, Circuit 0), so items did little for them. Stormspear and Gaze scale with both, which is why some abilities felt much stronger than others.
- Orb and Cloud prime to 35% Static, but death discharge needed 50%, so primed enemies killed by spenders paid nothing. With Orb + Thundercloud the bank stayed empty on stage 1.
- `levelDamage` was a flat 2.4 on a 15 base (16% per level), below the vanilla 20% rule.
- Full Stormspear (1400% plus a 100% area burst) outpaced Nano-Spear.

## Changes (only untouched defaults migrate)

| Setting | Was | Now |
|---|---|---|
| Death discharge (Static held at death) | 0.5 | 0.3 |
| Arc Bolt damage / proc | 1.6 (144%) / 0.8 | 1.9 (171%) / 1.0 |
| Open Circuit pulse damage | 0.6 (54%) | 0.8 (72%) |
| Stormspear full damage | 14 (1400%) | 12.5 (1250%) |
| Thundercloud strike / per charge | 0.9 / 0.18 (81-162%) | 1.65 / 0.25 (149-261%) |
| Thundercloud duration / cooldown / proc | 3 s / 12 s / 0.4 | 4 s / 10 s / 0.5 |
| Thundercloud attack speed | none | strike interval / attack speed, captured at cast, max 2x rate (0.375 s) |
| Hollowed Orb hit damage | 4.1 (297% free) | 5.0 (378% free, 738% at five) |
| Hollowed Orb cooldown | 7 s | 6 s |
| Hollowed Orb proc first / repeat | 0.5 / 0.1 | 0.8 / 0.25 (0.3 stays the burst) |
| Damage per level | 2.4 | 3.0 (20% of base) |

Gaze, Arc Step, Static thresholds and the income guard (2 charges/s) are unchanged.

Migrations: main step 16 (`Defaults version` 16) and a new `Charged defaults version` counter (2) for Thundercloud/Orb, which bind after the main migration pass.

## Verification

- All registered source suites pass (ChargedStormChecks 2335 assertions, ConfigMigrationChecks incl. new charged-migration cases, Arc Bolt/Stormspear default checks updated). Native access scan pass.
- In game: smoke runs with 0 errors; first-strike Cloud and Orb damage match the new coefficients natively; Cloud proc 0.5 confirmed.
- Independent review of the diff: no P1/P2; P3 hardening applied (safer reflection fallback, ghost warning, comments).
- Stu's dev config migrated to the new values. Arc Bolt damage was set to 1.9 by hand there, because a test launch had already stamped that config to version 16 before the Arc Bolt step existed. Backup: `com.johnstonstu.hollowsaint.cfg.bak-20261008`.

## Known gaps / next

- The `charge-13` autopilot segment is stale (expects one strike per enemy and the old 0.30 s gather tick). Its failures are script timing and the Shock bonus, not gameplay. Update or retire it.
- `AccessScannerFixtures` fails on Windows PowerShell 5 (enum cast); the real scan works.
- The in-game Thundercloud text still shows the base 0.75 s interval and doesn't mention attack speed.
- Open Circuit still ignores attack speed (approved fixed 0.5 s cadence). Candidate for a later pass with a low proc.
- Health is Commando's 110 (+33) with 15 armor; modded survivors commonly sit at 130-160. Watch deaths in playtest before changing.
- Multiplayer remains untested.

## Playtest focus

1. Orb + Thundercloud on stage 1-2: does the bank fill now, and do free casts feel worth pressing?
2. Arc Bolt feel early, and whether Spear + Gaze still feels strongest.
3. Attack-speed items with Thundercloud (Syringes): strikes visibly faster, effects not too noisy.
4. Late game: bank sitting at the income guard; Cloud with many proc items.
