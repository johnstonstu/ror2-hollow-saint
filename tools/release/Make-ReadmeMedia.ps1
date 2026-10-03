# Trims the showcase clips (tools\release\Record-Showcase.ps1) to their best windows and writes the README
# media to docs\media: animated WebP (20 fps; committed, outside LFS so Thunderstore can hotlink
# them), plus the skin lineup from the showcase stills. Matching 1280x720 MP4s go to artifacts\<Name>\readme.
# Usage: powershell -ExecutionPolicy Bypass -File tools\release\Make-ReadmeMedia.ps1 -Name showcase07
param([Parameter(Mandatory=$true)][string]$Name)
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
    @('gaze-hero',    'gaze-e',      0.4, 6.2, '1280:720:0:0', 960),
    @('crown',        'hero',        0.8, 5.2, '1120:630:80:40', 960),
    @('arc-bolt',     'arcbolt',     0.6, 4.8, $near, 640),
    @('stormspear',   'stormspear',  0.5, 5.5, $near, 640),
    @('arc-step',     'arcstep',     0.0, 4.6, $wide, 640),
    @('gaze',         'gaze-d',      0.4, 6.0, '1120:630:80:40', 640),
    @('open-circuit', 'opencircuit', 1.5, 6.5, $near, 640),
    @('storm',        'storm',       8.5, 7.0, $near, 640)
)
$inv = [Globalization.CultureInfo]::InvariantCulture
foreach ($c in $cuts) {
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
if (Test-Path (Join-Path $stills 'skin0.png')) {
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
