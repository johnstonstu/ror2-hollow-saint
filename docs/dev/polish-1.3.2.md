# 1.3.2 polish continuation

Local candidate following released 1.3.1 (`60b0cf3a`), reviewed October 10, 2026.
The working tree already contained Claude's 1.3.2 work when this continuation
started. The initial review stayed local; subsequent authorized Dev staging and
release preparation are recorded below. Stuart owns gameplay acceptance and the
Thunderstore upload.

## Scope and behavioral criteria

- Hold Primary during Gaze's wind-up or Thundercloud gathering: Arc Bolt keeps
  firing through native skill input. Cast strength uses the starting charges;
  new income remains reserved for a later cast.
- Stormspear pauses new bolts while charging and resumes when the throw ends.
  Starting a spear cancels only an unlaunched bolt using the spear hand. The
  other hand's in-flight cast completes. Both configured spear hands work.
- A bolt over the crown keeps the other arm raised and returns to the crown.
  Queued replacement gestures, migration between movement layers and long
  frames must not resurrect or overwrite an interrupted pose.
- Cancellation and owner loss release effect resources and owned sound cues.
  Damage, authored strike timing, network identities and serialized field order
  remain unchanged.
- Player docs stay compact and agree across all root/package language copies.

## What the continuation fixed

**Input and reserves.** Gaze previously took income earned during its gather
into the entry-fuel ledger at beam start. Normal 1.3.1 beam input cannot spend
that fuel (mid-beam surges are disabled), but those new charges disappeared from
the visible reserve until beam end. The server now snapshots the starting bank
and partitions later income into reserve at handoff. Existing Begin-packet
fields also carry that reserve to crown presentation; no packet layout changed.
Thundercloud's original frozen-entry behavior is retained and checked.
Gaze's Utility exit also now distinguishes cancellation from a beam handoff,
so the owning player's cancelled wind-up uses its cancellation fade instead of
the release flash. Remote transition behavior is unchanged and needs playtest.

**Animation.** Crown recovery now claims only the layer whose bolt belongs to
the held crown. A replacement arriving between animation updates clears the
stale arm pin. Movement migration and a missed resume window retain recovery.
Pose diagnostic sample values reset every frame rather than reporting an older
successful sample. The inherited pass also retains continuous bolt layer
weight, crown pose resumption, dash-jump push-off and bounded hit flinch.

**Effects and sound.** Spear cancellation fades owned cues over 180 ms while
throw release keeps its existing 30 ms stop. Orb and Gaze track overlapping
intake sounds and give cancellation tails bounded lifetimes. Gaze partial
initialization and interrupted teardown release detached objects. Cloud cleanup
handles owner despawn, and a stuck spear records its anchor immediately to
cover loss before the first frame. Existing palette, glow and body-current
fades are retained.

**Docs and fixtures.** The README keeps the earlier compact layout and adds
held-input behavior in English, Portuguese, Russian and Chinese. It no longer
claims Gaze always spends the whole bank. The developer guide points to the
actual verifier and timestamped candidate output. The `accept-132` fixture now
holds Secondary through natural spear recharge instead of resetting stock,
checks visible gather reserves during the beam, and fails missed pose windows.
Projectile polling is explicitly described as sampled evidence that can miss
short-lived projectiles.

## Evidence

- `tools/doctor.ps1`: all Quick, Build and Native prerequisites available.
- Final `tools/verify.ps1 -Scope Native`: **35/35 passed**, comprising all 31
  registered offline checks, locked restore, runtime compilation, scanner
  fixtures and installed native member-access verification.
- [Final report](../../artifacts/verification/20261010T144450-850500Z/results.json).
  [Runtime DLL](../../artifacts/verification/20261010T144450-850500Z/runtime/HollowSaint.dll).
  DLL SHA-256: `9C3582B3CA18897D0D3D605734645B160BEA887AAA3E789D9B32FDFD2D4AAC81`.
- Focused counts within that run: 2,561 ChargedStorm assertions; 6,274 GazeFuel
  assertions; 10,512 SpearAudio lifecycle assertions; 316 TerrainSpear assertions;
  56 crown ownership/recovery/migration checks using explicit Animator adapters.
- Build completed with zero errors and 30 warnings: the MMHOOK target-framework
  compatibility warning and obsolete APIs in existing development fixtures.
  They are recorded in `RuntimeBuild.log`; this is not a warning-free build.
- Local links checked in all eight README copies plus the developer guide,
  development map and this record. `git diff --check` passed.
