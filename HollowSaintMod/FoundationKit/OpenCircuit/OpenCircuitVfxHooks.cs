using System;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.OpenCircuit
{
    /// <summary>
    /// Public VFX contract for Open Circuit. Deliberately a PURE event surface — no
    /// EffectManager, no EffectCatalog, no prefab lookups (same rule as
    /// ArcStepVfxHooks). This module raises the events and carries the data; the VFX owner
    /// decides what to spawn and how it replicates.
    ///
    /// Required prefabs, from docs/unity-vfx-anim-spec-20260927.md's VFX table:
    ///   FX_HS_OpenCircuit_Crown     crown segments shimmer + tethers, LOCAL, parented under
    ///                               the Halo, alive from `Crown active` 0.7241 until `Recall`.
    ///   FX_HS_OpenCircuit_Pulse     long arc bolt from crown to each pulse target, NETWORKED,
    ///                               one per confirmed target per pulse.
    ///   FX_HS_OpenCircuit_GapSpark  gap arcs flicker while the segments separate, LOCAL,
    ///                               from `Unfold` 0.1724 to `Crown active` 0.7241.
    ///
    /// Nothing here is implemented by this module; every prefab is REQUIRED ART. Until the
    /// coordinator ships them, every raise below is a no-op event with no subscriber.
    /// </summary>
    public static class OpenCircuitVfxHooks
    {
        /// <summary>Authority side. The cast reached `Unfold` (f6, 0.17241) — start the
        /// gap-arc flicker. Raised once per activation.</summary>
        public static event Action<CharacterBody> UnfoldStarted;

        /// <summary>Authority side. The cast reached `Crown active` (f22, 0.72414) — ignite
        /// the crown, start the long-lived crown VFX. This is the same tick the 8 s buff is
        /// applied and the pulse schedule begins, so the flash and the first pulse are
        /// frame-aligned by construction.</summary>
        public static event Action<CharacterBody> CrownActivated;

        /// <summary>Authority side. One pulse resolved. `targets` may be empty — a pulse
        /// that hit nothing still fired its Bolt VFX in the repo spec. Pulses build
        /// Static on whatever they hit (see StormServer).</summary>
        public static event Action<CharacterBody, Vector3, Vector3[], float> PulseFired;

        /// <summary>Authority side. The buff ended and the `Recall` beat (f3, 0.09524) of
        /// "Open Circuit end" is playing — retract the tethers and dim the gaps. Not raised
        /// on death (the body is going away; the VFX is destroyed with it).</summary>
        public static event Action<CharacterBody> CrownRecalled;

        /// <summary>Convenience for the VFX owner's "is anything subscribed at all" check.</summary>
        public static bool HasSubscribers
        {
            get
            {
                return UnfoldStarted != null
                    || CrownActivated != null
                    || PulseFired != null
                    || CrownRecalled != null;
            }
        }

        // ---- Authority-side entry points. Called by OpenCircuitState / the pulse driver.
        // Public so a future replay/ghost system could drive them directly. ----

        public static void RaiseUnfold(CharacterBody body)
        {
            if (body == null) return;
            var handler = UnfoldStarted;
            if (handler != null) handler(body);
        }

        public static void RaiseCrownActivated(CharacterBody body)
        {
            if (body == null) return;
            var handler = CrownActivated;
            if (handler != null) handler(body);
        }

        /// <summary>origin = the crown/eye position the pulse visually leaves from; hit
        /// points are world-space so the VFX owner can aim one bolt at each without
        /// resolving sockets itself.</summary>
        public static void RaisePulse(CharacterBody body, Vector3 origin, Vector3[] hitPoints, float damage)
        {
            if (body == null) return;
            var handler = PulseFired;
            if (handler != null) handler(body, origin, hitPoints ?? EmptyPoints, damage);
        }

        public static void RaiseRecall(CharacterBody body)
        {
            if (body == null) return;
            var handler = CrownRecalled;
            if (handler != null) handler(body);
        }

        private static readonly Vector3[] EmptyPoints = new Vector3[0];
    }
}
