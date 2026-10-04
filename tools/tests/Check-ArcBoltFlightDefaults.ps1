# Offline success: speed migration 13 changes only exact legacy80, preserves custom
# and adjacent values, stays stable on rerun and retains production bind callbacks.
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$shared = [IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\FoundationKit\KitShared.cs'))
$config = [IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\FoundationKit\KitConfig.cs'))
function Block([string]$source, [string]$marker) {
    $start = $source.IndexOf($marker, [StringComparison]::Ordinal)
    if ($start -lt 0) { throw "Missing production source: $marker" }
    $open = $source.IndexOf('{', $start)
    $depth = 1
    $end = $open + 1
    while ($depth -gt 0 -and $end -lt $source.Length) {
        if ($source[$end] -eq '{') { $depth++ }
        if ($source[$end] -eq '}') { $depth-- }
        $end++
    }
    if ($depth -ne 0) { throw 'Unclosed production source.' }
    $source.Substring($start, $end - $start)
}
$tuning = Block $shared 'public static partial class KitTuning'
$migration = Block $config 'if (defaultsVersion.Value < 13)'
$migrate = Block $config 'private static void Migrate('
$bind = Block $config 'private static void F('
$binding = [regex]::Match($config, 'F\(c, bolt, "Projectile speed",[^\r\n]+').Value
$version = [regex]::Match($config, 'private const int CurrentDefaultsVersion = \d+;').Value
if (!$binding -or !$version) { throw 'Missing production binding or version.' }
$harness = @'
public static class ArcBoltFlightDefaultsChecks {
    private sealed class Definition { public string Section, Key; }
    private sealed class ConfigEntry<T> {
        public Definition Definition;
        public T DefaultValue;
        private T value;
        public T Value { get { return value; } set { this.value=value; if(SettingChanged!=null)SettingChanged(this,EventArgs.Empty); } }
        public event EventHandler SettingChanged;
    }
    private sealed class ConfigFile {
        public float? Saved;
        public ConfigEntry<float> Entry;
        public ConfigEntry<float> Bind(string section,string key,float def,string description) {
            Entry=new ConfigEntry<float>{Definition=new Definition{Section=section,Key=key},DefaultValue=def,Value=Saved??def};
            return Entry;
        }
    }
    private struct FloatOption { public ConfigEntry<float> Entry; public float Min,Max,Step; public bool Restart; }
    private static readonly List<FloatOption> Floats=new List<FloatOption>();
    private static class KitDescriptions { public static void Refresh(){} }
    private static int cases;
    private static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
    private static readonly float FreshDefault=KitTuning.ArcBoltProjectileSpeed;
    private static void Case(float? saved,int revision,float expected) {
        cases++;
        KitTuning.ArcBoltProjectileSpeed=FreshDefault;Floats.Clear();
        const string bolt="1. Arc Bolt";
        var c=new ConfigFile{Saved=saved};var defaultsVersion=new ConfigEntry<int>{Value=revision};
        BINDING
        MIGRATION
        Check(c.Entry.DefaultValue==120f,"wrong fresh binding");
        Check(c.Entry.Value==expected&&KitTuning.ArcBoltProjectileSpeed==expected,"wrong saved/live migration");
        defaultsVersion.Value=Math.Max(defaultsVersion.Value,CurrentDefaultsVersion);
        MIGRATION
        Check(c.Entry.Value==expected,"repeat migration changed speed");
        c.Entry.Value=135f;
        Check(KitTuning.ArcBoltProjectileSpeed==135f,"production callback stopped working");
    }
    public static string Run(){
        Check(FreshDefault==120f&&KitTuning.ArcBoltRadius==.75f,"wrong static flight defaults");
        Check(CurrentDefaultsVersion==13,"wrong migration marker");
        Case(null,12,120f);Case(80f,12,120f);Case(80f,0,120f);
        foreach(float custom in new[]{30f,79.99999f,80.00001f,100f,120f,150f,200f})Case(custom,12,custom);
        Case(80f,13,80f);Case(80f,14,80f);
        Check(KitTuning.ArcBoltDamageCoefficient==1.2f&&KitTuning.ArcBoltInterval==.5f,"damage/cadence changed");
        Check(KitTuning.ArcBoltProcCoefficient==.8f&&KitTuning.ArcBoltChainProc==.4f,"procs changed");
        return "ARC_BOLT_FLIGHT_DEFAULTS_PASS: "+cases+" production bind/migration cases; exact defaults, custom/adjacent values, repeat and callbacks.";
    }
VERSION
MIGRATE
BIND
}
'@
$harness = $harness.Replace('BINDING', $binding).Replace('MIGRATION', $migration).Replace('VERSION', $version).Replace('MIGRATE', $migrate).Replace('BIND', $bind)
$source = "using System; using System.Collections.Generic; namespace HollowSaint.FoundationKit {`n" + $tuning + "`n" + $harness + "`n}"
Add-Type -TypeDefinition $source
[HollowSaint.FoundationKit.ArcBoltFlightDefaultsChecks]::Run()
