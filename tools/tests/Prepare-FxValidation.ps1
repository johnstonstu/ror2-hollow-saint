# Copies actual production VFX into an isolated preview assembly. Game bindings
# are replaced by explicit preview adapters; shaders are preview approximations.
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
# Legacy fixtures still compile SpearAimPose, removed before the 1.2 release.
# Check the compatibility prerequisite before creating/updating ANY RuntimeCopies.
# Existing ignored copies can belong to another branch and are not valid evidence.
$legacyPose = Join-Path $repo 'HollowSaintMod\FoundationKit\SpearDischarge\SpearAimPose.cs'
if (-not (Test-Path -LiteralPath $legacyPose -PathType Leaf)) {
    Write-Output 'FX_VALIDATION_UNAVAILABLE: legacy fixtures require removed SpearAimPose. Port the fixtures to the 1.2 Stormspear/pose API before generating previews. No copies written. See docs/dev/README.md.'
    exit 2
}
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
$spearNames = @('SpearAimPose')
foreach ($name in $spearNames) {
    $source = Join-Path $repo "HollowSaintMod\FoundationKit\SpearDischarge\$name.cs"
    Save-Copy (Join-Path $dest "$name.cs") ([IO.File]::ReadAllText($source))
}
Save-Copy (Join-Path $dest 'SpearCarry.cs') ([IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\FoundationKit\Stormspear\SpearCarry.cs')))
Save-Copy (Join-Path $dest 'FoundationMotionPose.cs') ([IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\Character\Animation\FoundationMotionPose.cs')))
foreach ($name in @('FoundationArmPose','FoundationArmLifeMath')) {
    Save-Copy (Join-Path $dest "$name.cs") ([IO.File]::ReadAllText((Join-Path $repo "HollowSaintMod\Character\Animation\$name.cs")))
}
foreach ($name in @('FoundationAnimRules','FoundationLocomotionMath')) {
    Save-Copy (Join-Path $dest "$name.cs") ([IO.File]::ReadAllText((Join-Path $repo "HollowSaintMod\Character\Animation\$name.cs")))
}
# Keep the earlier pose fixtures' three-field presentation adapter. The production
# controller runs alongside it under a distinct preview class name and fixed frame
# clock; no decisions/transitions are replaced. The runner explicitly bridges its
# three outputs into the pose adapter each frame.
$presentation = [IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\Character\Animation\FoundationPresentation.cs'))
$presentation = $presentation.Replace('public sealed class FoundationPresentation :', 'public sealed class LivePresentation :').Replace('Time.deltaTime', 'PreviewFrame.DeltaTime')
Save-Copy (Join-Path $dest 'LivePresentation.cs') $presentation
$shared = [IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\Character\Animation\KitAnim.cs'))
$gestureStart = $shared.IndexOf('public static bool MoveGesture(')
$gestureEnd = $shared.IndexOf('public static Animator AnimatorOf(', $gestureStart)
if ($gestureStart -lt 0 -or $gestureEnd -lt 0) { throw 'MoveGesture method boundaries changed' }
$gesture = $shared.Substring($gestureStart, $gestureEnd - $gestureStart)
Save-Copy (Join-Path $dest 'GestureTransfer.cs') ('using UnityEngine;' + "`n" + 'namespace HollowSaint.FoundationKit { public static partial class KitAnim { private static readonly int EmptyHash = Animator.StringToHash("Empty");' + "`n" + $gesture + '} }')
$skin = [IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\Character\Appearance\FoundationSkin.cs'))
$first = $skin.IndexOf('private static Material ObsidianTint(')
$last = $skin.IndexOf('/// <summary>RoR2-style', $first)
if ($first -lt 0 -or $last -lt 0) { throw 'Skin tint method boundaries changed' }
$methods = $skin.Substring($first, $last - $first)
$copy = "using UnityEngine;`nnamespace HollowSaint.PreviewValidation { public static class ModelTints {`npublic static Material Apply(Material source, uint skin) => skin == 0 ? source : skin == 1 ? ObsidianTint(source) : RelicTint(source, (int)skin - 2);`n$methods`n} }"
Save-Copy (Join-Path $dest 'ModelTints.cs') $copy
foreach ($name in @('FoundationLightMaps','FoundationMaterials')) {
    Save-Copy (Join-Path $dest "$name.cs") ([IO.File]::ReadAllText((Join-Path $repo "HollowSaintMod\Character\Appearance\$name.cs")))
}
Save-Copy (Join-Path $dest 'FoundationSkinAnimation.cs') ([IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\Character\Animation\FoundationSkinAnimation.cs')))
"Prepared $($names.Count + $spearNames.Count + 8) exact runtime copies, fixed-clock presentation source, gesture transfer and model tint methods."
