# Playtest v0.9.17 release candidate (2026-10-02)

Build label: **v0.9.17 | release candidate**. No gameplay changes since 0.9.16; this is the 1.0 cleanup.
The 0.9.16 list below is still open, and is what decides 1.0.

## Before Thunderstore (about 20 minutes)

Multiplayer ships untested (the README says so).

- [ ] **Clean install:** r2modman profile **Hollow Saint Clean** (only the declared dependencies, now including Risk Of Options, plus the packaged zip, no config). Start it; Hollow Saint is in character select.
- [ ] **Mod Options:** Settings > Mod Options shows Hollow Saint with sections "0. Movement" to "7. Gaze of the Hollow". "6. Misc" has "Verbose log" (off).
- [ ] **One real run, stages 1 to 3, default config:** every skill, at least one Thunderbolt, one Gaze, one Open Circuit (8 s cooldown after the crown closes). Anything that feels wrong on fresh defaults?
- [ ] **Skins:** each of the 5 in the Loadout spin and in game.
- [ ] **Watch for:** the rare hand pop when a spear charge starts.
- [ ] **Log:** quit, then open `BepInEx/LogOutput.log` in the Clean profile. One Hollow Saint line ("Hollow Saint 1.1.0 loaded.") and no Hollow Saint warnings or errors. "ClipCursor failed" lines are the game, not us.
- [ ] **Icon:** the new gaze-beam icon (package name is now `Hollow_Saint`, shown as "Hollow Saint"). Previews in `artifacts/thunderstore-preview/`.

# Playtest v0.9.16 (2026-10-02)

Build label: **v0.9.16 | spear impact | step lift | 5 orbs**.

- [ ] **Spear sound:** every spear now hits with a thump and an electric zap as it sticks, and a charged spear adds a deeper boom on the burst. Is it punchy enough now, or too loud next to the rest?
- [ ] **Spear impact visual:** the burst has height: an expanding shell of rings, arcs leaping out over a dome, and a lightning column. Full charge is big; is it too busy?
- [ ] **Arc Step:** look up and step to rise about 2 m (onto ledges, out of a fall); look down in the air to drop. Level steps are unchanged. Options "3. Arc Step" > "Look lift" (0 = old behaviour).
- [ ] **Thunderbolt:** every 5 Electrocutes (5 orbs on the halo). Also new: an enemy that dies with half its Static or more Electrocutes as it falls (lights an orb, arcs to neighbours), so Static on fast kills isn't wasted. My harness pack fight (no items, level 1): about one Thunderbolt every 15 s. Often enough? Options "5. Storm" > "Death discharge" (0 = off).

# Playtest v0.9.15 (2026-10-02)

Build label: **v0.9.15 | slow thunderbolt | gaze armor | crown cooldown**. Includes the 0.9.14 left-hand spear, so that list below still applies.

- [ ] **Thunderbolt:** about 1.6 s from the orbs combining to the hit (was 0.6 s). Too slow in a real fight? Options "5. Storm": Thunderbolt telegraph (combine) and Thunderbolt flight.
- [ ] **Gaze:** B on the controller ends the beam. +30 armor while channeling (buff icon shows). Options "7. Gaze of the Hollow".
- [ ] **Open Circuit:** 10 s crown, 8 s cooldown after it closes (about 19 s between casts). Is it too long a wait, or still too often? "Cooldown after crown" in "4. Open Circuit" turns the old cast-start cooldown back on.
- [ ] **Storm text:** Skills tab passive and the "The Storm" box (hover the passive or Arc Bolt in Loadout). Better?

# Playtest v0.9.14 (2026-10-02)

Build label: **v0.9.14 | left hand spear | right hand bolts**.

- [ ] **Left hand spear:** charge with the left trigger. The spear forms in the left hand, sits raised to the left of the halo, and the throw whips over the left shoulder. Does it feel better on the controller?
- [ ] **Off hand:** while charging, the right hand is held out in front and Arc Bolt fires from it.
- [ ] **Grip:** the left fingers close on the shaft. These are procedural because the bundle only has right-hand grip clips. Do they look right up close, e.g. in the character spin?
- [ ] **Throw read:** the whip, release and follow-through across the body to the right hip.
- [ ] If anything looks worse than the right hand did, Options > "2. Stormspear" > "Spear hand" (Auto, Left, Right) switches it between throws, so you can compare.
- [ ] Still open from 0.9.13: spear sound, splash, CC, Gaze ramp, storm explanation.

# Playtest v0.9.13 (2026-10-02)

Build label: **v0.9.13 | stun pass | storm help**. From your notes:

- [ ] **Spear sound:** tap = one punchy hit; charged = crackle as it sticks, the strike on the burst. Still off? Say whether it's timing, volume or the sound itself.
- [ ] **Full-charge splash:** 100% of the spear's damage to everything around the stuck enemy (tap 50%), 10 m radius at full.
- [ ] **CC:** Electrocute is a 0.5 s jolt + Shocked on everyone. Arc Bolt procs 0.8 direct, chain 0.4/0.2/0.1. Try one Stun Grenade again. Stun Grenade shows the vanilla stars; Electrocute shows our lightning flash.
- [ ] **Gaze:** forks reach further the longer you hold the beam (40% to 160%).
- [ ] **Storm explanation:** Skills tab passive (3 steps) and the "The Storm" keyword box (hover the passive or Arc Bolt in Loadout). Does it make sense now?
- [ ] Static/Electrocutes come a bit slower now (lower proc). Is the Thunderbolt still frequent enough?

# Playtest v0.9.12 (2026-10-02)

Build label in game: **v0.9.12 | crown spear | intro skip**. Main things since 0.9.9:

