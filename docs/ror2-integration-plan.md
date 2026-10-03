# Hollow Saint: Unity to RoR2 implementation plan

Research/planning pass, September 27, 2026 PT. No gameplay implementation, package
installation, profile modification, commit or push performed in this pass.
Stuart selected **demo time new** as the compatibility reference. Work remains local.

## 1. Starting point and authority

The Unity movement preview works, but it is not a RoR2 character yet. Current evidence:

- Unity 2021.3.33f1, Built-In rendering; scene `ImportProof04`.
- v31 Blender source: 65 clips; Unity proof imports 15, with 137 mesh renderers and
  83 bones. Socket/hem samples and runtime skin deformation passed validation.
- Sash movement is baked follow-through. Dynamic cloth is not implemented.
- VFX source v07 contains 29 meshes in 11 FBXs and 25 textures; game effects remain unwired.
- No RoR2 components/dependencies were found in the current preview scripts/package manifest.
- Source strict animation QA remains **15/65**. Import success does not clear contact,
  pop or blend failures. Final animation polish remains a separate acceptance gate.
- Steam manifest still reports build **21587608**. Prior local inspection identified
  game 1.4.1. Recheck game/API versions when locking the implementation toolchain.
- Read-only profile inventory found `demo time new`, `demo time`, `ah641.1test`, and
  `Hollow Saint Dev`. The reference list contains AH64, tools, fixes and R2API modules;
  no separately named item-content pack was identified. This is package-list evidence,
  not proof of what every DLL registers. Runtime catalog inspection will settle that.

The approved [master plan](../art/MASTER-PLAN.md), especially its final decisions,
overrides older tentative mechanics in `implementation-plan.md` and early VFX notes.
This document proposes the integration details; it does not reopen approved kit choices.

## 2. Architecture and ownership

Keep the existing preview as an art regression scene. Build a separate game-facing
model prefab and C# plugin; do not ship the preview motor, camera, GUI or test runner.

| Layer | Owns | Implementation direction |
|---|---|---|
| Blender/export | Mesh, rig, animation, baked motion, marker manifest | Preserve source checkpoints; export all required clips/sockets reproducibly |
| Unity assets | Model prefab, Animator/masks, mounts, materials, VFX, icons, ragdoll | Author spatial/visual data and build an asset bundle |
| BepInEx plugin | Registration, kit, balance, meter, compatibility | Original modular C# using matching RoR2/R2API APIs |
| RoR2 systems | Movement/input, health, inventory, skills, catalogs, network lifecycle | Use native components and state machines |
| Compatibility data | Item/equipment placement and optional integrations | Versioned rules keyed by stable content identities |

Proposed modules: Content, Survivor, States, Combat, Charge, Presentation,
ItemDisplays, Compatibility, and DevTools. Dependencies point from presentation
toward gameplay data; visual effects never decide damage or charge.

The game-facing body needs CharacterBody, CharacterMotor/motor dependencies, input,
health/team/inventory integration, skill locator, state machines/network state machine,
camera/model locators, hurtboxes, and the appropriate network identity/transform setup.
Use a verified vanilla-compatible body construction pattern, with owned cloned assets;
never mutate a shared vanilla body. Model child owns Animator, CharacterModel renderer
infos, ChildLocator, skins and item displays. Add survivor/select-display definitions,
portrait, language tokens, crosshair, and a master/AI setup for Vengeance/Gummy clones.
Verify exact component wiring against the installed assemblies before construction.

