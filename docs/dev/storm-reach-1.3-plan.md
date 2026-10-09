# Thundercloud pacing and empowered Orb reach

Stuart requested a slower Thundercloud with more lightning action reaching its
targets, and a fully charged Orb able to bounce across a wider visible group.
Keep the candidate local and preserve earlier evidence.

Implementation choices: 0.9-second cloud ascent, first strike at 1.1 seconds,
2.4-second rolling target sweep, four cosmetic return strokes per struck target
spaced 0.55 seconds apart, then fade. One server damage hit per target remains.
Orb keeps its 18 m free/one-charge bounce range and gains 4.5 m per additional
charge: 27 m at three, 36 m at five. Existing base-range configuration remains;
an additive per-charge option defaults to 4.5 m. No additional damage/hit budget.

Success criteria, before tests:

- Cloud targets receive damage exactly once at their scheduled time, never early
  or again during return strokes; dead, occluded or departed victims stay excluded.
- Four visible cloud-to-target pulses fit before the cloud fades. Later flashes
  stop if a living target leaves the radius or moves behind world cover. Puffs form
  rounder billows; local VFX failures log and remain optional. All geometry uses
  existing skin palettes and network effect identities; no packet layout changes.
- Actual Orb server selection reaches a fresh enemy 25 m away only when empowered,
  while free/one-charge reach, walls, finite seven-hit budget, attenuation, harmless
  player relay and lifetime remain enforced. Custom base range still participates.
- Linked production tests check scheduling, timing, reach boundaries and actual
  server flight selection, with clear native-adapter limits.
- All descriptions and eight READMEs explain charge-dependent bounce reach.
- Doctor, source/build/native-access verification and focused private gameplay
  captures validate the rebuilt DLL. Native footage checks cloud pulse count,
  damage count, spending and the wider Orb chain; inspect frames and audio.
- The previously requested Astra high reviewer audits the final scoped changes.

Sequence: inspect current owners; implement pacing/presentation/reach; verify
linked behavior and build; stage with backup if the game is closed; capture
focused native cases; inspect evidence, refresh docs and hand off the local build.
