using System;
using System.Collections.Generic;
using EntityStates;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit
{

    /// <summary>Playtest evidence: logs the first few occurrences of each event, tagged
    /// with which machine it ran on, so a log shows every skill actually firing without
    /// flooding it at 2 shots per second.</summary>
    public static class KitLog
    {
        private const int PerEvent = 3;
        private static readonly Dictionary<string, int> counts = new Dictionary<string, int>();

        public static void Event(string name, string detail = null)
        {
            if (KitConfig.EventLog != null && !KitConfig.EventLog.Value && !DevAutopilot.Active) return;
            int n;
            counts.TryGetValue(name, out n);
            if (n >= PerEvent) return;
            counts[name] = n + 1;
            Plugin.Log.LogInfo("HOLLOW_SAINT_EVENT " + name + " #" + (n + 1) +
                " server=" + NetworkServer.active + (detail != null ? " " + detail : ""));
        }
    }
}
