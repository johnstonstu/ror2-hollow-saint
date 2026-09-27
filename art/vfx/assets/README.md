# Hollow Saint VFX source assets (Phase F, vfx-run1)

## Summary (vfx-run1, finished Sun 2026-09-27 ~1:15 AM PT)

- **Done: all 14 items on the VFX-ABILITY-PLAN 5c list, plus extras.** That comes to 29 meshes in 11 FBX files, 25 alpha
  PNG textures, and 53 previews (22 mesh PNG/GIFs, 25 texture sheets, 6 texture GIFs) plus `previews/_overview.png`.
- **Latest Blender source:** `hs-vfx-v07.blend`. Every step was saved under a new number (`v01`-`v07`); none was overwritten.
- **Checks:**
  - Every texture is power-of-two RGBA with real alpha.
  - Every FBX was re-imported headless: scale 1, both UV sets and the `Col` vertex colours survive, and lengths and
    orientation match. The data is in `fbx/fbx_manifest.json`.
- **Palette:**
  - The cyans come from `tools/blender/anim/vfx.py`.
  - The copper is sampled from the model scripts: aged copper blocks (0.21, 0.072, 0.028) and tabard trim
    (0.40, 0.17, 0.055).
- **Nothing touched outside `art/vfx/assets/`:** no anim files, rig, Unity, git or deletes. Superseded files went to
  `_old/`.
- **Left undone / open:**
  - **Unity shaders and prefabs.** That's Phase G. The Blender materials are previews only; the Unity settings are below.
  - **Afterimage mesh.** The game should bake the real skinned mesh (`BakeMesh`). The Blender proxy figure is for the
    preview only and isn't exported.
  - **Halo fit.** The halo ring radius (0.32 m) and gap angles (45/135/225/315 deg) are placeholders. They need fitting
    to the v18 halo in Unity, or re-measuring from the rig in a later run; I didn't open the rig, per the rules.
  - **Numbers are PROPOSALs.** Every lifetime, speed and HDR number below is a starting point to tune in game.

## Folder layout

| Path | What |
|---|---|
| `hs-vfx-v01.blend` ... `hs-vfx-v07.blend` | Blender sources. v01 bolts + halo; v02 all meshes; v03 materials + preview anim; v04-v06 look fixes; **v07 = current** (halo gap arcs split 1-4) |
| `textures/` | 25 PNG textures (RGBA, sRGB colour, straight alpha) |
| `fbx/` | 11 FBX files + `fbx_manifest.json` (tris, sizes, UV sets, re-import check) |
| `previews/` | Per-asset PNG/GIF previews on a dark background, `tex_*.png` texture sheets, `anim_*.gif`, `_overview.png` |
| `scripts/` | Everything that built this (textures.py, build_vfx.py, render_previews.py, post_previews.py, export_fbx.py, preview_textures.py, overview.py, hs_palette.py, hs_io.py) and `logs/` |
| `_old/` | Superseded outputs (never deleted); `_old/review/` holds the self-review copies |
| `vfx-run1-progress.txt` | Timestamped run log |

Rebuild, from the project root (background Blender only):

```text
python art/vfx/assets/scripts/textures.py
"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup --python art/vfx/assets/scripts/build_vfx.py -- --stage 3 --out hs-vfx-v08.blend
"...blender.exe" --background --factory-startup --python art/vfx/assets/scripts/render_previews.py -- --blend hs-vfx-v08.blend
"...blender.exe" --background --factory-startup --python art/vfx/assets/scripts/export_fbx.py -- --blend hs-vfx-v08.blend
python art/vfx/assets/scripts/post_previews.py
python art/vfx/assets/scripts/preview_textures.py
python art/vfx/assets/scripts/overview.py
```

The scripts never overwrite a `.blend` (build refuses an existing name). PNG/FBX/preview outputs that already exist are
moved into `_old/` first.

## Conventions (read before importing)

