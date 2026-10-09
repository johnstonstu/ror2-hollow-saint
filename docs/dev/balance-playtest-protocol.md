# Hollow Saint balance playtest protocol (overnight, agent-run)

For a game-testing agent. Goal: find which builds and item mixes are ahead or
behind, using repeatable fights plus a few natural runs. Record small, structured
data only. Do not change code, mod files or the repo except the results folder.

## 0. Rules

- Play only the **Hollow Saint Dev** profile in r2modman ("Start modded").
- Never touch the Steam game install (`steamapps\common\Risk of Rain 2`), other r2modman
  profiles, or other mods' files, and never run Steam repair/verify yourself. If the game
  will not start, stop and report.
- Never edit files under `HollowSaintMod/`, `tools/` or `docs/`. Write only to
  `artifacts/balance-playtest/`.
- One build per line of the matrix. If the game crashes or a run is invalid,
  note it in one line and move on; do not retry more than once.
- Keep data small (section 5). Never copy a whole log into the repo.
- Never edit or patch mod source, even to add logging. If something blocks scoring, stop and report; the developer adds instrumentation and rebuilds.

## 1. Setup (once)

1. In the profile config `BepInEx/config/com.johnstonstu.hollowsaint.cfg`, under
   `[6. Misc]`, set `Event log = true` (adds short storm summaries). Leave all
   other values alone.
2. DebugToolkit is installed. Open the console with Ctrl+Alt+` (backquote).
   Commands used below: `god`, `no_enemies`, `give_item <name> <count>`,
   `remove_all_items`, `spawn_ai <MasterName> <count>`, `next_stage`, `time_scale 1`.
   Do **not** use `kill_all`: with the EnemiesReturns mod installed it can throw an
   error. Always type full master names (below) so other mods' variants are not picked.
   If a command name differs, run `help` once and use the closest match; note it.
3. Character select: Hollow Saint. Default skin. Pick Secondary and Special per
   the build (loadout screen). Difficulty: Rainstorm. No artifacts.

## 2. Builds (Secondary + Special)

| Code | Secondary | Special | Name |
|---|---|---|---|
| B1 | Stormspear | Gaze of the Hollow | Stormcaller |
| B2 | Stormspear | Open Circuit | Lancer |
| B3 | Stormspear | Thundercloud | Siege |
| B4 | Hollowed Orb | Gaze of the Hollow | Seer |
| B5 | Hollowed Orb | Open Circuit | Conductor |
| B6 | Hollowed Orb | Thundercloud | Tempest |

## 3. Item sets (give at the start of each fight, after `remove_all_items`)

| Code | Theme | Items (DebugToolkit names, count) |
|---|---|---|
| I0 | None | nothing |
| I1 | Early damage | Syringe 3, Crowbar 2, CritGlasses 2 |
| I2 | Procs | ChainLightning 2, BleedOnHit 2, StickyBomb 1, Missile 1 |
| I3 | Cooldowns and stocks | SecondarySkillMagazine 2, AlienHead 1, UtilitySkillMagazine 1, Bandolier 1 |
| I4 | Late scaling | Syringe 10, CritGlasses 6, Crowbar 5, ChainLightning 3, AttackSpeedOnCrit 2, Behemoth 1, AlienHead 2 |

## 4. Scenarios (stage 1, Distant Roost or Titanic Plains; `god` on, `no_enemies` on)

Stand in an open area. Start a stopwatch from the in-game timer or the system
clock when the first enemy is in range. Use the skills the way the build intends:
prime with Orb/cloud/Gaze blasts, finish with Arc Bolt/spear, feed Circuit.

| Code | Fight | Measure |
|---|---|---|
| S1 | Boss: `spawn_ai TitanMaster 1` (Stone Titan) | seconds to kill |
| S2 | Pack: `spawn_ai BeetleMaster 10` | seconds to clear all |
| S3 | Mixed: `spawn_ai GolemMaster 3` then `spawn_ai WispMaster 6` | seconds to clear all |

**Scoring is automatic.** With Event log on, the mod writes
`HOLLOW_SAINT_FIGHT_START t=<time> monsters=<n>` when the first enemy appears and
`HOLLOW_SAINT_FIGHT_CLEAR t=<time> seconds=<duration> chargesEarned=<n>` when the
last one dies. Use `seconds` from the CLEAR line as the fight time. Make sure no
other monsters are alive before spawning (`no_enemies` on, wait for the stage to be
quiet). If no CLEAR line appears within 180 s, record `>180`.

**Aiming:** spawns appear near you. Back off to about 10 to 15 m, face the enemies
and hold Primary (Arc Bolt has aim assist). Use the Secondary and Special every time
they are ready. If Arc Bolt shows no hits after 10 s, you are not facing the enemies:
turn toward them before continuing; do not record that fight.

Between fights: wait for the CLEAR line, then 3 s, check the bank (charge buff icon) and note
it. Each fight starts with whatever charges you have; do not farm charges first.
Cap each fight at 180 s; if not finished, record `>180` and the boss HP left.

**Run order** (most useful first; stop wherever the night ends):
1. All builds x I0 x S1, S2, S3 (18 fights).
2. All builds x I4 x S1, S2, S3 (18).
3. All builds x I1 and I3 x S1, S2 (24).
4. All builds x I2 x S2, S3 (12).
5. Natural runs: for B2, B5 and B6, start a fresh Rainstorm run with `god` OFF,
   no commands, play stages 1 to 3 normally (about 15 min each). Record clear
   time per stage, deaths, and whether the Special was often waiting on charges.

## 5. Data (keep it small)

Folder: `artifacts/balance-playtest/`. Only these files:

**`results.csv`** (one row per fight, append only; `seconds` from the FIGHT_CLEAR line):
```
time,build,items,scenario,seconds,finished,bank_start,bank_end,charges_per_min,income_limited,wasted_full,notes
```
- `charges_per_min`, `income_limited`, `wasted_full` come from the latest
  `HOLLOW_SAINT_STORM_INCOME` line in the log after the fight (it prints every
  30 s and at run end). Use the values from the line closest to the fight end.
- `notes`: at most 15 words (e.g. "orb latch carried boss", "cloud never had charges").

**`log-extract.txt`**: after each game session (before relaunching, because the
log is overwritten on launch), append only lines containing `HOLLOW_SAINT_FIGHT_`, `HOLLOW_SAINT_STORM_INCOME`,
`HOLLOW_SAINT_STORM_SUMMARY`, `Exception` or `HOLLOW_SAINT_` plus `ERROR`, prefixed
with a `## session <n> <build>` header. Log path:
`%APPDATA%\r2modmanPlus-local\RiskOfRain2\profiles\Hollow Saint Dev\BepInEx\LogOutput.log`.
Keep at most 400 lines total; if it grows past that, keep only the INCOME lines.

**`notes.md`**: one short paragraph per build (max 5 sentences), written at the
end of each run-order block: what felt strong, weak, broken.

No screenshots or videos unless something looks broken (max 5 images total,
named `bug-<n>.png`, each with a one-line note in notes.md).

## 6. Final summary (`summary.md`, max one page)

1. Table: build x item set, average seconds for S1/S2/S3.
2. Top 2 and bottom 2 builds overall, and the item set where each gap is biggest.
3. Charge economy: typical charges per minute per build; any build where the
   income guard often limited income or the bank sat full (flooding), or where
   the Special waited on charges (starved).
4. Bugs or errors seen (one line each).
5. Three concrete tuning suggestions with the number to change, for Stuart to review.
