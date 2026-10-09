# Hollow Saint 1.3.0 local candidate

Historical first candidate. Stuart's subsequent playtest changes optional Orb
fuel and Open Circuit charge-up; see the [refinement plan](orb-refinement-1.3-plan.md)
and its separate result record. The hashes and evidence below belong to the
original candidate and are retained unchanged.

Status: local 1.3.0 candidate built, staged and packaged; independent audit completed.
Private staging and game execution were authorized by Stuart on October 6,
2026. This record describes a local candidate, with public publication separate.

## Kit and balance

Thundercloud is the third Special selection. Hollowed Orb is the second
Secondary selection. Existing defaults, saved skill identities and authored
assets remain compatible. Each new skill needs one stored Static Charge. Hold
its mapped native action to gather charges individually, then release to spend
the gathered count once. Early release preserves leftovers; Utility cancels
gathering and requests the equipped native Utility. Empty cloud releases refund
stock and fuel; an Orb thrown into empty space spends its committed fuel.

| Charges | Thundercloud radius / damage per enemy | Hollowed Orb diameter / first hit / total hits |
|---|---|---|
| 1 | 16 m / 270% | 0.6 m / 225% / 3 |
| 3 | 23 m / 495% | 0.8 m / 360% / 5 |
| 5 | 30 m / 720% | 1.0 m / 495% / 7 |

Damage is the base coefficient before native armor, crits, vulnerability and
items. Cloud range is 80 m with one rolling strike per eligible enemy. Orb range
is 70 m, bounce reach 18 m, speed 32 m/s. Fresh victims precede revisits; two
victims can alternate A-B-A-B. Repeat damage on a victim retains 65% of its
previous hit; proc coefficients are 0.5 first hit and 0.1 repeats. Cloud proc is
0.5. Cooldowns are 12 s after the cloud sequence and 7 s after Orb recovery.
Open Circuit moves the actual Orb gather and muzzle above the head, without
adding free damage, charges or bounces. Direct spender damage cannot refill
its own Static cost. The original Stormspear and Gaze balance is retained.

The cloud crown ascends and returns, the Orb uses both hands, and effects use
the active skin palette. The existing soundbank supplies charge tiers, throw,
thunder and impact. New icons, four-language descriptions, bounded settings,
README and changelog accompany version 1.3.0.

## Evidence and corrections

The baseline is HEAD `6c48893cc7b3f566617c8a5563accc22773eb74d`, descended
from released 1.2.0 `e44d582964bf7e69fb6e410e9eeaf2748b443197`.

- Initial native trial exposed shader loading, release aim drift, and a visited
  victim intercepting travel to a fresh one. Those defects were corrected.
- [Native trial 04](../../artifacts/charge13-native04/trace.txt): 75 new-ability
  assertions passed. Actual native health loss matches the 1/3/5-charge damage
  values, first/repeat proc coefficients, finite hit budgets, exact resource
  spending, empty admission, Utility cancellation and overhead Circuit launch.
  Captures include all seven installed skins (six authored plus one extra-mod
  skin). Three hand-motion threshold flags are retained for presentation review.
  The appended old Gaze fixture produced seven failures, preserved in this trace.
- [Gaze trial 05](../../artifacts/charge13-gaze05/trace.txt): all 32 Gaze
  assertions passed after isolating the fixture correctly with invulnerable
  dummies, native fixed-tick taps and the actual base Orb bank. Errors and
  hand-motion flags are both zero. It checks tiered releases, partial bank
  restoration, Utility cancellation, held-on-entry safety, expiry and native
  Orb recharge restoration.
- [Native trial 04 audio](../../artifacts/charge13-native04/audio-analysis.txt):
  219.6 seconds of WASAPI loopback, peak -2.8 dBFS, no clipped/near-clipped
  samples or failed sound posts. [Gaze trial 05 audio](../../artifacts/charge13-gaze05/audio-analysis.txt)
  has no clipped samples; 40 samples exceed -1 dBFS with an extra-mod acid-larva
  impact the loudest event. These are measured event/mix checks, not listening
  acceptance of every device or item-heavy fight.

These trials used Risk of Rain 2 1.4.1 build 912 in the private Hollow Saint Dev
profile. Mapped input-bank actions drive real native states and server damage.
The profile includes other mods; unrelated startup MoreStats missing-assembly
and shader-key messages are recorded, not attributed to the new abilities.

## Audit and final artifact

