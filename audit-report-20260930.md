# Hollow Saint v0.7.14: independent pre-playtest audit

Audited September 30, 2026 against the working tree on `codex/spear-channel`.
Read-only review: no source, asset, profile or git state was changed. Nothing pushed.

## Verdict

**Ready for a limited solo playtest on the Hollow Saint Dev profile.** No blocking
defect found. Findings below are cosmetic or rare-trigger issues, confirmed by code
and asset reading but not reproduced in the game. Multiplayer is not cleared.

## Candidate identity (verified)

- Local DLL `6E4971A5...A4F` and bundle13 `1AD13A0D...BC` match the handoff.
- No `.cs` file is newer than the built DLL.
- Rollback backup `artifacts/foundation/profile-backup-20260930-144839` matches its
  `stage.txt`: DLL `94C84E45...AA34`. The bundle hash is identical before and after,
  so v0.7.13 to v0.7.14 changed only the DLL.
- Not verified: the installed copy in `%APPDATA%` (outside the connected folder).
  Confirm the version line in the game log before judging anything.
- Not rerun: Unity wrappers and `dotnet build` (this audit shell is not Windows).
  Test counts are the handoff's; their outputs were inspected, not regenerated.

## Were the three preview-fault explanations independently supported?

1. **Stale GPU skinning in repeated renders: supported.** The CPU-baked study
   (current-flow02) and the ordinary SkinnedMeshRenderer capture over separate editor
   updates (end-pose02) agree on geometry at frames 80 and 119: wrists connected,
   spear in the fitted grip, ring mid-close at 80 and closed at 119. No production
   code moves bones after rendering, so there is no in-game analogue of this fault.
2. **Skipped closing clip: supported for production.** The driver plays
   `Halo/Open Circuit end` on the buff's falling edge (OpenCircuitPulseDriver.cs:143).
   In the controller that state has no exit transition, so it holds its last frame.
   That frame equals the Idle pose for arcs 1-4 (0.0 mm, 0.0 deg) and is within
   8 mm / 2.2 deg for halo root. Only Open Circuit code touches the Halo layer, so
   dash, sprint, jump and attacks cannot cut the close short.
3. **Held spear hidden on dash: supported.** SpearCarry visibility depends only on the
   out/recall state and invisibility, never on dash.

The 28 runtime copies used by the preview are byte-identical to current source.
The preview **does not** include OpenCircuitPulseDriver, the production `KitAnim.Play`
path, or real EntityState callbacks. PreviewBindings.cs:92 replaces `PlayOnBody` with
a plain 0.09 s CrossFade. EndPose checks arc translations only, not rotations.

## Findings (none blocking)

| # | Severity | Finding | Trigger | Location |
|---|---|---|---|---|
| 1 | Medium, cosmetic | A recast during an active crown snaps the ring shut, unfolds it again, then freezes on the cast's last frame. The hold loop never resumes. The ring still closes when the buff ends. | Open Circuit recast while the buff is up: Bandolier reset, 2+ Alien Heads, Purity, Brainstalks | Driver plays hold only on the false-to-true edge (OpenCircuitPulseDriver.cs:128-138). `AddTimedBuff` only refreshes (OpenCircuitState.cs:266). |
| 2 | Medium, rare | The ring stays unfolded until the next cast. | Open Circuit interrupted in its first 0.21 s (stun or freeze via SetStateOnHurt, KitRegistration.cs:141), so no buff is applied | Halo `Open Circuit` state has no exit. No buff means no close edge, so the safety net never runs. |
| 3 | Low | The halo safety net stops acting at 3.0 s. If it were ever needed, the ring would snap back to the bad pose. Its comment (Arc Bolt taking UpperBody) predates the separate Halo layer. It does not cover #1 or #2. | Only after a close edge | OpenCircuitPulseDriver.cs:67 |
| 4 | Low | One frame loses torso lean, arm life and spear aim, and the ring/current fit mismatches the rendered pose. The preview cannot show this. | Carry state changes while holding: crown start/end, after catch, 0.25 s after fan end | SpearCarry.cs:96-100 calls `PlayOnBody`, which calls `animator.Update(0)` twice (KitShared.cs:423-431) in LateUpdate at order 170. That runs after pose (100/101) and ring fit (150). |
| 5 | Low | Arcs jump up to 7 cm at close. The preview crossfades, which hides this. | Crown expiry at any hold-loop phase | Halo layer plays instantly (KitShared.cs:428). Hold-loop keys differ from the end clip's first frame by up to 73 mm. |
| 6 | Low | Arm life, lean and spear aim disappear while paused. The spear stays in the hand. Do not log this as a detach. | Solo pause (timeScale 0) | FoundationArmPose.cs:159 and FoundationMotionPose.cs:68 return after Restore on zero dt. |
| 7 | Low | The recall line trails the hand pose by one frame. | Recall | LightningLine order 50 runs before pose. Self-ticking anchored lines: KitFx.cs:491, StormChargeHalo.cs:154. |
| 8 | Low, MP only | The client shows a catch but the spear stays out, so a second recall is needed. | Recall reaches the server more than 0.15 s before the spear registers | ConduitSpearRecallState.cs:253-258 |
| 9 | Info | The Halo override layer masks every Body-layer halo curve, so the Spawn clip's folded arcs and the locomotion halo bob never show. For the same reason, the safety net's spawn-time rest capture reads the true rest (end-pose01 frame 0). | Always | HS_Halo.mask with Halo layer weight 1 |

