# Gaze tendril VFX candidate

> **Historical note:** This report records the initial candidate stage. Its statements about no screenshots and unverified visual intensity describe that stage, before the later approved native game captures of the thicker ambient tendrils and confirmed-hit connectors. The README now reuses one of those captures: [Gaze with the normal gameplay camera and HUD](../media/gaze-player-view.png). That visual approval does not establish multiplayer, physical controller, collision edge-case or performance acceptance. This documentation update adds no new game test.


Base: published `v1.1.0`, `957578597fa892b3bf22ef5ea6bf03a8161b523f`.
Branch: `codex/gaze-ground-tendrils`. Version constants and manifest remain unchanged.
Recommend **1.1.1** after playtest approval; this is not a release package.

## Changes

- Six broad, dim ground tendrils, each with one branch, refresh at 10 Hz throughout the firing channel without requiring an enemy hit. Impact geometry is conservatively clipped inside the current splash radius, including width. With no nearby endpoint ground, a small, dim splash below the caster shows channel energy; it is not a damage indicator. No floor means no ground geometry.
- Downward samples, slope rejection, height-step limits and segment world collision checks stop decorative paths at sampled gaps and obstacles. Decorative arcs use a colored inner line for readability, with no white core, strike burst, sound or light.
- Server core/splash contacts now produce white-core tendrils, as do the existing fork and chain hit events. They end at the position actually used for that hit, not a client-selected enemy. Grounded forks follow terrain when a complete route is available; otherwise confirmed contacts use a direct bolt. Hit snapshots last 220 ms and render immediately instead of the old staggered crawl.
- Geometry is reused per body: 12 dual-layer ambient strokes plus 12 dual-layer hit slots, 48 renderers total, nine vertices per stroke. New ambient sampling is bounded at 110 downward queries and 96 segment checks per refresh (early exits reduce this). Core/splash notifications are capped at four per eligible tick, at most one notification batch per 150 ms, independent of attack speed. Existing fork/chain event counts remain unchanged. Dense scenes may omit additional core/splash cosmetic contacts; damage is never capped by this budget.
- End, interruption, death and disable clear the geometry; body destruction/stage teardown removes its children. Late hit events are rejected while the owner is not actively channeling. The fixed pool overwrites oldest hit slots under pressure.
- Existing skin materials are shared, with no new textures, bundles, shaders, dependencies or registration-time asset loads. Missing materials suppress the affected renderer. Cosmetic effect-send failures log once and cannot interrupt a damage tick; the existing guarded effect registration remains intact.

## Validation

- Release compilation: 0 errors. Initial restore/build: 20 existing warnings; no-restore compilation: 19 (the restore duplicates the existing NU1701 package compatibility warning).
- Six gameplay/state/tuning files match the base exactly. Removing only the new cosmetic statements from `GazeServer.cs` reproduces the original file exactly. Damage, target searches, ordering, range, cadence, costs, status behavior and procs are unchanged. Version, plugin entry point and project dependencies also match the base.
- Sixteen deterministic checks execute the production `GazeTendrils.cs` against renderer/physics substitutes: bounded allocation across 1,000 refreshes/hits, pool expiry, empty channel, target endpoint, footprint, wall/gap/void cases, sky fallback, death, interruption, disable and repeated casts. These do not substitute for native Unity physics, shader or multiplayer testing.
- No game launch, screenshots, profile installation, publication, push, merge or tag.

## Later playtest

Use the same candidate on host and observer; retain the existing 1.1.0 asset bundle.

1. Channel with no enemies on flat ground and slopes. Expect broad, branching energy throughout firing; aim into sky, walls and over ledges. Check that the subdued caster splash reads as decoration and that no ground is invented over voids.
2. Hit one enemy, multiple enemies, a flying enemy and a moving enemy. Confirm bright bolts end at hit positions and remain distinct from ambient energy. Check fork and chain visibility on a remote observer as well as host, including late joining.
3. Recast, Arc Step, freeze/interrupt, die, disable/despawn and change stage during channeling. No tendrils should linger or restart from late packets. Repeat channels to check reuse.
4. Check all skin palettes and low/high graphics settings for readable width/bloom. Stress multiple Saints and high attack speed; confirm acceptable frame time and no growing object count. Compare health damage/procs, range and cooldown against 1.1.0.

Visual intensity, native collision behavior, real network latency and frame-time cost remain unverified until this playtest.
