# Hollow Saint hybrid refinement audit

Active visual goal: keep refining the actual model until it resembles the selected
concept. The previous turn made progress: it completed five local refinement
passes and directly compared the supplied Higgsfield mesh, changing the recommended
starting body. This turn constructs a separately saved hybrid rather than leaving
that recommendation unimplemented.

## Requirements and evidence to inspect

| Requirement | Evidence needed |
| --- | --- |
| Preserve selected LEFT A identity | Actual front and hero render beside the original sheet; slender ivory/dark body, blank mask, cyan core, copper halo and long dark tabard |
| Keep the stronger HF body contours | Matching source/hybrid cameras; no regression to straight tubular armor, long exposed neck or rounded slipper feet |
| Four separate halo sections | Saved-file object/geometry inspection and front/side renders showing real gaps and concealed attachment |
| Five useful fingers per hand | Close renders and separate articulated geometry, natural opposed thumb and tapered digits |
| Correct generated rear chest | Rear render with compact scapula forms, narrow dark spine and small charge node; no large duplicate front core |
| Coherent materials and light | Preserve useful source texture; check cyan emission is intentional and rear painted front glow is gone |
| Editable model and requested views | Reopen saved .blend, inspect hierarchy/geometry, verify front/side/back/hero/gameplay-distance PNGs exist |
| Honest milestone status | No claim of rigging, animation, export or game readiness from a static art study |
| Preserve originals and credentials | Original GLB and local v6 unchanged; no environment-file content included in artifacts |

## Planned review sequence

1. Inspect the first hybrid in matched front/hero/side/back views. Check cuts near
   shoulders and wrists for holes, wedges, leftovers and abrupt transitions.
2. Compare hand and halo size against the original HF silhouette and selected A.
3. Inspect the rear correction at close range and simulated gameplay distance.
4. Make another substantive pass for any visible regressions. Static checks cannot
   substitute for this comparison.
5. Update handoff with the actual best candidate and remaining limitations.

The construction remains provisional. Gameplay abilities and numerical balance
are unchanged by this visual work. No paid generation is needed for this pass.

## Hybrid v1 — rejected internal pass

Actual front/hero/side/back renders revealed destructive region-selection errors
in the derived copy. The original GLB and local v6 remain unchanged.

- Hand cut lacked a lower Z bound, removing lower-leg and foot geometry.
- Incomplete old halo removal plus broad hole-filling created large ivory shards.
- A few old finger fragments remained beside the replacement hand.
- Rear recoloring removed the false core but exposed overly irregular flat
  triangles; the spine/scapula correction needs further shaping and integration.

Added an explicit regression check for retained lower-leg/foot vertices. Its
first Z=.65 cutoff flagged **475 missing source vertices** in v1, consistent
with the visible damage. Direct coordinate inspection found two original
fingertips at Z=.644/.649; the guard now uses Z=.62 to isolate lower legs/feet,
with a 3e-6 m nearest-vertex tolerance for the 2e-6 m seam weld.
This is a failed intermediate pass, not a deliverable candidate. V2 must repair
selection boundaries before further material work or presentation.

## Hybrid v2 — surgery repaired, finish still incomplete

Actual hero/side/back renders now retain the stronger source body and feet,
remove old finger and halo debris, and show separate hands and four halo arcs.
The new preservation check passes **1,226 lower-leg/foot vertices with zero
displacement**. Both original file hashes are unchanged. The saved .blend opens,
uses packed texture data and five review cameras, and has finite geometry.

Visual audit still rejects a finished-state claim: nonplanar shoulder caps show
jagged patches, wrist sleeve boundaries need cleaning, and the rear correction
has lumpy surfaces and abrupt material boundaries. V3 targets proper local cap
topology, seated scapula/spine structure, darker rear graphite, a clean rear mask
edge and coherent body material/emission behavior.

## Latest user feedback and finish scope

The user says the model is starting to look pretty good and identifies the
hands as rotated awkwardly. This is direct positive evidence for the current
body direction, with an explicit remaining correction: align each hand with
its forearm and use a relaxed partly inward palm orientation. Keep proportions
stable. Complete local shoulder/material cleanup and inspect the final pose
from front, side and rear; further broad redesign would be counterproductive.

## Hybrid v5 — finishing audit

Both primary and independent visual review find that the actual HF-based hybrid
now clearly preserves LEFT A's silhouette, blank mask, curved ivory armor, dark
joints, cyan core, four-part copper halo and long tabard. It improves substantially
over rejected v1 and corrects the generated duplicate chest on the back.

The source texture now drives bounded real cyan emission. Ceramic/graphite
responses are separated by texture luminance, copper has restrained procedural
variation, and 45-degree normal smoothing improves surface continuity without
moving body vertices. The packed source image itself was not edited.

Two visible requirements remain before closing this visual refinement goal:

1. Align the hands with the outward-sloping forearms and give them a relaxed
   partly inward palm roll; current side-view fingers project forward awkwardly.
2. Remove two tall rectangular dark/tan cut-surface strips inside the rear
   shoulder caps. They still read as repair plates instead of joint anatomy.

No further proportion overhaul is needed. Texture resolution, deformation
topology, rigging, animations and game integration remain later project stages.

## Hybrid v7 — completed visual refinement milestone

Primary and independent reviewers inspected all five actual v7 renders. The
model clearly resembles selected LEFT A across front, hero, side, back and
simulated gameplay-distance views. The latest user feedback endorsed the body
direction; this final exact version has not yet received user acceptance.

- Hands now continue the forearm axes, with partly inward palm rotation and
  relaxed finger curl. Side view no longer shows the earlier abrupt pose mismatch.
- Shoulder repair surfaces are bowed and rounded; obsolete tan texture seams
  are suppressed into coherent graphite joints. Rear surfaces retain simple
  mechanical forms, with no torn shards or duplicate front chest/core.
- Blank ivory mask, curved ceramic shells, dark joints, cyan core, long tabard
  and four copper arcs remain intact. Foot and lower-leg silhouettes are preserved.
- Copper variation and bounded cyan emission are actual saved materials;
  original packed image pixels and source assets remain unchanged.

Saved-file QA passes: 66 model parts, 26,808 evaluated triangles, four independent
arcs, five digits per hand, five review cameras and packed texture. All 1,226
protected source lower-leg/foot vertices have zero displacement; both original
file hashes match. This confirms integrity in addition to the separate visual
audit; it does not establish deformation or game readiness.

[Final scene and reproduction](../art/hybrid/README.md),
[comparison gallery](../art/hybrid/review.html), and
[QA results](../art/hybrid/hollow-saint-hybrid-v7.qa.json).

The current visual goal is fulfilled. Remaining source texture blur, detailed
topology cleanup, rigging, animation and Unity/game tests belong to subsequent
project milestones. No additional Higgsfield credits were spent.
