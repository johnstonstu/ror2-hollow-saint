# 1.3 first-playtest Orb and Open Circuit refinement

Status: built, staged and locally packaged; final native regression passed.
This remains a local 1.3.0 candidate. Previous ZIPs, evidence and the original
Astra audit retain their own identities; that audit predates these changes.

The subsequent [Orb electrical presentation pass](orb-vfx-1.3-results.md)
records the current staged candidate. This document retains the prior gameplay
refinement's build identity and evidence.

## Player behavior

Hollowed Orb now works without Static. Short Secondary holds preserve the bank.
Hold past 0.50 seconds to gather empowering charges, then one every 0.30 seconds.
Release freezes the charge count and native aim. Tap windup is at least 0.18
seconds, followed by 0.06 seconds settling; recovery is 0.30 seconds. Cooldown
remains seven seconds after cast recovery. Utility cancels into its native
action without spending fuel. Empty throws consume native stock, not Static.

| Gathered charges | Orb diameter | First hit | Total hits |
|---|---|---|---|
| 0 | 0.6 m | 157.5% | 2 |
| 1 | 0.6 m | 225% | 3 |
| 3 | 0.8 m | 360% | 5 |
| 5 | 1.0 m | 495% | 7 |

Native aim assistance expands from 8 to 12 degrees and prefers reticle alignment
before distance, with visibility from the eye and muzzle. Finite launch/bounce
range, terrain collision, fresh-before-repeat priority, repeat attenuation and
proc limits are retained. Ball formation is substantial from the start; bright
feeds repeatedly flow from the chest and forearms into the ball. Normal casts
use the two-hand pose. During Open Circuit the ball gathers and launches above
the head, the Orb pose yields the arms, and native Primary remains available.
Expiry restores the normal forward pose and input ownership.

Open Circuit requires one charge, as Stuart selected. Hold Special to feed
stored charges into the crown at 0.12/0.30-second timing, then release. Counts
are committed once on the authenticated server before the authored unfold and
crown activation markers. Unused charges stay banked. Pre-commit cancellation
refunds native stock. A committed interruption still grants its paid window
when the body is alive.

| Gathered charges | Pulse interval at default settings | Density | Maximum visual arcs |
|---|---|---|---|
| 1 | 0.50 s | 1x | 6 |
| 3 | 0.333 s | 1.5x | 10 |
| 5 | 0.25 s | 2x | 14 |

Radius, duration, per-pulse damage, dwell zap and spear multiplier retain existing
settings. Extra pulses do not directly build Static; the original pulse cadence
continues generating it. Native stock recharge waits for the gather/release
state to finish, then remains held until the crown closes. This prevents an
early refill under heavy cooldown reduction.

No existing identifier, config key/default, Beat value, message ID/order or
authored asset was renamed. The existing request kind byte adds Circuit kind 2;
Cloud and Orb remain 0 and 1. A new hidden replicated buff carries Circuit's
paid strength, without a custom NetworkBehaviour. Four-language descriptions,
config option help, README and changelog reflect the optional Orb cost and new
Circuit charge-up. All participants must use the same candidate DLL/config.

## Evidence

- Linked production checks: 477 assertions covering empty-bank leases, free
  native admission, preserving a full bank on short casts, authenticated zero
  releases, stale/duplicate protection, frozen entry gains, optional tiers,
  aiming and obstructions, overhead Primary ownership, Circuit activation
  markers and baseline-only Static cadence. Explicit adapters do not certify
  Unity rendering or remote networking.
- [Native trial 01](../../artifacts/orb-circuit-refine01/trace.txt): **90/90
  assertions**, zero errors and motion-threshold events, 53 images, 151.8-second
  private solo trial. Actual health loss verifies free and empowered Orb tiers,
  exact spending and finite bounce budgets. Circuit produces 9/15/18 native
  damage reports in equal 1.5-second windows at 1/3/5 charges, with unchanged
  per-pulse damage. Native Primary hits during overhead Orb gathering. Moving
  and 50 m slow-flight cases pass; expiry preserves bank and restores forward
  geometry/control. Native tooltips format successfully.
- [Trial 01 audio](../../artifacts/orb-circuit-refine01/audio-analysis.txt):
  Speakers (High Definition Audio Device), peak -9.4 dBFS, no clipped or
  near-clipped samples and no failed sound posts. Device/output differs from
  the earlier overnight capture; absolute levels are not a controlled comparison.
- [Final Native verification](../../artifacts/verification/20261007T195401-383886Z/results.json)
  passes all 35 gates, including locked restore, runtime compilation, 477 linked
  ChargedStorm assertions, the existing Gaze suites, language/config compatibility
  and installed native access. Compilation has zero errors; existing obsolete
  API, MMHOOK compatibility and unavailable NuGet vulnerability-feed warnings
  are retained in the report.

Trial 01 tested DLL `B972CC2CA53052073F487EA04F9BD9EF03C7856A390A0AA08A5D3706A89D703A`.
The final revision additionally clarifies option help and prevents Circuit
recharge during gathering.

[Native trial 02](../../artifacts/orb-circuit-refine02/trace.txt) tests the final
DLL: **93/93 assertions**, zero errors and motion flags, 53 images and 148.7
seconds. In addition to all first-run criteria, it sets Open Circuit's native
recharge interval to 0.25 seconds and verifies at each 1/3/5-charge tier that
stock stays consumed and the recharge clock stays stopped throughout gathering.
The fixture restores its original interval afterward. Pulse report counts again
measure 9/15/18, and real Primary hits while the ball remains overhead. Its
[audio report](../../artifacts/orb-circuit-refine02/audio-analysis.txt) peaks at
-7.9 dBFS with no clipped/near-clipped samples or failed sound posts.

## Final local identity

The [ZIP](../../artifacts/candidates/20261007-125716-0459455/JohnstonStu-Hollow_Saint-1.3.0.zip)
is 18,801,955 bytes with eight allowed entries. Source/version, pinned bundle,
native access and shipped local-path gates pass. Built, staged and packaged DLLs
match the final native trial's identity:

- DLL SHA-256: `572275B5611ED5CC49CEEB1FD0830F062D6C583AE679B2579EC43F9CEA36DA81`.
- ZIP SHA-256: `73EA8DA1808930CB4203E045DB975C82DAB653D55B9FA74A05547D863474847A`.
- Language SHA-256: `D98F39784F909D04ED3A69DF95B99DF62FCCC2FD8CBFB2AE3C9A38BB92BC675B`.
- Bundle SHA-256: `7238F181B17B011A0BCC0BF13AB8C7D951CAA7930A24DB4F5B83F278BEA111B4`.
- Profile backup: `artifacts/foundation/profile-backup-20261007-125252`.
- Package gate output: `artifacts/charge-build-1.3/orb-circuit-package-final.txt`.

The private game trial quit after capture. The staged profile has no default
autopilot activation; the harness runs only with a process-specific environment.
All source changes remain local; previous candidates are retained.

## Remaining human acceptance

The game-owned harness uses mapped native actions and real server damage. It
does not prove physical/remapped controller ergonomics, two-client networking,
normal-camera combat feel or arbitrary item/mod combinations. Use the [manual
matrix](../manual-charge-build-1.3.md). Thundercloud's earlier nonreproduced
rejection remains documented in its historical acceptance record; its targeting
was not altered by this refinement. No public release or push is authorized.
