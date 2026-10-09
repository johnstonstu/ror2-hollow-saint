# Hollow Saint 1.3 independent Astra audit

Reviewed October 7, 2026 against `6c48893cc7b3f566617c8a5563accc22773eb74d`, including the untracked runtime and checks. That baseline descends from released 1.2.0; no divergent v09 merge is involved. Reviewer: requested Astra high subagent. The review changed only this report. No launch, profile operation, asset deletion, push or publication was performed by the reviewer.

The implementation has a coherent server-owned resource model. I found **no P1 defect and no demonstrated charge, authentication, stock or damage-scaling regression**. I found **two P2 presentation defects** in the initial audit snapshot; both now have source corrections and supporting native evidence. **Recommendation: accept as a local candidate for continued playtesting**, with native09's unexplained intermittent five-charge Cloud rejection explicitly retained as an unresolved limitation. The final full verification passes 35/35 gates, and native11 passes 23/23 focused assertions including three consecutive five-charge Cloud casts. This is not unconditional release readiness, real multiplayer acceptance, or physical-controller acceptance. The chronological evidence below preserves the failed trials rather than reclassifying them as passes.

## Findings and proposed corrections

### P2 — Orb presentation arrives independently of the damaging flight

Original locations: `HollowedOrb/HollowedOrbFlightFx.cs:43` and `:54`, with `HollowedOrb/ServerHollowedOrb.cs:44-45` (all feature paths below are under `HollowSaintMod/FoundationKit`).

The original visual interpolated from the launch point to the target's current position using a duration calculated once at launch, then destroyed itself 0.4 seconds later. The server instead homed from its current point at constant speed. This disagrees whenever the target moves. For example, ignoring the small collision radius, a victim initially 32 m away moving directly away at 10 m/s is reached by the server after about 1.45 s at the default 32 m/s, while the visual reaches the moving victim at 1 s and expires at 1.4 s. Sideways movement also gives the visual a different path. This is a source-derived reproduction, not an observed native moving-target trial.

The sender additionally clipped visual duration to four seconds. With the supported 10 m/s speed and a stationary target 60 m away, the visual completed in four seconds and disappeared at 4.4 seconds, but authoritative damage arrived around six seconds. A successful damaging throw could therefore appear to have ended well before its impact.

**Correction:** transmit the authoritative speed, advance presentation toward the same `body.corePosition` with constant-speed motion, and let authoritative impact/end messages terminate the segment. Keep a separate bounded orphan lifetime. Add a moving-target case and the slow/long-range configuration case; verify the payload as well as the motion helper.

**Status at report creation:** implemented in source by the main agent. `OrbFlightMotion` is shared with server travel; the effect now consumes speed and has a 12.4 s orphan lifetime. This addresses the identified fixed-duration error. Network latency can still cause visual lag; this is not full network interpolation or evidence of multiplayer acceptance.

### P2 — Thundercloud also fires an unrelated beam from above the cloud

Original location: `ChargedStorm/ChargedStormEffects.cs:48`, calling `Storm.RoyalCapacitorFx.Strike` immediately after the finite cloud-to-victim effect.

Each cloud hit spawned both the new line from the cloud and the existing Capacitor strike, whose `LightningRibbon` reaches much higher. The second bolt is visible above the cloud in [one-charge native capture](../../artifacts/charge13-native04/cloud-strike-1.png) and [five-charge native capture](../../artifacts/charge13-native04/cloud-strike-5.png). This conflicts with the requested visual source: the crown forms a cloud and bolts descend from that cloud. Damage was still applied once; this is a presentation defect, not double damage.

**Correction:** retain the finite cloud line and use an impact-only effect without the sky ribbon, with the existing thunder sound. Do not alter the ordinary Thunderbolt effect used by other skills.

**Status at report creation:** implemented in source by the main agent. `ChargedStormStrikeFx.Start` now invokes the existing impact-only `RoyalCapacitorFx.Splash` plus `KitSfx.Play(Beat.ThunderStrike)`. The reused beat has its existing sound throttle. A fresh native capture is needed to verify the resulting impact scale, sound and absence of the extra sky ribbon.

## Lower-priority observations

