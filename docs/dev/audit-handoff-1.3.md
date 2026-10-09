# Hollow Saint 1.3 — independent audit handoff

Prepared October 7, 2026, for Stuart's next reviewing agent. This is the current
entry point; earlier plans and audit briefs describe earlier candidates.

## Start here

> **Update, October 7 evening:** a storm flow pass (Static priming from Orb/cloud hits, cloud Shock, 0.25 s gather cadence, cloud silhouette, rewritten text) now sits on top of this candidate and is staged. See [storm flow results](storm-flow-1.3-results.md); hashes below describe the earlier frozen candidate.

Review the **current working tree**, including untracked source, tests, icons,
documentation and media. Branch is `main`, HEAD is
`6c48893cc7b3f566617c8a5563accc22773eb74d`. The 1.3 implementation is **uncommitted**;
`git diff HEAD` alone omits important new files. Released baseline is `v1.2.0`,
`e44d582964bf7e69fb6e410e9eeaf2748b443197`. Do not merge divergent `v09`.

Read [AGENTS.md](../../AGENTS.md), [development map](README.md),
[architecture](../kit-architecture.md), then this contract and the evidence below.
Keep the audit independent: reproduce claims and inspect production owners rather
than accepting previous audit recommendations as proof. Write findings in a new
report, with severity, exact source location, concrete trigger, player-visible
effect and reproduction/test evidence. Separate defects from tuning preferences
and missing acceptance evidence. Preserve prior reports and failed captures.

This handoff itself changes documentation only. No new audit, runtime change,
commit, reset, staging, gameplay launch, push or publication is performed for it.
Begin read-only; run verification as needed, retaining its new timestamped output.
Do not change the private profile or start gameplay merely to read this handoff.
Stuart will direct the next agent's scope. Public release requires explicit approval.

## Current design contract

- **Arc Bolt:** 144% effective direct damage, previously 108%; cadence and procs
  unchanged. Migration 15 updates only the exact previous default. Pending shots
  recheck input ownership before firing.
- **Hollowed Orb:** alternate Secondary; works with zero Static Charges. Short
  casts preserve the bank. Holding beyond 0.5 s gathers one charge at a time,
  then at 0.3 s intervals. Aim favors reticle alignment and checks eye/muzzle
  visibility. Release freezes aim/count. Utility cancels through native input.
- Normal Orb uses both hands, with visible body-to-ball energy, surface crackle,
  flight trail and branching electrical impacts/audio. During **Open Circuit**,
  formation and launch move above the head and leave Primary available. Circuit
  expiry restores normal geometry/input ownership.
- Orb launch range is 70 m, speed 32 m/s, lifetime 12 s. Bounce reach is **18 m
  free/one charge, 27 m at three, 36 m at five**. Existing base key/default remains;
  new per-extra-charge growth is 4.5 m (zero restores constant reach).
- Visit fresh enemies before revisits: A–B–C where possible, then A–B–A–B with
  two. With one/two reachable enemies remaining, the nearby owner may be a
  harmless relay. Require 3D feet proximity within 8 m, independent core-flight
  range and world clearance. Player contacts cause no damage, healing, procs,
  Static or enemy-hit spending. Terrain, target loss and lifetime can end a cast.
- **Open Circuit:** requires at least one stored charge. Gather into the crown
  and commit once on the server. One/three/five charges give 1/1.5/2× pulse density;
  extra pulses do not accelerate Static generation. Radius, duration and per-pulse
  damage retain existing settings; native recharge remains held while active.
- **Thundercloud:** third Special, requiring one charge. Gather individually,
  aim a broad area within 80 m, ascend/retreat while aiming and release the crown
  into a cloud. Empty-area cancellation returns bank/stock. Each eligible target
  receives **one damage hit**, with four cosmetic branching return strokes.
  Ascent is 0.9 s, first strike 1.1 s, pack sweep 2.4 s, returns 0.55 s apart.
  Cloud launch through fade is 3.842 s solo / 6.242 s pack; button release adds
  the existing 0.26 s delay plus frame/network latency. Recharge follows the
  longer sequence; Primary is available after release.
- Later visual strokes stop for dead/resolved, departed or occluded victims.
  Damage and spending remain server-owned; visual pulses cannot add damage.
  Direct Orb/cloud/Gaze opening/surge damage does not build Static.
