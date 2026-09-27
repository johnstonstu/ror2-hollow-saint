# Next run notes (written Sep 27 ~12:40 AM PT)

run3's prompt (refine-prompt-run3.txt) only says to read REFINEMENT-PLAN.md FIRST; it does not tell the agent to re-read the plan between items, so run3 may not pick up sections added after it launched (11:42 PM PT).

For the NEXT launch, include:
- **9f - Hand orientation on extended arms (REFINEMENT-PLAN.md, added 12:32 AM Sun) - MUST FIX, standing rule.** Extended arms: thumb up, palm forward, fingers curl in, hands mirrored. Fix via forearm/upper-arm roll, not wrist kinks. Add the automatic thumb-up / palm-forward check to hand QA.
- **Order: 9f must be done (with 9d+9e hands) BEFORE 10 EXPANDED.** If run3 has already started 10 EXPANDED without 9f, do 9f first in the next run and re-check the transitions after.
- v16 audit results: art/anim/wip/audit/ORIENT-AUDIT.md.
- Add to the prompt: "Re-read art/anim/REFINEMENT-PLAN.md before starting each item; Stuart appends new feedback while you run."

## Order update (Sep 27 ~12:45 AM PT) — PROPOSED, awaiting Stuart's approval
Stuart wants to approve the consolidated plan (`art/MASTER-PLAN-DRAFT.md`) before new work starts. If approved, the order is:
1. 9d+9e (in progress, run3)
2. 9f+9g
3. 9h (eight-direction locomotion, directional heel jets, multi-direction Arc Step)
4. 11 (Conduit Spear)
5. 10 EXPANDED LAST (all new clips, stick-circle smoothness, Discharge overlay over every state, skill cancels)

The next anim run MUST re-read `art/anim/REFINEMENT-PLAN.md` and `art/anim/wip/NEXT-RUN-NOTES.md` before starting EACH item, because Stuart appends feedback while runs are going. Put that sentence in the prompt.
- **Order (12:45 AM PT): 9g (shoulders/pauldrons, REFINEMENT-PLAN 9g; audit in art/anim/wip/audit-shoulder/SHOULDER-AUDIT.md) and 9f (hand orientation) come BEFORE 10 EXPANDED.** Discharge is now a PASSIVE (auto-fires at 100% charge on the next enemy hit) played as an upper-body/additive overlay, so 10 EXPANDED must include Discharge firing over run, glide, air and mid-cast states as transition cases.
- **9f: verify hand layout FIRST (art/anim/wip/audit/ORIENT-AUDIT.md).** The audit found thumbs pointing down on extended arms in Charge full, Open Circuit, Jump, Arc Bolt, Arc Step end, Discharge and Select intro, plus evidence the hand rigs may be mirrored left-to-right (index finger at the back and little finger at the front at rest; fingers curl against the palm side). The next run must first check the hand/finger layout by eye in close-up renders. If mirrored: fix the finger/thumb layout or the auto-curl direction at rig level, THEN correct forearm roll, THEN re-check every clip. Forearm roll alone won't satisfy the rule.
- **Phase A = 9f + 9g + 9i, before 9h (Stuart, 1:03 AM Sun, approved).** 9i = full-body audit fixes (REFINEMENT-PLAN 9i; report art/anim/wip/audit-full/FULL-AUDIT.md): tabard clipping hidden by the Blender-only Shrinkwrap (QA with Shrinkwrap off), neck/back hardware, chest part tearing, halo arcs in the shoulders, L elbow into the hip, R arm bones misfit (fix before the 9f roll work), twist bones, pops, symmetry. Add the new permanent QA: all-pairs contact, joint sanity, pops, foot slide/penetration, symmetry.
