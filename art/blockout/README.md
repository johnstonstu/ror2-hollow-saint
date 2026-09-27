# Hollow Saint blockout 01

Actual custom Blender geometry, ready for milestone 2 visual review. No rig,
animation, Unity export, or gameplay implementation is included.

[Editable .blend](hollow-saint-blockout-v1.blend)

## Front

![Front](hollow-saint-blockout-v1-front.png)

## Side

![Side](hollow-saint-blockout-v1-side.png)

## Back

![Back](hollow-saint-blockout-v1-back.png)

## Simulated gameplay distance

This is a Blender camera study, not a Risk of Rain 2 screenshot. Actual camera
framing, animation clearance and environmental contrast still need game tests.

![Gameplay-distance study](hollow-saint-blockout-v1-gameplay-distance.png)

## Construction proposals to review

- Full four-quarter copper halo; lower arcs behind upper torso. Stronger rear
  ring silhouette than some of the inconsistent generated concepts.
- Five simple dark digits per hand; compact symmetrical shoulder plates.
- Small rear cyan node and narrow dark yoke with visible clearance behind head.
- Separate front/back tabard panels ending just above knee center.
- Approximately 2.015 m body height and 0.89 m halo diameter, for review only.

Original LEFT A and detail sheet supply the identity. The single-character
generated input draft does not override this reconciled construction.

## Verification and reproduction

Saved .blend reopened and static checks passed for four halo objects, five
digits on each hand, four cameras, a rear charge node, and no armature. Measured
mask/halo depth clearance is approximately 0.152 m. These checks do not validate
motion, deformation, material import, or gameplay.

101 editable meshes, 2,208 vertices, 2,278 polygons, 4,012 triangle equivalents.
See [metrics](hollow-saint-blockout-v1-metrics.json).

Run construction only in a separate Blender process; it replaces the generated
v1 outputs. Preserve manual edits in a different versioned .blend first.

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --factory-startup --python tools/blender/build_hollow_saint_blockout.py
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background art/blockout/hollow-saint-blockout-v1.blend --python tools/blender/verify_hollow_saint_blockout.py
```

Construction source: `tools/blender/build_hollow_saint_blockout.py`.
It refuses interactive execution to preserve live Blender scenes.
The first build reported a thumbnail-cache warning; requested renders and
the saved/reopened .blend succeeded.