The requested [Astra high audit](charge-build-1.3-astra-audit.md) found no P1
defect and two P2 presentation defects, both corrected. Orb flight presentation
now shares constant-speed motion and target anchors with authoritative travel,
including slow settings beyond four seconds. Cloud strikes retain the finite
cloud-to-victim bolt with impact-only native VFX; the extra above-cloud beam was
removed. Finite bolts were strengthened for readability. Server replies also
confirm the release's displayed charge count. Portuguese names were reconciled
after recording their complete inventory.

[Native10](../../artifacts/charge13-native10/trace.txt) passes all 17 focused
assertions with zero errors and motion flags. The retreating target receives one
actual Orb hit, and the 50 m target at the supported 10 m/s setting receives one
hit. Maximum sampled visual speeds are 32.00093 and 10.00087 m/s. Cloud casts at
1/3/5 charges each hit all three eligible native victims, spend exactly their
loaded fuel and return control. Logged centers, heights and visibility support
the observed aimed-area behavior. Its [audio measurement](../../artifacts/charge13-native10/audio-analysis.txt)
covers 82.4 seconds, peak -5.4 dBFS, zero clipped/near-clipped samples and zero
failed sound posts. This establishes measured sound operation, not subjective
listening acceptance.

Preserved native06–09 failures document additional fixture defects: moving all
dummies instead of only the test victim, immediate aim sampling before native
relocation settled, and accumulating offsets while moving from interpolated
positions. Those were corrected with a controlled absolute path, settled native
positions and live gather aim. Native09's five-charge cloud rejection was not
reproduced in native10; diagnostic logging was added without changing runtime
target selection. Its cause is not established by the passing repeat. Native07
also provides passing Circuit-expiry, skin, existing skill and Gaze assertions;
that whole trial remains recorded as failed because its Orb motion cases failed.

Final full Native verification passes all 35 gates:
`artifacts/verification/20261007T145730-414096Z/results.json`, including 396
linked-production ChargedStorm assertions, locked restore, runtime compilation
and installed member access. Build warnings include existing obsolete native
APIs, MMHOOK target compatibility and unavailable NuGet vulnerability lookup;
compilation has zero errors. Historical prototype records retain their original
1.2 metadata.

### Final repeat and package

[Native11](../../artifacts/charge13-native11/trace.txt) passes all **23** focused
assertions with **zero errors and motion flags**. It repeats the motion cases,
one- and three-charge clouds, then three consecutive five-charge clouds. Every
cloud preparation reports three in-range and visible targets, every release
records three native hits, and each spends the exact loaded charge count.
Its 95.0-second [audio capture](../../artifacts/charge13-native11/audio-analysis.txt)
peaks at -3.1 dBFS with zero clipped/near-clipped samples and failed posts.
The earlier native09 rejection remains an unexplained, nonreproduced observation;
these repeats establish the recorded conditions, not every terrain/aim condition.

The [final local ZIP](../../artifacts/candidates/20261007-075952-8966867/JohnstonStu-Hollow_Saint-1.3.0.zip)
contains exactly eight allowed files and is 18,797,075 bytes. Package gates,
version parity, native access, pinned bundle and shipped local-path checks pass.
The tested, built, staged and packaged DLL hashes match. The private profile has
no default autopilot activation; scripted runs activate only through their
process environment and quit afterward.

| Artifact | SHA-256 |
|---|---|
| ZIP | `D0648768705142EE0DD7829186B2B8918BB7975D4D104E36A4FA2AFE70592A56` |
| DLL | `34E43AE2A7B3A6295DDF1168A9AE598F3E912A0A402E7AC11F83033CAB647EC5` |
| Language | `FD3ED8C04F71D3FE577435577AE1102430F770F7F90F1608C58A20D214435861` |
| Released bundle | `7238F181B17B011A0BCC0BF13AB8C7D951CAA7930A24DB4F5B83F278BEA111B4` |

Local staging backup: `artifacts/foundation/profile-backup-20261007-075659`.
Package gate output: `artifacts/charge-build-1.3/final-package.txt`.
Implementation and documentation remain local working changes; nothing was
committed, pushed, tagged or publicly published.

## Evidence limits

Installed member scans prove API access. Linked source suites use explicit
native adapters. Solo trials prove the recorded native conditions and do not
certify real two-client networking, physical/remapped controllers, ordinary
combat feel or arbitrary mod/item combinations. The [human acceptance matrix](../manual-charge-build-1.3.md)
keeps those cases pending. No public release or push is part of this candidate.
