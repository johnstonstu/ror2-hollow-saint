using BepInEx.Logging;

namespace HollowSaint
{
    /// <summary>
    /// v0.9.17 (release cleanup): Plugin.Log. Warnings and errors always reach the BepInEx log;
    /// Info lines (load diagnostics, rig fits, catalog checks, gameplay events) only when verbose
    /// logging is on (config "6. Misc" > "Verbose log", the Event log option) or the dev autopilot
    /// runs. A normal session logs one "loaded" line from us.
    /// </summary>
    internal sealed class HsLog
    {
        private readonly ManualLogSource source;
        /// <summary>Config "Verbose log".</summary>
        internal static bool Verbose;

        internal HsLog(ManualLogSource source) { this.source = source; }

        internal static bool Enabled
        {
            get
            {
                return Verbose || DevAutopilot.Active ||
                    (FoundationKit.KitConfig.EventLog != null && FoundationKit.KitConfig.EventLog.Value);
            }
        }

        internal void LogInfo(object data) { if (Enabled) source.LogInfo(data); }
        internal void LogWarning(object data) { source.LogWarning(data); }
        internal void LogError(object data) { source.LogError(data); }
        /// <summary>Always printed (the single startup line).</summary>
        internal void LogAlways(object data) { source.LogInfo(data); }
    }
}
