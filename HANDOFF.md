# Hollow Saint handoff

Read `next-session-handoff.md` first for v0.7.2: rear-arm/elbow/underside current and the
accepted ring-fed Arc Bolt throw, staged with bundle09. Older build history is
in `docs/NEXT-SESSION.md` below.

Current state only. History lives in `docs/archive/` (the long 2026-09-27 handoff is
`docs/archive/HANDOFF-20260927.md`).

## Current work: v0.5.2 sprint silhouette and skin lightning

Final DLL staged with unchanged bundle05; listing refreshed, startup checks pass.
User handles gameplay testing; avoid routine game launches to conserve usage. See `docs/NEXT-SESSION.md`
for current verification/staging status and `PLAYTEST.md` for acceptance.
This pass adjusts the rear sprint silhouette, makes heel exhaust compact, and
matches owned lightning/orbs to the equipped skin. Movement balance and audio
stay unchanged; no terrain foot IK is included.

## Earlier foundation (v0.4.3)

| Area | State |
|---|---|
| Model, rig, 65 source clips | Done in Blender (`art/anim/hollow-saint-anim-v31.blend`). |
| Game foundation (M1) | Verified in game 2026-09-27. |
| Skill kit (M3) | Four skills and Answered Prayer playtested in v0.4.0. v0.4.2 playtested positively; v0.4.3 shows only earned orbs, removes the HUD meter and adds 0.25 seconds of flight before impact. Awaiting next playtest. |
| Animation | bundle03: per-layer gesture rates and Open Circuit recall on the Halo layer; unchanged for v0.4.3. |
| VFX and SFX | Code-built lightning, ghosts, impacts, crown, reticle, meter glow, heel jets, afterimages; vanilla Wwise placeholders. |
| Select screen | Icons, portrait, description, keywords, lore, intro animation and flourish, Obsidian Saint skin. |
| Tuning | BepInEx config plus a Risk of Options page. |

**Next action: the playtest in `PLAYTEST.md`.** Rollback instructions are there too.

## Read first

1. `PLAYTEST.md` and `TODO.md`: what to test now and the master checklist.
2. `docs/kit-architecture.md`: how content, skill families, networking and damage hooks work, and the log lines that prove a build.
3. `docs/unity-vfx-anim-spec-20260927.md`: the 5-layer animator and clip import plan.
4. `docs/skins-and-loadouts.md`, `docs/ability-kit-workshop.md` and `art/concepts/kit-v2/`: design intent and reference art.

## Next

See `docs/NEXT-SESSION.md` and `PLAYTEST.md`. Next: evaluate passive readability,
then enemy shock overlay, Open Circuit audio/tendrils, footsteps, heel jets and spear persistence.

## Build pipeline

- Clips: `blender --background --factory-startup --python tools/blender/export_unity_clips.py -- v31_clipsNN`
- Bundle: Unity batch `-executeMethod HollowSaint.Preview.Editor.FoundationBundleBuilder05.RunBatch` (already built; use a new numbered builder/output for future changes, never overwrite bundle05)
- Verify: `-executeMethod HollowSaint.Preview.Editor.VerifyBundle02.RunBatch`; stills: `RenderBundle02Poses.RunBatch`, `RenderSkinPreview.RunBatch`
- Mod: `tools/dev-profile/Stage-Build.ps1 [-Bundle artifacts/foundation/bundleNN/hollowsaintassets]`

## Open design calls (PROPOSAL values in `KitTuning`)

- Open Circuit builds Static with its configured pulse weighting; charges come from Electrocutes.
- Conductor Mark: x1.5 damage from Hollow Saint skills.
- Arc Step i-frames: none.
- Chain hops proc at 0.5; Open Circuit pulses proc at 0; Thunderbolt procs at 1.0.
