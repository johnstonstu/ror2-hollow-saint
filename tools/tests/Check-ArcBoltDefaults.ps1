# Success: fresh/default configs use 1.2, migration 11 preserves custom floats
# (including adjacent representable values around 1), and repeat runs are stable.
# Compiles the production tuning, bind helper and migration step with an in-memory
# config adapter. This is an offline config check, not BepInEx persistence or gameplay QA.
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$shared = [IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\FoundationKit\KitShared.cs'))
$config = [IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\FoundationKit\KitConfig.cs'))
function Get-Block([string]$source, [string]$start) {
    $index = $source.IndexOf($start, [StringComparison]::Ordinal)
    if ($index -lt 0) { throw "Missing source block: $start" }
    $open = $source.IndexOf('{', $index)
    $depth = 1
    $end = $open + 1
    while ($depth -gt 0 -and $end -lt $source.Length) {
        if ($source[$end] -eq '{') { $depth++ }
        if ($source[$end] -eq '}') { $depth-- }
        $end++
    }
    if ($depth -ne 0) { throw "Unclosed source block: $start" }
    return $source.Substring($index, $end - $index)
}
$tuning = Get-Block $shared 'public static partial class KitTuning'
$migrate = Get-Block $config 'private static void Migrate('
$bind = Get-Block $config 'private static void F('
$step = Get-Block $config 'if (defaultsVersion.Value < 11)'
$version = [regex]::Match($config, 'private const int CurrentDefaultsVersion = \d+;').Value
$damageBind = [regex]::Match($config, 'F\(c, bolt, "Damage",[^\r\n]+').Value
$stamp = [regex]::Match($config, 'if \(defaultsVersion.Value < CurrentDefaultsVersion\) defaultsVersion.Value = CurrentDefaultsVersion;').Value
if (!$version -or !$damageBind -or !$stamp) { throw 'Missing production version, damage binding or version stamp.' }
$harness = @'
public static class ArcBoltDefaultsChecks {
    private sealed class Definition { public string Section, Key; }
    private sealed class ConfigEntry<T> {
        public Definition Definition;
        public T DefaultValue;
        private T value;
        public T Value { get { return value; } set { this.value = value; if (SettingChanged != null) SettingChanged(this, EventArgs.Empty); } }
        public event EventHandler SettingChanged;
    }
    private sealed class ConfigFile {
        public float? Saved;
        public ConfigEntry<float> Entry;
        public ConfigEntry<float> Bind(string section, string key, float def, string description) {
            Entry = new ConfigEntry<float> { Definition = new Definition { Section = section, Key = key }, DefaultValue = def, Value = Saved ?? def };
            return Entry;
        }
    }
    private struct FloatOption { public ConfigEntry<float> Entry; public float Min, Max, Step; public bool Restart; }
    private static readonly List<FloatOption> Floats = new List<FloatOption>();
    private static class KitDescriptions { public static void Refresh() {} }
    private static void Check(bool ok, string reason) { if (!ok) throw new Exception(reason); }
    private static void Case(float? saved, int version, float expected, int expectedVersion) {
        KitTuning.ArcBoltDamageCoefficient = FreshDefault;
        Floats.Clear();
        const string bolt = "1. Arc Bolt";
        var c = new ConfigFile { Saved = saved };
        var defaultsVersion = new ConfigEntry<int> { Value = version };
        // Production binding, migration gate and marker update are inserted here.
        BIND
        MIGRATION
        STAMP
        Check(c.Entry.DefaultValue == 1.2f, "Binding default differs from static default");
        Check(c.Entry.Value == expected, "Saved coefficient changed incorrectly: " + saved);
        Check(KitTuning.ArcBoltDamageCoefficient == expected, "Live coefficient differs from saved value");
        Check(defaultsVersion.Value == expectedVersion, "Wrong defaults marker");
        MIGRATION
        STAMP
        Check(c.Entry.Value == expected && KitTuning.ArcBoltDamageCoefficient == expected, "Repeat migration changed value");
        Check(defaultsVersion.Value == expectedVersion, "Repeat migration changed version");
        c.Entry.Value = 1.75f;
        Check(KitTuning.ArcBoltDamageCoefficient == 1.75f, "Live config callback no longer works");
    }
    private static readonly float FreshDefault = KitTuning.ArcBoltDamageCoefficient;
    public static string Run() {
        Check(FreshDefault == 1.2f && CurrentDefaultsVersion == 13, "Incorrect new defaults");
        Case(null, 1, 1.2f, CurrentDefaultsVersion);
        Case(1f, 1, 1.2f, CurrentDefaultsVersion);
        Case(1f, 10, 1.2f, CurrentDefaultsVersion);
        foreach (float custom in new[] { 0.75f, 0.9995f, 0.99999994f, 1.00000012f, 1.0005f, 1.2f, 1.5f }) Case(custom, 10, custom, CurrentDefaultsVersion);
        Case(1f, 11, 1f, CurrentDefaultsVersion);
        Case(1f, 12, 1f, CurrentDefaultsVersion);
        Check(KitTuning.ArcBoltInterval == 0.5f, "Cadence changed");
        Check(KitTuning.ArcBoltProcCoefficient == 0.8f && KitTuning.ArcBoltChainProc == 0.4f, "Proc coefficients changed");
        Check(KitTuning.ArcBoltMaxChainTargets == 4 && KitTuning.ArcBoltChainRange == 12f && KitTuning.ArcBoltChainFalloff == 0.75f, "Chain tuning changed");
        return "ARC_BOLT_DEFAULTS_PASS: 12 config cases; fresh/binding defaults, exact old-default migration, nearby custom values, repeat/version guards and live callbacks. Cadence/procs/chain tuning unchanged. Offline adapter only.";
    }
'@
$harness = $harness.Replace('BIND', $damageBind).Replace('MIGRATION', $step).Replace('STAMP', $stamp)
$source = "using System; using System.Collections.Generic; namespace HollowSaint.FoundationKit {`n" + $tuning + "`n" + $harness + "`n" + $version + "`n" + $migrate + "`n" + $bind + "`n} }"
Add-Type -TypeDefinition $source
[HollowSaint.FoundationKit.ArcBoltDefaultsChecks]::Run()
