using HollowSaint.FoundationKit.ChargedStorm;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.HollowedOrb
{
    internal static class OrbAimTargeting
    {
        internal static HealthComponent Select(CharacterBody owner, Vector3 muzzle, Vector3 aim, float range)
        {
            Vector3 eye = KitUtil.EyePosition(owner);
            var candidates = ChargedStormTargeting.Find(owner, eye, range, aim, OrbCastFlow.AimAssistAngle, true);
            HealthComponent best = null;
            float bestAngle = float.PositiveInfinity, bestDistance = float.PositiveInfinity;
            foreach (var health in candidates)
            {
                Vector3 delta = ChargedStormTargeting.Point(health) - eye;
                float distance = delta.magnitude;
                float angle = Vector3.Angle(aim, delta);
                if (angle > OrbCastFlow.AimAssistAngle || !ChargedStormTargeting.Clear(muzzle, ChargedStormTargeting.Point(health))) continue;
                if (angle < bestAngle - .1f || (Mathf.Abs(angle - bestAngle) <= .1f && distance < bestDistance))
                { best = health; bestAngle = angle; bestDistance = distance; }
            }
            return best;
        }
    }
}
