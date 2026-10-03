# Trims the showcase clips (tools\release\Record-Showcase.ps1) to their best windows and writes the README
# media to docs\media: animated WebP (20 fps; committed, outside LFS so Thunderstore can hotlink
# them), plus the skin lineup from the showcase stills. Matching 1280x720 MP4s go to artifacts\<Name>\readme.
# Usage: powershell -ExecutionPolicy Bypass -File tools\release\Make-ReadmeMedia.ps1 -Name showcase07
#        -Only gaze-hero writes just the listed outputs (no skin lineup), leaving the other media as they are.
# Cuts marked 'hq' (the README hero) use the hero HQ profile: cut from raw.mp4 (via clips\offsets.txt from
# Record-Showcase) instead of the re-encoded clip, 1280x720 at 30 fps (or the source rate if lower), WebP
# quality 90, method 6, no filtering beyond the scale. Over -MaxHqMB the cut is shortened (centred on the
# chosen window, at least 4 s) rather than lowering quality. -Hq applies the profile to every listed cut.
param([Parameter(Mandatory=$true)][string]$Name, [string[]]$Only, [switch]$Hq, [double]$MaxHqMB = 9.8)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$clips = Join-Path $repo ("artifacts\" + $Name + "\clips")
$stills = Join-Path $repo ("artifacts\" + $Name + "\stills")
$media = Join-Path $repo 'docs\media'
$videos = Join-Path $repo ("artifacts\" + $Name + "\readme")
New-Item -ItemType Directory -Force $media, $videos | Out-Null

# output name, source clip, start s, length s, crop of the 1280x720 clip (w:h:x:y; zooms in on the Saint), WebP width
# The two full-width clips at the top of the README (gaze-hero, crown) are encoded larger than the per-skill clips.
$wide = '1280:720:0:0'; $near = '1040:585:120:110'
$cuts = @(
    @('gaze-hero',    'gaze-e',      1.4, 4.6, '1280:720:0:0', 1280, 'hq'),
    @('crown',        'hero',        0.8, 5.2, '1120:630:80:40', 960),
    @('arc-bolt',     'arcbolt',     0.6, 4.8, $near, 640),
    @('stormspear',   'stormspear',  0.5, 5.5, $near, 640),
    @('arc-step',     'arcstep',     0.0, 4.6, $wide, 640),
    @('gaze',         'gaze-d',      0.4, 6.0, '1120:630:80:40', 640),
    @('open-circuit', 'opencircuit', 1.5, 6.5, $near, 640),
    @('storm',        'storm',       8.5, 7.0, $near, 640)
)
$inv = [Globalization.CultureInfo]::InvariantCulture
$raw = Join-Path $repo ("artifacts\" + $Name + "\raw.mp4")
$offsets = @{}
$offsetFile = Join-Path $clips 'offsets.txt'
if (Test-Path $offsetFile) {
    foreach ($line in Get-Content $offsetFile) { if ($line -match '^(\S+) ([\d\.]+) ') { $offsets[$Matches[1]] = [double]::Parse($Matches[2], $inv) } }
}
foreach ($c in $cuts) {
    if ($Only -and $Only -notcontains $c[0]) { continue }
    if (($Hq -or $c.Count -gt 6) -and (Test-Path $raw) -and $offsets.ContainsKey($c[1])) {
        # Hero HQ: from the raw recording; the crop is given in 1280x720 clip pixels, so scale to 720p first.
        $fps = 30
        $rate = (& ffprobe -v error -select_streams v:0 -show_entries stream=avg_frame_rate -of csv=p=0 $raw) -split '/'
        if ($rate.Count -eq 2 -and [double]$rate[1] -gt 0) { $fps = [Math]::Min(30, [Math]::Round([double]$rate[0] / [double]$rate[1])) }
        [double]$start = $offsets[$c[1]] + [double]$c[2]; [double]$len = [double]$c[3]
        $webp = Join-Path $media ($c[0] + '.webp'); $mp4 = Join-Path $videos ($c[0] + '.mp4')
        $vf = 'scale=1280:720:flags=lanczos,crop=' + $c[4] + ',scale=' + $c[5] + ':-2:flags=lanczos'
        while ($true) {
            $ss = $start.ToString('0.000', $inv); $tt = $len.ToString('0.000', $inv)
            & ffmpeg -hide_banner -loglevel error -y -ss $ss -t $tt -i $raw -vf ('fps=' + $fps + ',' + $vf) -c:v libwebp_anim -lossless 0 -quality 90 -compression_level 6 -loop 0 -an $webp
            $mb = (Get-Item $webp).Length / 1MB
            if ($mb -le $MaxHqMB -or $len -le 4.0) { break }
            [double]$shorter = [Math]::Max(4.0, [Math]::Floor($len * $MaxHqMB / $mb * 10) / 10)
            $start += ($len - $shorter) / 2; $len = $shorter
            "{0}: {1:0.0} MB, shortening to {2:0.0} s" -f $c[0], $mb, $len
        }
        & ffmpeg -hide_banner -loglevel error -y -ss $ss -t $tt -i $raw -vf ('fps=' + $fps + ',' + $vf) -c:v libx264 -preset slow -crf 18 -pix_fmt yuv420p -movflags +faststart -an $mp4
        "{0,-13} hq {1}w {2} fps {3:0.0} s from raw {4} s  webp={5,6:0} KB  mp4={6,6:0} KB" -f $c[0], $c[5], $fps, $len, $ss, ((Get-Item $webp).Length / 1KB), ((Get-Item $mp4).Length / 1KB)
        continue
    }
    $src = Join-Path $clips ($c[1] + '.mp4')
    if (-not (Test-Path $src)) { "skip " + $c[0] + " (no " + $c[1] + ".mp4)"; continue }
    $ss = ([double]$c[2]).ToString('0.00', $inv); $tt = ([double]$c[3]).ToString('0.00', $inv)
    $webp = Join-Path $media ($c[0] + '.webp'); $mp4 = Join-Path $videos ($c[0] + '.mp4')
    $crop = 'crop=' + $c[4]
    & ffmpeg -hide_banner -loglevel error -y -ss $ss -t $tt -i $src -vf ($crop + ',scale=1280:720:flags=lanczos') -c:v libx264 -preset slow -crf 22 -pix_fmt yuv420p -movflags +faststart -an $mp4
    & ffmpeg -hide_banner -loglevel error -y -ss $ss -t $tt -i $src -vf ($crop + ',fps=20,scale=' + $c[5] + ':-2:flags=lanczos') -c:v libwebp_anim -quality 72 -compression_level 6 -loop 0 -an $webp
    "{0,-13} webp={1,6:0} KB  mp4={2,6:0} KB" -f $c[0], ((Get-Item $webp).Length / 1KB), ((Get-Item $mp4).Length / 1KB)
}

# Skin lineup: the Saint cropped from each front-facing still, side by side, names underneath.
$names = 'Cracked Icon', 'Obsidian Saint', 'Verdigris Relic', 'Solar Vespers', 'Umbral Choir'
if (-not $Only -and (Test-Path (Join-Path $stills 'skin0.png'))) {
    $font = 'C\:/Windows/Fonts/segoeuib.ttf'
    $inputs = @(); $parts = @()
    for ($i = 0; $i -lt 5; $i++) {
        $inputs += '-i'; $inputs += (Join-Path $stills ("skin$i.png"))
        $parts += "[${i}:v]crop=440:660:740:360,scale=330:495,pad=330:560:0:0:color=0x16172a,drawtext=fontfile='$font':text='" + $names[$i] + "':fontcolor=0xe4e4ee:fontsize=24:x=(w-text_w)/2:y=513[s$i]"
    }
    $filter = ($parts -join ';') + ';' + ((0..4 | ForEach-Object { "[s$_]" }) -join '') + 'hstack=inputs=5'
    & ffmpeg -hide_banner -loglevel error -y @inputs -filter_complex $filter -frames:v 1 (Join-Path $media 'skin-lineup.png')
    "skin-lineup.png " + [int]((Get-Item (Join-Path $media 'skin-lineup.png')).Length / 1KB) + " KB"
}
