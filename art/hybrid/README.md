# Hollow Saint hybrid v7

Current visual review model: [editable Blender scene](hollow-saint-hybrid-v7.blend).
[Compare all five views against the original HF mesh or local v6](review.html).

The supplied Higgsfield body preserves the selected LEFT A silhouette. Local
Blender work replaces fused hands with separate articulated digits, reconstructs
four copper halo arcs, corrects the generated duplicate rear chest, and gives
cyan areas real bounded emission. The final passes align hands with forearms,
roll palms inward, and clean rear shoulder seams without changing proportions.

Views: [front](hollow-saint-hybrid-v7-front.png),
[three quarter](hollow-saint-hybrid-v7-hero.png),
[side](hollow-saint-hybrid-v7-side.png),
[back](hollow-saint-hybrid-v7-back.png),
[simulated gameplay distance](hollow-saint-hybrid-v7-gameplay-distance.png).
These are actual Blender renders, without image retouching.

Saved-file inspection passed: 66 parts, 26,808 evaluated triangles, four editable
halo arcs, five digits per hand, five cameras and packed texture data. All 1,226
protected source lower-leg/foot vertices have zero displacement. Original GLB
and local v6 file hashes are unchanged. [QA details](hollow-saint-hybrid-v7.qa.json).

## Reproduction

Current build entry is `../../tools/blender/hybrid_build.py`. `source-v7/` records
all build dependencies; snapshot root paths assume the original tools/blender
location, so snapshots are historical records rather than standalone launchers.
The build uses the preserved `../comparison/higgsfield-sam3d-review.blend`.

From workspace root:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --factory-startup --python-exit-code 1 --python tools/blender/hybrid_build.py
```

This regenerates v7 outputs. Preserve this checkpoint and change the presentation
version before making another pass. Interactive execution is refused.

Saved-file QA, without modifying the scene:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background art/hybrid/hollow-saint-hybrid-v7.blend --python-exit-code 1 --python tools/blender/inspect_hybrid_scene.py
```

## Scope

The visual likeness milestone is ready for user review. Source 1024px texture
blur, production retopology, deformation, rigging, animation, Unity conversion
and game validation remain separate work. Distant rendering is a simulated
Blender camera, not an in-game screenshot. No extra paid generation was used.
