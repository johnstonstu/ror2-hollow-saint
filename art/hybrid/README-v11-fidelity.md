# Hollow Saint v11 fidelity pass

User direction (2026-09-26): LEFT A is the target, the extra v9 detail is preferred,
and a fidelity pass toward the concept images comes before further animation work.

Open [the review page](v11/review.html) or the
[concept / before / after sheet](v11/comparison.jpg).
Editable scene: [hollow-saint-hybrid-v11.blend](hollow-saint-hybrid-v11.blend).

Built from v10, so the v9 rear detail, the revised rig, and all six `HS_v10`
actions are retained. v1–v10 files are byte-for-byte unchanged (SHA-256 checked).

## Changes toward the concept sheets

- **Halo:** depth doubled (0.036 to 0.076 m), aged-copper block shader with three
  seamed blocks per arc, darker lips, thin cyan light seams centred in the side
  gaps and a dim top seam. The v9 shoulder bands remain as flush inlays.
- **Cyan conductors:** geometric emissive mask stripe over the crown, temple nodes,
  white-hot core with ring, neck/chest/abdomen lines and upper/lower spine lines.
  Painted cyan in the source texture (knees, elbows) now emits.
- **Tabard:** dark graphite cloth front and back with copper trim frame and the
  circle-and-line sigil; edge caps remove the tan side slivers.
- **Hands:** cyan fingertip lights on all ten distal finger bones.
- **Presentation:** Cycles, AgX medium-high contrast, lighter studio, stills
  re-framed at ortho 2.75, and a new 85 mm bust camera.

Surface details are skinned with weights interpolated from the body; fingertip,
halo seam and inlay parts are bone-parented. Saved-file [QA](v11/qa.json) reopens
the file and checks every `HS_v10` action: maximum surface drift is 8 mm (Arc Step
abdomen line), all others at or below 3 mm; weights are normalised.

## Known limits

- The body is still the HF-derived mesh with its 1024 px texture; mask and armour
  forms are unchanged, so fine surface detail remains soft up close.
- Halo gap seams are parented to one neighbouring arc and will not stay centred
  while the Open Circuit crown unfolds.
- Tabard overlays follow the body weights; no cloth motion.
- Renders are Blender studio evidence only; no export or in-game test.

## Reproduction (Blender 5.2, background only)

1. `tools/blender/build_hybrid_v11.py` — rebuild from v10 and render all views.
2. `tools/blender/review_hybrid_v11.py` — read-only saved-file QA.
3. `tools/blender/package_review_v11.py` — normal Python with Pillow.

`inspect_v11_source.py` and `probe_v11_surface.py` are the read-only surveys used
to place details.
