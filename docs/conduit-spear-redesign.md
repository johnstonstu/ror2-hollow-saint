# Conduit Spear Redesign

## Status

Implemented in v0.6.0 (spear-anchor | recall-throw). Not yet playtested. The decisions below are Stu's; the "Built" section describes what the code does. The sections after it are the original proposal, kept for context.

## Decisions (settled)

1. **Lifetime:** planted until recast. Auto-cleanup only on owner death, owner body destroyed or replaced, stage change, disconnect, or the owner moving more than the leash distance (default 100 m) away.
2. **Enemy hit:** the throw still deals 450% and applies the Conductor Mark, then the spear sticks in that enemy and rides it (pulses and radius centre on it). When the host dies, the spear drops to the ground below its last position (raycast down, fallback to the last position) and stays planted.
3. **World hit:** plants at the impact point, leaning into the surface along the throw direction. Walls, steep slopes (normal.y < 0.5) and ceilings drop it to the ground below. A total miss drops to the ground after 80 m of flight (or 80% of the leash), or despawns if there is no ground within 60 m.
4. **Pulse:** every 1.5 s, 50% damage to enemies within 10 m of the anchor (BlastAttack, no falloff, proc 0.3, DamageSource.Secondary). It builds Static through the normal StormServer damage hook, using its own Static weight (default 0.5, proc-equivalent) instead of the item proc.
5. **Primary spread:** a confirmed Arc Bolt hit on an enemy inside the owner's anchor radius is conducted through the spear to up to 3 other enemies in the radius (60% damage each, proc 0.5, crit follows the bolt). It runs once per bolt, for the primary victim only; chain hops never trigger it. Spread targets join the bolt's hit set, so the normal 3-hop chain then skips them (each enemy is hit at most once per bolt). Spread damage never re-enters the Arc Bolt path, so it cannot cascade.
6. **Recast:** with a spear planted, the server recalls it at the start of the cast; it flies back to the hand over 0.3 s, and the throw gesture and release are delayed by the same amount on every machine. First cast (no spear out) throws normally. One spear per player; cooldown unchanged.

## Built (v0.6.0)

- `SpearDischarge/ConduitSpearAnchor.cs` (new): lives on the spear projectile prefab. The projectile itself survives impact and becomes the anchor, so its existing ProjectileNetworkTransform keeps the lance ghost in place on every client with no custom networking. Server-only phases: Flying, Planted, Stuck (riding an enemy hurtbox), Recalling. One anchor per owner in a server registry; the owner carries a hidden replicated buff (`bdHsSpearPlanted`) while an anchor is registered so the casting client knows to wait for the recall.
- `ConductorMark.cs`: `ConduitSpearImpact` no longer destroys the projectile; it hands off to the anchor (stick, plant on world, or drop to ground). Impact resolution is server only. The template's flight sound loop is cleared.
- `ConduitSpearState.cs`: recall delay before the throw gesture and release.
- `ArcBoltChain.cs`: calls `ConduitSpearAnchor.TrySpread` once per confirmed bolt hit, before the chain.
- `StormServer.cs`: pulse damage uses `ConduitSpearPulseStaticWeight` for Static.
- VFX beats (networked, owner skin palette): `SpearPulse` (faint radius ring plus a small crackle at the spear butt; sound only when the pulse hit something, throttled 1 s), `SpearPulseArc` (thin arc to up to 3 pulse targets), `SpearConduct` (bolt hit to spear flare), `SpearSpread` (thick branched arc spear to target), `SpearRecall` (arc from spear to hand).
- Config sliders under "2. Conduit Spear": Anchor radius (10), Pulse interval (1.5), Pulse damage (0.5), Pulse proc coefficient (0.3), Pulse Static weight (0.5), Spread targets (3), Spread damage (0.6), Recall duration (0.3), Leash distance (100).

## Known limitations

- Pulses and spread are gated by a hard radius with no line of sight check.
- If the cast is interrupted during the recall (stun, death), the spear is already recalled and no throw happens; the cooldown is spent.
- Clients see the planted spear through the projectile's network transform, so a spear riding a fast enemy lags slightly behind it on remote clients.
- The skill description numbers are fixed text; they do not follow config changes.

## Core idea

Redesign Conduit Spear as a reusable lightning anchor. The player throws the spear to a location, where it sticks in the ground and remains active through its cooldown. While planted, it periodically shocks nearby enemies and helps the player's primary attacks spread through enemies in its area. When the cooldown is ready and the player casts Conduit Spear again, the spear returns from its current location to the player's hand, then is thrown toward the new target.

