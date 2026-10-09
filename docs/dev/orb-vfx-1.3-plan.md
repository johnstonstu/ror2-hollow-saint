# Hollowed Orb electrical presentation pass

Scope: presentation only, on the accepted local 1.3 candidate. Keep damage,
charges, aim, travel, hit budgets, controls, animation timing and network payloads.

Success criteria, defined before verification:

- Both gathered and travelling Orbs have visible, jagged lightning around the
  spherical surface, with short outward forks rather than hidden interior chords.
- Surface paths must also read across the front of the shell. Native comparisons
  revealed fading near the opaque sphere; use owned Orb material variants to
  harden particle depth fading while retaining normal depth testing.
- The smallest free Orb and fully empowered Orb remain readable at the gameplay
  camera; lightning follows the ball and respects each skin palette.
- Confirmed impacts have a short radial lightning burst, sparks, expanding rings
  and layered electrical audio. Cleanup-only impact messages remain silent.
- Surface effects reuse a fixed set of renderers; flight/impact effects have
  bounded lifetimes. Audio uses the existing throttled, fallback-capable mapping.
- Native captures show gathering, flight and impacts; native damage reports keep
  the existing free/one/five-charge hit budgets. Audio has no failed posts/clipping.
- Required source/build/native-access verification passes. Native screenshots and
  loopback capture prove local presentation only, not multiplayer or subjective
  acceptance during normal combat.

Sequence: implement shared surface crackle and impact/flight presentation; build
and run repository verification; stage the local Dev profile; run a focused native
capture, inspect images/audio, refine if needed; record candidate identity/results.
