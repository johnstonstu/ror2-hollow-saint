# Charge build toward 1.3: initial prototype record

Historical milestone: the evidence below describes the first, unversioned
prototype. The later full 1.3 authorization, staged native trials and audit are
tracked by [the finish plan](charge-build-1.3-finish-plan.md). Preserve the original
hashes and reports below as evidence of that earlier build.

Built 2026-10-06 Pacific / 2026-10-07 UTC. This implements Stu's authorized
local workshop toward 1.3.0, with a possible one-ability 1.2.5 milestone.
Gameplay acceptance is pending; metadata remains 1.2.0.

## What is implemented

- Third Special: working name Thundercloud. Native hold/release gathers Static
  Charges into the crown, with a short up/back hover, live area preview, actual
  crown ascent, a large growing cloud, descending near-to-far strikes and fade.
- Second Secondary: Hollowed Orb. Both-hand gather/throw pose, charge-dependent
  ball size, visible travel, bounded server collision/range, fresh-enemy priority
  and finite revisits: A-B-C first, otherwise A-B-A-B.
- Shared server lease and authenticated native release. Gather does not spend;
  early release spends its frozen gathered portion once. Cancellation preserves
  bank/stock. New gains cannot enlarge the current entry allowance. Direct new
  damage runs inside the existing Storm scope to prevent direct self-funding.
- Native admission prevents competing actions during gathering. Utility waits
  for cancellation before executing its equipped skill. Gaze's contextual
  overrides recognize the new kit definitions and preserve underlying recharge.
- Separate tuning sections, two icons drawn with the existing icon tools, and
  four complete language entries. The released bundle and soundbank are reused.

Existing GUID, assembly identity, names/tokens, packet layouts/IDs, configuration
migrations and Unity asset GUIDs are preserved. Family defaults/order remain
Gaze/Open Circuit/Thundercloud and Stormspear/Hollowed Orb. New request/reply IDs
are 29040/29041; a foreign handler collision is logged and disables affected new
admission rather than overwriting the other mod's handler.

## Evidence

Baseline at HEAD `6c48893cc7b3f566617c8a5563accc22773eb74d`, descended from
released v1.2.0 `e44d582964bf7e69fb6e410e9eeaf2748b443197`. No v09 merge.
Initial baseline Native run passed all 34 gates.

Final run: `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -Scope Native`.

[Final report](../../artifacts/verification/20261007T060543-777304Z/results.json):
**35/35 passed**, including all registered source suites, language/tooling/version
checks, locked restore, runtime build, scanner fixtures and installed-member access.
Build: **0 errors, 21 warnings** (existing obsolete API/reference warnings and an
unavailable NuGet vulnerability endpoint). Version parity remains 1.2.0.

- ChargedStormChecks: **373 assertions**, linked production accounting, admission,
  release/packet, actual server flight and area logic with explicit in-memory
  native/presentation/driver adapters. Covers stale/duplicate/foreign releases,
  partial spending, frozen gains, cancellation, timeout/launch failure, Utility
  ordering, walls/range, death, stage change, deduplication and finite revisits.
- ConfigMigrationChecks: **221 assertions**, including new custom options/callbacks
  and unchanged ordered migrations.
- GazePrimaryChecks: **670 assertions**, including recharge for new kit subclasses
  while Gaze owns native contextual overrides.
- Language checks: **51 tokens across four languages**, complete placeholders,
  style tags and existing legacy-mode coverage.
- Native member access: **ACCESS_CHECK_PASS**. The earlier run caught four
  private CharacterBody transform-field references; all use legal component
  paths in the final DLL. This proves installed API access, not gameplay.
- Both new 256px icons were visually reviewed. No legacy Unity preview was used.

## Reviewable local handoff

[Handoff manifest](../../artifacts/charge-build-1.3/20261007T060543-777304Z/handoff.json)
records source HEAD, the dirty working tree, report and file hashes. Files:

- `HollowSaint.dll`: `E08B6D7D59B212414E54128F8F3843DAE7B98610340BE5432BE36E94A999B0AF`
- `HollowSaint.language`: `3D967946B894C4121B9FF2375212924E9F83F5DBFA989CEF8306EFE160066F5D`
- Released `hollowsaintassets`: `7238F181B17B011A0BCC0BF13AB8C7D951CAA7930A24DB4F5B83F278BEA111B4`

The original assets and unrelated authored/untracked files remain intact. No
profile staging, gameplay launch, version bump, commit, push or publication was
performed. This folder is a private development handoff, not a release package.

## Next acceptance boundary

Stuart runs the [manual matrix](../manual-charge-build-1.3.md), especially actual
crown/hand pose restoration, wide-field coverage, cloud opacity/thunder, Unity
physics and physical-controller host/client behavior. Default numbers are a
balance hypothesis in the [plan](charge-build-1.3-plan.md), not accepted final
balance. These observations decide whether to split a 1.2.5 milestone and what
to tune before deliberately versioning a 1.3.0 candidate.
