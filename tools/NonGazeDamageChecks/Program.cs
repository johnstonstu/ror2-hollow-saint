using System;
using System.IO;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.Storm;
using HollowSaint.FoundationKit.Stormspear;

// Success: production snapshots retain exactly 90% of the prior independent damage,
// including custom curves, zero coefficients and inherited fractions; source guards
// verify runtime roots use those policies and excluded paths do not rescale damage.
static class Program
{
    static int checks;
    static void Check(bool valid, string reason)
    { checks++; if (!valid) throw new Exception(reason); }
    static void Near(float actual, float expected, string reason) =>
        Check(Math.Abs(actual - expected) <= .0001f * Math.Max(1f, Math.Abs(expected)), reason);

    static int Main(string[] args)
    {
        try { Run(args.Length > 0 ? args[0] : Directory.GetCurrentDirectory()); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
    static void Run(string root)
    {
        foreach (float raw in new[] { 0f, .2f, .6f, 1.2f, 1.5f, 3.5f, 10f, 14f, 23.75f })
        foreach (float stat in new[] { 12f, 17f, 103f })
        {
            float original = raw;
            float direct = stat * KitDamagePolicy.Effective(raw);
            Near(direct, stat * raw * .9f, "native coefficient scaled exactly once");
            Check(raw == original, "custom/raw configuration unchanged");
            foreach (float fraction in new[] { 0f, .35f, .5f, 1f, 1.7f })
            {
                Near(direct * fraction, stat * raw * fraction * .9f, "inherited burst/chain remains .9, not .81");
                var prayer = new PrayerStrikeSnapshot(stat, raw, fraction, 3.5f, true);
                Near(prayer.Damage, stat * raw * .9f * SpearFeedbackPolicy.OrdinaryStrikeMultiplier, "ordinary full-hold Prayer snapshot once");
                Near(prayer.SplashDamage, stat * raw * fraction * .9f * SpearFeedbackPolicy.OrdinaryStrikeMultiplier, "ordinary splash inherits once");
                Check(prayer.Crit && prayer.SplashRadius == 3.5f, "Prayer crit and radius unchanged");
            }
        }
        foreach (float tap in new[] { .5f, 3.5f, 12f })
        foreach (float full in new[] { 1f, 14f, 30f })
        foreach (float charge in new[] { 0f, .25f, .5f, 1f })
        {
            float raw = tap + (full - tap) * charge;
            float coefficient = SpearFeedbackPolicy.Direct(raw, charge);
            Near(coefficient, raw * .9f * (1f - .1f * charge), "charge-weighted additional spear reduction");
            var conductor = new SpearConductorSchedule(charge, coefficient * 17f, coefficient);
            float prior = 17f * (.20f + .15f * charge);
            Near(conductor.Damage, prior * .9f, "conductor recovers launch stat, then scales own coefficient once");
            Check(Math.Abs(conductor.Damage - prior * .81f) > .001f, "reject double-reduced conductor regression");
            Near(conductor.Radius, 4f + 2f * charge, "conductor radius unchanged");
        }
        var frozen = new PrayerStrikeSnapshot(17, 10, .5f, 3, false);
        Near(frozen.Damage, 65.025f, "ordinary landing coefficient is 3.825");
        Near(frozen.SplashDamage, 32.5125f, "ordinary landing splash remains a fraction");
        var funded = new PrayerStrikeSnapshot(17, 10, .5f, 3, true, funded: true);
        Near(funded.Damage, 130.05f, "funded strike has targeted 15% reduction");
        Near(funded.SplashDamage, 65.025f, "funded splash inherits targeted reduction once");
        Near(SpearFeedbackPolicy.Recharge(5), 6, "default spear recharge increases to six");
        Near(SpearFeedbackPolicy.Recharge(2), 2.4f, "raw custom cooldown receives one fixed adjustment");

        string Read(string path) => File.ReadAllText(Path.Combine(root, "HollowSaintMod/FoundationKit", path));
        void Has(string path, string expression) => Check(Read(path).Contains(expression), path + " missing integration: " + expression);
        void ExcludesPolicy(string path) => Check(!Read(path).Contains("KitDamagePolicy"), path + " must inherit or remain excluded");
        Has("ArcBolt/ArcBoltState.cs", "KitDamagePolicy.Effective(KitTuning.ArcBoltDamageCoefficient) * damageStat");
        Has("Stormspear/StormspearThrowState.cs", "SpearFeedbackPolicy.Direct(StormspearTuning.DamageAt(charge), charge) * damageStat");
        Has("Stormspear/StormspearProjectile.cs", "SpearFeedbackPolicy.Direct(StormspearTuning.DamageAt(shot.Charge), shot.Charge)");
        Has("Stormspear/StormspearProjectile.cs", "projectileDamage != null && projectileDamage.crit, funded: funded)");
        Has("Stormspear/StormspearRegistration.cs", "SpearFeedbackPolicy.Recharge(StormspearTuning.Cooldown)");
        Has("OpenCircuit/OpenCircuitPulseDriver.cs", "KitDamagePolicy.Effective(KitTuning.OpenCircuitPulseDamageCoefficient) * body.damage");
        Has("Storm/StormServer.cs", "KitTuning.ElectrocutePopDamageCoefficient * attacker.damage");
        ExcludesPolicy("Storm/StormServer.cs");
        Has("KitConfig.cs", "Shared passive damage remains unchanged for every triggering skill.");
        Has("Storm/ThunderboltDriver.cs", "snapshot.Funded ? 1f : 0f");
        Has("Stormspear/StormspearProjectile.cs", "new PrayerStrikeSnapshot(launchOwner.damage, KitTuning.ThunderboltDamageCoefficient,");
        Has("ArcBolt/ArcBoltChain.cs", "damage *= KitTuning.ArcBoltChainFalloff;");
        Has("Stormspear/StormspearProjectile.cs", "damage * fraction * scale");
        foreach (string path in new[] { "ArcBolt/ArcBoltChain.cs", "ArcBolt/ArcBoltProjectile.cs", "Storm/ThunderboltDriver.cs",
            "Stormspear/SpearConductor.cs", "Gaze/GazeState.cs", "Gaze/GazeServer.cs", "Gaze/GazeFuelPulse.cs", "Gaze/GazeFuelLedger.cs", "KitShared.cs" })
            ExcludesPolicy(path);
        foreach (string coefficient in new[] { "KitTuning.ArcBoltDamageCoefficient", "StormspearTuning.TapDamage",
            "SpearConductorSchedule.TapCoefficient", "SpearConductorSchedule.FullCoefficient", "KitTuning.OpenCircuitPulseDamageCoefficient",
            "KitTuning.ThunderboltDamageCoefficient" })
            Has("KitDescriptions.cs", "Pct(KitDamagePolicy.Effective(" + coefficient + "))");
        Has("KitDescriptions.cs", "Pct(SpearFeedbackPolicy.Direct(StormspearTuning.FullDamage, 1f))");
        Has("KitDescriptions.cs", "args[\"pop\"] = Pct(KitTuning.ElectrocutePopDamageCoefficient);");
        Has("KitDescriptions.cs", "Pct(GazeTuning.DamagePerSecond)");
        Has("KitDescriptions.cs", "Bonus(KitTuning.ShockedDamageMultiplier)");
        Has("KitDescriptions.cs", "Pct(StormspearTuning.BurstDamageFraction)");
        Has("KitDescriptions.cs", "Pct(StormspearTuning.BurstDamageFractionFull)");
        Console.WriteLine("PASS " + checks + " native damage policy/inheritance and source-integration assertions");
    }
}
