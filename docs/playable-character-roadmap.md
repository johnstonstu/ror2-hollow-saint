# Hollow Saint: roadmap to a playable survivor

Planning only, 2026-09-26. No installation, gameplay implementation, export,
or publication is authorized by this planning session. Resume when requested.

## Starting point

- Selected Hollow Saint / Cracked Icon model, current animation study v10.
- Six animation studies; corrected weights, finger pivots and halo controls.
- No C# mod, Unity asset project, exported game asset or in-game test yet.
- Earlier environment inspection found the game using Unity 2021.3.33f1;
  installed Hub editor was Unity 6. Compatible asset tooling remains unverified.
- An empty, separate Hollow Saint Dev r2modman profile already exists.

## 1. Establish a working development pipeline

- Recheck game patch and current compatible BepInEx/R2API dependencies; pin versions.
- Prepare the dedicated development profile and verify a minimal original plugin.
- Choose an appropriate patched Unity 2021 LTS editor compatible with the game;
  validate a tiny asset bundle before committing the entire asset project.
- Set up repeatable local C# build and asset-bundle export/import steps.
- Keep existing game profiles intact and preserve the Blender source checkpoints.

Done when: the plugin registers its expected test content, and a small custom
bundle asset visibly appears in game with correct material and scale.

## 2. Build a first playable combat prototype

- Register a separate selectable survivor with body, master, stats, movement,
  camera, team, health/hurtboxes, skill slots and descriptions.
- Use temporary visuals where needed to test gameplay immediately.
- Implement aimed primary hit, bounded automatic chains and hit-earned charge.
- Decide whether full charge discharges automatically or uses a separate input.
- Server owns target selection, damage and charge; synchronize the visible state.
- Explicitly define misses, obstructions, dead targets, repeated hurtboxes,
  item-triggered damage, charge reset and empowered-shot behavior.
- Add a readable charge indicator and provisional configurable tuning.

Done when: a selected survivor can enter a stage, move, aim, hit an isolated
enemy, chain through a crowd and build/spend charge as specified. No duplicate
targets, recursive item chains or unintended changes to vanilla survivors.

## 3. Prepare the model for gameplay

- Inspect and correct shoulder/elbow deformations; selectively retopologize if
  weights alone cannot support the necessary range of motion.
- Validate hips, knees, ankles and wrists under locomotion poses; the v10 lower
  body staying still is not proof of good walking/running deformation.
- Choose a simple tabard solution: restrained bone animation first; simulation
  only if needed. Avoid legs passing through the cloth.
- Establish stable root, export axes, scale, bind pose and bone naming.
- Convert export-relevant curves/modifiers to appropriate mesh output in a copy.
- Export baked clips and model, checking materials, textures and transparency.
- Add hand emission sockets, core/halo attachment points and item-display anchors.

Done when: the custom model imports at the correct size/orientation, retains
its materials, and survives representative extreme poses without major tearing.

## 4. Complete the gameplay animation set

- Choose grounded running versus hovering; this affects the whole locomotion set.
- Add movement/sprint, directional blending, jump, airborne/fall, landing and death.
- Refine primary/charge/discharge and any approved supporting ability clips.
- Build the Unity Animator transitions and upper-body aiming/attack layers so
  the character can move while firing. Match attack speed and cast timing.
- Keep game-driven movement consistent with animation; the current dash is
  an in-place pose study with no actual displacement or collision handling.

Done when: no T-poses or stuck states during move/fire/jump/land/death transitions;
attack visuals coincide with the actual hit at different attack speeds.

## 5. Create lightning effects and sound

Start with runtime references to suitable existing game effects/sounds for the
combat prototype, then replace them with a cohesive custom effect family.
Do not package extracted game assets for distribution.

