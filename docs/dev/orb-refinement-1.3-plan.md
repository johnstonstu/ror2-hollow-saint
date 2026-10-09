# Hollowed Orb refinement after Stuart's first playtest

Stuart selected short casts that preserve Static, with longer holds gathering
stored charges. Implement locally against the completed 1.3 candidate. Preserve
the original package and acceptance record as historical evidence.

Success criteria, before implementation/tests:

- Native Orb admission works at zero Static; Thundercloud still needs one.
- Release before 0.50 seconds spends no Static, including with a full bank.
  The first empowering charge loads at 0.50 seconds, then every 0.30 seconds.
- A zero-charge Orb is substantial (0.6 m), deals 157.5% base damage and has two
  total hits. Existing one/three/five-charge values remain 225/360/495% and
  three/five/seven hits. Cooldown remains seven seconds initially.
- Authentication, exclusive bank lease, server timing, duplicate protection,
  Utility cancellation and stock accounting apply equally to free casts.
- A tap has a 0.18-second minimum windup and 0.06-second release settle; charge
  count and release aim freeze once. Orb recovery is 0.30 seconds. Cloud timings
  and its resource/damage economy remain unchanged.
- Initial homing assists within 12 degrees, prefers crosshair alignment before
  distance, and excludes obstructed targets. Finite range and wall collision
  still apply. Normal and overhead muzzles converge onto native aim.
- Both hands frame the base ball, ease into the throw, and restore authored
  animation. Visible energy repeatedly travels from chest/arms into the ball,
  including at zero charges; charge absorption has a stronger separate cue.
- Open Circuit now requires one stored charge (Stuart's explicit selection).
  Hold Special to feed charges at the existing 0.12/0.30-second cadence. Release
  commits once, then preserves authored unfold and crown activation markers.
  One/three/five charges give 1x/1.5x/2x pulse density and 6/10/14 visual arcs.
  Radius, duration, per-pulse damage, dwell and spear multiplier are preserved.
  Extra pulses suppress direct Static gains so baseline generation does not grow.
- Overhead Orb gather leaves native Primary available, with no two-hand Orb
  pose. Circuit expiry restores forward two-hand ownership. New gains during
  gathering remain outside that cast's frozen allowance.
- Four-language tooltips and active documentation explain the optional spend.
- Linked production checks, full Native verification, then a focused private
  game capture prove their own scopes. Physical controls and multiplayer remain
  human acceptance cases. Do not publish or overwrite historical artifacts.
