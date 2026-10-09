# Next special: charge-up strike call (design notes, 2026-10-06)

Status: local runtime prototype built toward 1.3.0 after Stu authorized implementation and balance work on 2026-10-06. **Thundercloud** is the working name. See the [current balance plan](charge-build-1.3-plan.md); earlier proposals below are design history, not additional runtime behavior.
It is **its own special**, a third option alongside Gaze of the Hollow and Open Circuit.
Target: 1.3.0, with a possible 1.2.5 milestone for one ability. No version bump or release is part of this local prototype.

## Implemented prototype contract

Hold native Special to gather a frozen entry allowance, at most five charges by default. Release spends only gathered charges after server validation. A 16 m starting radius grows to 30 m at five charges, with 80 m aim reach. Each eligible enemy is hit once in a near-to-far sweep; there is no leftover-charge clap. The cloud rises for 0.65 s, starts striking at 0.8 s, rolls across targets for up to 0.9 s and fades over 0.6 s. Empty-area releases preserve the bank and return stock. Utility cancels gathering before it takes movement ownership. The short up/back gather hop and actual crown translation are prototype presentation requiring native acceptance.

Damage, range, size, cooldown and capacity are configurable test values. Final balance and the final name remain open. Coverage is around the aimed position, so enemies outside the camera may be eligible when inside the previewed radius and visible from the cloud. See the [acceptance matrix](../manual-charge-build-1.3.md) for the broad-field-of-view test.

## What Stu asked for
- Same hold-to-charge start as the 1.2 Gaze: hold Special and the stored Static Charges spiral into the crown one at a time.
- **New release sequence:** the charged crown rises into the air and creates a large thundercloud. Lightning reaches down from that visible cloud to attack enemies around it.
- **Admission:** at least one stored Static Charge is required to use the skill. An empty bank must not activate or spend skill stock.
- **Movement idea:** a short upward-and-backward hop into a hover could give the player a view of the strikes and time to focus on targets. Stu likes the possibility; distance, timing and control behavior are still proposals.
- The earlier one-charge/one-strike sketch is a candidate, not a settled damage or target-budget rule. Stu wants to workshop damage and extra charges.
- **Placement/coverage confirmed:** the player aims a cloud position; it attacks enemies around that point, including outside the player's forward view. The earlier view-cone target restriction is superseded.
- **Coverage emphasis:** Stu wants a pretty large area that can strike the enemies he is looking at across his field of view. A small crosshair-centered patch would miss the intended feel. Keep the aimed-position concept, but workshop a broad reach and footprint that encompass the visible group. This clarification emphasizes forward battlefield coverage; whether off-screen enemies are also eligible remains a targeting detail to settle.
- **Growth confirmed:** the cloud starts fairly big at the minimum usable charge count, with decent aim reach, and grows as more charges are gathered. Growth must increase the actual attack footprint as well as the visible cloud. Exact starting size, growth curve and aim range remain tunable workshop values.
- **Lifetime confirmed:** one rolling sequence of strikes, then the cloud fades. No repeated storm ticks.
- **Order:** the strikes fan out, starting with the closest enemies and working outward to the distant ones.
- **Sound:** rolling thunder as the strikes walk outward.
- **Extra charges** (more charges than targets): not decided. A combined final clap was the earlier lean; it is not approved as the final behavior.
- Bosses and stronger targets need thought; how hard a full five-bolt cast should hit a single boss is to be tuned in play.
- Prior playtest feedback favors charge strength conveyed by crown animation and VFX rather than a word-heavy charge HUD.

## Proposed cast flow

1. Check native skill stock and a nonempty stored-charge bank.
2. Hold Special to gather charges into the crown; show the gathered power through crown growth, light and audio.
3. Prototype a small hop upward and backward relative to the initial planar aim, then a brief hover with free camera aim. The hop is proposed for the gather phase so the player can choose a target before release; its exact timing remains open.
4. Release sends the charged crown upward. It expands into a layered thundercloud over the chosen combat area; even one charge produces a large cloud, and additional gathered charges expand its coverage.
5. Lightning visibly connects the cloud to enemies around the aimed position in one near-to-far rolling sequence.
6. The cloud dissipates, the crown returns, and normal movement resumes. Exact sequence duration and cooldown remain tunable proposals.

