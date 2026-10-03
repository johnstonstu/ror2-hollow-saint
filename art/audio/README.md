# Hollow Saint audio

Mostly original deterministic synthesis, mono PCM16 at 48 kHz. Soft filtered
footfalls, airy glide, restrained electrical Circuit and attack accents are
starting levels for gameplay mixing.

**v0.9.1: three cues come from recorded samples** (Pixabay Content License, see
`samples/CREDITS.md`): `ThunderStrike` (replaces the synthesized thunderclap),
and the new `SpearBurst` (charged Stormspear impact, burst radius 5 m and up; taps
keep the synthesized `SpearImpact`) and `SpearThrowHeavy` (layer on charged and
crown throws). `tools/audio/import_samples.py` trims, fades, filters and levels them.

`hollow-saint-audition.wav` plays footsteps, glide, Circuit, cast and chain with
half-second gaps, followed by Answered Prayer at its actual timings (charge at
0, launch at 0.35, impact at 0.60 seconds). `audition-cues.json` gives timestamps. `source/manifest.json`
records individual peak/RMS levels. `bank-validation.json` records binary checks
and zero clipped samples. Listening in combat and testing the game's SFX slider
remain required; offline amplitude does not establish perceived in-game balance.

Use Python with numpy, then Wwise 2023.1.4.8496 at the path in the authoring script:

```powershell
python tools/audio/synthesize_sfx.py
python tools/audio/import_samples.py   # must follow synthesize: replaces ThunderStrike
python tools/audio/author_bank.py
python tools/audio/check_bank.py
```

The checked-in Wwise project already contains the sound/event objects. Authoring
copies source WAVs into Originals, refreshes properties and events, and generates
`HollowSaintAudio/GeneratedSoundBanks/Windows/HollowSaint.bnk` (format 150).
Only that content bank is embedded. Never ship or load generated `Init.bnk`;
the content references the game's existing SFX_BUS 213475909 and defines no bus.

All 33 sounds use positional spatialization with attenuation: 0 dB through 5 m,
-9 dB at 12 m, -24 dB at 24 m, -60 dB at 40 m. GlideLoop and CircuitLoop are infinite
two-second sources. Their Stop events target only the posting GameObject and
fade over 120 ms. Post start and stop on the same emitter. Event names are listed
in `events.json`.

Answered Prayer uses `Play_HS_ThunderTelegraph` (0.35 s rising static),
`Play_HS_ThunderRelease` (0.24 s discharge), and `Play_HS_ThunderStrike`
(1.2 s thunderclap: sharp crack transient, rolling low rumble tail, peak -11 dBFS like SpearImpact). Runtime uses the strike sound with an owned
copy of the Capacitor visual; the global vanilla effect stays unchanged.
`Stop_HS_ThunderTelegraph` stops only that emitter with a 30 ms fade on cancel,
re-pick, death, or release. For configured gathers longer than the default
0.35 s, the finite static finishes early. Shorter gathers stop it at release.
ArcBoltCast, ChainHop, and ChargeTick have clearer sparse crackles at their
original peak ceilings. Footsteps, glide, and Circuit WAVs are unchanged.

v08 additions: SpearRecall, SpearCatch, FanStart, FanEnd, SpearPulse (quiet, repeats), SpearStruck, and FanLoop (infinite 2 s loop with Stop_HS_FanLoop, same 120 ms emitter-local stop as the other loops). `hollow-saint-audition-v08.wav` (cues in `audition-cues-v08.json`) plays them plus the new ThunderStrike with half-second gaps.
