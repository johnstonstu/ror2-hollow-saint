# Hollow Saint: animation plan

Planning document, 2026-09-26. Builds on the [playable roadmap](playable-character-roadmap.md)
section 4 and the six `HS_v10` studies. No export or game work is implied yet.

## What the rig is today

From the read-only audit (`tools/blender/audit_rig_animation_readiness.py`,
output in `art/hybrid/v12/rig-audit.json`):

- 54 bones, 47 deforming, one non-deforming `root` at the origin, unit scale.
- Spine chain: `pelvis > spine > chest > neck > head`; arms with `scapula`,
  `upperarm`, `forearm`, `hand` and three-joint fingers and thumb.
- Legs: `thigh > shin > foot` only. There is no toe bone for the digitigrade claw
  feet, and the foot bones point toward +Y (behind the character, which faces −Y).
  Confirm whether that is intentional heel geometry or needs re-orienting before
  any walk cycle.
- Four `halo` bones carry the arcs, seams and inlays; Open Circuit animates them.
- No IK, no constraints, no control bones: every clip is hand-keyed forward
  kinematics on deform bones.
- No tabard bones; the front and back cloth follow pelvis/thigh weights.
- Pauldrons (v13+) are skinned shells blended from chest and upper-arm weights.

## What Risk of Rain 2 needs from a survivor

A survivor's Animator combines a locomotion layer with upper-body gesture layers,
so the character can run while casting. The game drives it from movement and aim
state. The exact parameter names should be copied from a current working
survivor template when the Unity project exists; the clip set below is the stable part.

| Group | Clips | Notes for Hollow Saint |
|---|---|---|
| Idle | idle loop, idle-in-combat variant (optional) | v10 "contained storm" idle is the base |
| Locomotion | run forward/back/left/right (blend tree), sprint | biggest missing set; style decision below |
| Air | jump, ascend, descend loop, land (soft/hard) | halo can trail slightly on descend |
| Aim | aim pitch and yaw poses (additive) | spine/neck/head only so casting stays aimed |
| Primary | Arc Bolt one-shot, left/right hand alternation | v10 snap is a start; needs a mirrored version |
| Charge / discharge | charge loop, full-charge hold, discharge | v10 studies exist; core/halo pulses via material |
| Utility | Arc Step dash start/loop/end | v10 is in-place; game moves the body |
| Special | Open Circuit crown unfold | v10 study exists |
| Presentation | spawn/intro, character-select idle, death (or ragdoll) | select idle can show the halo turning |

Every gesture clip must read correctly layered over running, and its release frame
must line up with the gameplay hit at any attack speed (clips get sped up).

## Status: v16 rig built (2026-09-26)

Items 1–5 below are done in `art/hybrid/hollow-saint-hybrid-v16.blend`; see
`art/hybrid/README-v15-v16-rig.md`. The audit also found the leg bones were not fitted
to the mesh (knee 12 cm low); they are now refit. Correctives (item 6) remain open;
the shoulder crumples in high arm raises. Next: the locomotion block-out below.

v17/v18 (`art/hybrid/README-v17.md`, `README-v18.md`) keep this rig but move `head`,
`head socket` and the halo bones up (11 cm in v18) and lengthen `neck`. Build locomotion on
v18; start from `docs/animation-handoff.md`.

## Rig upgrade before locomotion (originally proposed as v15)

1. Add a toe bone per foot and re-orient the foot chain; foot roll matters for
   the claw feet.
2. Add leg IK (foot controller + knee pole) and arm IK with FK/IK switching as
   control bones. They stay out of export; clips are baked to the deform bones.
3. Give each pauldron its own bone (child of scapula/upper arm) and rigid-parent
   the shells. That replaces weight blending with predictable, clip-free motion.
4. Add two- or three-bone chains for the front and back tabard, so run cycles can
   swing the cloth away from the legs (hand-keyed first, no simulation).
5. Add socket bones or empties: fingertip muzzles (both index fingers), core,
   halo centre, head. Gameplay code and effects attach here.
6. Shoulder and elbow correctives only after locomotion poses show where the
   fused mesh tears.

## Pipeline

Blender actions (controls) → bake to deform bones → FBX (deform bones only, unit
scale, −Y forward converted to Unity +Z) → Unity 2021.3 project matching the game
version → Animator controller with locomotion blend tree and gesture layers →
AssetBundle loaded by the plugin. Validate with one tiny clip first, per the roadmap.

## Order of work

1. Rig upgrade above; re-verify the six `HS_v10` clips still play (retarget by
   re-keying if bone orientations change).
2. Locomotion block-out: idle ↔ run forward, then the four-direction blend and
   sprint. Review as turntable and motion-player renders like v10.
3. Air set and landing.
4. Aim additive poses, then rework Arc Bolt as a gesture layered over running.
5. Charge/discharge, Arc Step and Open Circuit as gestures; death and spawn.
6. Export test, then in-game prototype per the roadmap.

## User decisions (2026-09-26)

- **Locomotion: hybrid.** Walk/run on the claw feet normally; sprint transitions
  into a low glide (feet leave the ground, legs trail, tabard streams back).
  Needs run → glide-enter → glide loop → glide-exit clips and a sprint-only blend.
- **Halo lags and sways** with motion. Implement as a halo root bone whose
  motion is keyed as follow-through (or a damped runtime component in Unity).
  Keep the lag small so the silhouette stays readable.
- **Death: ragdoll**, like most survivors. Requires ragdoll colliders/joints on
  the main bones in Unity; no authored death clip.
- **Next: small visual touch-ups, then the v15 rig upgrade.**