- **Units:** metres. Blender Z up, character forward = Blender -Y (the rig convention).
- **FBX:**
  - Export settings: forward -Z, up Y, Apply Transform (bake space transform), FBX_SCALE_ALL, no animation.
  - In Unity: 1 unit = 1 m, transform scale 1.
  - Blender -Y becomes **Unity +Z (forward)** and Blender +Z becomes **Unity +Y**.
  - All meshes have identity transforms; the pivot is the object origin described per asset.
- **Unity import:** Scale Factor 1, Convert Units on, Read/Write off, no animation, no rig. Materials "None" (make your
  own). Normals "Import" is fine: everything is unlit.
- **UV sets:**
  - **UV0 `UVMap`:** U = 0..1 along the length and V = 0..1 across (ribbons). On cones and lances, U runs around and V
    along (0 = nozzle/tail, 1 = tip).
  - **UV1 `UVTile`:** the same, but U is in texture tiles (for tiled/scrolling textures). In Unity this is `uv2` / TEXCOORD1.
- **Vertex colour `Col`:** white, alpha = end fade/taper. Multiply it into alpha in every shader.
- **Double-sided:** every ribbon is a crossed pair (two strips at 90 deg) or a flat strip, so use **Cull Off**.
- **Textures:**
  - Straight (un-premultiplied) alpha, sRGB colour, alpha = glow coverage.
  - **Additive:** output `rgb * a * tint * HDR`. **Alpha-blended** (scorch decal only): alpha is coverage.
  - Import as Default, sRGB on, Alpha Is Transparency on, Wrap = Repeat for `*_tile`, `noise_*`, `trail_*`,
    `ghost_*`, `band_*`, `jet_exhaust`; Clamp for the rest; mipmaps on.
  - Exceptions: set `hs_spear_dissolve_mask` sRGB **off**, and `noise_*` / `ghost_*` sRGB **off** (data masks).
- **RoR2 shader mapping [PROPOSAL]:** most vanilla FX use `Hopoo Games/FX/Cloud Remap` (HGCloudRemap): a main texture,
  a remap ramp, and two cloud textures with scroll speeds, in additive or blend mode.
  - Suggested textures: `_MainTex` = our sprite, `_RemapTex` = `hs_meter_ramp` (or a cyan ramp),
    `_Cloud1Tex`/`_Cloud2Tex` = `hs_noise_scroll` / `hs_noise_streak` / `hs_noise_crackle`.
  - A small custom unlit additive shader works too. Custom shaders need care in asset bundles; decide in Phase G.
- **HDR intensity:** Unity HDR colour "Intensity" is in stops (multiplier = 2^I). The Blender `vfx.py` jets use
  strength 9 (outer), 16 (arcs) and 22 (core) with no bloom. With RoR2's bloom, start at about a third of that and tune.

## Palette

| Name | Linear RGB (Blender / Unity HDR base) | sRGB hex | Use |
|---|---|---|---|
| Core, white-hot cyan | (0.85, 0.97, 1.0) | `#EDFCFF` | bolt cores, lance, flashes (`vfx.py` core, strength 22) |
| Outer cyan | (0.25, 0.8, 1.0) | `#89E7FF` | glow edges, jets, mark ring, ghost rim (`vfx.py` outer, 9) |
| Arc cyan | (0.55, 0.9, 1.0) | `#C4F3FF` | arcs, filaments, trails (`vfx.py` arcs, 16) |
| Copper (aged blocks, light) | (0.21, 0.072, 0.028) | `#7E4C2F` | halo copper; meter ramp copper band (`build_hybrid_v14.py`) |
| Copper trim | (0.40, 0.17, 0.055) | `#AA7342` | meter ramp mid band (`build_hybrid_v11.py` tabard trim) |
| Copper glow (trim hue at max 1.0) | (1.0, 0.425, 0.1375) | `#FFAE68` | copper tail sparks, embers, meter ramp copper peak |
| Copper dark | (0.055, 0.02, 0.009) | `#422718` | scorch rim, meter ramp low end |

## Asset sheet

Frame numbers are 24 fps clip frames from VFX-ABILITY-PLAN section 2. Sockets use the plan's names, with the
proposed ChildLocator key in brackets. All timings, sizes and intensities are **[PROPOSAL]**.