- [ ] **Sticking spear** (0.9.10): lodges in the enemy, crackles about a quarter second, bursts on everyone else around it. Ground hits stick and do a half-strength burst. Does the stick read from the normal camera? Is 0.25 s right? (Options: "2. Stormspear" Stick seconds / Ground burst.)
- [ ] **Balance:** full spear 1600% (the burst no longer hits the stuck target), Gaze cooldown 12 s. Too strong or weak anywhere?
- [ ] **Shading** (0.9.11): his own look is back, with cloak/shield/crit/immune/elite effects on top. Play a run on Umbral, including a dark stage. Pick up a Personal Shield Generator or an elite aspect if one drops.
- [ ] **Character select:** shorter skill text, bolder icons, Umbral readable on the select screen.
- [ ] **Crown spear** (0.9.12): with Open Circuit as the special, the spear above the head is now the real spear model instead of only lightning lines, with less clutter around it. Check from behind and when turning side-on.
- [ ] **High attack speed:** Arc Bolt arm motion should no longer snap at very high attack speed (lots of Syringes).
- [ ] Anything in the item displays that looks wrong (they use Commando's placements).

Not done yet: multiplayer test, hand-fitted item displays.

# v0.9.8: spear beside | gaze default

Confirm the tag **v0.9.8 | spear beside | gaze default**.

- **Gaze of the Hollow is now the default Special**; Open Circuit is the alternate. If the loadout
  screen still shows Open Circuit, it is a saved pick from before the swap (the slot order changed):
  pick Gaze once and it sticks.
- Spear hold re-aimed for the camera right behind you: the hand holds the spear out to the right of
  the halo, nose-up, so its whole length shows beside the body (grip about 90 px and tip 55-70 px
  clear of the head at 1280x720). It grows and brightens visibly from thin to full as it charges.
- Left hand is held out in front toward the target while charging. Arc Bolt fires straight out of
  it with a small recoil (no separate arm gesture while the spear is up); while bolting the spear
  draws further back, and settles back beside you about 0.7 s after the last bolt.
- The throw whips from the side position and releases right of the head, so the launch is visible.

Checked: build and access scan pass; `artifacts/spear-beside098b` errors=0, all hold checks pass at
level/up/down aim; KIT_VERIFIED special=HollowSaintGazeOfTheHollow. Your 21:15 log had no Hollow
Saint errors (one "Invalid Layer Index -1" warning pair before a spear charge, not seen in any test run).

- [ ] From the normal camera: spear readable beside you while charging? Charge growth obvious?
- [ ] Charge + Arc Bolt: left hand out front, bolts from it, spear back. Feel right?
- [ ] Throw readable from behind.

# v0.9.7: javelin flow | overhand toss

Confirm the in-game tag **v0.9.7 | javelin flow | overhand toss** (Hollow Saint Dev).
Only the hand-form Stormspear hold and throw changed; aim assist and everything else is as v0.9.6.

Hold (charging):
- The spear no longer snaps into a fixed pose. The hand draws it up from the right hip, out and
  over the shoulder into the cocked hold (0.3 s, eased both ends).
- The wind-up deepens with the charge: hand drawn further back, torso twists and leans back more.
- The left hand reaches forward toward the target while you hold (gives way whenever Arc Bolt fires).
- Slow breathing bob on the hand, shaft and chest; the full-charge tremble is kept.
- Spear tip carried nose-up about 14 degrees, like a javelin.

Throw:
- Overhand arc: torso rotates through first, elbow comes up high and the hand drives over the
  shoulder onto the aim line.
- **The spear now leaves the hand at the top of the whip, 0.083 s after you let go** (was instantly
  on release, while the arm was still cocked). Projectile, launch flash and throw sounds all fire
  from the hand at that moment. Tunable: `StormspearTuning.HandReleaseDelay` (0 = old behaviour).
- Follow-through sweeps down and across to the left hip while the left hand pulls back, with a
  small forward lean of the body, then eases back to idle over about 0.25 s.

Checked: Release build and access scan pass; focused javelin run `artifacts/javelin-flow097d`
errors=0, all rear-pose checks pass at level, up and down aim (grip 0.42-0.45 out, 0.42-0.55 back,
0.52-0.57 up, elbow 61-107 deg); aim-assist flights unchanged. The remaining pop flags come right
after paused slow-motion captures (the long render frame), not from the pose.

- [ ] Hold right click from behind: does the draw-up and the charge wind-up read smoothly?
- [ ] Tap throws: does the 0.083 s release feel laggy? If so it can go to 0.066 or 0.05.
- [ ] Charged throw: does the toss read as one motion, spear leaving at the top?
- [ ] Hold while firing Arc Bolt: left arm hand-off between guide and bolt.

# v0.9.6: aim assist | cocked hold

Installed into **Hollow Saint Dev** and ready for playtest. Confirm the in-game
tag **v0.9.6 | aim assist | cocked hold**. r2modman's listing may still show
v0.9.4: its metadata update refused while the manager was open. The installed
game DLL is v0.9.6; the manager's cached listing does not identify that DLL.

Arc Bolt and hand/crown Stormspear now acquire one visible enemy within
3 degrees either side of the launch direction and 80 m, then gently steer
toward it. Steering is limited to 45 degrees/s and 8 degrees across the whole
flight. No new target is acquired after loss, passing the target or obstruction.
Both skill sections have an **Aim assistance angle** option (0 disables it).
Server projectile flight also drives the visible projectile; no straight local
prediction is used for these guided shots. Check multiplayer latency separately.

Includes the higher, bent-elbow spear hold from the v0.9.5 candidate below.
Release build and game API access pass. Offline aim geometry checks pass all
15 cases, including near-miss recovery at both default projectile speeds;
Stormspear damage/radius checks pass. Focused solo game run completed with
zero skill-script/validation errors, 60 captures, 70 rear pose assertions and
8 actual projectile-flight assertions in `artifacts/aim-cocked-v096-check`.
Both unassisted near misses acquired nothing and hit nothing; the assisted
bolt and spear each acquired an enemy and landed one direct hit, within the
8-degree budget (7.61 / 6.31 degrees used). These four controlled shots are
functional checks, not an accuracy-rate or moving-target benchmark.

Checked grip: 0.49–0.51 arm lengths outward, 0.44–0.49 behind the shoulder,
0.50–0.51 above it; elbow bends 54–104 degrees. Shaft projects 72–99 px at
early charge and 181–251 px at full charge in checked 1280x720 / 60-degree
rear views. Rear, normal-camera, side, elevated-aim and throw frames reviewed.
The capture harness flagged five abrupt motions during transitions; paused
captures and slow-motion throws do not prove full-speed fluidity. Judge live
feel, especially strafing, moving enemies, quick taps and the throw transition.

The first run (`artifacts/aim-cocked-v096`) failed before arena setup because
the lobby disappeared during a wait. The harness now rechecks the lobby and
the repeated run completed. Neither run stopped a user game process.
Final DLL SHA256:
`DF1F48EC854FD596A24EC29EEBE0912A19C8A2F9A5D0E75105A7734647EA4EFD`.

- [ ] Try small misses either side of moving enemies with Arc Bolt and hand/crown spear.
- [ ] Try crowded packs, distant targets and aiming near walls; judge assistance strength.
- [ ] Hold left trigger from directly behind: judge grip, elbow, charging visibility and throw.
- [ ] If needed, tune **Aim assistance angle** independently in the Arc Bolt and Stormspear sections.

Rollback with the game closed: restore the v0.9.4 DLL from
`artifacts/foundation/profile-backup-20261001-202244`. Proc values are unchanged.

# v0.9.5 candidate: javelin | cocked hold

Built locally; **not staged or game-verified yet** because Risk of Rain 2 was
running when Stage-Build was called. Installed Dev remains v0.9.4.

The user reported the holding pose looks wrong. This candidate puts the hand
higher above/behind the shoulder and shortens the reach to bend the elbow,
instead of holding the arm almost straight sideways. Additional shaft yaw
keeps its rear end exposed. The focused harness now checks an elevated grip,
45-110 degree elbow bend and visible rear projection, as well as camera alignment.
Release compilation passes; game checks must run after the game is closed and
the DLL can be staged. Item proc balance is unchanged pending the user's choice.

Current chain behavior: direct bolt proc 1.0, all three bounces proc 0.5.
Damage already falls by 0.75 each bounce. Proposed item weighting is
0.5 / 0.25 / 0.125 for bounces. This also reduces their Static contribution;
Will-o'-the-wisp is an on-kill effect and is not directly reduced by this change.

The wider ability/item audit is in [docs/proc-balance-audit.md](docs/proc-balance-audit.md).
It covers Gaze, spear burst, Thunderbolt, healing and on-kill items, plus the
missing Behemoth event on manual lightning hits. Its balance suggestions are
not applied to the current candidate.

# v0.9.4: javelin | drawn-back charge

Launch **Hollow Saint Dev** and confirm **v0.9.4 | javelin | drawn-back charge**.

The hand is drawn much farther back and outside the right shoulder, with the
elbow lifted outward and a stronger torso wind-up. The spear's rear end now
fans out behind the back at a visible diagonal. This addresses the end-on
view that still hid too much of the v0.9.3 spear from the rear gameplay camera.
The fitted grip, arm/core current and halo feed follow the new pose together.

Verification: Release build, game-member access and Stormspear tuning checks
pass. Focused game run completed with zero skill-script/validation errors,
60 captures, and 50 rear-view assertions. The normal player camera is explicitly
aligned with the scripted aim; body-facing and aim checks reject side views.
Rear shaft projection is 75-91 px at early charge and 177-235 px at full charge
in the checked 1280x720 / 60-degree views. Actual grip reaches 0.75-0.77 arm
lengths outward and 0.53-0.57 behind the shoulder. Rear/player/side captures,
off-hand casting, aim up/down, jump and recovery were visually reviewed.
Evidence: `artifacts/javelin-drawn-v094-rear`. The first
`javelin-drawn-v094` run had a camera/input mismatch and is not rear-view proof.
The corrected harness still flags three abrupt hand motions during throws or
landing; paused captures and slow-motion throws do not prove full-speed fluidity.

Reproduce: set `HS_SEGMENTS=javelin`, run `tools/dev-profile/Run-Autopilot.ps1`
with a fresh `-Name`, then clear the environment variable. No test mode is
enabled during ordinary launches. Final DLL SHA256:
`AFE46BF6C9AED26EF044B6F1B2A8ABA2FD4050FA5BB4452C2F459D441A40FEA0`.

- [ ] Hold left trigger with the camera directly behind, aiming straight ahead:
  judge the forming spear, its full length behind/beside you, and the energy feed.
- [ ] Charge while firing off-hand bolts, strafing and jumping. Aim up/down.
  Check shoulder/elbow shape, attachment, visibility and clipping.
- [ ] Release a full charge and quick taps: does the drawn-back hold read as
  winding up and chucking a javelin over the shoulder?

Rollback with the game closed: restore HollowSaint.dll from
`artifacts/foundation/profile-backup-20261001-192316` (v0.9.3).
The asset bundle and skill tuning are unchanged.

# v0.9.3: javelin | wider charge pose

Launch **Hollow Saint Dev** and confirm **v0.9.3 | javelin | wider charge pose**.

The hand-form javelin now charges farther outside the right shoulder. The actual
arm and fitted grip move together, with less inward shaft yaw so the growing
spear stays exposed from behind. The existing throw path and skill tuning remain.
This build also retains v0.9.2's restored Gaze.

Verification: Release build and access/tuning checks pass. Scripted solo run
completed with zero errors during its skill script; 200 captures are in
`artifacts/javelin-wide-v093`. Rear forming/full-charge, side grip and throw/recovery
frames were reviewed. Full-charge eye-to-shaft clearance measured 52-53 cm
(previous capture: 30 cm). The capture harness still flags abrupt hand motion
during attacks/transitions (23 flags across the run versus 26 in the prior run);
these include deliberate throws and screenshot hitches, so this is not proof of
pop-free motion. Judge the live transition feel below.

- [ ] Hold secondary with the camera directly behind: can you clearly see the
  short forming spear as well as the full charge beside the body?
- [ ] Charge while strafing, jumping, aiming up/down and firing off-hand Arc Bolt.
  Check the wide arm silhouette, head clearance and hand attachment.
- [ ] Release full charges and quick taps: check the transition from the wide
  hold into the throw, and back to ordinary movement.

Rollback with the game closed: restore HollowSaint.dll from
`artifacts/foundation/profile-backup-20261001-191230`. The asset bundle is unchanged.

# v0.9.0: stormspear | crown-spear

Launch **Hollow Saint Dev** and confirm **v0.9.0 | stormspear | crown-spear** in the top-left corner.

## What changed

- **Primary.** Arc Bolt only, both hands. The held-spear fan is gone, so the primary never changes.
- **Secondary: Stormspear** (replaces Conduit Spear). Hold to form a spear of lightning in your right hand, release to throw. 2.0 s to full at base attack speed (faster with attack speed). 400% tap up to 1400% full.
- **Off-hand casting.** While you charge in the hand, Arc Bolt keeps firing from the left hand only, at half rate. Release and both hands come back.
- **Impact burst.** No planting and no recall. The spear bursts into AoE lightning on impact: 3 m for a tap up to 9 m at full, for 50% of the hit.
- **Stocks.** 5 s cooldown. Backup Mags add spears. Tap to dump them fast, or charge each one.
- **Open Circuit.** It's the same crown and pulses, but while it's up the spear forms above your head instead, charges 2.5x faster, and both hands keep casting at full rate. A fully charged spear thrown in the crown calls a Thunderbolt where it lands.
- **Charge VFX.** Current runs from the core up the arm into the palm, tendrils pull off the halo, and the spear grows and locks in at 1/3, 2/3 and full with ticks. At full there's a crackle and a ready flash, and the halo dims while the power sits in your hand. On release the halo snaps back.
- **Removed.** The Conductor mark, planted-spear pulses and spread, recall, and the fan. Their config entries are gone. Everything new is in config section "2. Stormspear".

## Please check

- [ ] Charge and throw while moving and jumping. Does the half-rate off-hand Arc Bolt feel like a fair cost?
- [ ] Is 2.0 s to full right? (Slider: "2. Stormspear" charge seconds.)
- [ ] Tap-dump with extra stocks: does it feel snappy?
- [ ] Is the burst size and look readable on tap vs full?
- [ ] Open Circuit: does the spear above the head look right? Does the full-charge Thunderbolt feel like a nuke?
- [ ] Hand spear size in the palm (the model length is clamped; tell me if it looks too long or short).
- [ ] Any arm pops between the charge hold, throw and off-hand bolts.

Smoke run (autopilot, arena): 0 errors. Full hand charge, tap throw and crown full charge with Thunderbolt all fired. Screenshots are in artifacts/autopilot-v090.

# v0.8.0: crown states | steady pose

Launch **Hollow Saint Dev** and confirm **v0.8.0 | crown states | steady pose** in the top-left corner.

## What changed

- **Ring.** The halo is now a state machine driven by the crown: rest, unfold, hold, close. It always returns to rest. It closes cleanly if the cast is interrupted, stays open through a recast, and blends from hold to close instead of jumping. The old halo safety net is gone.
- **No pose pops.** Animation requests no longer force a mid-frame animator evaluation. Idle gesture layers fade to zero weight instead of holding a stale arm pose, which caused the hand pops.
- **Aim.** The torso, neck and head follow the crosshair, fully in combat and as a relaxed look-at out of combat. Toggle: "Aim follows crosshair".
- **Pause.** Arm life, lean and spear aim hold while the game is paused.
- **Impact.** Spear impact, Thunderbolt and bolt-on-spear hits get a brief hit-pause and camera shake. Arc Bolt and the spear throw get a light camera kick. Toggle: "Impact feel".
- **Body reactions.** Heavy hits rock the torso back, and catching the spear lands in the shoulder and chest.
- **Death.** The Saint ragdolls, the held spear drops as a physics prop, and the body light gutters out.
- **Shock overlay.** Shocked and Electrocuted enemies show the vanilla shock overlay.
- **Sound.** New original sounds: spear recall hum, catch clank, fan start/loop/end, planted-spear pulse and struck zap. Thunderbolt has a new thunderclap.
- **Items.** Item displays use Commando's placements as a first pass. They will be rough. Toggle: "Item displays (first pass, restart)".
- **Hopoo shading (experimental, off by default).** Switches the body to RoR2's own shader, so elite, cloak, freeze and shield overlays show. Restart after toggling. Compare it against the current look.
- **Performance.** The skin light atlases are BC7-compressed, about 37 MiB down to about 9 MiB of GPU memory. One-shot lightning bolts are pooled, with a budget for proc-heavy fights.
- **Multiplayer.** A spear recall that reaches the server before the spear registers now starts on arrival, instead of being dropped.

## Please check

- [ ] Open Circuit while moving, dashing and attacking. Let it expire. Recast it if a cooldown reset turns up. The ring always ends closed.
- [ ] Wrists and hands during throw, catch, fan start/end, and the switch between standing and moving gestures. No pops.
- [ ] Aim up and down with every skill. The torso, head and arms should look intentional.
- [ ] Impact feel on spear hits and Thunderbolt. Is it too strong or too weak?
- [ ] Die once: ragdoll, spear drop, light fade.
- [ ] Listen to the new sounds in combat, especially the fan loop and spear pulse levels. Audition file: art/audio/hollow-saint-audition-v08.wav.
- [ ] Pick up items: are the displays acceptable as a first pass?
- [ ] Optional: turn on Hopoo shading, restart, and compare all five skins.

## Rollback

With the game closed, restore both files from the newest artifacts/foundation/profile-backup-* folder made before v0.8.0. To return to v0.7.14 exactly, use profile-backup-20260930-181849, which has the v0.7.14 DLL and bundle13.

# v0.7.14: corrected hand/ring preview

The earlier Solar current-flow01 GIF was misleading: same-editor-frame captures
reused older skinned poses, and its fixture skipped the ring's actual closing clip.
That explains the detached hands and displaced ring Stu identified. Retire that
clip as visual attachment evidence.

Reviewed replacements: current-flow02/skin0-handoffs.gif through skin4-handoffs.gif,
with freshly CPU-baked geometry, the real halo closing path and held-spear state
preserved through dash/recovery. Independent end-pose02/native-frames uses ordinary
skinned rendering over separate editor updates; reviewed end frames keep wrists
connected and return the ring to rest. Check-EndPose deliberately fails when the
closing clip is skipped, and passes with it. Native route checks: 90 cases pass.

The installed DLL/model remains v0.7.14; this corrected the preview, not production
geometry or gameplay. These are native pose studies with explicit skill/motor
adapters. Actual game input, combat feel, bloom and multiplayer remain unverified.
Use the checklist below after reviewing the corrected visual result.

# v0.7.14 final daytime playtest

Launch **Hollow Saint Dev** and confirm **v0.7.14 | skin flow | cached lighting**.
This retains today's movement, finger/grip, spear recovery and connected lightning
refinements. Final lighting metadata caching preserves the v0.7.13 appearance.

- [ ] Try fan -> throw -> Arc Bolt -> free recall/catch -> fan while standing,
  strafing, sprinting, jumping/landing and dashing. Look for soft arm/finger recovery,
  a stable held grip and a continuous body-to-hand/spear source during abilities.
- [ ] Open/close Open Circuit while attacking and moving; rapidly switch casting
  sides and restart abilities during recovery. Check ring/core/arm pulses and light
  transitions without flashes or lingering trails at rest/death/invisibility.
- [ ] Repeat across all five skins. Cycle selection skins after a pulse has ended,
  return to default, and inspect front/back body lights, hands, spear and fan hues.
- [ ] Judge busy-fight readability, bloom, actual performance and multiplayer.
  Check near/far atlas detail and the known small forearm-strip skin-weight flaw.

Verified native recovery, all-skin material/geometry and connected-current checks;
5,000 stable cache ticks and live emission updates; native renders reviewed. Build
has zero errors (known NU1701 warning); access and offline controls/motion checks
pass. Mono's allocation counter failed calibration, so there is no measured FPS or
zero-GC claim. Final v0.7.14 real-game version/catalog/five-skin startup checks pass
(game-smoke0714); the baseline ProBuilder shader error, OS cursor-confinement denial
and existing JobTempAlloc warning remain, with no unexpected/mod errors. The test game closed normally.
Live input, body spawn, gameplay feel, bloom, physics and networking remain unverified.

Rollback to v0.7.13: with game closed restore BOTH DLL and bundle13 from
`artifacts/foundation/profile-backup-20260930-144839` into the Dev plugin folder.
For v0.7.12 use the prior backup below. Nothing was pushed or publicly released.

# Previous v0.7.13 playtest: smoother light recovery and reliable skin transfers

Launch **Hollow Saint Dev** and confirm **v0.7.13 | current flow | softer recovery**.
The model's ability light eases in/out and refreshes its color after skin changes.
All previous movement, finger/grip and connected-lightning work remains; bundle13,
attack controls/timing and balance are unchanged.

- [ ] Start/release the held fan repeatedly, close Open Circuit and exit dash.
  Look for a brief, smooth return of body lights to their normal glow, without a pop.
- [ ] Restart another ability during recovery. The light boost should flow into it;
  ordinary bolts/lines and hit timing should retain their usual response.
- [ ] Switch through all five skins after the model light has settled, then while
  abilities are active where supported. Check core, arms, hands and ring match the
  selected skin. Repeat the character-select skin cycle and return to the default.
- [ ] Check death/respawn, invisibility and pause/unpause for stuck brightness.
  Continue movement/grip/finger, all-skill, bloom and performance checks below.

Native recovery/palette verification: **21 cases / 2,319 assertions**, all five skins
at 30/60/144 Hz, all skin-pair transfers and mannequin selections. Skin atlas/current
integration and prior VFX/spear/movement/arm cases also pass. Native captures were
reviewed; they are lighting/route studies, not gameplay or actual bloom. Build/access
and offline checks pass. Real v0.7.13 content startup/catalog/five-skin checks pass.
The baseline ProBuilder shader error and OS cursor-confinement denial remain;
gameplay, input, bloom and networking are unverified. The test game closed normally.

Rollback to v0.7.12: with game closed, restore BOTH DLL and bundle13 from
`artifacts/foundation/profile-backup-20260930-142413` into the Dev plugin folder.

# Previous v0.7.12 playtest: unified skin-colored body light

Launch **Hollow Saint Dev** and confirm **v0.7.12 | skin current | unified glow**.
Older baked body lights now match the relic skin's lightning. The body and selection
display share the compact surface layout; existing animations, controls and balance remain.

- [ ] Switch through Cracked Icon, Obsidian Saint, Verdigris Relic, Solar Vespers and
  Umbral Choir in character select, then in play. Check front/back body seams,
  shoulders, core, ring and hands retain armor detail and the correct light color.
- [ ] Alternate Arc Bolt hands, fan -> throw -> recall/catch -> fan and Open Circuit.
  Body light details should pulse with active current and settle after the ability.
  Check the posterior-arm -> elbow -> underside -> hand/spear connection in motion.
- [ ] Repeat while strafing, sprinting, jumping/landing and dashing. Check held grip,
  free-finger recovery, idle/death/respawn cleanup and busy fight readability.
- [ ] Check real bloom, brightness and performance on each skin. Inspect near and
  far views for atlas seams/mip artifacts; user gameplay remains essential.

Verified: five-skin light/geometry/pulse checks, 90 connected-current cases, all prior
700 VFX / 96 spear / 192 movement / 144 arm cases, and 240 baked grip poses plus
242 dense finger-marker poses. Native front/rear renders and body-current studies
were reviewed using actual game material tuning. Build/access/offline checks pass.
Real v0.7.12 startup/catalog/five-skin checks pass. The baseline ProBuilder shader
error remains, and this smoke logged an OS cursor-confinement permission error.
This is content startup evidence, not a gameplay, bloom or multiplayer test.
Seven shared 1024px skin atlases add roughly 37 MiB of GPU storage; body skinning
uses ~74% fewer vertices than the previous runtime split, with the same triangles.

Rollback to v0.7.11: with game closed, restore BOTH DLL and bundle12 from
`artifacts/foundation/profile-backup-20260930-133057` into the Dev plugin folder.
Use this backup, not the later intermediate v0.7.12 audit backups.

# Previous v0.7.11 playtest: connected-current transfers

Launch **Hollow Saint Dev** and confirm **v0.7.11 | connected current | softer transfers**.
The core feed now eases between casting sides while staying attached to the moving
torso. Bundle12, the finger refinement, controls, attack timing and balance remain.

- [ ] After throwing, alternate Arc Bolt hands rapidly and use Open Circuit while
  firing. Watch the short core-to-ring connection as the active arm changes.
- [ ] Fan -> throw -> recall/catch -> fan, then interrupt with dash/jump/sprint.
  Check the ring-to-back-of-arm -> elbow -> underside -> hand/spear route in motion.
- [ ] Repeat with all five skins, including front/rear views. Current lines should
  keep the selected skin's color throughout transfers and settle after an ability.
- [ ] Check death/respawn and idle cleanup. Continue the grip, movement and finger
  checks below; real busy-fight transitions and bloom still need gameplay review.

Native verification: **90 cases / 544,308 assertions**, five palettes at 30/60/144 Hz,
including translated/turning native poses, connected endpoints and cleanup. All prior
VFX/spear/movement/arm cases were rechecked and pass; build/access/offline checks pass.
`artifacts/current-flow01/skin*-handoffs.gif` are body-route inspection studies with
currents sustained near release/catch markers. They are not gameplay or proof of
actual ability cadence, full fan/crown rendering or game bloom. v0.7.11 has not been
game-launched; last startup smoke remains v0.7.8. Some new model light strips appear
cyan in Solar previews and are queued for the next focused material audit.

Rollback to v0.7.10: with game closed, restore BOTH DLL and bundle12 from
`artifacts/foundation/profile-backup-20260930-123117` into the Dev plugin folder.

# v0.7.10 playtest: softer finger handoffs between abilities

Launch **Hollow Saint Dev** and confirm **v0.7.10 | finger flow | softer handoffs**.
Free-finger life now gently follows changing hand poses. Bundle12, attack timing,
controls, balance and the connected lightning pass remain.

- [ ] Throw, alternate Arc Bolt hands, then open/close Open Circuit. Look for calm
  finger motion as the gestures change and recover; repeat while strafing/jumping.
- [ ] Recall/catch -> hold -> fan -> throw repeatedly. The right hand should retain
  its fitted grip, then ease into free-finger motion after release.
- [ ] Compare default and maximum Arm life intensity, then toggle it off/on. Check
  fingers settle without a stuck twist. Repeat movement/dash/ability checks below.
- [ ] Continue all-skin lightning and busy fight checks. Judge the small finger
  improvement in actual play; the native closeup exaggerates its visibility.

Native checks: **234 clip sequences / 24 handoffs / 60,587 assertions** at
30/60/144 Hz. Default high-frame-rate additive steps reduced from ~0.95 to ~0.19
degrees. All prior VFX/spear/movement/arm/controller checks pass, as do build/access
and offline checks. `artifacts/finger-flow01/free-fingers.gif` uses actual native
geometry/animation with neutral inspection materials, not gameplay or skin colors.
v0.7.10 has not been game-launched; last startup smoke remains v0.7.8. Real feel,
physics/networking and game bloom remain unverified.

Rollback to v0.7.9: with game closed, restore BOTH DLL and bundle12 from
`artifacts/foundation/profile-backup-20260930-113413` into the Dev plugin folder.

# v0.7.9 playtest: movement/skill handoffs and recovery

Launch **Hollow Saint Dev** and confirm **v0.7.9 | state flow | motion recovery**.
Bundle12 and normal animation timing/balance remain. This build prevents an invalid
motor sample from corrupting subsequent locomotion animation.

- [ ] Walk/run/strafe in all directions, reverse, stop and resume. Check calm arm
  follow-through and that the body returns naturally to standing.
- [ ] Jump, double jump and land while moving. Dash forward/back/left/right while
  rising and falling; exit should choose the matching air pose before landing.
- [ ] Sprint-glide into jump/dash, release sprint and start moving again. Watch the
  torso/arms settle and listen for a glide loop that fails to stop.
- [ ] After throwing, fire Arc Bolt while starting/stopping movement. Repeat Open
  Circuit during movement changes. Gesture transfers should preserve their progress.
- [ ] Continue the fitted-grip, fan/recall/throw and all-skin checks below. Busy fights,
  real networking and animation feel still need actual gameplay review.

New native production-controller checks: **63 cases / 293,566 assertions** at
30/60/144 Hz, plus all previous VFX/spear/movement/arm checks. Covers actual native
Animator handoffs and invalid-sample recovery using explicit game adapters and a
fixed frame clock. `artifacts/presentation-flow01/sprint-jump-dash.gif` is a rooted
pose study, not gameplay. v0.7.9 has not been game-launched; last startup smoke is
v0.7.8 (catalog/five-skin pass, baseline ProBuilder shader error remains).

Rollback to v0.7.8: with game closed, restore BOTH DLL and bundle12 from
`artifacts/foundation/profile-backup-20260930-102516` into the Dev plugin folder.

# v0.7.8 playtest: fitted grasp through throwing and catching

Launch **Hollow Saint Dev** and confirm **v0.7.8 | grip timing | spear flow**.
Bundle12 corrects finger interpolation at release/catch. Earlier movement, VFX,
controls and balance remain.

- [ ] Throw from standing, strafing, sprint-gliding and airborne movement. The
  fingers should stay seated around the molded grip until release, then open with
  the existing follow-through. Watch the thumb and ring/little fingers closely.
- [ ] Recall/catch while moving and aiming up/down. The index fingertip should
  settle into the fitted grasp when the spear arrives without dipping into it.
  Repeat rapid catch -> fan -> throw transitions.
- [ ] Hold/channel and use Open Circuit in all skins. Check that the fitted grip
  remains stable while the free hand, wrists and body retain their soft reactions.
  Compare normal and maximum arm-life intensity, then test the option off.

Native checks pass: 240 baked poses with all 27 hand parts, zero triangle-surface
intersections and about 1.21 mm minimum sampled grip-vertex clearance; 3,630 dense
finger checks across 242 pre-release/post-arrival poses. All earlier native VFX,
spear, movement and arm-pipeline cases pass on model12. Neutral-material inspection
closeups are under `artifacts/grip-flow02`; these do not represent game lighting.
Build/access and offline checks pass. v0.7.8 loaded in the actual game and passed
catalog/five-skin checks; the baseline ProBuilder shader error persists. Gameplay,
busy transition blends, feel and networking remain untested.

Rollback to v0.7.7: with game closed, restore BOTH DLL and bundle11 from
`artifacts/foundation/profile-backup-20260930-093051` into the Dev plugin folder.

# v0.7.7 playtest: fingers easing out of the fitted grip

Launch **Hollow Saint Dev** and confirm **v0.7.7 | finger flow | arm recovery**.
All earlier spear, movement and skin/VFX refinements remain; bundle11 and balance are unchanged.

- [ ] Throw the held spear while standing, moving and airborne. Procedural finger
  sway should ease in over about 0.2 s after the hand opens instead of switching
  on immediately. The authored release timing and pose remain.
- [ ] Recall/catch, hold and channel again. Right fingers must keep the exact fitted
  grasp; the free hand retains its calm wrist/finger follow-through.
- [ ] Alternate Arc Bolt arms after throwing, then return to the fan. Casting-arm
  reactions stay subdued and recover without sticking or accumulating pose offsets.
- [ ] Sprint/turn/stop, jump/land, dash and use Open Circuit with held and free hands.
  Try the Arm life toggle. No stuck bends after disabling the option, death or respawn.
- [ ] If multiplayer is available, watch a remote Saint moving and being repositioned.
  Arm motion should follow interpolated travel; a large position snap should clear
  old spring energy rather than fling the arms. Real networking remains untested.

Build/access and arm-life, spear-mode and animation-rule checks pass. New native
whole-pipeline validation: **39,514 assertions / 144 cases** with actual motion,
arm-life, carry and aiming components on bundle11. It verifies held finger/socket
protection, free-finger fade, correct casting arm, all-bone restoration and cleanup.
Existing 700 VFX, 96 spear and 192 movement cases also pass. The native
`artifacts/arm-flow01/free-hand.gif` study includes procedural arms and torso;
game-state inputs are explicit adapters. No gameplay/skin-weight collision/network proof.
v0.7.7 has not been launched in game.

Rollback to v0.7.6: with the game closed, restore BOTH DLL and bundle11 from
`artifacts/foundation/profile-backup-20260930-081920` into the Dev plugin folder.

# v0.7.6 playtest: movement follow-through and dash handoff

Launch **Hollow Saint Dev** and confirm **v0.7.6 | movement flow | dash handoff**.
The spear aiming, skin/VFX pass and bundle11 are retained. Controls and balance are unchanged.

- [ ] Start, stop, reverse and strafe in every direction. The torso should gently
  react to acceleration and settle; head counterbalance keeps the gaze calmer.
  Foot placement should remain consistent with the existing locomotion.
- [ ] Turn sharply while walking/running, then stop. Check that the small roll
  follows through softly without a persistent sideways lean or repeated wobble.
- [ ] Repeat while channeling the fan, firing Arc Bolt, recalling/throwing and
  using Open Circuit. Torso reactions should be quieter during skill gestures;
  the fitted fingers and spear grip stay stable.
- [ ] Sprint-glide into Arc Step: the additive glide posture hands back over about
  80 ms instead of disappearing immediately. Check jump/fall and quick changes of direction.
- [ ] Jump out of a dash while rising. It should select ascent on exit rather than
  briefly showing the falling loop. Compare an apex/falling exit and a grounded exit.
- [ ] Check death, stage changes and interrupted motion for stuck posture. Busy
  fights, multiplayer interpolation and whole-body spear clearances still need playtesting.

Release build/access check pass. Native movement validation: **195,431 assertions
across 192 cases** (eight directions, ground/air/glide, casting/free, 15/30/60/144 Hz).
Ground feet/root/pelvis remain unchanged, finger locals stay fitted, additive poses
restore without drift, and interruption/invalid-motion cleanup passes. The native
700 VFX and 96 spear cases also pass; animation rules, locomotion and arm-life
checks were rerun. `artifacts/motion-flow01/{turn-stop,glide-dash}.gif` are native
pose studies with explicit state inputs, not game footage. v0.7.6 was not game-tested.

Rollback to v0.7.5: with the game closed, restore BOTH DLL and bundle11 from
`artifacts/foundation/profile-backup-20260930-072556` into the Dev plugin folder.
Use the retained v0.7.5 spear and v0.7.4 skin/VFX checklists below too.

# v0.7.5 playtest: spear aiming and recovery flow

Launch **Hollow Saint Dev** and confirm **v0.7.5 | spear flow | aim recovery**.
The v0.7.4 lightning/skin pass and bundle11 are retained. Controls and damage are unchanged.

- [ ] Aim the fan up/down, turn, strafe, sprint and jump while channeling. The arm
  should raise smoothly and keep the spear fixed in the fitted grip.
- [ ] Release primary while aiming high or low: the spear arm should ease back into
  its authored carry pose over 0.25 s, without an immediate aiming snap.
- [ ] Restart primary during that recovery. It should resume from the current arm
  pose. Try fan -> throw rapidly; the new throw should pick up the prior arm offset.
- [ ] Throw while moving/airborne: the spear aligns with aim at the original release
  marker, then the arm follows through freely. Check recall/catch and Open Circuit
  against the existing fitted-grip and all-skin VFX checks below.

Release build/access check pass. Native carry validation: 21,344 assertions across
96 movement/facing/aim cases; existing 700 VFX cases also pass. Spear modes, arm life
and animation rules were rerun and pass. Motion studies: `artifacts/spear-flow01/standing.gif`
and `running.gif`; these use native Unity poses but do not simulate flight or gameplay.
The actual game startup loaded v0.7.5 and passed kit/catalog/skin checks. One existing
Hidden/ProBuilder/EdgePicker shader lookup error also occurs in the preserved earlier
startup log; this is not a clean whole-game log. No gameplay/input/network test was done.

Rollback to v0.7.4: with the game closed, restore BOTH DLL and bundle11 from
`artifacts/foundation/profile-backup-20260930-065938` into the Dev plugin folder.
Use this section together with the v0.7.4 skin/VFX checklist below.

# v0.7.4 playtest: connected lightning rhythm and skin colors

Launch **Hollow Saint Dev** and confirm **v0.7.4 | VFX flow | skin colors**.
This uses the existing bundle11 animations and fitted spear. Damage, balance and
spear controls are unchanged from v0.7.3.

- [ ] Check Default and Obsidian (cyan), Verdigris (green), Solar (amber) and
  Umbral (violet), in dark and bright areas. Body current, spear, fan, crown,
  release connections and enemy effects should keep their theme without washing
  the hand or spear tip into a solid white patch.
- [ ] Feel the shared pulse move from core/ring down the rear arm, around the
  elbow, under the forearm and into the hand/spear. Crackles should reinforce
  the same rhythm. Current stays off at idle; the held spear retains its quiet glow.
- [ ] Fire Arc Bolt and throw the spear while moving: the brief release connection
  should follow the hand, then break as the projectile continues. Recall should
  finish at the moving catch hand without a floating or duplicate spear.
- [ ] Hold/release the fan repeatedly. Its light eases in over about 60 ms and
  fades over about 90 ms; damage stops immediately when channeling ends.
- [ ] Walk, sprint, turn, reverse, strafe both ways, jump and land while using
  fan -> throw -> Arc Bolt -> recall -> fan. Repeat with Open Circuit and Arc Step;
  try Thunderbolt during movement. Look for pose snaps, disconnected current,
  stale trails, abrupt brightness changes or effects covering the grip.
- [ ] Interrupt skills, die, change stage and switch skins. No lingering body
  current, fan or dash afterimages. Reused projectile effects must keep their owner color.
- [ ] If multiplayer is available, use two Saints with different skins. Static
  and mark effects remember their own latest contributing Saint's color; neither
  should overwrite the other's color. Check return/catch timing and remote cleanup.
- [ ] Check busy fights for bloom, readability and performance. Keep
  `BepInEx/LogOutput.log` and note the skin, skill and movement when something looks wrong.

Release build: 0 errors (existing NU1701 warning), access check and all nine offline
checks pass. Native Unity validation passes 15,629 assertions across 700 posed
skin/movement/ability/phase cases, including fading and cleanup. Its game-state
adapters and additive preview shader do not test actual game bloom or networking.
Imported animation/layer checks pass with animation culling disabled. The animated
grip clears all 27 hand parts (zero intersections, 375 fixed-finger checks,
1.209 mm minimum sampled vertex clearance). Actual game feel remains unverified.

Rollback to v0.7.3: with the game closed, restore BOTH DLL and bundle11 from
`artifacts/foundation/profile-backup-20260929-225855` into the Dev plugin folder.
Older checklists below are historical; this version's VFX checks take precedence.

# v0.7.3 playtest: fitted held spear and lightning fan

Launch **Hollow Saint Dev** and confirm **v0.7.3 | held-spear | lightning-fan**.
The DLL and bundle11 are a pair; restore both together for rollback.

- [ ] Spawn with the fitted spear in the right hand. At rest, the spear keeps its
  quiet silhouette and the body has no coursing current. Check the molded grip from
  behind and the side while walking, sprinting, jumping and landing.
- [ ] Hold primary: raise the spear, channel the crackling fan from its tip, and
  settle back into carry on release. Check up/down aim, turning and Open Circuit.
  Current should follow core/ring -> rear upper arm -> elbow -> underside -> hand ->
  grip -> shaft -> head. Check the thicker moving pulse and trailing free hand.
- [ ] Fan starting defaults: 8 m, 60-degree full cone, 5 distinct visible enemies,
  120% damage and 35% Static per second at base attack speed, 0.1 item proc per tick.
  These are first-playtest proposals, configurable under **2. Conduit Spear**.
  Compare Static/crowd control with the harder ranged Arc Bolt. No through-wall hits.
- [ ] Press secondary to throw: fingers release as the spear leaves; primary becomes
  the existing ranged Arc Bolt. Check throw while running/jumping and after a fan.
- [ ] While it is flying/planted/stuck, press secondary once to recall and catch it,
  including at zero secondary stock. It stays held. Recall must preserve stock and
  recharge progress. A separate press with a ready charge throws it again.
- [ ] Try primary -> throw -> primary -> recall -> fan rapidly. No duplicate spears,
  floating grip, stuck cone, repeated release flash or forced rethrow. Extra secondary
  charges still enlarge the single planted spear; the fitted held grip stays its size.
- [ ] Interrupt before/after throw release, interrupt the fan, die, change stage and
  switch skins. No persistent current/cone; a pre-release interrupted throw refunds
  its charge while alive. Check busy fights for VFX readability and performance.
- [ ] If multiplayer is available, check two Saints with different spear states/skins,
  free recall at zero stock, owner damage authority and the moving return/catch.

Release build: 0 errors, existing NU1701 warning. All nine offline checks, the real
game access check, and Unity carry/clip/layer checks pass. The final built bundle's
grip clears all 27 imported hand pieces (zero intersections, about 1.25 mm nearest
sampled vertex clearance); all 375 fixed-finger samples pass. Gameplay, network latency,
all-body weapon clearances and VFX readability/performance need this playtest.
The animated review in `art/anim/v37/spear-flow-standing-running.gif` is a Blender
study, not game footage; its flight timing and lightning are illustrative.
Keep the Dev profile's `BepInEx/LogOutput.log` after testing.

Rollback to v0.7.2: with the game closed, restore BOTH DLL and bundle from
`artifacts/foundation/profile-backup-20260929-214127` into the Dev plugin folder.
Older playtest checklists below are historical; the v0.7.3 controls take precedence.

# v0.7.2 playtest: rear-arm current route

Launch **Hollow Saint Dev** and confirm **v0.7.2 | rear-arm | elbow-flow**.
Bundle09 replaces only Arc Bolt left/right; v0.7.0 balance and other clips remain.

- [ ] Idle: no coursing lightning or idle electrical crackles. Charge orbs can remain.
- [ ] Fire single/alternating/rapid Arc Bolts: core/shoulder and ring feel connected;
  from directly behind, current runs down the BACK of the upper arm, wraps around
  the elbow and follows the UNDERSIDE of the forearm into the hand. Check it through
  the whole throw from rear and side views, with a thick release pulse.
  The brief outgoing connection breaks; the bolt continues freely.
- [ ] Fire standing, running, jumping, landing and while Open Circuit is active.
  Report any stuck current, pose snap or unreadable hand release. Running torso lean
  still lowers the hand; aim/gesture blending is pending.
- [ ] Spear throw/recall, Arc Step and Thunderbolt: feel powered by the same core/ring.
  Open Circuit should connect to both arms while keeping its existing crown effects.
- [ ] Interrupt a cast, die, change stage and switch skins between runs: no lingering
  lines; colors match the skin. Check effect readability in busy fights and performance.
- [ ] If multiplayer is available, compare both Saints' colors/current and interrupted casts.

Release build, route geometry/timing checks and access check pass. Prior Unity
imported-clip/layer checks still apply to the retained bundle09. Actual game visuals
and performance are unverified. Leave `BepInEx/LogOutput.log` available after testing.
Rollback to v0.7.1 restores the DLL from `artifacts/foundation/profile-backup-20260929-185446`
and keeps bundle09. Rollback to v0.7.0 restores BOTH DLL and bundle from
`artifacts/foundation/profile-backup-20260929-182237`, with the game closed.

Also test the v0.7.0 balance/features below; their defaults remain current.

# v0.7.0 playtest: balance, spear stacks, halo and arm life

Launch **Hollow Saint Dev** and confirm
**v0.7.0 | balance-spear | circuit-armlife**. Bundle07 retains the v34 Arc Bolt
animation. The proposed v35 palm push is awaiting review on its own branch.

- [ ] Early packs: judge whether Electrocute cascades and spear spread feel
  controlled while the direct Arc Bolt hit still feels effective.
- [ ] Check Mod Options: untouched pop defaults should be 150% damage, 2 targets,
  0.3 proc and 15% Static; spread defaults should be 35% damage, 2 targets and
  0.3 proc. Existing custom settings should remain. Skill text should match.
- [ ] With no Backup Magazines, plant/recall/rethrow and shoot the spear. Add
  extra secondary charges: still one spear, but its size, radius and spread
  target count grow within caps. Check growth while the spear is already planted.
- [ ] Place a wall between the spear and enemies: pulses/spread should respect
  line of sight; nearby visible enemies should still be hit.
- [ ] Build charge and cast Open Circuit standing, moving and airborne: orbs
  follow the copper halo as it becomes a crown, arcs bridge the halo gaps, and
  tendrils originate at the ring. Check skin colours and cleanup after it ends.
- [ ] Sprint, turn, reverse, stop, jump and land: arm reactions settle smoothly
  without runaway wobble. Fire while moving: the casting arm stays readable.
- [ ] Try the Arm life toggle and individual reaction controls in `0. Movement`.
- [ ] Sprint into Arc Step: sprint should continue through the dash.
- [ ] If multiplayer is available, verify separate spear ownership, charge
  growth, halo motion and skin colours for two Saints.

Report which feature feels wrong and the circumstances. Leave the Dev profile's
`BepInEx/LogOutput.log` available. This build has not been run in game by the agent.
Rollback: use the DLL in `artifacts/foundation/profile-backup-20260929-172936`
with the game closed and keep bundle07.

The checklists below describe older builds; v0.7.0's defaults take precedence.

# v0.6.3 playtest: shootable spear and v34 arms

Launch **Hollow Saint Dev**. Confirm **v0.6.3 | shoot-spear | arms-v34** (bundle07).
Not launched before handoff.

## Arms (v34)

- [ ] Standing idle and combat idle: arms out at the sides, open hands, no curled fists at the hips.
- [ ] Hold Arc Bolt: each shot is a quick hurl that returns to the out-at-sides pose; consecutive shots flow, no wind-up hitch.
- [ ] Cast while running: legs and torso keep running, only the arms throw.
- [ ] Walk, run, glide: relaxed open hands with trailing fingers; run arms swing opposite the legs.
- [ ] Combat on/off while standing, idle fidgets every 8-15 s, Run stop from a sprint, Land settle: smooth, no snaps.
- [ ] If breathing/finger motion is too much, lower "Arm life intensity" (0. Movement).

## v0.6.0 checklist: Conduit Spear anchor

Launch **Hollow Saint Dev**. Confirm **v0.6.0 | spear-anchor | recall-throw**.
Bundle05 is unchanged. This build was not launched before handoff; check the log for
`HOLLOW_SAINT_EVENT SPEAR_` lines and any Conduit Spear warnings.

## Plant and pulse

- [ ] Throw at flat ground: the lance stays visible, leaning into the ground, in your skin colours.
- [ ] Throw at a wall, a steep slope and a ceiling: it should drop to the ground below, not float.
- [ ] Throw into the sky: it should land about 80 m out (or vanish if there is no ground).
- [ ] Every 1.5 s enemies within 10 m take a small hit and build Static; a faint ring shows the radius. Audio should stay quiet over a long fight.

## Stick in an enemy

- [ ] Spear an enemy: 450% hit plus Conductor Mark, then the spear rides that enemy and pulses around it.
- [ ] Kill the host: the spear drops to the ground under it and keeps pulsing.
- [ ] Spear-kill an enemy outright: it plants on the ground under the kill.

## Arc Bolt spread

- [ ] Arc Bolt an enemy inside the radius: arcs go hit -> spear -> up to 3 other enemies in the radius, visibly thicker than the pulse arcs.
- [ ] The bolt's normal chain still happens and does not re-hit the spread targets.
- [ ] Bolts on enemies outside the radius behave as before (no spread).
- [ ] Shootable spear: shoot the planted spear itself (aim at or just beside the shaft, up to about 1.6 m, config "Spear hit radius"). The bolt pops on the spear with a white-hot flash at its top, a crackle along the shaft, a small fast ground ring, a short zap, and the lance swells briefly. It then arcs to up to 3 enemies in the radius; with nobody nearby you still get the flash and zap. Rapid fire should not stack the zap into noise (max 6/s).
- [ ] Shooting a spear that rides an enemy counts as hitting that enemy (normal bolt, chain and spread).
- [ ] Arc Bolt on an enemy in the radius with no other enemies nearby: a thin hit -> spear link and small spear flare still show.
- [ ] Watch for bolts aimed past the spear at a far enemy being eaten by the spear; if it happens too often, lower "Spear hit radius".

## Recall and rethrow

- [ ] After the cooldown, recast: the spear flies back to your hand (about 0.3 s), then the throw goes to the new aim point. Only one spear exists.
- [ ] First cast with no spear out throws immediately.
- [ ] Die, run more than 100 m away, and change stage: the spear should disappear each time and the next cast throws immediately.

## Multiplayer (if available)

- [ ] Two Saints: each spear only spreads its own owner's bolts; client recast shows the recall.
- [ ] A client sees planted spears, pulses and spread arcs in the owner's palette.

# v0.5.2 playtest: sprint silhouette and skin lightning

Launch **Hollow Saint Dev**. Confirm **v0.5.2 | sprint-silhouette | skin-lightning**.
Bundle05 is unchanged. Build/startup checks do not replace this visual playtest.

## Rear-view sprint

- [ ] Hold full sprint with the camera directly behind, then move it to either side. Judge the knee bend, separation of the feet, relaxed arms and forward drive together.
- [ ] Sprint straight, turn, reverse, stop and restart. The pose should blend rather than snap; the short heel plumes should follow the feet without long parallel rails.
- [ ] Jump, land, dash and sprint again. Watch for stuck limbs, misplaced exhaust or lingering jets after death.
- [ ] Recheck slow walking and strafing. The previous speed-matched cadence should be retained.

## Skin effects

- [ ] Compare default/Obsidian cyan, Verdigris green, Solar gold and Umbral purple.
- [ ] Earn charge orbs and trigger Answered Prayer. Check orb edges, halo crackle, gather/release and the strike.
- [ ] Check Arc Bolt, chain hops, spear, Open Circuit, dash and heel thrust. Bright cores should remain readable while outer color follows the equipped skin.
- [ ] Switch skins between runs. If possible, play with two Saints using different skins; effects should retain their owner's palette.

## Feedback

Most useful: a rear-view full-sprint screenshot, whether the feet/legs look natural now, and which effect still looks cyan on Umbral or becomes hard to see.

Movement speed, damage, cooldowns, audio and bundle05 are unchanged. This pass does not add terrain foot IK. Leave the Dev profile's BepInEx/LogOutput.log available after testing.

## Rollback

The staging script saves the replaced DLL in artifacts/foundation/profile-backup-*.
Use the v0.5.1 backup named in docs/NEXT-SESSION.md; keep bundle05. Close the game before restoring it. Only modify Hollow Saint Dev.
