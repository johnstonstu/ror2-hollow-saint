# Hollow Saint: VFX and ability hookup plan (draft)

Drafted Sun Sep 27, 2026, ~12:35 AM PT, for Stuart. This is a **planning draft**: nothing is built or wired yet.
Every item carries one of two tags:

- **[REPO]** comes from files in this project: clip `clip.json` / `art/anim/v16/catalog.json` markers, the
  `tools/blender/anim/clips/*.py` and `vfx.py` constants, the rig README, and the docs listed below.
- **[PROPOSAL]** is a suggestion that nobody has approved yet. That covers cancel windows, new sockets, VFX looks
  beyond the concept prompts, Unity/RoR2 structure, and all timings that aren't a clip marker.

Sources I read: `HANDOFF.md`, `docs/design-brief.md`, `docs/ability-kit-workshop.md`, `docs/animation-plan.md`,
`docs/animation-handoff.md`, `docs/playable-character-roadmap.md` (section 5, effects), `docs/technical-notes.md`,
`art/concepts/hollow-saint-art-pack.md`, the concept prompts in `art/concepts/prompts/` (Arc Bolt, Open Circuit,
Arc Step, charge states), `art/hybrid/README-v15-v16-rig.md`, `art/anim/STATUS.md`, `art/anim/REFINEMENT-PLAN.md`,
`art/anim/v16/catalog.json`, and the per-clip `art/anim/wip/*/clip.json`. I didn't open Blender.

> **Live-data caveat.** The run-3 agent is reworking the heel jets (9d+9e) and will then do 10 EXPANDED
> (transitions). The jet/spark numbers below come from `glide.py`, `run.py` and `vfx.py` as they stood at ~12:30 AM,
> and they will change. Transition work may also add clips or retime starts and ends. **Re-read those files and the
> newest `vN/catalog.json` after run 3 finishes,** before you lock any event frame.

## 0. Ground rules from the repo

- **24 fps.** The character faces -Y in Blender. Clip frames are 1-based, and loops repeat frame 1 as their last
  frame (e.g. Run `1-17` is 16 unique frames). [REPO]
- **Gestures are upper-body layers.** Arc Bolt, Charge, Discharge and Open Circuit only move the spine up plus the
  arms and halo. Their `layer` is "upper body (mask excludes pelvis/legs)", so they can play over locomotion. [REPO]
- **Arc Step is in place.** The game moves the body. [REPO]
- **Palette and style.** Ivory, dark, copper and cyan. The effects are "white-hot cyan electricity with thin defined
  edges, localized highlights instead of full screen bloom". Ordinary attacks should look "precise and repeatable,
  not a giant ultimate beam", and the effects stay "thin enough to see body". [REPO: concept prompts, art pack]
- **Charge reads on the halo.** Gaps light up as charge builds. The charge-state board goes dormant (dark gaps) →
  charging (2 gaps bridged) → saturated (all 4 gaps bridged, nearly continuous bright rim, back cyan lines
  visible) → release (segments rotate outward like petals to vent). [REPO: `saint-charge-v1.txt`, kit workshop]
- **Glide VFX already exists in Blender** (`vfx.py`). Heel thruster cones and arcs are driven by keyed root-bone
  properties `hs_glow`, `hs_jet`, `hs_spark_L/R` and `hs_jet_dir`. STATUS says outright that "Unity drops the
  drivers; its VFX should read the keyed hs_glow/hs_jet curves." [REPO]
- **Blender jet colours** (linear RGB, emission strength) [REPO: `vfx.py`]:
  - outer: (0.25, 0.8, 1.0) at 9
  - core: (0.85, 0.97, 1.0) at 22
  - arcs: (0.55, 0.9, 1.0) at 16
  - Glow boost: emission × (1 + 1.6·hs_glow) on the cyan conductor, core and halo gap materials.
- **Gameplay values are hypotheses.** Arc Bolt's 0.5 s base interval, charge 0-100, and the empowered 270%/7-target
  shot all come from the design brief. Conduit Spear, Arc Step and Open Circuit, plus auto vs. manual charge release,
  are still **proposals**. [REPO: HANDOFF, design brief]

## 1. Sockets