### 1. Short arcs (3 variants)
- **Files:**
  - `fbx/HS_arc_short.fbx` (`HS_arc_short_A/B/C`, 128-192 tris each)
  - texture `textures/hs_bolt_ribbon_tile_512x64.png`
  - previews `previews/arc_short.png`, `anim_bolt_flipbook.gif`
- **Shape:** 0.4 m long along +Z (Unity) from a pivot at the start, crossed ribbons 0.018 m wide. C has a small fork.
- **Unity use:** mesh ParticleSystem (Render Mode Mesh, pick A/B/C at random, random roll about Z), or a scaled mesh
  between two points (scale Z = distance / 0.4).
- **Settings:**
  - Additive, tint Arc cyan, HDR ×4 (I ≈ 2).
  - Lifetime 0.06-0.12 s with 2-3 frame on/off flicker: re-roll the variant every 2 frames, or scroll `UVTile.x` at -8 /s.
  - Fingertip crackle: 1-3 arcs, 0.05-0.12 m (scale 0.15-0.3).
- **Sockets / beats:**
  - `R/L muzzle` [MuzzleR/L]: Arc Bolt anticipation f3 crackle.
  - Chain hop: Arc Bolt f5, one hop per chained target, scaled to the hop distance.
  - Halo gaps and Charge-loop orb arcs: `halo socket` [Halo] / `orb socket` [Orb].

### 2. Long bolts (3 variants)
- **Files:** `fbx/HS_bolt_long.fbx` (`HS_bolt_long_A/B/C`, 1024 tris each), texture `hs_bolt_ribbon_tile_512x64.png`,
  preview `previews/bolt_long.png`.
- **Shape:** 10 m along +Z from the start pivot, 0.06 m wide.
- **Unity use:**
  - Scale local Z = hit distance / 10 (keep X/Y = 1) and aim +Z at the hit point.
  - LineRenderer alternative: `hs_bolt_ribbon_stretch_1024x128.png`, Texture Mode Stretch, width 0.05-0.07 m, 12-20
    jittered points re-rolled every 2 frames.
- **Settings:** additive, tint Core, HDR ×6 (I ≈ 2.6). Life 0.10-0.15 s with a 2-3 frame fade (plan 2.1). `UVTile.x`
  scroll -10 /s. Empowered shot: width ×1.6, HDR ×10.
- **Sockets / beats:**
  - Arc Bolt tracer from `R/L muzzle` at f5 (release), to the first hit and then each hop.
  - Open Circuit pulse strike from `halo arc N tip` [HaloArc1-4] to the target during the hold. Keep it brighter and
    thicker than the hand bolt: HDR ×8, width ×1.4.

### 3. Branching bolts (2 variants)
- **Files:** `fbx/HS_bolt_branch.fbx` (`HS_bolt_branch_A/B`, ~1.2k tris), preview `previews/bolt_branch.png`. Sprite
  version: `hs_bolt_branch_flipbook_4x4_1024.png`.
- **Shape:** 3 m along +Z from the start pivot, with 4-5 branches (some forked).
- **Unity use:** mesh, or mesh particles with 1-2 per hand.
- **Settings:** additive, tint Core, HDR ×6. Life 0.15 s, scale 0.6-1.2, random roll. Empowered shot: add one at the first target.
- **Sockets / beats:** Discharge proc (passive) from `L/R palm` [PalmL/R] (or muzzles) along the fling direction, 2
  per hand; empowered Arc Bolt at the target.

### 4. Halo ring arc (segmented ring + 4 gap arcs)
- **Files:**
  - `fbx/HS_halo_ring.fbx`: `HS_halo_ring_segments` (320 tris) and `HS_halo_gap_arc_1..4` (64 tris each)
  - textures `hs_band_profile_64.png` (segments), `hs_bolt_ribbon_tile_512x64.png` (gap arcs)
  - previews `previews/halo_ring.png/.gif`, `previews/halo_charge.png/.gif`
- **Shape:** ring in the Unity XY plane (faces ±Z), radius 0.32 m (placeholder), 4 flat segments with 16 deg gaps
  centred on 45/135/225/315 deg. Gap arcs: 1 = top-right, 2 = top-left, 3 = bottom-left, 4 = bottom-right.