- The earlier continuation run at `20261010T144253-024503Z` failed because the
  TerrainSpear harness lacked Gaze lookup adapters used by the inherited input
  gate. Those adapters and Gaze overlap regression cases were added before the
  passing final run. No failed run is counted as acceptance.

The earlier `artifacts/accept132-r1`, `accept132-r2` and `smoke132-r1` directories
are inherited historical evidence. The r2 trace reports successful solo
input/resource checks, but does not certify this continuation's final DLL,
current arm blending, physical controllers or multiplayer. The old handoff
mentioned `claude/polish-1.3.2.md`; that file was not present in this checkout.

The inherited Thunderbolt ring cancellation code handles legacy presentation
beats. Current `ThunderboltDriver` performs explicit spear strikes and has no
automatic telegraph; the review found no production producer of those legacy
telegraph beats. Do not report that plumbing as a verified active gameplay fix.

## Stuart's focused native acceptance

1. Hold Primary through Gaze and Thundercloud gathering, while stationary and
   moving. Check both alternating hands, crown return, and newly earned reserve
   charges staying visible through Gaze.
2. Hold Primary, charge/throw the spear, then hold Secondary through its real
   cooldown. Check automatic re-entry and both configured spear hands.
3. Cancel each gather with Utility; check no false release flash, no stuck
   crown light, and a short sound tail. Repeat with death/despawn or a stage exit.
4. Check normal and high attack speed, a low frame rate, arm-life disabled,
   dash-jump push-off and hit-flinch settling. Inspect from the normal camera.
5. In a host/client session, check projectile requests, reserve presentation,
   cancel/release cues and skin palettes. A solo fixture cannot prove these.

Offline animation adapters cannot certify Unity clip sampling or visible pose
quality. Audio lifecycle assertions cannot certify the final audible mix.
Native member-access verification does not execute gameplay. These acceptance
limits remain open before a public 1.3.2 release.

## Orb and dash audio follow-up

Added an electrical Orb charge rise, quiet looping hold, and fuller discharge,
plus a subtle crackling accent on Arc Step. These use original project synth
sources. Orb audio owns its playing IDs and fades on release, cancellation,
death, disposal and stage changes. The throw uses a short-lived stationary
emitter so a close hit does not truncate its tail.

- Native verification: **35/35 passed** in
  `artifacts/verification/20261010T175258-671837Z/results.json`.
- Spear/Orb lifecycle coverage: 17,527 assertions across 1,000 casts each.
- Audio bank validation preserved all 50 previous media entries and 161 routing
  objects exactly; added four sounds, including one loop. See
  `art/audio/orb-dash-validation.json` and `art/audio/README.md`.
- Refreshed Hollow Saint Dev using `tools/dev-profile/Stage-Build.ps1`; its checks,
  build and native-access scan passed. Installed assembly version is 1.3.2.0.
- Verified installed DLL SHA-256:
  `8C51349B86C42B77D679D84DF09841F68594929D7A549490FF33777853AB55CE`.
- Previous profile files backed up in
  `artifacts/foundation/profile-backup-20261010-105412`.

Dry preview: `artifacts/audio/orb-dash-132-20261010T175026Z/orb-and-dash-preview.wav`.
This follow-up supersedes the earlier candidate DLL above. No gameplay was
launched; native listening, combat balance and multiplayer acceptance remain
with Stuart. Nothing was published.

## Release preparation

Stuart authorized the GitHub push and local package preparation, retaining the
Thunderstore upload. The English Thunderstore README now has about 576 visible
words (81% fewer than 1.3.1), with 1.3.2 highlights and a link to the detailed
GitHub guide. Its repository links and media targets resolve locally.

- Packaging reran all 31 source suites, compiled the release and passed native
  member-access verification. Report: `artifacts/verification/20261010T180611-725279Z/results.json`.
- Build: zero errors; existing compatibility/obsolete API warnings plus a
  NuGet advisory lookup warning because its service was unreachable.
- ZIP: `artifacts/candidates/20261010-110656-7885874/JohnstonStu-Hollow_Saint-1.3.2.zip`.
- ZIP SHA-256: `156FDDC4BCC86821FB006CDD1D113F7CA4600092FB648A614AF63D3FA44EC01A`.
- Archive integrity and its eight allowlisted entries verified; packaged DLL
  exactly matches the audio follow-up staged in Dev. No player config ships.
- No Thunderstore upload or new gameplay execution was performed.
