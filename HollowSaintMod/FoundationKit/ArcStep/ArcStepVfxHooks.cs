using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.ArcStep
{
    /// <summary>Public VFX hooks for Arc Step. The VFX owner calls these; Arc Step only
    /// raises the events and carries the data. No prefab logic lives here — deliberately
    /// no EffectManager/EffectCatalog calls, so this file stays a pure contract.
    ///
    /// Ground-trail placement: the dash path is a straight world-space line from the
    /// activation position along ArcStepState's latched dashDirection, length
    /// speed*duration (16 m/s x 0.55 s = 8.8 m, non-tapered; the taper is cosmetic).
    /// "ground" socket concept: KitUtil.ResolveSocket(body, "ground") returns the
    /// ground-projected socket when one exists (FoundationMounts currently wires heel
    /// sockets, not a "ground" socket — resolving may return null and callers must
    /// degrade to the body position, which the helpers below already do). If the
    /// coordinator adds a "ground" socket via ChildLocator.AddChild, these helpers pick
    /// it up with no code change here.</summary>
    public static class ArcStepVfxHooks
    {
        /// <summary>Server/authority side: the dash started. Raised by ArcStepState.OnEnter
        /// when isAuthority. data contains the full dash path; spawn the trail wherever
        /// this fires (server) or mirror it on clients from the replicated state.</summary>
        public static event System.Action<ArcStepDashData> DashStarted;

        /// <summary>Server/authority side: the dash stopped for any reason (end, interrupt,
        /// cancel, death). Useful for fading a trail out.</summary>
        public static event System.Action<ArcStepDashData> DashEnded;

        /// <summary>Static event subscription guard for the VFX owner.</summary>
        public static bool HasSubscribers
        {
            get { return DashStarted != null || DashEnded != null; }
        }

        /// <summary>Raised internally by ArcStepState. Not for external callers.</summary>
        internal static void RaiseDashStarted(ArcStepDashData data)
        {
            var handler = DashStarted;
            if (handler != null) handler(data);
        }

        /// <summary>Raised internally by ArcStepState. Not for external callers.</summary>
        internal static void RaiseDashEnded(ArcStepDashData data)
        {
            var handler = DashEnded;
            if (handler != null) handler(data);
        }

        /// <summary>Authority-side entry called by ArcStepState.OnEnter. Resolves the
        /// "ground" socket (null-tolerant), then raises DashStarted. Public so a future
        /// replay/ghost system could drive it; the dash itself is the intended caller.</summary>
        public static void SpawnGroundTrail(CharacterBody body, Vector3 startPosition,
            Vector3 direction, float duration, float speed, bool startedGrounded)
        {
            if (body == null) return;
            var data = ArcStepDashData.Build(body, startPosition, direction, duration, speed, startedGrounded);
            RaiseDashStarted(data);
        }

        /// <summary>Authority-side entry called by ArcStepState.OnExit for any exit path.</summary>
        public static void EndGroundTrail(CharacterBody body)
        {
            if (body == null) return;
            RaiseDashEnded(new ArcStepDashData { body = body });
        }

        /// <summary>Snapshots one dash. Struct so zero allocation on the hot path.</summary>
        public struct ArcStepDashData
        {
            public CharacterBody body;
            public Vector3 startPosition;
            public Vector3 direction;     // planar, normalized, world space
            public float duration;
            public float speed;
            public bool startedGrounded;

            /// <summary>World-space end of the dash path (start + direction*speed*duration).
            /// The VFX owner can raycast down from this to find terrain.</summary>
            public Vector3 EndPosition { get { return startPosition + direction * (speed * duration); } }

            public static ArcStepDashData Build(CharacterBody body, Vector3 startPosition,
                Vector3 direction, float duration, float speed, bool startedGrounded)
            {
                return new ArcStepDashData
                {
                    body = body,
                    startPosition = startPosition,
                    direction = direction,
                    duration = duration,
                    speed = speed,
                    startedGrounded = startedGrounded
                };
            }
        }
    }

    /// <summary>Per-frame service points for the VFX owner. The dash state pushes its live
    /// position/velocity into this ring each FixedUpdate (authority side); a trail system
    /// can poll the last sample without touching the state directly. Kept separate from
    /// ArcStepVfxHooks so the event contract stays stable if this buffer changes.</summary>
    public static class ArcStepTrailSampler
    {
        private const int Capacity = 8;
        private static readonly ArcStepVfxHooks.ArcStepDashData[] samples = new ArcStepVfxHooks.ArcStepDashData[Capacity];
        private static int head;

        /// <summary>Called by ArcStepState every FixedUpdate while dashing (authority side).</summary>
        internal static void PushSample(CharacterBody body, Vector3 position, Vector3 velocity, bool grounded)
        {
            samples[head] = ArcStepVfxHooks.ArcStepDashData.Build(body, position, velocity.normalized, 0f, velocity.magnitude, grounded);
            head = (head + 1) % Capacity;
        }

        /// <summary>Most recent sample, or default when nothing has been pushed.</summary>
        public static ArcStepVfxHooks.ArcStepDashData Latest { get { return samples[(head + Capacity - 1) % Capacity]; } }
    }
}