- **Unity use:** mesh children of `halo socket` [Halo].
  - Scale and rotate them to the v18 halo. Better: parent each gap arc to its halo arc tip so the gaps follow the
    lagging halo arcs.
  - Segments: persistent shimmer (Open Circuit crown, charge rim).
  - Gap arcs: toggled by the meter.
- **Settings:**
  - Segments: additive, Outer cyan, HDR ×2 → ×4 with charge. Second-layer `hs_noise_crackle_256` scrolling 0.3 /s
    along `UVTile.x` for shimmer.
  - Gap arcs: additive, Core, HDR ×6. Flicker (re-roll roll / UV offset every 2-3 frames).
  - **Meter mapping (2.3a):** 0-24% none; gap 1 at 25%, 2 at 50%, 3 at 75%, 4 at 100%; at 100% add a ready ping (glow
    ring card flash).
  - **Discharge petal vent:** all 4 gap arcs at HDR ×12 for 2 frames, then fade over 6-8 frames with the meter dump.

### 5. Bolt flipbook (4×4)
- **Files:** `textures/hs_bolt_flipbook_4x4_1024.png`, `textures/hs_bolt_branch_flipbook_4x4_1024.png`; previews
  `tex_hs_bolt_*`, `anim_bolt_flipbook.gif`, `anim_bolt_branch_flipbook.gif`.
- **Frames:**
  - 0-1: leader draws in (with a hot tip).
  - 2-3: full strike (brightest).
  - 4-11: flicker variations (the same overall shape re-jittered every 2 frames).
  - 12-15: fade and thin.
- **Unity use:** ParticleSystem Texture Sheet Animation, Tiles 4×4, Whole Sheet.
  - **One-shot strike:** Frame over Time linear 0→1, Cycles 1, Stretched/Horizontal Billboard or Mesh = card quad.
    Lifetime 0.25-0.33 s (48-64 fps).
  - **Sustained crackle** (orb, fingertips, halo gaps): Start Frame random 4-11 and Frame over Time constant, re-emitted
    every 2 frames.
- **Settings:** additive, tint white (colour is baked), HDR ×3-5. Each bolt runs horizontally across its cell (left →
  right), so stretch the billboard along velocity or use length-scaled cards.
- **Beats:** all bolts (impact crackle, fingertip crackle, Charge orb, Open Circuit tethers).

### 6. Spark sprite
- **Files:**
  - `textures/hs_spark_streak_128.png` (cyan), `hs_spark_streak_copper_128.png` (copper), `hs_spark_star_128.png`
  - `hs_spark_sheet_2x2_256.png`: streak cyan, star, streak copper, dot
  - previews `tex_hs_spark_*`
- **Unity use:** ParticleSystem, Stretched Billboard (streaks: head on the right, so the velocity runs to the right),
  Speed Scale 0.02-0.04, Length Scale 1. For random variety, use the sheet with Texture Sheet 2×2, Start Frame random.
- **Settings:**
  - Additive, HDR ×3-6.
  - Lifetime 0.15-0.35 s, Start Speed 3-8 m/s, Gravity 0.5-1, size 0.03-0.08 m, Colour over Lifetime white → Outer cyan → 0.
  - Counts: impacts 6-12, run push-off 2-4 per toe-off (**sprayed opposite the travel vector**, 2.10).
  - Materialize: 1-2 copper sparks at the spear tail on release (2.9).
- **Sockets / beats:**
  - Arc Bolt impacts (at the target).
  - Run push-off: `L/R heel socket` → [HeelJetL/R] on each toe-off (run ≈ f5 / f13).
  - Arc Step arrival puff at `ground`.
  - Discharge lingering sparks to ~f14.
  - Conduit Spear impact and the copper tail sparks.

### 7. Glow / corona cards (soft, star, ring)
- **Files:** `textures/hs_glow_soft_256.png`, `hs_glow_star_256.png`, `hs_glow_ring_256.png`; mesh card
  `fbx/HS_quads.fbx` → `HS_card_quad_1m` (1×1 m, faces -Z, i.e. toward the camera when unrotated); previews
  `tex_hs_glow_*`, `card_glow.png`.