- Preserve existing identities, message layouts, config migrations, Beat values,
  authored animation timing/layers/sockets and Unity GUIDs. No new bundle was built.

Default effective damage includes the existing non-Gaze 0.9 multiplier once:

| Gathered charges | Orb first hit / total hits / diameter / bounce | Cloud radius / damage |
|---:|---|---|
| 0 | 157.5% / 2 / 0.6 m / 18 m | Not admitted |
| 1 | 225% / 3 / 0.6 m / 18 m | 16 m / 270% |
| 3 | 360% / 5 / 0.8 m / 27 m | 23 m / 495% |
| 5 | 495% / 7 / 1.0 m / 36 m | 30 m / 720% |

Each repeat retains 65% of that victim's previous damage. Orb fresh/repeat proc
coefficients are 0.5/0.1; cloud is 0.5. Orb recharge is 7 s after recovery;
cloud recharge is 12 s after its sequence. Custom settings can change these values.

## Review owners and priorities

All source paths below are relative to `HollowSaintMod`:

| Review area | Starting owners |
|---|---|
| Admission, bank leases, cancellation, duplicates, stale replies, death/stage cleanup | `FoundationKit/ChargedStorm/StoredCharge*`, `FoundationKit/Storm/DischargeMeter.cs` |
| Authority and transport; remote-owner input versus server damage | `StoredChargeTransport.cs`, `StoredChargeDriver.cs`, `ChargedStormDamage.cs`; Gaze `Networking`/`Runtime` |
| Orb aim, collision, finite travel, fresh ordering and harmless relay | `FoundationKit/HollowedOrb/ServerHollowedOrb.cs`, `Orb*` policies/geometry/flow |
| Cloud once-only targets, timing, effect coverage and cleanup | `FoundationKit/Thundercloud`, `ChargedStormTargeting.cs`, `ChargedStormEffects.cs` |
| Overhead Orb, free Primary, Circuit cost/density/recharge | `FoundationKit/OpenCircuit`, `HollowedOrbPose.cs`, `StoredChargeState.cs`, `FoundationKit/ArcBolt` |
| Controls, skill stock, loadout registration, Gaze overrides | `Content/KitRegistration.cs`, `ChargedStormRegistration.cs`, `StoredCharge*SkillDef.cs`, Gaze `Runtime/GazeSkillOverrides.cs` |
| Configuration, migration, tooltips and scaling parity | `FoundationKit/Configuration`, `Localization`, `Language/HollowSaint.language`, eight READMEs, package metadata |
| VFX/SFX, skin palettes, animation, optional-failure handling | Orb crackle/flight/pose, cloud FX/crown pose, `StormVisualPrimitives.cs`, `Audio`, `Character` |
| Tests and what their adapters actually establish | `tools/ChargedStormChecks`, Gaze/ConfigMigration checks, `tools/checks.json`, opt-in `Development/DevAutopilot.*` |

Prioritize remote-client correctness and lifecycle/resource/stock behavior, then
moving targets/world collision, descriptions/scaling and presentation. Stationary
solo fixtures leave these questions open even when their assertions pass.

## Evidence map — keep candidate identities separate

| Evidence | What it establishes and limits |
|---|---|
| [Latest results](storm-reach-1.3-results.md) and [machine handoff](../../artifacts/charge-build-1.3/storm-reach-handoff.json) | Final candidate, changes, hashes, package, limits and media |
| [Final Native verification](../../artifacts/verification/20261008T022148-902526Z/results.json) | 35/35 gates, 884 linked ChargedStorm assertions; build/native API access is not gameplay |
| [Latest native trace](../../artifacts/storm-reach-native02/trace.txt), [prelaunch identity](../../artifacts/storm-reach-native02/build.json), [review](../../artifacts/storm-reach-review-1.3/index.html) | 33 behavior passes, zero failures/pop flags; one/six cloud victims each get one damage hit/four visual pulses; full Orb ABABABA across clear 26.81873 m gap, free cast one hit |
| [Latest audio](../../artifacts/storm-reach-native02/audio-analysis.json) | Peak −1.91558 dBFS, zero overs/near-clips/failed posts; numeric capture analysis, not universal subjective mix approval |
| [Early-game/relay results](early-balance-1.3-results.md), [whole-kit review](../../artifacts/review-1.3/index.html) | Earlier DLL: 86 native checks, vulnerable-owner and tall-Titan relay; Primary paired measurement and controlled 54.89824 s itemless Titan; whole-kit presentations. Not a retest of final DLL |
| [Orb/Circuit refinement](orb-refinement-1.3-results.md), [Orb VFX](orb-vfx-1.3-results.md) | Earlier native overhead Primary/expiry/moving-flight and six-palette VFX checks, with their own identities |
| [Final scoped Astra audit](../../artifacts/charge-build-1.3/astra-storm-reach-audit.md), [earlier whole-kit audit](../../artifacts/charge-build-1.3/astra-final-audit.md) | Prior independent findings/recommendations; review them critically, do not treat as fresh execution |
| [Manual acceptance matrix](../manual-charge-build-1.3.md) | Pending human/host-client acceptance; do not mark rows passed based on linked tests |

