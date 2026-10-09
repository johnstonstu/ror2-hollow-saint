# 1.3 independent audit brief

**Historical prototype brief.** Its mandatory Orb charge, fixed bounce range and
lone-target behavior were superseded. Start a new audit with the
[current 1.3 handoff](audit-handoff-1.3.md); retain this file as historical evidence.

Requested reviewer: Astra, high reasoning effort. Review local changes against
HEAD `6c48893cc7b3f566617c8a5563accc22773eb74d`, descended from released 1.2.0
`e44d582964bf7e69fb6e410e9eeaf2748b443197`. Do not merge divergent v09.

## Design contract

- Thundercloud is the third Special selection: gather existing bank charges into
  the crown, release an aimed broad area, crown ascends, one near-to-far rolling
  sequence strikes each eligible enemy once, then cloud fades and crown returns.
- Hollowed Orb is the second Secondary: both-handed gather and forward throw,
  substantial minimum size, growth with charges, fresh A-B-C before revisits,
  A-B-A-B fallback when only two enemies are available, finite total hits.
- During Open Circuit, Orb gathers and launches above the head like the spear.
  Posture changes only: no free fuel, damage or bounce bonus.
- Both require one stored charge, gather individually, freeze entry allowance,
  spend once on authenticated server commitment, and retain unspent charges.
  Utility cancels into its native skill; empty cloud cancels/refunds.
- Preserve existing default selections/identities, configuration migrations,
  Beat enum values, existing packets and authored animation layers/timing.

## Starting balance

| Charges | Cloud radius / damage per enemy | Orb diameter / first hit / total hits |
|---|---|---|
| 1 | 16 m / 270% | 0.6 m / 225% / 3 |
| 3 | 23 m / 495% | 0.8 m / 360% / 5 |
| 5 | 30 m / 720% | 1.0 m / 495% / 7 |

Cloud range 80 m, cooldown 12 s after its sequence. Orb launch range 70 m,
bounce range 18 m, speed 32 m/s, cooldown 7 s after release recovery. Repeat
hits retain 65% of the previous hit on that victim. First-hit proc 0.5,
repeat proc 0.1; cloud proc 0.5. Existing non-Gaze 0.9 multiplier applies once.
The Orb is a spread-out crowd spender; lone enemies get one hit. Stormspear
and Gaze remain available for focused threats. Direct spender damage does not
build Static; native item events remain invoked.

## Audit targets and evidence

Inspect `FoundationKit/ChargedStorm`, `Thundercloud`, `HollowedOrb`, modified
meter/admission/Gaze overrides, `Content/KitRegistration`, configuration,
localization, icons, README/changelog/manifest and development trials. Link any
findings to precise source lines and explain user-visible failure conditions.

- `artifacts/charge13-native01`: first solo run found shader failure, short
  release aim drift, and visited enemies intercepting travel to a fresh target.
- `artifacts/charge13-native02`: 75 behavioral assertions passed; visible cloud
  still inadequate and Orb too flat. Loopback audio peaked at -2.5 dBFS, no
  clipping or failed posts. Do not treat these visuals as final.
- `artifacts/charge13-native03`: 75 assertions passed; shaded Orb, root scaling
  correction, smoother pose. Cloud smoke still lacked a readable silhouette.
- `artifacts/charge13-native04`: all 75 new-ability assertions passed with the
  native shaded cloud volume. Its appended Gaze fixture failed seven assertions:
  frame-timed taps crossed the opening-charge threshold, vulnerable dummies
  contaminated resource isolation, and a temporary external Orb override used
  a different recharge path from the player's base loadout. Preserve the failures.
  Audio peaked at -2.8 dBFS, no clipping, near-clipping or failed posts.
- `artifacts/charge13-gaze05`: corrected fixture uses native fixed-tick taps,
  invulnerable dummies and Orb's base skill bank. All 32 Gaze release/recharge
  assertions passed, errors=0, pops=0. The recording has no clipped samples;
  40 samples exceed -1 dBFS, with the loudest event an extra-mod acid-larva impact.
- `tools/ChargedStormChecks`: linked production bank, transport, admission,
  server collision/flight, cloud schedule and target rules with explicit native
  adapters. Other registered suites cover configuration and legacy kit behavior.

Solo trials exercise real native mapped actions, health loss and server effects.
They do not certify physical-controller controls, real remote networking,
ordinary combat feel, or arbitrary mod/item combinations. Native member scans
prove legal installed API access, not gameplay. The desktop inspection tool
stalled on app-access approval; screenshots are game-rendered cameras.

Keep the audit read-only except a dedicated audit report in `docs/dev/`.
Do not publish, push, launch another game, change a profile or alter authored
assets. Public release remains separately authorized. Preserve unrelated
untracked art/doc files. Report P1/P2 defects separately from test limitations
and optional tuning; identify any evidence that is insufficient or misleading.