- **Portuguese naming (corrected):** the initial added character/passive resource sentences used “Nuvem de Tempestade”, while the actual skill token and README called it “Nuvem Trovejante”. This affected `HollowSaint.language`'s `HS_DESCRIPTION`, `HS_DESCRIPTION_RELEASE`, `HS_PASSIVE_STORM_DESC` and `HS_PASSIVE_STORM_DESC_RELEASE`, sourced from `tools/charge-language-finish.py`. The implementing agent inventoried and aligned these five exact occurrences. This was P3 text consistency, not a broken translation or missing token.
- **Remote gather presentation (partially reconciled):** `StoredChargeState.FixedUpdate` advances `loaded` on non-authority copies until the reply arrives (`:88-92` in the reviewed snapshot). Only the owner freezes `awaiting`. Originally `ReceiveReply` did not use `p.count` before ending the gather; the implementing agent now reconciles the count and calls `visual.Confirm` before release. Source review confirms the final release strength follows the committed count. Before that reply, an early release near a gather boundary can still let observers show an extra predicted intake cue. Accounting remains server-owned and correct. Keep this cosmetic prediction limitation in the two-client test; it is not established as a native multiplayer failure by the supplied solo evidence.
- **Cloud art/readability:** the shaded volume establishes a visible cloud, but the sixteen flattened opaque lobes read as a dark roof in the five-charge wide capture. Varying height/roundness and improving soft edges is useful art polish. The reviewed images are deliberately placed cinematic cameras, so they do not establish that the ordinary player camera is obstructed. This is not a demonstrated gameplay blocker.
- **Open Circuit expiration:** the Orb muzzle/pose directly chooses overhead versus forward geometry from the current Circuit state. The supplied trial casts well before Circuit expires. Include expiry while holding an Orb in native acceptance to inspect the visual transition and confirm hand, ball and authoritative muzzle remain understandable.

## Resource, controls and combat review

`StoredChargeCastLedger`, `DischargeMeter`, `StoredChargeState` and `StoredChargeTransport` preserve a frozen entry allowance and debit the replicated bank only after an authenticated server commitment. Gathering alone does not spend. Releases are checked against the active native state, cast token, kind, owning connection, life state and finite aim. Duplicate commitment cannot spend twice. New bank gains cannot expand an active gather. The shared lease excludes spear claims and competing Gaze bank ownership. Cancellation and pre-request exits return native stock; an unacknowledged throw intentionally does not optimistically refund it.

Input remains on native skill buttons and SkillDefs. No raw keyboard or joystick polling was added. Primary/other casting admission is blocked during gather, and Utility queues its native execution after cancellation. The default families retain Stormspear first and Gaze/Open Circuit first/second; Orb and Cloud are appended. Gaze's contextual overrides recognize the new SkillDef subclasses and preserve the equipped Orb's base recharge.

Cloud preparation finds and deduplicates eligible enemies around the aimed point, checks cloud-to-victim world clearance, sorts near to far and schedules one strike per selected victim. Damage checks life/team/radius/clearance again at strike time. Target and cast limits bound the operation. Empty preparation rejects before spending.

Orb authority uses world-cleared launch geometry, finite travel, terrain collision, owner/stage/body replacement guards and a finite hit budget. Fresh nearby targets are selected before revisits; the last victim is excluded. Previously hit non-target bodies do not intercept a path to a fresh victim. A single surviving enemy receives one hit rather than invented immediate self-bounces. First-hit proc is 0.5 and repeat proc 0.1; repeat damage is multiplied by 0.65 for each prior hit on that victim. Damage invokes normal hit reporting under the existing Storm damage scope, preventing direct spender damage from generating its own Static while preserving native item event hooks.

The advertised defaults match the code after the existing non-Gaze 0.9 multiplier, applied once:

| Charges | Cloud radius / damage per victim | Orb diameter / first hit / total-hit ceiling |
|---|---|---|
| 1 | 16 m / 270% | 0.6 m / 225% / 3 |
| 3 | 23 m / 495% | 0.8 m / 360% / 5 |
| 5 | 30 m / 720% | 1.0 m / 495% / 7 |

Cloud is a broad crowd spender; Orb distributes finite hits and attenuates revisits. Neither test counts nor these coefficients establish ordinary-run balance. Moving/flying targets, dense packs, terrain, item synergies and boss encounters remain the meaningful balance tests. Cooldowns begin after the owning cast state ends (12 s Cloud, 7 s Orb), and the new cooldown options correctly declare restart required.

