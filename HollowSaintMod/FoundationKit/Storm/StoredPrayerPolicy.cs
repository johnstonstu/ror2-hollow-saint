using System;

namespace HollowSaint.FoundationKit.Storm
{
    /// <summary>Stored Prayer is claimed only by a created server projectile.
    /// The entire full bank is spent once; partial banks and Gaze reserve stay intact.</summary>
    internal static class StoredPrayerPolicy
    {
        internal static int Capacity(int configured) => Math.Max(2, Math.Min(20, configured));
        internal static bool TrySpend(ref int charge, int configured, bool server, bool created, bool ownerAlive, bool gazeActive, out int spent)
        {
            spent = 0;
            if (!server || !created || !ownerAlive || gazeActive || charge < Capacity(configured)) return false;
            spent = charge;
            charge = 0;
            return true;
        }
    }

    internal readonly struct PrayerStrikeSnapshot
    {
        internal readonly bool Empowered, Crit;
        internal readonly float Damage, SplashDamage, SplashRadius;
        internal PrayerStrikeSnapshot(float ownerDamage, float coefficient, float splashFraction, float splashRadius, bool crit, bool funded = false)
        {
            Empowered = true;
            // Both funded Prayer and the unfunded Crown bonus enter with raw tuning.
            Damage = ownerDamage * KitDamagePolicy.Effective(coefficient) * (funded ? Stormspear.SpearFeedbackPolicy.FundedStrikeMultiplier : 1f);
            SplashDamage = Damage * Math.Max(0f, splashFraction);
            SplashRadius = Math.Max(0f, splashRadius);
            Crit = crit;
        }
    }

    /// <summary>Landing, miss, owner loss or stage loss closes a funded projectile.
    /// Neither a second collider nor cleanup can create another strike or refund.</summary>
    internal struct PrayerImpactClaim
    {
        private bool closed;
        internal bool TryResolve(bool funded, bool qualifyingLanding, bool ownerValid, bool sameStage)
        {
            if (closed) return false;
            closed = true;
            return funded && qualifyingLanding && ownerValid && sameStage;
        }
        internal void Cancel() { closed = true; }
    }
}
