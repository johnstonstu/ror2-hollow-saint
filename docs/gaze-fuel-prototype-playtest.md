# Gaze orb-fuel prototype: candidate playtest

Source base: published 1.1.1, commit `20a821abb9f0f254d966536da82052007e734c1e`.
Candidate branch: `codex/gaze-fuel-prototype`. This is a private solo prototype;
the package version remains 1.1.1 while its behavior is evaluated.

## Intended behavior

Normal Gaze keeps its existing timing, beam damage, targeting and charge gains.
At cast entry, existing charges become finite fuel. Up to five intake/pulse groups
consume that entry fuel; newly earned charges remain visibly separate for the
next move. The default capacity stays five; supported configuration is 2–20.
Each group contributes weak individual target strikes, with at most eight
distinct targets. A full bank budgets 2.5 additional damage coefficient per
eligible target over the whole cast, with no extra finale attack, item proc,
Static gain or strike-generated charge gain.

On every channel exit, pending fueled damage stops immediately. Launched fuel
stays spent; unlaunched fuel and accepted new charges return on ordinary living
exits. Death or controller disable discards the remaining bank. A full returned
bank remains held until an explicit future use; a partial
bank continues normal passive progression. Lowering the configured capacity
does not discard already accepted charges: the cast uses a frozen capacity,
and a subsequent cast can accommodate its retained bank, bounded at twenty.
Whether outside-cast banking should replace the passive more broadly remains
a separate design decision.

Holding a full returned bank changes Answered Prayer in this prototype: its
automatic bolt stays suppressed until the next explicit Gaze claim or meter
consumption. This is provisional playtest behavior, not a permanent passive
decision. Ordinary outside-cast charging and partial returned banks retain the
existing passive behavior.

Ground radius has one source, `GazeFuelSchedule.SpreadRadius`: an 8 metre base
multiplied by the dimensionless reach interpolation 0.4–1.6 over the four-second
beam. Its envelope is 3.2–12.8 metres. The five scheduled launches occur earlier
than the beam end, giving nominal radii 4.328, 6.008, 7.688, 9.368 and 11.048
metres before fixed-step timing. The same frozen radius is sent to presentation
and used for target selection; terrain routes, walls, gaps and ledges can reduce
the actual footprint. Moving selected targets are checked again at resolution:
ground strikes require their current terrain route and position within the
frozen radius, and the primary strike requires unobstructed line of sight within
the beam range. Decorative spread does not add an area damage hit.

## Later playtest

Only install and launch after a separate authorized handoff with the game closed.

- Compare zero-charge Gaze with published 1.1.1: timing, core hits, forks,
  cancellation, movement and resource generation should match.
- Try one, partial and full entry banks. Watch each orb curve into the back of
  the crown, the crown brighten, a widening pulse travel down the beam, then
  spread on real terrain. Weak target strikes should be individually readable.
- Earn charges during the beam. Their separate reserve must survive the cast
  and must never feed another pulse in that same cast.
- Cancel during intake, travel and ground spread, using recast, Arc Step and
  controller cancel; also test interruption, death and stage transition.
  No fueled hit or lingering presentation should occur after exit.
- Aim at empty ground, walls, slopes, ledges, airborne enemies and crowded
  enemies. Terrain visuals must not invent a floor or imply hits beyond the
  actual selected targets. Confirm no large extra finale or automatic bolt.
- Test capacities 2, 5, 6 and 20, including changing capacity during a cast.
  Verify bounded pulse count, conserved charges and stable visual intensity.
- Compare host and remote observer, including rapid consecutive casts and
  lag. Late messages must not revive ended casts or duplicate strikes/orbs.
- Check multiple Saints channeling together for readability and performance.

Source checks and compilation cannot establish live appearance, multiplayer
delivery timing, terrain quality or gameplay feel. Those remain playtest gates.

## Integration status

The mechanics ledger, schedule and packet-retirement tests pass 1,591 assertions.
Existing primary defaults (12 cases), spear defaults (20 cases), crown gesture
flow (27 checks), language (33 tokens) and animation rules also pass. The initial
combined Release build has zero errors and 19 existing warnings.

The presentation now carries separate frozen beam and ground anchors, and uses
the public Unity component transform. The combined DLL passes actual-game API
accessibility validation. The private package is a solo playtest candidate;
live appearance and gameplay feel remain unverified.

Presentation traffic uses reliable UNet messages and synchronized server time
for event age. Spawn-lag buffering is bounded to sixteen owners, sixty-four
packets per owner and two seconds. Mid-cast joining observers have no snapshot.
Remote authority cancellation reasons may be reported as `Interrupted` on the
server, with the same refund and cancellation behavior.
Until a bounded mid-cast snapshot is implemented and observer delivery is
playtested, this candidate is limited to private solo evaluation. The observer
checks above describe later multiplayer acceptance work.
