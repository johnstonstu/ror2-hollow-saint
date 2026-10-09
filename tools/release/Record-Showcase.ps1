# Records README footage: launches the Hollow Saint Dev profile with the dev autopilot's showcase script
# (HS_SEGMENTS=showcase) and captures ONLY the game window (ffmpeg gfxcapture, Windows Graphics Capture),
# so nothing else on screen is recorded even when the game is behind other windows. Then cuts every
# "CLIP name start|end utc=..." pair in the trace into <name>.mp4 (1280x720) and <name>.gif (640 wide)
# under artifacts\<Name>\clips. Stage the build first (tools\dev-profile\Stage-Build.ps1).
# Usage: powershell -ExecutionPolicy Bypass -File tools\release\Record-Showcase.ps1 -Name showcase07
#        add -CutOnly to re-cut an existing recording (raw.mp4 + trace.txt) without launching the game.
#        -Quick gaze -Skin 4 records only the Gaze takes, on skin 4 (Umbral).
param([Parameter(Mandatory=$true)][string]$Name, [int]$TimeoutSeconds = 300, [double]$Pad = 0.4,
      [string]$ProfileName = 'Hollow Saint Dev', [switch]$CutOnly, [string]$Quick, [string]$Skin,
      [string]$Segments = 'showcase', [switch]$SkipGifs)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$out = Join-Path $repo ("artifacts\" + $Name)
$clips = Join-Path $out 'clips'
New-Item -ItemType Directory -Force $clips | Out-Null
$raw = Join-Path $out 'raw.mp4'
$progress = Join-Path $out 'ffmpeg-progress.txt'
$inv = [Globalization.CultureInfo]::InvariantCulture
$roundtrip = [Globalization.DateTimeStyles]::RoundtripKind
$recStart = $null

if (-not $CutOnly) {
    if (Test-Path -LiteralPath $raw) { throw 'Recording already exists; use a fresh Name or -CutOnly to preserve previous footage.' }
    $game = 'C:/Program Files (x86)/Steam/steamapps/common/Risk of Rain 2'
    $profile = Join-Path $env:APPDATA ('r2modmanPlus-local/RiskOfRain2/profiles/' + $ProfileName)
    $preloader = Join-Path $profile 'BepInEx/core/BepInEx.Preloader.dll'
    $identity = Join-Path $out 'build.json'
    if (Test-Path -LiteralPath $identity) { throw 'Build identity already exists; preserve it and choose a fresh recording Name.' }
    $pluginFiles = Join-Path $profile 'BepInEx/plugins/JohnstonStu-HollowSaint'
    [ordered]@{
        capturedBeforeLaunchUtc = [DateTime]::UtcNow.ToString('o'); profile = $ProfileName; segments = $Segments
        dllSha256 = (Get-FileHash (Join-Path $pluginFiles 'HollowSaint.dll')).Hash
        languageSha256 = (Get-FileHash (Join-Path $pluginFiles 'HollowSaint.language')).Hash
        bundleSha256 = (Get-FileHash (Join-Path $pluginFiles 'hollowsaintassets')).Hash
    } | ConvertTo-Json | Set-Content -LiteralPath $identity -Encoding UTF8
    if (Get-Process -Name 'Risk of Rain 2' -ErrorAction SilentlyContinue) { throw 'A game is already running; close it first' }
    $env:HS_AUTOPILOT = $out
    $env:HS_SEGMENTS = $Segments
    if ($Quick) { $env:HS_SHOWCASE_QUICK = $Quick }
    if ($Skin) { $env:HS_SHOWCASE_SKIN = $Skin }
    $arguments = '--doorstop-enabled true --doorstop-target-assembly "' + $preloader + '" --r2profile "' + $ProfileName + '"'
    $p = Start-Process -FilePath (Join-Path $game 'Risk of Rain 2.exe') -WorkingDirectory $game -ArgumentList $arguments -PassThru
    Remove-Item Env:HS_AUTOPILOT, Env:HS_SEGMENTS, Env:HS_SHOWCASE_QUICK, Env:HS_SHOWCASE_SKIN -ErrorAction SilentlyContinue

    # The game window, by handle, once it exists.
    $hwnd = 0
    for ($i = 0; $i -lt 120 -and $hwnd -eq 0; $i++) {
        Start-Sleep -Milliseconds 500
        $p.Refresh()
        if ($p.HasExited) { throw 'game exited before its window appeared' }
        $hwnd = [int64]$p.MainWindowHandle
    }
    if ($hwnd -eq 0) { Stop-Process -Id $p.Id -Force; throw 'no game window' }
    Start-Sleep -Seconds 2

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = 'ffmpeg'
    $psi.Arguments = '-hide_banner -loglevel error -y -f lavfi -i "gfxcapture=hwnd=' + $hwnd + ':capture_cursor=0:max_framerate=30:resize_mode=scale_aspect,hwdownload,format=bgra" ' +
        '-vf "fps=30,scale=1920:1080:force_original_aspect_ratio=decrease:flags=lanczos,pad=1920:1080:-1:-1,format=yuv420p" -c:v libx264 -preset veryfast -crf 18 ' +
        '-progress "' + $progress + '" "' + $raw + '"'
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardError = $true
    $ff = [System.Diagnostics.Process]::Start($psi)
    $errTask = $ff.StandardError.ReadToEndAsync()

    # Rough recording start (wall clock minus ffmpeg's output time); only narrows the sync-flash search.
    $last = $null
    for ($i = 0; $i -lt 30 -and -not $last -and -not $ff.HasExited; $i++) {
        Start-Sleep -Milliseconds 500
        $now = [DateTime]::UtcNow
        $last = (Get-Content $progress -ErrorAction SilentlyContinue | Select-String '^out_time_us=(\d+)' | Select-Object -Last 1)
    }
    if (-not $last -or $ff.HasExited) { Stop-Process -Id $p.Id -Force; throw ('ffmpeg did not start: ' + $errTask.Result) }
    $recStart = $now.AddTicks(-[int64]$last.Matches[0].Groups[1].Value * 10)

    if (-not $p.WaitForExit($TimeoutSeconds * 1000)) { Stop-Process -Id $p.Id -Force; "TIMEOUT: game stopped after $TimeoutSeconds s" }
    if (-not $ff.HasExited) { $ff.StandardInput.Write('q'); $ff.StandardInput.Flush() }
    if (-not $ff.WaitForExit(30000)) { $ff.Kill() }
    $log = Join-Path $profile 'BepInEx/LogOutput.log'
    if (Test-Path $log) { Copy-Item $log (Join-Path $out 'LogOutput.log') -Force }
    "recording=" + $raw
}
if (-not (Test-Path $raw)) { throw "no recording at $raw" }
$trace = Join-Path $out 'trace.txt'
if (-not (Test-Path $trace)) { throw 'no trace written' }
Get-Content $trace -TotalCount 1

# Sync: the showcase flashes the screen white at the logged "SYNC utc=" time; the first bright frame in
# the recording pins recording time to wall-clock time.
$sync = Get-Content $trace | Select-String 'SYNC utc=(\S+)' | Select-Object -First 1
if (-not $sync) { throw 'no SYNC line in the trace' }
$syncUtc = [DateTime]::Parse($sync.Matches[0].Groups[1].Value, $inv, $roundtrip).ToUniversalTime()
[double]$from = 0.0; [double]$span = 180.0
if ($recStart) { $from = [Math]::Max(0.0, ($syncUtc - $recStart).TotalSeconds - 10.0); $span = 25.0 }
Push-Location $out
try { & ffmpeg -hide_banner -loglevel error -y -ss $from.ToString('0.000', $inv) -t $span.ToString('0', $inv) -i raw.mp4 -vf 'scale=64:36,signalstats,metadata=print:key=lavfi.signalstats.YAVG:file=yavg.txt' -f null - }
finally { Pop-Location }
$pts = 0.0; $flash = $null
foreach ($line in Get-Content (Join-Path $out 'yavg.txt')) {
    if ($line -match 'pts_time:([\d\.]+)') { $pts = [double]::Parse($Matches[1], $inv) }
    elseif ($null -eq $flash -and $line -match 'YAVG=([\d\.]+)' -and [double]::Parse($Matches[1], $inv) -gt 225) { $flash = $from + $pts }
}
if ($null -eq $flash) { throw 'sync flash not found in the recording' }
$recStart = $syncUtc.AddSeconds(-$flash)
[ordered]@{ videoStartUtc = $recStart.ToString('o'); syncUtc = $syncUtc.ToString('o'); syncFrameSeconds = $flash } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'recording.json') -Encoding UTF8
"sync flash at " + $flash.ToString('0.000', $inv) + "s"

