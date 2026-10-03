# Hollow Saint to-do

Master checklist. `[x]` done, `[ ]` open, `[~]` partly done or placeholder in place.
Items marked **(Stuart)** need a person at the game, editor or a design call.

## 1. Core kit (gameplay)

- [x] Skills install on the live body (own SkillFamilies, not SetSkillInternal)
- [x] Arc Bolt: hold-to-fire, cadence scales with attack speed
- [x] Arc Bolt: chain hops to 3 more enemies with falloff, line of sight
- [x] Conduit Spear: 450% projectile, Conductor Mark debuff
- [x] Conductor Mark amplification applied before damage (TakeDamageProcess hook)
- [x] Conductor Mark: one active mark per Hollow Saint (a new mark replaces the old)
- [x] Arc Bolt chains prefer the marked enemy when it is in range
- [x] Arc Step: 2 charges, 4 directions, usable in air
- [x] Arc Step: jump cancels into a momentum jump
- [x] Open Circuit: 8 s crown on its own state machine, 0.5 s pulses, 8 m
- [x] Discharge: meter as replicated buff stacks, capped 6-target burst
- [x] Discharge arms 0.25 s after filling
- [ ] Arc Step preserves sprint (resume sprint after dash if sprint was held)
- [x] Discharge snap should read as its own hit (distinct damage color)
- [ ] Balance pass: coefficients against Commando/Mage/Artificer baselines **(Stuart playtest)**
- [ ] Decide Arc Step i-frames **(Stuart)**
- [ ] Decide Open Circuit charge rate (currently 1 per 3 connected pulses) **(Stuart)**
- [ ] Decide Conductor Mark multiplier (currently x1.5) **(Stuart)**
- [ ] Alt skills (later): e.g. manual Discharge release, alternate utility

## 2. Networking and robustness

- [x] Projectiles fired from authority through ProjectileManager
- [x] Buffs and damage on the server copy of states
- [x] Crown state machine networked, reset on death and stun
- [x] Review findings S3 (meter replication) and S5 (fail-open check) fixed
- [x] S6 damage source tag centralised (KitUtil.SourceOf)
- [ ] Two-player test: host plus client, every skill **(Stuart)**
- [ ] Late-join and respawn: meter and crown state correct after respawn
- [ ] Verify effects stay within VFX budget with many items (Ukulele, Tesla, etc.)
- [x] Config file (BepInEx ConfigEntry) for tunables so balance changes need no rebuild

## 3. VFX

- [x] Code-built VFX system (Beats, networked effect prefab, palette ramp)
- [x] Arc Bolt ghost, muzzle flash, impact, chain arcs
- [x] Spear lance ghost, impact, Conductor Mark reticle
- [x] Arc Step afterimages and take-off ring
- [x] Open Circuit open ring, halo crown arcs, pulse rings, halo-to-target arcs
- [x] Chest core glow and halo crackle driven by the meter
- [x] Discharge burst and arcs
- [x] Arc Step ground trail (thin angular line along the path)
- [x] Heel jets during glide (sprint) and on jump
- [ ] Halo gaps light per 25% of meter (needs halo gap sockets or offsets)
- [ ] Tune sizes, brightness and counts in game **(Stuart playtest)**
- [ ] Wire the Blender VFX meshes (art/vfx/assets, 11 FBX) once the bundle is rebuilt

## 4. SFX

- [x] Beat to sound table with vanilla Wwise placeholders (KitSfx)
- [x] Server beats carry sound to every client
- [x] Open Circuit ambient loop while the crown is up (start/stop events)
- [x] Glide loop sound while sprinting
- [x] Custom sound design: v0.5.0 Wwise 2023.1 bank, 23 original effects and per-character loop stops.
- [ ] Audition custom bank in combat, distance attenuation and game SFX slider **(Stuart)**.
- [x] Footsteps and landing using vanilla step events

## 5. Animation

