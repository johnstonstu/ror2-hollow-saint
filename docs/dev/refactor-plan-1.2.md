# Hollow Saint: refactor plan after 1.2.0

Prepared 2026-10-06. This is an implementation plan, not a record of completed runtime changes.

Implementation now uses `codex/refactor-120` from the release tag. See [results](refactor-120-results.md) for completed work and remaining native acceptance gates. The baseline/branch observations below describe the original review.

**Recommendation:** improve the development and verification workflow first, then extract focused responsibilities from the runtime. Preserve the released gameplay contract throughout. The project already has valuable small gameplay policies and regression checks; extend those strengths rather than replace the architecture wholesale.

## 1. Baseline and review evidence

This review targets **`v1.2.0`, commit `e44d582964bf7e69fb6e410e9eeaf2748b443197`**. The main working directory is still on `v09` at `2309d759`. The histories diverge: `HEAD...v1.2.0` contains 107 commits unique to the current branch and 51 unique to the release side. Starting implementation on the current checkout without resolving that distinction would refactor an obsolete baseline.

A source-only release snapshot and machine-readable findings are in `artifacts/refactor-review-120/`. It uses about 14 MB, including check builds. No new full Unity worktree was created.

| Measured item in the release tree | Baseline |
|---|---:|
| Tracked files | 1,249 |
| Runtime C# files | 176 |
| Runtime C# lines, including comments and blanks | 25,706 |
| Runtime files above 300 lines | 19 |
| Gaze C# files | 44 |
| DevAutopilot files / lines compiled by the main project | 16 / 3,026 |
| Standalone check projects | 20 |
| Distinct production files linked by those projects | 52 |

The linked-file count is an inventory, **not a coverage percentage**. Large assertion totals in loop-based tests are also not a measure of independent scenario coverage.

Executed against the release snapshot:

- All 20 standalone projects passed using `dotnet run`, with .NET SDK 10.0.302.
- `Check-Language.ps1` passed: 47 tokens and four language sections.
- `Check-Stormspear.ps1` and `Check-AimForgiveness.ps1` passed.
- Three existing CS0108 warnings appeared in `GazePresentationChecks` substitutes. First-use SDK PATH/workload notices did not prevent successful check execution.

The checks use pure policies and explicit Unity/RoR2 substitutes. This review did not build the complete runtime DLL, run Unity, launch the game, or validate native multiplayer, rendering, or audio. Those are separate gates below.

The release tree already contains only 137 tracked art files and no `art/anim/wip/` files. Do not carry the old branch's render history back into the release baseline.

## 2. Findings that determine the order of work

| Finding | Concrete evidence | Consequence |
|---|---|---|
| Development entry points disagree about the current project | `docs/kit-architecture.md` still describes Conduit Spear and a one-layer bundle; `docs/dev/README.md` leads into a pre-release Gaze trial; the old TODO still lists release work as pending | Agents and developers can follow valid historical instructions for the wrong version |
| Fast checks are useful but fragmented | 20 executable projects plus separate PowerShell checks; no release-tree CI workflow, SDK pin, solution, or project AGENTS.md | Check selection and environment setup depend on remembering prior chats |
| A shared preview preparation path is broken in the release tree | `Prepare-FxValidation.ps1:20` names `SpearAimPose.cs`, which is absent from the tag | Several Unity check scripts depend on preparation that cannot complete as written |
| Preview preparation depends on exact source text | It extracts methods from `KitShared.cs` and `FoundationSkin.cs` using `IndexOf` and rewrites presentation source | Innocent source organization can break verification or leave stale generated copies |
| Native API verification can be incomplete without saying so | `Check-Access.ps1` catches member-resolution errors and skips unresolved members | Missing reference assemblies can weaken a reported pass |
| Release checks differ from staging checks | `Make-Package.ps1` checks language and package structure; `Stage-Build.ps1` runs the native access scanner; neither invokes the complete standalone suite | A package can be created without running the project's best regression checks |
| Version edits have three destinations but the helper updates two | `Plugin.Version`, package manifest, and csproj `<Version>`; `tools/bump.py` updates the first two | Future metadata can drift despite the current tag being aligned |
| Large files combine separable responsibilities | `KitShared.cs` 752 lines; `KitFx.cs` 727; `FoundationPresentation.cs` 816; `SpearCarry.cs` 780 | Small changes require broad context and produce overlapping edits |
| Saved configuration is a compatibility system | `KitConfig.cs` contains binding, UI integration, live callbacks, legacy settings, and ordered default migrations | A tidy-looking removal or rename can reset player preferences or alter effective damage |
| Networking and lifecycle need clearer ownership | `GazeFuelTransport` installs static subscriptions without a corresponding uninstall method; warning suppression is one global flag; `StaticFx.Remove` has an empty catch | Cleanup and diagnostics deserve explicit contracts and targeted tests; this is not proof of an observed gameplay failure |
| Numbered Unity assets are not independent backups | Builder15 uses the GameFoundation14 character prefab, GameFoundation11 spear, and v31 source textures; Builder14 derives from GameFoundation13 | Deleting or moving earlier numbered folders can break the active build |