Henry's maintained tutorial is a reference for this pattern, including ChildLocator,
renderer integration, state machines and displays; it is not a promise that copying a
template unchanged will work. Check reuse terms before importing source/assets.
[Reference](https://github.com/ArcPh1r3/HenryTutorial/wiki/Tutorial).

## 3. Toolchain and build pipeline

1. Pin game build, compiler references, BepInEx/runtime package versions and the R2API
   modules actually used. Record a lockfile plus a compatibility manifest.
2. Retain Unity 2021.3.33f1 for the established proof. Validate a tiny real asset-bundle
   load in game before scaling up. Any editor migration gets a separate compatibility test.
3. Add a compatible, pinned ThunderKit/RoR2 import workflow and RoR2EditorKit for
   game-component authoring. First test assembly import/compilation without changing
   `ImportProof04`; keep imported game assets local and out of distributable bundles.
4. Use a separate netstandard2.1 plugin project. The current R2API build configuration
   references Unity 2021.3.33 and GameLibs 1.4.1-r.0, supporting that starting point;
   package compile versions and installed mod-package versions are different things.
5. Register bodies, masters, survivor/skill definitions, states, buffs, effects and
   projectiles through one deliberate content-pack path. Do not double-register via
   both a custom provider and convenience APIs.
6. Build DLL + custom asset bundle to a local staging directory, validate contents,
   then install only into Hollow Saint Dev during the implementation phase.

Candidate dependencies: BepInEx/RoR2BepInExPack and necessary ContentManagement,
Prefab, Language modules; Items for the chosen display integration, DamageType for
skill provenance, Networking only if extra messages are needed, and other modules
only when used. Avoid requiring the entire R2API umbrella by habit.

Sources: [editor version](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Unity-Version/),
[ThunderKit workflow](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/ThunderKit/Crash-Course-and-Getting-Started/),
[EditorKit package](https://raw.githubusercontent.com/risk-of-thunder/RoR2EditorKit/main/package.json),
[R2API build references](https://raw.githubusercontent.com/risk-of-thunder/R2API/master/R2API.props),
[content packs](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/C%23-Programming/IContentPackProvider/).

## 4. Attachment contract: item mounts and effect sockets

ChildLocator maps stable public names to transforms. A bone can keep its Blender
name while exposing a conventional locator name. Prefer Unity child transforms on
the game-facing prefab for static offsets; add/bake bones only when the socket needs
its own authored motion. Validate scale/orientation and uniqueness automatically.

Proposed mount groups (final exact aliases checked against vanilla/template content):

| Region / locator aliases | Parent or placement | Fitting intent |
|---|---|---|
| Head, face, crown | Head bone, separate face/top offsets | Glasses/headwear fit the blank mask without covering the entire identity |
| Chest, stomach, pelvis | Corresponding torso bones | Torso items clear the cyan core and front sash |
| UpperArmL/R, LowerArmL/R, HandL/R | Arm and hand bones | Armbands/weapons follow casts without crossing palm effects |
| ThighL/R, CalfL/R, FootL/R | Leg and foot bones | Leg items clear the moving sash and heel jet outlets |
| ShoulderL/R, back, equipment slots | Stable shoulder/chest-relative mounts | Large items avoid the halo's swept volume and camera |
| Purpose-built side/hip mounts | Pelvis/chest-relative | Alternative positions for bulky equipment and collision-heavy items |

Keep gameplay/VFX names separate: left/right muzzle and palm, core, halo center and
arc tips, heel emitters, spear, orb, back tether, and ground reference. Existing rig
already has muzzle/core/halo/head/heel sockets; missing palm/orb/tip/other mounts need
explicit creation and validation. A ground reference does not replace a terrain raycast.
Do not attach ordinary equipment to swaying halo arcs or cloth tips by default.

Produce a mount manifest with name, transform path, parent, axes and intended use.
Changing a public alias is a compatibility change, not a casual rig rename.
The reference tutorial explicitly recommends familiar body-part locator names for
compatibility and explains that only display-relevant bones need entries.
[ChildLocator guidance](https://github.com/ArcPh1r3/HenryTutorial/wiki/Tutorial#childlocator-1).

## 5. Item display implementation and compatibility policy

**An attachment point is not an item placement.** Each ItemDef/EquipmentDef needs a
display-rule group: follower prefab, named child, local position, angles and scale,
plus the appropriate rule type/limb mask where applicable. One item can have multiple
rules. Visual rules must not alter item stats or proc behavior.

### Vanilla and installed DLC

- Enumerate the actual loaded ItemCatalog and EquipmentCatalog; avoid a hard-coded
  historical item count. Include visible base-game/DLC items and equipment. Record
  intentionally invisible/internal content separately from missing display support.
- Resolve display prefabs from loaded game content. Do not assume a ground pickup
  model is a valid worn display, or bundle copied vanilla assets.
- Give Hollow Saint its own ItemDisplayRuleSet. Existing survivor rules can suggest
  prefab references and starting transforms, but every placement needs refitting.
- Store editable, versioned placement data rather than thousands of hand-edited
  C# statements. Validate required references, finite transforms, sensible scales,
  unique ownership, and locator resolution before generating runtime groups.
- Use ItemDisplayPlacementHelper in the isolated test profile for in-game fitting.
  Its current published tool supports editing rules and exporting JSON/C#; pin a
  version compatible with the installed game. Keep it a development-only dependency.
  [Maintainer's tool](https://github.com/KingEnderBrine/-RoR2-ItemDisplayPlacementHelper),
  [maintainer's changelog](https://thunderstore.io/c/riskofrain2/p/KingEnderBrine/ItemDisplayPlacementHelper/changelog).

### Modded items

Proposed precedence: explicit Hollow Saint compatibility override, item author's
Hollow Saint-specific rule, validated generic rule, then unsupported-display report.
This is our desired policy; test its implementation against the pinned R2API version.

R2API's inspected source applies model/body-specific rules and can otherwise use
defaults. It provides `DoNotAutoIDRSFor` to suppress nonspecific defaults. Specific
rules may replace existing groups, so initialization order matters. Plan an
idempotent final merge after relevant registrations, followed by runtime-rule
regeneration. Do not rebuild rules every pickup or globally suppress other models.
[ItemAPI source](https://raw.githubusercontent.com/risk-of-thunder/R2API/master/R2API.Items/ItemAPI.cs).

R2API's rule dictionary also checks that a display has an ItemDisplay component and
renderer metadata. We will validate those along with the attachment child; matching
the name alone does not prove the item fits.
[Rule dictionary source](https://raw.githubusercontent.com/risk-of-thunder/R2API/master/R2API.Items/ItemDisplayRuleDict.cs).

For unknown packs: preserve compatible explicit rules, quarantine invalid defaults,
and log one actionable missing-rule entry per item. Recommended fallback is to omit
an unsupported cosmetic display while retaining its gameplay, rather than scatter
unknown-sized objects at the torso. No claim of universal future-pack visual support.
Expose a small documented registration/data format for future pack adapters. Use
soft dependencies and delayed type access so an absent optional mod cannot prevent boot.

Stuart's chosen reference is demo time new. Preserve it unchanged. Start Hollow Saint
Dev minimal; later reproduce its relevant enabled package set in an isolated test
profile. Record package GUID/version, content identity and tested display status.
Inventory runtime catalogs to catch items registered inside packages whose names do
not advertise an item pack. New packs get explicit adapter/test rows.

### Display acceptance

Generate a coverage report: supported, intentionally invisible, no source display,
missing rule, invalid child, and needs visual fit. Every visible vanilla/DLC display
must be fitted or have an explicit unresolved exception before a release candidate.
Check items individually and in crowded loadouts during idle, run, sprint/glide,
jump, crouched dash poses, aimed casts and death. Exercise pickup/removal/scrapping,
equipment swap, corruption/transformation, extra equipment slots where applicable,
skin changes, stage transition and remote-player visibility. Test stacking according
to each item's own display behavior; do not instantiate one cosmetic per stack blindly.
Static screenshots alone cannot certify animation clearance.

## 6. Animation, materials and runtime performance

Import remaining 50 clips with masks and reference poses; define the game Animator
contract explicitly. Drive locomotion from native velocity/input, grounded and sprint
state. Use 2D walk/run blending, glide, air/land, turn/start/stop, masked skill gestures,
additive aim, and a low-priority full-meter accent. Discharge is an overlay that must
not cancel locomotion or clobber the active cast. Map native stun/freeze/death behavior.

Gameplay states own fire/recovery/cancel times; clip markers provide authored timing
data. Animation events only decorate gameplay. Preserve Arc Bolt's 0.5 s repeat and
f13 interrupt rather than waiting for its full recovery tail. Test 1x/2x/3x attack
speed, low frame rate, simultaneous inputs and interruptions without duplicate hits.
[Skill/state integration](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Skills/).

Fit game shaders and CharacterModel renderer metadata for cloak, shield/buff overlays,
outlines, skins and death. Replace temporary Standard-material approximations only
after an in-game visual comparison. Preserve the subtle baked sash first; optional
local secondary simulation comes later if blending visibly needs it, with pinned
roots, limited excursion, teleport/reset handling and no gameplay/network authority.

Profile the current 137 renderers with four survivors, item-heavy loadouts and VFX.
Consolidate compatible static pieces/materials in a derived export if needed, while
preserving mounts, animation and skin toggles. Set measured renderer/particle budgets;
avoid premature arbitrary mesh limits. Evaluate culling separately from authoritative
combat: offscreen animation must never suppress a hit or charge update.

## 7. Approved kit and custom code

| Mechanic | Preserve approved behavior | Integration work |
|---|---|---|
| Arc Bolt | Aimed first hit, automatic bounded chains, 0.5 s base interval, alternate hands | Native attack pipeline; finite target search, correct muzzle visuals, speed-scaled cadence |
| Conduit Spear | Moving right-hand throw, f7 release, 450%, 5 s cooldown, 6 s mark | Projectile/hit state, conductor buff and target ownership rules |
| Arc Step | Four directions, diagonals via blending, usable in air, 2 charges at 5 s | Collision-aware native motor state, serialize direction, preserve stock/cooldown modifiers |
| Open Circuit | 12 s cooldown, 8 s buff, pulses feed meter | Server-owned duration/pulse controller, replicated buff and bounded target search |
| Discharge | About 10 Arc Bolt hits to fill, fires on next enemy hit at full | Per-body charge state, one consume per trigger, overlay + HUD/halo feedback |

Damage uses RoR2 attack pathways so crit, armor, shields, on-hit/on-kill effects and
proc coefficients apply normally. Deduplicate chains by health-bearing entity, cap
targets/range, enforce team/line-of-sight rules and avoid revisiting a target. Reuse
vanilla orb/projectile machinery only after checking its actual behavior against this
contract. Tag skill-origin damage; item procs and Discharge's own hits never refill
Discharge. Define whether individual chain hits contribute and tune gains without
violating the approved approximate fill rate. Consume a full meter before dispatching
the Discharge hit so recursive callbacks cannot trigger it again.

Use native components/events and R2API extension points first. Custom hooks should be
narrow and body/provenance-filtered: damage notification for charge/marks, stats if
the buff needs them, HUD lifecycle, optional compatibility initialization. Prefer a
standard server damage event where available. IL patches are a last resort requiring
a documented missing API, version guard and focused regression test.

## 8. Networking and effects

Plan for all peers to install matching mod versions. Native entity-state authority
is not synonymous with server authority: follow the game's input/motor model, while
the server owns successful damage accounting, charge, marks and passive triggers.
Specify one execution path per attack; never fire independently on client and server.
Use existing attack/state serialization where appropriate. Any extra client request
must validate sender/body ownership, state/cooldown, direction and sequence on server.

Synchronize persistent meter/ready state and buff data, including initial snapshots;
send effect endpoints/trigger events once as needed. Do not stream every bone or
particle transform. Serialize dash direction and other nondeterministic state data.
If using SyncVars/RPCs in the external plugin, configure and verify the required UNet
weaving; choose explicit messages/manual serialization only with an equivalent
initial-state and lifecycle contract. Reset/restore behavior on death, respawn and
stage transitions must be explicit and tested.
[UNet guidance](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/C%23-Programming/Networking/UNet/),
[R2API networking](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/C%23-Programming/Networking/R2API.NetworkingAPI/).

Convert source VFX into bounded reusable effect prefabs: muzzle/impact, actual chain
endpoints, spear/trail/mark, heel jets, dash afterimages, halo/core meter, Circuit
pulses and Discharge. Register networked gameplay prefabs separately from local
cosmetic followers. Pool frequent visuals, clear them on interruption/death/despawn,
budget lights/afterimages/audio voices and add restrained flash/shake settings.

## 9. Milestones, gates and effort ranges

These are provisional hands-on work estimates, not scheduled completion promises.
Re-estimate after M1 proves the real game pipeline. Art corrections and human
multiplayer sessions can extend them; support for arbitrary item packs is open-ended.

| Milestone | Deliverable and pass condition | Estimated effort |
|---|---|---|
| M1: game foundation | Locked dependencies, tiny bundle loaded, selectable custom body; movement, damage, death, respawn, camera and equipment work in Hollow Saint Dev | 3–6 h |
| M2: model contract | Stable mounts, full animation contract, game shaders, ragdoll; representative items stay attached through motion | 4–8 h |
| M3: first combat slice | Arc Bolt + charge/Discharge work for host and client; misses/procs cannot falsely charge and a trigger cannot double-fire | 5–9 h |
| M4: remaining kit | Spear, four-way/air Step, Circuit, overlay/cancel rules, VFX/audio/UI; speed/cooldown scaling measured | 6–12 h |
| M5: display coverage | Catalog-derived vanilla/DLC audit, visual fit pass, reference-profile comparison, unsupported-mod report | 4–10 h |
| M6: compatibility/polish | Two-peer tests, item-heavy runs, transformations, clones, stages, performance and remaining visual defects | 6–12 h plus playtests |

Total initial planning range: **28–57 hours**, plus unresolved source-art repair and
additional mod-pack fitting. Order is M1 → M2 → M3 → M4 → M5 → M6, but place a small
representative display set during M2 and run multiplayer checks from M3 onward.
Do not postpone networking or all item displays until the last pass.

Test matrix includes: no-item baseline; attack/move-speed and cooldown extremes;
on-hit/on-kill chains; equipment; skill-replacement items; debuffs/stun/freeze;
Vengeance/Gummy clone AI; death/revive/stage cleanup; two Hollow Saints together;
host and remote ownership; selected DLC combinations; and the reference mod set.
Automated tests assert finite-chain/provenance/serialization/rule validity properties;
actual game sessions certify native behavior and multiplayer visibility.

## 10. Decisions and next action

No decision blocks M1. Already settled: appearance, core kit/cooldowns, passive
Discharge, air/directional Step, baked restrained sash as the working baseline,
local-only work, and demo time new as the compatibility reference.

Before M4, review the still-open design points: Arc Step invulnerability (existing
default is none), jump/landing jet accents, and whether Circuit pulses continue
during glide/dash (recommend yes while its buff is active). Exact proc coefficients,
chain contribution to charge, meter persistence and animation-overlay priority need
written defaults and playtest tuning. These are proposals, not silently approved rules.
Before pack-specific fitting, identify item packs beyond the current reference list.

Recommended next implementation task: **M1 only**, plus the mount-name specification
and a small item-fitting sample from M2. Deliver a real in-game foundation before
expanding the full kit. This research request does not start that implementation.

## 11. Approval and controller-first amendment

Stuart approved this plan and authorized implementation after the research pass.
He primarily uses a controller; keyboard/mouse remains fully supported. This approval
supersedes the planning-only wording above. Begin M1 plus the early mount specification.

- Use RoR2's native player input, InputBankTest, SkillLocator and movement pipeline.
  No hard-coded keyboard keys, joystick button numbers or separate custom input stack
  in the shipped survivor. Respect user remapping, aim sensitivity/inversion, dead zones,
  sprint preferences and input-device switching through the game's own systems.
- Preserve analog stick magnitude for movement; blend walk/run continuously rather
  than converting every nonzero input into full speed. Test circles, diagonals,
  partial tilt, rapid reversals and stick release without chatter or foot snapping.
- Aim follows the native camera/aim ray. Initial skills should retain compatible
  native aim-assist behavior; verify target acquisition on controller without adding
  unapproved auto-aim or changing the aimed-first-hit design.
- Arc Step uses camera-relative movement input at activation; proposed neutral-stick
  fallback is forward along planar aim/facing. Latch direction once per activation,
  retain analog diagonal intent, and test airborne activation and camera turning.
- Glide is the approved sprint presentation, driven by native sprint state rather
  than requiring Left Shift. Skills use the four native slots; equipment/interact/jump
  remain native actions. Holding primary must support intended repeat-fire cadence.
- HUD and loadout flows must remain usable with controller focus, rebinding and
  device-appropriate native prompts. No mouse-only required custom menu or button.
- M1 acceptance explicitly includes gamepad movement, camera, sprint, jump, interact,
  equipment and selection, followed by keyboard/mouse parity. M3/M4 cover skill holds,
  simultaneous movement/aim/cast, releases, interruptions and host/client ownership.
- A physical-controller playtest is required before claiming controller feel verified.
  Automated input/state checks cannot establish dead-zone feel or comfortable aiming.
  The current Unity preview's keyboard controls are an art harness, not the shipped
  input implementation or proof of controller support.
