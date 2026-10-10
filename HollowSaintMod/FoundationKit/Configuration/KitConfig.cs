using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;

namespace HollowSaint.FoundationKit
{
    public static partial class KitConfig
    {
        internal struct FloatOption { public ConfigEntry<float> Entry; public float Min, Max, Step; public bool Restart; }
        internal struct IntOption { public ConfigEntry<int> Entry; public int Min, Max; public bool Restart; }

        internal static readonly List<FloatOption> Floats = new List<FloatOption>();
        internal static readonly List<IntOption> Ints = new List<IntOption>();
        internal static readonly List<ConfigEntry<bool>> Bools = new List<ConfigEntry<bool>>();

        /// <summary>Debug logging of the first few gameplay events (HOLLOW_SAINT_EVENT lines).</summary>
        public static ConfigEntry<bool> EventLog;
        /// <summary>Stormspear "Spear hand" (Auto, Left, Right).</summary>
        public static ConfigEntry<SpearDischarge.SpearHand> SpearHand;

        private const string move = "0. Movement", bolt = "1. Arc Bolt", spear = "2. Stormspear", step = "3. Arc Step",
                circuit = "4. Open Circuit", storm = "5. Storm", misc = "6. Misc", gaze = "7. Gaze of the Hollow";

        internal static void Bind(ConfigFile c)
        {
            BindMovement(c);
            BindArcBolt(c);
            BindStormspear(c);
            BindArcStep(c);
            BindOpenCircuit(c);
            BindGaze(c);
            BindStorm(c);
            ApplyMigrations(c);
            BindLogging(c);
            BindChargedStorm(c);
            ApplyChargedStormMigrations(c);
        }

        private static void F(ConfigFile c, string section, string key, float def, Action<float> set,
            float min, float max, float stepSize, string description, bool restart = false)
        {
            var entry = c.Bind(section, key, def, description);
            set(entry.Value);
            entry.SettingChanged += (s, e) => { set(entry.Value); KitDescriptions.Refresh(); };
            Floats.Add(new FloatOption { Entry = entry, Min = min, Max = max, Step = stepSize, Restart = restart });
        }

        private static void I(ConfigFile c, string section, string key, int def, Action<int> set,
            int min, int max, string description, bool restart = false)
        {
            var entry = c.Bind(section, key, def, description);
            set(entry.Value);
            entry.SettingChanged += (s, e) => { set(entry.Value); KitDescriptions.Refresh(); };
            Ints.Add(new IntOption { Entry = entry, Min = min, Max = max, Restart = restart });
        }

        private static void B(ConfigFile c, string section, string key, bool def, Action<bool> set, string description)
        {
            var entry = c.Bind(section, key, def, description);
            set(entry.Value);
            entry.SettingChanged += (s, e) => set(entry.Value);
            Bools.Add(entry);
        }

    }
}
