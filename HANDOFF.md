# Hollow Saint handoff

Updated 2026-09-26. Selected character remains **Hollow Saint A / Cracked Icon**.
Do not restart character selection. Names and supporting abilities are proposals.

## Current milestone

READY FOR ANIMATION: [Hybrid v18](art/hybrid/hollow-saint-hybrid-v18.blend) is the
animation start file (longer neck, pauldrons angled over the deltoid, larger chest plates).
Brief for the animation agent: [docs/animation-handoff.md](docs/animation-handoff.md).
[v18 review](art/hybrid/v18/review.html), [notes](art/hybrid/README-v18.md).

PREVIOUS VISUAL PASS: user asked for all concept gap fixes. [Hybrid v17](art/hybrid/hollow-saint-hybrid-v17.blend)
raises the head 9 cm on a cabled neck (head/halo bones moved), adds ear sockets, a recessed
core housing with layered chest plates and segmented abdomen, faceted swept pauldrons, a halo
yoke, a slimmer waist and crisper shading, on the v16 rig. Drift QA passes.
[v17 review](art/hybrid/v17/review.html), [notes](art/hybrid/README-v17.md).

PREVIOUS RIG PASS: user decisions recorded in docs/animation-plan.md (walk/run with
sprint turning into a glide, halo lags and sways, ragdoll death). [Hybrid v15](art/hybrid/hollow-saint-hybrid-v15.blend)
adds forearm conductors and matches pauldron tone. [Hybrid v16](art/hybrid/hollow-saint-hybrid-v16.blend)
is the animation-ready rig: leg chain refit to the digitigrade mesh (old knee was 12 cm off),
toe bones, leg/arm IK (influence 0), pauldron bones, tabard chains, halo root, effect sockets.
The six HS_v10 clips deform the body identically to v15. [v16 review](art/hybrid/v16/review.html),
[notes](art/hybrid/README-v15-v16-rig.md). Next: locomotion block-out (walk/run, then glide).

V13-V14 POLISH PASSES: user liked v12 direction, asked for thicker plates and more
polish rounds, and to start planning animation. [Hybrid v13](art/hybrid/hollow-saint-hybrid-v13.blend)
rebuilt the pauldrons as thicker faceted plates with a stacked under-plate, calmed the
core and added bloom; [hybrid v14](art/hybrid/hollow-saint-hybrid-v14.blend) moved the
palette to the concept (warm bone ivory, deep red-brown copper, cyan core glow).
[v14 review](art/hybrid/v14/review.html), [comparison](art/hybrid/v14/comparison.jpg),
[notes](art/hybrid/README-v13-v14-polish.md). QA PASS for both. Animation planning:
[docs/animation-plan.md](docs/animation-plan.md) (rig audit, RoR2 clip set, proposed
v15 rig upgrade, open decisions). v1–v12 unchanged.

V12 PAULDRON PASS: [hybrid v12](art/hybrid/hollow-saint-hybrid-v12.blend) is
built from v11 and adds broad ivory ceramic pauldrons (procedural ellipsoid shells
sized from the measured shoulder caps, tilted outward, bevelled plate edges),
skinned to chest/upper-arm weights. [Review page](art/hybrid/v12/review.html),
[comparison sheet](art/hybrid/v12/comparison.jpg),
[notes](art/hybrid/README-v12-pauldrons.md). QA PASS (decals unchanged; pauldron
drift 17 mm max at Arc Step, no visible clipping in pose renders). v1–v10 hashes
unchanged; v11 not re-saved. Awaiting user review of v11 + v12.

V11 FIDELITY PASS: user confirmed LEFT A is the target, preferred the extra v9
detail, and asked for a fidelity/rendering pass toward the concept images before
more animation. [Hybrid v11](art/hybrid/hollow-saint-hybrid-v11.blend) is built
from v10 (rig and six actions retained): thick aged-copper halo with block seams
and cyan gap seams, geometric emissive mask/core/neck/chest/spine conductors,
dark copper-trimmed tabard with sigil, fingertip lights, emissive painted cyan,
Cycles studio. [Review page](art/hybrid/v11/review.html),
[comparison sheet](art/hybrid/v11/comparison.jpg),
[notes](art/hybrid/README-v11-fidelity.md). Saved-file QA PASS across all
HS_v10 actions; v1–v10 hashes unchanged. Awaiting user review of v11. All work
used background Blender only; the live Blender MCP session belongs to a
separate project and was not touched.

