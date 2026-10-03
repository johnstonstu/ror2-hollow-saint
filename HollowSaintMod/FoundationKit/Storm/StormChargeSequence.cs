using System;
using HollowSaint.FoundationKit.Vfx;

namespace HollowSaint.FoundationKit.Storm
{
    /// <summary>Presentation-only protocol: delayed packets cannot undo a later release.</summary>
    internal sealed class StormChargeSequence
    {
        private int lastRevision;
        internal int LastRevision { get { return lastRevision; } }
        private float gatherUntil;
        private float gatherDuration = 0.35f;
        private float releasedUntil;
        internal float GatherAmount { get; private set; }
        internal bool Released(float now) { return now < releasedUntil; }

        internal bool Receive(Beat beat, float duration, int revision, float now)
        {
            if (revision <= lastRevision) return false;
            lastRevision = revision;
            if (beat == Beat.ThunderGather)
            {
                GatherAmount = 0f; // a re-pick restores the orbit before the new convergence
                gatherDuration = Math.Max(0.05f, duration);
                gatherUntil = now + gatherDuration + 0.45f;
                return false;
            }
            gatherUntil = 0f;
            if (beat != Beat.ThunderRelease) return false;
            GatherAmount = 0f;
            releasedUntil = now + 0.35f;
            return true;
        }

        internal void Tick(float now, float dt, bool alive)
        {
            bool gathering = alive && now < gatherUntil;
            float step = Math.Max(0f, dt) / (gathering ? gatherDuration : 0.15f);
            GatherAmount = gathering ? Math.Min(1f, GatherAmount + step) : Math.Max(0f, GatherAmount - step);
        }
    }
}