## 3. What a successful refactor must preserve

Record these as executable fixtures where possible, and as explicit native acceptance cases otherwise:

- Server ownership of damage, resource admission, spending, and healing. Presentation must not award resources or authorize damage.
- Gaze entry/reserve separation, spend-at-launch behavior, grouped hold/release behavior, cancellation/death behavior, bounded duration, and duplicate/stale-event rejection. Keep the legacy tap mode while it remains a supported option.
- Stored Prayer's full-bank claim, successful-launch snapshot, once-only landing resolution, and protection against owner/stage loss.
- Stormspear hand selection, input-device switching between throws, throw completion/refund rules, native stock/recharge handling, and primary overlap rules inside and outside Open Circuit.
- Circuit dwell's actual-hit admission, one zap per victim per crown, bounded bookkeeping, and reset conditions.
- Raw configuration values versus effective runtime coefficients. Apply inherited damage modifiers exactly once; preserve proc coefficients and config migration order.
- Native skill overrides, foreign-mod override precedence, stock, cooldown timers, and restoration of valid per-skill instance data.
- Animation execution order, socket names, layer/state names, authored timing, release positions, and visual/audio cleanup.
- Plugin GUID, assembly identity, content names, language tokens, achievement/unlock identifiers, config keys, explicit Beat values, message IDs, serialization field order, and Unity GUIDs.

Moving a C# file should initially preserve its namespace and type name. Renaming a registered EntityState or serialized MonoBehaviour is a compatibility decision, not a folder cleanup.

## 4. Proposed organization

Keep the existing project roots and one shipping DLL. That limits disruption to release scripts, embedded resources, Unity metadata, and agent instructions. This is a destination map, not a request to move every file in one change.

```text
HollowSaintMod/
  Plugin.cs
  HollowSaint.csproj
  Content/                   # provider, registration, body construction, content registry
  Character/
    Animation/               # animation bridge, locomotion/presentation, pose passes
    Appearance/              # materials, skins, mastery visuals
    Rig/                     # sockets, mounts, mesh and ragdoll construction
  FoundationKit/
    Configuration/           # binding, per-feature settings, migrations, options adapter
    Shared/                  # narrow shared damage, targeting and resource contracts
    ArcBolt/
    ArcStep/
    Storm/                   # Static, stored Prayer, meter, strike lifecycle
    Stormspear/               # include spear carry/pose files; preserve old namespaces first
    OpenCircuit/
    Gaze/
      Rules/                 # ledger, schedule, input, duration and recovery policies
      Runtime/               # EntityStates, controller, skill overrides
      Networking/            # existing packets, registration and transport
      Presentation/          # HUD, audio, beam/crown visuals
    Vfx/                     # genuinely shared visual primitives and beat routing
  Audio/                     # shared bank and sound routing
  Localization/
  Diagnostics/               # bounded logging and content/native audits
  Development/               # opt-in autopilot and capture scenarios
  Icons/
  Language/
  Package/
tools/
  verify.ps1                 # proposed verification entry point
  doctor.ps1                 # proposed environment/input diagnosis
  checks.json                # proposed check inventory and feature selection
  *Checks/                   # preserve existing test project locations initially
  tests/                     # pure legacy and native Unity checks, classified explicitly
  dev-profile/
  release/
  blender/
  audio/
HollowSaintUnityProject/      # keep paths and .meta identities until dependency audit
docs/
  dev/README.md              # current developer entry point
  architecture/              # current runtime, networking and asset contracts
  decisions/                 # short durable decisions and reasons
  history/                   # dated proposals and superseded acceptance records
art/                         # editable, required source assets
artifacts/                   # ignored generated checks, captures, builds and backups
AGENTS.md                    # concise project workflow and routing instructions
```

Do not create a new general-purpose framework, dependency-injection container, or assembly per skill. Pure rules can remain in the shipping project and be linked by tests while this remains practical. Keep collaborators concrete unless a real runtime boundary or test seam warrants an interface.

## 5. Implementation sequence

