# Hollow Saint: supplied Higgsfield starter vs local pass 6

Inspected 2026-09-26. [Interactive comparison](review.html).

**Recommendation: pivot to the Higgsfield body as the visual/sculpting base.**
It is substantially closer to selected LEFT A in the mask and compact neck,
torso silhouette, curved armor, knee openings, feet, and material breakup.
The locally built model is useful for separate editable components and construction
ideas; continuing to refine its entire body has less value than correcting the
stronger generated starting shape. This recommendation has now been implemented
in [hybrid v7](../hybrid/README.md), preserving both original assets.

| Area | Supplied Higgsfield model | Local Blender v6 |
| --- | --- | --- |
| Concept resemblance | Stronger contours and surface character | Recognizable identity, but more regular/mannequin-like |
| Mask/neck | Compact, flowing silhouette; texture seam | Clean blank mask and real cyan inlay; more exposed neck |
| Armor/feet | Better curved shell openings and feet | More individually editable, less faithful shapes |
| Hands | Fused fingers/hook shapes need reconstruction | Five separate articulated digits; reshape to match new body |
| Halo | Side cyan divisions painted onto continuous geometry; fused to shoulder area | Four independent arcs, useful for future unfolding animation |
| Back | Repeats front chest/core; wrong rear anatomy | Deliberate rear yoke/node, useful construction reference |
| Materials | One 1024 x 1024 texture with painted glow and shading | Separate ivory, graphite, copper and emissive materials |
| Animation | No armature, actions or skin weights | No armature, actions or skin weights |

## Direct inspection

Source preserved: `output/higgsfield-hollow-saint/hollow-saint-sam3d.glb`.
The imported review `.blend` is a separate file, with display scale normalized
to 2.2 m total halo-to-ground height. Original topology and texture are retained.
Front, hero, side, back, simulated gameplay-distance and neutral-clay renders
were generated locally with the local model's studio/cameras. No paid job ran here.

The GLB contains one mesh, **8,108 triangles**, 6,350 imported vertices, one UV
layer, one material and one embedded 1024px texture. Raw imported topology has
235 islands and 4,140 boundary edges. These are coincident seam splits: a
temporary analysis-only weld at 1e-6 source units produces **4,048 vertices,
one connected component, zero boundary edges and zero nonmanifold edges**.
Do not describe the source as broken or full of holes based on raw seam counts.
This closed-surface result does not establish suitable deformation topology.

Material inspection confirms emission strength **0**, no emission texture link,
and base-color texture input. The visible glow is painted into its color map.
Clay rendering confirms that the side halo gaps are not geometric separations
and the fingers are fused. The original GLB was not welded or overwritten.

## Implemented hybrid scope

1. Preserve both originals; make a separately named hybrid `.blend`.
2. Use the HF body as the main shape reference/base. Reconstruct the back to
   match the original rear concept instead of the generated duplicate front.
3. Separate/rebuild the halo into four independent segments, adapting the local
   construction to the HF framing; remove fused shoulder bridges.
4. Replace fused hands with properly shaped five-digit hands. Adapt proportions
   and attachment, rather than dropping the current hands in unchanged.
5. Split ivory/graphite/copper/glow into usable materials and remove baked fake
   rear core/light details. Preserve the stronger armor and foot silhouette.
6. Render the hybrid for review before retopology/rigging, animation or Unity work.

Hybrid v7 now implements this visual scope, including corrected hand alignment
and rear shoulder seams. No rig or playable survivor exists. Sources and original
renders remain available; game readiness has not been established.
