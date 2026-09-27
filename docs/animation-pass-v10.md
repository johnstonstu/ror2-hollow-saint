# v10 animation pass

Plan: preserve v7–v9, repair rig attachment/pivot issues in a new v10 file,
author six editable animation studies, and deliver a scrub/play review gallery.
Supporting ability motions are visual proposals, not approved gameplay rules.

Success criteria established before implementation:

- Six named, retained actions: idle, Arc Bolt, charge loop, discharge, Arc Step,
  and Open Circuit. Each contains visible motion and labeled timing events.
- All pose locations, rotations, and scales are keyed at every authored pose so
  switching clips cannot retain another clip's translations.
- Idle and charge have matching endpoint transforms; one-shot actions recover
  to their intended neutral pose.
- Cameras, studio lights, and ground remain stationary throughout animation.
- Finger controls pivot at the modeled segment joints. Halo lips/bands follow
  their corresponding segment controls.
- Every sampled body vertex is finite; weights sum to one. Inspect rendered
  front/side/hero extremes for tearing, clipping, and unintended motion.
- Reopen the saved file and rerun numerical checks. Compare source file hashes
  to prove v7–v9 remain unchanged.

Scope: Blender animation study and local review. Game export, Unity integration,
collision-aware dash movement, VFX, and production retopology are later work.