Each numbered stage can contain several small commits. Finish its acceptance gate before moving into the next riskier stage. File moves, formatting, logic extraction, and actual bug fixes should be separate review units.

### Stage 0 — Establish the release baseline

Create the implementation branch from the exact 1.2.0 tag in a suitable checkout after preserving that checkout's local work. Reuse an appropriate checkout where ownership allows; avoid recreating the large historical art worktrees. Do not merge the old `v09` history into the release simply to make this directory current.

Record source commit, package metadata, current release ZIP, bundle and sound-bank hashes, SDK/Unity versions, and required local inputs. Keep the released package immutable and give candidate outputs their own location. Capture the default configuration and a small set of prior-version/customized configuration fixtures.

**Gate:** the implementation source is demonstrably the release baseline; the original package and required asset inputs remain available; existing uncommitted work has not been folded into the refactor accidentally.

### Stage 1 — Make the project understandable to an agent or new developer

Update `docs/dev/README.md` into a short start page: current baseline, main source map, quick checks, build/stage commands, required inputs, and where acceptance evidence lives. Write current architecture and networking notes from the release code. Mark older design/playtest notes with their dates and status; preserve them as history instead of converting historical claims into current facts.

Add a compact root `AGENTS.md` and scoped instructions only where they prevent repeated mistakes:

- Runtime: authority boundaries, compatibility identifiers, config migration rules, and per-feature check routing.
- Tools: commands, exit-code requirements, output locations, and environment configuration.
- Unity/assets: .meta preservation, active dependency graph, selected builder and bundle pin.

Include a task recipe: identify the baseline; name the intended behavior; locate the feature and its consumers; select checks; make a bounded change; report evidence and remaining native acceptance. Future delegated tasks, if requested, should have explicit file ownership and avoid shared registration/config files until integration.

**Gate:** a fresh agent can find the correct implementation files and verification command for a Gaze, spear, localization, or asset change without reading old chat logs. Historical notes are clearly distinguishable from current instructions. This is a navigation exercise, not a new automated test suite.

### Stage 2 — Unify and repair verification

Introduce `tools/verify.ps1` and a check inventory. Keep existing executable check projects working rather than spending the first refactor converting them to a new framework.

Suggested command contract:

```powershell
# Proposed commands; not implemented by this planning pass.
.\tools\doctor.ps1
.\tools\verify.ps1 -Scope Quick
.\tools\verify.ps1 -Scope Quick -Feature Gaze
.\tools\verify.ps1 -Scope Build
.\tools\verify.ps1 -Scope Native
.\tools\verify.ps1 -Scope Package
```

`Quick` runs pure/substitute checks and language validation. `Build` compiles the runtime from known inputs. `Native` validates against the installed game assemblies and runs explicitly selected Unity checks. `Package` builds and verifies a local candidate package after its required gates. Verification must not implicitly stage, launch the game, or publish.

The runner should report pass, fail, unavailable, and deliberately skipped checks separately; propagate child exit codes; reject an empty or incomplete required suite; and save a compact JSON summary with durations and source revision. A feature selection is a convenience for iteration, while a release candidate still requires the full relevant suite.

Repair preview preparation before relying on it: remove or replace the missing SpearAimPose dependency based on current production behavior, validate the complete source list before writing outputs, and eliminate method extraction by textual boundaries. Use complete shared source units or narrow maintained adapters. Generated RuntimeCopies need a source manifest/hash so an older successful report cannot validate newer code.

Make unresolved native references explicit failures or inconclusive results for required assemblies, with a documented allowlist for intentionally optional ones. Keep the native public-member scan: publicized reference assemblies make it a valuable gate.

Add a Windows CI job for the environment-independent checks. Select and pin a supported SDK compatible with the existing net10.0 check projects; do not upgrade the game's netstandard2.1 target as a side effect. Share build settings where useful and use locked restore for the runtime's existing lockfile. CI must report native-only checks as unavailable when licensed/local inputs are missing.

Route staging and packaging through the same required verification definitions. Preserve package allowlisting, forward-slash ZIP paths, localization checks, local-path scanning, and bundle hash validation. Consolidate version metadata or validate/update all three current locations atomically.

**Gate:** one command executes all current fast checks, a failing child makes the command fail, missing prerequisites cannot look like a complete pass, and preview preparation contains no reference to nonexistent production files. The packaging route and the staging route agree on their required checks.

### Stage 3 — Separate configuration and compatibility responsibilities

Split config binding by feature while keeping exact section/key names and default values. Extract ordered default migrations and optional Risk of Options integration from `KitConfig`. Centralize environment paths in a local ignored configuration plus checked-in examples/validation; retain the dev profile as an explicit target.

