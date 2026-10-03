# Hollow Saint: next-phase plan

Written September 27, 2026, after reading `HANDOFF.md`, `docs/next-agent-handoff-20260927.md`,
`docs/ror2-integration-plan.md`, `docs/controller-acceptance.md`, the plugin source, the
`Foundation.controller`, the source clip catalog, and the 21:53 PT `LogOutput.log`.
Planning only — nothing was implemented, built into the profile, or launched.

## What I verified myself (not taken on trust)

| Claim | Result |
|---|---|
| Plugin builds | **Green.** `dotnet build HollowSaintMod/HollowSaint.csproj -c Release --no-restore` succeeds, 0 errors, 1 pre-existing transitive `NU1701` MMHOOK warning. ~0.4s. |
| Mannequin null refs are real | **Confirmed, 2 occurrences**, both `SurvivorMannequinSlotController.ApplyLoadoutToMannequinInstance` (`IL_003C`), at log lines 404 and 412. |
| Animator warnings are real | **Confirmed**: exactly 5 paired `Animator.GotoState: State could not be found` + `Invalid Layer Index '-1'` at log lines 424–433, immediately after `HOLLOW_SAINT_BODY_STARTED`. |
| Working baseline markers | **Present exactly once each**: `HOLLOW_SAINT_CATALOG_CHECKS_PASS` and `HOLLOW_SAINT_BODY_STARTED nativeInput=True nativeMotor=True authority=True`. Log mtime 21:53:46 PT, matches the handoff. |
| Controller contents | **1 layer named `Body`, 15 authored states**, 10 bool + 10 float params declared but **no transitions and no blend tree** — states are unreachable except `Idle` (the only `defaultState`). |
| Clip gap | **15 of 65** source clips are in the game bundle. Confirmed against `art/anim/v31/catalog.json`. |
| Renderer invariant | `FoundationAudit.cs:32` hard-asserts `renderers.Length == 140` (137 source + 3 from the 4-way body split). Any model-structure change breaks startup unless this assert changes in the same build. |
| Git state | 198 changed paths (113 M, 30 D, 55 untracked), **0 commits** for the entire integration pass. `HollowSaintUnityProject/` (796 MB) and `HollowSaintMod/` are **entirely untracked**. `git lfs status` runs clean now. |
| Disk | 258 GB free on `C:`. Not a constraint. |

## Root cause found: the mannequin crash

The handoff says `HollowSaintDisplay` "lacks `ModelSkinController` and `CharacterModel`".
That is correct, and I decompiled the caller to pin the exact failure. From `RoR2.dll`,
`SurvivorMannequinSlotController.ApplyLoadoutToMannequinInstance()`:

```csharp
ModelSkinController componentInChildren = mannequinInstanceTransform.GetComponentInChildren<ModelSkinController>();
componentInChildren.StartCoroutine(componentInChildren.ApplySkinAsync(skinIndex, AsyncReferenceHandleUnloadType.OnSceneUnload));
```

`GetComponentInChildren<ModelSkinController>()` returns **null** and the very next line
dereferences it. Vanilla never hits this because every survivor display prefab ships a
`ModelSkinController`. Ours does not, because `FoundationContent.cs:23` builds the display
as a raw clone of the bundle model and attaches only materials and `FoundationPresentation` —
`CharacterModel` and `ModelSkinController` are added in `FoundationBody.cs`, which only runs
for the playable body.

**This is a pure content-assembly omission, not an engine or API problem, and it needs no
Unity rebuild** — the fix is entirely in the plugin.

Two traps when fixing it:

1. `ModelSkinController` is `[RequireComponent(typeof(CharacterModel))]`, and its
   `ApplySkinAsync` ends with `characterModel.forceUpdate = true`. A `ModelSkinController`
   added without a `CharacterModel` just moves the NRE one line later.
2. The display is instantiated standalone with no `CharacterBody` parent, so
   `characterModel.body` will be null. `ApplySkinAsync` itself does not touch `.body`
   (verified in the decompile), but `CharacterModel`'s own update path may. Needs a
   deliberate decision, not a shrug.

