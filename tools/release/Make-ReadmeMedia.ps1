# Trims the showcase clips (tools\release\Record-Showcase.ps1) to their best windows and writes the README
# GIFs to docs\media (480 wide, 15 fps; committed, outside LFS) and matching 1280x720 MP4s to
# artifacts\<Name>\readme (for uploading to a GitHub release or video host; not committed).
# Usage: powershell -ExecutionPolicy Bypass -File tools\release\Make-ReadmeMedia.ps1 -Name showcase06
param([Parameter(Mandatory=$true)][string]$Name)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$clips = Join-Path $repo ("artifacts\" + $Name + "\clips")
$media = Join-Path $repo 'docs\media'
$videos = Join-Path $repo ("artifacts\" + $Name + "\readme")
New-Item -ItemType Directory -Force $media, $videos | Out-Null

# output name, source clip, start s, length s (seconds into the cut clip)
$cuts = @(
    @('hero',        'opencircuit', 3.0, 4.7),
    @('arc-bolt',    'arcbolt',     0.2, 5.0),
    @('stormspear',  'stormspear',  0.0, 4.6),
    @('arc-step',    'arcstep',     0.0, 4.6),
    @('gaze',        'gaze',        0.4, 5.6),
    @('storm',       'storm',       7.0, 8.5),
    @('skins',       'skins',       0.0, 10.2)
)
$inv = [Globalization.CultureInfo]::InvariantCulture
foreach ($c in $cuts) {
    $src = Join-Path $clips ($c[1] + '.mp4')
    if (-not (Test-Path $src)) { "skip " + $c[0] + " (no " + $c[1] + ".mp4)"; continue }
    $ss = ([double]$c[2]).ToString('0.00', $inv); $tt = ([double]$c[3]).ToString('0.00', $inv)
    $gif = Join-Path $media ($c[0] + '.gif'); $mp4 = Join-Path $videos ($c[0] + '.mp4')
    & ffmpeg -hide_banner -loglevel error -y -ss $ss -t $tt -i $src -c:v libx264 -preset slow -crf 24 -pix_fmt yuv420p -movflags +faststart -an $mp4
    & ffmpeg -hide_banner -loglevel error -y -ss $ss -t $tt -i $src -vf 'fps=15,scale=480:-1:flags=lanczos,split[a][b];[a]palettegen=max_colors=96:stats_mode=diff[p];[b][p]paletteuse=dither=bayer:bayer_scale=5:diff_mode=rectangle' -loop 0 $gif
    "{0,-13} gif={1,6:0} KB  mp4={2,6:0} KB" -f $c[0], ((Get-Item $gif).Length / 1KB), ((Get-Item $mp4).Length / 1KB)
}