### Existing (v16 rig, non-deforming bones) [REPO]
| Socket | Where | Use |
|---|---|---|
| `L muzzle` / `R muzzle` | Index fingertips. At Arc Bolt release the muzzle points within 0.06-0.08 deg of -Y (straight ahead) | Arc Bolt origin and fingertip flash, Discharge side bursts |
| `core socket` | Chest core (≈ -0.04, -0.115, 1.51 m) | Charge glow, Discharge core flash, spawn |
| `halo socket` | Halo centre | Charge-level halo glow, Open Circuit crown centre |
| `head socket` | Head | Mask seam flicker, optional select-screen snap glow |
| `L heel socket` / `R heel socket` | Heel spur. The jet mount sits at the socket tail +2 cm (Achilles base) | Run push-off sparks, lift-off burst, glide jets, glide-exit cut |
| `halo root`, `halo 1-4` | Halo lag root and the four arcs (non-deform) | Per-arc gap arcs, crown pulse emitters |
| `root` | Origin | Ground decals, Arc Step trail anchor, carries the `hs_*` curves |

### To add [PROPOSAL]
| New socket | Parent / placement | Why |
|---|---|---|
| `orb socket` | Child of `chest` (or `root`), keyed per frame to the midpoint of the two palms. Script data: Charge loop orb ≈ (-0.04, -0.30, 1.13), Charge full ≈ (-0.04, -0.36, 1.21) [REPO: `special.py` ORB_LOOP/ORB_FULL] | Charge orb has no bone today. A baked midpoint keeps the orb between the hands on every frame |
| `L palm` / `R palm` | Child of `hand`, palm centre, +Z out of the palm | Discharge and Open Circuit open-hand bursts. The muzzles are fingertips and point the wrong way for spread-hand releases |
| `L heel jet` / `R heel jet` | Bone version of the `VFX \| L/R heel jet` empties (heel socket tail +2 cm) | The current mounts are Blender **objects**, not bones, so FBX won't carry them. Orientation should follow the character (the Blender mount copies rig rotation), not the ankle |
| `back socket` | Upper back / yoke | Open Circuit tethers from the crown to the upper back (concept: "thin cyan electrical tethers ... to the upper back") |
| `halo arc 1-4 tip` (optional) | Children of `halo 1-4`, outer edge centre | Crown pulse strike origin and gap arcs between the segments |
| `ground` (optional) | Child of `root` at floor level | Landing, Arc Step and Discharge ground decals if you don't want a runtime raycast |

**Export flag [REPO + PROPOSAL]:** `animation-plan.md` says "FBX (deform bones only)". Every socket above is
non-deforming, so the export preset **must include the socket bones** (or re-create them as child transforms in
Unity). Otherwise the ChildLocator has nothing to point at.

## 2. Per-clip event sheet

Frame times are (frame - 1) / 24 s at 1.0× speed. "TBD - measure" means no data exists yet.

### 2.1 Arc Bolt right / left (Primary) [REPO: `primary.py`, `clip.json`]
- **Clip:** `HS_anim | Arc Bolt right` / `left`, frames 1-20 (0.79 s), upper-body gesture. Alternate L and R.
- **Events:**
  - Start 1.
  - Anticipation f3: the hand cocks by the shoulder with index and middle pressed to the thumb (finger accent).
  - **Release / fire f5** (0.167 s, 21% into the clip).
  - Recoil and halo flare f6.
  - Hold f8-11.
  - Recovered f20 (exact rest).