PREVIOUS ANIMATION PASS: user requested pushing the rig toward custom animations.
[Hybrid v10](art/hybrid/hollow-saint-hybrid-v10.blend) adds six editable studies:
idle, Arc Bolt, charge loop, discharge, in-place Arc Step, and unfolding crown.
[Motion player](art/hybrid/v10-animation/review.html) and
[rig/animation notes](art/hybrid/README-v10-animation.md). Corrected studio bone
parenting, fitted finger pivots, recentered halo controls, and replaced hand-to-leg
weight contamination with topology-aware arm isolation and regional weights.
All v7–v9 source hashes are unchanged. Saved-file numerical QA covers 270 frames;
rendered hero/side poses and motion previews remain art review evidence only.
Fused shoulder/elbow deformation, cloth, locomotion, VFX, export and game tests
remain outstanding. Supporting skills and animation timings are proposals.

LATEST USER REVIEW: v1 was rejected as visually too far from the concepts.
The subsequent refinement goal authorized repeated modeling/render/review passes.
Hybrid v7 now meets the visual likeness milestone after actual five-view review
and saved-scene checks. This completes this visual pass, not the survivor project.
Preserve the original and intermediate passes for comparison.

LATEST VISUAL FEEDBACK: user says the model is starting to look pretty good
and is entering final polishing passes. They specifically identify the hands
as rotated awkwardly. Preserve the improving body/proportions; finish natural
hand-to-forearm alignment and relaxed palm orientation, plus remaining local
shoulder/material cleanup. Do not restart character or body design.

**Current model: [hybrid v7](art/hybrid/hollow-saint-hybrid-v7.blend).**
Hands now follow the forearms with relaxed inward palm rotation. Rear shoulder
seams are cleaned, the duplicate generated rear chest is corrected, and the
halo comprises four separate arcs. [Five-view comparison](art/hybrid/review.html)
and [deliverable notes](art/hybrid/README.md). User feedback endorsed the body
direction; exact v7 acceptance has not yet been given.

LATEST REQUEST: user asked to try rigging the render and making animations with
Higgsfield. The connected Higgsfield catalog exposes a Meshy rigging model, but
the rigging job submission tool is not available, and no matching 3D Jutsu
project exists. The only listed Higgsfield 3D project is unrelated. A local
first-pass Blender study is saved separately as
[hybrid v8](art/hybrid/hollow-saint-hybrid-v8.blend), with three editable actions
and [review notes](art/hybrid/README-v8-rig.md). Keep v7 as the visual checkpoint;
v8's fused-mesh weights and deformation are provisional and not game validated.

LATEST REAR FEEDBACK: user wants the rear halo/shoulder transition closer to the
original Higgsfield version. The latest small candidate is
[hybrid v9](art/hybrid/hollow-saint-hybrid-v9.blend): two dark diagonal yoke
branches were removed, and pale-cyan inlay bands now cross the side arcs at
shoulder height. The v8 rig and actions remain in the v9 file. Compare
[rear](art/hybrid/v9-review-back.png) and [side](art/hybrid/v9-review-side.png);
this candidate is not yet user-approved.

Local Blender-only refinement reached pass v6. Environment inspection and
[implementation plan](docs/implementation-plan.md) are recorded. Original v1 is
preserved but rejected. See [comparison gallery](art/refinement/review.html) and
[visual review evidence](docs/model-fidelity-review.md). No C# survivor, Unity
project, or playable build exists. The first v8 rig and actions are an unapproved
deformation study, not an in-game asset.

LATEST USER STEERING: another agent produced a Higgsfield SAM3D GLB, supplied at
output/higgsfield-hollow-saint/hollow-saint-sam3d.glb. It was inspected and rendered
in the same studio as v6. [Direct comparison](art/comparison/review.html) and
[findings/next pass](art/comparison/comparison.md). Recommendation is to use the
HF body as the stronger visual base, adapting local separate halo/hand/back
construction. **Hybrid v7 is the current visual review deliverable**, with source
scripts, a full source-v7 snapshot and actual views in art/hybrid/. Earlier cut,
hand-pose and rear seam issues were corrected. Original GLB preserved. See
[hybrid audit](docs/hybrid-model-review.md) and [hybrid comparison](art/hybrid/review.html).

Reserve Higgsfield spending for useful capabilities unavailable through
ChatGPT/local tools. User asked to investigate a 3D starter mesh, then chose
**Continue with local Blender for now** when the website fallback needed login.
Do not resume paid generation automatically. No Higgsfield job was submitted,
no reference media uploaded, and no generation credits spent this continuation.

## Confirmed design and gameplay

- Selected LEFT A in art/concepts/hollow-saint-variations-v1.png: slender ivory
  ceramic body, blank mask, dark joints, small cyan core, segmented copper halo,
  dark hip tabard.
- Aimed left-click lightning with automatic chains after initial hit.
- Successful hits build charge that strengthens chains.
- r2modman is the mod manager; preserve existing game and profile setup.

Conduit Spear, Arc Step, Open Circuit, automatic/manual charge release, charge
gain rules, locomotion style, and **all numerical gameplay balance values**
remain proposals. Earlier numerical criteria are untested hypotheses.

## Current hybrid deliverables

