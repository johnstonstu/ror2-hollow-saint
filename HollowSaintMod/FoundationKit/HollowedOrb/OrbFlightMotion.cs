using UnityEngine;

namespace HollowSaint.FoundationKit.HollowedOrb
{
    /// <summary>Shared constant-speed motion for authoritative travel and its visual.</summary>
    internal static class OrbFlightMotion
    {
        internal static float Travel(float distance, float speed, float remaining, float dt) =>
            Mathf.Min(remaining, Mathf.Min(speed * dt, distance));

        internal static Vector3 Advance(Vector3 point, Vector3 destination, float speed, float dt)
        {
            var delta = destination - point;
            return delta.sqrMagnitude > .0001f ? point + delta.normalized *
                Travel(delta.magnitude, speed, float.PositiveInfinity, dt) : destination;
        }
    }
}