Unchanged from the handoff: the right-forearm glow-strip skin-weight flaw, the two
known log errors and the JobTempAlloc warning.

## Spear controls (code review)

- Free recall bypasses the stock gate. No stock deduction, no recharge reset.
- A throw needs a ready charge. Interrupting before release refunds the charge
  (authority only, while alive).
- Primary with the spear held uses the fan. Fan exits on release or when not held.
  Primary with the spear out uses Arc Bolt.
- Primary and secondary are blocked while recalling.
- Fan minimum priority is Skill, so a throw can interrupt it. Order is correct: fan
  OnExit, then throw OnEnter.
- Right fingers take no procedural life while gripping. The spear visual is parented
  to SpearGripSocket, so the grip cannot separate from the hand.

## All-skin lightning (code review plus contact sheet)

- Lines, fan, spear, crown and beats resolve their palette from `body.skinIndex`.
  Network beats carry the arc color. Obsidian keeping cyan is intentional.
- FoundationSkinAnimation runs at order 201, after ModelSkinController.Start, and
  refreshes its classification when the material reference changes. Its property
  block writes keep other writers' values.
- The current-flow02 contact sheet shows ring, body and current recolored in all five
  skins at frames 64 through 119.

## Minimum playtest checklist

1. Check that the log reports v0.7.14.
2. Open Circuit: dash, jump, sprint and Arc Bolt through the whole window, then let
   it expire. The ring should close within about 1 s and stay closed.
   `HOLLOW_SAINT_HALO_RESET` should not appear.
3. If a cooldown reset turns up, recast during the crown and note #1.
4. Spear:
   - Hold for the fan, release, throw (charge spent).
   - Arc Bolt while the spear is out.
   - Free recall and catch, throw again once a charge is ready.
   - Interrupt a throw before release.
5. Close camera on both wrists and the grip during release, catch, fan start/end,
   dash and landing.
6. Repeat items 2 and 4 on all five skins.
7. Death and respawn, stage change, pause.
8. Search the log for `HALO_RESET`, `SPEAR_RECALL_MISSING`, `ANIM_PENDING`,
   `ANIM_STATE_MISSING`, `BODY_CURRENT` and exceptions.

## Rollback to v0.7.13

With the game closed, copy `HollowSaint.dll` and `hollowsaintassets` from
`artifacts\foundation\profile-backup-20260930-144839` into
`%APPDATA%\r2modmanPlus-local\RiskOfRain2\profiles\Hollow Saint Dev\BepInEx\plugins\JohnstonStu-HollowSaint`.
Expected DLL SHA256 afterwards: `94C84E4557D2EBC5426873E14D8A7AB9A4155F33BCC0AC47AA3C1AC99C94AA34`.
The bundle hash does not change.
