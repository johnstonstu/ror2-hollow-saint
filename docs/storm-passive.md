# Storm passive: Static, Electrocute, Thunderbolt

Status: approved direction (Stu, 2026-09-28). Target: v0.4.0 playtest build.
Replaces the current Discharge meter (own hits fill a meter, next hit bursts).

Working passive name: **Answered Prayer**. The Saint's lightning builds Static on enemies, overloaded enemies are Electrocuted, and every Electrocute is a prayer the storm counts. When enough are counted, the sky answers with a Thunderbolt.

## 1. The loop

1. **Static** builds on enemies from Hollow Saint hits. Enemies crackle harder as it rises.
2. At 100% Static the enemy is **Electrocuted**: stunned (or Shocked, for bosses), and it pops a small arc burst that spreads Static to neighbours.
3. Each Electrocute adds 1 **charge** to the Saint's core. The chest core brightens and one halo gap lights per 25%.
4. At full charge the storm answers: a **Thunderbolt** is called down on a strong enemy nearby, after a short telegraph.

Two readable beats: enemies popping (frequent, small) and the thunderbolt (rare, big).

## 2. Numbers (all config sliders, section "5. Storm")

| Parameter | Default | Notes |
|---|---|---|
| Static threshold | 25% of target max health | A hit worth 25% of max health fills Static from 0 to 100% |
| Minimum Static per full-proc hit | 6% | So bosses still build (v0.7: was 8%) |
| Static multiplier | x proc coefficient | Chain hops (0.5) build half; bolt and spear build full |
| Crit bonus | x1.5 | |
| Conductor-marked target | x1.25 (v0.7, was x1.5) | Gives the spear a job: choose what pops first |
| Static decay | starts 2 s after the last hit, drains 50%/s | |
| Electrocute stun | 1.5 s | Native RoR2 stun; bosses and stun-immune enemies skip it |
| Shocked (stun-immune) | +15% damage taken, 3 s (v0.7, was +20%) | Applied instead of the stun |
| Electrocute pop | 150% damage, 6 m, up to 2 enemies, proc 0.3 | Gives each 15% Static (cascade). v0.7: was 250%, 3 enemies, proc 0.5, 40% |
| Per-enemy immunity | 4 s after an Electrocute | No stunlock |
| Electrocute cap | 4 per second per Saint | Screen and performance guard |
| Charges per Thunderbolt | 6 | |
| Thunderbolt damage | 1000%, proc 1.0, can crit | Triggers on-hit items |
| Thunderbolt splash | 50% of strike damage, 3 m | |
| Thunderbolt target range | 30 m, needs line of sight | |
| Thunderbolt telegraph | 0.35 s | |
| Thunderbolt cooldown | 4 s minimum between strikes | Charge holds (core glows white) if no target |

Target choice: rank enemies in range by bosses, then elites, then current health, and pick randomly among the top 3. It should feel like the storm chose someone worth striking, not the closest wisp.

## 3. Scaling (why it stays relevant)

- **Damage items**: Static is based on hit size against the target's health, so bigger hits electrocute faster. The burst and Thunderbolt use the damage stat like every other skill.
- **Attack speed**: more hits, faster Static.
- **Proc coefficient**: Static inherits RoR2's own weighting for on-hit effects.
- **Crit**: more Static, and the Thunderbolt itself can crit.
- **On-hit items**: the Thunderbolt is a proc 1.0 hit (Ukulele, ATG, Kjaro/Runald bands, etc.). This is its late-game punch.

Expected damage share (single target, rough):

| Stage | Electrocute rate | Thunderbolt rate | Thunderbolt DPS | Arc Bolt DPS (ref) |
|---|---|---|---|---|
| Early | ~0.5/s | ~1 per 12 s | ~85%/s | ~200%/s + chains |
| Late | capped 4/s | capped 1 per 4 s | ~250%/s | ~600%/s at 3x AS |

The Thunderbolt stays an accent, never the main damage. Late game, trash dies before it electrocutes, so the mechanic shifts from crowd control to elite and boss pressure. That is intended (same as PoE shock).

## 4. Presentation

**Static on enemies**
- Crackle sparks on the enemy body, intensity in 4 tiers (25/50/75/100%). Driven by a replicated hidden buff (0 to 4 stacks) so every client sees it.
- No sound per hit (spam). A faint crackle tick at tier 3 and up, throttled.

