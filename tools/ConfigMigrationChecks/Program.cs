using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using HollowSaint;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.Gaze;
using HollowSaint.FoundationKit.Stormspear;
using HollowSaint.FoundationKit.SpearDischarge;

// Success: the complete production Bind preserves custom values, applies ordered
// migrations 1..15, synchronizes live callbacks, and leaves newer revisions alone.
// Persistence is an explicit in-memory adapter, not a BepInEx disk-format test.
static class Program
{
    static int checks;
    static readonly Dictionary<FieldInfo, object> Defaults = new[] { typeof(KitTuning), typeof(GazeTuning),
        typeof(StormspearTuning), typeof(GazeReleaseTuning), typeof(FoundationHopoo), typeof(HollowSaint.FoundationKit.ChargedStorm.ChargedStormTuning) }
        .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        .Where(f => !f.IsLiteral && !f.IsInitOnly).ToDictionary(f => f, f => f.GetValue(null));
    static void Check(bool ok, string why) { checks++; if (!ok) throw new Exception(why); }
    static ConfigFile Config(int revision)
    {
        foreach (var pair in Defaults) pair.Key.SetValue(null, pair.Value);
        KitConfig.Floats.Clear(); KitConfig.Ints.Clear(); KitConfig.Bools.Clear();
        var config = new ConfigFile();
        config.Saved[new("6. Misc", "Defaults version")] = revision;
        return config;
    }
    static void Seed(ConfigFile c, string section, string key, object value) => c.Saved[new(section, key)] = value;
    static void CheckValues(ConfigFile c)
    {
        Check(c.Read<float>("1. Arc Bolt", "Damage") == 1.9f && KitTuning.ArcBoltDamageCoefficient == 1.9f, "damage migration/live value");
        Check(c.Read<float>("1. Arc Bolt", "Projectile speed") == 120f, "speed migration");
        Check(c.Read<float>("2. Stormspear", "Full damage") == 12.5f, "ordered historical full-damage migrations");
        Check(c.Read<float>("7. Gaze of the Hollow", "Proc coefficient") == .3f, "Gaze proc migration");
        Check(c.Read<float>("7. Gaze of the Hollow", "Fork damage") == .7f, "Gaze fork migration");
    }
    static void FreshAndOld()
    {
        var fresh = Config(1); KitConfig.Bind(fresh); CheckValues(fresh);
        Check(fresh.Read<int>("6. Misc", "Defaults version") == 17, "fresh revision stamp");
        Check(!fresh.Read<bool>("6. Misc", "Event log"), "fresh event log");
        int count = fresh.Entries.Count;
        Check(count > 80 && count == KitConfig.Floats.Count + KitConfig.Ints.Count + KitConfig.Bools.Count + 5,
            "all options registered once, plus hand, legacy mode and three revision markers");
        for (int revision = 1; revision <= 17; revision++)
        {
            var c = Config(revision);
            Seed(c, "1. Arc Bolt", "Damage", revision < 11 ? 1f : revision < 15 ? 1.2f : revision < 16 ? 1.6f : 1.9f);
            Seed(c, "1. Arc Bolt", "Projectile speed", revision < 13 ? 80f : 120f);
            Seed(c, "2. Stormspear", "Full damage", revision < 4 ? 14f : revision < 12 ? 16f : revision < 16 ? 14f : 12.5f);
            Seed(c, "7. Gaze of the Hollow", "Proc coefficient", revision < 14 ? .5f : .3f);
            Seed(c, "7. Gaze of the Hollow", "Fork damage", revision < 14 ? 1f : .7f);
            Seed(c, "2. Stormspear", "Spear in left hand", false);
            KitConfig.Bind(c); CheckValues(c);
            Check(c.Read<int>("6. Misc", "Defaults version") == 17, "old revision stamped");
            Check(c.Read<SpearHand>("2. Stormspear", "Spear hand") == (revision < 10 ? SpearHand.Right : SpearHand.Auto), "legacy hand conversion");
            Check(c.Saves == (revision < 10 ? 1 : 0), "legacy hand save happens only before revision 10");
            KitConfig.Floats.Clear(); KitConfig.Ints.Clear(); KitConfig.Bools.Clear();
            KitConfig.Bind(c); CheckValues(c);
            Check(c.Entries.Count == count, "repeat bind does not create additional config keys");
        }
    }
    static void ChargedDefaults()
    {
        // 1.3.1: Thundercloud/Orb bind after ApplyMigrations and use their own counter.
        var c = Config(16);
        Seed(c, "8. Thundercloud", "Strike damage", .9f); Seed(c, "8. Thundercloud", "Cooldown", 15f);
        Seed(c, "9. Hollowed Orb", "Hit damage", 4.1f); Seed(c, "5. Storm", "Death discharge", .5f);
        KitConfig.Bind(c);
        Check(c.Read<float>("8. Thundercloud", "Strike damage") == 1.65f, "cloud strike default migrates");
        Check(c.Read<float>("8. Thundercloud", "Cooldown") == 15f, "custom cloud cooldown kept");
        Check(c.Read<float>("9. Hollowed Orb", "Hit damage") == 5f && HollowSaint.FoundationKit.ChargedStorm.ChargedStormTuning.OrbDamage == 5f, "orb damage default migrates live");
        Check(c.Read<float>("5. Storm", "Death discharge") == .5f, "revision-16 config is not re-migrated by the storm step");
        Check(c.Read<int>("6. Misc", "Charged defaults version") == 3, "charged revision stamp");
        var gazeOld = Config(16); Seed(gazeOld, "7. Gaze of the Hollow", "Range", 60f); KitConfig.Bind(gazeOld);
        Check(gazeOld.Read<float>("7. Gaze of the Hollow", "Range") == 90f, "Gaze range migrates 60 -> 90 at revision 17");
        var gazeCustom = Config(16); Seed(gazeCustom, "7. Gaze of the Hollow", "Range", 75f); KitConfig.Bind(gazeCustom);
        Check(gazeCustom.Read<float>("7. Gaze of the Hollow", "Range") == 75f, "custom Gaze range kept");
        var old = Config(15); Seed(old, "5. Storm", "Death discharge", .5f); KitConfig.Bind(old);
        Check(old.Read<float>("5. Storm", "Death discharge") == .3f, "death discharge migrates from revision 15");
        KitConfig.Floats.Clear(); KitConfig.Ints.Clear(); KitConfig.Bools.Clear();
    }
    static void CustomAndCallbacks()
    {
        foreach (float custom in new[] { .99999994f, 1.00000012f, 1.3f })
        {
            var c = Config(1); Seed(c, "1. Arc Bolt", "Damage", custom); KitConfig.Bind(c);
            Check(KitTuning.ArcBoltDamageCoefficient == custom, "adjacent/custom damage preserved");
        }
        foreach (float custom in new[] { 1.1999999f, 1.2000002f, 1.3f })
        {
            var c = Config(14); Seed(c, "1. Arc Bolt", "Damage", custom); KitConfig.Bind(c);
            Check(KitTuning.ArcBoltDamageCoefficient == custom, "adjacent/custom 1.3 damage preserved");
        }
        var same = Config(15); Seed(same, "1. Arc Bolt", "Damage", 1.2f); KitConfig.Bind(same);
        Check(KitTuning.ArcBoltDamageCoefficient == 1.2f, "current custom old default preserved");
        foreach (float custom in new[] { .49999997f, .50000006f, .8f })
        {
            var c = Config(13); Seed(c, "7. Gaze of the Hollow", "Proc coefficient", custom); KitConfig.Bind(c);
            Check(GazeTuning.ProcCoefficient == custom, "adjacent/custom Gaze proc preserved");
        }
        var current = Config(99);
        Seed(current, "8. Thundercloud", "Starting radius", 19f);
        Seed(current, "1. Arc Bolt", "Damage", 1f);
        Seed(current, "6. Misc", "Event log", true);
        Seed(current, "6. Misc", "Event log defaults version", 1);
        KitConfig.Bind(current);
        Check(HollowSaint.FoundationKit.ChargedStorm.ChargedStormTuning.CloudRadius == 19f, "custom new ability radius preserved");
        var radius = (ConfigEntry<float>)current.Entries[new("8. Thundercloud", "Starting radius")];radius.Value=23f;
        Check(HollowSaint.FoundationKit.ChargedStorm.ChargedStormTuning.CloudRadius == 23f, "new ability callback updates live tuning");
        Check(KitTuning.ArcBoltDamageCoefficient == 1f && current.Read<int>("6. Misc", "Defaults version") == 99, "future revision preserved");
        Check(KitConfig.EventLog.Value, "current custom event logging preserved");
        var entry = (ConfigEntry<float>)current.Entries[new("1. Arc Bolt", "Damage")];
        int refreshes = KitDescriptions.Refreshes; entry.Value = 2.4f;
        Check(KitTuning.ArcBoltDamageCoefficient == 2.4f && KitDescriptions.Refreshes == refreshes + 1, "live callback refreshes language once");
        KitConfig.SpearHand.Value = SpearHand.Left;
        Check(SpearCarry.HandMode == SpearHand.Left, "hand callback");
    }
    static void HistoricalDefaults()
    {
        var old = Config(1);
        Seed(old, "3. Arc Step", "Speed", 16.0005f);
        Seed(old, "5. Storm", "Pop targets", 3);
        Seed(old, "5. Storm", "Charges per Thunderbolt", 6);
        Seed(old, "6. Misc", "RoR2 body shader (restart)", false);
        Seed(old, "6. Misc", "Event log", true);
        KitConfig.Bind(old);
        Check(KitTuning.ArcStepSpeed == 20f, "historical tolerant float matching retained");
        Check(KitTuning.ElectrocutePopTargets == 2 && KitTuning.StormChargeMax == 5, "historical integer migrations");
        Check(!FoundationHopoo.Enabled, "ordered shader true/false migrations");
        Check(!KitConfig.EventLog.Value && old.Read<int>("6. Misc", "Event log defaults version") == 1, "legacy log migration");
        var custom = Config(1);
        Seed(custom, "3. Arc Step", "Speed", 16.01f);
        Seed(custom, "5. Storm", "Pop targets", 8);
        KitConfig.Bind(custom);
        Check(KitTuning.ArcStepSpeed == 16.01f && KitTuning.ElectrocutePopTargets == 8, "custom historical values retained");
    }
    static void Main()
    {
        FreshAndOld();
        ChargedDefaults(); CustomAndCallbacks(); HistoricalDefaults();
        Console.WriteLine($"PASS {checks} complete binding/migration assertions");
    }
}
