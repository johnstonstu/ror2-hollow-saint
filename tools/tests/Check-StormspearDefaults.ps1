# Success: real binding/migration code upgrades only exact old defaults, preserves
# custom/adjacent floats, handles fresh and old configs, and is repeat-safe.
# In-memory adapter only; no installed config is read or written.
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$config = [IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\FoundationKit\KitConfig.cs'))
function Get-Block([string]$source, [string]$start) {
    $index = $source.IndexOf($start, [StringComparison]::Ordinal)
    if ($index -lt 0) { throw "Missing source block: $start" }
    $open = $source.IndexOf('{', $index); $depth = 1; $end = $open + 1
    while ($depth -gt 0 -and $end -lt $source.Length) {
        if ($source[$end] -eq '{') { $depth++ }
        if ($source[$end] -eq '}') { $depth-- }
        $end++
    }
    if ($depth -ne 0) { throw "Unclosed source block: $start" }
    $source.Substring($index, $end - $index)
}
$migration = Get-Block $config 'if (defaultsVersion.Value < 12)'
# Include the historical full-damage migration: fresh 14 must return to 14.
$oldMigration = (Get-Block $config 'if (defaultsVersion.Value < 4)').Replace('Migrate(gaze, "Cooldown", 16f, 12f, v => Gaze.GazeTuning.Cooldown = v);','')
$binds = [regex]::Matches($config, 'F\(c, spear, "(?:Tap|Full) damage",[^\r\n]+') | ForEach-Object Value
$migrate = Get-Block $config 'private static void Migrate('
$bind = Get-Block $config 'private static void F('
$version = [regex]::Match($config, 'private const int CurrentDefaultsVersion = \d+;').Value
$stamp = [regex]::Match($config, 'if \(defaultsVersion.Value < CurrentDefaultsVersion\) defaultsVersion.Value = CurrentDefaultsVersion;').Value
$tuning = [IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\FoundationKit\Stormspear\StormspearTuning.cs'))
$harness = @'
public static class StormspearDefaultsChecks {
 private sealed class Definition { public string Section, Key; }
 private sealed class ConfigEntry<T> {
  public Definition Definition; public T DefaultValue; private T value;
  public T Value { get { return value; } set { this.value=value; if(SettingChanged!=null) SettingChanged(this,EventArgs.Empty); } }
  public event EventHandler SettingChanged;
 }
 private sealed class ConfigFile {
  public float? Tap, Full;
  public Dictionary<string, ConfigEntry<float>> Entries = new Dictionary<string, ConfigEntry<float>>();
  public ConfigEntry<float> Bind(string section,string key,float def,string description) {
   var entry=new ConfigEntry<float> { Definition=new Definition {Section=section,Key=key}, DefaultValue=def, Value=(key=="Tap damage"?Tap:Full)??def };
   Entries.Add(key,entry); return entry;
  }
 }
 private struct FloatOption { public ConfigEntry<float> Entry; public float Min, Max, Step; public bool Restart; }
 private static readonly List<FloatOption> Floats=new List<FloatOption>();
 private static class KitDescriptions { public static void Refresh() {} }
 private static int cases;
 private static void Check(bool ok,string reason) { if(!ok) throw new Exception(reason); }
 private static void Case(float? tap,float? full,int version,float expectedTap,float expectedFull) {
  Stormspear.StormspearTuning.TapDamage=3.5f; Stormspear.StormspearTuning.FullDamage=14f; Floats.Clear();
  const string spear="2. Stormspear";
  var c=new ConfigFile {Tap=tap,Full=full}; var defaultsVersion=new ConfigEntry<int> {Value=version};
  BINDS
  OLDMIGRATION
  MIGRATION
  STAMP
  Check(c.Entries["Tap damage"].DefaultValue==3.5f && c.Entries["Full damage"].DefaultValue==14f,"binding defaults");
  Check(c.Entries["Tap damage"].Value==expectedTap && c.Entries["Full damage"].Value==expectedFull,"saved/custom migration");
  Check(Stormspear.StormspearTuning.TapDamage==expectedTap && Stormspear.StormspearTuning.FullDamage==expectedFull,"live migration");
  Check(defaultsVersion.Value==Math.Max(version,CurrentDefaultsVersion),"version marker");
  MIGRATION
  STAMP
  Check(c.Entries["Tap damage"].Value==expectedTap && c.Entries["Full damage"].Value==expectedFull,"repeat migration");
  c.Entries["Tap damage"].Value=6f; c.Entries["Full damage"].Value=20f;
  Check(Stormspear.StormspearTuning.TapDamage==6f && Stormspear.StormspearTuning.FullDamage==20f,"live callbacks"); cases++;
 }
 public static string Run() {
  Check(CurrentDefaultsVersion==12,"migration version");
  Case(null,null,1,3.5f,14f); Case(4f,14f,1,3.5f,14f);
  Case(4f,16f,10,3.5f,14f); Case(4f,16f,11,3.5f,14f);
  Case(4f,20f,11,3.5f,20f); Case(6f,16f,11,6f,14f);
  foreach(float tap in new[]{3.99999976f,4.00000048f,3.9995f,4.0005f,3.5f,6f}) Case(tap,16f,11,tap,14f);
  foreach(float full in new[]{15.999999f,16.000002f,15.9995f,16.0005f,14f,20f}) Case(4f,full,11,3.5f,full);
  Case(4f,16f,12,4f,16f); Case(4f,16f,13,4f,16f);
  return "STORMSPEAR_DEFAULTS_PASS: "+cases+" cases; fresh/historical/default/custom/adjacent floats, independent entries, repeat/version guards, live callbacks. Offline adapter only.";
 }
 HELPERS
}
'@
$harness = $harness.Replace('BINDS',($binds -join "`n")).Replace('OLDMIGRATION',$oldMigration).Replace('MIGRATION',$migration).Replace('STAMP',$stamp).Replace('HELPERS',($version+"`n"+$migrate+"`n"+$bind))
$stub = 'namespace UnityEngine { public static class Mathf { public static float Clamp01(float v) { return v<0?0:(v>1?1:v); } } }'
Add-Type -TypeDefinition ("using System; using System.Collections.Generic;`n"+$stub+$tuning+"`nnamespace HollowSaint.FoundationKit {"+$harness+'}')
[HollowSaint.FoundationKit.StormspearDefaultsChecks]::Run()
