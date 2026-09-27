# Hollow Saint v9 rear update

Versioned from the v8 rig study. Open
[hollow-saint-hybrid-v9.blend](hollow-saint-hybrid-v9.blend) in Blender 5.2.
The v7 visual checkpoint and v8 rig file are unchanged.

## Rear change

- Removed the two thin dark yoke branches that ran diagonally from the back node
  into the shoulder area.
- Replaced the hairline side-gap wires with pale-cyan inlay bands crossing the
  copper arcs at shoulder height, echoing the supplied Higgsfield back view.
- Preserved the four separate arc meshes, scapula shells, center charge node,
  spine conductor, and all three v8 animation actions.

See [rear view](v9-review-back.png) and [side view](v9-review-side.png).

## Rebuild

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python-exit-code 1 --python tools/blender/hybrid_back_v9.py
```

This is a small visual study; inspect it in Blender before treating it as the
new accepted model. The fused-body rig remains approximate and is not game
validated.
