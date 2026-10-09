# 1.3 early-game balance, player relay and review media

Completed locally on October 7, 2026. The private **Hollow Saint Dev** profile and
local 1.3.0 package contain the same candidate. No public release, push or media
upload was performed. Earlier recordings and packages remain preserved.

## What changed

- Arc Bolt's default direct damage rises from **108% to 144%**, with its existing
  cadence, aim assistance, range and proc coefficients. Migration 15 changes only
  the exact previous default; custom values remain intact.
- Hollowed Orb remains free with an empty bank. Short casts preserve Static;
  holding beyond 0.5 seconds gathers charges individually. Its surface crackles,
  body feeds, trails and branching impact bursts use the skin's lightning colors,
  with throttled electrical impact sounds.
- Fresh enemies remain the priority. With one or two reachable enemies left, the
  ball can return through the nearby player and continue its finite enemy-hit
  budget. Player contacts cause no damage, proc, healing or Static generation and
  consume no enemy hit. The two-target order is A–B–A–B after fresh targets run out.
- Relay eligibility uses **full 3D foot-to-foot distance within 8 m**, allowing a
  nearby tall boss despite its high core. Actual core travel must independently
  fit the configured bounce range (18 m by default), with world clearance. Walls,
  owner death/replacement, missing targets and the existing lifetime still end it.
- Pending Primary shots now recheck gathering ownership before discharge. This
  closes a timing hole while preserving Primary during Circuit's overhead Orb.
- English, Chinese, Russian and Brazilian Portuguese descriptions and README
  copies explain the new damage, free casts, costs, controls and relay. Historical
  Gaze settings and ordinary/funded spear strikes are distinguished accurately.

Default effective damage includes the existing non-Gaze multiplier exactly once:

| Orb charges | First enemy hit | Total enemy-hit budget | Diameter |
|---:|---:|---:|---:|
| 0 | 157.5% | 2 | 0.6 m |
| 1 | 225% | 3 | 0.6 m |
| 3 | 360% | 5 | 0.8 m |
| 5 | 495% | 7 | 1.0 m |

Each repeat on the same enemy retains 65% of that enemy's previous damage.
First/repeat proc coefficients are 0.5/0.1. Launch range remains 70 m.
Thundercloud's one/three/five-charge radius is 16/23/30 m, dealing 270/495/720%
once per eligible enemy in one rolling sequence. Open Circuit uses at least one
charge, with 1/1.5/2 times pulse density; extra pulses do not accelerate Static
generation. Circuit's Orb gathers overhead while Primary remains available.

## Early-game measurements

The final paired native run is
[`early13-baseline-clean03`](../../artifacts/early13-baseline-clean03/trace.txt).
Both windows use a level-one, itemless, zero-crit Saint with damage 15 and attack
speed 1 against a level-one, itemless Titan with 2,100 HP and 20 armor. Each window
has exactly one enemy and ends with one stored charge. Native damage reports and
the target's HP delta agree to floating-point precision.

| Primary | Hits | Span | Damage | Measured DPS |
|---|---:|---:|---:|---:|
| Previous 108% | 39 | 20.25931 s | 536.625 | 26.48782 |
| Current 144% | 39 | 20.27015 s | 715.5 | 35.29822 |

Equal hit counts demonstrate exactly **33.3% more damage**, with unchanged charge
generation. Elapsed-time differences slightly affect measured DPS. These values
include real armor and Shocked windows, rather than ideal coefficient estimates.

The current-DLL itemless Titan benchmark in
[`early13-final09`](../../artifacts/early13-final09/trace.txt) finishes in
**54.89824 seconds**, with 2,103.89 reported damage across 116 hits, using Primary
and bank-preserving free Orbs. The free Orb now returns through the player and
hits this tall boss again. The pilot is protected and target AI is disabled for
this stationary benchmark: it does not measure normal first-boss survival,
moving-target accuracy or arbitrary item draws.

Astra's whole-kit audit supports this first tuning pass without further blanket
damage increases. Static's minimum buildup still makes the first charge take
roughly 21 eligible Primary hits, subject to Electrocute immunity/timing. The
Primary increase does not shorten that floor; free Orb supplies early damage
without competing with the ultimate bank. Spear, Gaze and cloud have different
costs, proc value and interruption exposure, so their coefficients alone do not
establish comparative encounter balance.

## Verification and presentation