## Animation, effects, sound and text

The existing presentation owners, serialized identities, animation names and authored callback order were not renamed or refactored. New feature-local controllers add the Orb arm solve and crown translation, restoring their saved transforms on update/disable. Native images show substantial Orb size, an overhead Circuit gather, skin coloring and restored arms/crown. I directly inspected both icons plus representative Cloud strike, normal Orb, Circuit Orb, Crimson Orb, Cloud restoration and Gaze images; I did not visually inspect every one of the 119 images in native04.

Native04 reports **three** threshold motion events: two hands at Utility transition (10.9/11.6 cm) and one hand at Circuit Orb recovery (10.1 cm). The monitor compares frame displacement without time normalization and only excludes 0.35 s after screenshot captures. These records cannot isolate an animation ownership defect; they also cannot support “zero pops” for the new abilities. Preserve them as transition polish observations for a continuous native capture. Gaze05's zero-pop result applies to its Gaze-only trial.

Effect registration uses separate new catalog entries, avoids double root scaling and keeps optional material/effect failures from aborting content loading. Orb and Cloud reuse the existing soundbank. Native04's 219.6 s loopback analysis reports **-2.8 dBFS**, zero clipped or near-clipped samples and no failed recorded sound posts. That is useful level/posting evidence, not a listening review or a mix guarantee under crowded combat. Gaze05 reports **-0.1 dBFS**, zero clipped samples and 40 near-clipped samples; its capture contains other-mod enemy effects. Do not describe every recording as free of near clipping, or infer per-event causal audio attribution from overlapping event windows alone.

All four locale entries include the new skills and control/overhead descriptions; numeric descriptions are derived from bounded tuning and bank capacity. The Portuguese naming note above is the specific consistency issue found. Plugin, assembly project and manifest declare 1.3.0, the manifest retains the existing identity/dependencies, both new PNG icons are embedded by the existing resource glob, and the local package script continues to verify and copy an allowlist with the pinned released bundle. Existing external README media are not new native evidence. The final candidate ZIP did not yet exist as an audited output of this review.

## Evidence limits and acceptance status

- [Native verification report](../../artifacts/verification/20261007T135521-734485Z/results.json): 35/35 gates passed for that snapshot, including 378 linked ChargedStorm assertions and 670 Gaze primary assertions. These use explicit native adapters; they do not reproduce Unity physics, Animator evaluation or real transport scheduling. The driver/presentation adapters are not the runtime effect lifecycle.
- [Native04 trace](../../artifacts/charge13-native04/trace.txt): new-ability portion has 75 passing behavioral assertions and 80 new-skill images across seven skin selections. The full file truthfully ends `errors=7 pops=3`; its appended old-Gaze portion failed seven checks. It must not be presented as an entirely passing run.
- [Gaze05 trace](../../artifacts/charge13-gaze05/trace.txt): corrected fixture uses native base Orb selection, fixed-tick entry taps and protected test dummies; reports complete, zero errors and zero pops, with 39 Gaze images. It covers the existing release/cancel cases and restoration of the equipped Orb's elapsed cooldown. This is the relevant later Gaze evidence, not grounds to erase native04 failures.
- Native04/gaze05 logs include the pre-existing missing MoreStats assembly and a startup `Hidden/ProBuilder/EdgePicker` shader-key error. The latter is distinct from the native HGStandard shader used by the new Orb/cloud. These are not evidence of a new-feature failure, but the logs are not wholly error-free.
- No real remote client/server session, reconnect/packet scheduling acceptance, physical controller trial, ordinary combat playthrough or exhaustive item/mod interaction test was supplied. Native mapped-input automation, actual native damage reports, linked-code assertions, member-access scans and visual stills prove different things.
- The later full Native run stopped at a language expectation after additive resource text changed. The implementing agent reported correcting the exact expected sentence and passing the dedicated language check; that does not replace a new full Native report for the revised source.
- Corrective source re-review included the speed payload, shared motion helper, server travel call, client target point, acknowledged-count reconciliation and new moving-target fixture. The expanded linked suite is reported as 396 assertions, including the speed payload, constant-speed moving-target steps, range bounds and confirmed presentation count. The new native fixture measures moving-target visual speed and one authoritative hit. Its execution result and final Native report are still pending at this report's finalization.