The intended combat rhythm is:

**Throw and plant → fight around the anchor → recall and redeploy.**

## Proposed gameplay flow

1. **Initial throw:** Conduit Spear is thrown using its existing ability input and targeting behavior as a starting point.
2. **Plant:** On reaching its destination, the spear embeds in the ground and becomes the center of its active area.
3. **Passive pulses:** At a regular interval, the planted spear shocks nearby enemies. These pulses provide steady area pressure and can apply or interact with the existing Conductor/Static mechanics.
4. **Attack-triggered spread:** When the player's primary attack hits an enemy within the spear's radius, lightning arcs from that hit to other valid enemies in the area. Each enemy can be hit at most once by a given primary attack event.
5. **Recall and redeploy:** After the cooldown is ready, the next cast first pulls the planted spear back to the player's hand, then throws it toward the newly selected location. The player chooses when to move the anchor by choosing when to cast again.

## Starting constraints

- Keep the existing Conduit Spear input and cooldown framework where practical.
- Allow one active planted spear per player. A recast relocates that spear rather than creating another one.
- Keep the spear planted while the cooldown is running; cooldown readiness enables the recall-and-rethrow cast.
- Treat the periodic pulse as supporting damage and the attack-triggered spread as the main interactive payoff.
- Bound chain targets and prevent chain hits from recursively creating further full chains.
- Preserve multiplayer ownership so each player's spear and attack-triggered arcs affect the correct targets.

## Feedback and readability

- Show the spear visibly embedded at its destination.
- Give its effective radius a readable cue, such as a subtle ground mark or brief ring when it pulses.
- Make each passive pulse visually distinct from attack-triggered chains.
- On recast, show the spear traveling back to the player before its new throw, so the relocation is understandable.
- Use clear impact, pulse, recall, and redeploy audio cues without making periodic pulses noisy during longer fights.

## Implementation plan

1. Inspect the current Conduit Spear projectile, impact handling, Conductor Mark behavior, cooldown/state flow, and multiplayer ownership.
2. Define planting rules: valid surfaces, enemy hits, slopes, and fallback behavior if the spear cannot find a suitable ground point.
3. Add a planted state with duration/lifetime tied to the ability's cooldown and reliable cleanup on death, disconnect, or body replacement.
4. Add a server-authoritative periodic pulse with configurable radius, interval, damage, and status effect behavior.
5. Connect primary attack hits within the spear's radius to a bounded lightning chain. Track targets per attack event to prevent duplicate hits and avoid recursive proc chains.
6. Add recast behavior: recall the existing planted spear to the owner, play the return motion, then launch it toward the new aim point.
7. Add planted, pulse, chain, recall, and redeploy VFX/SFX feedback.
8. Tune the radius, pulse interval, duration, damage, target cap, and proc coefficient in gameplay, including multiplayer checks.

## Original open questions (answered above)

- Does the spear remain planted indefinitely until recast, or expire after a fixed duration?
- What happens if the spear initially hits an enemy: plant at the impact point, stick in the enemy, or continue to a nearby surface?
- Should passive pulses deal direct damage, apply Static/Conductor, or do both?
- Should every primary attack type participate, or only specific primary skills/projectiles?
- Should attack spread trigger when the primary attack hits an enemy inside the radius (recommended starting point), or require the attack to hit the spear itself?
- What radius, pulse interval, chain target cap, and proc behavior best fit the intended power level?
- Should recall be immediate on recast, or have a short visible return travel time before the new throw begins?

## Success criteria

- The spear remains at its planted location throughout the cooldown and is visibly readable in combat.
- Nearby enemies receive periodic shocks while the spear is planted.
- Primary attacks against enemies in the spear's area spread lightning to other valid enemies without repeatedly hitting the same target from one attack or causing runaway chains.
- Casting again after cooldown visibly recalls the old spear and throws it to the new location.
- The behavior remains attributable to the owning player and cleans up correctly when that player or spear is removed.

## Extra secondary charges and line of sight

One spear only. Each bonus charge (secondary maxStock - 1, read live from the owner SkillLocator) adds 25% of base radius (cap 2.0x), +1 spread target (cap +4) and 10% lance scale (cap 1.5x). Pulse damage stays flat. Clients derive the scale from the same replicated inventory via CharacterBody stats. The pulse (BlastAttack NearestHit, centred near the shaft top) and spread targets (linecast from the shaft top against world geometry) no longer hit through walls.
