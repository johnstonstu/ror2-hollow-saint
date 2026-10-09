# Next secondary: two-handed bouncing lightning ball

Status: Stu's concept, recorded 2026-10-06. Working name: **Hollowed Orb**
(tentative). Actively workshopped alongside the
[thundercloud Special](next-special-strike-call.md), which remains first for
runtime implementation. Both now have local runtime prototypes under the
[1.3 balance plan](charge-build-1.3-plan.md). Final balance remains a playtest decision.

## What Stu asked for

- A second Secondary ability that takes the spear's place when selected.
- Bring both hands together to grow and charge an electrical ball between them.
- Throw or blast the ball forward with both hands.
- The ball visibly bounces between enemies, ping-ponging around the group.
- It should feel substantial: an orb roughly one third to one half the size of
  a player character. Exact dimensions and camera appearance need a prototype.
- **Growth confirmed:** the orb grows in size with charges. Its charge
  source is the stored Static Charge bank. Hold Secondary to gather those
  charges one at a time, like the Special; release launches the gathered orb.
- **Admission confirmed:** requires at least one stored Static Charge to use.
  It does not have an empty-bank fallback cast.
- Stu authorized charge-dependent damage and finite bounce counts for a local
  balance prototype. Defaults are 3/5/7 total hits for 1/3/5 charges, counting
  the first impact. Exact values remain tunable.
- **Revisits confirmed:** prefer A-B-C when a fresh enemy is reachable; otherwise
  allow A-B-A-B, bounded by the remaining hit budget. Never immediate A-A.

Stu explicitly revised the design sequencing: workshop both skills together
now, while implementing the new Special first. Both are intended for this
build. No current instruction changes Stormspear's default selection or deletes
its content.

## Presentation direction

The key read is a physical ball of electricity: it gathers between both hands,
launches with a two-handed motion, and travels visibly between enemy impacts.
Visible flight and changes of direction should sell the ping-pong motion.
Radius, brightness, sound and impact reactions need to preserve that large-ball
silhouette without obscuring the enemies. These are prototype goals.

## Current workshop questions

1. How do gathered charges affect damage and bounce count in addition to the
   confirmed size growth?
2. Does the ball grow or get stronger after each bounce, or retain its launch
   size/power?
3. Revisit behavior is settled; tune per-victim repeat attenuation and judge
   one-boss damage during playtests. The prototype hits a lone boss once.
4. Prototype bounces require a clear path; terrain or no eligible next enemy
   ends flight. Test actual Unity collisions and moving targets.
5. Charge limits, speed, reach, bounce spacing, damage/proc budget, cooldown,
   interruption behavior and final skill name.

## Shared charge interaction (proposal)

Both skills require a nonempty stored-charge bank and gather one charge at a
time while their native skill input is held. The cloud and orb should make
visibly different uses of the same power: the
cloud expands over an area, while the orb grows between the hands and travels
between enemies. A proposed resource contract lets the player release early
to allocate part of the stored bank to one cast, retain the rest, and use newly
earned charges for the next cast. Each committed cast snapshots its size and
power; an airborne orb cannot spend the same charges already committed to a
cloud. Admission and intake are confirmed; the exact commitment, cancellation,
refund and simultaneous-input rules are implemented as the local prototype
contract described in the balance plan, pending manual acceptance.

The tactical intent of this proposal is that early release can leave part of
the bank for the other skill, whereas putting the full bank into one skill
requires earning more charges before the other can be used.

Before implementation, define success criteria for charge/release, finite
bounce budgets, target deduplication/revisit policy, target death in flight,
server-owned damage and owner/stage loss. Two-handed animation and apparent
ball size require native visual acceptance. Follow the existing development
verification and separate staging/playtest boundaries.
