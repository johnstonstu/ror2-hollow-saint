using System;
using System.Collections.Generic;

// In-memory persistence and presentation adapters. Configuration, tuning and
// migrations are linked production sources; no installed BepInEx config is used.
namespace BepInEx.Configuration
{
    public record ConfigDefinition(string Section, string Key);
    public class ConfigEntry<T>
    {
        public ConfigDefinition Definition;
        public T DefaultValue;
        private T value;
        public T Value { get => value; set { this.value = value; SettingChanged?.Invoke(this, EventArgs.Empty); } }
        public event EventHandler SettingChanged;
    }
    public class ConfigFile
    {
        public readonly Dictionary<ConfigDefinition, object> Saved = new();
        public readonly Dictionary<ConfigDefinition, object> Entries = new();
        public int Saves;
        public ConfigEntry<T> Bind<T>(string section, string key, T value, string description) =>
            Bind(new ConfigDefinition(section, key), value);
        public ConfigEntry<T> Bind<T>(ConfigDefinition key, T value)
        {
            if (Entries.TryGetValue(key, out var existing)) return (ConfigEntry<T>)existing;
            var entry = new ConfigEntry<T> { Definition = key, DefaultValue = value,
                Value = Saved.TryGetValue(key, out var saved) ? (T)saved : value };
            Entries.Add(key, entry);
            entry.SettingChanged += (_, _) => Saved[key] = entry.Value;
            return entry;
        }
        public void Remove(ConfigDefinition key) { Entries.Remove(key); Saved.Remove(key); }
        public void Save() { Saves++; }
        public T Read<T>(string section, string key) => ((ConfigEntry<T>)Entries[new(section, key)]).Value;
    }
}
namespace UnityEngine { public static class Mathf { public static float Clamp01(float x) => Math.Clamp(x, 0f, 1f); } }
namespace HollowSaint
{
    public static class FoundationBody
    {
        public static float BaseMoveSpeed = 7, SprintMultiplier = 1.45f, BaseJumpPower = 15;
        public static bool ItemDisplays = true;
        public static int MovementUpdates;
        public static void ApplyMovement() { MovementUpdates++; }
    }
    public static class FoundationArmPose
    {
        public static bool LifeEnabled = true;
        public static float LifeIntensity = 1, ReactionIntensity = 1, AirIntensity = 1, IdleSwayIntensity = 1, FollowThrough = 1;
    }
    public static class FoundationAimPose { public static bool Enabled = true; }
    public static class FoundationHopoo { public static bool Enabled, OverlaysOnStandard = true; }
    public static class HsLog { public static bool Verbose; }
}
namespace HollowSaint.FoundationKit
{
    public static class KitDescriptions { public static int Refreshes; public static void Refresh() { Refreshes++; } }
}
namespace HollowSaint.FoundationKit.Vfx { public static class ImpactFeelSettings { public static bool Enabled = true; } }
namespace HollowSaint.FoundationKit.OpenCircuit { public static class OpenCircuitTuning { public static bool CooldownAfterCrown = true; } }
namespace HollowSaint.FoundationKit.SpearDischarge
{
    public enum SpearHand { Auto, Left, Right }
    public static class SpearCarry { public static SpearHand HandMode; }
}