**The right fix is to share one display-construction path between body and mannequin**, not to
copy-paste the body code. Both need identical `SkinDef` + `RendererInfo` data or the
mannequin will render differently from the in-game model.

## Plan

Sequenced so each phase is independently verifiable and each ends at a playtest gate.
**Phases 1 and 2 need no Unity work and no new bundle** — they are plugin-only.

### Phase 0 — Rollback point (do this before any edit)

The whole integration pass is uncommitted, and `HollowSaintUnityProject/` has never been
tracked. One bad edit to a 796 MB untracked tree is unrecoverable. `docs/next-agent-handoff-20260927.md:68`
also records a Git LFS access-denied error on this repo, so this needs care.

Options, in order of preference:
- **Local commit of just the plugin + docs** (`HollowSaintMod/`, `docs/`, `HANDOFF.md`) — small,
  fast, no LFS involvement, gives a real rollback point for everything Phase 1–2 touches.
- Leave the Unity project untracked and rely on the existing `artifacts/foundation/profile-backup-*`
  plus a fresh copy of `GameFoundation01/`.
- Full commit including Unity — **needs your explicit OK.** ~796 MB, first time adding LFS-tracked
  media, and the repo already has a history of LFS process hangs.

**No push either way.** Nothing goes to GitHub without you naming it.

### Phase 1 — Fix the mannequin (highest value, smallest diff)

Plugin-only. No Unity, no bundle rebuild, no new assets.

- Refactor the `CharacterModel` + `ModelSkinController` + `SkinDef` construction out of
  `FoundationBody.cs` into a shared helper (e.g. `FoundationSkin.cs`) used by both paths.
- Call it for the display in `FoundationContent.cs:23`, so the mannequin gets a real
  `ModelSkinController` and the `GetComponentInChildren` lookup succeeds.
- Decide and document the `characterModel.body == null` case for the standalone display.
- Keep the rear-shell fix intact — it came from the 4-way body mesh split in
  `FoundationMeshSplitter.cs`, which is unrelated to skins. Do not touch it.
- Extend `FoundationAudit.cs` to assert the display also has both components and a valid
  skin array. **Update the startup assert in the same build** (learned lesson, handoff line 66).

**Gate:** build green → you launch via r2modman → zero `ApplyLoadoutToMannequinInstance`
null refs, and the select screen shows Hollow Saint intact from front and rear.

### Phase 2 — Close the animation contract

The 5 warning pairs are the whole story: 5 bad requests, each producing one
`GotoState` failure plus one `Invalid Layer Index '-1'`. That pairing means a caller is
doing `GetLayerIndex(...)` → `-1` → `SetLayerWeight(-1, ...)` or `CrossFade(..., -1)`.

Diagnosis before fix, as the handoff insists:
- Instrument rather than guess. Add a temporary log in `FoundationPresentation.Start()` that
  dumps `animator.layerCount`, every layer name, and every state name/hash, so the next log
  names the exact states being requested instead of us inferring them.
- Expect the culprits to be the temporary Commando skills asking for vanilla state names
  (`Primary1`, `Secondary`, `Utility`, `Dash`, `Death`, `Spawn`) that do not exist in a
  controller with one `Body` layer and 15 Hollow Saint states.

Two candidate fixes, and the choice is a real tradeoff:
- **(a) Narrow the callers** — stop the temporary Commando skills from requesting animation at
  all. Smallest diff, warnings gone immediately, but the character animates only via
  `FoundationPresentation` until the real kit lands.
- **(b) Supply the states** — add a compatibility layer with the vanilla state names the
  temporary skills need. No warnings *and* vanilla skills animate, but it hard-codes Commando
  assumptions into a controller we will later replace with the real Arc Bolt / Spear / Step
  contract. The handoff explicitly warns against adding aliases blindly.

I lean **(a) for M1**, because the real skills replace the Commando placeholders in M3 and
(b) is throwaway work. Your call.

**Gate:** zero Hollow Saint-caused animator state/layer warnings across select, move,
attack, jump, sprint.

### Phase 3 — Focused M1 playtest (yours)

One session. The handoff and `docs/controller-acceptance.md` both require a **physical**
controller pass — the 8BitDo appearing in the first log is not evidence.

