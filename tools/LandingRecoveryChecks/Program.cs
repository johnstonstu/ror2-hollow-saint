using System;
using System.IO;
using HollowSaint.FoundationKit.Storm;
using HollowSaint.FoundationKit.Stormspear;
using HollowSaint.FoundationKit.Gaze;

// Success: hand/Crown full-holds get one ordinary strike, funded taps still upgrade,
// launch snapshots remain immutable, and healing is bounded by actual entry spends
// across all supported capacities, retained banks, duplicate events and cancellation.
static class Program
{
    static int checks;
    static void Check(bool condition, string reason) { checks++; if (!condition) throw new Exception(reason); }
    static void Near(float actual, float expected, string reason) => Check(Math.Abs(actual - expected) < .0001f * Math.Max(1f, expected), reason);
    static void Main(string[] args)
    {
        foreach (byte form in new byte[] { 0, StormspearShot.CrownCombo })
        foreach (float charge in new[] { 0f, .5f, .97f, .998f, 1f })
        {
            var shot = new StormspearShot(StormspearShot.ForceForCharge(charge), form);
            Check(shot.FullyHeld == (charge == 1f), "full-hold eligibility independent of Crown and legacy tolerance");
            foreach (bool funded in new[] { false, true })
            {
                bool eligible = funded || shot.FullyHeld;
                var snapshot = eligible ? new PrayerStrikeSnapshot(17, 10, .5f, 3, true, funded) : default;
                var landing = new PrayerImpactClaim();
                Check(landing.TryResolve(snapshot.Empowered, true, true, true) == eligible, "one eligible landing only");
                Check(!landing.TryResolve(true, true, true, true), "duplicate collision closed");
                if (eligible)
                {
                    Near(snapshot.Damage, funded ? 130.05f : 65.025f, "ordinary exactly half funded including funded taps");
                    Near(snapshot.SplashDamage, snapshot.Damage * .5f, "splash inherits once");
                    Check(snapshot.Funded == funded && snapshot.Crit && snapshot.SplashRadius == 3, "immutable upgrade/crit/radius");
                }
            }
        }
        foreach (int capacity in new[] { 2, 5, 6, 20 })
        foreach (float maxHealth in new[] { 100f, 137f, 1000f })
        for (int entry = 0; entry <= capacity; entry++)
        {
            var ledger = new GazeFuelLedger();
            ledger.Begin(entry, capacity);
            var budget = new GazeRecoveryBudget();
            budget.Begin(maxHealth, ledger.Capacity, ledger.Entry);
            Near(budget.Claim(0), 0, "no heal before spend");
            for (int gain = 0; gain < capacity; gain++) ledger.TryGain();
            float total = 0;
            for (int spent = 1; spent <= entry; spent++)
            {
                Check(ledger.TrySpend(1), "entry launch spends");
                float amount = budget.Claim(ledger.Spent);
                Near(amount, maxHealth * .05f / capacity, "capacity-normalized base heal");
                total += amount;
                Near(budget.Claim(ledger.Spent), 0, "duplicate event never heals");
            }
            Check(!ledger.TrySpend(1), "reserve cannot fund same cast");
            Near(budget.Claim(entry + 1), 0, "beyond-entry cannot heal");
            Near(total, maxHealth * .05f * entry / capacity, "partial/full bank exact bounded budget");
            budget.Clear();
            Near(budget.Claim(1), 0, "ended/dead/disabled cast cannot heal");
        }
        var retained = new GazeFuelLedger(); retained.Begin(20, 2);
        Check(retained.Capacity == 20, "config lowering retains actual frozen bank capacity");
        var frozen = new GazeRecoveryBudget(); frozen.Begin(100, retained.Capacity, retained.Entry);
        Near(frozen.Claim(1), .25f, "retained twenty normalizes by frozen twenty rather than configured two");
        frozen.Begin(float.NaN, 5, 5); Near(frozen.Claim(1), 0, "invalid health cannot create healing");
        foreach (bool owner in new[] { false, true }) foreach (bool stage in new[] { false, true })
        { var landing = new PrayerImpactClaim(); Check(landing.TryResolve(true, true, owner, stage) == (owner && stage), "owner/stage loss prevents ordinary and funded strike"); }

        string root = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
        string Read(string relative) => File.ReadAllText(Path.Combine(root, "HollowSaintMod/FoundationKit", relative));
        string impact = Read("Stormspear/StormspearProjectile.cs"), driver = Read("Storm/ThunderboltDriver.cs"), gaze = Read("Gaze/GazeFuelController.cs"), storm = Read("Storm/StormServer.cs");
        Check(impact.Contains("!funded && !shot.FullyHeld") && !impact.Contains("ServerStrikeAt("), "all full-holds and no legacy duplicate");
        Check(impact.IndexOf("launchStage = Stage.instance") < impact.IndexOf("TryClaimSpearPrayer"), "ordinary stage snapshot captured even without bank");
        Check(driver.Contains("snapshot.Funded ? 1f : 0f") && driver.Contains("snapshot.Funded ? 0.5f : 0f") && driver.Contains("if (snapshot.Funded && victim"), "ordinary has no item proc or forced status");
        Check(gaze.IndexOf("if (!ledger.TrySpend(1)) continue;") < gaze.IndexOf("recovery.Claim(ledger.Spent)"), "recovery strictly after actual spend");
        Check(!gaze.Substring(gaze.IndexOf("internal void Receive(")).Contains(".Heal("), "observers cannot heal from received events");
        Check(storm.IndexOf("HasProc(ProcType.HealNova)") < storm.IndexOf("TryDeathDischarge(victim"), "native healing-orb feedback blocked before hits and killing discharge");
        Console.WriteLine("PASS " + checks + " landing/recovery assertions");
    }
}
