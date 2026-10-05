using System;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.OpenCircuit
{
    public enum CircuitDwellBeat { Progress, Reset, Zap }
    /// <summary>Server-only confirmed victim contract. VFX subscribers own replication.
    /// Origin is the actual damage sphere centre, targetPoint is world space. Progress
    /// cannot imply a hit; only Zap is emitted after one native damage attempt.</summary>
    public static class CircuitDwellHooks
    {
        public static event Action<CharacterBody, HurtBox, Vector3, Vector3, float, CircuitDwellBeat> Changed;
        internal static void Raise(CharacterBody owner, HurtBox target, float progress, CircuitDwellBeat beat)
        {
            var handlers = Changed;
            if (handlers == null || !owner || !target) return;
            foreach (Action<CharacterBody, HurtBox, Vector3, Vector3, float, CircuitDwellBeat> handler in handlers.GetInvocationList())
                try { handler(owner, target, owner.corePosition, target.transform.position, progress, beat); }
                catch (Exception error) { Plugin.Log.LogWarning("HOLLOW_SAINT_CIRCUIT_DWELL_VFX " + error); }
        }
    }
}