- **Unity use:** billboard particles (1 particle) or the card mesh.
- **Settings (additive, all [PROPOSAL]):**

| Beat | Card | Size | Life | HDR |
|---|---|---|---|---|
| Arc Bolt fingertip flash, f5 at `R/L muzzle` | star | 0.18-0.25 m | 1-2 frames (0.06 s) | ×8 |
| Spear palm flash, f7 at `R palm` | star + soft | 0.35 m | 2 frames | ×8 |
| Spear core flash at the end of the materialize, f6 at `core socket` | soft | 0.5 m | 3 frames | ×4 |
| Discharge core flash, release at `core socket` | star + soft | 0.6-0.8 m | 3 frames | ×10 |
| Discharge radial ring pulse | ring | 0.3 → 2.0 m | 0.2 s | ×4 → 0 |
| Charge orb, between the palms at `orb socket` | soft (+ crackle flipbook) | 0.25 m (loop), 0.35 m (full) | loop, pulse on f11/f7 | ×3 → ×6 |
| Meter-full ready ping at `halo socket` | ring | 0.4 → 0.9 m | 0.15 s | ×6 |

### 8. Heel-jet exhaust cone + ribbon (+ burst)
- **Files:**
  - `fbx/HS_heel_jet.fbx`: `HS_jet_cone_outer` (r 0.05, 0.5 m), `HS_jet_cone_core` (r 0.0225, 0.35 m),
    `HS_jet_arcs` (3 zigzag ribbons inside the plume), `HS_jet_ribbon` (0.6 m tapered streak)
  - `fbx/HS_jet_burst.fbx`: `HS_jet_burst_spikes` (14 radial spikes in a ~35 deg cone), `HS_jet_burst_ring` (shock ring)
  - textures `hs_jet_exhaust_256.png`, `hs_noise_streak_256.png`
  - previews `heel_jet.png/.gif`, `jet_burst.png/.gif`, `jet_ribbon.png`, `anim_jet_exhaust_scroll_u.gif`,
    `anim_noise_streak_scroll.gif`
- **Shape:** the pivot is the nozzle and the plume extends along local +Z, so orient the mount with
  `LookRotation(-travelDir)`. This matches the `vfx.py` dimensions.
- **Unity use:** persistent meshes under `L/R heel jet` [HeelJetL/R], driven by `HollowSaintVFXController` (plan
  section 3.4):
  - Scale = `hs_jet + hs_spark_side` on all 3 axes (0 = hidden).
  - Pitch = `hs_jet_dir` below horizontal.
  - Yaw = the smoothed character-space travel vector (`hs_move_x/y`, or `characterMotor.velocity` projected into model
    space, smoothed 4-6 frames; 2.10).
  - Offset the spawn 4-6 cm outward when the exhaust points into the leg.
- **Scrolling material note:**
  - Cones: base = `hs_jet_exhaust_256` on `UVMap`. Its U runs around the cone and V along it; V has the hot-at-nozzle
    gradient baked in, so **don't scroll V on this layer**.
  - Multiply by a second layer, `hs_noise_streak_256` on `UVTile` (tiling U 2), scrolled **along V at -3 UV/s** for
    flow. Add a slow U swirl of 0.5 /s on the base layer.
  - Arcs: `hs_bolt_ribbon_tile` scrolled along `UVTile.x` at -8 /s, with the mesh spinning about local Z at 130-160 deg/s
    (L +, R -, as in `vfx.py`).
  - Ribbon: `hs_noise_streak` rotated 90 deg (streaks along U), scroll U -5 /s.
- **Settings:** additive, outer HDR ×3 (I ≈ 1.6), core ×6 (I ≈ 2.6), arcs ×4. Vertex alpha fades the nozzle rim and the tip.
- **TrailRenderer (optional extra streak):** texture `hs_trail_angular_512x128` or `hs_noise_streak` rotated, Time
  0.08-0.12 s, Width 0.06 → 0.008 m (same curve as `HS_jet_ribbon`), Min Vertex Distance 0.05, additive Outer cyan ×2.
