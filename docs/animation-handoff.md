# Hollow Saint: animation handoff brief

For the agent starting locomotion. Written 2026-09-26. Read `docs/animation-plan.md` for
the full clip list and pipeline; this page is the starting state and the ground rules.

## Start file

`art/hybrid/hollow-saint-hybrid-v18.blend` (visuals signed off for animation start).
Do not edit it in place: save your work as a new file (e.g. `hollow-saint-anim-v1.blend`)
built by a script under `tools/blender/`. Never overwrite any existing `.blend` checkpoint.

## Safety rules (the user runs other projects on this machine)

- Blender only as `blender --background --factory-startup --python-exit-code 1 --python ...`.
- Never touch the interactive Blender instance (PID 4500 at time of writing), or any Unity,
  Node or MCP session belonging to another project. Do not kill processes you did not start.
- Renders: Cycles OPTIX GPU works; keep review renders modest (1100x1400).

## Rig facts (`Hollow Saint | v8 rig`, 79 bones, 56 deform, 24 fps, character faces -Y)

- Spine `pelvis > spine > chest > neck > head`; `root` at origin (non-deform, use for
  root motion if needed).
- Legs `thigh > shin > foot > toe` (digitigrade; knee z 0.70, ankle z 0.16). IK:
  `L/R foot IK` + `L/R knee pole` drive the `shin` IK and `foot` copy-rotation constraints.
- Arms `scapula > upperarm > forearm > hand` + fingers. IK: `L/R hand IK` + `L/R elbow pole`.
- **All IK constraints ship at influence 0.** Turn them up while keying, then bake to the
  deform bones; exported clips must be FK on deform bones only.
- `L/R pauldron`: copy 40% of upper-arm rotation; shells are rigid on these bones.
- Tabard chains `tabard front.1-3`, `tabard back.1-2`; hand-key swing (no simulation).
  Tabard overlays have a Shrinkwrap (outside) so cloth doesn't sink into the body.
- Halo: `halo root` (child of `chest`) > `halo 1-4` (non-deform; arcs are bone-parented).
  User wants **small lag/sway** on `halo root` as follow-through.
- Sockets (non-deform): `L/R muzzle`, `core socket`, `halo socket`, `head socket`,
  `L/R heel socket`.
- Existing clips: six `HS_v10 | ...` studies (idle, Arc Bolt, Arc Step, Charge, Discharge,
  Open Circuit). Keep them playing; `HS_v8` actions are old and can be ignored.

## First task (user decisions)

Locomotion block-out on v18: idle to run forward, then walk, then sprint turning into a
**low glide** (run -> glide-enter -> glide loop -> glide-exit; feet leave the ground, legs
trail, tabard streams back). Death is **ragdoll** in Unity (no clip). Review as hero/side
renders or short turntables, like `art/hybrid/v10`.

## Known limitations

- Shoulders crumple in high arm raises (shoulder/elbow correctives are deferred until
  locomotion poses show where the fused mesh tears).
- Rigid pauldrons can sit up to ~7 cm off the skin in big arm poses; check silhouettes.
- No export or in-game test has been done yet.

## QA tools

- `HS_VERSION=v18 blender ... <file> --python tools/blender/review_hybrid_v11.py`:
  weight sums, non-finite vertices, overlay surface drift per `HS_v10` clip. Tag new
  objects with a version key if you add any. Set `HS_VERSION` to the highest tag present.
- `tools/compare_concept_sheets.py`: concept vs render sheets.