- Doctor confirmed the pinned .NET 10.0.302 SDK and local prerequisites. Full
  [`Native verification`](../../artifacts/verification/20261007T225006-958437Z/results.json)
  passed **35/35**, including **607 linked production assertions** in
  ChargedStormChecks. Native access checks verify installed assembly members;
  the separate recordings below exercise gameplay.
- Final09 passes **86 native assertions**, with zero failures, script errors or
  pose-monitor flags. All seven vulnerable-player checks remain 110 HP to 110 HP,
  with no player damage report or recorded foreign-fixture intrusion. Humanoid
  casts prove 2/3/5/7 hits, a distant owner prevents relay, two enemies alternate
  ABABABA, and the close Titan proves 2/7 hits with actual owner HurtBox contacts.
  Titan feet proximity is approximately 3.356 m despite a 9.723 m core distance.
- [`Showcase03`](../../artifacts/showcase13-final03/trace.txt) passes 12 assertions
  with zero failures/errors and confirms the actual spear/Gaze skill identities
  and damage. Frame inspection covers Orb charge/flight/impact, cloud tiers,
  Circuit overhead casting with Primary, spear, Gaze and Arc Step. The earlier
  Orb VFX03 run separately checked all six palettes; that effect code is retained.
- **One spear-release pose flag remains documented:** a 15.8 cm left-hand step at
  the authored charge-to-throw transition. Both reviewers inspected consecutive
  30 fps frames without seeing a detached hand, stuck pose or repeated oscillation.
  Astra considers it nonblocking for this local candidate, but it is not proven
  a false positive. The spear's animation/controller timing was not changed.
- Calibrated stereo loopback audio reports zero overs, near-clips or failed event
  posts in all three final recordings. Peaks: showcase03 −1.866 dBFS over 169.13 s;
  final09 −6.909 dBFS over 191.36 s; baseline03 −9.720 dBFS over 111.01 s. These are
  measured scene levels and event posting, not universal mix acceptance.

The requested [Astra high audit](../../artifacts/charge-build-1.3/astra-final-audit.md)
reviews mechanics, authority, controls, descriptions, damage/scaling and
presentation. Physical controller/keyboard feel, remote multiplayer and an
ordinary moving/attacking first-boss playtest remain in the
[human acceptance matrix](../manual-charge-build-1.3.md).

Failed or partial fixtures are retained honestly. Final07 suffered foreign-stage
intrusion, including an identified Jellyfish hit and an unattributed damage
report. Showcase01 had empty-terrain aim/framing failures. Showcase02's spear and
Gaze clips exposed saved loadout choices and are invalid for those abilities.
Final captures explicitly select and check the intended skills; continuously
isolated fixtures register their authorized masters before spawn. The fixed-tick
guard is not a claim of instantaneous exclusion.

## Videos, README and candidate identity

[Watch the whole-kit review](../../artifacts/review-1.3/showcase-1.3.mp4), or open
[the individual clips and benchmarks](../../artifacts/review-1.3/index.html).
Videos are 1280×720 at 30 fps with stereo AAC sound. Three fresh animated WebPs
in `docs/media/` show Orb, Thundercloud and Circuit in all four root README
copies. Package READMEs retain existing public media links and clearly identify
1.3 as a local review candidate.

All final captures record these hashes **before** launch; source build, staged
profile and package entry readback match:

| Component | SHA256 |
|---|---|
| DLL | `B39244E2BB0A9E532BB110548B28471D2BAC84817C05E845DB3297A924B0DBDE` |
| Language | `F154A8F930C6B8CC57A36C91411D113100037D7664E4587455B6F69993D1F6EC` |
| Asset bundle | `7238F181B17B011A0BCC0BF13AB8C7D951CAA7930A24DB4F5B83F278BEA111B4` |

The [local package](../../artifacts/candidates/20261007-155224-7847812/JohnstonStu-Hollow_Saint-1.3.0.zip)
is 18,814,568 bytes with eight allowlisted entries. ZIP SHA256:
`AD4C4B9FC9255895ADEDC376FA8DBF613B25EAA36AAD7C90A944B4EA4F3043D7`.
Its README/changelog/manifest/icon/license match current sources.
[Identity report](../../artifacts/charge-build-1.3/early13-final-package-identity.json).
Private-profile backup: `artifacts/foundation/profile-backup-20261007-154849`.
Legacy Unity preview fixtures remain unavailable and were not counted as evidence.
