using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;

namespace HollowSaint.FoundationKit
{
    public static partial class KitConfig
    {
        private static void ApplyMigrations(ConfigFile c)
        {
            // Defaults migration: BepInEx keeps saved values, so a changed default only reaches an
            // existing config through here. Each step rewrites an entry only when its saved value
            // still equals the OLD default (the player never touched it); anything the player
            // changed is kept. Bump CurrentDefaultsVersion and add a step whenever a default changes.
            // A fresh config file starts at version 1, runs every step harmlessly (no value matches
            // an old default) and is stamped current.
            var defaultsVersion = c.Bind(misc, "Defaults version", 1, "Internal: tracks default-value migrations. Do not edit.");
            if (defaultsVersion.Value < 2)
            {
                Migrate(step, "Speed", 16f, 20f, v => KitTuning.ArcStepSpeed = v);
            }
            if (defaultsVersion.Value < 3)
            {
                // v0.7 early-game balance pass (tools/balance/dps_model.py).
                Migrate(storm, "Pop damage", 2.5f, 1.5f, v => KitTuning.ElectrocutePopDamageCoefficient = v);
                MigrateInt(storm, "Pop targets", 3, 2, v => KitTuning.ElectrocutePopTargets = v);
                Migrate(storm, "Pop proc coefficient", 0.5f, 0.3f, v => KitTuning.ElectrocutePopProc = v);
                Migrate(storm, "Pop Static", 0.4f, 0.15f, v => KitTuning.ElectrocutePopStatic = v);
                Migrate(storm, "Static minimum per hit", 0.08f, 0.06f, v => KitTuning.StaticMinGain = v);
                Migrate(storm, "Shocked damage taken", 1.2f, 1.15f, v => KitTuning.ShockedDamageMultiplier = v);
                // (v0.9: the spear spread/anchor entries these steps used to migrate are gone.)
            }
            // v0.9: the Conduit Spear / held fan / Conductor entries no longer exist; BepInEx leaves
            // their saved values as unread orphaned lines, which is harmless.
            if (defaultsVersion.Value < 4)
            {
                // v0.9.10: the spear burst no longer hits the struck enemy, so the direct hit goes up;
                // Gaze comes back sooner.
                Migrate(spear, "Full damage", 14f, 16f, v => Stormspear.StormspearTuning.FullDamage = v);
                Migrate(gaze, "Cooldown", 16f, 12f, v => Gaze.GazeTuning.Cooldown = v);
            }
            if (defaultsVersion.Value < 5)
            {
                // v0.9.10: RoR2's body shader on by default so status and elite effects show.
                MigrateBool(misc, "RoR2 body shader (restart)", false, true, v => FoundationHopoo.Enabled = v);
            }
            if (defaultsVersion.Value < 6)
            {
                // v0.9.11: back to the authored shading (overlays now work on it); undo the v5 flip.
                MigrateBool(misc, "RoR2 body shader (restart)", true, false, v => FoundationHopoo.Enabled = v);
            }
            if (defaultsVersion.Value < 7)
            {
                // v0.9.13 (Stu playtest): Electrocute is a short jolt; the full-charge splash is wider.
                Migrate(storm, "Electrocute stun", 1.5f, 0.5f, v => KitTuning.ElectrocuteStunSeconds = v);
                Migrate(spear, "Burst radius (full)", 9f, 10f, v => Stormspear.StormspearTuning.BurstRadiusFull = v);
            }
            if (defaultsVersion.Value < 8)
            {
                // v0.9.15 (Stu playtest): slower, bigger Thunderbolt; Open Circuit crown longer, cooldown after the crown.
                Migrate(storm, "Thunderbolt telegraph", 0.35f, 0.6f, v => KitTuning.ThunderboltTelegraphSeconds = v);
                Migrate(circuit, "Duration", 8f, 10f, v => KitTuning.OpenCircuitBuffSeconds = v);
                Migrate(circuit, "Cooldown", 12f, 8f, v => KitTuning.OpenCircuitCooldown = v);
            }
            if (defaultsVersion.Value < 9)
            {
                // v0.9.16 (Stu playtest): Thunderbolts a little more often.
                MigrateInt(storm, "Charges per Thunderbolt", 6, 5, v => KitTuning.StormChargeMax = v);
            }
            if (defaultsVersion.Value < 10)
            {
                // 1.1.0: "Spear in left hand" (bool, default true) became "Spear hand" (Auto/Left/Right).
                // The old default maps to the new default Auto; a player who turned it off keeps Right.
                var oldHand = new ConfigDefinition(spear, "Spear in left hand");
                var old = c.Bind(oldHand, true);
                if (!old.Value) SpearHand.Value = SpearDischarge.SpearHand.Right;
                c.Remove(oldHand);
                c.Save();
            }
            if (defaultsVersion.Value < 11)
            {
                // Primary damage pass: preserve custom values, including those close to 1.0.
                Migrate(bolt, "Damage", 1.0f, 1.2f, v => KitTuning.ArcBoltDamageCoefficient = v, matchExactly: true);
            }
            if (defaultsVersion.Value < 12)
            {
                // Stormspear damage pass: burst inherits the curve; preserve every custom coefficient.
                Migrate(spear, "Tap damage", 4f, 3.5f, v => Stormspear.StormspearTuning.TapDamage = v, matchExactly: true);
                Migrate(spear, "Full damage", 16f, 14f, v => Stormspear.StormspearTuning.FullDamage = v, matchExactly: true);
            }
            if (defaultsVersion.Value < 13)
            {
                // Reliability pass: migrate only the exact prior default; preserve custom speeds.
                Migrate(bolt, "Projectile speed", 80f, 120f, v => KitTuning.ArcBoltProjectileSpeed = v, matchExactly: true);
            }
            if (defaultsVersion.Value < 14)
            {
                // 1.2 Gaze pass: 7 s beam means more ticks and fork volleys; preserve custom values.
                Migrate(gaze, "Proc coefficient", 0.5f, 0.3f, v => Gaze.GazeTuning.ProcCoefficient = v, matchExactly: true);
                Migrate(gaze, "Fork damage", 1f, 0.7f, v => Gaze.GazeTuning.ForkDamage = v, matchExactly: true);
            }
            if (defaultsVersion.Value < CurrentDefaultsVersion) defaultsVersion.Value = CurrentDefaultsVersion;
            KitDescriptions.Refresh();
        }

        private const int CurrentDefaultsVersion = 14;

        private static void MigrateBool(string section, string key, bool oldDefault, bool newDefault, Action<bool> set)
        {
            foreach (var b in Bools)
            {
                if (b.Definition.Section != section || b.Definition.Key != key) continue;
                if (b.Value == oldDefault) { b.Value = newDefault; set(newDefault); }
                return;
            }
        }

        private static void MigrateInt(string section, string key, int oldDefault, int newDefault, Action<int> set)
        {
            foreach (var o in Ints)
            {
                if (o.Entry.Definition.Section != section || o.Entry.Definition.Key != key) continue;
                if (o.Entry.Value == oldDefault) { o.Entry.Value = newDefault; set(newDefault); }
                return;
            }
        }

        private static void Migrate(string section, string key, float oldDefault, float newDefault, Action<float> set, bool matchExactly = false)
        {
            foreach (var o in Floats)
            {
                if (o.Entry.Definition.Section != section || o.Entry.Definition.Key != key) continue;
                if (matchExactly ? o.Entry.Value == oldDefault : Math.Abs(o.Entry.Value - oldDefault) < 0.001f)
                { o.Entry.Value = newDefault; set(newDefault); }
                return;
            }
        }

    }
}