**Electrocute**
- VFX: bright cyan flash on the body (small, max 1 m), 3 to 4 short arcs crawling over it, arcs to each pop target (staggered 0.04 s), sparks.
- SFX: `Play_captain_m2_tazer_impact`, throttled 0.08 s.
- Stunned enemies keep a thin crackle loop while stunned.

**Core charge (Saint)**
- Chest core brightness ramps with charge. Halo gaps light one per 25% (spec: hs-kit-v2-halo-gap-states.png).
- Charge tick: soft `Play_mage_m1_cast_lightning`. Full: `Play_railgunner_R_gun_ready` (existing MeterFull beat).

**Thunderbolt**
- Telegraph (0.35 s): crackling ground ring under the target shrinking inward, a flicker of light overhead, `Play_captain_shift_preImpact`.
- Strike: a thick vertical bolt from 25 m above with 2 to 3 forks, white-hot core, cyan glow (short 0.25 s life plus a 0.6 s afterglow), ground ring, sparks bursting outward and falling, one point light (range 12, 0.3 s), small camera shake near the target. SFX `Play_captain_shift_impact` layered with `Play_mage_R_lightningBlast`.
- Saint gesture: "Meter full flourish" on the Overlay layer as an arms-up call, only when UpperBody is idle (never interrupts skills).

## 5. Networking

- Static, Electrocute, charge and Thunderbolt logic run **server-side**.
- Replicated state lives in buffs: `bdHsStatic` (hidden, 0 to 4 tier stacks on the enemy), `bdHsShocked` (visible debuff), `bdHsStormCharge` (Saint, 0 to 6, replaces the Discharge charge buff).
- Visuals go through the existing KitFx server beats (EffectManager), so all clients see them.
- Static on an enemy is shared; charge goes to the Saint whose hit triggered the Electrocute.

## 6. Build plan

**Phase 1: data and server logic**
1. `StormTuning` fields + config section "5. Storm" (replace the Discharge section; keep a defaults migration).
2. `StaticServer`: hooks `GlobalEventManager.onServerDamageDealt`. Filters: attacker is a Hollow Saint, damage > 0, not a Storm damage source (reentrancy guard). Per-victim state in a dictionary keyed by HealthComponent (value, last hit time, immune until), cleaned on death. Sets the tier buff.
3. Decay tick in a server FixedUpdate driver.
4. `Electrocute(victim, attacker)`: stun via `SetStateOnHurt.SetStun` when `canBeStunned`, else Shocked buff; pop via `KitUtil.CappedBlast`-style capped search; per-Saint rate cap; add charge.
5. Shocked amplification in the existing `TakeDamageProcess` hook (ConductorMarkServer).
6. `ThunderboltDriver` on the Saint body: server tick; when charge is full and the cooldown is ready, pick a target, send the telegraph beat, strike after 0.35 s (target may have died: re-pick or refund).
7. Remove the old Discharge meter "next hit bursts" path (DischargeEmitter), keep its VFX beats where useful.

**Phase 2: presentation**
8. New beats: `StaticTier`, `Electrocute`, `ElectrocuteArc`, `ThunderTelegraph`, `ThunderStrike`, `ChargeTick`. Sound table entries with throttles.
9. Enemy crackle component: added client-side when the tier buff appears (a lightweight tracker driven from the buff count), removed at 0.
10. Core and halo gap presentation reads the new charge buff.
11. Thunderbolt gesture on the Overlay layer.

**Phase 3: text and UI**
12. Passive name, description and keywords: Static, Electrocute, Shocked, Thunderbolt. Select-screen description tips updated.
13. Buff icons for Shocked and Storm charge (reuse/tint existing icon pipeline in tools/icons/make_icons.py).

**Phase 4: verify**
14. Compile, IL access check, smoke load.
15. Playtest checklist: pack of lemurians (cascade), single elite, boss (no stun, Shocked applies, Thunderbolt prefers boss), Open Circuit in a group, attack-speed items late run (caps hold, no screen wash), multiplayer if possible.
16. Log lines: `HOLLOW_SAINT_EVENT ELECTROCUTE`, `THUNDERBOLT target=... damage=...`, first N only.

## 7. Open questions

- Does Open Circuit feel too strong once pulses build Static on everything? Tune pulse Static separately if needed (slider).
- Should allies' hits build Static? Default no (Saint hits only).
- Passive name: "Answered Prayer" is a working title.