Test fresh configuration, every supported migration version, customized values that must survive, exact-match floating-point migrations, legacy keys, and raw-versus-effective damage. Preserve the distinction between settings applied immediately and those sampled at spawn, cast entry, projectile creation, or restart. Do not introduce new mid-cast config behavior during extraction.

Move compatibility-only fields into a clearly marked section when helpful, but do not silently remove serialized settings such as the retained off-hand rate or old automatic-discharge controls.

**Gate:** the same input configuration yields the same effective values and persisted migration result before and after extraction. No new defaults or balance changes ride along with this stage.

### Stage 4 — Extract the shared runtime files

Start with changes that have clear boundaries:

| Current hotspot | Proposed responsibilities | Required evidence |
|---|---|---|
| `KitShared.cs` | Existing `KitTuning`, `HsDamageSource`, `KitContent`, `DischargeMeter`, `KitAnim`, `KitUtil`, `KitLog`, and `KitTokens` become focused source units; then narrow utilities only where warranted | Content ordering/identity, meter conservation, animation contracts, linked check paths |
| `KitFx.cs` | Keep Beat values; separate sound selection/throttling, effect transport, effect playback, and visual dispatch | Same serialized effect fields, event counts, sound ownership and delayed playback behavior |
| `DevAutopilot*` and diagnostic files | Move under Development/Diagnostics; identify scenarios and required evidence | Existing opt-in activation and capture behavior remain available; normal gameplay stays unaffected |

Use physical moves with unchanged namespaces first. Update test links, preview generators, source-scanning assertions, documentation links, embedded-resource paths, and script references together. Replace fragile `Contains`/`IndexOf` gameplay assertions with executable integration seams as those seams are introduced; keep intentional architecture checks where they express a real boundary.

Do not make dev tooling a second assembly immediately. First remove unnecessary runtime-to-autopilot coupling through a small diagnostics context. A separate development build can be considered later if it materially simplifies the shipping artifact and both variants have reliable build checks.

**Gate:** content identity and ordering remain stable; all existing checks still exercise production code; pure file moves have no behavioral diff; extracted units have a clear reason to change.

### Stage 5 — Make ownership and failure behavior explicit

Document which owner releases each hook, network handler, material, mesh, audio loop, pending request queue, and per-body cache. Start with Gaze transport and shared presentation resources rather than creating a universal lifecycle framework.

Add focused scenarios for repeated registration, partial registration failure, handler collision with another mod, run/stage changes, body replacement, disable, and destroy. Preserve another mod's registrations. Determine which services intentionally live for the process and which must reset per run/body; do not add broad static resets without that distinction.

Replace silent cleanup catches with contextual, bounded diagnostics. Preserve existing host callback containment: optional cosmetic failure should not prevent content registration or stop the game initialization routine. Log and propagate internal errors to an owning boundary; do not indiscriminately rethrow from game callbacks. Record this project-specific exception policy in runtime guidance.

Improve diagnostic suppression by failure category rather than one global flag where it currently hides unrelated failures. Include enough context to distinguish owner/cast/operation while avoiding per-frame log spam.

**Gate:** repeated lifecycle scenarios leave no extra owned handlers, audio loops, pending events, or live visual objects; failure of optional presentation cannot authorize gameplay or prevent required kit content from being registered.

### Stage 6 — Refactor the two riskiest presentation controllers

`FoundationPresentation` should retain a thin MonoBehaviour adapter for native observations and animation application. Extract a deterministic phase decision model and separate state mapping, locomotion sampling, and audio cues. Preserve world-space smoothing, airborne debounce, transition timing, and late-update order.

`SpearCarry` should separate hand/input selection, socket resolution, pose trajectory/IK math, and visual lifetime while retaining one component that owns update order. Do not distribute these into many independently scheduled MonoBehaviours: its explicit order relative to arm poses, spear ghosts, current, and VFX is part of the behavior.

Gaze already has ledger, sequence, schedule, and policy separations. Organize its 44 files into rule/runtime/network/presentation groups and clarify controller ownership. Avoid a full Gaze state-machine rewrite or simply adding more partial files to satisfy line limits.

**Gate:** recorded input/timing traces preserve state transitions and output poses within explicit tolerances; resource/input checks remain green; native acceptance verifies hand alignment, transition continuity, death/respawn cleanup, and host/client behavior before these changes are called complete.

### Stage 7 — Make asset iteration reproducible

