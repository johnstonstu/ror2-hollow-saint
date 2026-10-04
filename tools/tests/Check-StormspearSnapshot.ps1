param([string]$GameAssembly = 'C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed\RoR2.dll')
# Success: actual shot helper preserves charge and crown identity through float/byte
# transport, overlapping throws and owner changes; actual engine and source wiring
# still carry that snapshot to detonation. This is not a Unity multiplayer playtest.
# Run with pwsh: the production immutable structs require its modern C# compiler.
$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$folder = Join-Path $repo 'HollowSaintMod\FoundationKit\Stormspear'
$shot = Get-Content (Join-Path $folder 'StormspearShot.cs') -Raw
$tuning = Get-Content (Join-Path $folder 'StormspearTuning.cs') -Raw
$stub = @'
namespace UnityEngine { public static class Mathf { public static float Clamp01(float v) { return v < 0f ? 0f : (v > 1f ? 1f : v); } } }
'@
$checks = @'
namespace HollowSaint.FoundationKit.Stormspear {
 public static class SnapshotChecks {
  static int count;
  static void Check(bool ok, string why) { count++; if (!ok) throw new System.Exception(why); }
  static bool Near(float a, float b) { return System.Math.Abs(a-b) < 0.00001f; }
  // NetworkWriter.Write(float) is lossless IEEE single. PackedUInt32 encodes
  // our combo values 0 and 1 as one byte. Full engine field plumbing is checked below.
  static StormspearShot Transport(float charge, bool crown) {
   using (var stream = new System.IO.MemoryStream()) {
    var writer = new System.IO.BinaryWriter(stream);
    writer.Write(StormspearShot.ForceForCharge(charge));
    writer.Write(crown ? StormspearShot.CrownCombo : (byte)0);
    stream.Position = 0;
    var reader = new System.IO.BinaryReader(stream);
    return new StormspearShot(reader.ReadSingle(), reader.ReadByte());
   }
  }
  public static string Run() {
   foreach (float charge in new[] {0f, .01f, .125f, .3333f, .5f, .969f, .971f, 1f})
    foreach (bool crown in new[] {false,true}) {
     var local = new StormspearShot(StormspearShot.ForceForCharge(charge), crown ? StormspearShot.CrownCombo : (byte)0);
     var remote = Transport(charge,crown);
     Check(Near(remote.Charge, charge), "remote charge precision");
     Check(Near(local.Charge,remote.Charge) && local.FromCrown == remote.FromCrown,"host/remote snapshot parity");
     Check(Near(StormspearShot.ForceForCharge(charge),8f+12f*charge),"knockback changed");
    }
   var fullCrown = Transport(1f,true);
   var fullHand = Transport(1f,false);
   // Delayed detonation and overlapping throws must use the original shot, not latest owner form.
   var laterHand = Transport(.2f,false);
   Check(fullCrown.CallsThunderbolt(true,1f),"crown throw lost bonus after crown closed / later hand throw");
   Check(!fullHand.CallsThunderbolt(true,1f),"hand throw gained bonus after crown opened");
   Check(!laterHand.CallsThunderbolt(true,1f),"partial hand throw gained bonus");
   Check(!fullCrown.CallsThunderbolt(false,1f),"disabled bonus ignored");
   Check(!Transport(.969f,true).CallsThunderbolt(true,1f),"undercharge bonus");
   Check(Transport(.971f,true).CallsThunderbolt(true,1f),"existing charge tolerance lost");
   float launchedDamage = StormspearTuning.DamageAt(1f)*15f;
   foreach (float laterOwnerDamage in new[] {12f,17.4f,30f}) {
    Check(Near(fullCrown.Charge,1f),"owner stat change corrupted charge");
    Check(Near(StormspearTuning.BurstRadiusAt(fullCrown.Charge),10f),"owner stat change corrupted radius");
    float fraction = StormspearTuning.BurstDamageFraction + (StormspearTuning.BurstDamageFractionFull-StormspearTuning.BurstDamageFraction)*fullCrown.Charge;
    Check(Near(launchedDamage*fraction,launchedDamage),"owner stat change corrupted burst damage");
   }
   // Demonstrate the precise previous regression, so the scenario is not vacuous.
   Check(StormspearTuning.ChargeFromCoefficient(launchedDamage/17.4f) < .97f,"old level-up regression scenario no longer discriminates");
   Check(fullCrown.CallsThunderbolt(true,1f),"level-up lost bonus");
   float savedTap = StormspearTuning.TapDamage, savedFull = StormspearTuning.FullDamage;
   StormspearTuning.TapDamage = 9f; StormspearTuning.FullDamage = 30f;
   Check(Near(fullCrown.Charge,1f) && Near(laterHand.Charge,.2f),"damage curve change corrupted in-flight charge");
   Check(fullCrown.CallsThunderbolt(true,1f),"damage curve change lost crown eligibility");
   StormspearTuning.TapDamage = savedTap; StormspearTuning.FullDamage = savedFull;
   return "STORMSPEAR_SNAPSHOT_PASS: " + count + " behavioral assertions; transport precision, host/remote parity, overlapping forms, unchanged knockback, stat-change radius/burst, eligibility boundaries.";
  }
 }
}
'@
Add-Type -TypeDefinition ($shot + [Environment]::NewLine + $stub + $tuning + $checks)
[HollowSaint.FoundationKit.Stormspear.SnapshotChecks]::Run()
$engine = (& ilspycmd -t RoR2.Projectile.ProjectileManager $GameAssembly) -join [Environment]::NewLine
if ($LASTEXITCODE -ne 0) { throw 'Could not inspect installed ProjectileManager' }
foreach ($pattern in @('writer.Write(force);','writer.WritePackedUInt32(comboNumber);','force = reader.ReadSingle();','comboNumber = (byte)reader.ReadPackedUInt32();','fireMsg.force = fireProjectileInfo.force;','fireMsg.comboNumber = fireProjectileInfo.comboNumber;','force = fireMsg.force,','comboNumber = fireMsg.comboNumber,','projectileController.Networkcombo = fireProjectileInfo.comboNumber;','component.force = fireProjectileInfo.force;','projectileController.DispatchOnInitialized();')) {
 if (!$engine.Contains($pattern)) { throw "Actual engine transport changed: $pattern" }
}
$init = $engine.Substring($engine.IndexOf('private static void InitializeProjectile('))
if ($init.IndexOf('component.force = fireProjectileInfo.force;') -gt $init.IndexOf('projectileController.DispatchOnInitialized();')) { throw 'Snapshot fires before fields initialized' }
$impact = Get-Content (Join-Path $folder 'StormspearProjectile.cs') -Raw
$throw = Get-Content (Join-Path $folder 'StormspearThrowState.cs') -Raw
if ($impact.Contains('directDamage / body.damage') -or $impact.Contains('StormspearCharge.InCrown(body)')) { throw 'Impact still uses mutable owner state' }
foreach ($pattern in @('onInitialized += CaptureShot','projectileDamage.force','controller.combo','crit, shot, struck','d.shot = shot','shot.CallsThunderbolt')) {
 if (!$impact.Contains($pattern)) { throw "Snapshot wiring missing: $pattern" }
}
if (!$impact.Contains('conductorShot = new SpearConductorSchedule(') -or
    !$impact.Contains('SpearConductor.Begin(body, struck, anchor, point, crit, conductorShot)')) {
 throw 'Conductor initialization snapshot or immutable impact handoff missing'
}
$conductor = Get-Content (Join-Path $folder 'SpearConductor.cs') -Raw
if ($conductor.Contains('StormspearTuning.DamageAt(') -or !$conductor.Contains('conductor.schedule = snapshot;')) {
 throw 'Conductor must reuse initialization snapshot without impact-time normalization'
}
if (!$throw.Contains('comboNumber = form == SpearForm.Crown') -or !$throw.Contains('force = StormspearShot.ForceForCharge(charge)')) { throw 'Throw does not transmit snapshot' }
'STORMSPEAR_WIRING_PASS: actual installed engine serialization and initialization ordering, immutable impact/detonation wiring.'
