# Lightning survivor design brief

Status: initial proposal, 2026-09-26. Only the two core combat choices below are confirmed.

The user subsequently selected Hollow Saint A / Cracked Icon. See
`ability-kit-workshop.md` for the latest four-slot proposal and `../HANDOFF.md`
for current project state. The numbers and acceptance criteria below remain
untested hypotheses, not approved balance. Supporting abilities have since
been expanded into visual proposals, but are not confirmed mechanics.

## Confirmed by the user

1. The primary fires aimed bolts with automatic chaining after the first hit.
2. Hitting enemies builds charge that strengthens chains.

## Combat identity

A mid-range survivor who turns clustered enemies into opportunities to charge
up a powerful lightning discharge. Aiming the first hit matters. Chaining gives
immediate feedback and crowd coverage. Ordinary damage and attack-speed growth
remain useful throughout a run.

The proposed charge loop is: land hits, fill charge, fire an empowered bolt,
repeat. These rules and numbers are starting hypotheses for playtesting.

## Primary: Arc Bolt (working skill name)

| Parameter | First prototype value |
| --- | --- |
| Base interval | 0.5 seconds, modified by attack speed |
| Initial targeting | Aimed hit up to 45 metres, obstructed by terrain |
| Initial damage | 180% of the survivor's damage stat |
| Normal chain | Up to 3 additional distinct enemies |
| Jump range | 15 metres from the previous target |
| Chain damage | 80% of survivor damage per secondary target; no compounding falloff |
| Charge | +10 per successful direct hit, +3 per successful secondary hit |
| Charge maximum | 100 |
| Empowered shot | 270% initial damage, 120% per chain hit, up to 6 additional targets |
| Initial proc coefficients | 1.0 direct, 0.2 secondary, subject to item testing |

- One health-bearing entity can be hit at most once by each cast, even if it has multiple hurtboxes.
- Jump to the nearest eligible, living enemy with line of sight from the previous target.
- Do not target allies, neutral interactables, or already-hit enemies.
- Misses generate no charge. Item-triggered damage generates no charge for this mechanic.
- Crossing 100 charge arms the NEXT cast. It does not transform a cast already in progress.
- Starting an empowered cast consumes charge, including on a miss; that cast generates no charge.
- Cap charge at 100 and show a distinct full-charge cue. No passive decay in the first prototype.
- An isolated boss still grants direct-hit charge; the empowered direct hit helps single-target damage.
- Keep the skill's chaining independent of owning Ukulele; test item interactions separately.

At base speed, this proposal reaches full charge in ten direct hits against an
isolated enemy, or six casts when each cast hits four enemies. These are simple
design calculations, not measured game results.

## Later skill ideas, not commitments

- Secondary: mark a priority enemy so chain routing can be directed deliberately.
- Utility: a short lightning dash for repositioning.
- Special: a temporary overcharge window that increases chain reach and intensity.

Build the primary first. Select the remaining skills after testing its pacing,
boss damage, and ability to handle groups.

## Proposed milestone 1: playable combat prototype

Use a temporary visual model loaded from the installed game or another permitted
placeholder. Keep art replacement independent of the combat implementation.

1. Create a C# mod foundation compatible with the installed game and required mod dependencies.
2. Register a separate survivor and primary skill without changing the original survivor globally.
3. Implement authoritative target selection, finite chaining, damage, and charge accounting.
4. Add visible chain effects and a charge indicator with an unmistakable empowered state.
5. Package a local build for a dedicated development mod profile.
6. Run the acceptance checks below and record observed results.

## Acceptance criteria, defined before implementation

- With four enemies inside jump range and line of sight, a normal cast hits the aimed enemy plus three distinct enemies exactly once each.
- With more than seven eligible enemies, an empowered cast still hits at most seven total.
- An enemy outside range or behind an obstruction is excluded; killing a target during travel does not create a duplicate hit or an invalid-target error.
- An isolated direct hit grants exactly 10 charge; a four-target normal cast grants 19; a miss and item-only damage grant zero.
- At 100 charge, exactly one subsequent cast is empowered and consumes the meter. Its own hits cannot regenerate charge.
- Increasing the damage stat changes damage proportionally. Doubling attack speed approximately halves the firing interval, allowing for simulation timing.
- Charge display agrees with the authoritative state, including after death and a new stage. Proposed policy: reset on death, preserve through a living stage transition.
- Item interaction checks with Ukulele and other on-hit items show bounded chains and no recursive self-triggering of this skill.
- The original survivor remains unchanged when selected separately.
- Record a short in-game playtest of crowds and an isolated boss; log failures with reproducible conditions.

Multiplayer readiness is a separate acceptance gate: compare host and client
damage, visuals, and charge in a two-player session before describing the mod
as multiplayer tested. Build with server-authoritative damage from the outset.

## Deferred decisions

Final name, silhouette refinements, backstory, animation style, remaining abilities, icons,
audio, alternate skins, unlock conditions, and public release balance.
