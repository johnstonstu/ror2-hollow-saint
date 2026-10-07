# Next special: charge-up strike call (design notes, 2026-10-06)

Status: idea from Stu after the 1.2.0 release (live on Thunderstore). Not started. Name not decided.
It is **its own special**, a third option alongside Gaze of the Hollow and Open Circuit.
Target: a point release after 1.2 (Stu said "1.21 or 1.25"; Thunderstore needs three-part numbers, so 1.2.1 or similar).

## What Stu asked for
- Same hold-to-charge start as the 1.2 Gaze: hold Special and the stored Static Charges spiral into the crown one at a time.
- Each charge drawn in becomes one lightning strike, in the style of the Answered Prayer Thunderbolt (the Royal Capacitor strike the mod already tints and reuses).
- Targets prioritized inside the player's field of view, up to about five.
- **Order:** the strikes fan out, starting with the closest enemies and working outward to the distant ones.
- **Sound:** rolling thunder as the strikes walk outward.
- **Extra charges** (more charges than targets): not decided. Stu's lean: fold them into one big clap at the end.
- Bosses and stronger targets need thought; how hard a full five-bolt cast should hit a single boss is to be tuned in play.

## What already exists to build on
- Charge phase: `HollowSaintMod/FoundationKit/Gaze/GazeState.Charge.cs` (absorb timing 0.12 s then 0.30 s each, armor, hover, Utility backs out, count serialized to the server) and `HollowSaintMod/FoundationKit/Gaze/Fx/GazeChargeUpFx.cs` (orbs spiral into the crown, `Play_HS_GazeLoad1`-`5` sounds). Could be lifted into a shared charge-up state used by both specials.
- Strike presentation: `HollowSaintMod/FoundationKit/Storm/RoyalCapacitorFx.cs`, `Strike(position, owner)` spawns the palette-tinted Thunderbolt with the `Play_HS_ThunderStrike` sound. `ThunderboltDriver` owns damage, procs and splash for the passive's strikes.
- Targeting references: the Gaze lock-on bolt (`HollowSaintMod/FoundationKit/Gaze/GazeFuelController.Under.cs`, BullseyeSearch with line of sight) and Arc Bolt's small aim-assist cone.
- Static Charges: `DischargeMeter` (bank of 5 by default, `KitTuning.StormChargeMax`).
- Audio: the Wwise bank is authored by `tools/audio/author_bank.py` from WAVs in `art/audio/source` (mix trims in `MIX_TRIM_DB`). ThunderStrike is built from a Pixabay sample and kept out of git. A rolling-thunder layer would be a new cue (sample or synth).
- Test harness: `HollowSaintMod/DevAutopilot*.cs` segments via `HS_SEGMENTS` (see `DevAutopilot.GazeCharge.cs` for a charge-up test pattern); `tools/dev-profile/Stage-Build.ps1` stages into the `Hollow Saint Dev` profile.

## Design sketch
- **Targeting:** BullseyeSearch from the aim ray with a view cone (about 40 to 60 degrees), line of sight on. Pick up to N targets (N = charges, max 5) by angle to the crosshair, then sort the **strike order by distance from the Saint, near to far**. Lock targets at release; a marker over each during the charge-up reads well from the default camera (right behind the player).
- **Timing:** strikes land one after another walking outward, about 0.12 to 0.18 s apart, with slightly longer gaps for farther targets so the thunder rolls. The rolling-thunder bed starts at the first strike and tails off after the last.
- **Extra charges:** candidate is a final clap on the highest-health target (or the crosshair point) that scales with the leftover charges, landing after the fan-out. Alternative: refund them. Decide in play.
- **Bosses and elites:** options are (a) the final clap naturally favors the boss, (b) a priority weight for bosses/elites in target selection, (c) a cap so five bolts plus a clap on one boss is strong but not a one-shot. References: the full-bank Thunderbolt is about 765%; the Gaze opening blast is 600% per charge.
- **Empty bank:** decide whether a tap with no charges still calls one bolt or fails with feedback.
- **Cooldown:** starts when the last strike lands.
- **Multiplayer:** server picks targets and spawns strikes (networked effects); clients show the charge-up and markers.
- **Text:** new skill name/description tokens in all four languages in `HollowSaintMod/Language/HollowSaint.language`; keep character-select descriptions to about two lines (1.2 trimmed them for that reason).

## Open questions for Stu
1. Name.
2. Extra charges: final clap or refund (lean: clap).
3. Damage for a full cast on a single boss (tune in play).
4. Empty-bank behavior.
