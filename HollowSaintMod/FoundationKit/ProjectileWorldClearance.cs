using UnityEngine;
using RoR2;

namespace HollowSaint.FoundationKit
{
    // World-only checks: enemy colliders and cosmetic triggers remain native impacts.
    internal static class ProjectileWorldClearance
    {
        internal static bool Clear(Vector3 origin, Vector3 direction, float distance, float radius)
        {
            if (Physics.CheckSphere(origin, radius, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) return false;
            return distance <= 0f || !Physics.SphereCast(origin, radius, direction, out _, distance,
                LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
        }

        internal static bool TryCorrection(Vector3 origin, Vector3 current, Vector3 proposed,
            float distance, float radius, out Vector3 accepted)
        {
            bool clear = Clear(origin, proposed, distance, radius);
            accepted = clear ? proposed : current;
            return clear;
        }

        internal static Vector3 LaunchOrigin(Vector3 aimOrigin, Vector3 muzzle, float radius)
        {
            // Never relocate through a wall to reach the muzzle. If the anchor itself is
            // obstructed, retain the native muzzle/collision path rather than inventing an exit.
            if (Physics.CheckSphere(aimOrigin, radius, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) return muzzle;
            Vector3 offset = muzzle - aimOrigin;
            float distance = offset.magnitude;
            if (distance <= 0.001f) return muzzle;
            if (!Physics.SphereCast(aimOrigin, radius, offset / distance, out var hit, distance,
                LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) return muzzle;
            Vector3 safe = aimOrigin + offset / distance * Mathf.Max(0f, hit.distance - 0.02f);
            return Physics.CheckSphere(safe, radius, LayerIndex.world.mask, QueryTriggerInteraction.Ignore) ? aimOrigin : safe;
        }
    }
}
