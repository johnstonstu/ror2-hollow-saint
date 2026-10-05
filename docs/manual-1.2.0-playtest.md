# Manual Gaze 1.2.0 candidate: mechanics and later playtest

Status: source-reviewed candidate; the checks below are **pending native gameplay verification**. This document records no installation, game launch, controller acceptance, multiplayer acceptance or publication.

## Controls and timing

- Press the mapped **Special** action once to start Gaze, consuming one normal Special stock. The ordinary beam starts after the windup and remains automatic.
- Press the mapped **Primary** action to request **one entry orb per accepted press**. If Primary was held on entry, release it first. Holding never repeats, and pulse presses never consume another Special stock, even with extra stocks available. Normal Primary shots do not fire alongside pulses.
- During the channel, Primary uses a contextual pulse icon and shows acknowledged available entry fuel; queued intake and reserve are excluded. Secondary, Utility and Special are unavailable. Movement and camera aiming remain active. Native overrides restore the original skills, stock and cooldown progress on every exit.
- Known kit base skills keep their normal recharge progress, with Gaze's Special cooldown held until its channel ends and Stormspear's prerelease pause retained. An unknown custom base skill or a preexisting contextual override from another mod retains its saved stock and timer rather than running that mod's callbacks against Gaze's temporary data. Known kit skills retain their original instance data; unknown custom skills keep the engine's freshly assigned data so disposed state is not revived. Higher-priority external overrides are not removed or rolled back; mod combinations require separate acceptance.
- An already committed spear throw finishes normally. An unthrown charge is interrupted and refunded before its slot is overridden; the native Secondary bank stays active until that handoff completes, with further Secondary input claimed.
- Taps during windup, with no available entry fuel, too close together, or too late are rejected. Accepted taps are at least **0.25 seconds** apart; rejected taps are not deferred into an automatic burst.
- An accepted tap starts **0.32 seconds of intake**, followed by **0.35–0.55 seconds of travel**, then up to **0.30 seconds of ground spread**. Aim is sampled at launch, after intake.
- Admission requires intake plus 0.05 seconds safety to finish before the **current** beam end (more than 0.37 seconds remaining). Maximum travel, spread and safety must fit within the end including only this pulse's prospective extension. Pending intakes reserve extension headroom but do not provide unearned time. At the 14-second cap, admission again requires more than 1.22 seconds remaining. Equality is rejected. A delayed intake is checked again before spending; it cannot revive an expired cast.
- **There is no manual cancel binding.** The channel ends naturally or through a real interruption, death or disable. Utility is unavailable, so Arc Step is not a channel cancel; neither Special nor a hidden physical B/Circle shortcut ends it.
- Cancellation and natural end stop pending work. A launched pulse cancelled before impact has still spent its orb; there is no refund for a miss or an interrupted arrival.

Sources: [GazeState](../HollowSaintMod/FoundationKit/Gaze/GazeState.cs), [request and duration policies](../HollowSaintMod/FoundationKit/Gaze/GazeManualInputPolicy.cs), [fuel controller](../HollowSaintMod/FoundationKit/Gaze/GazeFuelController.cs), [schedule](../HollowSaintMod/FoundationKit/Gaze/GazeFuelLedger.cs).

## Fuel and duration

Answered Prayer now stores **Static Charges** with no automatic passive discharge, including full banks returned from Gaze. The configured capacity remains unchanged (default five, allowed 2–20). A successful server-created spear throw with a full bank spends it and snapshots one landing Thunderbolt. Partial banks stay intact. A failed throw before projectile creation spends no bank; an empowered miss, owner death or scene change grants no refund and cannot resurrect the bonus. A funded Crown spear suppresses the old separate free Crown bonus, while an unfunded Crown spear retains its existing behavior. Spear, burst and conductor mechanics persist; their damage now follows the separate 10% non-Gaze feedback pass documented in solo-feedback-1.2.0-playtest.md.