Charge-driven area growth is settled; damage scaling and target count remain open. A proposal is one strike per eligible enemy in the area, with gathered charges also increasing each strike's strength and the cloud's visual intensity. This would supersede one-charge/one-target allocation and remove the need for a separate leftover-charge clap. Damage scaling and this allocation rule are proposals, not approved behavior.

Aim reach and area coverage are separate tuning controls: reaching a distant group must not imply a tiny strike patch, and widening the cloud alone must not leave distant visible enemies unreachable. Use near, midrange and distant groups spread across the camera view in native acceptance. Exact distances and field-of-view treatment are not approved numbers yet.

## What already exists to build on
- Charge phase: `HollowSaintMod/FoundationKit/Gaze/Runtime/GazeState.Charge.cs` (absorb timing 0.12 s then 0.30 s each, armor, hover, Utility backs out, count serialized to the server) and `HollowSaintMod/FoundationKit/Gaze/Presentation/GazeChargeUpFx.cs` (orbs spiral into the crown, `Play_HS_GazeLoad1`-`5` sounds). Reuse presentation where practical; keep the new skill's state/resource lifecycle separate initially to avoid changing released Gaze behavior.
- Strike presentation: `HollowSaintMod/FoundationKit/Storm/RoyalCapacitorFx.cs`, `Strike(position, owner)` spawns the palette-tinted Thunderbolt with the `Play_HS_ThunderStrike` sound. `ThunderboltDriver` owns damage, procs and splash for the passive's strikes.
- Targeting references: the Gaze lock-on bolt (`HollowSaintMod/FoundationKit/Gaze/Runtime/GazeFuelController.Under.cs`, BullseyeSearch with line of sight) and Arc Bolt's small aim-assist cone.
- Static Charges: `DischargeMeter` (bank of 5 by default, `KitTuning.StormChargeMax`).
- Movement: Gaze's `BeginHover`, `Hover` and `GazeFallGuard` demonstrate motor-driven lift, ceiling checks and gravity/fall cleanup. Arc Step also writes motor velocity; the new skill must settle movement ownership and Utility cancellation so the two states cannot fight over velocity.
- Cloud presentation: there is no complete thundercloud effect in the current kit. `VfxAssets`, skin palettes and lightning lines are reusable foundations. The existing Capacitor effect is an impact; it does not itself establish visible bolt origins inside a new cloud.
- Audio: the Wwise bank is authored by `tools/audio/author_bank.py` from WAVs in `art/audio/source` (mix trims in `MIX_TRIM_DB`). ThunderStrike is built from a Pixabay sample and kept out of git. A rolling-thunder layer would be a new cue (sample or synth).
- Test harness: `HollowSaintMod/Development/DevAutopilot*.cs` segments via `HS_SEGMENTS` (see `DevAutopilot.GazeCharge.cs` for a charge-up test pattern); `tools/dev-profile/Stage-Build.ps1` stages into the `Hollow Saint Dev` profile.

## Earlier strike-call sketch (provisional after cloud addition)
- **Historical targeting (superseded):** view-cone selection of up to N targets (N = charges, max 5). Current targeting instead covers enemies around an aimed cloud position. Range, radius, line-of-sight policy and charge-to-target allocation remain to be specified.
- **Timing:** strikes land one after another walking outward, about 0.12 to 0.18 s apart, with slightly longer gaps for farther targets so the thunder rolls. The rolling-thunder bed starts at the first strike and tails off after the last.
- **Extra charges:** candidate is a final clap on the highest-health target (or the crosshair point) that scales with the leftover charges, landing after the fan-out. Alternative: refund them. Decide in play.
- **Bosses and elites:** options are (a) the final clap naturally favors the boss, (b) a priority weight for bosses/elites in target selection, (c) a cap so five bolts plus a clap on one boss is strong but not a one-shot. References: the full-bank Thunderbolt is about 765%; the Gaze opening blast is 600% per charge.
- **Empty bank:** resolved: requires at least one stored charge; no unfunded fallback bolt.
- **Cooldown:** proposal starts when the last strike lands in the single sequence.
- **Multiplayer:** server picks targets and spawns strikes (networked effects); clients show the charge-up and markers.
- **Text:** new skill name/description tokens in all four languages in `HollowSaintMod/Language/HollowSaint.language`; keep character-select descriptions to about two lines (1.2 trimmed them for that reason).

