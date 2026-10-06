# Runs one autopilot pass while recording the game's audio output (WASAPI loopback).
# Output: artifacts\<Name>\ (autopilot files) + game-audio.wav/.json for tools\audio\analyze_capture.py.
# Usage: powershell -ExecutionPolicy Bypass -File tools\audio\Run-AudioCapture.ps1 -Name audio01 [-Segments storm] [-Python <venv python>]
param([Parameter(Mandatory=$true)][string]$Name, [string]$Segments = '', [string]$Python = "$env:LOCALAPPDATA\hs-audio-venv\Scripts\python.exe", [int]$TimeoutSeconds = 600)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$out = Join-Path $repo ("artifacts\" + $Name); New-Item -ItemType Directory -Force $out | Out-Null
$stop = Join-Path $out 'stop-recording.flag'; Remove-Item $stop -EA SilentlyContinue
$rec = Start-Process -FilePath $Python -ArgumentList @((Join-Path $PSScriptRoot 'record_loopback.py'), (Join-Path $out 'game-audio.wav'), $stop) -PassThru -WindowStyle Hidden -RedirectStandardOutput (Join-Path $out 'recorder.out') -RedirectStandardError (Join-Path $out 'recorder.err')
Start-Sleep 1
if ($Segments) { $env:HS_SEGMENTS = $Segments }
try { & powershell -ExecutionPolicy Bypass -File (Join-Path $repo 'tools\dev-profile\Run-Autopilot.ps1') -Name $Name -TimeoutSeconds $TimeoutSeconds }
finally { Remove-Item Env:HS_SEGMENTS -EA SilentlyContinue; New-Item -ItemType File $stop | Out-Null; $rec.WaitForExit(60000) | Out-Null }
Get-Content (Join-Path $out 'recorder.out') -EA SilentlyContinue