- [x] Body-layer presentation state machine using all 15 bundled clips
- [x] In-between clips: run start/stop, jump, land, glide enter/exit, Arc Step sequence
- [x] Export the remaining 50 of 65 clips from Blender v31
- [x] Build GameFoundation02 controller: 4 layers (Body, UpperBody, Overlay, Halo); verified in batch
- [x] 8-direction locomotion blend tree (walk and run)
- [x] Upper-body skill gestures while moving
- [ ] Aim layer (pitch/yaw poses; clips exported, layer not built yet)
- [x] Arc Step right/back/forward clips (only left is bundled)
- [ ] Death: ragdoll setup
- [ ] Strict clip QA beyond 15/65 (leg pops, tabard contacts) **(Stuart review)**

## 6. Character select and UI

- [x] Name, subtitle, outro, description with tips
- [x] Skill names and descriptions with damage styling
- [x] Keyword tooltips (Agile, Conductor, Discharge)
- [x] Skill, passive and buff icons
- [x] Character portrait icon (body.portraitIcon)
- [x] Select-screen flourish (spark and sound when chosen)
- [x] Logbook lore entry
- [x] Mission-failed flavor text
- [x] Storm charge presentation: earned halo orbs; separate HUD meter disabled at Stu's request in v0.4.3

## 7. Model, materials, items

- [~] FoundationMaterials is a diagnostic pass (graphite rear shell) **(Stuart look)**
- [ ] Hopoo shader conversion for the body materials
- [ ] Item display rules (vanilla items fitted to the 23 mounts)
- [x] Skins: default plus at least one alternate
- [ ] Mastery achievements and unlockable skin (later)

## 8. Tooling and repo

- [x] Stage-Build.ps1 (build, back up, stage, hash)
- [x] Repo tidy, docs consolidated, source assets tracked
- [x] kit-architecture.md
- [x] Batch-mode Unity bundle build script (no editor clicks)
- [x] Automated Blender clip export for all 65 clips

## 9. Release (later, only on Stuart's go)

- [ ] Thunderstore manifest, icon, README, changelog
- [ ] Remove debug log spam (KitLog) or gate it behind config
- [ ] Push to GitHub

## 10. Added during the overnight pass

- [x] Select intro / select idle clips on the character-select mannequin
- [x] Spawn clip on first spawn, combat idle when in combat
- [x] Obsidian Saint skin (tinted clones, cyan kept) with skin swatches
- [x] Fix: returning a gesture layer to Empty froze all gestures (KitAnim.Stop)
- [x] Separate rate parameters per gesture layer (bundle03)
- [ ] Skin-aware VFX palette so coloured skins can recolour the lightning
- [ ] Alternate skills from docs/skins-and-loadouts.md (Arc Lance, Grounding Rod, Ascension, Litany)
- [ ] Pivot and plant-turn clips wired into presentation (exported, unused)
- [ ] Glide heel jets: tune angle and density in game **(Stuart playtest)**

## Playtest 2026-09-28 (v0.2.1 footage)
- [x] Private-member access via publicized GameLibs broke anim/dash (v0.2.1, IL check gate added)
- [x] Rings/flashes oversized and white (v0.2.2)
- [x] Halo maroon -> bone (v0.2.3, verify HOLLOW_SAINT_HALO_BONE log)
- [x] Arc Bolt fires from hand, converges on crosshair (v0.2.3)
- [ ] Halo stuck unfolded after Open Circuit: code safety net in v0.2.3; if HOLLOW_SAINT_HALO_RESET logs, do the proper fix in bundle03 (Open Circuit end on Halo layer)
- [ ] Survivor slot 19 not drawn in select grid (registered, not hidden, portrait ok)
- [ ] Broad refinement: animation feel, VFX, SFX per skill (needs Stu's notes per skill)
- [ ] Register-DevMod needs r2modman closed; listing lags DLL when open

## Next (from mod research, 2026-09-28)
- [x] On-screen Storm charge meter retired in v0.4.3 at Stu's request; earned orbs are the charge readout
- [x] Halo charge orbs and Royal Capacitor strike effect playtested positively; v0.4.3 impact pause awaiting feedback
- [x] Bounded Storm summary logging with uncapped counters for tuning
- [ ] Shock overlay on Electrocuted enemies via TemporaryOverlay + vanilla shock material
- [ ] Consider custom Wwise bank via R2API.Sound for a signature thunderclap (NetworkSoundEventDef)
- [ ] Camera shake on Thunderbolt once ShakeEmitter public API is confirmed
- [ ] Dedicated buff icons for Shocked / Storm charge
