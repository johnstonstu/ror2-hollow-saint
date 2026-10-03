# Skins and alternate loadouts (proposals)

Built variants and ideas for future passes. Prototype skins are available without unlocks.
Every idea keeps the Hollow Saint identity: a cracked devotional icon, a four-piece
halo, lightning that chains, and a meter that gets spent all at once.

## Skins

Recolours are cheap: the skin system builds tinted material clones in code
(`FoundationSkin.MakeVariant`), so a new palette is about 20 lines and no model work.
A skin that also changes the lightning colour needs the VFX palette (`HsPalette`)
to follow `body.skinIndex`, which is a small refactor (listed as a to-do).

| Skin | Look | Lightning | Cost | Status |
|---|---|---|---|---|
| **Cracked Icon** (default) | Warm ivory plates, graphite body, aged copper halo | Cyan and white | - | Built |
| **Obsidian Saint** | Black-glass plates, pewter halo and trim | Cyan and white | Recolour | **Built** |
| **Verdigris Relic** | Bronze/jade materials, moss tabard, slow emissive breath | Cyan and white | Animated material variant | Built for v0.5.0 |
| **Solar Vespers** | Gilded plates, ivory tabard, warm emissive pulse | Cyan and white | Animated material variant | Built for v0.5.0 |
| **Umbral Choir** | Dark ceramic, violet accents, staggered shimmer | Cyan and white | Animated material variant | Built for v0.5.0 |
| **Cathedral Glass** | Plates become stained-glass panels, lead-line seams | Multicoloured arcs that shift hue per hop | New emission texture | Proposed, grandmastery tier |

Unlocks, if wanted: Mastery skin for beating Monsoon, following the vanilla pattern
(an `UnlockableDef` plus an achievement). Suggest Solar Vespers for mastery.

The three v0.5.0 variants reuse the existing mesh, textures, rig and clips. Only
the model's emissive accents animate; per-model property blocks keep skins from
changing shared materials. Skin swaps restore the active material's emission.
The original two skin indices remain unchanged. Skill VFX recolouring and new
silhouettes remain future work. Check the selection mannequin and live body.

## Alternate skills

Each slot keeps the design rule from the ability workshop: the primary builds the
meter, everything else has its own cooldown and neither needs nor spends charge.

### Primary: **Arc Lance** (alt to Arc Bolt)
Hold to channel a continuous beam from the fingertips (like a short-range tether)
that jumps to one extra target per second held, up to 3. Deals steady damage and
builds charge per tick rather than per shot. Plays well with attack speed through
tick rate. Trade-off: shorter range (25 m), no projectile travel time.
*Reuses:* LightningLine for the beam, the chain search, the gesture layer.

### Secondary: **Grounding Rod** (alt to Conduit Spear)
Throw a rod that sticks in the ground for 6 s. Enemies within 6 m are Conductors
while it stands, and your chains always route through the rod, so it acts as a relay
that extends chain range across a room. Trade-off: area control instead of a single
priority target.
*Reuses:* spear projectile, Conductor Mark buff, LightningLine relays.

### Utility: **Ascension** (alt to Arc Step)
One charge. Rise straight up 12 m in a column of lightning, then hover for up to
2 s while firing (glide physics). Landing sends a small shock ring. Trade-off: no
horizontal escape, but excellent for sightlines and dodging ground attacks.
*Reuses:* glide heel jets, CircuitOpen ring, Descend/Glide clips.

### Special: **Litany** (alt to Open Circuit)
Kneel and channel for up to 4 s (rooted). Every 0.5 s a large bolt strikes the
highest-health enemy within 30 m and chains to 4 more. Releasing early refunds
part of the cooldown. Trade-off: big single-target and pack damage, but you stop
moving; the opposite of Open Circuit's run-and-gun.
*Reuses:* Discharge burst visuals, chain logic, Spawn clip for the kneel.

### Passive variant idea: **Manual Discharge**
The design workshop left "automatic versus manual release" open. A loadout toggle
could make Discharge fire on a key instead of the next hit (on the special slot's
secondary input, or as a replacement special). Worth testing only after the
automatic version has been played.

## Suggested order

1. Play the current kit and both skins; settle balance with the Risk of Options sliders.
2. Skin-aware VFX palette (unblocks the three coloured skins).
3. Arc Lance and Grounding Rod (most reuse, most distinct play patterns).
4. Ascension and Litany.
5. Mastery unlock.
