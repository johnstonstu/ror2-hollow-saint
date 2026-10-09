# Hollowed Orb electrical presentation — local 1.3.0

Status: verified, staged in **Hollow Saint Dev**, privately packaged, and reviewed
by the requested **Astra high** auditor. Game exited normally and is closed.
Source remains local, uncommitted and unstaged; no upload or public release.

## Visible changes

The held and travelling ball now has six jagged surface paths with colored glow
and three short complementary forks. These use 15 fixed LineRenderers and reused
point storage, following the ball's position and empowered diameter. Surface
shapes refresh every 75 ms without consuming gameplay RNG. The shaded sphere
remains visible beneath the lightning. Two short trailing bolts accompany flight.
Existing chest/forearm energy feeds remain; the hidden cross-shell chord is removed.

Confirmed hits produce nine forked radial bolts, small sparks, two expanding
rings and a short local light. Visible bursts last about 0.2–0.3 seconds; owned
particles and transient objects expire afterward. Existing SpearStruck and
BoltImpact sounds supply impact and crackle layers, using their existing global
1/6-second and 0.22-second throttles and missing-bank fallback mappings.
Cleanup-only messages stop the corresponding flight and return before creating
any impact visuals or sounds.

Native comparisons revealed that the shared particle materials softened bolts
close to the opaque sphere. Orb-only cached clones harden `_InvFade` and disable
the soft-particle keyword while preserving normal depth testing. Installed
Cloud Remap materials report that the property is present. Final captures show
continuous bright bolts across the sphere's face, including Umbral and free
casts; shared kit materials are untouched. Partial sphere construction is also
destroyed if an optional effect fails, with operation context logged by its caller.

Damage, costs, aim, controls, bounce rules, animation timing, skill descriptions
and network payloads remain the prior refinement's behavior. Overhead gathering
and launching during Open Circuit retain their existing placement and free arms.

## Evidence

- `tools/doctor.ps1`: all required local inputs available, .NET 10.0.302.
- Final source/build/native-access verification: **35/35**, including 477 linked
  ChargedStorm assertions. Report:
  `artifacts/verification/20261007T210953-136559Z/results.json`.
  Runtime build: zero errors, 31 warnings from package/API compatibility and
  development fixture calls. A misplaced directive in an intermediate compile
  was corrected before staging this candidate.
- Final native trial `artifacts/orb-vfx03`: **30/30 assertions**, **59 real Orb
  damage reports**, **69 images**, zero fixture errors or pose-pop flags.
  Ten actual casts cover free and one-charge tiers, fully empowered casts on
  all six owned skins plus the profile's appended skin, and an overhead cast.
  Each preserves exact optional spending, native hit budget and finite cleanup.
- Representative gather/flight/impact frames inspected, including Umbral,
  Crimson, Solar, Verdigris, the house palette and overhead placement.
  Examples: `orb-vfx-tier-0-flight.png`, `orb-vfx-skin-4-gather_front.png`,
  `orb-vfx-skin-0-impact-2.png`, `orb-vfx-overhead-gather_side.png`.
- Final WASAPI loopback: **125.1 seconds**, peak **−9.4 dBFS**, zero overs,
  near-clips or failed sound posts. 33 SpearStruck and 26 BoltImpact posts;
  rapid impacts are intentionally throttled. See
  `artifacts/orb-vfx03/audio-analysis.txt` and `game-audio.wav`.
- First two comparisons `orb-vfx01`/`orb-vfx02` retain their earlier identities,
  30-pass traces and images. They exposed the front-surface fading corrected in
  the accepted third capture; they are not the final DLL's visual evidence.

## Astra high focused audit

The auditor independently inspected source, final native frames, verification
counts and audio analysis for the final DLL below. **No remaining actionable
P1/P2 findings.** The review confirmed outside-shell geometry, fixed renderer
count, bounded effect/partial-construction cleanup, silent cleanup messages,
audio throttles/fallbacks, isolated materials and unchanged gameplay/protocols.
The final comparison resolved the only presentation concern: faint front bolts.
This review is separate from the earlier broad 1.3 audit.

Evidence is solo mapped-input fixture execution, representative screenshots and
objective audio analysis, not subjective listening or ordinary-combat acceptance.
Multiplayer, physical inputs, missing-bank fault injection and sustained
performance remain outside this pass. Existing profile startup errors are
distinct from the zero-error fixture result.

## Candidate identity and rollback

Built, staged and packaged DLL SHA-256:
`8353BEE5310571AA489E3F95E1C06FEF48A7AE3B17EBE13753655DDE25D56D44`.

Language SHA-256:
`D98F39784F909D04ED3A69DF95B99DF62FCCC2FD8CBFB2AE3C9A38BB92BC675B`.

Pinned bundle SHA-256:
`7238F181B17B011A0BCC0BF13AB8C7D951CAA7930A24DB4F5B83F278BEA111B4`.

Private ZIP (eight allowlisted entries, 18,804,875 bytes):
`artifacts/candidates/20261007-141321-6479798/JohnstonStu-Hollow_Saint-1.3.0.zip`.
ZIP SHA-256:
`15B9299258C46C4E615F75AB2F3D25420FD0F881C4F610E233D841B2F0332825`.

Latest backup: `artifacts/foundation/profile-backup-20261007-141104`
(prior surface candidate `768F1277…`). To return to the pre-VFX refinement,
use `artifacts/foundation/profile-backup-20261007-135914`
(DLL `572275B5…`). With the game closed, restore the backed-up DLL and language
to the Hollow Saint Dev plugin folder. Bundle was unchanged. Earlier ZIPs and
captures remain preserved.
