# Channel and Circuit feedback prototype from installed a74445a1

Source/build only; no installation or game interaction. Existing private version remains 1.2.0. Final exact-candidate Astra review and fresh game-closed confirmation are required before installation. Native appearance, input latency, perceived audio and camera feel remain pending.

## Gaze input and cleanup

Fresh mapped Special press cancels an active Gaze, including after duration extensions. The activation press is seeded as held, so keeping it down cannot self-cancel. Fresh mapped Utility cancels, restores the original slots and machine locks, then attempts the **actual equipped Utility** through native `GenericSkill.ExecuteIfReady`. No direct Arc Step construction, stock grant, cooldown reset or hardcoded keyboard/controller button. An unavailable Utility still exits Gaze but does not invent a dodge. A changed equipped definition, death, authority loss, disable or half-second transition timeout retires the queued attempt. Simultaneous fresh Special/Utility chooses Utility once.

Native authority sends the Crown state transition; the server's OnExit ends fuel work. Unlaunched entry fuel refunds and merges with reserve on living exits. Launched pulses are spent, stop on authoritative cancellation and do not refund. Original skill overrides, stock/recharge progress, gravity, armor and combat locks restore through existing teardown. Remote cancellation timing follows native state-transition latency; owner cancellation immediately closes local admission. Gaze damage, +2-second grants, 14-second cap, launch ramp, targeting and cadence are unchanged.

## Timer and pulse feedback

The compact bar starts full for the currently available duration. Its scale is snapshotted at first visible channel time and each **actual positive duration grant**, then stays fixed while counting down. A successful extension refills against newly available remaining time; the bright border/title briefly shows the actual grant (+2s or a final fraction). A pulse at the duration cap grants no fictitious time. Exact remaining seconds remain visible. Five small pips and a restrained brighter energy accent show successful launch tiers; there are no second/third bars or time-tick dividers. New casts reset the scale/tier and observers have no other-player meter.

Five finite electrical surges intensify at confirmed spend levels 1-5, gains 1.0/1.15/1.30/1.45/1.60. Level 5 caps further sound growth. Each is the established 0.24-second discharge waveform; no loop restarts or layered duplicate voices. The existing 0.24-second coalescer handles delayed acknowledgement clusters. Original 35 installed media entries and 114 routing objects are byte-identical. Five new media/events give 40 PCM sources/44 events; loops remain three, local stops four and owned busses zero. Peaks 6553/7536/8519/9502/10485 PCM16 leave about 9.9 dB headroom at the loudest source. No master/bus gain change. Missing custom bank keeps the indexed Captain tazer fallback without a synthetic volume ramp.

The launch cue adds a 0.12-second decaying native shake emitter only for the active local owner's current camera. Amplitude grows from 0.125 to 0.225 and coalesces at 0.24 seconds; accepted Launch sequence deduplication precedes it. It respects the mod Impact Feel toggle and the native camera's `UserProfile.screenShakeScale`. No aim ray, input, camera rotation or persistent camera override is written. Split local sessions suppress the cue to avoid the native global emitter affecting another local view. Rejected taps, intake cancellation, observers and late packets after local cancellation do not shake.

## Circuit bounded dwell zap and visual contract

Existing pulses retain their 0.5-second default cadence, 54% effective damage, Static weight, zero item-proc coefficient and actual core-centred spherical radius. Only actual pulse-hit hurtboxes admit dwell victims. Server fixed steps inspect the enemy's enabled hurtbox colliders against that same sphere using closest points. Three seconds inside earns **one 270% current body-damage zap per victim per crown**, normal crit and item proc coefficient 1. Attack speed does not shorten the threshold or add zaps. No sustained damage multiplier. Radius, expiry, team change, death, inactive collider and disallowed dash reset partial progress; departure/reentry cannot rearm a spent victim. Crown end, disable, source driver disable and stage changes clear bookkeeping.

At most 64 distinct health components are admitted per crown; spent entries remain as bounded tombstones. Victims with more than 32 hurtboxes use the first 32 for dwell eligibility, a conservative limit rather than extra hit volume. The zap runs inside the existing Storm damage guard, so it does not directly build Static or award Prayer fuel. Normal item callbacks remain; asynchronous item damage retains the game's usual consequences. No independent item coefficient nerf or custom proc cascade is added. Rejected damage attempts consume the one-shot opportunity but emit no confirmed target connection.

