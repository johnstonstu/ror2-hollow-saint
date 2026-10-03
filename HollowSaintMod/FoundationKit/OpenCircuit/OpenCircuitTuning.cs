namespace HollowSaint.FoundationKit.OpenCircuit
{
    /// <summary>
    /// Tuning that belongs to Open Circuit but has no approved number in KitTuning.
    /// KitShared.cs is append-only and coordinator-owned, so nothing here overrides an
    /// approved value — every approved number (12 s cooldown, 8 s buff, 0.5 s pulse
    /// interval, 0.6x pulse coefficient, 8 m radius) is read from KitTuning at its
    /// single point of use. Members here are either derived from the real clip frame
    /// numbers or are a PROPOSAL parked for Stuart's M5 pass.
    ///
    /// Frame arithmetic (all clips 1-based inclusive, normalized fraction =
    /// (frame - 1) / (end - start), per docs/unity-vfx-anim-spec-20260927.md §4):
    ///
    ///   "Open Circuit" 1-30          span = 29
    ///     Unfold        f6  -> (6  - 1) / 29 =  5 / 29 = 0.17241  (spec: 0.1724)
    ///     Crown active  f22 -> (22 - 1) / 29 = 21 / 29 = 0.72414  (spec: 0.7241)
    ///     clip length         = 29 / 24 = 1.20833 s              (spec: 1.2083 s)
    ///
    ///   "Open Circuit hold" 1-25     span = 24
    ///     Pulse          f1  -> (1  - 1) / 24 =  0 / 24 = 0.00000 (spec: 0.0000)
    ///     loop length          = 24 / 24 = 1.00000 s              (spec: 1.0 s)
    ///
    ///   "Open Circuit end" 1-22      span = 21
    ///     Recall         f3  -> (3  - 1) / 21 =  2 / 21 = 0.09524 (spec: 0.0952)
    ///     Recovered      f22 -> (22 - 1) / 21 = 21 / 21 = 1.00000 (spec: 1.0000)
    ///     clip length         = 21 / 24 = 0.87500 s              (spec: 0.875 s)
    ///
    /// Every fraction below matches the coordinator's Unity spec to 4 decimals.
    /// </summary>
    public static class OpenCircuitTuning
    {
        // ---- Cast clip ("Open Circuit", 1-30). NOT attack-speed scaled. ----
        // The repo anim spec lists the cast as a fixed "cast 1.2083 s (29 f)" with no
        // "/attackSpeed" qualifier (unlike Arc Bolt 0.5 s / attackSpeed and Conduit Spear
        // 0.6 s / attackSpeed), because the approved 8 s buff and 0.5 s pulse cadence are
        // both fixed numbers — scaling the gesture with attack speed would decouple the
        // "Crown active" beat from a fixed-length buff window.
        /// <summary>v0.9.15: the cooldown waits for the crown to close (KitConfig "Cooldown after crown").</summary>
        public static bool CooldownAfterCrown = true;
        public const float CastClipSeconds = 29f / 24f;      // 1.20833 s
        public const float UnfoldNormalizedTime = 5f / 29f;  // 0.17241
        public const float CrownActiveNormalizedTime = 21f / 29f; // 0.72414

        // ---- Hold clip ("Open Circuit hold", 1-25, loops). ----
        public const float HoldLoopSeconds = 24f / 24f;      // 1.0 s
        public const float PulseNormalizedTime = 0f / 24f;   // 0.0 — first frame of the loop

        // ---- End clip ("Open Circuit end", 1-22). ----
        public const float EndClipSeconds = 21f / 24f;           // 0.875 s
        public const float RecallNormalizedTime = 2f / 21f;     // 0.09524

        // ---- Animator wiring (repo anim spec §2 layers, §5 state-name rule). ----
        // State names are SPACE form per §5; these clips are already authored that way.
        // Cast prefers the Halo layer (new controller; the UpperBody mask excludes the halo
        // bones). CastAnimLayerFallback is used when the Halo layer lacks the state.
        public const string CastAnimLayer = "Halo";
        public const string CastAnimLayerFallback = "UpperBody";
        public const string CastAnimState = "Open Circuit";
        public const string HoldAnimLayer = "Halo";        // layer 4 — halo-only mask, so the
                                                           // crown keeps animating under an
                                                           // Arc Bolt that owns UpperBody
        public const string HoldAnimState = "Open Circuit hold";
        public const string EndAnimLayer = "UpperBody";    // arms (and old-controller fallback)
        public const string EndAnimLayerHalo = "Halo";     // new controller: end clip ends on the rest pose
        public const string EndAnimState = "Open Circuit end";
        // bundle06 arm clips on the gesture layers (UpperBody standing / UpperArms moving).
        public const string CastArmsState = "Open Circuit arms";
        public const string HoldArmsState = "Open Circuit arms hold";
        /// <summary>After another gesture interrupts the arms hold, wait this long with the arm
        /// layers resting before raising the hold again (so a bolt does not flicker it).</summary>
        public const float HoldArmsResumeDelay = 0.3f;

        /// <summary>Playback-rate parameter passed to PlayAnimation. The repo anim spec §3
        /// says RoR2 already drives "attackSpeed" on model animators and EntityStates pass it
        /// as the playbackRate parameter name for all gesture durations. Same string the
        /// Spear and Discharge modules use so the kit is uniform.</summary>
        public const string PlaybackRateParam = "attackSpeed";

        /// <summary>PROPOSAL: the crown VFX that ignites at `Crown active` stays alive
        /// under the halo-only Halo layer until the `Recall` beat of the end clip. The repo
        /// anim spec VFX table says "from Crown active 0.7241 until Recall", so this is
        /// spec-derived rather than invented — kept here so the driver has one place to
        /// read it from.</summary>
        public const bool CrownVfxSpansBuff = true;

        // ------------------------------------------------------------------ //
        //  PROPOSAL — parked for Stuart's M5 pass
        //
        //  These are `static readonly`, NOT `const`, on purpose. A const bool is folded by
        //  the C# compiler, so a branch on it compiles to dead code and emits CS0162
        //  "unreachable code" — the warning the ArcStep module carries deliberately for its
        //  one documented const false. That warning is not approved for this file, so these
        //  stay readonly. They remain single-line, one-place flips.
        // ------------------------------------------------------------------ //

        /// <summary>PROPOSAL: whether the pulses keep striking while the body is gliding
        /// or mid-Arc-Step. DEFAULT: ALLOWED.
        ///
        /// The architecture makes "allowed" the free option: the pulses run from
        /// OpenCircuitPulseDriver, a component that ticks off the BUFF, not off the
        /// EntityStateMachine. Nothing about a dash or a glide touches that component, so
        /// with this true the crown keeps pulsing through both with no extra code.
        ///
        /// Setting it false makes OpenCircuitPulseDriver consult
        /// ArcStepState.IsBodyDashing(body) and suppress the pulse tick. The glide half
        /// CANNOT be implemented yet: glide is presentation only in this build (layer-0
        /// animator states "Glide enter / Glide loop / Glide exit", repo anim spec §2) with
        /// no code-level predicate to query. A coordinator-supplied IsGliding hook would be
        /// needed to enforce the glide half — see RESULT-OPENCIRCUIT.md REQUEST 4.
        ///
        /// The only reason to set this false is a power-balance read (a stationary AoE
        /// window that also survives the mobility tools). Parked, not decided.</summary>
        public static readonly bool AllowPulsesDuringGlideAndArcStep = true;

        /// <summary>PROPOSAL: fires the first pulse on the very tick the crown lights,
        /// instead of waiting a full OpenCircuitPulseInterval. Default true so the damage
        /// lands on the same frame as the visible ignition flash the Unity spec calls for
        /// ("Crown active 0.7241 — crown ignition flash; buff + pulse schedule starts").
        /// With false the first pulse is delayed by 0.5 s, costing 17 pulses per buff
        /// becoming 16.</summary>
        public static readonly bool FirstPulseIsImmediate = true;

        /// <summary>PROPOSAL: the Open Circuit buff is hidden from the HUD until its icon
        /// art lands. A BuffDef with a null iconSprite still occupies a HUD buff-bar slot;
        /// hiding it removes any chance of a blank entry rendering (or throwing) on a
        /// Vanilla RoR2 BuffBar. Flipping this to false is a one-line change once the
        /// coordinator supplies FX_HS_OpenCircuit_BuffIcon — isHidden affects DISPLAY
        /// ONLY; HasBuff, the whole pulse schedule and the replication path are unaffected.
        /// See RESULT-OPENCIRCUIT.md REQUEST 5.</summary>
        public static readonly bool HideBuffFromHudUntilArtExists = true;

        // NOTE: BuffDef in 1.4.1 has NO spellCard field this module could rely on
        // (decompile-verified field list: iconPath, iconSprite, buffColor, canStack,
        // eliteDef, isDebuff, isDOT, ignoreGrowthNectar, isCooldown, isHidden, flags,
        // stackingDisplayMethod, startSfx). startSfx is a NetworkSoundEventDef and is
        // deliberately left null — R2API.ContentAddition.AddBuffDef logs a warning for a
        // startSfx whose eventName is empty, so assigning one without the real asset
        // would spam the log. REQUIRED ART: crown VFX, pulse bolt and buff icon are all
        // listed in RESULT-OPENCIRCUIT.md.
    }
}