Create a machine-readable asset/build manifest describing the approved bundle hash, sound-bank hash, active prefab/controller roots, Unity version, source textures, clip sources, and reproduction commands. The first manifest describes the existing build; it does not require a new bundle.

Use Unity's dependency graph to record the complete transitive asset closure, including .meta GUIDs. Builder15's direct references to GameFoundation14, GameFoundation11, and v31 textures demonstrate why numeric age is not a deletion criterion. Audit which historical generators still need sources absent from the release tree; either document restoration from retained history or retire the generator explicitly.

Only after the active closure and rebuild path are verified should historical builders/assets move into an archive or leave the active checkout. Preserve GUIDs during Unity moves. Add explicit build output selection instead of creating another permanently retained numbered snapshot for each iteration.

Keep new render sequences and captures under ignored artifacts. Retain editable sources, concise verification reports, chosen review media, and immutable release inputs. Any later retention cleanup must protect inputs referenced by the manifest and offer a dry run.

**Gate:** a prepared environment can reproduce an acceptable candidate from documented inputs, with asset names/controller contracts checked. Rebuilt Unity bundles may not be byte-identical; compare content and functional contracts as well as provenance, and keep the approved shipped bundle pin unchanged until a replacement is accepted.

## 6. Rename and move checklist

Before each rename, enumerate the identifier and path across source, config, docs, comments, strings, filenames, directory names, build outputs, tests, and generators. The first move inventory must include:

- csproj linked sources in all 20 check projects;
- `Prepare-FxValidation.ps1` copy/extraction paths;
- source-path assertions in LandingRecoveryChecks and NonGazeDamageChecks;
- PowerShell/Blender build and validation paths;
- registrations, reflection strings, serialized class names and packet contracts;
- Unity .meta GUIDs and prefab/controller references;
- embedded icon, language and sound-bank resource names;
- packaging/staging output paths and developer documentation links.

Keep this inventory attached to the specific move. File movement, identifier renaming, and implementation changes should be independently reviewable.

## 7. Acceptance and useful metrics

| Dimension | Measure and acceptance |
|---|---|
| Functionality | Existing 20 projects plus language/legacy checks remain green; named gameplay invariants and native scenarios are preserved |
| Iteration speed | Record cold and warm verification times on this machine; target a warm Quick run within 60 seconds if practical; feature checks must remain available without Unity |
| Agent usability | One current start page; one root instruction file; every feature has an implementation location, relevant checks, and required native acceptance listed |
| Modifiability | A change to a feature rule should normally touch that feature, its checks, and necessary descriptions/config; shared edits need a stated reason |
| Readability | Prefer logic files under about 300 lines and methods under about 50; use responsibility boundaries, not line splitting. Format touched code; avoid a repository-wide formatting diff |
| Build reliability | SDK and dependencies are explicit; missing required inputs fail clearly; source revision and asset hashes accompany candidate outputs |
| Compatibility | No accidental changes to tokens, config keys, enum values, packet order, registered type identities, or Unity GUIDs |
| Diagnostics | No silent failure in touched code; one category's warning does not suppress all other failures; no per-frame spam |
| Performance | Measure native allocations, frame time, object counts, physics queries and packet counts under the same workload; then optimize demonstrated hotspots |
| Storage | Fast code checks require no full Unity clone; routine checks create no tracked renders or new permanent asset snapshots |

Native acceptance should cover: keyboard/mouse and controller; host plus remote owner and observer; empty/full/custom-capacity banks; held inputs at entry; interruptions; death/respawn; stage transitions; repeated casts; foreign skill overrides; optional asset/audio failures; and representative item/proc load. Record which cases ran and which remain pending. Synthetic tests cannot certify native behavior.

Do not choose arbitrary FPS gains or a broad test-coverage percentage before profiling. For changed hot paths, compare warm-state allocations and bounded object counts against the release under matched scenes and input traces. Pool or cache only after identifying allocation frequency and ownership.

## 8. Recommended first implementation tranche

1. Establish the exact release baseline and current development/agent guide.
2. Add the unified fast-check runner, environment diagnosis, and independent CI checks.
3. Repair the preview source inventory and native-access gate, then align staging/package validation and version checks.
4. Begin the configuration compatibility fixtures and the mechanical `KitShared` extraction as separate changes.

Stop this tranche at a usable checkpoint before attempting pose/controller extraction. It should make the next gameplay change easier to locate, verify, and review even if the later refactor stages never happen.

Keep a known-good parent for every extraction. If a native regression appears, revert the smallest affected runtime change while retaining independent documentation/tooling improvements. Review and acceptance of this refactor do not authorize a public release; publication still follows Stuart's explicit release approval.
