# Charge kit: local prototype toward 1.3.0

This records the initial prototype plan. Stuart subsequently authorized a
complete local **1.3.0 candidate**, private staging and gameplay. Metadata now
identifies as 1.3.0. The [finish plan](charge-build-1.3-finish-plan.md) and final
acceptance record supersede the prototype's earlier staging/version boundary.
Public publishing remains separate.

## Balance hypothesis

Keep the five-charge bank and released Static/Electrocute generation initially.
Arc Bolt earns charges through regular combat; Open Circuit supports generation
without needing entry fuel; Gaze retains its existing entry/reserve rules.
Stormspear retains its released free hold-charge and full-bank Prayer upgrade.
Changing its resource model is a later comparison, not an implicit part of this
prototype.

The new options require one stored charge. Holding gathers entry charges one
at a time (0.12 s first, 0.30 s each following). Early release spends only what
was gathered. Cancellation before launch preserves the bank. One gathering
cast owns the bank at a time. New charges earned during gathering cannot enlarge
that cast's frozen entry allowance. Launched attacks retain their own snapshots.
Release freezes count/aim and lets the final intake finish for 0.26 s before the
commitment request. The 3 s reply timeout does not optimistically return stock
for an unacknowledged throw. Native Utility is queued only after cancellation.

| Proposal at default capacity | 1 charge | 3 charges | 5 charges |
|---|---:|---:|---:|
| Thundercloud radius | 16 m | 23 m | 30 m |
| Thundercloud damage per enemy, effective | 270% | 495% | 720% |
| Hollowed Orb diameter | 0.6 m | 0.8 m | 1.0 m |
| Orb damage per first victim hit, effective | 225% | 360% | 495% |
| Orb total hit budget (includes first hit) | 3 | 5 | 7 |

These are test values, not final balance. Both apply the existing 0.9 non-Gaze
damage multiplier once. Cloud aim reach is 80 m, one rolling sequence, 12 s
cooldown after the cast ends. Orb launch reach is 70 m, speed 32 m/s, bounce
reach 18 m, 7 s cooldown after release/recovery. Orb revisits lose 35% damage
per previous hit on that victim; per-victim first-hit proc 0.5, revisit proc 0.1.
Cloud proc 0.5. No inherited splash or healing. Direct spender damage runs in
the existing Storm damage scope so it cannot directly refill its own cost;
item-proc consequences retain native behavior.

Thundercloud covers a broad aimed area, grows with gathered charges, and hits
each eligible enemy once, ordered near to far from the Saint. A bounded target
list protects heavy fights. Empty-area releases cancel without spending fuel.
The prototype hop goes up/back during gathering, with native Utility cancellation.

Orb prefers unvisited enemies, then earlier victims when no fresh target is
reachable: A-B-C before A-B-A-B. It cannot bounce immediately into the victim
it just hit or invent extra solo-boss hits. World obstruction, no next target,
spent hit budget, owner loss or stage loss ends its flight. A genuine throw/miss
spends its committed charges. Growth after individual bounces is deferred.

## Work order

1. Verify the released/refactored baseline and define resource/target policies.
2. Implement shared server-owned charge claims and native skill admission.
3. Build the third Special with cloud ascent, area preview and descending bolts.
4. Build Hollowed Orb as a second Secondary with two-handed pose and visible flight.
5. Add tunable options, short translated descriptions and distinct icons.
6. Run source checks, build and installed-member verification; record evidence.
7. Hand off a local DLL plus a manual acceptance matrix. Stuart stages/launches
   separately and judges visuals, sound, controller feel and host/client behavior.

## Success criteria before tests

- Empty bank cannot activate either new option or consume native skill stock.
- Entry allowance is frozen; partial spending, cancellation, stale/duplicate
  release, simultaneous claims and new gains preserve exact charge accounting.
- Targets are distinct per cloud cast; increasing charges expands eligibility
  and visible coverage. Distance ordering and bounded schedules are deterministic.
- Orb chooses A-B-C when available, A-B-A when C is unavailable, never immediate
  A-A, and stops after its charge-dependent finite hit budget. Repeat attenuation
  is per victim and never changes other victims' first-hit damage.
- Server validates owner, active native state, cast identity, gathered count and
  finite aim. Clients cannot authorize damage or charge spending.
- Gravity, fall protection, poses, previews and held-input claims are released
  on every exit. Optional presentation failure cannot erase committed damage.
- Existing identity, family defaults/order, configuration migrations, Gaze and
  Stormspear behavior remain intact. New checks are registered centrally.

Pure policies prove accounting and selection, not Unity rendering or multiplayer
execution. Native scope proves installed-member access, not gameplay.

## Local build completed

Both options have runtime implementation, distinct loadout icons, translated
descriptions and tuning bindings. The final verification run passed all 35
registered checks/gates, including compilation and installed-member access.
See the [build record](charge-build-1.3-results.md) and
[manual acceptance matrix](../manual-charge-build-1.3.md). All gameplay and pose
acceptance was pending at this prototype milestone. Subsequent native trials,
the Astra audit and the candidate package are recorded separately. No public
1.2.5 or 1.3.0 release has been published by this work.
