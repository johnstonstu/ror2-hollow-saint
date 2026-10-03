# Local Unity import and movement proof

Authorized by Stuart on September 27, 2026. Keep everything local; no remote pushes.
Source character: art/anim/hollow-saint-anim-v31.blend. Preserve all source checkpoints.

1. Locate/install Unity 2021.3.33f1; create HollowSaintUnityProject using Built-In rendering.
2. Export the character and representative baked clips, retaining sockets and documenting VFX curve handling.
3. Import and verify scale, skinning, clips, materials, sockets and event timings.
4. Create a basic movement/animation test scene and verify its compile/runtime behavior.
5. Document what passes and what remains before the RoR2 bundle/survivor integration milestone.

Success criteria: editor opens the correct project/version; zero script compile errors;
imported mesh and rig remain intact; representative clips preserve their frame timings;
model bounds and facing are correct; sash bones and sockets survive; movement test
supports running, braking, jumping and representative clip playback. Render the imported
character for visual review. Do not describe an unexecuted scaffold as a working prototype.

Initial estimates: editor download/install is variable; project setup 15-30 minutes once
the editor is available; first import/animation test 30-60 minutes. Full RoR2 integration
is separate and will be estimated after checking dependencies and the dev profile.

Completed local preview milestone: see HollowSaintUnityProject/README.md. All five setup steps completed; full RoR2 integration remains separate.