- Editable scene: art/hybrid/hollow-saint-hybrid-v7.blend; five matching PNGs.
- Build: tools/blender/hybrid_build.py; dependency snapshot: art/hybrid/source-v7.
- Saved-file QA: art/hybrid/hollow-saint-hybrid-v7.qa.json, PASS.
- 66 model parts; 26,808 evaluated triangles; four arcs and five digits per hand.
- 1,226 source lower-leg/foot vertices preserved with zero displacement.
- Packed source texture; original GLB and local v6 file hashes unchanged.
- Actual front/hero/side/back/distance views inspected. Source texture blur and
  coarse deformation topology remain production limitations; no rig/game tests.

## Historical local refinement deliverables

- Editable latest: art/refinement/hollow-saint-refinement-v6.blend.
- Five actual PNG renders with matching prefix: front, hero, side, back,
  gameplay-distance. The last is a simulated Blender camera, not in game.
- All v2-v6 scenes and renders preserved; source snapshots for v4/v5/v6 only.
- Current construction: tools/blender/refine_hollow_saint.py and refine_*.py.
- Read-only reopen checks: tools/blender/verify_hollow_saint_refinement.py.
- v6 base geometry: 123 meshes, 48 curves, 5,343 vertices, 5,607 polygons.
- Saved-file QA passed: 171 parts, five cameras, four separate arcs, finite
  geometry and no linked external assets. Evaluated output: 31,994 triangles.
- Actual improvements: raised hips/knees and wider stance; bowed blank mask;
  curved chest shells and smaller core; tapered arms; curled articulated fingers;
  long draped tabard; smoother shin cutouts; split toes; fitted cyan conductors.
- Four independent halo arcs now have shortened lower sections to follow
  selected LEFT A. This replaces the earlier full-ring construction proposal.
- Build exits successfully and all five renders saved. Thumbnail-cache errors
  do not affect the saved scene or PNGs. Original interactive scene untouched.

V5 closes visible chest gaps and seats previously floating cyan accents. V6 adds
rear tendon/calf shapes, copper variation and restores the front cloth motif.
The supplied HF model is nevertheless substantially closer to LEFT A's contours.
User acceptance is not established. No rig, export or game-readiness claim.

## Historical rejected v1 blockout

- [Editable .blend](art/blockout/hollow-saint-blockout-v1.blend)
- [Front](art/blockout/hollow-saint-blockout-v1-front.png)
- [Side](art/blockout/hollow-saint-blockout-v1-side.png)
- [Back](art/blockout/hollow-saint-blockout-v1-back.png)
- [Simulated gameplay distance](art/blockout/hollow-saint-blockout-v1-gameplay-distance.png)
- [Review gallery and reproduction](art/blockout/README.md)
- Construction: tools/blender/build_hollow_saint_blockout.py
- Saved-file checks: tools/blender/verify_hollow_saint_blockout.py
- Counts: art/blockout/hollow-saint-blockout-v1-metrics.json

Original procedural geometry made in isolated background Blender. Existing
interactive scene untouched. 101 editable meshes, 2,208 vertices, 2,278 polygons,
4,012 triangle equivalents. Saved-file reopening and static checks passed:
four halo quadrants, five digits per hand, four cameras, rear cyan node, no
armature, and approximately 0.152 m mask/halo depth clearance. Four renders inspected.

No deformation, animation clearance, export, Unity bundle or game test.
Gameplay-distance image is a **simulated Blender camera view**.
Blender reported a thumbnail-cache write warning; .blend save/reopen and all
requested renders succeeded. Build script refuses live interactive execution.

## Historical v1 construction proposals (superseded by refinement)

- Approximately 2.015 m body height, 0.89 m halo diameter, 0.045 m halo thickness.
- Full four-quarter ring; lower arcs behind torso/shoulders; no duplicate ring.
- Simple dark back yoke, small rear cyan node and short spine accent.
- Compact paired ivory shoulder caps; five simple dark digits per hand.
- Separate front/rear tabard panels ending just above knee center.

These were unapproved v1 inferences. Latest passes use the selected LEFT A's long
mid-shin tabard and open lower ring, with four separate arcs. Original LEFT A
and detail sheet supply identity; 2D turnarounds are approximate.

## Environment verified

- Workspace: C:/Users/stuwj/Documents/Coding/ror2-lightning.
- No Git repository/remote, commits, or release created.
- Blender **5.2.0 LTS**, C:/Program Files/Blender Foundation/Blender 5.2/blender.exe.
  Live MCP responds; initial interactive scene was clean default Cube/Camera/Light.
- Game: C:/Program Files (x86)/Steam/steamapps/common/Risk of Rain 2.
- Local metadata: game **1.4.1**, Steam build **21587608**, engine
  **2021.3.33f1 (ee5a2aa03ab2)**. No runtime game launch yet.