| Effect | Purpose | Priority |
|---|---|---|
| Fingertip flash | Show the instant a cast fires | First playable visual pass |
| Hand-to-hit lightning | Communicate the aimed initial strike | First playable visual pass |
| Target-to-target arcs | Show the actual selected chain route | First playable visual pass |
| Hit sparks | Confirm impact without hiding targets | First playable visual pass |
| Charge/core/halo glow | Show buildup and full-charge readiness | First playable visual pass |
| Empowered discharge | Distinguish the charged shot | First playable visual pass |
| Spear and target mark | Support proposed secondary | After kit decision |
| Dash trail/afterimage | Communicate proposed movement skill | After kit decision |
| Crown pulse and recall | Support proposed special | After kit decision |

Proposed implementation: a reusable jagged line effect with emissive material,
small particles for sparks, trails for motion and controlled material emission
for charge. Inspect a working RoR2 effect and its registration/networking first.
Unity's built-in Line Renderer and Particle System are candidates; no VFX Graph
or paid effect package is required by this plan.

Code supplies the hand socket and actual hit/chain endpoints. Damage is resolved
by gameplay code; the visual arc illustrates that result. Register effects for
networked playback, give them finite lifetimes, bound their counts and test
visibility/performance at normal gameplay distance and high attack speed.

Sound list: cast snap, impact, charge buildup, full-charge cue, empowered crack,
dash and crown pulse. Use existing runtime sound events for the prototype;
investigate a compatible Wwise soundbank workflow for original audio later.

## 6. Finish the chosen kit and presentation

- Confirm secondary, utility and special before polishing their assets.
- Conduit Spear, Arc Step and Open Circuit remain proposals, including cooldowns,
  damage, dash collision/air behavior and crown targeting rules.
- Add skill icons, crosshair, portrait, selection display, descriptions and charge UI.
- Check character lighting, damage flashes, invisibility, item displays and shadows.
- Add spawn/selection polish and optional ragdoll after the core loop works.

Done when: every skill slot has intentional usable behavior and clear feedback.

## 7. Validate a local playable build

- Test isolated targets, crowds, obstacles, flying enemies and bosses.
- Check attack/movement speed changes and representative on-hit items.
- Verify charge and effects across death, stage changes and run restart.
- Test host and client separately; ensure no doubled damage, missing effects,
  divergent charge or persistent objects after interruption/disconnect.
- Check sustained late-run effects load, logs and material/animation failures.
- Package a local r2modman import with explicit dependency versions and instructions.

Done when: a local run can be played through with the agreed kit, readable
feedback and no known blocking errors. Multiplayer claims require actual
host/client testing. Public Thunderstore release is a separate approval step.

## Recommended next work session

Begin with pipeline setup and a minimal selectable survivor. Validate the small
Unity bundle, then implement the primary/charge prototype using temporary effects.
The first custom VFX task should be one complete Arc Bolt: hand flash, main bolt,
one chain link and impact. Expand the kit after this end-to-end path works.

Decisions can wait until implementation: grounded/hover locomotion, automatic/
manual charge release, and approval or revision of the supporting three skills.
No answer is needed to close this planning session.

## Research references

- [RoR2 Unity version](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Unity-Version/):
  game engine baseline; verify editor/bundle compatibility during setup.
- [First mod](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Getting-Started/First-Mod/)
  and [R2API](https://github.com/risk-of-thunder/R2API): plugin/dependency foundation.
- [Custom character workflow](https://risk-of-thunder.github.io/R2Wiki/Custom-Character-Creation/):
  reference architecture; check age, API compatibility and reuse terms for examples.
- [Loading assets](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Loading-Assets/):
  bundle, language and soundbank integration.
- [Unity Line Renderer](https://docs.unity3d.com/2021.3/Documentation/Manual/class-LineRenderer.html)
  and [Particle System](https://docs.unity3d.com/2021.3/Documentation/Manual/Built-inParticleSystem.html):
  candidate custom lightning/spark building blocks.
- [Wwise setup](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Sounds/WWise/Getting-Started/):
  later custom audio investigation.
