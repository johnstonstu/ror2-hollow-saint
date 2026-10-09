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
        private static long primes, refunds, closingStrikes, incomeLimited, bankFull;

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
            primes = refunds = closingStrikes = incomeLimited = bankFull = 0;
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

        internal static void RecordPrime() { if (EnsureActive()) primes++; }
        internal static void RecordRefund() { if (EnsureActive()) { refunds++; chargesAdded++; } }
        internal static void RecordClosingStrike() { if (EnsureActive()) closingStrikes++; }
        /// <summary>A charge that could not bank: guard-limited or the bank was already full.</summary>
        internal static void RecordUnbanked(bool limited) { if (!EnsureActive()) return; if (limited) incomeLimited++; else bankFull++; }

        // Balance playtests: a "fight" is the span from the first living monster to none left.
        // Two bounded lines per fight, only with Event log on; no per-kill spam.
        private static int fightMonsters;
        private static float fightStart;
        private static long fightCharges;
        private static void TrackFight()
        {
            if (KitConfig.EventLog == null || !KitConfig.EventLog.Value) return;
            int alive = 0;
            foreach (TeamComponent member in TeamComponent.GetTeamMembers(TeamIndex.Monster))
                if (member && member.body && member.body.healthComponent && member.body.healthComponent.alive) alive++;
            if (fightMonsters == 0 && alive > 0)
            {
                fightStart = Time.time; fightCharges = chargesAdded;
                Plugin.Log.LogAlways("HOLLOW_SAINT_FIGHT_START t=" + Time.time.ToString("0.0", CultureInfo.InvariantCulture) + " monsters=" + alive);
            }
            else if (fightMonsters > 0 && alive == 0)
                Plugin.Log.LogAlways("HOLLOW_SAINT_FIGHT_CLEAR t=" + Time.time.ToString("0.0", CultureInfo.InvariantCulture) +
                    " seconds=" + (Time.time - fightStart).ToString("0.0", CultureInfo.InvariantCulture) +
                    " chargesEarned=" + (chargesAdded - fightCharges));
            fightMonsters = alive;
        }

        internal static void Tick()
        {
            if (NetworkServer.active) TrackFight();
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
            if (qualifyingHits == 0 && chargeElectrocutes == 0 && thunderbolts == 0) return;
            float minutes = Mathf.Max(1f / 60f, (Time.time - startedAt) / 60f);
            // Storm loop health is always logged (one bounded line per 30 s): starved, healthy or flooding?
            Plugin.Log.LogAlways("HOLLOW_SAINT_STORM_INCOME reason=" + reason +
                " chargesPerMinute=" + (chargesAdded / minutes).ToString("0.0", CultureInfo.InvariantCulture) +
                " primes=" + primes + " circuitRefunds=" + refunds + " closingStrikes=" + closingStrikes +
                " incomeLimited=" + incomeLimited + " wastedBankFull=" + bankFull);
            if (KitConfig.EventLog != null && !KitConfig.EventLog.Value) return;
            Plugin.Log.LogInfo("HOLLOW_SAINT_STORM_SUMMARY reason=" + reason +
                " runSeconds=" + (Time.time - startedAt).ToString("0.0", CultureInfo.InvariantCulture) +
                " qualifyingHits=" + qualifyingHits + " chargeElectrocutes=" + chargeElectrocutes +
                " chargesAdded=" + chargesAdded + " thunderbolts=" + thunderbolts +
                " strikeElectrocutes=" + strikeElectrocutes);
        }
    }
}
