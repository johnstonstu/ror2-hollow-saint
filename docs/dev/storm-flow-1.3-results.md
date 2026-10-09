# Storm flow pass on the 1.3 candidate

October 7, 2026 (Pacific). Picks up from the [1.3 audit handoff](audit-handoff-1.3.md).
Local only: nothing committed, pushed or published. Version stays 1.3.0
(build keywords `storm flow`). Source snapshot before this pass:
`artifacts/source-backup/pre-stormflow-20261007.tgz`.

## Why

The charge abilities spent Static Charges but did nothing for the storm that
earns them: Orb and cloud damage was fenced off from Static entirely, so the
kit read as two separate loops. Charges also come slowly early (about 21 Arc
Bolt hits per charge on a tanky target), and two of the three new abilities
require one. This pass connects spending back to earning without letting a
cast refund itself.

## What changed

| Area | Change | Owner |
|---|---|---|
| Priming | Orb hits prime Static by 35% x the hit's damage falloff (fresh 35%, first revisit 22.75%, ...). Cloud strikes prime 35%. Capped at 95%: priming can never Electrocute, never awards a charge, respects Electrocute immunity, never lowers existing Static. Primed value holds 1 s longer before normal decay. | `Storm/StaticPrimePolicy.cs`, `StormServer.PrimeStatic`, `ServerHollowedOrb.Impact`, `ServerThundercloud.Tick` |
| Shock | Each cloud strike leaves its living victim Shocked (+15% damage taken, 3 s), applied after the hit so the cloud does not amplify itself. | `StormServer.Shock` |
| Config | New: Thundercloud `Static priming` (0.35), `Strikes Shock` (true), `Static priming cap` (0.95, shared); Hollowed Orb `Static priming` (0.35). New keys only, no migration. | `KitConfig.ChargedStorm.cs` |
| Gather cadence | Charge interval 0.30 s to 0.25 s for Orb, Circuit and cloud. Five Special charges by 1.12 s (was 1.32), five Orb charges by 1.50 s (was 1.70). Orb's 0.5 s free-cast window and Specials' 0.12 s first charge are unchanged. | `StoredChargeCastLedger.ChargeInterval` |
| Cloud look | Sky height `radius*0.6` clamped 12 to 21 m (was `radius*0.5`, 10 to 18). Lobes flattened (height 0.36r to 0.2r) and spread wider; smoke puffs smaller and less opaque. Each committed strike flashes a light inside the cloud for 0.22 s. | `ChargedStormTargeting.CloudCenter`, `StormVisualPrimitives.CloudVolume`, `ThundercloudFx`, `ChargedStormStrikeFx` |
| Text | Orb, cloud, Circuit, passive, survivor and Static/Storm keyword text rewritten shorter in en/zh-CN/ru/pt-BR; duplicated Orb/cloud paragraphs removed from survivor and passive text. Orb lists the Static keyword; cloud lists Shocked and Static. READMEs (8) and changelog updated. | `Language/HollowSaint.language`, `ChargedStormRegistration`, READMEs |

Balance intent (single tanky target, itemless): Arc Bolt alone earns roughly
one charge per 10 s; weaving a free Orb every ~7.3 s adds about 0.55 Static
per cast, close to 1.8x charge rate. Packs benefit more because primed enemies
that die at 50%+ Static already discharge. Per Stuart's approach this is
deliberately generous; turn the priming values down if the bank fills too fast.

Unchanged by design: Open Circuit still requires a charge (Stuart's call), Gaze
opening blasts and surges still build no Static, all damage numbers, cooldowns,
hit budgets and relay rules.

## Verification

- `verify.ps1 -Scope Native` ([report](../../artifacts/verification/20261008T035121-093240Z/results.json)):
  every source suite, locked restore and runtime build passed. ChargedStormChecks
  now has **909** linked assertions (was 884); the new `StormFlowChecks.cs`
  covers the cap/never-full policy, cloud prime + Shock once per living victim,
  the Shock option, Orb prime per enemy hit with falloff, no priming on player
  relay contacts, balanced damage scope and the 0.25 s cadence.
- `AccessScannerFixtures` **failed for an environment reason**: its synthetic
  Mono.Cecil fixture needs PowerShell 7 and this machine's shell only has
  Windows PowerShell 5.1 (enum `-bor` cast). It does not touch the mod. The
  real `Check-Access.ps1` scan passed against both the verification runtime
  and the staged DLL (`ACCESS_CHECK_PASS`).
- No gameplay was run. Feel, the cloud's new silhouette and the priming rate
  need Stuart's playtest.

## Staged build

`Stage-Build.ps1` reran verification, rebuilt Release and staged to the
Hollow Saint Dev profile. Backup of the previous files:
`artifacts/foundation/profile-backup-20261007-205308`.

| Asset | SHA-256 |
|---|---|
| DLL | `7BA3F1F5B4C8EA138BD1B5A50ECB5756CD5A3E666C396F09EA09298F8AD3A986` |
| Language | `1560EA1F14CFD3E59E93DD9639401A31266772BE2C99CACAA2D8FFFFC8CACD8D` |
| Bundle | unchanged |

The previous private package under `artifacts/candidates/` is now stale.

## Playtest checklist

1. Free Orb into a pack, then Arc Bolt: primed enemies should crackle (Static
   tier visuals) and Electrocute within a hit or two.
2. Boss: Orb + Arc Bolt weaving. Does the bank fill noticeably faster? Too fast?
3. Cloud over a pack: Shocked icons on survivors, then a quick Arc Bolt sweep.
4. Cloud from close range: is the upper view clearer? Does the flash read?
5. Hold to gather: does 0.25 s per charge feel snappy without overshooting?
6. Read Orb/cloud/Circuit/passive tooltips in game for fit and clarity.