- **Beats:**
  - **Glide:** enter ignition f3.5-10 plays the burst once; the loop is steady with flicker; exit cuts over ~3 frames
    with a short trail afterglow.
  - **Run push-off:** short flashes only (scale 0.35 peak, dying about 2.5 frames later) plus sparks, never a full jet.
  - **Arc Step launch:** the burst plays along the dash vector at `L/R heel jet`. Spikes scale 0.25 → 1 over 3 frames
    (0.12 s) while fading; the ring scales 1 → 2.5 over 0.15 s while fading.

### 9. Noise / scroll texture
- **Files:**
  - `textures/hs_noise_scroll_256.png`: isotropic fBm
  - `hs_noise_streak_256.png`: long streaks along V
  - `hs_noise_crackle_256.png`: Voronoi-edge electric crackle
  - all grey with alpha = value, all tile in both axes
  - previews `tex_hs_noise_*` (tiled 2×2), `anim_noise_scroll.gif`, `anim_noise_streak_scroll.gif`
- **Unity use:** second/third texture layers (Cloud1/Cloud2 in HGCloudRemap) for jets, trails, crown shimmer and the
  spear shell.
- **Suggested scroll speeds (UV/s):** jets streak -3 V; trails -2 U; crown crackle 0.3 U; spear shell 1.5 V.

### 10. Discharge meter glow ramp
- **Files:** `textures/hs_meter_ramp_256x16.png` and `hs_meter_ramp_1024x32.png` (same gradient); previews
  `tex_hs_meter_ramp_*`, `anim_meter_ramp_fill.gif`.
- **Gradient:** dark graphite (0%) → copper dark (10%) → copper trim (30%) → copper glow (45%) → transition →
  Outer cyan (66%) → Arc cyan (84%) → Core (94%) → white (100%). Alpha rises from 0.15 to 1 over 0-35%, so the dim end
  stays subtle when the ramp is used as coverage.
- **Unity use:** sample at u = meter (0-1) for the core and halo emission colour, and for the HUD meter fill. Sample in
  C# with `GetPixelBilinear(meter, 0.5)` (texture Read/Write on) or in the shader (Clamp wrap). It can also be the
  `_RemapTex` of a Cloud Remap material.
- **Settings:** emission = ramp(meter) × (1 + 2.5·meter) (2.3a), with a breathing pulse from 0.5 Hz at 0% to 1.5 Hz near
  100%. On the Discharge proc, dump to 0 over 6-8 frames.
- **Sockets:** `core socket` [Core], `halo socket` [Halo] (plus the gap arcs above), and the UI.

### 11. Conduit Spear projectile + trail + materialize
- **Files:**
  - `fbx/HS_spear.fbx`:
    - `HS_spear_lance_core`: 1.2 m, pivot at the centre, tip at +Z, flattened blade head; UV V 0 tail → 1 tip
    - `HS_spear_lance_shell`: fresnel glow shell, same pivot
    - `HS_spear_trail`: 1.6 m tapered ribbon behind the tail
  - `fbx/HS_spear_materialize.fbx`: `HS_spear_filaments`, 6 jagged filaments starting ~0.3-0.4 m behind the tail (on
    the forearm) and converging onto the lance axis; UV U 0 = forearm end, 1 = lance end
  - textures `hs_spear_dissolve_mask_128x512.png`, `hs_bolt_ribbon_tile_512x64.png`, `hs_spark_streak_copper_128.png`
  - previews `spear_materialize.png/.gif`, `spear_flight.png`, `tex_hs_spear_dissolve_mask_128x512.png`
- **Dissolve mask channels (linear, sRGB off):**
  - R = gradient (0 tail → 1 tip, along UV V)
  - G = tileable noise
  - **B = 0.82·gradient + 0.18·noise**, the value to threshold
  - A = B
- **Materialize shader (as in `HS_M_spear_*_materialize`):**
  - visible = `mask.b < _Threshold`, with a hot band where `_Threshold - 0.07 < mask.b < _Threshold` at ×2.5 intensity.
  - Filaments use `UVMap.x < _Draw` in the same way, with a 0.12 hot head.
