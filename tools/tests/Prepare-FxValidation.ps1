# Copies actual production VFX into an isolated preview assembly. Game bindings
# are replaced by explicit preview adapters; shaders are preview approximations.
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$dest = Join-Path $repo 'HollowSaintUnityProject\Assets\HollowSaint\FxValidation\RuntimeCopies'
New-Item -ItemType Directory -Force $dest | Out-Null
$utf8 = [Text.UTF8Encoding]::new($false)
function Save-Copy([string]$path, [string]$content) {
    if (Test-Path -LiteralPath $path) {
        $before = [IO.File]::ReadAllText($path)
        if ($before -eq $content) { return }
    }
    [IO.File]::WriteAllText($path, $content, $utf8)
}
$names = @('SkinFxPalette','LightningLine','LightningRhythm','AbilityCurrentWindow','ArmCurrentPath','BodyCurrentFx','HaloRing','HaloRingShape','SpearVisual','CircuitCrownFx','DischargeLink','VictimFxTheme')
foreach ($name in $names) {
    $source = Join-Path $repo "HollowSaintMod\FoundationKit\Vfx\$name.cs"
    Save-Copy (Join-Path $dest "$name.cs") ([IO.File]::ReadAllText($source))
}
$spearNames = @('SpearAimPose','SpearCarry')
foreach ($name in $spearNames) {
    $source = Join-Path $repo "HollowSaintMod\FoundationKit\SpearDischarge\$name.cs"
    Save-Copy (Join-Path $dest "$name.cs") ([IO.File]::ReadAllText($source))
}
Save-Copy (Join-Path $dest 'FoundationMotionPose.cs') ([IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\FoundationMotionPose.cs')))
foreach ($name in @('FoundationArmPose','FoundationArmLifeMath')) {
    Save-Copy (Join-Path $dest "$name.cs") ([IO.File]::ReadAllText((Join-Path $repo "HollowSaintMod\$name.cs")))
}
foreach ($name in @('FoundationAnimRules','FoundationLocomotionMath')) {
    Save-Copy (Join-Path $dest "$name.cs") ([IO.File]::ReadAllText((Join-Path $repo "HollowSaintMod\$name.cs")))
}
# Keep the earlier pose fixtures' three-field presentation adapter. The production
# controller runs alongside it under a distinct preview class name and fixed frame
# clock; no decisions/transitions are replaced. The runner explicitly bridges its
# three outputs into the pose adapter each frame.
$presentation = [IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\FoundationPresentation.cs'))
$presentation = $presentation.Replace('public sealed class FoundationPresentation :', 'public sealed class LivePresentation :').Replace('Time.deltaTime', 'PreviewFrame.DeltaTime')
Save-Copy (Join-Path $dest 'LivePresentation.cs') $presentation
$shared = [IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\FoundationKit\KitShared.cs'))
$gestureStart = $shared.IndexOf('public static bool MoveGesture(')
$gestureEnd = $shared.IndexOf('public static Animator AnimatorOf(', $gestureStart)
if ($gestureStart -lt 0 -or $gestureEnd -lt 0) { throw 'MoveGesture method boundaries changed' }
$gesture = $shared.Substring($gestureStart, $gestureEnd - $gestureStart)
Save-Copy (Join-Path $dest 'GestureTransfer.cs') ('using UnityEngine;' + "`n" + 'namespace HollowSaint.FoundationKit { public static partial class KitAnim { private static readonly int EmptyHash = Animator.StringToHash("Empty");' + "`n" + $gesture + '} }')
$skin = [IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\FoundationSkin.cs'))
$first = $skin.IndexOf('private static Material ObsidianTint(')
$last = $skin.IndexOf('/// <summary>RoR2-style', $first)
if ($first -lt 0 -or $last -lt 0) { throw 'Skin tint method boundaries changed' }
$methods = $skin.Substring($first, $last - $first)
$copy = "using UnityEngine;`nnamespace HollowSaint.PreviewValidation { public static class ModelTints {`npublic static Material Apply(Material source, uint skin) => skin == 0 ? source : skin == 1 ? ObsidianTint(source) : RelicTint(source, (int)skin - 2);`n$methods`n} }"
Save-Copy (Join-Path $dest 'ModelTints.cs') $copy
foreach ($name in @('FoundationLightMaps','FoundationMaterials','FoundationSkinAnimation')) {
    Save-Copy (Join-Path $dest "$name.cs") ([IO.File]::ReadAllText((Join-Path $repo "HollowSaintMod\$name.cs")))
}
"Prepared $($names.Count + $spearNames.Count + 8) exact runtime copies, fixed-clock presentation source, gesture transfer and model tint methods."