The local HUD timer appears after Gaze ignition and uses the actual acknowledged remaining time. Its fixed fourteen-second scale makes a successful +2-second launch visibly lengthen the bar. It hides on exit, death, disable, stage/body replacement and when the native HUD is hidden; observers have no meter for another player's body.

**Entry fuel** is the existing passive bank claimed when Gaze starts. Accepted intake reserves availability, but the orb is **spent only at launch**. New gains during the cast go into a separate **reserve** and cannot feed that cast's manual shots. Living exits return unlaunched entry fuel plus reserve; death/disable clears them. Returning a full bank must not automatically fire an end-of-cast Thunderbolt. Pulse visuals do not authorize damage or spend resources; the server does.

The baseline duration is frozen at cast entry:

```text
min(6, clamp(configuredBaseSeconds, 1, 6) + 0.10 * clamp(level - 1, 0, 20))
```

With the default base, this is **4 seconds at level 1**, **5 at level 11**, and **6 at level 21 and above**, after the windup. A configured base above six seconds is capped. Mid-cast level changes do not extend the current beam. Level scaling grants time, not free entry orbs or extra Special stocks.

Each **successfully launched** entry-fuel pulse adds **2 seconds**, clamped to **14 seconds total beam time**. A final grant can be smaller when it reaches the cap. Four seconds reaches fourteen after five launches; six seconds reaches fourteen after four. Further entry-fuel pulses can still fire when arrival fits, but grant no time. Rejected taps, cancelled intakes and new reserve gains grant no time. The fourteen-second cap is the starting private playtest tuning, not a final balance commitment.

**Bounded launch ramp:** each successfully launched entry pulse adds one step, up to five per cast. Ordinary core, impact splash, automatic fork and chain damage gain 5% per step, up to +25%; the fueled pulse coefficient, spear/conductor damage, proc coefficients, tick cadence, targeting, range and radii stay unchanged. The existing reliable Launch packet carries the absolute spent count after server spending, so duplicates add no step. Rejected requests, cancelled intakes and reserve gains add none. Damage applies at successful launch; presentation eases to the wider settled beam beneath the travelling burst. New casts reset to zero. Additional launches beyond five can still spend fuel but grant no further ramp.

Reach and ground-spread progression use the frozen baseline, so extending the channel cannot retract the beam or shrink later pulses. Extended time stays at the existing maximum reach. No movement root, defense buff or invulnerability was added.

Longer duration also means more ordinary-beam exposure, longer hover/armor uptime, and a later cooldown start. Duration and manual-tap cadence do not scale with attack speed; the ordinary beam retains its existing attack-speed tick behavior without a second duration multiplier.

## Presentation to inspect

The ordinary beam starts focused (0.65 m body) and settles wider after confirmed launches, gaining 0.40 m per step up to 2.65 m after five launches. Haze, sheath and core widen modestly with it; final animated line widths are clamped to the configured hit diameter, including the minimum-radius setting. The beam stays distinct from a **fatter travelling pulse** (2.95 m sleeve before envelope/diameter caps) for each launched charge. Static Charges appear as compact irregular lightning clusters, keeping the primary skin energy through crown intake and travel, with fine complementary filaments. Pulses add two narrow branching forks (one with reduced effects). Ground strokes lash outward with varied paths and a fading wake rather than a continuously lit radial star. Confirmed enemy connectors should agree with actual hits; decorative lashes stay within the existing footprint and do not authorize damage.

All six palettes retain their primary identity and add sparse secondary color: cyan/lilac for Default and Obsidian, mint/blue for Verdigris, gold/violet for Solar, violet/ice cyan for Umbral, and crimson/amber for Crimson Vow. Shared forks and sparks apply that layering to Arc Bolt, Stormspear/conductors, Arc Step, Open Circuit and Thunderbolt. Native chain and Thunderbolt variants now include the sixth Crimson slot. Inspect contrast, bloom, crowded fights and reduced effects before accepting the appearance.