- **Timeline** (clip frames at 1× speed; scale with attack speed; the whole spear set is parented to
  `spear socket` [Spear], along the R forearm conductor, at the palm end):
  - f2-f5 (0.04-0.17 s): filaments `_Draw` 0 → 1.05.
  - f2-f6 (0.04-0.21 s): lance core `_Threshold` 0 → 1.02; the shell follows 1 frame later.
  - f6: filaments fade out (0.1 s); core flash card at `core socket`.
  - **f7 release (0.25 s):** unparent the lance, palm flash at `R palm`, launch along the aim at **~150 m/s**, and spawn
    1-2 copper tail sparks.
- **Flight:**
  - Core: additive Core colour, HDR ×8.
  - Shell: additive Outer cyan ×2, fresnel power ~2, `hs_noise_streak` scroll V 1.5 /s.
  - Trail: use `HS_spear_trail` as a child mesh, or a TrailRenderer (Time 0.12 s, Width 0.05 → 0, texture
    `hs_bolt_ribbon_tile` in Tile mode, Outer cyan ×3). At 150 m/s the TrailRenderer needs Min Vertex Distance ≤ 0.3 m.
- **Impact:** tight spark burst (8-12 sparks), a star card ×6 for 2 frames, then the conductor mark (below).

### 12. Conductor mark
- **Files:**
  - `textures/hs_conductor_mark_512.png`: 4 broken ring segments at 0.36 of the width, a centre diamond, and a faint
    inner guide ring
  - `fbx/HS_conductor_mark.fbx`:
    - `HS_mark_ring_segments`: 4 segments of 62 deg, r 0.25 m, 0.028 m wide
    - `HS_mark_diamond`: 0.07 × 0.087 m
    - `HS_mark_quad`: 0.694 m, so the PNG ring matches r 0.25 m
  - texture `hs_band_profile_64.png` (segment band)
  - previews `mark.png/.gif`, `mark_quad.png`, `tex_hs_conductor_mark_512.png`
- **Unity use:** billboard (a Billboard Renderer or a script that faces the camera) centred on the marked enemy's
  chest (body centre, offset toward the camera). Scale with enemy size: r ≈ 0.6 × body radius, clamped 0.25-1.2 m.
  - Mesh version: the segments rotate independently of the diamond.
  - PNG version: cheapest (one quad).
- **Settings:**
  - Additive Outer cyan ×3 (segments), Core ×4 (diamond).
  - Segment rotation 30-45 deg/s.
  - Pop-in: scale 1.4 → 1 over 0.1 s.
  - Chain arrival: brighten ×2 and scale 1.15 for 0.1 s.
  - Lifetime = mark duration, **6 s** (5b); fade over the last 0.3 s.
  - One mark at a time; moving it to a new target fades the old one over 0.1 s.
- **Beat:** Conduit Spear impact (secondary), chain preference visual.

### 13. Arc Step afterimage + ground trail
- **Files:**
  - `fbx/HS_step_ground_trail.fbx`: `HS_step_ground_trail`, a 9 m angular zigzag flat ribbon along +Z, 3 cm above the
    floor, 0.05 m wide, ±0.05-0.14 m lateral
  - textures `hs_trail_angular_512x128.png` (tileable along U), `hs_ghost_scanline_256.png`
  - previews `arc_step.png` (proxy figure + trail), `tex_hs_trail_angular_512x128.png`, `tex_hs_ghost_scanline_256.png`
- **Ground trail, Unity use:**
  - Mesh: pivot at the dash start, aimed along the dash vector (4 directions + blends; usable in the air, so use the
    mesh only when grounded and the TrailRenderer in the air). Scale Z = dash distance / 9 (dash ≈ 9 m in 0.25 s, 5b).
  - Or a TrailRenderer on `ground`/`root` [Ground]: Time 0.35 s, Width 0.05 m, Texture Mode Tile (1 tile per 0.5 m),
    `hs_trail_angular`, additive Arc cyan ×3.
  - Emit from dash start f7 until Arrive (end f5), then fade 0.3 s.