- .NET SDKs **8.0.423**, **10.0.302**; netstandard reference pack **2.1.0**.
- Standard Unity Hub has only **6000.5.4f1**. Compatible asset editor and actual
  bundle-load test outstanding. Do not assume Unity 6 compatibility.
- No Unity MCP exposed. Candidate dependencies/API signatures in technical notes.
  No dependency restore or survivor build run.

Created isolated empty profile:
C:/Users/stuwj/AppData/Roaming/r2modmanPlus-local/RiskOfRain2/profiles/Hollow Saint Dev

Contains only mods.yml = []; no loader/mods installed. Existing demo time and
demo time new manifest hashes checked unchanged. No selected profile, global
settings or game files changed. Approved sandbox access used.
Helper: tools/dev-profile/New-HollowSaintDevProfile.ps1 refuses existing target.
Refresh r2modman and install selected complete dependencies into Hollow Saint Dev
only. Cached packages are observations, not a compatibility lock.

## Higgsfield and credentials

- NEW supplied asset (generated by another agent, cost not verified here):
  output/higgsfield-hollow-saint/hollow-saint-sam3d.glb. One textured mesh,
  8,108 triangles, one 1024px texture, no rig/actions/weights. Temporary weld
  analysis confirms one closed component; raw UV-seam islands are not damage.
  Fused fingers/halo, painted glow and duplicate front chest on back need repair.
  Comparison review scene/renders are in art/comparison/. No additional paid
  jobs or source uploads were performed by this continuation.

- .env is private: never print, upload, copy, package or include its contents
  in messages/handoffs. Git, Docker and npm rules exclude environment variants.
- Combined API credential passed documented **estimate-only** request: HTTP 200;
  sample SOUL v2 estimate **0.05 credits / USD 0.004**. This is not a charge,
  remaining balance, or verification of 3D access.
- Probe: tools/higgsfield/Test-ApiAccess.ps1. Reads in memory, prints allowlisted
  status/cost only, disables redirects. Approved network access succeeded.
- Connector separately reported **4.85 credits / free plan**, pending trial.
  Do not equate that with direct API balance. No trial activated.
- Catalog lists Meshy image-to-3D and rigging. Image estimate tool rejects 3D and
  requests generate_3d, which is **not exposed** here. No documented direct Meshy
  endpoint established. Website 3D Jutsu needed login; user chose local Blender.
  This is a capability gap, not a plan/balance finding.
- Draft art/concepts/hollow-saint-3d-input-v1.png made through ChatGPT's built-in
  image tool, not Higgsfield. Prompt in sibling prompts. It still has long-tabard,
  hand and lower-ring ambiguity; source art only, not a mesh or override of blockout.

## Exact next steps

0. Animation agent: follow docs/animation-handoff.md; build a locomotion block-out
   (idle to walk/run forward, then sprint to glide) on v18. Iterate visuals before
   returning to deformation/animation polish (shoulders/elbows, tabard follow).
1. Review the delivered hybrid v7 five-view gallery. Preserve this checkpoint;
   do not restart body design. Any further requested visual changes should use
   a new version. Rigging and detailed production cleanup follow the planned
   blockout/prototype review milestone. Keep LEFT A as the design source.
2. Prepare original C# primary prototype with exact dependency versions and
   permitted local placeholder visuals. Register separate survivor without
   changing original globally. Do not copy/run HenryTutorial wholesale: reuse
   terms and author-specific postbuild copy remain unresolved.
3. Establish explicitly provisional charge loop for playtesting. Clarify release
   policy before treating it as settled design; keep tuning configurable.
4. Install dependencies into Hollow Saint Dev only. Build and validate aimed
   hits, finite distinct chains, authoritative damage and visible charge.
5. Record crowd, isolated target, item procs, scaling, death and stage transitions.
   Two-player host/client tests are required for multiplayer-tested claims.
6. Resolve compatible patched 2021 LTS asset editor and tiny bundle-load test
   before custom art integration. Placeholder combat does not depend on this.
7. Review v9's rear transition and v8 deformation/animation poses; refine
   skinning, game export, VFX/SFX and supporting abilities. Revisit Higgsfield
   rigging when an actual 3D submission endpoint is available.

No missing user information blocks continued local visual refinement. Profile
dependencies, asset editor and live tests are setup work still to do.
No publication without explicit approval naming the release.

## Other references

- docs/ability-kit-workshop.md: supporting kit proposals.
- art/concepts/hollow-saint-art-pack.md: concept gallery.
- docs/design-brief.md: earlier numerical hypotheses.
- docs/technical-notes.md: evidence and sources.
- docs/implementation-plan.md: milestone plan.

Global agreements apply: read before overwrite; enumerate before broad renames;
preserve credentials; confirm GitHub identity before remote configuration; never
publish a release without explicit approval.