The physical crown arcs expand **radially**, preserving their thickness, and return to their original pose on every exit. Open Circuit now moves its four real metal crown elements to the equator of the core-centred damage sphere, with sparse lightning between them. Its previous dome is replaced. The pose must yield to Gaze and restore on end/death/disable/model replacement. Bone docks are radius-bounded; native metal mesh extents remain unmeasured. These are candidate visual expectations, not approval of the in-game result.

Arc Bolt's main impact uses a dedicated copy of the shorter ChainHop crackle at 0.82 PCM gain, keeping the 0.22-second global impact gap and no extra impact accent. Other bank media and routing remain byte-identical. Each deduplicated server-confirmed Gaze Launch uses the existing 0.24-second electrical discharge cue, with clustered acknowledgements coalesced. Original Gaze startup, charge/hum/crackle loops and end are preserved. The timer now uses a compact charcoal panel with separated seconds, two-second meter divisions and a brief actual-extension cue. See solo-feedback-1.2.0-playtest.md for exact balance and audio details; native acceptance remains pending.

**Near-expiry multiplayer limitation:** duration is earned on the server and acknowledged by the existing reliable Launch packet. A remote owner can reach its old local deadline before a very late grant arrives and exit after the server spent the orb. This prototype retains the existing owner end policy; it has no speculative client extension. Test late pulses under latency before multiplayer acceptance.

## Later solo playtest

Record the candidate commit/DLL hash, game build, level, config, input device/bindings, entry fuel and Special stocks for each case. Use the normal gameplay camera and visible HUD; mark a row passed only after observing the expected result. Controller rows require a physical controller session.