Walk the `docs/controller-acceptance.md` rows: selection + loadout navigation, partial
stick, circles/figure-eight/reversals, camera, sprint, jump/interact/equipment, device
swap, rebinding. Then movement, damage, death, respawn. Record which rows actually
happened — not just "the game loaded".

**I do not launch the game or drive game UI.** You do that through r2modman. I stage the
DLL, hand you the hash, and read the fresh log after.

### Phase 4 — M2: real model and animation contract

The big lift, and it needs Unity.

- Import the **remaining 50 of 65 clips** into a new numbered output
  (`GameFoundation02/` — the builder at `FoundationBundleBuilder.cs:34` *refuses* to
  overwrite `GameFoundation01`, by design).
- Replace the 15 unreachable states with a real graph: 8-direction locomotion blend tree,
  glide layer, air/land, masked upper-body attack layer (`spine` + descendants), additive
  aim, halo, stun/freeze/death, interruption.
- Wire `FoundationPresentation` to drive those parameters instead of `CrossFade`-ing a single
  layer-0 state.
- **Keep the 4-slot body split and the 140-renderer invariant.** Any derived export or
  material consolidation must preserve it or replace it with something equally verified.
- Decide the real shader path. `FoundationMaterials.cs` is an explicit diagnostic —
  it forces rear graphite opaque/dark and caps emission at 0.9 — and `ignoreOverlays = true`
  is set on every renderer. That is why it looks flat. Compare against the real materials
  in game before deciding to remove it.

Source-side caveat: strict full QA is **15/65**, not sign-off. FBX import is not animation
approval. The tabard is baked restrained follow-through, not dynamic cloth.

### Phase 5 — M3: Arc Bolt as the first vertical combat slice

One complete ability before any others, per the integration plan:
native skill input, aimed initial hit, bounded chains, damage provenance,
**server-owned** charge on successful hit only, Discharge consume-on-next-hit.

Test explicitly: misses and item procs cannot fill charge; no target is hit twice; a full
meter cannot recursively trigger twice. Check host *and* client early — this is where
networking bugs are cheapest to fix.

Get hand flash, bolt, one chain, impact, sound and meter feedback coherent before touching
Spear, four-way Step, or Circuit.

### Phase 6 — M4/M5/M6

Remaining kit, then VFX/SFX wiring (v07 source exists, unwired), then item display fitting
on the 23 existing locators — mounts alone are not display rules, and modded items need an
explicit unsupported-display report. Then the full acceptance matrix, host/client,
cooldown/attack-speed extremes, death/revive, sustained VFX load.

## What I need from you

1. **Rollback point** — local plugin+docs commit only (my recommendation), or include Unity?
2. **Animation fix** — (a) narrow the Commando callers, or (b) supply vanilla-compatible states?
3. **The open proposals** still labelled undecided in the handoff, whenever you feel like
   settling them: Arc Step i-frames (default none?), heel jets on jump/land, whether Open
   Circuit can strike during glide or Arc Step.
4. **Anything you want to plug in** — Unity MCP or computer use if editor work gets blocked
   (neither is needed for Phases 1–3), or context I have not asked for.

## Constraints I am holding to

- **You launch and playtest. I build and stage.** No game automation, no UI driving.
- **`Hollow Saint Dev` profile only.** `demo time new` and `demo time` stay untouched
  references.
- **Local only.** No commits, pushes, profile exports or Thunderstore release without
  your explicit go.
- **No `KeyCode`, no `Input.GetKey`, no joystick APIs.** Native `CharacterMotor`,
  `InputBankTest` and `SkillLocator` are retained; animation *reads* movement, never
  normalizes or replaces it. Keyboard/mouse parity is required alongside controller.
- **Preserve the Blender sources and your open Unity/Blender sessions.** Numbered saves,
  never overwrite; superseded files go to `_old/`.
- **Never launch via `tools/dev-profile/Start-Foundation.ps1`.**
- Not a finished playable survivor, and I will not describe it as one: the final
  Arc Bolt / Conduit Spear / Arc Step / Open Circuit / Discharge kit is not implemented.
