# Local 1.3 early-game balance, player relay and media pass

Stuart requested an itemless first-boss balance check, nearby player Orb bounces,
a whole-kit audit, new gameplay videos and a complete 1.3 README update.
Keep this local: no public release, push or media upload.

Clarification offered: harmless relay versus empowering a hit, and whether
"arc blast" means Primary, Orb or Gaze. Until a reply, use a harmless owner relay
when one or two nearby enemies remain, retaining the existing finite enemy-hit
budget, repeat attenuation and zero self-damage. Audit all three attacks.

Success criteria defined before tests:

- Fresh enemies remain first. With one/two enemies and the living owner within
  8 m in full 3D feet proximity, the Orb can travel to the player and back to an eligible enemy. The actual core return still must fit the bounce range and world clearance. Player
  contacts cause zero damage/procs/fuel gain and do not spend the enemy-hit budget.
- No owner-only loops: range, terrain collision, owner death/body replacement,
  no eligible enemy and overall lifetime still end the cast. A third fresh enemy
  overrides any planned revisit. Remote effect payloads/IDs remain unchanged.
- Linked-production tests cover eligibility boundaries, finite solo/two-enemy
  relays, fresh ordering, repeat scaling, zero self-hit reporting and cancellation.
- Native baseline and candidate measurements record item counts, body damage,
  attack speed, crit, enemy HP/armor/level and actual hit/DPS/kill timing. Separate
  stationary boss measurements from normal moving/attacking combat acceptance.
- Investigate Primary cadence, charge generation, free Orb opportunity cost,
  spear/Gaze/Circuit/cloud output and overlap before changing numbers. Any default
  migration is appended, matches only exact prior defaults and preserves customs.
- The requested Astra high auditor checks the full kit, controls, presentation,
  descriptions and scaling, then reviews the final implementation/evidence.
- New videos show real native gameplay: free/empowered Orb, player relay,
  Thundercloud, charged Circuit with overhead Orb, Gaze and an itemless boss test.
  Keep raw captures/logs and publishable local media separate; videos are review
  artifacts, not evidence of ordinary-combat or multiplayer acceptance.
- README and bundled/localized player documentation consistently describe 1.3,
  current damage/cost/controls and new media; public URLs must not claim new local
  media has been uploaded. Preserve previous recordings and candidates.
- Doctor, all source/build/native-access checks, focused native tests, video
  frame/duration checks, backup staging and private package identity are recorded.

Sequence: inventory and source audit; instrument no-item native baseline; measure;
implement/test relay and justified tuning; native regression/video capture; review
media and documentation; final Astra audit; stage/package and report evidence.
