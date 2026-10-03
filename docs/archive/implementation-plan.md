# Hollow Saint implementation plan

Status: active milestone plan, 2026-09-26. The user rejected the initial blockout
and explicitly authorized repeated visual refinement passes against selected A.
That supersedes the initial pause before detailed visual modeling. Current work
is the local Blender fidelity loop, documented in model-fidelity-review.md.
Rigging and game integration remain later milestones; no playable prototype exists.

## Scope and decisions

- Preserve selected A / Cracked Icon: slender ivory ceramic body, blank mask,
  dark joints, cyan core, segmented copper halo, and dark hip tabard.
- Confirmed mechanics: aimed primary, automatic chains after initial hit,
  successful hits build charge that strengthens chains.
- Conduit Spear, Arc Step, Open Circuit, charge release policy, and all balance
  values remain proposals. Earlier numerical criteria are hypotheses.
- Reserve paid Higgsfield use for useful capabilities unavailable through
  ChatGPT and local tools. Consider mesh/animation/VFX/SFX only after checking
  actual capability and cost. Do not spend credits on ordinary reference sheets.
- No release, account purchase, trial activation, or modification of existing
  game/mod profiles. No credential logging, copying, or packaging.

## 1. Environment and isolated development setup

1. Inspect game metadata and API signatures without running unknown mod builds.
2. Record installed Unity engine and editor separately; choose a compatible
   asset-bundle editor only after checking current community guidance.
3. Verify .NET and reference assemblies, then pin exact compatible dependencies.
4. Identify existing r2modman profiles. Prepare a new Hollow Saint Dev profile;
   do not clone a large gameplay modpack or switch the user's selected profile.
5. Verify Blender connection; build in an isolated process to preserve the open
   scene. Check Higgsfield connector and direct API separately without generation.

Success: evidence and exact remaining setup steps recorded. A catalog listing
does not prove account access; an SDK installed does not prove a playable mod.

## 2. Custom blockout and visual review

Use original LEFT A and detail sheet for identity; use orthographic studies for
proportion guidance, not exact geometry. Construct editable named pieces:

- One halo with four independent arcs; current passes shorten the lower two
  to follow selected LEFT A's open bottom. The full ring was an earlier proposal.
- Narrow dark yoke on upper back; lower halo arcs sit behind shoulder blades.
- Compact mirrored ivory shoulder caps with visible dark joints.
- Five articulated, tapered dark digits per hand.
- Separate narrow front/back tabard panels extending to mid-shin as in LEFT A.
- Small cyan rear node for player-camera readability.

These resolve concept contradictions provisionally. Save an editable .blend,
reproducible build script, matched front/side/back renders, and a simulated
gameplay-distance rear view. Inspect silhouette, halo clearance, tabard length,
and cyan visibility before requesting review. No rig or game-ready claim.

## 3. Playable primary prototype

Using the environment findings, build a small original C# survivor
foundation; use permitted local placeholder visuals initially. Keep combat
logic separate from art loading and supporting abilities.

1. Register a separate survivor/body and primary state without globally
   modifying its placeholder source survivor.
2. Server owns hit validation, finite chain target selection, damage and charge.
3. Deduplicate targets by health-bearing entity, not hurtbox; bound chain count,
   candidate search and proc behavior. Exclude dead/allied/obstructed targets.
4. Charge only from this skill's successful hits. Synchronize visible charge;
   do not let item damage recursively generate the mechanic.
5. Keep isolated-target damage and charge useful. Obtain charge-release choice
   before treating an automatic or manual spending rule as settled design.
6. Put provisional tuning in explicit configuration; pin dependency versions
   after inspecting local API compatibility. Build output stays local.

Acceptance criteria defined before tests: a cast cannot revisit an entity or
exceed its configured cap; misses and item-only damage add no charge; range and
obstacles affect eligibility; clients cannot independently apply damage; charge
display matches authoritative values. Configured damage/attack speed scaling
must be measurable. Verify crowd, isolated enemy, on-hit items, death and stage
transitions. Record actual observations separately from automated unit checks.
Two-player host/client testing is required before claiming multiplayer tested.

## 4. Refine after reviews

Refine topology, rig, locomotion/casting animations, Unity materials, bounded
effects and audio. Prototype supporting abilities only as explicitly identified
experiments until approved. Validate custom assets in the compatible Unity
workflow and in game; generated meshes and auto-rigs require inspection.

## Ownership during this inspection

- Primary agent: docs, handoff, direct API inspection, integration decisions.
- Compatibility agent: read-only game/API/dependency research.
- Modeling agent: tools/blender and art/blockout; no shared docs edits.
- Profile agent: tools/dev-profile and isolated development-profile preparation.

Each milestone records outputs, validation, limitations and exact next actions
in HANDOFF.md. This plan does not establish any proposed gameplay as approved.