Known limits and retained issues:

- Final native trace has **49 Windows ClipCursor access-denied errors**, all the
  same category. Do not report a zero-error run. First focused capture failed
  Orb LOS preconditions and is retained at `artifacts/storm-reach-native01`.
- Large smooth opaque cloud lobes obscure much of the upper view. Softer smoke
  and silhouette/occlusion polish remain unfinished visual refinement.
- An already spawned cloud bolt can follow a victim for its remaining lifetime
  (at most 0.392 s); later stroke starts recheck radius/LOS. Actual moving-client
  coverage, unresolved network victims and heavy VFX budgets need review.
- Earlier showcase retains a 15.8 cm spear-release hand-step flag; frame review
  found no obvious detached hand/stuck pose, but it is not proven a false positive.
- Physical mapped controls, remote multiplayer, normal moving/attacking first-boss
  combat and dense late-game performance remain unaccepted. Early balance targets
  had AI disabled and the pilot protected; item draw/luck was not modeled.
- Legacy Unity preview fixtures reference a removed component and are unavailable.
  Cached RuntimeCopies are not acceptance evidence. Build logs retain compatibility,
  obsolete API and unavailable vulnerability-feed warnings.

## Exact frozen candidate and reproduction

Version declarations agree at **1.3.0**. The final private profile was staged and
the game closed at handoff preparation. Backup:
`artifacts/foundation/profile-backup-20261007-192302`.

| Component | SHA-256 |
|---|---|
| DLL | `058FFFF1E30296D116F62A244E0919F8CD1F18280A21C7507E3FFFAFC24C62FC` |
| Language | `0019C65AFBF6558B7507F79149FDAC6ACCD56D00731B18A21B82A62DDD01C55A` |
| Bundle | `7238F181B17B011A0BCC0BF13AB8C7D951CAA7930A24DB4F5B83F278BEA111B4` |

[Private package](../../artifacts/candidates/20261007-192646-1818306/JohnstonStu-Hollow_Saint-1.3.0.zip):
18,819,463 bytes / eight entries / SHA-256
`B6EADF5D1C8CA956668BF357FC71BD83238AAD869CE37D73C2994E78350D04A2`.
[Readback identity](../../artifacts/charge-build-1.3/storm-reach-package-identity.json).
These assets are ignored local evidence; a fresh checkout will not contain them.
Keep this workspace or explicitly carry needed artifacts forward. Do not clean
ignored files, delete authored art or reset untracked implementation.

[Worktree inventory](../../artifacts/charge-build-1.3/audit-handoff-worktree.json)
records changed/untracked paths and candidate-source hashes at preparation.
It is an inventory, not a Git commit or source backup. Review new files with
`git ls-files --others --exclude-standard`; inspect tracked changes with
`git diff HEAD`. Unrelated untracked art/history remains untouched.

From the repository root, with pinned SDK/Python prerequisites:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/doctor.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -Scope Native
python tools/bump.py --check
git diff --check
```

Verification never stages or launches. Its runtime build lands in the new report's
`runtime/` directory; do not assume `bin/Release` was rebuilt. The earlier stale-DLL
staging mistake was caught before capture. Before any subsequently authorized
stage/capture/package, verify the actual input hashes and preserve previous output.

Suggested next-agent request: independently audit the current local 1.3 working
tree using this handoff; report actionable defects and acceptance gaps with exact
locations and evidence, without modifying runtime/profile or publishing. Cover
authority, resources, stock/cooldowns, controls, targeting/collision, damage and
scaling, descriptions, VFX/SFX, animation and cleanup. Identify which new gameplay
tests would resolve the remaining risks. Write a separate report and retain all
existing evidence.
