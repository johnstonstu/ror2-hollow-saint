# Manual Gaze 1.2.0 candidate: mechanics and later playtest

Status: source-reviewed candidate; the checks below are **pending native gameplay verification**. This document records no installation, game launch, controller acceptance, multiplayer acceptance or publication.

## Controls and timing

- Press the mapped **Special** action once to start Gaze, consuming one normal Special stock. The ordinary beam starts after the windup and remains automatic.
- Release, then tap Special again to request **one entry orb per accepted tap**. Holding never repeats. The activation hold is not a fuel tap, and later taps never consume another Special stock, even with extra stocks available.
- Taps during windup, with no available entry fuel, too close together, or too late are rejected. Accepted taps are at least **0.25 seconds** apart; rejected taps are not deferred into an automatic burst.
- An accepted tap starts **0.32 seconds of intake**, followed by **0.35–0.55 seconds of travel**, then up to **0.30 seconds of ground spread**. Aim is sampled at launch, after intake.
- Admission requires **more than 1.22 seconds remaining** before beam end: intake + maximum travel + spread + 0.05 seconds safety. Equality is rejected. A delayed intake is checked again before spending, so a fixed-step hitch cannot spend fuel into an impossible arrival.
- **Arc Step is the only deliberate early-cancel action.** Repressing Special fires a fuel request; the former physical B/Circle shortcut no longer cancels. A button bound to Utility still invokes Arc Step normally. Natural timeout, interruption, death and disable also end the cast.
- Cancellation and natural end stop pending work. A launched pulse cancelled before impact has still spent its orb; there is no refund for a miss or an interrupted arrival.

Sources: [GazeState](../HollowSaintMod/FoundationKit/Gaze/GazeState.cs), [request and duration policies](../HollowSaintMod/FoundationKit/Gaze/GazeManualInputPolicy.cs), [fuel controller](../HollowSaintMod/FoundationKit/Gaze/GazeFuelController.cs), [schedule](../HollowSaintMod/FoundationKit/Gaze/GazeFuelLedger.cs).

## Fuel and duration

**Entry fuel** is the existing passive bank claimed when Gaze starts. Accepted intake reserves availability, but the orb is **spent only at launch**. New gains during the cast go into a separate **reserve** and cannot feed that cast's manual shots. Living exits return unlaunched entry fuel plus reserve; death/disable clears them. Returning a full bank must not automatically fire an end-of-cast Thunderbolt. Pulse visuals do not authorize damage or spend resources; the server does.

Beam duration is frozen at cast entry:

```text
min(6, clamp(configuredBaseSeconds, 1, 6) + 0.10 * clamp(level - 1, 0, 20))
```

With the default base, this is **4 seconds at level 1**, **5 at level 11**, and **6 at level 21 and above**, after the windup. A configured base above six seconds is capped. Mid-cast level changes do not extend the current beam. Level scaling grants time, not free entry orbs or extra Special stocks.

Longer duration also means more ordinary-beam exposure, longer hover/armor uptime, and a later cooldown start. Duration and manual-tap cadence do not scale with attack speed; the ordinary beam retains its existing attack-speed tick behavior without a second duration multiplier.

## Presentation to inspect

The ordinary beam should read as a **thin baseline**, with a **fatter travelling pulse** for each launched orb. Intake should move an entry orb into the crown before that pulse travels out. Confirmed enemy connectors should agree with actual hits; the expanding ground pattern is not blanket area damage.

The physical crown arcs expand **radially**, preserving their thickness, and return to their original pose on every exit. Open Circuit has a new lightning dome to inspect separately: it must follow its active state and clear when the effect ends. These are candidate visual expectations, not approval of the in-game result.

## Later solo playtest

Record the candidate commit/DLL hash, game build, level, config, input device/bindings, entry fuel and Special stocks for each case. Use the normal gameplay camera and visible HUD; mark a row passed only after observing the expected result. Controller rows require a physical controller session.

| Case | Expected observation | Status |
|---|---|---|
| Cast and hold | One Special stock consumed; ordinary beam begins after windup; no fueled pulse from the initial hold. | Pending |
| Release and tap | Each accepted later edge takes one entry orb through intake, launch, travel and arrival; holding that tap adds no repeats. | Pending |
| Mash / extra stocks | Closely spaced taps do not bypass the 0.25 s gate; no stock loss, restart or delayed burst from rejected taps. | Pending |
| Windup / empty entry | Windup taps do not queue; zero entry fuel allows the ordinary beam but no fueled shots. Reserve earned later stays unavailable for shots. | Pending |
| Intake cancellation | Arc Step before launch returns the unlaunched orb on a living exit; no pulse damage follows. | Pending |
| In-flight cancellation | Arc Step after launch removes pending arrivals without refunding that spent orb. Repeat at a near-arrival boundary. | Pending |
| Late admission | Taps with 1.22 s or less remaining do not intake or spend. Earlier accepted pulses finish before natural end; no post-end pulse damage. | Pending |
| Reserve / full merge | New gains remain separate; living exit returns the expected bank, with no automatic end-of-cast Thunderbolt. | Pending |
| Duration bounds | Default levels 1/11/21 give 4/5/6 s of beam; higher levels and configured bases above 6 stay capped. Level-up during a cast does not extend it. | Pending |
| Attack speed | Faster ordinary-beam ticks do not shorten manual admission spacing or extend duration. | Pending |
| Aim and terrain | Steer during intake; verify launch aim, near/far travel, ground arrival, moving targets, walls, slopes and ledges. No invented ground strikes over voids. | Pending |
| Input parity | Keyboard and physical controller both honor mapped Special edges and Utility cancellation. Physical B/Circle alone has no old cancellation behavior. | Pending |
| Visual hierarchy | Thin baseline and fat pulse remain distinguishable with HUD visible, against bright/dark terrain and across skins. | Pending |
| Crown restoration | Natural end, Arc Step, interruption, death and repeated casts restore physical crown geometry without drift or lingering expansion. | Pending |
| Open Circuit dome | Dome appears only during its active effect, follows the character, respects the intended visual radius and clears on end/death/disable. | Pending |
| Cleanup | Interruption, death and stage transition leave no orb, pulse, crown or dome remnants; subsequent casts behave normally. | Pending |

## Native red-skin mastery

Crimson Vow is appended at index 5; the original five skin indices stay unchanged. Its shared unlock is `Skins.HollowSaint.Mastery`. The achievement inherits the installed game's native `BasePerSurvivorClearGameMonsoonAchievement`: Hollow Saint's body requirement, a native winning ending, and a difficulty marked `countsAsHardMode`. This includes ordinary Monsoon wins and obliteration according to the native ending policy, plus native qualifying modded difficulties. It awards the usual ten lunar coins. Earlier clears are not replayed for this newly registered achievement.

**Native acceptance pending.** Confirm achievement/catalog discovery, intended locked state, localized text, body and display skin agreement, every available qualifying ending and persistence through a normal restart. GPU atlas recoloring, optional-material fallback, shader appearance and multiplayer red/gold palettes remain untested in the game. No unlock was granted and no save was edited during preparation.

## Multiplayer observers

**Pending; solo success does not verify networking.** A later authorized host/client session must check owner-only tap admission, duplicate/stale request rejection, cast IDs, reserve/entry agreement, delayed packets near the admission cutoff, cancellation ordering, and host/observer agreement on intake, launch, strike and cleanup. Include late join and an observer watching a different player's cast. Input feedback currently waits for server acknowledgement, so latency may change perceived tap response.
