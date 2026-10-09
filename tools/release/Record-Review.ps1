# Local game-window review recording with WASAPI audio; no upload or release.
param([Parameter(Mandatory=$true)][string]$Name, [string]$Segments = 'early-13',
      [int]$TimeoutSeconds = 300, [string]$Python = 'python', [switch]$MuxOnly)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$out = Join-Path $repo ('artifacts\' + $Name)
if (-not $MuxOnly) {
if (Test-Path -LiteralPath $out) { throw 'Choose a new recording Name to preserve existing evidence.' }
New-Item -ItemType Directory -Path $out | Out-Null
$stop = Join-Path $out 'stop-recording.flag'
$recorder = Join-Path $repo 'tools\audio\record_loopback.py'
$wav = Join-Path $out 'game-audio.wav'
$args = '"' + $recorder + '" "' + $wav + '" "' + $stop + '" ' + ($TimeoutSeconds + 120)
$rec = Start-Process -FilePath $Python -ArgumentList $args -WindowStyle Hidden -PassThru `
    -RedirectStandardOutput (Join-Path $out 'recorder.out') -RedirectStandardError (Join-Path $out 'recorder.err')
[void]$rec.Handle
try {
    Start-Sleep -Seconds 1
    if ($rec.HasExited) { throw ('Audio recorder failed: ' + (Get-Content (Join-Path $out 'recorder.err') -Raw)) }
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Record-Showcase.ps1') `
        -Name $Name -Segments $Segments -TimeoutSeconds $TimeoutSeconds -SkipGifs
    if ($LASTEXITCODE -ne 0) { throw 'Game-window recording failed; see recording output.' }
}
finally {
    New-Item -ItemType File -Path $stop | Out-Null
    if (-not $rec.WaitForExit(30000)) { $rec.Kill(); throw 'Audio recorder failed to stop and save within 30 seconds.' }
}
if ($rec.ExitCode -ne 0) { throw ('Audio capture failed (exit ' + $rec.ExitCode + '): ' + (Get-Content (Join-Path $out 'recorder.err') -Raw)) }
}
$wav = Join-Path $out 'game-audio.wav'
& $Python (Join-Path $repo 'tools\audio\analyze_capture.py') $out
if ($LASTEXITCODE -ne 0) { throw 'Audio analysis failed.' }
$video = Get-Content (Join-Path $out 'recording.json') -Raw | ConvertFrom-Json
$audio = Get-Content (Join-Path $out 'game-audio.json') -Raw | ConvertFrom-Json
$analysis = Get-Content (Join-Path $out 'audio-analysis.json') -Raw | ConvertFrom-Json
$audioStart = [DateTimeOffset]::FromUnixTimeMilliseconds([long]$audio.first_ms).UtcDateTime
$videoStart = [DateTime]::Parse($video.videoStartUtc).ToUniversalTime()
$review = Join-Path $out 'review'; New-Item -ItemType Directory -Force -Path $review | Out-Null
$inv = [Globalization.CultureInfo]::InvariantCulture
foreach ($line in Get-Content (Join-Path $out 'clips\offsets.txt')) {
    if ($line -notmatch '^(\S+) ([\d.]+) ([\d.]+)$') { continue }
    $clip = $Matches[1]; $offset = [double]::Parse($Matches[2], $inv)
    # Remove measured loopback startup latency so audio events match the video flash clock.
    $audioOffset = ($videoStart - $audioStart).TotalSeconds + $offset + [double]$analysis.latency_s
    if ($audioOffset -lt 0) { throw ('Audio starts after clip: ' + $clip) }
    & ffmpeg -hide_banner -loglevel error -n -i (Join-Path $out ('clips\' + $clip + '.mp4')) `
        -ss $audioOffset.ToString('0.000', $inv) -i $wav -map 0:v:0 -map 1:a:0 `
        -c:v copy -c:a aac -b:a 192k -t $Matches[3] -movflags +faststart (Join-Path $review ($clip + '.mp4'))
    if ($LASTEXITCODE -ne 0) { throw ('Review audio mux failed: ' + $clip) }
}
'review=' + $review
