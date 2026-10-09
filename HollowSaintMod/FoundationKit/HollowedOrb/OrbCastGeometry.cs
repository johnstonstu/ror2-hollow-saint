using HollowSaint.FoundationKit.ChargedStorm;
using HollowSaint.FoundationKit.Stormspear;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.HollowedOrb
{
    // Presentation and server flight use the same world-cleared muzzle. Open
    // Circuit changes the cast posture, never its cost, damage or bounce budget.
    internal static class OrbCastGeometry
    {
        internal static Vector3 Point(CharacterBody body, Vector3 aim, int charges, bool pushing = false)
        {
            Vector3 desired = StormspearCharge.InCrown(body)
                ? KitUtil.EyePosition(body) + Vector3.up * StormspearTuning.CrownSpearHeight + aim * (pushing ? .4f : 0f)
                : body.corePosition + aim * (pushing ? 1.3f : .9f);
            float radius = ChargedStormTuning.Diameter(Mathf.Max(1, charges)) * .4f;
            return ProjectileWorldClearance.LaunchOrigin(body.corePosition, desired, radius);
        }

        internal static Vector3 Direction(CharacterBody body, Vector3 origin, Vector3 aim)
        {
            Vector3 target = ChargedStormTargeting.AimPoint(body, aim, ChargedStormTuning.Bound(ChargedStormTuning.OrbRange, 10f, 100f));
            Vector3 delta = target - origin;
            return delta.sqrMagnitude > .01f && Vector3.Dot(delta, aim) > 0f ? delta.normalized : aim;
        }
    }
}
