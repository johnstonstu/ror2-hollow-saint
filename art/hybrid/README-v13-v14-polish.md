# Hollow Saint v13 and v14 polish passes

User direction (2026-09-26): keep going on polish, thicken the plates, move
closer to the LEFT A concept before animation.

- [v14 review page](v14/review.html) and [concept / v12 / v14 sheet](v14/comparison.jpg)
- [v13 review page](v13/review.html) and [concept / v12 / v13 sheet](v13/comparison.jpg)
- Editable scenes: [v13](hollow-saint-hybrid-v13.blend), [v14](hollow-saint-hybrid-v14.blend)

Both build on the previous file; rig and all six `HS_v10` actions are retained.
v1–v12 files are unchanged.

## v13: shape and light

- **Pauldrons rebuilt:** thicker 26 mm faceted upper plate (10×8 segment shell,
  angle-limited chamfers) plus a stacked 20 mm under-plate tilted further out,
  echoing the layered shoulder in the detail study. Replaces the v12 shells.
- **Core:** hot centre emission 14 → 7, tinted cyan.
- **Bloom:** compositor Glare (Bloom) on emissive highlights for the concept's soft glow.
- **Halo:** two plates per arc with finer seams (was three brick-like blocks).

## v14: concept palette

- **Warm bone ivory:** multiply tint (0.93, 0.84, 0.68) on the body texture,
  posterior blend and warm-ivory parts; pauldron ceramic matched.
- **Aged copper:** deeper red-brown, metallic 0.35 → 0.18, rougher; darker lips.
- **Core:** emission 7 → 4 with deeper cyan; bloom strength raised to carry the glow.

## QA

`HS_VERSION=v13` / `v14` with `review_hybrid_v11.py`: both PASS. Decal drift is
unchanged (max 8.3 mm). Pauldron shell drift is informational, max 15 mm at Arc Step
frame 9. The `pose-arc-step` and `pose-arc-bolt` renders show no clipping.

## Known limits

- Pauldrons are still weight-blended shells; the animation plan proposes a
  dedicated bone per pauldron.
- The body mesh and 1024 px texture are unchanged, so fine detail stays soft up close.
- Blender studio evidence only; no export or in-game test.

## Reproduction (Blender 5.2, background only)

1. `tools/blender/build_hybrid_v13.py`, then `build_hybrid_v14.py`.
2. `HS_VERSION=v14` + `tools/blender/review_hybrid_v11.py`.
3. `python tools/blender/package_review_polish.py v12 v14 "..."`.
