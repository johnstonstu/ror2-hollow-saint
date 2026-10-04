namespace HollowSaint.FoundationKit
{
    /// <summary>Runtime balance for native damage outside Gaze. Configuration remains
    /// raw; apply once at an independent coefficient, never to inherited splash/chain
    /// fractions, body damage, item proc coefficients or Shocked's damage modifier.
    /// Passive Electrocute is shared, including reactions triggered by Gaze.</summary>
    internal static class KitDamagePolicy
    {
        internal const float NonGazeMultiplier = 0.9f;
        internal static float Effective(float rawCoefficient) => rawCoefficient * NonGazeMultiplier;
    }
}