| Case | Expected observation | Status |
|---|---|---|
| Cast and hold | One Special stock consumed; ordinary beam begins after windup; no fueled pulse from the initial hold. | Pending |
| Release and tap | Each accepted later edge takes one entry orb through intake, launch, travel and arrival; holding that tap adds no repeats. | Pending |
| Mash / extra stocks | Closely spaced taps do not bypass the 0.25 s gate; no stock loss, restart or delayed burst from rejected taps. | Pending |
| Windup / empty entry | Press Primary during windup and keep holding through ignition: no pulse. Release, then press again: one pulse. Presses while rate-limited or without entry fuel must likewise require a later release/press rather than firing when readiness returns. Zero entry allows the ordinary beam; reserve earned later stays unavailable for shots. | Pending |
| Intake interruption | A real living interruption before launch returns the unlaunched orb; no pulse damage follows. | Pending |
| In-flight interruption | An interruption after launch removes pending arrivals without refunding that spent orb. Repeat at a near-arrival boundary. | Pending |
| Late admission | With extension headroom, a tap with more than 0.37 s remaining can launch and earn its own extra time. Less time rejects before intake; hitches beyond actual expiry never revive it. At the cap, more than 1.22 s is required. Pending intakes cannot borrow each other's grants. | Pending |
| Reserve / full merge | New gains remain separate; living exit returns the expected bank, with no automatic end-of-cast Thunderbolt. | Pending |
| Duration bounds | Default levels 1/11/21 give 4/5/6 s of beam; higher levels and configured bases above 6 stay capped. Level-up during a cast does not extend it. | Pending |
| Pulse extensions | Each successful launch adds 2 s up to 14 s total. Check 4 s + five launches and 6 s + four launches. Failed taps and cancelled intakes add zero. Reach never retracts at launch; each launch also raises ordinary beam damage by 5%, up to +25%, producing more total output and later cooldown. | Pending |
| Launch ramp | Verify zero fuel starts narrow at base damage; launches 1-5 settle wider with 5/10/15/20/25% ordinary damage gain. Sixth and later launches add no ramp. Failed taps, cancelled intakes and duplicate acknowledgements add none; a new cast starts narrow at base damage. At the minimum configured radius, animated baseline layers stay inside the hit diameter. Fueled pulse damage stays unchanged. | Pending |
| Stored Prayer | With a full bank and enemies nearby, wait without using an ability: no automatic strike or spend. Repeat after a full Gaze reserve merge. | Pending |
| Full / partial spear | Full configured banks of 2/5/6/20 are claimed once at successful throw and produce one Thunderbolt at qualifying enemy/terrain impact. Partial banks remain. Failed throws before spawn preserve the bank. | Pending |
| Crown / misses | A funded Crown spear delivers one Thunderbolt, without the old bonus duplicating it. An unfunded Crown retains its ordinary feature. Test lifetime miss, friendly collision, owner death and stage transition: no delayed bonus or refund. | Pending |
| Timer / pulse cue | Only the local player sees actual remaining seconds; fired pulses add two seconds and one electrical cue; clustered delayed acknowledgements coalesce. Rejected taps, cancelled intake and duplicate launch packets add no cue. Native HUD hide, exit, death and body replacement clear it. | Pending |
| Arc impact clarity | At high attack speed and many simultaneous hits, the main impact crackle is quieter and throttled, with restrained flashes and no extra impact accent. Damage remains unchanged. | Pending |
| Attack speed | Faster ordinary-beam ticks do not shorten manual admission spacing or extend duration. | Pending |
| Aim and terrain | Steer during intake; verify launch aim, near/far travel, ground arrival, moving targets, walls, slopes and ledges. No invented ground strikes over voids. | Pending |
| Input parity | Keyboard and physical controller honor mapped Special activation and fresh Primary edges. Holding Primary on entry produces no pulse until release/press. Default controller RB→RT and keyboard R→LMB are examples, not hardcoded bindings. | Pending |
| Native skill overrides | Pulse icon/count agree with available entry fuel; Primary does not also fire Arc Bolt, and Secondary/Utility/Special cannot activate. Originals and stock/cooldowns restore after expiry, interruption, death, disable, stage exit and respawn without free restock. | Pending |
| Spear handoff | Enter Gaze while charging, during prerelease, and during a failed throw. Unthrown/failed shots refund correctly, committed throws finish, and no refund goes into the temporary locked bank. | Pending |
| Visual hierarchy | Thin baseline and fat pulse remain distinguishable with HUD visible, against bright/dark terrain and across skins. | Pending |
| Crown restoration | Natural end, interruption, death and repeated casts restore physical crown geometry without drift or lingering expansion. | Pending |
| Open Circuit perimeter | Metal crown elements expand to the actual damage sphere equator, with sparse arcs. Gaze takes pose ownership cleanly; end/death/disable/model replacement restore the authored crown. | Pending |
| Cleanup | Interruption, death and stage transition leave no orb, pulse, crown or perimeter remnants; subsequent casts behave normally. | Pending |

## Native red-skin mastery

Crimson Vow is appended at index 5; the original five skin indices stay unchanged. Its shared unlock is `Skins.HollowSaint.Mastery`. The achievement inherits the installed game's native `BasePerSurvivorClearGameMonsoonAchievement`: Hollow Saint's body requirement, a native winning ending, and a difficulty marked `countsAsHardMode`. This includes ordinary Monsoon wins and obliteration according to the native ending policy, plus native qualifying modded difficulties. It awards the usual ten lunar coins. Earlier clears are not replayed for this newly registered achievement.

**Native acceptance pending.** Confirm achievement/catalog discovery, intended locked state, localized text, body and display skin agreement, every available qualifying ending and persistence through a normal restart. GPU atlas recoloring, optional-material fallback, shader appearance and multiplayer red/gold palettes remain untested in the game. No unlock was granted and no save was edited during preparation.

## Multiplayer observers

**Pending; solo success does not verify networking.** A later authorized host/client session must check owner-only tap admission, duplicate/stale request rejection, cast IDs, reserve/entry agreement, delayed packets near the admission cutoff, cancellation ordering, and host/observer agreement on intake, launch, strike and cleanup. Include late join and an observer watching a different player's cast. Input feedback currently waits for server acknowledgement, so latency may change perceived tap response.