Public server-only `CircuitDwellHooks.Changed(owner, hurtBox, origin, targetPoint, progress, CircuitDwellBeat)` distinguishes Progress, Reset and Zap. Origin is actual `body.corePosition`; target point is a world snapshot. Progress emits only at eight quantized tiers. These hooks do not replicate by themselves and cosmetic subscriber failures are isolated. After a successful native zap attempt, `Beat.CircuitDwellZap` travels once through existing EffectManager, carrying the attacker's network reference and exact `DamageInfo.position`. Host/observers call Astra's `OpenCircuitDomeFx.ShowConfirmedStrike` once; no extra local playback or damage from that visual.

Astra commit e45e934a4ef61a36ca11807fa4ecf7a1d6161ecc supplies terrain contours, modest upward arcs and four pooled 0.20-second confirmed connections. World capsule checks suppress obstructed paths; total budget is 96 checks/frame plus at most four terrain probes. Budget/pool exhaustion, absent owner, Gaze crown ownership or invisible/inactive crown drops cosmetics without retrying gameplay. Native terrain collider behavior and copper mesh extents remain unaccepted.

## Targeted spear changes and composed damage

Raw configs/default migrations are unchanged. New prototype factors apply at runtime: direct spear damage uses the prior 0.9 factor times `(1 - 0.10 * charge)`; recharge uses raw seconds times 1.2; funded Prayer strike/splash use the prior 0.9 factor times 0.85. Tap damage and the independent conductor coefficients stay unchanged. Conductor normalization uses the same effective direct coefficient as launch, preventing the spear's extra reduction leaking into conductor damage. Unfunded Crown strike remains 900%; funded Crown still suppresses its separate free strike. Item proc coefficients and all Gaze/passive-pop coefficients remain unchanged.

| Default component | Installed a74445a1 | This prototype |
|---|---:|---:|
| Spear direct, tap/full | 315% / 1260% | 315% / 1134% |
| Enemy burst, tap/full (other victims) | 157.5% / 1260% | 157.5% / 1134% |
| Conductor per tick, tap/full | 18% / 31.5% | 18% / 31.5% |
| Funded strike / splash | 900% / 450% | 765% / 382.5% |
| Unfunded Crown strike / splash | 900% / 450% | 900% / 450% |
| Recharge per stock, after release | 5s | 6s |

A full-bank full-charge primary victim receives 2160% to 1899% combined direct+funded strike (about 12.1% less, before crit/armor/items). A neighbour receiving full burst, funded splash and all four full conductor ticks changes from 1836% to 1642.5% (about 10.5% less); that upper bound requires every tick to select and retain the neighbour. Conductor does not strike the lodged enemy itself. Hand full-charge 2s plus recharge changes the approximate no-item cycle from 7s to 8s; crown charge at 2.5x changes 5.8s to 6.8s. Attack speed and normal cooldown items continue to scale their existing parts. No special anti-item cooldown floor. Custom values stay saved: e.g. raw cooldown 2s becomes effective 2.4s without rewriting config.

## Focused later playtest

- Hold the cast Special press through windup; it must not cancel. Release/press again at baseline and after extensions: immediate local exit, no unlaunched spend, reserve retained, no late pulse impact after server teardown.
- Rebind Special/Utility on keyboard and physical controller. Utility cancels into the equipped ready skill once; empty stock exits without a free dash. Confirm stock/recharge progress and no duplicate activation after held input, death, disable, stage change or higher-priority override.
- Start with zero/partial/full fuel: bar starts full, drains visibly, refills on actual grants, shows partial final grant and five capped energy pips. Compare scale at 4s and 6s baselines. No false +2s at 14s cap; hidden HUD/observers show no stale UI.
- Compare pulse spends 1-5 and later: electrical energy audibly grows, no clipping or stacked repeated loop. Impact Feel off and native screen shake zero suppress kick. No crosshair aim or projectile direction changes. Verify observers and split view.
- Stay within Circuit for three seconds, leave before threshold, return, cross its vertical boundary, dash, and reopen: once per enemy per crown, no outside zaps, no timer progress retained across exit. Check armour/crit/item scaling and no direct self-refuel from the new zap.
- Check terrain slopes/ledges/ceilings and actual confirmed target tethers on host/observer; blocked strokes may disappear conservatively. Crowded targets respect the four-slot visual pool and bounded queries.
- At fixed stats/config compare the spear table and composed full-bank hit. Verify unfunded Crown bonus and conductor unchanged; normal proc/cooldown items still work. Gaze and shared Electrocute pop must match installed damage.

Source substitutes establish logic and bounds; no Unity rendering, perceived mix, real input device, live performance or multiplayer acceptance is claimed. Near-expiry remote duration acknowledgements remain an inherited limitation.
