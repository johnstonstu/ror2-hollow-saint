using System.Globalization;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.Storm
{
    /// <summary>Uncapped counts, bounded output: one summary per 30 seconds and run end.</summary>
    internal static class StormTelemetry
    {
        private const float ReportInterval = 30f;
        private static bool installed;
        private static bool active;
        private static float startedAt;
        private static float nextReport;
        private static long qualifyingHits;
        private static long chargeElectrocutes;
        private static long chargesAdded;
        private static long thunderbolts;
        private static long strikeElectrocutes;

        internal static void Install()
        {
            if (installed) return;
            Run.onRunStartGlobal += OnRunStart;
            Run.onRunDestroyGlobal += OnRunEnd;
            installed = true;
        }

        internal static void Uninstall()
        {
            if (!installed) return;
            Run.onRunStartGlobal -= OnRunStart;
            Run.onRunDestroyGlobal -= OnRunEnd;
            Finish();
            installed = false;
        }

        private static void OnRunStart(Run run)
        {
            Finish();
            if (NetworkServer.active) Begin();
        }

        private static void OnRunEnd(Run run) { Finish(); }

        private static void Begin()
        {
            active = true;
            startedAt = Time.time;
            nextReport = startedAt + ReportInterval;
            qualifyingHits = chargeElectrocutes = chargesAdded = thunderbolts = strikeElectrocutes = 0;
        }

        private static bool EnsureActive()
        {
            if (!NetworkServer.active) return false;
            if (!active) Begin();
            return true;
        }

        internal static long Thunderbolts { get { return thunderbolts; } }
        internal static long ChargeElectrocutes { get { return chargeElectrocutes; } }
        internal static void RecordHit() { if (EnsureActive()) qualifyingHits++; }
        internal static void RecordStrike() { if (EnsureActive()) thunderbolts++; }

        internal static void RecordElectrocute(bool awardsCharge, bool addedCharge)
        {
            if (!EnsureActive()) return;
            if (awardsCharge) chargeElectrocutes++;
            else strikeElectrocutes++;
            if (addedCharge) chargesAdded++;
        }

        internal static void Tick()
        {
            if (!active) return;
            if (!NetworkServer.active) { Finish(); return; }
            if (Time.time < nextReport) return;
            Report("periodic");
            nextReport = Time.time + ReportInterval;
        }

        private static void Finish()
        {
            if (!active) return;
            Report("end");
            active = false;
        }

        private static void Report(string reason)
        {
            if (KitConfig.EventLog != null && !KitConfig.EventLog.Value) return;
            if (qualifyingHits == 0 && chargeElectrocutes == 0 && thunderbolts == 0) return;
            Plugin.Log.LogInfo("HOLLOW_SAINT_STORM_SUMMARY reason=" + reason +
                " runSeconds=" + (Time.time - startedAt).ToString("0.0", CultureInfo.InvariantCulture) +
                " qualifyingHits=" + qualifyingHits + " chargeElectrocutes=" + chargeElectrocutes +
                " chargesAdded=" + chargesAdded + " thunderbolts=" + thunderbolts +
                " strikeElectrocutes=" + strikeElectrocutes);
        }
    }
}