Acceptance recommendation: retain the two original findings and their correction record, run final verification, then capture the corrected Cloud impact and a moving/slow long-range Orb. Keep multiplayer, physical controls, normal-camera readability and combat balance explicitly unverified. No publication approval is implied by this audit.

## Native06 follow-up: acceptance remains open

The subsequent [native06 trace](../../artifacts/charge13-native06/trace.txt) reports complete with **five failed assertions and six motion-threshold events**. This supersedes the statement above that execution of the corrective fixture was pending; it does not establish acceptance of the corrected build.

The moving and slow-flight visual-speed assertions passed, with reported maxima of 32.00089 and 10.00093 m/s. However, neither case recorded the required authoritative hit on its lone target. Both hit assertions failed. Cloud then hit three dummies at one charge, only two at three charges, and did not commit the five-charge cast. Its three-charge server `THUNDERCLOUD_BEGIN` already reports two selected targets, so the missing Lemurian was excluded at preparation rather than lost solely during the later strike schedule. No five-charge cloud begins. The source still rejects an empty preparation before spending; these observations alone do not demonstrate a spending defect.

The reviewer inspected native06's Cloud one/three/five-charge frames and moving-restoration frame. The successful one-charge frame no longer shows the old bright sky ribbon extending above the cloud, supporting that specific visual correction. The five-charge frame contains no cloud and cannot serve as corrected five-charge presentation evidence.

There are concrete **fixture validity gaps** to resolve before assigning these misses to the runtime:

- `ChargeMovingTarget` relocates the Lemurian to 25 or 50 m using the player's fixed ground height, without grounding that destination or proving a sphere-clear flight lane. Arena setup only measured a 40 m chest-height ray. This does not establish valid terrain placement at 50 m or clearance for the Orb's actual radius.
- The `chargeDummyMoving` flag suspends `HoldDummy` anchoring for all three dummies, including the two non-moving Beetles. It restores one position but does not reset the moved body's motor velocity. Exact position, velocity, life state and line of sight at release/restoration were not recorded in native06.
- Cloud captures `DummyChest(0)` once before gathering. A transient displaced or below-ground core at that instant could preserve an invalid aim through the gather even if anchoring subsequently restores the dummy. This is a hypothesis requiring position/aim diagnostics, not a confirmed cause.

The next diagnostic run should record every dummy's life/health, core/foot position and motor velocity before and after relocation and before each Cloud aim capture; initial Orb selection/LOS; world-collision termination; and Cloud aimed center, sky point and per-target eligibility rejection. Preserve native06's failures and avoid changing resource or damage rules merely to satisfy an unvalidated fixture.

The added Circuit-expiry checks passed: the bank and gather remain intact through expiry, geometry returns to the forward muzzle, and release spends five once and recovers. These assertions cover the gameplay transition; they do not by themselves certify smooth animation between the overhead and forward poses. Gaze's release/recharge assertions also passed in this run. Multiplayer and physical-control limits remain unchanged.

## Native07/08: narrower corrective evidence

[Native07](../../artifacts/charge13-native07/trace.txt) reports two failures and five motion events. Both failures are the moving/slow Orb hit checks; the existing skill, Cloud, Gaze and Circuit-expiry checks pass. Its motion-fixture initial core positions still match the old nearby dummy location immediately after relocation, demonstrating that immediate transform sampling did not establish settled targeting for those tests.

[Native08](../../artifacts/charge13-native08/trace.txt) is a focused run with one failure and zero motion events. After waiting for relocation to settle, the stationary 50 m / 10 m/s Orb records one authoritative hit and a maximum observed visual speed of 10.00091 m/s. Cloud one/three/five-charge damage and spending checks also pass. The moving target still receives no recorded hit, despite a speed-bounded visual. Its endpoint core moves approximately 15 m upward and 4.4 m sideways from the settled starting core, inconsistent with the intended horizontal retreat. The test incrementally moved from the body's current foot position, permitting accumulated body/interpolation offsets and movement during gathering to contaminate that path. This remains a fixture diagnosis; the exact native source of every offset was not measured.

The revised fixture now pins the victim and resets velocity while gathering, updates aim during gathering, checks the launch cone, and computes travel from an absolute start point plus distance along the intended direction. Source review supports these changes. `MOTION_RESTORED` is logged immediately after teleport, so its core reading can still reflect the preceding endpoint; it should not be interpreted as proof that final restoration failed.

