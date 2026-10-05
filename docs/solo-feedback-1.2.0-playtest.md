# Solo feedback revision from installed 9be97ff3

Private source/build candidate. Installation waits for the final combined Astra audit and a separate parent go. Native rendering, perceived audio and HUD placement are pending playtest. No release-version bump: this remains the existing 1.2.0 private prototype.

## Controls, text and audio

Gaze keeps its mapped Special activation, fresh Primary pulse taps, entry-only fuel, separate reserve, launch-only two-second grants up to fourteen seconds and five-step ordinary damage ramp up to +25%. Its own ordinary and fueled damage are unchanged in this revision. Character-select Gaze text is shortened to channel damage/duration, tap/charge use, extension/ramp caps and saved new charges. Detailed controls and cancellation behavior remain in the original manual playtest guide and pulse tooltip.

The added pulse cue now uses the existing mod `Play_HS_ThunderRelease`: a 0.24-second electrical discharge. The original Gaze startup, charge, hum, crackle and ending events remain unchanged. Its existing indexed fallback is `Play_captain_m2_tazer_shoot`. Delayed launch acknowledgements arriving within 0.24 seconds coalesce into one cue, rather than stacking voices. This does not delay gameplay or change the pulse schedule.

Arc Bolt's main impact uses its dedicated `Play_HS_BoltImpact` event with the established ChainHop crackle at 0.82 PCM gain, approximately -1.72 dB relative to the prior default impact source. The active cue stays 0.20 seconds, padded to its historical 0.28-second media slot. ChainHop and all 34 other bank media entries remain byte-identical; event/action/routing/loop hierarchy is unchanged. No master volume or SFX-bus gain changes. The existing 0.22-second impact throttle and removal of the extra impact accent remain.

The local timer is a compact charcoal panel below the center view, with separate muted title and bright seconds, a cyan three-pixel meter and two-second divisions. A confirmed duration increase briefly shows its actual grant, including a fractional final grant. The fixed fourteen-second scale still makes grants visibly extend the bar. It follows the local native HUD canvas, hides for observers/hidden HUD/death/state exit, restores shared GUI color and creates no persistent HUD objects. Native font scale and positioning require inspection.

## Integrated crown and Gaze visuals

Astra visual commit f7481f6e48de869e483abd349187443fe06857e3 is integrated. The sustained body grows from 0.65 m to 2.65 m through the existing five launch steps; haze grows 0.90→2.95 m, sheath 0.80→2.80 m and core 0.09→0.17 m. Final animated widths remain capped to the configured hit diameter. The traveling sweep has a 2.95 m envelope capped to that diameter, with radial point bounds. Timing, targeting, damage radius and palette selection are unchanged.

All three reserve display layers stay hidden during Gaze. Earned reserve counts still accumulate, remain unavailable as current-cast fuel and reconcile with retained entry fuel on exit. Reconciled charges appear at the crown rather than flying from an invisible reserve position.

Open Circuit's dome is replaced by the actual four metal crown elements moving to the equator of its core-centred damage sphere, with four sparse perimeter hops and two intermittent upward arcs. There are six pooled strokes/twelve line renderers. Expansion takes 0.65 seconds and return 0.30 seconds; gameplay duration stays unchanged. Replicated buff state supports observers. Gaze takes crown ownership during all its non-idle phases; expiry, death, invisibility, model loss and disable restore the original pose. Bone dock centres reach the configured radius. Native metal mesh extents, animation ordering and appearance remain unmeasured.

## Non-Gaze ability damage: exactly one 0.9 factor

Saved config values remain **raw and unchanged**, including custom values. No defaults migration or config write is added. Gameplay applies 0.9 once at each independent non-Gaze ability damage coefficient; descriptions show the effective coefficient, and relevant config help explains the factor. Editing a raw ability coefficient of 2.0 therefore yields 1.8 effective damage. The shared Electrocute passive is exempt and continues to use its raw coefficient. Repeated setting changes and launches do not compound the factor.

| Damage source | Prior default | Effective default |
|---|---:|---:|
| Arc Bolt direct | 120% | 108% |
| Stormspear direct, tap/full | 350% / 1400% | 315% / 1260% |
| Conductor, tap/full per tick | 20% / 35% | 18% / 31.5% |
| Open Circuit pulse | 60% | 54% |
| Funded Prayer / unfunded Crown Thunderbolt | 1000% | 900% |
| Shared Electrocute pop (exempt) | 150% | 150% |

Arc Bolt chains inherit their already-reduced direct damage. Spear bursts inherit the reduced launch damage; default enemy splash at tap/full is 175%/1400% → 157.5%/1260%. Terrain burst retains its existing fraction. Thunderbolt splash remains 50% of the strike, giving 500% → 450% by default. Those inherited fractions are not multiplied by 0.9 again.

Conductor recovers the frozen launch damage stat using the **effective** spear coefficient, then reduces its independent conductor coefficient once. This avoids an unintended 0.81 factor. Prayer snapshots reduce their coefficient once for both funded and unfunded Crown paths; landing/direct/splash paths do not reduce again.

**Shared passive exception:** Electrocute pop stays unchanged globally, including when Gaze triggers the Static reaction. Its default coefficient remains 150%, and custom pop values remain fully effective. No damage attribution or trigger-context tracking is added. This preserves Gaze's passive damage consequences as well as its own core, impact splash, automatic forks/chains and fueled pulse coefficients. Shocked's damage-taken multiplier, body damage, item coefficients and all proc coefficients remain unchanged. Items may naturally inherit a smaller triggering non-Gaze ability hit; no extra item reduction was added. Arc Step has no native damage to reduce.

## Focused later playtest

Record the candidate commit and DLL hash. Use the normal solo profile and camera; native results below remain pending.

| Case | Expected |
|---|---|
| Electrical pulse | A fresh accepted Primary tap produces one discharge rather than the startup beat. Original startup/loops/end sound normal. Mash/rejected taps/cancelled intakes add no cue. |
| Dense impacts | Arc Bolt's default main impact is modestly quieter in a busy fight; ChainHop and unrelated ability cues keep their existing mix. |
| Timer | Read seconds at normal resolution, bright/dark terrain and HUD scale. Launch grants visibly extend the bar and briefly show +2s or the actual final fraction. No crosshair/hotbar obstruction. |
| Timer lifecycle | Hidden HUD, other-player view, death, disable, stage/body replacement and a new cast leave no stale timer or extension cue. |
| Short description | Character-select Gaze description fits in English, Chinese, Russian and Portuguese; displayed base damage/duration match tuning. |
| Non-Gaze ability damage | At fixed body damage/crit/config, compare Arc Bolt, spear direct/burst, conductor, Circuit and Prayer/Crown to 9be97ff3: each is 90%, never 81%. |
| Configs / Gaze / shared passive | Custom raw ability coefficients remain saved; tooltips show 90% of them. Gaze's ordinary/fueled damage and Electrocute pops triggered by Gaze or any other skill match the installed baseline. Custom pop coefficients remain fully effective. |
| Crown and reserves | Verify the integrated physical crown/perimeter, more dramatic Gaze width ramp and hidden reserve display. Reserve gains must stay mechanical, unavailable as current-cast fuel, and reconcile on exit. |

Near-expiry remote duration acknowledgement remains a known multiplayer limitation. The pulse audio coalescer limits clustered acknowledgement audio, but native host/client timing, observers and live performance are not accepted by solo source checks.