- **Hit window:** at f5. The hit is hitscan or an instant orb, so gameplay code resolves the hit and chain on the
  fire frame and the visuals only illustrate it. [REPO: roadmap "Code supplies the hand socket and actual hit/chain
  endpoints".]
- **Cancel window [PROPOSAL]:** locked f1-5, soft-cancellable from f6 (into another Arc Bolt, Arc Step, jump or
  sprint), with an Any-priority fade from f11.
  - Mismatch: the design brief interval is **0.5 s**, but the clip runs 0.79 s. Either scale playback to the fire
    interval (the release lands at 0.105 s at 0.5 s) or let the next shot cancel from ~f8.
  - **TBD - measure** whether f5 still reads when played at 1.6×+ speed.
- **Socket:** `R muzzle` / `L muzzle`.
- **VFX:**
  - f3: faint cyan charge glint at the pinched fingertips, plus 1-2 tiny crawling arcs along the forearm conductor
    [PROPOSAL].
  - f5: a white-hot fingertip flash (1-2 frames), then a **single thin jagged bolt** from the muzzle to the first hit.
    From there, one clean zigzag per chain hop, no branching (from the concept). Small impact sparks and a brief
    reflected light at each target.
  - f6: halo flare [REPO: the clip already flares the halo segments].
  - Bolt life: ~0.1-0.15 s with a 2-3 frame fade [PROPOSAL].
  - Empowered shot: thicker, whiter core and a brighter halo vent. The design brief's empowered shot is the 270% /
    up-to-7-target one.
- **Charge feedback (passive-like):** each hit raises halo gap brightness in steps. Show 2 gaps lit mid-charge and
  4 at full (from the charge board).
  - There's no "passive" skill doc in the repo. Charge-from-hits is the confirmed mechanic, and I'm treating it as
    the passive readout [PROPOSAL].

### 2.2 Aim poses (additive)
- **Clips:** `Aim up/down/left/right/neutral`, frames 1-2, static additive poses. The Unity reference pose is
  'Aim neutral'. [REPO]
- **VFX:** none. They only matter because `muzzle` must follow the aimed arm, so resolve the socket after the aim
  layer. Arc Bolt tracer origin should be read from the socket at fire time and not cached [PROPOSAL].

### 2.3 Charge loop / Charge full (charge mechanic; "Charge")
- **Clips** [REPO]:
  - `Charge loop`: 1-41 (40 f loop, 1.67 s). Pulse high f11, low f31. Cupped orb, tremor, halo breathing.
  - `Charge full`: 1-25 (24 f loop, 1 s). Pulse high f7, 3-beat heartbeat surge, halo held wide and crackling.
- **Events:** loops, so there's no fire. Anticipation, release and cancel are **TBD**, because the charge
  input/release design is still open (auto vs. manual) [REPO: open decision]. Pulse markers drive the VFX beats.
- **Cancel [PROPOSAL]:** cancellable on any frame (it's a hold), cross-fading 4-6 frames back to the locomotion
  upper body. Charge full → Discharge should cut on the next pulse or immediately, depending on the input design.
- **Socket:** `orb socket` (new), plus `core socket` and `halo socket`.
- **VFX:**
  - Charge loop: a small cyan orb of current between the palms, with thin arcs from each fingertip into the orb.
    Orb scale and brightness surge on f11 and dim on f31. Core glow tracks charge %.
  - Charge full: a larger, whiter orb with more frequent arcs, and 3 quick surges per loop keyed to the heartbeat
    (≈f7 peak). All 4 halo gaps bridged. Back cyan lines visible.
  - None of this should cover the body (thin arcs only).

### 2.4 Discharge (empowered release / "break the seal")
- **Clip:** `Discharge`, frames 1-28 (1.125 s), upper-body gesture. [REPO]
- **Events** [REPO: markers + `special.py`]:
  - Gather f5 (fingers clench to the core).
  - Tense f7.
  - **Release f9** (0.333 s).
  - **1-frame hit-stop, f9-10** (HITSTOP = 1).
  - Recovered f28.
  - `clip.json` already lists `socket_release: ["L muzzle", "R muzzle", "core socket"]`.
- **Hit window:** f9 (with f9-10 as the visual hold) [REPO frames; damage timing is a PROPOSAL].
- **Cancel [PROPOSAL]:** locked f1-10. Soft cancel from ~f14 into movement skills. Gesture fades out from ~f18.
  The earlier critique already said the recovery is long, so a cancel window there is reasonable.
- **Socket:** `core socket` (burst origin), `L/R palm` (new) or `L/R muzzle` for the two side lances.
- **VFX:**
  - f5-8: arcs draw inward from the fingertips to the core, and the core brightens.
  - f9: a white core flash, a radial ring pulse, and short thick bolts out through both hands along the fling
    direction.
  - Halo arcs burst outward [REPO: the halo spring burst is animated], with a matching "petal vent" flash in the
    halo gaps (from the charge board).
  - Optional ground scorch decal.
  - Sparks linger to ~f14, then fade.

### 2.5 Open Circuit / hold / end (Special)
- **Clips** [REPO]:
  - `Open Circuit`: 1-30 (1.21 s). Unfold f6, **Crown active f22**. Ends on hold f1.
  - `Open Circuit hold`: 1-25 (24 f loop). Pulse marker f1. Arm surge twice per loop, in step with the crown spread
    pulse (≈f4 and f16 from `sin(2·tau·t)`; **TBD - measure**).
  - `Open Circuit end`: 1-22 (0.875 s). Recall f3, Recovered f22.
- **Events:**
  - Anticipation f1-6 (hands rise to frame the core).
  - Release/"activation" at f22 (crown active).
  - Crown pulse strikes happen during hold. Their gameplay rate is **not** tied to the clip (the pulse interval is
    a PROPOSAL).
  - Recovery is the `end` clip.
- **Cancel [PROPOSAL]:** the crown persists for the buff duration whether or not the upper body is still in the
  gesture.
  - The hold is designed to layer over running, and the kit says the player keeps moving and attacking.
  - Arc Bolt should override the arms (upper-body mask) while the **halo bones** keep playing the hold on their
    own mask layer. STATUS already suggests "halo bones on their own avatar-mask layer would let the crown hold
    play under Arc Bolt".
- **Sockets:** `halo socket` (crown centre), `halo 1-4` / new arc tips (pulse origins), `back socket` (tethers),
  `L/R palm` (hands framing during the unfold).
- **VFX:**
  - f6-22: gap arcs spark as each segment separates. Thin tethers from `back socket` to each arc.
  - f22: crown ignition flash.
  - Hold: slow shimmer, plus a **brighter single pulse strike** from the crown to a target, then chains. This must
    look distinct from the thin hand bolt (from the concept).
  - End f3-22: tethers retract and the gaps dim as the ring re-forms.
  - A continuous crown spin, if wanted, belongs in Unity on `halo root` [REPO: STATUS].

### 2.6 Arc Step start / loop / end (Utility)
- **Clips** [REPO]:
  - `Arc Step start`: 1-7 (0.25 s). Dash start f7 (finger accent f4).
  - `Arc Step loop`: 1-11 (10 f loop).
  - `Arc Step end`: 1-16. Arrive f5, Recovered f14.
- **Events:**
  - Anticipation f1-6 of start. Dash start/"release" at start f7.
  - Travel: loop, for however long the dash lasts (a PROPOSAL; distance and duration are open).
  - Arrive: end f5. Recovery to f14.
- **Cancel [PROPOSAL]:** Arc Bolt allowed from end f5. Everything allowed from end f10.
- **Sockets:** `root`/`ground` (trail), `L/R heel socket` (small kick sparks at launch), `halo 1-4` (trail flicker).
- **VFX** (from the concept):
  - Launch: a faint blue translucent **afterimage** left at the start position.
  - Travel: one **thin angular cyan trail** just above the ground, restrained sparks, no explosion. Halo pieces
    trail slightly.
  - Arrival: a small spark puff at the feet, and the halo re-forms.
  - Arc Step keeps stored charge, so halo gap glow is unchanged [REPO: kit].

### 2.7 Locomotion that carries VFX (heel jets) [REPO: current `run.py`, `glide.py`, `vfx.py`; subject to run 3]
| Clip | Frames | Events | Jet behaviour now (keyed props) |
|---|---|---|---|
| Walk forward | 1-27 loop, 1.5 m/s. L contact 1, R contact 14 | heel-toe, grounded | **No thrust** (9d) |
| Run forward (+ backward/left/right) | 1-17 loop; 6.0 / 4.25 / 4.5 / 4.5 m/s. L contact 1, R contact 9 | toe-off each foot | `hs_spark_L/R`: 0.35 peak, rises into each toe-off and dies ~2.5 f later (by the formula ≈ L f3-7, peak ~f5; R ≈ f11-15, peak ~f13). `hs_jet_dir` = 12 deg below horizontal. Strafes/backpedal: **TBD** whether they get sparks too |
| Glide enter | 1-13 (0.5 s), from Run f10. Push off 3, Airborne 5 | last push-off = lift-off | Run sparks fade f3-5. `hs_jet` ramps in ≈f3.5-6 plus an ignition **burst** bump over ≈f3.5-10. `hs_glow` ramps ≈f2.5-11. Exhaust tilts to 58 deg (lift) by ~f5, then eases to 28 deg by ~f12 |
| Glide loop | 1-33 (32 f loop), 8.7 m/s. Hover high 1, low 17 | steady | `hs_jet` ≈ 1 with 3/7/11-harmonic flicker (seamless). `hs_glow` pulsing. `jet_dir` 28 deg |
| Glide exit | 1-14 (0.54 s), ends on Run f1. Legs down 5, L contact 14 | jets cut, feet plant | `hs_jet` cuts ≈f1.5-4.5 (fast). `hs_glow` fades ≈f2-11. Both 0 at the Run hand-off, no spark on landing |
| Jump / Ascend / Descend / Land | Jump 1-11 (Crouch 3, Takeoff 6). Ascend/Descend 1-21 loops. Land 1-15 (L touch 2, R touch 4, Compress 6, Recovered 15) | | No jets today. **[PROPOSAL]** a tiny heel spark at Jump f6 and a ground dust/static ring at Land f2-6 |

- **Sockets:** `L/R heel socket` → `L/R heel jet` (new bone). The exhaust trails back along the travel line,
  tilted by `hs_jet_dir` (9e).
- **VFX (Unity port) [PROPOSAL]:** keep the Blender structure: a hot white-cyan core cone, an outer cyan cone, and
  2-3 spinning zigzag ribbon arcs. Add a short additive trail and small ember sparks.
  - Drive scale from `hs_jet + hs_spark_side`, tilt from `hs_jet_dir`, and body/halo emission from `hs_glow`.
  - Run sparks are short flashes, and they must not turn into full jets.

### 2.8 Presentation (low priority)
- **Spawn** 1-72: Awaken f13, **Halo lit f38**, Ready f60. Core flicker on f13 and halo gap ignition arc by arc
  around f38 [PROPOSAL].
- **Select intro** 1-44: **Snap f13**, Halo flare f15. A fingertip snap spark at `R muzzle` f13 and a halo flash f15.
- **Idle** finger twitches at f30/f78 could spawn micro crackles at the fingertips [PROPOSAL].

### 2.9 Conduit Spear (Secondary) — PROPOSED, awaiting Stuart's approval (REFINEMENT-PLAN item 11)
Design chosen per Stuart's "use best judgment" (12:37 AM PT). Kit basis [REPO: `ability-kit-workshop.md`, concept prompt]:
an energy lance from an extended palm (not a carried weapon), focused single-target damage, a broken four-segment cyan
ring mark with a tiny centre diamond, one marked enemy at a time, chains prefer the mark, own cooldown, no charge spend.
- **Clip [PROPOSAL]:** `Conduit Spear`, 20 f (0.79 s), upper-body layer (plus an optional standing full-body variant).
  Thrown from the RIGHT palm, javelin-style; the left arm points at the target. Frames are **TBD until the clip exists**;
  the targets are:
  - anticipation f1-6, spear materializes along the R forearm f2-6
  - **release f7** (0.25 s, fraction 6/19 ≈ 0.32)
  - follow-through f7-11, recovery f12-20
- **Cancel [PROPOSAL]:** locked f1-7, skills/jump/sprint from f11, any-state fade from f14.
- **Sockets [PROPOSAL]:** `R palm` (launch point, +Z out of the palm), `L palm` (aim-guide glint), optional
  `spear socket` (child of R forearm, along the conductor) for the materialize.
- **VFX [PROPOSAL]:**
  - f2-6 materialize: thin cyan filaments crawl up the R forearm conductor and converge into a lance mesh that
    fades in from the tail to the tip; the core flashes at f6.
  - f7 release: palm flash (1-2 f) and a white-hot core lance projectile (~150 m/s) with a short cyan trail ribbon
    and a copper-tinted spark or two at the tail.
  - Impact: a tight spark burst plus the **conductor mark**, a flat broken cyan ring (4 segments + centre diamond)
    billboarded on the target's chest. It holds for the mark duration with a slow segment rotation and
    brightens when a chain arrives.
  - Chains arriving at a marked target use the normal thin Arc Bolt hop, so the lance stays the brightest event.

### 2.10 Directional heel jets — PROPOSED, awaiting Stuart's approval (REFINEMENT-PLAN items 9e + 9h)
- Emitters stay at the heel/Achilles mount (`L/R heel jet`). The **exhaust always trails opposite to the actual
  travel vector** in character space: forward run → back, backpedal → forward, strafe right → left, diagonals in
  between. Thrust visibly pushes the body along the travel direction.
- Replace the single pitch `hs_jet_dir` with a character-space travel vector keyed per clip (`hs_move_x`,
  `hs_move_y`; pitch stays as `hs_jet_dir`). In Unity, use the real velocity (`characterMotor.velocity` projected
  into model space), smoothed over ~4-6 f so a stick circle rotates the plume smoothly instead of snapping.
- Push-off sparks fire on each foot's toe-off in **all 8 directions**, sprayed opposite to travel. They get
  weaker in walk (none, per 9d) and stronger in sprint. In blends, fire only from the dominant clip's contact so
  sparks don't double.
- Spawn offset: when the exhaust direction points into the leg (backpedal, crossing strafes), offset the spawn
  point ~4-6 cm outward so the plume never passes through the shin.
- Glide: jets trail opposite to glide velocity. On a turn, the plume swings and the body leans in.
- Arc Step (multi-direction): a short heel burst on launch along the dash vector, and the trail follows the dash.

## 3. Unity / RoR2 hookup (planning level) [PROPOSAL unless marked]

Assumptions (verify against a working survivor template, e.g. HenryTutorial [REPO: technical-notes], and the game
version, 1.4.1 on Unity 2021.3.33 [REPO: HANDOFF]):

1. **ChildLocator** on the model maps the names above to transforms: `MuzzleL`, `MuzzleR`, `Core`, `Orb`, `PalmL`,
   `PalmR`, `Halo`, `HaloArc1-4`, `Back`, `HeelJetL`, `HeelJetR`, `Head`, `Ground`.
2. **One EntityState per skill.**
   - Duration = baseDuration / attackSpeed.
   - The fire moment is a normalized fraction taken from the clip marker, e.g. Arc Bolt 4/19 ≈ 0.21 and Discharge
     8/27 ≈ 0.30. This keeps the release frame and the gameplay hit together at any attack speed, which
     animation-plan.md requires [REPO].
   - Use `PlayAnimation(layer, state, "<Skill>.playbackRate", duration)` on a gesture layer, then spawn effects with
     `EffectManager.SimpleMuzzleFlash(prefab, gameObject, "MuzzleR", false)` for flashes, networked `EffectData` for
     tracers/impacts, and an orb/`LightningOrb`-style chain for hops [REPO: LightningOrb fields in technical-notes;
     reuse is unproven].
3. **Timed fraction or Animator event?** Prefer **timed fractions in the EntityState** for anything that deals
   damage (authoritative, speed-safe). Use Animator/AnimationEvents only for purely cosmetic beats: pulse surges,
   footstep sparks, Spawn/Select flashes.
4. **Heel jets and glow are persistent, not one-shots.**
   - Put a `HollowSaintVFXController` MonoBehaviour on the model. It reads the `hs_glow`, `hs_jet`, `hs_spark_L/R`
     and `hs_jet_dir` curves and drives the jet particle systems and material emission.
   - Unverified: whether Blender's FBX export plus Unity's "Animated Custom Properties" import carries bone custom
     property curves. If not, re-author them as clip curves in Unity's import settings from the same formulas, or
     compute them at runtime from sprint/glide state.
5. **Layers and masks.**
   - Base locomotion (blend tree: run fwd/back/left/right, walk, sprint → glide).
   - Upper-body gesture layer (mask excludes pelvis and legs [REPO]).
   - Additive aim layer (reference = Aim neutral [REPO]).
   - A separate halo-only layer so the crown hold survives Arc Bolt [REPO suggestion in STATUS].
6. **Networking.** Register every prefab with the effect catalog (R2API Prefab/Effects as needed). Give them finite
   lifetimes and bounded counts. Persistent jets are local-only cosmetics, driven on each client from the
   replicated sprint/glide state [REPO: roadmap requires registered networked effects and host/client testing].
7. **Prototype first.** The roadmap says to start with existing game effects for the combat prototype and never
   package extracted game assets [REPO].

## 4. VFX across transitions and cancels [PROPOSAL]

| Situation | Behaviour |
|---|---|
| Run → Glide enter → Glide loop | Jets are **one persistent system** that ramps (no respawn at the clip boundary). The last run spark hands over to the ignition burst [REPO: `enter_props`] |
| Glide loop → Glide exit → Run | Jets cut fast (~3 f) with a short afterglow on the trail. Glow fades slower (~10 f). No spark at touchdown [REPO: `exit_props`] |
| Glide → jump / fall / skill (interrupt) | Jets keep their current value and fade over ~4-6 f, never a hard pop. If a gesture starts mid-glide, the jets keep going (legs stay in the glide layer) |
| Run sparks during direction changes | Sparks belong to each toe-off. When the blend tree moves between run directions, sparks follow the blended foot contact; drop them instead of doubling |
| Arc Bolt cancelled after fire | The tracer and impacts are already spawned, so let them finish (they're short-lived). The fingertip glint fades with the hand |
| Arc Bolt cancelled before f5 | No flash. The anticipation glint fades over 2-3 f |
| Charge hold released / interrupted | Orb collapses inward over ~4 f (fade and scale down) unless it transitions into Discharge, where it becomes the gather flash |
| Discharge | The burst is fire-and-forget. Cancelling the recovery only fades the lingering sparks |
| Open Circuit active while doing anything else | Crown, tethers and pulses persist for the buff duration regardless of other gestures (halo layer). Tethers re-target smoothly when the body turns or dashes |
| Open Circuit ends early (death, stage change) | Tethers retract and the crown recall runs as a fade; no hard vanish while alive. Death: ragdoll + fade |
| Arc Step | The afterimage spawns at launch and lives ~0.3-0.5 s even if the step is cancelled. The trail stops emitting at Arrive (end f5) and fades |
| Hard cuts | Only for death/despawn and stage transitions. Everywhere else, fade 2-6 f |

## 5. Phase checklist (ordered)

1. **Wait for run 3** (9d+9e heel jets, then 10 EXPANDED). Then re-read the event frames from the newest
   `vN/catalog.json` and `clip.json` files, and the `hs_*` formulas in `glide.py` and `run.py`.
2. **Blender, add sockets** (new numbered file, background only, never overwrite a checkpoint): `orb socket`
   (baked palm midpoint), `L/R palm`, `L/R heel jet` bones, `back socket`, optional halo arc tips and `ground`.
   Add a socket position/orientation line to QA per clip at each event frame.
3. **Marker sweep.** Make sure every VFX beat above has a named marker in its clip (e.g. Arc Bolt `Anticipation`
   and `Bolt release` exist; Charge pulses exist). Add `Cancel` markers once Stuart approves the windows.
4. **Export test.** One small FBX (Arc Bolt R + Glide enter) with deform bones **plus socket bones**, unit scale,
   -Y → +Z. Check the sockets survive, and whether the `hs_*` curves import. Then the full set.
5. **Unity 2021.3 project.** Import, Animator (base locomotion, gesture layer with mask, additive aim, halo layer),
   ChildLocator, and the `HollowSaintVFXController` for jets and glow.
6. **Build prefabs, in the order the roadmap recommends:**
   1. Arc Bolt: fingertip flash, bolt/tracer, one chain hop, impact.
   2. Halo charge glow.
   3. Heel jets and run sparks.
   4. Discharge burst.
   5. Open Circuit crown, tethers and pulse.
   6. Arc Step afterimage and trail.
   7. Presentation.
7. **Wire EntityStates:** fire at normalized marker fractions, playbackRate scaled to attack speed, effects through
   the ChildLocator, registered and networked.
8. **Test:** attack speed 1× / 2× / 3× (flash on the release frame), move-and-fire, glide interrupts, cancels,
   crowds (effect counts), and host plus client.

## 5b. Cooldowns and tuning (proposal) — PROPOSED, awaiting Stuart's approval
Everything here is **[PROPOSAL]** unless tagged [REPO]. Clip lengths are at 24 fps. Durations scale with
attack speed where noted. The Discharge meter replaces "charge": 0-100, no decay, reset on death [REPO: design
brief policy], and item-proc damage gives 0 [REPO].

| Ability | Cooldown / interval | Duration & anim fit | Damage | Proc coeff. | Meter gain |
|---|---|---|---|---|---|
| **Arc Bolt** (primary) | 0.5 s interval / attack speed [REPO] | 20 f clip (0.79 s); fire f5 (0.167 s at 1×). See the resolution below | 180% + 3 chains at 80%, 15 m hops [REPO] | 1.0 direct / 0.2 chain [REPO] | +10 direct, +3 per chain hit [REPO numbers, now for the meter] |
| **Conduit Spear** (secondary) | **5 s**, 1 stock | 20 f (0.79 s), release f7 (0.25 s); state duration 0.6 s / attack speed, recovery cancellable | **450%** single target | **1.0** | **+15** on hit; mark lasts **6 s** |
| **Arc Step** (utility) | **5 s** per stock, **2 stocks** | start 7 f (0.29 s windup; trim to ~4 f of gameplay lock) + dash **0.25 s** / **~9 m** + end, cancellable from end f5 | none | — | **0** (preserves meter [REPO]) |
| **Open Circuit** (special) | **12 s** (starts when the cast ends) | cast 30 f, crown active f22 (0.88 s); buff **8 s**; pulse every **1.0 s** | **300%** per pulse + 2 chains at 100% | **0.5** pulse / 0.2 chain | **0** (pulses must not build charge [REPO]) |
| **Discharge** (passive) | Triggers at 100 on the next enemy hit | Overlay 28 f (1.125 s), release f9. The damage fires at the trigger hit, so the overlay enters at ~f7 (see below) | **270%** on the triggering target + **120%** chains to up to **6** more [REPO empowered-shot numbers] | 1.0 / 0.3 | 0 (consumes the meter; its own hits give nothing [REPO]) |

Headlines: Arc Bolt 0.5 s (repo) · Conduit Spear 5 s · Arc Step 2×5 s · Open Circuit 12 s (8 s buff) ·
Discharge ~10 direct bolt hits to fill (~5 s of steady fire at 1× on a single target).

**Arc Bolt 0.79 s clip vs. 0.5 s fire rate: recommendation.** Keep the 0.5 s interval [REPO] and don't
speed up the whole clip (at 1.58× the release lands at 0.105 s and the follow-through gets lost).
- Instead, the EntityState lasts 0.5 s / attack speed, with playbackRate = attack speed.
- The next shot (other hand, since L/R alternate) interrupts at state end, around f13, with a 3-frame
  crossfade. The recovery tail f13-20 only plays when firing stops.
- Fire stays at the normalized marker (f5 = 0.167 s × 1/attack speed).
- Optional: compress the anticipation (f1-5 played at 1.5×) if 0.167 s feels laggy in playtests. Then add an
  `Interrupt` marker at f13 and QA the f13 → other-hand f1 blend in 10 EXPANDED.

**Discharge overlay timing.** The burst is authoritative and instant on the trigger hit. A 0.33 s gather
would lag behind it, so enter the overlay at f7 ("tense") with a 2-frame crossfade, or author a short
`Discharge snap` variant (~14 f, release f3) in Phase C. It plays on the upper-body layer over every state
(idle, run, glide, air, Arc Step, mid-cast).

## 5c. Custom lightning asset list — PROPOSED, awaiting Stuart's approval
Source assets live in `art/vfx/assets/` (Blender sources `hs-vfx-vNN.blend`, numbered, never overwritten), with
textures in `textures/`, meshes in `fbx/` and previews in `previews/`. Palette: white-hot cyan core
(0.85, 0.97, 1.0), outer cyan (0.25, 0.8, 1.0), arc cyan (0.55, 0.9, 1.0) [REPO: `vfx.py`], and copper accents
matching the halo.

| # | Asset | Type | Unity use |
|---|---|---|---|
| 1 | Short arc (3 variants) | mesh strip / curve → FBX + UV'd ribbon | Arc Bolt chain hop, fingertip crackle, halo gap arcs |
| 2 | Long bolt (3 variants) | ribbon mesh | Arc Bolt primary tracer, Open Circuit pulse strike |
| 3 | Branching bolt (2 variants) | ribbon mesh | Discharge side lances, empowered shot |
| 4 | Ring arc (halo) | segmented ring mesh | Open Circuit crown shimmer, Discharge petal vent, charge gaps |
| 5 | Bolt flipbook | 4×4 sprite sheet PNG (alpha) | particle texture-sheet animation for all bolts |
| 6 | Spark sprite | PNG (alpha) | impact / push-off / materialize sparks |
| 7 | Glow / corona cards | 2-3 PNGs (soft, star, ring) | fingertip flash, core flash, orb, palm flash |
| 8 | Heel-jet exhaust cone + ribbon | mesh + scrolling material | glide jets, run push-off, Arc Step launch burst |
| 9 | Noise / scroll texture | tileable PNG | UV scroll for jets, trails, crown |
| 10 | Discharge meter glow ramp | gradient PNG (dark → copper → cyan → white) | halo/core emission by meter %, meter UI |
| 11 | Conduit Spear projectile | mesh (lance) + trail ribbon | secondary projectile + TrailRenderer |
| 12 | Conductor mark | broken 4-segment ring + diamond, PNG + mesh | Conduit Spear mark decal/billboard |
| 13 | Arc Step afterimage + ground trail | ribbon + ghost material notes | utility |
| 14 | Ground scorch / static ring | decal PNG | Discharge, landing, Arc Step arrival |

## 6. Open questions for Stuart

1. ~~Charge release: auto at 100 or manual?~~ **Decided by Stuart:** Discharge is a passive; the meter fires automatically on the next enemy hit at 100%, played as an upper-body overlay. Original question: That decides whether the Charge loop/full clips are a held input, and
   where Discharge plugs in (empowered Arc Bolt vs. a separate button).
2. Is **Discharge** the empowered primary shot, its own skill, or the Open Circuit cast? The docs list Charge/
   Discharge as clips, but the four-slot kit is Arc Bolt / Conduit Spear / Arc Step / Open Circuit.
3. ~~Conduit Spear has no animation. Is it still in the kit?~~ **Resolved (proposed, 12:45 AM PT):** yes. Right-palm thrown energy lance with a forearm materialize beat and a conductor mark; see 2.9 and REFINEMENT-PLAN item 11. Awaiting Stuart’s approval of the design.
4. Cancel windows (all proposals above): OK to adopt them as markers?
5. (Recommendation in 5b: keep 0.5 s, next shot interrupts at ~f13.) Arc Bolt: 0.79 s clip vs. a 0.5 s fire interval. Scale the clip to the interval, or allow a cancel into the next
   shot?
6. Heel jets on strafes and backpedal: proposed YES in 2.10 (all 8 directions). Jump/land: still open.
7. Should the crown keep striking while the player is gliding or Arc Stepping?
8. Exact bolt colour: keep the Blender jet cyan (0.25, 0.8, 1.0) / white-hot core for everything, or give the
   crown pulse or empowered shot a distinct tint (e.g. whiter/hotter)?

_Updated Sep 27 ~12:45 AM PT: added 2.9 Conduit Spear, 2.10 directional heel jets, 5b cooldowns/tuning, 5c asset list (all PROPOSED, awaiting Stuart’s approval). Backup of the previous version: `VFX-ABILITY-PLAN.bak2.md`._