- **Afterimage (ghost material notes):**
  - At launch (start f7), bake the character's SkinnedMeshRenderers (`BakeMesh`) into a pooled mesh at the start
    position, including the halo pieces.
  - Material: additive fresnel rim (Outer cyan, HDR ×1.5, rim power 2-3) plus `hs_ghost_scanline_256` in screen space
    or world Y (8 px lines), scrolling 0.5 /s, at 25-35% body fill.
  - Alpha 1 → 0 over **0.35 s** (plan: 0.3-0.5 s, lives even if the step is cancelled), scale 1.0 → 1.03 and a slight
    drift opposite the dash.
  - Keep it faint ("thin enough to see body"). The Blender `PREVIEW_afterimage_proxy` shows the look only and isn't exported.
- **Also:** heel burst at launch (item 8), arrival spark puff (item 6), and the static ring decal at arrival (item 14).

### 14. Ground scorch / static ring decal
- **Files:**
  - `textures/hs_scorch_decal_512.png`: **alpha-blended** char with Lichtenberg cracks and a copper-dark rim
  - `hs_scorch_glow_512.png`: **additive** glowing cracks and copper embers, the same crack layout
  - `hs_static_ring_512.png`: **additive** jagged electric ring with spurs
  - quad `fbx/HS_quads.fbx` → `HS_decal_quad_1m` (1×1 m, faces up)
  - previews `decal_scorch.png`, `decal_scorch_ring.png`, `tex_hs_scorch_*`, `tex_hs_static_ring_512.png`
- **Unity use:** Unity 2021 built-in pipeline, so there's no URP decal projector. Two options:
  - A ground-aligned quad from a raycast (offset 1-2 cm along the normal).
  - The game's own decal component, if Phase G finds it usable.
- **Settings:**
  - Scorch (alpha blend): 1.5-2.5 m, life 4 s plus a 1 s fade.
  - Scorch glow (additive, Core ×3): same size and position, fading over 0.6 s.
  - Static ring (additive ×3): 0.6 → 2.0 m over 0.25 s while fading; add 2-4 arc particles.
- **Beats:** Discharge proc (scorch + glow + ring under the target, optional), landing (ring only, small, Land f2-6),
  Arc Step arrival (ring only, 1 m).

### Extras
- `hs_band_profile_64.png`: a clean soft band across V that tiles along U, for any glowing strip (mark, halo, burst ring).
- `hs_bolt_ribbon_stretch_1024x128.png`: a single full bolt for LineRenderer Stretch mode.
- `HS_card_quad_1m`: generic card for the glow textures.

## Blender materials (preview only)
`HS_M_*` materials in `hs-vfx-v07.blend` are pure additive (Emission added over Transparent), except
`HS_M_scorch_decal` (alpha mix). Each carries a custom property `hs_note` (texture, colour, strength). Preview
animation is keyed or driven in the .blend:
- Mapping nodes named `HS mapping` scroll by `frame*rate`.
- The spear dissolve and filament draw-on are keyed on the `HS threshold` value nodes (frames 1-24).

The strengths are tuned for the Standard view transform in the previews, not for Unity.
Every object carries a `unity_use` custom property (exported to FBX as a user property).

## Self-review notes
Findings from reviewing each preview, and what changed:
- **Textures:**
  - The first-pass bolts were too smooth (they read as wavy curves). Midpoint displacement is now fractal, with
    constant relative amplitude.
  - Glow tails were clipped into faint boxes; they now have a hard falloff to zero at 4.5× the glow width.
  - The streak noise was first too coarse, then too grainy; it's now band-limited 1D noise with a warp.
- **Meshes:**
  - The first renders blew out to white. Strengths were lowered, and the nozzle rim now fades.
  - The first jet burst read as a solid crescent; it's now radial spikes plus a shock ring.
  - Gap arcs were brightened so the bridged gaps read. The materialize filaments were thinned.
  - The halo gap arcs were split into 4 objects so the meter can light them one at a time.
- **Known limits:**
  - Crossed ribbons show a doubled line when seen exactly side-on at close range (normal for this technique).
  - The halo radius and gap angles aren't measured from the rig.
  - In the flat previews, the 10 m long bolts are only a few pixels wide.
