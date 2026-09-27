# Hybrid v18: short concept follow-up

Same script as v17 (`tools/blender/build_hybrid_v17.py`, output name from `HS_OUT`, default
`v18`), built from v16. Differences from v17:

- Head lift 11 cm (was 9): longer neck.
- Pauldron caps tilt 30 deg (was 16), 6% longer, wrap slightly further: they now angle down
  over the deltoid like the concept.
- Larger upper-chest plates; both rib plates build (outline points slide inward until they
  land on front-facing skin; right torso mesh has a gap near dx .09, z 1.33).

QA (`HS_VERSION=v18`): PASS; surface drift identical to v17 (max 11.6 mm, abdomen segment 2 in
Arc Step; others 1.2-7.0 mm). Review: `v18/review.html`. This is the animation start file; see
`docs/animation-handoff.md`.
