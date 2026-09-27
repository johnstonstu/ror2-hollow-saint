# Hollow Saint local refinement

Latest local pass: [editable v6](hollow-saint-refinement-v6.blend).
[Browse local passes beside concepts](review.html).
[Compare supplied Higgsfield starter against v6](../comparison/review.html).

The Higgsfield body now offers the stronger visual starting point. See the
[comparison recommendation](../comparison/comparison.md). The resulting
[hybrid v7](../hybrid/README.md) is now the current review deliverable; these
local passes remain intact as historical studies and component references.

V2-v6 are actual editable Blender studies, with five matching PNG views per
version. V6 improves the rear anatomy, copper material and core border, and
restores the tabard motif. V1 remains under `../blockout/` and was rejected.

Latest views: [front](hollow-saint-refinement-v6-front.png),
[three quarter](hollow-saint-refinement-v6-hero.png),
[side](hollow-saint-refinement-v6-side.png),
[back](hollow-saint-refinement-v6-back.png),
[simulated gameplay distance](hollow-saint-refinement-v6-gameplay-distance.png).

Current source: `../../tools/blender/refine_hollow_saint.py` plus its helper
modules. Snapshots exist in source-v4, source-v5 and source-v6; v2/v3 are
preserved as scenes/renders only. Snapshot files are historical source records;
their root-path assumptions still reflect their original tools/blender location.

Reproduce the current version from workspace root using:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --factory-startup --python tools/blender/refine_hollow_saint.py
```

This overwrites the current version's generated outputs. Preserve a new snapshot
and increment the presentation version before another pass. Build scripts reject
interactive execution; they preserve the user's existing Blender scene.

V6 saved-file reopening and integrity checks passed: 171 editable parts, four
independent halo arcs, five articulated digits per hand, five cameras, no external
linked assets, and finite evaluated geometry. Base mesh: 5,343 vertices / 5,607
polygons; evaluated output including bevels/curves: 31,994 triangles. These counts
are not evidence of visual fidelity or animation readiness.

No rig, deformation test, export, Unity bundle or in-game validation. Renders
were not retouched. This continuation used no paid generation.