$stills = Join-Path $out 'stills'
New-Item -ItemType Directory -Force $stills | Out-Null
foreach ($line in Get-Content $trace) {
    if ($line -match 'STILL (\S+) utc=(\S+)') {
        $t = ([DateTime]::Parse($Matches[2], $inv, $roundtrip).ToUniversalTime() - $recStart).TotalSeconds
        & ffmpeg -hide_banner -loglevel error -y -ss $t.ToString('0.000', $inv) -i $raw -frames:v 1 (Join-Path $stills ($Matches[1] + '.png'))
        "still " + $Matches[1] + " at " + $t.ToString('0.00', $inv) + "s"
    }
}

$starts = @{}
# Where each clip sits in raw.mp4 (Make-ReadmeMedia's hero HQ cuts read it to encode from the raw recording).
$offsets = Join-Path $clips 'offsets.txt'
Set-Content -Path $offsets -Value '# clip start_s length_s (in raw.mp4)' -Encoding ASCII
foreach ($line in Get-Content $trace) {
    if ($line -match 'CLIP (\S+) (start|end) utc=(\S+)') {
        $t = ([DateTime]::Parse($Matches[3], $inv, $roundtrip).ToUniversalTime() - $recStart).TotalSeconds
        if ($Matches[2] -eq 'start') { $starts[$Matches[1]] = $t; continue }
        [double]$at = $starts[$Matches[1]] - $Pad
        if ($at -lt 0) { $at = 0.0 }
        [double]$len = $t - $at + $Pad
        $ss = $at.ToString('0.000', $inv); $tt = $len.ToString('0.000', $inv)
        Add-Content -Path $offsets -Value ($Matches[1] + ' ' + $ss + ' ' + $tt) -Encoding ASCII
        $mp4 = Join-Path $clips ($Matches[1] + '.mp4'); $gif = Join-Path $clips ($Matches[1] + '.gif')
        & ffmpeg -hide_banner -loglevel error -y -ss $ss -t $tt -i $raw -vf 'scale=1280:720:flags=lanczos' -c:v libx264 -preset slow -crf 22 -pix_fmt yuv420p -movflags +faststart -an $mp4
        if (-not $SkipGifs) {
            & ffmpeg -hide_banner -loglevel error -y -ss $ss -t $tt -i $raw -vf 'fps=15,scale=640:-1:flags=lanczos,split[a][b];[a]palettegen=max_colors=128:stats_mode=diff[p];[b][p]paletteuse=dither=bayer:bayer_scale=4:diff_mode=rectangle' $gif
        }
        "{0,-12} at {1,8}s  {2,5:0.0}s  mp4={3:0} KB" -f $Matches[1], $ss, $len, ((Get-Item $mp4).Length / 1KB)
    }
}
