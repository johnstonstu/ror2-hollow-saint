# Hollow Saint A: four-slot ability workshop

Status: recommended kit proposal, not approved mechanics or implemented code.
User-confirmed foundation: aimed primary with automatic chain lightning; hits
build charge for stronger chains; Hollow Saint option A appearance.

## Recommended kit

| Slot | Working name | Purpose |
| --- | --- | --- |
| Primary | Arc Bolt | Aim the first hit; chain through eligible nearby enemies; build charge toward stronger chains. |
| Secondary | Conduit Spear | Focused energy strike that brands one target and helps direct subsequent chains. |
| Utility | Arc Step | Short directional dash that preserves stored charge. |
| Special | Open Circuit | Temporarily unfold the halo into a moving crown that periodically strikes and chains while the player continues moving and attacking. |

## Primary: Arc Bolt

![Arc Bolt](../art/concepts/hollow-saint-primary-arc-bolt-v1.png)

A repeatable two-finger electrical snap. Aim the initial hit; subsequent jumps
choose nearby eligible enemies. Damage and attack speed should retain value.
The halo displays accumulated charge through illuminated gaps.

Recommendation: reserve charge for empowering this attack. Automatic release
at maximum charge remains the simple initial proposal, but manual release is an
open choice. Charge per direct hit versus per chain hit also remains unapproved;
the numerical values in the early design brief are only starting hypotheses.
An isolated enemy must still allow charge generation.

## Secondary: Conduit Spear

![Conduit Spear](../art/concepts/hollow-saint-secondary-conduit-spear-v1.png)

Fire a short-lived energy lance, not a carried physical weapon. It deals focused
damage and applies a temporary broken-ring conductor mark. Recommended rule:
only one active marked enemy per survivor; a newly marked target replaces the old.
Chains prefer the marked enemy only when it meets ordinary range, line-of-sight,
team, living-target, and not-already-hit requirements.

For lone bosses, the strike already supplies focused damage. A small bonus to
direct primary hits on the marked target is an optional test, not a settled
requirement. Do not enable repeated hits on the same enemy within one chain.
Secondary uses its own cooldown and neither requires nor spends charge.

## Utility: Arc Step

![Arc Step](../art/concepts/hollow-saint-utility-arc-step-v1.png)

Short directional movement with a faint afterimage and thin angular ground trail.
Reform quickly and resume aiming. Preserve charge; movement should remain usable
independently of the primary's meter. Use collision-aware movement, not wall traversal.

Dash distance, stocks, cooldown, airborne behavior, vertical control, and any
invulnerability remain open. The illustration shows escaping a shot; it does
not establish invulnerability or a damaging decoy.

## Special: Open Circuit

![Open Circuit](../art/concepts/hollow-saint-special-open-circuit-v1.png)

The existing halo unfolds above the survivor into a wider crown that follows it
for a limited duration. It periodically strikes a nearby enemy and chains onward.
The survivor can continue normal movement and primary attacks. Each pulse must
work against an isolated target even when no secondary chain targets exist.

Recommendation: independent cooldown, no charge requirement or charge spending.
This replaces the briefly explored idea of spending the primary's stored charge
on the special. Crown pulses should not build primary charge or recursively
spawn more crown pulses. Prefer the secondary mark when eligible. Keep pulse
count, target count, proc behavior, and visuals bounded.

Visually, use the same halo pieces rather than spawning another permanent ring.
The generated crown depicts more than four segments; reconcile it to the selected
four-quarter construction proposal before modeling. Main artwork illustrates
multiple moments at once and is not an exact simultaneous-damage specification.

## Why this kit fits together

The primary provides the constant combat rhythm. The secondary directs damage
toward a priority threat. The utility creates space and better firing angles.
The special adds a temporary storm without taking away the player's aiming and
movement. That leaves each slot useful without making every action depend on
the same resource.

Potential risks to test: too much automatic targeting; visual noise with items;
special damage eclipsing the primary; bosses feeling weak without crowds;
charge increasing too quickly with attack speed and dense packs.

## Before implementation

Confirm the proposed secondary, movement, and special; settle charge release
behavior; reconcile the selected model sheets. No damage coefficients, cooldowns,
proc values, animation durations, or multiplayer behavior are validated yet.

All four new illustrations were created with the built-in image tool, using the
selected A figure and materials sheet as references. Exact prompts are saved in
`art/concepts/prompts/` with matching filenames. These are original fan-mod
concepts, not actual game screenshots. No 3D model or playable mod was made.
