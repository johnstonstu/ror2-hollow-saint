using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Gaze
{
    /// <summary>Server-created focus history owned by the attacker's body, never a static root.</summary>
    public sealed class GazeFocusTracker : MonoBehaviour
    {
        private struct Entry { internal float Focus, LastHit; }
        private readonly Dictionary<HealthComponent, Entry> entries = new Dictionary<HealthComponent, Entry>();
        private readonly List<HealthComponent> stale = new List<HealthComponent>();
        private HealthComponent owner;
        private Stage stage;
        private float nextPrune;

        private void Awake() { owner = GetComponent<HealthComponent>(); stage = Stage.instance; }
        private void FixedUpdate() { Refresh(Time.time); }
        private void OnDisable() { entries.Clear(); stale.Clear(); nextPrune = 0f; }

        private bool Refresh(float now)
        {
            if (!owner || !owner.alive || !owner.isActiveAndEnabled || stage != Stage.instance)
            {
                OnDisable();
                stage = Stage.instance;
                return false;
            }
            if (now < nextPrune) return true;
            nextPrune = now + 2f;
            // Keep history through configurable grace/fade windows; pruning must
            // not erase retained focus when a player chooses a slower decay.
            float expiry = Mathf.Max(4f, GazeFocusPolicy.GraceSeconds + GazeFocusPolicy.DecaySeconds);
            foreach (var pair in entries)
                if (!pair.Key || !pair.Key.alive || !pair.Key.isActiveAndEnabled || now - pair.Value.LastHit > expiry)
                    stale.Add(pair.Key);
            foreach (var victim in stale) entries.Remove(victim);
            stale.Clear();
            return true;
        }

        internal float Hit(HealthComponent victim, float now, out int newTier)
        {
            newTier = 0;
            if (!Refresh(now) || !victim || !victim.alive) return 1f;
            bool known = entries.TryGetValue(victim, out var entry);
            int before = known ? GazeFocusPolicy.Tier(entry.Focus) : 0;
            float next = GazeFocusPolicy.Next(known ? entry.Focus : 0f, known ? entry.LastHit : -1f, now);
            entries[victim] = new Entry { Focus = next, LastHit = now };
            int after = GazeFocusPolicy.Tier(next);
            if (after > before) newTier = after;
            return GazeFocusPolicy.Multiplier(next);
        }
    }
}