## Open questions for Stu
1. Name.
2. Charge model: strengthen one strike per enemy in the area, or keep a finite bolt budget with an excess-charge rule.
3. Damage budget and proc behavior for a full cast on a single boss versus a crowd.
4. Starting aim range and broad area radius, growth per gathered charge, maximum coverage and line-of-sight policy.
5. Hop/hover timing and distance; whether Utility cancels and whether full charge waits for release.

## Implementation plan and acceptance gates

- Add a feature owner under `FoundationKit` with independent registration, rules, runtime and presentation. Append a third Special variant while preserving Gaze/Open Circuit identities, order and defaults.
- Use native skill readiness and input for the nonempty-bank requirement. Recheck resources on the server at commitment; resource ownership must account for newly earned charges, spear claims and interrupted casts.
- Freeze the authoritative cast budget and aim/cloud placement at the chosen commitment boundary. Server decisions authorize damage and spending; replicated presentation never authorizes either.
- Build cloud growth/ascent, internal flashes, descending bolts and dissipation as feature-local presentation. Optional visual/audio failure must preserve committed gameplay.
- Prototype movement through CharacterMotor with collision and ceiling handling, explicit Utility interaction, and balanced cleanup on cancellation, stun, death and stage/body loss.
- Keep damage, target coverage, excess-charge behavior and storm lifetime adjustable for local playtest work. Do not reuse the spear's funded multiplier as an implicit new-special balance decision.
- Register new source check suites in `tools/checks.json`; run Quick, Build and Native verification. No verification scope stages the profile or launches gameplay.

Behavioral success criteria to define before writing tests:

- Zero charges prevents activation and stock consumption; a charged, otherwise-ready skill appears as the third selectable Special.
- Gathering or cancelled presentation alone cannot spend resources. The chosen commit rule spends exactly the accepted budget once, with no stale/duplicate replay or borrowing of later gains.
- Target membership, order, damage budget and excess handling follow the agreed rules, including a lone boss, no valid target, target death and owner/stage loss.
- Movement ends cleanly on every exit path; native Utility cannot compete with hover movement or leave gravity/fall handling active.
- A remote client's cast produces the same server-owned cloud placement and strikes, with no client damage or optimistic spending.
- In-game visual acceptance: crown ascent clearly becomes a cloud; bolts originate visibly in that cloud; the hop keeps the combat area readable; effects fit ceilings and skin palettes.
- Coverage acceptance: enemies spread across the intended visible combat group fall inside the previewed storm footprint, at both near and distant aiming positions; the cloud's visual footprint communicates its actual strike area.
- Growth acceptance: minimum-charge coverage is already substantial; increasing gathered charges enlarges both the rendered cloud and eligible strike footprint. Configured capacities above the default five must have explicitly bounded scaling.

Source checks and installed-member scans do not prove rendering, controller feel, audio quality or multiplayer gameplay. Stuart launches and playtests; profile staging is a separate action.

## Related Secondary workshop

Stu's working name for the [two-handed bouncing lightning ball](next-secondary-lightning-ball.md) is **Hollowed Orb**. It also grows with charges. Stu now explicitly wants to workshop both abilities together, including their charge interaction, while retaining the Special as the first runtime implementation priority. Do not discard or postpone Orb design questions merely because its implementation comes second.

The current bank already has distinct Gaze reservation and full-bank spear-claim paths. Both new skills now require at least one stored charge and gather charges one at a time while held. Before adding either new consumer, agree how stored charges are reserved, committed and returned, and how casts interact. A proposed shared rule is that a charge can empower only one committed cast; gathering is refundable until launch, and each launched cloud/orb retains its own snapshot while newly earned charges fund later casts. Early release would leave part of the bank available for the other skill.
