# Thundercloud pacing and empowered Orb reach — local 1.3 refinement

October 7, 2026 (Pacific time; verification timestamps use October 8 UTC).
This continues the [early-game balance candidate](early-balance-1.3-results.md).
Nothing was published or pushed.

## Result

Thundercloud takes 0.9 seconds to rise, begins striking at 1.1 seconds and rolls
across a pack over 2.4 seconds. Each struck target receives four branching visual
strokes spaced 0.55 seconds apart, with exactly one server damage hit. Cloud launch
through fade is 3.842 seconds for a single target and 6.242 seconds for a pack.
Button release adds the existing 0.26-second release delay before launch, plus
frame/network delay.
Cloud billows are rounder and taller. Primary becomes available after release;
Special's recharge waits for the longer sequence to finish. That longer next-cast
cycle and later damage arrival are deliberate timing changes.

Orb bounce reach is 18 m free/one charge, 27 m at three charges and 36 m at five.
The new **Bounce range per extra charge** option defaults to 4.5 m; setting it to
zero restores constant reach. The existing 18 m base setting remains intact.
Initial range, speed, damage, finite hit budget, repeat attenuation, resource cost
and harmless player relay rules are unchanged by this refinement. The 8 m foot
proximity rule remains separate from charge-dependent core-flight reach.

Later cloud strokes recheck shared radius/world visibility and stop when a
resolved victim dies or leaves coverage. Existing effect payload fields carry
radius and actual cloud height; no custom message layout or content identity was
changed. An already spawned bolt may follow a departing target for its remaining
lifetime, at most 0.392 seconds. The helper tests establish the new gate; this
capture used stationary targets and does not establish moving-client acceptance.

## Verification and native evidence

- Doctor completed; the legacy Unity preview remains explicitly unavailable.
- [Native verification](../../artifacts/verification/20261008T022148-902526Z/results.json):
  35/35 registered gates passed, including 884 linked ChargedStorm assertions.
  Build/native access checks and linked adapters are distinct from gameplay.
- [Focused capture trace](../../artifacts/storm-reach-native02/trace.txt):
  33 behavioral assertions passed, zero failed, zero pose-pop flags.
- One-charge cloud: one 40.5-damage report, four visual strokes, correct fuel.
  Five-charge cloud: six distinct 108-damage reports, four strokes per target,
  correct fuel. These are itemless fixture damage values at body damage 15.
- Both wide-Orb trials used an actual clear launch and swept world lane with a
  26.81873 m core gap. The free cast hit its aimed victim once and preserved five
  charges. The full cast spent five and completed A–B–A–B–A–B–A. Its damage was
  74.25, 74.25, 48.2625, 48.2625, 31.37063, 31.37063, 20.39091, with unchanged
  fresh/repeat procs. Seven is the original full-charge hit budget.
- The trace retains **49 Windows ClipCursor access-denied errors**, all with the
  same cursor-confinement message. No other captured error category occurred.
  These warnings are not silently filtered or reported as zero errors. The
  scripted ability assertions and game-window/audio capture still completed.
- [Audio analysis](../../artifacts/storm-reach-native02/audio-analysis.json):
  105.30352 seconds, peak −1.91558 dBFS, zero overs, near-clipping samples or
  failed sound posts; 274 events. Numeric analysis does not replace listening.

The [first focused capture](../../artifacts/storm-reach-native01/trace.txt) is
retained as a partial/rejected result: cloud assertions passed, but both Orb
fixtures failed their world-clearance precondition and the full cast ended after
one hit. The corrected fixture searches for a genuinely clear lane; production
collision rules were not weakened. An initial staging command selected the
previous Release DLL; this was caught before gameplay, then explicitly rebuilt
and checked. The accepted capture's prelaunch identity matches the final build.

## Review media and audit

Open the [four-clip review](../../artifacts/storm-reach-review-1.3/index.html),
[combined video](../../artifacts/storm-reach-review-1.3/storm-and-reach-1.3.mp4)
(36.954687 seconds), or [full native footage](../../artifacts/storm-reach-native02/raw.mp4).
The individual clips retain stereo AAC audio, 1280×720 at 30 fps; the combined
video decodes without errors. Inspected actual frame sequences show target-reaching
returns and the Orb crossing the wide pair. This is controlled native footage,
not a live-enemy balance trial. The rounded cloud still reads as smooth solid
lobes and occupies much of the upper view; softer smoke and occlusion polish
remain possible. The earlier complete-kit showcase and media are preserved.

The root READMEs now use a [fresh cloud animation](../media/thundercloud-return-13.webp).
All eight root/package READMEs and four in-game languages describe the revised
reach and distinguish visual returns from damage hits. Package public media
references remain historical until an explicitly approved release.

The user-requested [Astra high scoped audit](../../artifacts/charge-build-1.3/astra-storm-reach-audit.md)
reviews source, assertions, native trace, frames, audio and artifact identity.
The earlier whole-kit audit remains separate. Physical keyboard/controller use,
remote clients, multiplayer, moving-target combat and dense late-game VFX load
remain manual checks. The earlier nonblocking spear pose diagnostic is retained
in the earlier results; it was not revalidated by these four clips.

## Exact local candidate

The game is closed. The **Hollow Saint Dev** profile contains the rebuilt DLL and
language, backed up at `artifacts/foundation/profile-backup-20261007-192302`.
The verification runtime, Release output, staged profile, prelaunch `build.json`
and private package agree:

| Asset | SHA-256 |
|---|---|
| DLL | `058FFFF1E30296D116F62A244E0919F8CD1F18280A21C7507E3FFFAFC24C62FC` |
| Language | `0019C65AFBF6558B7507F79149FDAC6ACCD56D00731B18A21B82A62DDD01C55A` |
| Unchanged bundle | `7238F181B17B011A0BCC0BF13AB8C7D951CAA7930A24DB4F5B83F278BEA111B4` |

[Private 1.3.0 package](../../artifacts/candidates/20261007-192646-1818306/JohnstonStu-Hollow_Saint-1.3.0.zip):
18,819,463 bytes, eight allowlisted entries, SHA-256
`B6EADF5D1C8CA956668BF357FC71BD83238AAD869CE37D73C2994E78350D04A2`.
Its DLL/language/bundle and README/changelog were read back and compared with
their intended inputs. See [package identity](../../artifacts/charge-build-1.3/storm-reach-package-identity.json)
and [handoff](../../artifacts/charge-build-1.3/storm-reach-handoff.json).