The reviewer directly compared native07's faint five-charge strike with [native08's stronger first strike](../../artifacts/charge13-native08/cloud-first-strike-5.png). The stronger finite line is readable beneath the cloud, connects to the victim, and has no independent above-cloud sky ribbon. This supports the Cloud presentation correction. The cloud's dark, flattened volume remains optional art polish; these staged wide-camera captures do not certify normal-camera readability.

The moving-target hit assertion remains open pending native09. Speed-only passes are not substituted for successful authoritative contact, and none of these runs establish real multiplayer acceptance.

## Native09: Orb corrective cases pass; Cloud repeatability remains open

[Native09](../../artifacts/charge13-native09/trace.txt) supplies the previously missing Orb evidence. Both the controlled retreating target and stationary slow long-range target receive exactly one authoritative hit; observed visual maxima are 32.00093 and 10.00097 m/s. Launch-cone measurements are approximately 0.678 and 0.385 degrees. This closes the specific native moving/slow-flight cases for the corrected Orb presentation.

The run nevertheless reports **two failed assertions and zero motion events**: five-charge Cloud does not commit or hit its three intended dummies. One- and three-charge Cloud casts hit all three. The reviewer inspected the failed five-charge screenshot and later Lemurian sound events: the target remains present and active, so the earlier simple target-loss hypothesis is insufficient for this run. There is still no preparation-rejection geometry recorded to distinguish an empty aimed area, world obstruction or another admission condition. Capture the actual release direction, aimed center, sky point and eligibility counts before assigning the cause or claiming stable final Cloud acceptance. Native08's successful five-charge capture remains valid presentation evidence, but does not erase this failure.

## Final corrective review: native10/11 and full verification

[Native10](../../artifacts/charge13-native10/trace.txt) passes **17/17 assertions**, with zero errors and zero motion events. Its preparation diagnostics show all three intended targets both in range and visible at one, three and five charges. Cloud centers lie near the sampled targets and sky heights are 10, 11.5 and 15 m respectively, consistent with the intended geometry. The reviewer inspected its five-charge first-strike capture; the finite bolt clearly connects the cloud underside to the victim. Its loopback report records -5.4 dBFS peak with no clipped or near-clipped samples.

[Native11](../../artifacts/charge13-native11/trace.txt) passes **23/23 assertions**, with zero errors and zero motion events. It repeats the moving and slow Orb cases successfully, with one hit per lone victim and maximum observed visual speeds of 32.00092 and 10.00087 m/s. It then runs Cloud at one, three, five, five and five charges. All five casts select and hit three intended victims, spend the exact requested charge count and return control. The three `THUNDERCLOUD_PREPARE_5` records independently report `inRange=3 visible=3`; all have the expected 15 m cloud height. The reviewer directly inspected the saved five-charge first-strike frame. Repeated five-charge screenshots share filenames, so the trace and diagnostic records establish the earlier repeats; the saved image represents the final repeat.

The [final full Native report](../../artifacts/verification/20261007T145730-414096Z/results.json) passes **35/35 gates**, including **396 ChargedStorm assertions** and **670 Gaze primary assertions**. These are the final reviewed source/build/member-access gates, distinct from native gameplay evidence.

Native10/11 added diagnostics and repeated the scenario; **runtime target selection behavior was not changed to make them pass**. Consequently, native09's intermittent rejection is non-reproduced, not explained or proven fixed. The confirmed presentation defects are corrected and the local candidate has sufficient evidence for continued private playtesting, provided this unresolved observation travels with the candidate. A future recurrence should capture the rejected preparation geometry before changing targeting behavior. Ordinary gameplay feel, normal-camera cloud readability, physical controls, remote networking and broader item/mod interactions remain unverified. Publication is neither requested nor approved by this recommendation.

Reviewed candidate identity, independently matched against the local built DLL and language file:

- DLL SHA-256: `34E43AE2A7B3A6295DDF1168A9AE598F3E912A0A402E7AC11F83033CAB647EC5`.
- Language SHA-256: `FD3ED8C04F71D3FE577435577AE1102430F770F7F90F1608C58A20D214435861`.

The package ZIP was still being assembled when this audit was finalized; archive contents and ZIP hash belong in the packaging handoff, not in this audit's verified claims.
